/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction.PoseDetection
{
    public enum TransformFeature
    {
        WristUp,
        WristDown,
        PalmDown,
        PalmUp,
        PalmTowardsFace,
        PalmAwayFromFace,
        FingersUp,
        FingersDown,
        PinchClear
    };

    public class TransformFeatureValueProvider
    {
        public struct TransformProperties
        {
            public TransformProperties(Pose centerEyePos,
                Pose wristPose,
                Handedness handedness,
                Vector3 trackingSystemUp)
            {
                CenterEyePose = centerEyePos;
                WristPose = wristPose;
                Handedness = handedness;
                TrackingSystemUp = trackingSystemUp;
            }

            public readonly Pose CenterEyePose;
            public readonly Pose WristPose;
            public readonly Handedness Handedness;
            public readonly Vector3 TrackingSystemUp;
        }

        public static float GetValue(TransformFeature transformFeature, TransformJointData transformJointData,
            TransformConfig transformConfig)
        {
            TransformProperties transformProps =
                new TransformProperties(transformJointData.CenterEyePose, transformJointData.WristPose,
                    transformJointData.Handedness, transformJointData.TrackingSystemUp);
            switch (transformFeature)
            {
                case TransformFeature.WristDown:
                    return GetWristDownValue(in transformProps, in transformConfig);
                case TransformFeature.WristUp:
                    return GetWristUpValue(in transformProps, in transformConfig);
                case TransformFeature.PalmDown:
                    return GetPalmDownValue(in transformProps, in transformConfig);
                case TransformFeature.PalmUp:
                    return GetPalmUpValue(in transformProps, in transformConfig);
                case TransformFeature.PalmTowardsFace:
                    return GetPalmTowardsFaceValue(in transformProps, in transformConfig);
                case TransformFeature.PalmAwayFromFace:
                    return GetPalmAwayFromFaceValue(in transformProps, in transformConfig);
                case TransformFeature.FingersUp:
                    return GetFingersUpValue(in transformProps, in transformConfig);
                case TransformFeature.FingersDown:
                    return GetFingersDownValue(in transformProps, in transformConfig);
                case TransformFeature.PinchClear:
                default:
                    return GetPinchClearValue(in transformProps, in transformConfig);
            }
        }

        public static Vector3 GetHandVectorForFeature(TransformFeature transformFeature,
            in TransformJointData transformJointData,
            in TransformConfig transformConfig)
        {
            TransformProperties transformProps =
                new TransformProperties(transformJointData.CenterEyePose, transformJointData.WristPose,
                    transformJointData.Handedness, transformJointData.TrackingSystemUp);
            return GetHandVectorForFeature(transformFeature, in transformProps, in transformConfig);
        }

        private static Vector3 GetHandVectorForFeature(TransformFeature transformFeature,
            in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            Vector3 handVector = Vector3.zero;
            switch (transformFeature)
            {
                case TransformFeature.WristDown:
                case TransformFeature.WristUp:
                    handVector = transformProps.Handedness == Handedness.Left ?
                        transformProps.WristPose.forward :
                        -1.0f * transformProps.WristPose.forward;
                    break;
                case TransformFeature.PalmDown:
                case TransformFeature.PalmUp:
                case TransformFeature.PalmTowardsFace:
                case TransformFeature.PalmAwayFromFace:
                    handVector = transformProps.Handedness == Handedness.Left ?
                        transformProps.WristPose.up : -1.0f * transformProps.WristPose.up;
                    break;
                case TransformFeature.FingersUp:
                case TransformFeature.FingersDown:
                    handVector = transformProps.Handedness == Handedness.Left ?
                        transformProps.WristPose.right : -1.0f * transformProps.WristPose.right;
                    break;
                case TransformFeature.PinchClear:
                default:
                    handVector = transformProps.Handedness == Handedness.Left ?
                        transformProps.WristPose.forward : -1.0f * transformProps.WristPose.forward;
                    break;
            }
            return handVector;
        }

        public static Vector3 GetTargetVectorForFeature(TransformFeature transformFeature,
            in TransformJointData transformJointData,
            in TransformConfig transformConfig)
        {
            TransformProperties transformProps =
                new TransformProperties(transformJointData.CenterEyePose, transformJointData.WristPose,
                    transformJointData.Handedness, transformJointData.TrackingSystemUp);
            return GetTargetVectorForFeature(transformFeature, in transformProps, in transformConfig);
        }

        private static Vector3 GetTargetVectorForFeature(TransformFeature transformFeature,
           in TransformProperties transformProps,
           in TransformConfig transformConfig)
        {
            Vector3 targetVector = Vector3.zero;
            switch (transformFeature)
            {
                case TransformFeature.WristDown:
                case TransformFeature.PalmDown:
                case TransformFeature.FingersDown:
                    targetVector = OffsetVectorWithRotation(
                        GetVerticalVector(transformProps.CenterEyePose,
                            transformProps.TrackingSystemUp, false,
                            in transformConfig),
                        in transformConfig);
                    break;
                case TransformFeature.WristUp:
                case TransformFeature.PalmUp:
                case TransformFeature.FingersUp:
                    targetVector = OffsetVectorWithRotation(
                        GetVerticalVector(transformProps.CenterEyePose,
                            transformProps.TrackingSystemUp, true,
                            in transformConfig),
                        in transformConfig);
                    break;
                case TransformFeature.PalmTowardsFace:
                    targetVector = OffsetVectorWithRotation(
                        -1.0f * transformProps.CenterEyePose.forward,
                        in transformConfig);
                    break;
                case TransformFeature.PalmAwayFromFace:
                    targetVector = OffsetVectorWithRotation(
                        transformProps.CenterEyePose.forward,
                        in transformConfig);
                    break;
                case TransformFeature.PinchClear:
                    targetVector = OffsetVectorWithRotation(
                        -1.0f * transformProps.CenterEyePose.forward,
                        in transformConfig);
                    break;
                default:
                    break;
            }
            return targetVector;
        }

        private static float GetWristDownValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            var handVector = GetHandVectorForFeature(TransformFeature.WristDown,
                in transformProps,
                in transformConfig);
            var targetVector = GetTargetVectorForFeature(TransformFeature.WristDown,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetWristUpValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            var handVector = GetHandVectorForFeature(TransformFeature.WristUp,
                in transformProps,
                in transformConfig);
            var targetVector = GetTargetVectorForFeature(TransformFeature.WristUp,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetPalmDownValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            var handVector = GetHandVectorForFeature(TransformFeature.PalmDown,
                in transformProps,
                in transformConfig);
            var targetVector = GetTargetVectorForFeature(TransformFeature.PalmDown,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetPalmUpValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            var handVector = GetHandVectorForFeature(TransformFeature.PalmUp,
                in transformProps,
                in transformConfig);
            var targetVector = GetTargetVectorForFeature(TransformFeature.PalmUp,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetPalmTowardsFaceValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            var handVector = GetHandVectorForFeature(TransformFeature.PalmTowardsFace,
                in transformProps,
                in transformConfig);
            var targetVector = GetTargetVectorForFeature(TransformFeature.PalmTowardsFace,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetPalmAwayFromFaceValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            var handVector = GetHandVectorForFeature(TransformFeature.PalmAwayFromFace,
                in transformProps,
                in transformConfig);
            var targetVector = GetTargetVectorForFeature(TransformFeature.PalmAwayFromFace,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetFingersUpValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            var handVector = GetHandVectorForFeature(TransformFeature.FingersUp,
                in transformProps,
                in transformConfig);
            var targetVector = GetTargetVectorForFeature(TransformFeature.FingersUp,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetFingersDownValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            var handVector = GetHandVectorForFeature(TransformFeature.FingersDown,
                in transformProps,
                in transformConfig);
            var targetVector = GetTargetVectorForFeature(TransformFeature.FingersDown,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetPinchClearValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            var handVector = GetHandVectorForFeature(TransformFeature.PinchClear,
                in transformProps,
                in transformConfig);
            var targetVector = GetTargetVectorForFeature(TransformFeature.PinchClear,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static Vector3 GetVerticalVector(in Pose centerEyePose,
            in Vector3 trackingSystemUp,
            bool isUp,
            in TransformConfig transformConfig)
        {
            switch (transformConfig.UpVectorType)
            {
                case UpVectorType.Head:
                    return isUp ? centerEyePose.up : -1.0f * centerEyePose.up;
                case UpVectorType.Tracking:
                    return isUp ? trackingSystemUp : -1.0f * trackingSystemUp;
                case UpVectorType.World:
                default:
                    return isUp ? Vector3.up : Vector3.down;
            }
        }

        private static Vector3 OffsetVectorWithRotation(in Vector3 originalVector,
            in TransformConfig transformConfig)
        {
            Quaternion rotationAmount = Quaternion.Euler(transformConfig.RotationOffset);
            return rotationAmount * originalVector;
        }
    }
}
