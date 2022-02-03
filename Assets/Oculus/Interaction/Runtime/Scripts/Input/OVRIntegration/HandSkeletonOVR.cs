/************************************************************************************
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

namespace Oculus.Interaction.Input
{
    public class HandSkeletonOVR : MonoBehaviour, IHandSkeletonProvider
    {
        private readonly HandSkeleton[] _skeletons = { null, null };

        public HandSkeleton this[Handedness handedness] => _skeletons[(int)handedness];

        protected void Awake()
        {
            _skeletons[0] = CreateSkeletonData(Handedness.Left);
            _skeletons[1] = CreateSkeletonData(Handedness.Right);
        }

        public static HandSkeleton CreateSkeletonData(Handedness handedness)
        {
            HandSkeleton handSkeleton = new HandSkeleton();

            // When running in the editor, the call to load the skeleton from OVRPlugin may fail. Use baked skeleton
            // data.
            if (handedness == Handedness.Left)
            {
                ApplyToSkeleton(OVRSkeletonData.LeftSkeleton, handSkeleton);
            }
            else
            {
                ApplyToSkeleton(OVRSkeletonData.RightSkeleton, handSkeleton);
            }

            return handSkeleton;
        }

        private static void ApplyToSkeleton(in OVRPlugin.Skeleton2 ovrSkeleton, HandSkeleton handSkeleton)
        {
            int numJoints = handSkeleton.joints.Length;
            Assert.AreEqual(ovrSkeleton.NumBones, numJoints);

            for (int i = 0; i < numJoints; ++i)
            {
                ref var srcPose = ref ovrSkeleton.Bones[i].Pose;
                handSkeleton.joints[i] = new HandSkeletonJoint()
                {
                    pose = new Pose()
                    {
                        position = srcPose.Position.FromFlippedXVector3f(),
                        rotation = srcPose.Orientation.FromFlippedXQuatf()
                    },
                    parent = ovrSkeleton.Bones[i].ParentBoneIndex
                };
            }
        }
    }
}
