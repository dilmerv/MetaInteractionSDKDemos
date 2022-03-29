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
using UnityEngine.Events;

namespace Oculus.Interaction
{
    /// <summary>
    /// This component makes it possible to connect Grabbables in the
    /// inspector to Unity Events that are invoked on Grabbable Updates
    /// </summary>
    public class GrabbableUnityEventWrapper : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IGrabbable))]
        private MonoBehaviour _grabbable;

        private IGrabbable Grabbable { get; set; }

        [SerializeField]
        private UnityEvent _onAdd;
        [SerializeField]
        private UnityEvent _onRemove;
        [SerializeField]
        private UnityEvent _onTransfer;
        [SerializeField]
        private UnityEvent _onUpdate;

        public UnityEvent OnAdd => _onAdd;
        public UnityEvent OnRemove => _onRemove;
        public UnityEvent OnUpdate => _onUpdate;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Grabbable = _grabbable as IGrabbable;
        }
        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Grabbable);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Grabbable.WhenGrabbableUpdated += HandleWhenGrabbableUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Grabbable.WhenGrabbableUpdated -= HandleWhenGrabbableUpdated;
            }
        }

        private void HandleWhenGrabbableUpdated(GrabbableArgs args)
        {
            switch (args.GrabbableEvent)
            {
                case GrabbableEvent.Add:
                    _onAdd.Invoke();
                    break;
                case GrabbableEvent.Update:
                    break;
                case GrabbableEvent.Remove:
                    _onRemove.Invoke();
                    break;
                default:
                    break;
            }
        }

        #region Inject

        public void InjectAllGrabbableUnityEventWrapper(IGrabbable grabbable)
        {
            InjectGrabbable(grabbable);
        }

        public void InjectGrabbable(IGrabbable grabbable)
        {
            _grabbable = grabbable as MonoBehaviour;
            Grabbable = grabbable;
        }

        #endregion
    }
}
