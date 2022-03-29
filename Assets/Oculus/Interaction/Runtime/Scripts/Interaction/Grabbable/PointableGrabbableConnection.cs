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
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class PointableGrabbableConnection : MonoBehaviour
    {
        [SerializeField]
        private Grabbable _grabbable;

        [SerializeField, Interface(typeof(IInteractable), typeof(IPointable))]
        private MonoBehaviour _pointableInteractable;

        private IPointable Pointable;
        private IInteractable Interactable;

        protected bool _started = false;

        #region Editor

        private void Reset()
        {
            _grabbable = this.GetComponentInParent<Grabbable>();
            _pointableInteractable = this.GetComponent(typeof(IPointable)) as MonoBehaviour;
        }

        #endregion

        protected virtual void Awake()
        {
            Pointable = _pointableInteractable as IPointable;
            Interactable = _pointableInteractable as IInteractable;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(_grabbable);
            Assert.IsNotNull(Pointable);
            Assert.IsNotNull(Interactable);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Pointable.OnPointerEvent += HandlePointableEvent;
                _grabbable.WhenGrabbableUpdated += HandleWhenGrabbableUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Pointable.OnPointerEvent -= HandlePointableEvent;
                _grabbable.WhenGrabbableUpdated -= HandleWhenGrabbableUpdated;
            }
        }

        private void HandlePointableEvent(PointerArgs args)
        {
            switch (args.PointerEvent)
            {
                case PointerEvent.Select:
                    _grabbable.AddGrabPoint(args.Identifier, new Pose(args.Position, args.Rotation));
                    break;
                case PointerEvent.Move:
                    _grabbable.UpdateGrabPoint(args.Identifier, new Pose(args.Position, args.Rotation));
                    break;
                case PointerEvent.Unselect:
                    _grabbable.RemoveGrabPoint(args.Identifier, new Pose(args.Position, args.Rotation));
                    break;
            }
        }

        private void HandleWhenGrabbableUpdated(GrabbableArgs args)
        {
            if (args.GrabbableEvent != GrabbableEvent.Remove)
            {
                return;
            }

            Interactable.RemoveInteractorById(args.GrabIdentifier);
        }

        #region Inject

        public void InjectAllPointableGrabbableConnection<T>(Grabbable grabbable,
                                                                    T pointableInteractable)
            where T : IInteractable, IPointable
        {
            InjectGrabbable(grabbable);
            InjectPointableInteractable(pointableInteractable);
        }

        public void InjectGrabbable(Grabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public void InjectPointableInteractable<T>(T pointableInteractable) where T:IInteractable, IPointable
        {
            _pointableInteractable = pointableInteractable as MonoBehaviour;
            Pointable = _pointableInteractable as IPointable;
            Interactable = _pointableInteractable as IInteractable;
        }

        #endregion
    }
}
