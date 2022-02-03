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

namespace Oculus.Interaction.GrabAPI
{
    public class FingerPinchAPI : IFingerAPI
    {
        private bool _isPinchVisibilityGood;
        private float DistanceStart => _isPinchVisibilityGood ? PINCH_HQ_DISTANCE_START : PINCH_DISTANCE_START;
        private float DistanceStopMax => _isPinchVisibilityGood ? PINCH_HQ_DISTANCE_STOP_MAX : PINCH_DISTANCE_STOP_MAX;
        private float DistanceStopOffset => _isPinchVisibilityGood ? PINCH_HQ_DISTANCE_STOP_OFFSET : PINCH_DISTANCE_STOP_OFFSET;

        private const float PINCH_DISTANCE_START = 0.03f;
        private const float PINCH_DISTANCE_STOP_MAX = 0.1f;
        private const float PINCH_DISTANCE_STOP_OFFSET = 0.04f;

        private const float PINCH_HQ_DISTANCE_START = 0.016f;
        private const float PINCH_HQ_DISTANCE_STOP_MAX = 0.1f;
        private const float PINCH_HQ_DISTANCE_STOP_OFFSET = 0.016f;

        private const float PINCH_HQ_VIEW_ANGLE_THRESHOLD = 40f;

        private HandJointId[] thumbJointList = new[]
        {
            HandJointId.HandThumb0,
            HandJointId.HandThumb1,
            HandJointId.HandThumb2,
            HandJointId.HandThumb3,
            HandJointId.HandThumbTip
        };

        private class FingerPinchData
        {
            private readonly HandJointId _tipId;
            private float _minPinchDistance;

            public Vector3 TipPosition { get; private set; }
            public float PinchStrength;
            public bool IsPinching;
            public bool IsPinchingChanged;

            public FingerPinchData(HandFinger fingerId)
            {
                _tipId = HandJointUtils.GetHandFingerTip(fingerId);
            }

            public void UpdateTipPosition(IHand hand)
            {
                if (hand.GetJointPoseFromWrist(_tipId, out Pose pose))
                {
                    TipPosition = pose.position;
                }
            }

            public void UpdateIsPinching(float distance, float start, float stopOffset, float stopMax)
            {
                IsPinchingChanged = false;
                if (!IsPinching)
                {
                    if (distance < start)
                    {
                        IsPinching = true;
                        IsPinchingChanged = true;
                        _minPinchDistance = distance;
                    }
                }
                else
                {
                    _minPinchDistance = Mathf.Min(_minPinchDistance, distance);
                    if (distance > stopMax ||
                        distance > _minPinchDistance + stopOffset)
                    {
                        IsPinching = false;
                        IsPinchingChanged = true;
                        _minPinchDistance = float.MaxValue;
                    }
                }
            }
        }

        private readonly FingerPinchData[] _fingersPinchData =
        {
            new FingerPinchData(HandFinger.Thumb),
            new FingerPinchData(HandFinger.Index),
            new FingerPinchData(HandFinger.Middle),
            new FingerPinchData(HandFinger.Ring),
            new FingerPinchData(HandFinger.Pinky)
        };

        public bool GetFingerIsGrabbing(HandFinger finger)
        {
            if (finger == HandFinger.Thumb)
            {
                for (int i = 1; i < Constants.NUM_FINGERS; ++i)
                {
                    if (_fingersPinchData[i].IsPinching)
                    {
                        return true;
                    }
                }

                return false;
            }

            return _fingersPinchData[(int)finger].IsPinching;
        }

        public Vector3 GetCenterOffset()
        {
            float maxStrength = float.NegativeInfinity;
            Vector3 thumbTip = _fingersPinchData[0].TipPosition;
            Vector3 center = thumbTip;

            for (int i = 1; i < Constants.NUM_FINGERS; ++i)
            {
                float strength = _fingersPinchData[i].PinchStrength;
                if (strength > maxStrength)
                {
                    maxStrength = strength;
                    Vector3 fingerTip = _fingersPinchData[i].TipPosition;
                    center = (thumbTip + fingerTip) * 0.5f;
                }
            }

            return center;
        }

