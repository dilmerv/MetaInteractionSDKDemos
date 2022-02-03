﻿/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using static OVRSkeleton;

namespace Oculus.Interaction.Input
{
    public class FromOVRHandDataSource : DataSource<HandDataAsset, HandDataSourceConfig>
    {
        [Header("OVR Data Source")]
        [SerializeField, Interface(typeof(IOVRCameraRigRef))]
        private MonoBehaviour _cameraRigRef;

        [Header("Shared Configuration")]
        [SerializeField]
        private Handedness _handedness;

        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private MonoBehaviour _trackingToWorldTransformer;
        private ITrackingToWorldTransformer TrackingToWorldTransformer;

        [SerializeField, Interface(typeof(IHandSkeletonProvider))]
        private MonoBehaviour _handSkeletonProvider;
        private IHandSkeletonProvider HandSkeletonProvider;

        [SerializeField, Interface(typeof(IDataSource<HmdDataAsset, HmdDataSourceConfig>))]
        private MonoBehaviour _hmdData;
        private IDataSource<HmdDataAsset, HmdDataSourceConfig> HmdData;

        private readonly HandDataAsset _handDataAsset = new HandDataAsset();
        private OVRHand _ovrHand;
        private OVRInput.Controller _ovrController;
        private float _lastHandScale;
        private HandDataSourceConfig _config;

        private IOVRCameraRigRef CameraRigRef;

        protected override HandDataAsset DataAsset => _handDataAsset;

        // Wrist rotations that come from OVR need correcting.
        public static Quaternion WristFixupRotation { get; } =
            new Quaternion(0.0f, 1.0f, 0.0f, 0.0f);

        // It is important that this creates an object on the fly, as it is possible it is called
        // from other components Start methods.
        public override HandDataSourceConfig Config => _config ?? InitConfig();

        protected virtual void Awake()
        {
            TrackingToWorldTransformer = _trackingToWorldTransformer as ITrackingToWorldTransformer;
            HmdData = _hmdData as IDataSource<HmdDataAsset, HmdDataSourceConfig>;
            CameraRigRef = _cameraRigRef as IOVRCameraRigRef;
            HandSkeletonProvider = _handSkeletonProvider as IHandSkeletonProvider;
        }

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(CameraRigRef);
            Assert.IsNotNull(TrackingToWorldTransformer);
            Assert.IsNotNull(HandSkeletonProvider);
            Assert.IsNotNull(HmdData);
            if (_handedness == Handedness.Left)
            {
                _ovrHand = CameraRigRef.LeftHand;
                _ovrController = OVRInput.Controller.LHand;
            }
            else
            {
                _ovrHand = CameraRigRef.RightHand;
                _ovrController = OVRInput.Controller.RHand;
            }
        }

        private HandDataSourceConfig InitConfig()
        {
            if (_config != null)
            {
                return _config;
            }

            _config = new HandDataSourceConfig()
            {
                Handedness = _handedness,
                TrackingToWorldTransformer = TrackingToWorldTransformer,
                HandSkeleton = HandSkeletonProvider[_handedness],
                HmdData = HmdData
            };

            return _config;
        }

