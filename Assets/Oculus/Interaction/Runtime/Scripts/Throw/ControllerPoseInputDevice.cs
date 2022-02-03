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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Throw
{
    /// <summary>
    /// Provides pose information for a controller.
    /// </summary>
    public class ControllerPoseInputDevice : MonoBehaviour, IPoseInputDevice
    {
        [SerializeField, Interface(typeof(IController))]
        private MonoBehaviour _controller;
        public IController Controller { get; private set; }
        [SerializeField]
        private Transform _trackingSpaceTransform;

        public bool IsInputValid =>
            Controller.IsConnected &&
            Controller.IsPoseValid;

        public bool IsHighConfidence => IsInputValid;

        public bool GetRootPose(out Pose pose)
        {
            pose = new Pose();
            if (!IsInputValid)
            {
                return false;
            }

            if (!Controller.TryGetPose(out pose))
            {
                return false;
            }

            return true;
        }

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_controller);
            Assert.IsNotNull(_trackingSpaceTransform);
        }

        public (Vector3, Vector3) GetExternalVelocities()
        {
            return (Vector3.zero, Vector3.zero);
        }

        #region Inject

        public void InjectAllControllerPoseInputDevice(
            IController controller,
            Transform trackingSpaceTransform)
        {
            InjectController(controller);
            InjectTrackingSpaceTransform(trackingSpaceTransform);
        }

        public void InjectController(IController controller)
        {
            _controller = controller as MonoBehaviour;
            Controller = controller;
        }

        public void InjectTrackingSpaceTransform(Transform trackingSpaceTransform)
        {
            _trackingSpaceTransform = trackingSpaceTransform;
        }

        #endregion
    }
}
