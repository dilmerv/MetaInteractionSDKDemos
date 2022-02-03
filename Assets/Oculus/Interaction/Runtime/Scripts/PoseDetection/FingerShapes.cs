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
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.PoseDetection
{
    public enum FingerFeature
    {
        Curl,
        Flexion,
        Abduction,
        Opposition
    }

    public class FingerShapes
    {
        #region Joints Visualization Mappings
        private static readonly HandJointId[][] CURL_LINE_JOINTS =
        {
            new [] {HandJointId.HandThumb2, HandJointId.HandThumb3, HandJointId.HandThumbTip},
            new [] {HandJointId.HandIndex2, HandJointId.HandIndex3, HandJointId.HandIndexTip},
            new [] {HandJointId.HandMiddle2, HandJointId.HandMiddle3, HandJointId.HandMiddleTip},
            new [] {HandJointId.HandRing2, HandJointId.HandRing3, HandJointId.HandRingTip},
            new [] {HandJointId.HandPinky2, HandJointId.HandPinky3, HandJointId.HandPinkyTip}
        };
        private static readonly HandJointId[][] FLEXION_LINE_JOINTS =
        {
            new [] {HandJointId.HandThumb1, HandJointId.HandThumb2, HandJointId.HandThumb3},
            new [] {HandJointId.HandIndex1, HandJointId.HandIndex2, HandJointId.HandIndex3},
            new [] {HandJointId.HandMiddle1, HandJointId.HandMiddle2, HandJointId.HandMiddle3},
            new [] {HandJointId.HandRing1, HandJointId.HandRing2, HandJointId.HandRing3},
            new [] {HandJointId.HandPinky1, HandJointId.HandPinky2, HandJointId.HandPinky3}
        };
        private static readonly HandJointId[][] ABDUCTION_LINE_JOINTS =
        {
            new [] {HandJointId.HandThumbTip, HandJointId.HandThumb1, HandJointId.HandIndex1, HandJointId.HandIndexTip},
            new [] {HandJointId.HandIndexTip, HandJointId.HandIndex1, HandJointId.HandMiddle1, HandJointId.HandMiddleTip},
            new [] {HandJointId.HandMiddleTip, HandJointId.HandMiddle1, HandJointId.HandRing1, HandJointId.HandRingTip},
            new [] {HandJointId.HandRingTip, HandJointId.HandRing1, HandJointId.HandPinky1, HandJointId.HandPinkyTip},
            Array.Empty<HandJointId>()
        };
        private static readonly HandJointId[][] OPPOSITION_LINE_JOINTS =
        {
            Array.Empty<HandJointId>(),
            new [] {HandJointId.HandThumbTip, HandJointId.HandIndexTip},
            new [] {HandJointId.HandThumbTip, HandJointId.HandMiddleTip},
            new [] {HandJointId.HandThumbTip, HandJointId.HandRingTip},
            new [] {HandJointId.HandThumbTip, HandJointId.HandPinkyTip},
        };
        #endregion

        #region Joint Calculation Mappings
        private static readonly HandJointId[][] CURL_ANGLE_JOINTS =
        {
            new[]
            {
                HandJointId.HandThumb1, HandJointId.HandThumb2, HandJointId.HandThumb3,
                HandJointId.HandThumbTip
            },
            new[]
            {
                HandJointId.HandIndex1, HandJointId.HandIndex2, HandJointId.HandIndex3,
                HandJointId.HandIndexTip
            },
            new[]
            {
                HandJointId.HandMiddle1, HandJointId.HandMiddle2, HandJointId.HandMiddle3,
                HandJointId.HandMiddleTip
            },
            new[]
            {
                HandJointId.HandRing1, HandJointId.HandRing2, HandJointId.HandRing3,
                HandJointId.HandRingTip
            },
            new[]
            {
                HandJointId.HandPinky1, HandJointId.HandPinky2, HandJointId.HandPinky3,
                HandJointId.HandPinkyTip
            }
        };

        private static readonly HandJointId[][] FLEXION_ANGLE_JOINTS =
        {
            new[]
            {
                HandJointId.HandWristRoot, HandJointId.HandThumb1, HandJointId.HandThumb2
            },
            new[]
            {
                HandJointId.HandWristRoot, HandJointId.HandIndex1, HandJointId.HandIndex2
            },
            new[]
            {
                HandJointId.HandWristRoot, HandJointId.HandMiddle1, HandJointId.HandMiddle2
            },
            new[]
            {
                HandJointId.HandWristRoot, HandJointId.HandRing1, HandJointId.HandRing2
            },
            new[]
            {
                HandJointId.HandWristRoot, HandJointId.HandPinky1, HandJointId.HandPinky2
            }
        };
        #endregion

        public virtual float GetValue(HandFinger finger, FingerFeature feature, IHand hand)
        {
            switch (feature)
            {
                case FingerFeature.Curl:
                    return GetCurlValue(finger, hand);
                case FingerFeature.Flexion:
                    return GetFlexionValue(finger, hand);
                case FingerFeature.Abduction:
                    return GetAbductionValue(finger, hand);
                case FingerFeature.Opposition:
                    return GetOppositionValue(finger, hand);

                default:
                    return 0.0f;
            }
        }

        protected float ComputeAngleSum(HandJointId[] joints, IHand hand)
        {
            if (!hand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses poses))
            {
                return 0.0f;
            }

            float angleSum = 0;
            for (int i = 0; i < joints.Length - 2; i++)
            {
                ref readonly Pose midpointPose = ref poses[joints[i + 1]];
                Vector3 pos0 = poses[joints[i]].position;
                Vector3 pos1 = midpointPose.position;
                Vector3 pos2 = poses[joints[i + 2]].position;
                Vector3 bone1 = pos0 - pos1;
                Vector3 bone2 = pos2 - pos1;
                float angle = Vector3.SignedAngle(bone1, bone2, midpointPose.forward * -1f);
                if (angle < 0f) angle += 360f;
                angleSum += angle;
            }
            return angleSum;
        }

        public float GetCurlValue(HandFinger finger, IHand hand)
        {
            HandJointId[] handJointIds = CURL_ANGLE_JOINTS[(int)finger];
            return ComputeAngleSum(handJointIds, hand) / (handJointIds.Length - 2);
        }

        public float GetFlexionValue(HandFinger finger, IHand hand)
        {
            return ComputeAngleSum(FLEXION_ANGLE_JOINTS[(int)finger], hand);
        }

        public float GetAbductionValue(HandFinger finger, IHand hand)
        {
            if (finger == HandFinger.Pinky
                || !hand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses poses))
            {
                return 0.0f;
            }

            HandFinger nextFinger = finger + 1;
            Vector3 fingerProximal = poses[HandJointUtils.GetHandFingerProximal(finger)].position;
            Vector3 proximalMidpoint = Vector3.Lerp(
                fingerProximal,
                poses[HandJointUtils.GetHandFingerProximal(nextFinger)].position,
                0.5f);
            Vector3 normal1;
            if (finger == HandFinger.Thumb)
            {
                normal1 = poses[HandJointUtils.GetHandFingerTip(finger)].position -
                                  fingerProximal;
            }
            else
            {
                normal1 = poses[HandJointUtils.GetHandFingerTip(finger)].position -
                                  proximalMidpoint;
            }

            Vector3 normal2 = poses[HandJointUtils.GetHandFingerTip(nextFinger)].position -
                              proximalMidpoint;
            Vector3 axis = Vector3.Cross(normal1, normal2);
            return Vector3.SignedAngle(normal1, normal2, axis);
        }

        public float GetOppositionValue(HandFinger finger, IHand hand)
        {
            if (finger == HandFinger.Thumb
                || !hand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses poses))
            {
                return 0.0f;
            }

            Vector3 pos1 = poses[HandJointUtils.GetHandFingerTip(finger)].position;
            Vector3 pos2 = poses[HandJointId.HandThumbTip].position;
            return Vector3.Magnitude(pos1 - pos2);
        }

        public virtual IReadOnlyList<HandJointId> GetJointsAffected(HandFinger finger, FingerFeature feature)
        {
            switch (feature)
            {
                case FingerFeature.Curl:
                    return CURL_LINE_JOINTS[(int)finger];
                case FingerFeature.Flexion:
                    return FLEXION_LINE_JOINTS[(int)finger];
                case FingerFeature.Abduction:
                    return ABDUCTION_LINE_JOINTS[(int)finger];
                case FingerFeature.Opposition:
                    return OPPOSITION_LINE_JOINTS[(int)finger];
                default:
                    return null;
            }
        }
    }
}
