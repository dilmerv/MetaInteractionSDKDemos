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

namespace Oculus.Interaction.HandPosing
{
    /// <summary>
    /// Utility struct containing for measuring perceived distances
    /// when rotations are also involved
    public struct PoseTravelData
    {
        private const float DEGREES_TO_PERCEIVED_METERS = 0.5f / 360f;

        public static float PerceivedDistance(in Pose from, in Pose to)
        {
            Pose grabOffset = PoseUtils.RelativeOffset(from, to);
            float translationDistance = grabOffset.position.magnitude;

            float rotationDistance = DEGREES_TO_PERCEIVED_METERS * Mathf.Max(
                Mathf.Max(Vector3.Angle(from.forward, to.forward),
                Vector3.Angle(from.up, to.up),
                Vector3.Angle(from.right, to.right)));

            float travelDistance = Mathf.Max(translationDistance, rotationDistance);

            return travelDistance;
        }
    }
}