        protected override void UpdateData()
        {
            _handDataAsset.IsDataValid = true;
            _handDataAsset.IsConnected =
                (OVRInput.GetConnectedControllers() & _ovrController) > 0;

            if (_ovrHand != null)
            {
                var skeletonProvider = (IOVRSkeletonDataProvider)_ovrHand;
                var poseData = skeletonProvider.GetSkeletonPoseData();
                if (poseData.IsDataValid && poseData.RootScale <= 0.0f)
                {
                    if (_lastHandScale <= 0.0f)
                    {
                        poseData.IsDataValid = false;
                    }
                    else
                    {
                        poseData.RootScale = _lastHandScale;
                    }
                }
                else
                {
                    _lastHandScale = poseData.RootScale;
                }

                if (poseData.IsDataValid && _handDataAsset.IsConnected)
                {
                    UpdateDataPoses(poseData);
                    return;
                }
            }

            // revert state fields to their defaults
            _handDataAsset.IsConnected = default;
            _handDataAsset.IsTracked = default;
            _handDataAsset.RootPoseOrigin = default;
            _handDataAsset.PointerPoseOrigin = default;
            _handDataAsset.IsHighConfidence = default;
            for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; fingerIdx++)
            {
                _handDataAsset.IsFingerPinching[fingerIdx] = default;
                _handDataAsset.IsFingerHighConfidence[fingerIdx] = default;
            }
        }

        private void UpdateDataPoses(SkeletonPoseData poseData)
        {
            _handDataAsset.HandScale = poseData.RootScale;
            _handDataAsset.IsTracked = _ovrHand.IsTracked;
            _handDataAsset.IsHighConfidence = poseData.IsDataHighConfidence;
            _handDataAsset.IsDominantHand = _ovrHand.IsDominantHand;
            _handDataAsset.RootPoseOrigin = _handDataAsset.IsTracked
                ? PoseOrigin.RawTrackedPose
                : PoseOrigin.None;

            for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; fingerIdx++)
            {
                var ovrFingerIdx = (OVRHand.HandFinger)fingerIdx;
                bool isPinching = _ovrHand.GetFingerIsPinching(ovrFingerIdx);
                _handDataAsset.IsFingerPinching[fingerIdx] = isPinching;

                bool isHighConfidence =
                    _ovrHand.GetFingerConfidence(ovrFingerIdx) == OVRHand.TrackingConfidence.High;
                _handDataAsset.IsFingerHighConfidence[fingerIdx] = isHighConfidence;

                float fingerPinchStrength = _ovrHand.GetFingerPinchStrength(ovrFingerIdx);
                _handDataAsset.FingerPinchStrength[fingerIdx] = fingerPinchStrength;
            }

            // Read the poses directly from the poseData, so it isn't in conflict with
            // any modifications that the application makes to OVRSkeleton
            _handDataAsset.Root = new Pose()
            {
                position = poseData.RootPose.Position.FromFlippedZVector3f(),
                rotation = poseData.RootPose.Orientation.FromFlippedZQuatf()
            };

            if (_ovrHand.IsPointerPoseValid)
            {
                _handDataAsset.PointerPoseOrigin = PoseOrigin.RawTrackedPose;
                _handDataAsset.PointerPose = new Pose(_ovrHand.PointerPose.localPosition,
                        _ovrHand.PointerPose.localRotation);
            }
            else
            {
                _handDataAsset.PointerPoseOrigin = PoseOrigin.None;
            }

            // Hand joint rotations X axis needs flipping to get to Unity's coordinate system.
            var bones = poseData.BoneRotations;
            for (int i = 0; i < bones.Length; i++)
            {
                // When using Link in the Unity Editor, the first frame of hand data
                // sometimes contains bad joint data.
                _handDataAsset.Joints[i] = float.IsNaN(bones[i].w)
                    ? Config.HandSkeleton.joints[i].pose.rotation
                    : bones[i].FromFlippedXQuatf();
            }

            _handDataAsset.Joints[0] = WristFixupRotation;
        }

        #region Inject

        public void InjectAllFromOVRHandDataSource(UpdateModeFlags updateMode, IDataSource updateAfter,
            Handedness handedness, ITrackingToWorldTransformer trackingToWorldTransformer,
            IHandSkeletonProvider handSkeletonProvider, IDataSource<HmdDataAsset, HmdDataSourceConfig> hmdData)
        {
            base.InjectAllDataSource(updateMode, updateAfter);
            InjectHandedness(handedness);
            InjectTrackingToWorldTransformer(trackingToWorldTransformer);
            InjectHandSkeletonProvider(handSkeletonProvider);
            InjectHmdData(hmdData);
        }

        public void InjectHandedness(Handedness handedness)
        {
            _handedness = handedness;
        }

        public void InjectTrackingToWorldTransformer(ITrackingToWorldTransformer trackingToWorldTransformer)
        {
            _trackingToWorldTransformer = trackingToWorldTransformer as MonoBehaviour;
            TrackingToWorldTransformer = trackingToWorldTransformer;
        }

        public void InjectHandSkeletonProvider(IHandSkeletonProvider handSkeletonProvider)
        {
            _handSkeletonProvider = handSkeletonProvider as MonoBehaviour;
            HandSkeletonProvider = handSkeletonProvider;
        }

        public void InjectHmdData(IDataSource<HmdDataAsset,HmdDataSourceConfig> hmdData)
        {
            _hmdData = hmdData as MonoBehaviour;
            HmdData = hmdData;
        }

        #endregion
    }
}
