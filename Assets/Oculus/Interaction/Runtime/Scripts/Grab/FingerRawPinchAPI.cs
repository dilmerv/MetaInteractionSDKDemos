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
    public class FingerRawPinchAPI : IFingerAPI
    {
        private class FingerPinchData
        {
            private readonly HandFinger _finger;
            private readonly HandJointId _tipId;

            public float PinchStrength;
            public bool IsPinching;
            public bool IsPinchingChanged;

            public Vector3 TipPosition { get; private set; }

            public FingerPinchData(HandFinger fingerId)
            {
                _finger = fingerId;
                _tipId = HandJointUtils.GetHandFingerTip(fingerId);
            }

            public void UpdateTipPosition(IHand hand)
            {
                if (hand.GetJointPoseFromWrist(_tipId, out Pose pose))
                {
                    TipPosition = pose.position;
                }
            }

            public void UpdateIsPinching(IHand hand)
            {
                PinchStrength = hand.GetFingerPinchStrength(_finger);
                bool isPinching = hand.GetFingerIsPinching(_finger);
                IsPinchingChanged = isPinching != IsPinching;
                IsPinching = isPinching;
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
            return _fingersPinchData[(int)finger].IsPinchingChanged &&
                   _fingersPinchData[(int)finger].IsPinching == targetPinchState;
        }

        public float GetFingerGrabStrength(HandFinger finger)
        {
            return _fingersPinchData[(int)finger].PinchStrength;
        }

        public void Update(IHand hand)
        {
            for (int i = 0; i < Constants.NUM_FINGERS; ++i)
            {
                _fingersPinchData[i].UpdateIsPinching(hand);
            }

        }
    }
}
