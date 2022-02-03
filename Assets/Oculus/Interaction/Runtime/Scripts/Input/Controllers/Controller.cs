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

namespace Oculus.Interaction.Input
{
    public class Controller :
        DataModifier<ControllerDataAsset, ControllerDataSourceConfig>,
        IController
    {
        public Handedness Handedness => Config.Handedness;

        public bool IsConnected
        {
            get
            {
                var currentData = GetData();
                return currentData.IsDataValid && currentData.IsConnected;
            }
        }

        public bool IsPoseValid
        {
            get
            {
                var currentData = GetData();
                return currentData.IsDataValid &&
                       currentData.RootPoseOrigin != PoseOrigin.None;
            }
        }

        public bool IsPointerPoseValid
        {
            get
            {
                var currentData = GetData();
                return currentData.IsDataValid &&
                       currentData.PointerPoseOrigin != PoseOrigin.None;
            }
        }

        public event Action ControllerUpdated = delegate { };

        public bool IsButtonUsageAnyActive(ControllerButtonUsage buttonUsage)
        {
            var currentData = GetData();
            return
                currentData.IsDataValid &&
                (buttonUsage & currentData.ButtonUsageMask) != 0;
        }

        public bool IsButtonUsageAllActive(ControllerButtonUsage buttonUsage)
        {
            var currentData = GetData();
            return currentData.IsDataValid &&
                   (buttonUsage & currentData.ButtonUsageMask) == buttonUsage;
        }

        /// <summary>
        /// Retrieves the current controller pose, in world space.
        /// </summary>
        /// <param name="pose">Set to current pose if `IsPoseValid`; Pose.identity otherwise</param>
        /// <returns>Value of `IsPoseValid`</returns>
        public bool TryGetPose(out Pose pose)
        {
            if (!IsPoseValid)
            {
                pose = Pose.identity;
                return false;
            }

            pose = Config.TrackingToWorldTransformer.ToWorldPose(GetData().RootPose);
            return true;
        }

        /// <summary>
        /// Retrieves the current controller pointer pose, in world space.
        /// </summary>
        /// <param name="pose">Set to current pose if `IsPoseValid`; Pose.identity otherwise</param>
        /// <returns>Value of `IsPoseValid`</returns>
        public bool TryGetPointerPose(out Pose pose)
        {
            if (!IsPointerPoseValid)
            {
                pose = Pose.identity;
                return false;
            }

            pose = Config.TrackingToWorldTransformer.ToWorldPose(GetData().PointerPose);
            return true;
        }

        public override void MarkInputDataRequiresUpdate()
        {
            base.MarkInputDataRequiresUpdate();

            if (Started)
            {
                ControllerUpdated();
            }
        }

        protected override void Apply(ControllerDataAsset data)
        {
            // Default implementation does nothing, to allow instantiation of this modifier directly
        }
    }
}
