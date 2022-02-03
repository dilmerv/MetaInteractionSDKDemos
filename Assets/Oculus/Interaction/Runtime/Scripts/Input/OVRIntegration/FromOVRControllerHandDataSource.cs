/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction.Input
{
    public class FromOVRControllerHandDataSource : DataSource<HandDataAsset, HandDataSourceConfig>
    {
        [SerializeField]
        private Transform[] _bones;

        [SerializeField]
        private AnimationCurve _pinchCurve = AnimationCurve.EaseInOut(0.1f, 0f, 0.9f, 1f);

        [SerializeField]
        private Vector3 _rootOffset;
        [SerializeField]
        private Vector3 _rootAngleOffset;

        [Header("OVR Data Source")]
        [SerializeField, Interface(typeof(IOVRCameraRigRef))]
        private MonoBehaviour _cameraRigRef;
        private IOVRCameraRigRef CameraRigRef;

        [Header("Shared Configuration")]
        [SerializeField]
        private Handedness _handedness;

        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private MonoBehaviour _trackingToWorldTransformer;
        private ITrackingToWorldTransformer TrackingToWorldTransformer;

        [SerializeField, Interface(typeof(IDataSource<HmdDataAsset, HmdDataSourceConfig>))]
        private MonoBehaviour _hmdData;
        private IDataSource<HmdDataAsset, HmdDataSourceConfig> HmdData;

        private readonly HandDataAsset _handDataAsset = new HandDataAsset();
        private OVRInput.Controller _ovrController;
        private Transform _ovrControllerAnchor;
        private HandDataSourceConfig _config;
        private Pose _poseOffset;


        public static Quaternion WristFixupRotation { get; } =
            new Quaternion(0.0f, 1.0f, 0.0f, 0.0f);

        protected override HandDataAsset DataAsset => _handDataAsset;

        private HandSkeleton _skeleton;

        public override HandDataSourceConfig Config
        {
            get
            {
                if (_config == null)
                {
                    InitConfig();
                }

                return _config;
            }
        }

        protected void Awake()
        {
            _skeleton = HandSkeletonOVR.CreateSkeletonData(_handedness);
            TrackingToWorldTransformer = _trackingToWorldTransformer as ITrackingToWorldTransformer;
            HmdData = _hmdData as IDataSource<HmdDataAsset, HmdDataSourceConfig>;
            CameraRigRef = _cameraRigRef as IOVRCameraRigRef;
        }

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(CameraRigRef);
            Assert.IsNotNull(TrackingToWorldTransformer);
            Assert.IsNotNull(HmdData);
            if (_handedness == Handedness.Left)
            {
                Assert.IsNotNull(CameraRigRef.LeftHand);
                _ovrControllerAnchor = CameraRigRef.LeftController;
                _ovrController = OVRInput.Controller.LTouch;
            }
            else
            {
                Assert.IsNotNull(CameraRigRef.RightHand);
                _ovrControllerAnchor = CameraRigRef.RightController;
                _ovrController = OVRInput.Controller.RTouch;
            }

            Pose offset = new Pose(_rootOffset, Quaternion.Euler(_rootAngleOffset));
            if (_handedness == Handedness.Left)
            {
                offset.position.x = -offset.position.x;
                offset.rotation = Quaternion.Euler(180f, 0f, 0f) * offset.rotation;
            }
            _poseOffset = offset;

            InitConfig();

        }

        private void UpdateSkeleton()
        {
            for (int i = 0; i < _skeleton.joints.Length; i++)
            {
                _skeleton.joints[i].pose.position = _bones[i].localPosition;
                _skeleton.joints[i].pose.rotation = _bones[i].localRotation;
            }
        }

        private void InitConfig()
        {
            if (_config != null)
            {
                return;
            }

            UpdateSkeleton();

            _config = new HandDataSourceConfig()
            {
                Handedness = _handedness,
                TrackingToWorldTransformer = TrackingToWorldTransformer,
                HandSkeleton = _skeleton,
                HmdData = HmdData
            };
        }

        protected override void UpdateData()
        {
            _handDataAsset.IsDataValid = true;
            _handDataAsset.IsConnected = (OVRInput.GetConnectedControllers() & _ovrController) > 0;
            if (!_handDataAsset.IsConnected)
            {
                // revert state fields to their defaults
                _handDataAsset.IsTracked = default;
                _handDataAsset.RootPoseOrigin = default;
                _handDataAsset.PointerPoseOrigin = default;
                _handDataAsset.IsHighConfidence = default;
                for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; fingerIdx++)
                {
                    _handDataAsset.IsFingerPinching[fingerIdx] = default;
                    _handDataAsset.IsFingerHighConfidence[fingerIdx] = default;
                }
                return;
            }

            _handDataAsset.IsTracked = true;
            _handDataAsset.IsHighConfidence = true;
            _handDataAsset.HandScale = 1f;

            _handDataAsset.IsDominantHand =
                OVRInput.GetDominantHand() == OVRInput.Handedness.LeftHanded
                && _handedness == Handedness.Left
                || (OVRInput.GetDominantHand() == OVRInput.Handedness.RightHanded
                    && _handedness == Handedness.Right);

            float indexStrength = _pinchCurve.Evaluate(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, _ovrController));
            float gripStrength = _pinchCurve.Evaluate(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, _ovrController));

            _handDataAsset.IsFingerHighConfidence[(int)HandFinger.Thumb] = true;
            _handDataAsset.IsFingerPinching[(int)HandFinger.Thumb] = indexStrength >= 1f || gripStrength >= 1f;
            _handDataAsset.FingerPinchStrength[(int)HandFinger.Thumb] = Mathf.Max(indexStrength, gripStrength);

            _handDataAsset.IsFingerHighConfidence[(int)HandFinger.Index] = true;
            _handDataAsset.IsFingerPinching[(int)HandFinger.Index] = indexStrength >= 1f;
            _handDataAsset.FingerPinchStrength[(int)HandFinger.Index] = indexStrength;

            _handDataAsset.IsFingerHighConfidence[(int)HandFinger.Middle] = true;
            _handDataAsset.IsFingerPinching[(int)HandFinger.Middle] = gripStrength >= 1f;
            _handDataAsset.FingerPinchStrength[(int)HandFinger.Middle] = gripStrength;

            _handDataAsset.IsFingerHighConfidence[(int)HandFinger.Ring] = true;
            _handDataAsset.IsFingerPinching[(int)HandFinger.Ring] = gripStrength >= 1f;
            _handDataAsset.FingerPinchStrength[(int)HandFinger.Ring] = gripStrength;

            _handDataAsset.IsFingerHighConfidence[(int)HandFinger.Pinky] = true;
            _handDataAsset.IsFingerPinching[(int)HandFinger.Pinky] = gripStrength >= 1f;
            _handDataAsset.FingerPinchStrength[(int)HandFinger.Pinky] = gripStrength;

            _handDataAsset.PointerPoseOrigin = PoseOrigin.RawTrackedPose;
            _handDataAsset.PointerPose = new Pose(
                OVRInput.GetLocalControllerPosition(_ovrController),
                OVRInput.GetLocalControllerRotation(_ovrController));

            for (int i = 0; i < _bones.Length; i++)
            {
                _handDataAsset.Joints[i] = _bones[i].localRotation;
            }

            _handDataAsset.Joints[0] = WristFixupRotation;

            // Convert controller pose from world to tracking space.
            Pose pose = new Pose(_ovrControllerAnchor.position, _ovrControllerAnchor.rotation);
            pose = Config.TrackingToWorldTransformer.ToTrackingPose(pose);

            PoseUtils.Multiply(pose, _poseOffset, ref _handDataAsset.Root);
            _handDataAsset.RootPoseOrigin = PoseOrigin.RawTrackedPose;
        }

        #region Inject

        public void InjectAllFromOVRControllerHandDataSource(UpdateModeFlags updateMode, IDataSource updateAfter,
            Handedness handedness, ITrackingToWorldTransformer trackingToWorldTransformer,
            IDataSource<HmdDataAsset, HmdDataSourceConfig> hmdData, Transform[] bones, AnimationCurve pinchCurve,
            Vector3 rootOffset, Vector3 rootAngleOffset)
        {
            base.InjectAllDataSource(updateMode, updateAfter);
            InjectHandedness(handedness);
            InjectTrackingToWorldTransformer(trackingToWorldTransformer);
            InjectHmdData(hmdData);
            InjectBones(bones);
            InjectPinchCurve(pinchCurve);
            InjectRootOffset(rootOffset);
            InjectRootAngleOffset(rootAngleOffset);
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

        public void InjectHmdData(IDataSource<HmdDataAsset, HmdDataSourceConfig> hmdData)
        {
            _hmdData = hmdData as MonoBehaviour;
            HmdData = hmdData;
        }

        public void InjectBones(Transform[] bones)
        {
            _bones = bones;
        }

        public void InjectPinchCurve(AnimationCurve pinchCurve)
        {
            _pinchCurve = pinchCurve;
        }

        public void InjectRootOffset(Vector3 rootOffset)
        {
            _rootOffset = rootOffset;
        }

        public void InjectRootAngleOffset(Vector3 rootAngleOffset)
        {
            _rootAngleOffset = rootAngleOffset;
        }

        #endregion
    }
}
