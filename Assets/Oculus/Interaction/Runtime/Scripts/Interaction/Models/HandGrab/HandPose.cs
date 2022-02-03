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
using UnityEngine.Serialization;

namespace Oculus.Interaction.HandPosing
{
    /// <summary>
    /// Data for the pose of a hand for grabbing an object.
    /// It contains not only the position of the hand and the fingers but
    /// other relevant data to the pose like the scale or which
    /// fingers are locked.
    ///
    /// Even though this class is Serializable it is not a Component.
    /// The HandPoseEditor class should be used
    /// (in conjunction with the HandGrabInteractableEditor class)
    /// to edit the values in the inspector.
    /// </summary>
    [System.Serializable]
    public class HandPose
    {
        [SerializeField]
        private Handedness _handedness;

        [SerializeField]
        private JointFreedom[] _fingersFreedom = FingersMetadata.DefaultFingersFreedom();

        [SerializeField]
        private Quaternion[] _jointRotations = new Quaternion[FingersMetadata.HAND_JOINT_IDS.Length];

        /// <summary>
        /// Handedness of the hand.
        /// </summary>
        public Handedness Handedness
        {
            get => _handedness;
            set => _handedness = value;
        }

        /// <summary>
        /// Collection of joints and their rotations in this hand.
        /// It follows the FingersMetadata.HAND_JOINT_IDS convention
        /// </summary>
        public Quaternion[] JointRotations
        {
            get
            {
                if (_jointRotations == null
                    || _jointRotations.Length == 0)
                {
                    _jointRotations = new Quaternion[FingersMetadata.HAND_JOINT_IDS.Length];
                }
                return _jointRotations;
            }
            set
            {
                _jointRotations = value;
            }
        }

        /// <summary>
        /// Indicates which fingers can be locked, constrained or free in this
        /// hand pose.
        /// It follows the Hand.HandFinger order for the collection.
        /// </summary>
        public JointFreedom[] FingersFreedom
        {
            get
            {
                if (_fingersFreedom == null
                    || _fingersFreedom.Length == 0)
                {
                    _fingersFreedom = FingersMetadata.DefaultFingersFreedom();
                }
                return _fingersFreedom;
            }
        }

        public HandPose()
        {
        }

        public HandPose(HandPose other)
        {
            this.CopyFrom(other);
        }

        /// <summary>
        /// Copies the values over to the hand pose
        /// without requiring any new allocations.
        ///
        /// This is thanks to the fact of the fingers freedom
        /// and joints rotations collections being always
        /// fixed size and order.
        /// </summary>
        /// <param name="from">The hand pose to copy the values from</param>
        public void CopyFrom(HandPose from)
        {
            this.Handedness = from.Handedness;
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                this.FingersFreedom[i] = from.FingersFreedom[i];
            }
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; i++)
            {
                this.JointRotations[i] = from.JointRotations[i];
            }
        }

        /// <summary>
        /// Interpolates between two HandPoses, if they have the same handedness and joints.
        /// </summary>
        /// <param name="from">Base HandPose to interpolate from.</param>
        /// <param name="to">Target HandPose to interpolate to.</param>
        /// <param name="t">Interpolation factor, 0 for the base, 1 for the target.</param>
        /// <param name="result">A HandPose positioned/rotated between the base and target.</param>
        public static void Lerp(in HandPose from, in HandPose to, float t, ref HandPose result)
        {
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; i++)
            {
                result.JointRotations[i] = Quaternion.SlerpUnclamped(from.JointRotations[i], to.JointRotations[i], t);
            }

            HandPose dominantPose = t <= 0.5f? from : to;
            result.Handedness = dominantPose.Handedness;
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                result.FingersFreedom[i] = dominantPose.FingersFreedom[i];
            }

        }
    }
}