        public bool GetFingerIsGrabbingChanged(HandFinger finger, bool targetPinchState)
        {
            if (finger == HandFinger.Thumb)
            {
                // Thumb pinching changed logic only happens on
                // the first finger pinching changed when pinching,
                // or any finger pinching changed when not pinching
                bool pinching = GetFingerIsGrabbing(finger);
                if (pinching != targetPinchState)
                {
                    return false;
                }

                if (pinching)
                {
                    for (int i = 1; i < Constants.NUM_FINGERS; ++i)
                    {
                        if (_fingersPinchData[i].IsPinching == pinching &&
                            !_fingersPinchData[i].IsPinchingChanged)
                        {
                            return false;
                        }
                    }

                    return true;
                }
                else
                {
                    for (int i = 1; i < Constants.NUM_FINGERS; ++i)
                    {
                        if (_fingersPinchData[i].IsPinchingChanged)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return _fingersPinchData[(int)finger].IsPinchingChanged &&
                   _fingersPinchData[(int)finger].IsPinching == targetPinchState;
        }

        public float GetFingerGrabStrength(HandFinger finger)
        {
            if (finger == HandFinger.Thumb)
            {
                float max = 0.0f;
                for (int i = 1; i < Constants.NUM_FINGERS; ++i)
                {
                    max = Mathf.Max(max, _fingersPinchData[i].PinchStrength);
                }
                return max;
            }

            return _fingersPinchData[(int)finger].PinchStrength;
        }

        public void Update(IHand hand)
        {
            _isPinchVisibilityGood = PinchHasGoodVisibility(hand);
            for (int i = 0; i < Constants.NUM_FINGERS; ++i)
            {
                _fingersPinchData[i].UpdateTipPosition(hand);
            }

            Vector3 thumbTipPosition = _fingersPinchData[0].TipPosition;
            for (int i = 1; i < Constants.NUM_FINGERS; ++i)
            {
                float distance = GetClosestDistanceToThumb(hand, _fingersPinchData[i].TipPosition);
                _fingersPinchData[i].UpdateIsPinching(distance,
                    DistanceStart, DistanceStopOffset, DistanceStopMax);
                float pinchPercent = (distance - DistanceStart) /
                                                 (DistanceStopMax - DistanceStart);

                _fingersPinchData[i].PinchStrength = 1f - Mathf.Clamp01(pinchPercent);
            }
        }

        private float GetClosestDistanceToThumb(IHand hand, Vector3 position)
        {
            float minDistance = float.MaxValue;
            for (int i = 0; i < thumbJointList.Length - 1; i++)
            {
                if (!hand.GetJointPoseFromWrist(thumbJointList[i], out Pose pose0))
                {
                    return float.MaxValue;
                }
                if (!hand.GetJointPoseFromWrist(thumbJointList[i+1], out Pose pose1))
                {
                    return float.MaxValue;
                }

                minDistance = Mathf.Min(minDistance,
                    ClosestDistanceToLineSegment(position, pose0.position, pose1.position));
            }

            return minDistance;
        }

        private float ClosestDistanceToLineSegment(Vector3 position, Vector3 p0, Vector3 p1)
        {
            Vector3 lineVec = p1 - p0;
            Vector3 fromP0 = position - p0;
            float normalizedProjection = Vector3.Dot(fromP0, lineVec) / Vector3.Dot(lineVec, lineVec);
            float closestT = Mathf.Clamp01(normalizedProjection);
            Vector3 closestPoint = p0 + closestT * lineVec;
            return (closestPoint - position).magnitude;
        }

        private bool PinchHasGoodVisibility(IHand hand)
        {
            if (!hand.GetJointPose(HandJointId.HandWristRoot, out Pose wristPose)
                || !hand.GetCenterEyePose(out Pose centerEyePose))
            {
                return false;
            }

            Vector3 handVector = -1.0f * wristPose.forward;
            Vector3 targetVector = -1.0f * centerEyePose.forward;

            if (hand.Handedness == Handedness.Right)
            {
                handVector = -handVector;
            }

            float angle = Vector3.Angle(handVector, targetVector);
            return angle <= PINCH_HQ_VIEW_ANGLE_THRESHOLD;
        }
    }
}
