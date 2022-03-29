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

namespace Oculus.Interaction
{
    public class GrabInteractable : Interactable<GrabInteractor, GrabInteractable>,
                                      IRigidbodyRef
    {
        private Collider[] _colliders;
        public Collider[] Colliders => _colliders;

        [SerializeField]
        Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        [SerializeField]
        private Grabbable _grabbable;
        public Grabbable Grabbable => _grabbable;

        [SerializeField, Optional]
        private Transform _grabSource;

        [SerializeField]
        private bool _useClosestPointAsGrabSource;

        [SerializeField]
        private float _releaseDistance = 0f;

        [SerializeField]
        private bool _resetGrabOnGrabsUpdated = true;

        [SerializeField, Optional]
        private PhysicsGrabbable _physicsGrabbable = null;

        private static CollisionInteractionRegistry<GrabInteractor, GrabInteractable> _grabRegistry = null;

        protected bool _started = false;

        #region Properties
        public bool UseClosestPointAsGrabSource
        {
            get
            {
                return _useClosestPointAsGrabSource;
            }
            set
            {
                _useClosestPointAsGrabSource = value;
            }
        }
        public float ReleaseDistance
        {
            get
            {
                return _releaseDistance;
            }
            set
            {
                _releaseDistance = value;
            }
        }

        public bool ResetGrabOnGrabsUpdated
        {
            get
            {
                return _resetGrabOnGrabsUpdated;
            }
            set
            {
                _resetGrabOnGrabsUpdated = value;
            }
        }
        #endregion

        protected virtual void Awake()
        {
            if (_grabRegistry == null)
            {
                _grabRegistry = new CollisionInteractionRegistry<GrabInteractor, GrabInteractable>();
                SetRegistry(_grabRegistry);
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            Assert.IsNotNull(Rigidbody);
            _colliders = Rigidbody.GetComponentsInChildren<Collider>();
            Assert.IsTrue(Colliders.Length > 0,
            "The associated Rigidbody must have at least one Collider.");
            Assert.IsNotNull(_grabbable);
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                Grabbable.WhenGrabbableUpdated += HandleGrabbableUpdated;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                Grabbable.WhenGrabbableUpdated -= HandleGrabbableUpdated;
            }
            base.OnDisable();
        }

        private void HandleGrabbableUpdated(GrabbableArgs args)
        {
            switch (args.GrabbableEvent)
            {
                case GrabbableEvent.Remove:
                    RemoveInteractorById(args.GrabIdentifier);
                    break;
            }
        }

        public Pose GetGrabSourceForTarget(Pose target)
        {
            if (_grabSource == null && !_useClosestPointAsGrabSource)
            {
                return target;
            }

            if (_useClosestPointAsGrabSource)
            {
                return new Pose(
                    Collisions.ClosestPointToColliders(target.position, _colliders),
                    target.rotation);
            }

            return _grabSource.GetPose();
        }

        public void ApplyVelocities(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            if (_physicsGrabbable == null)
            {
                return;
            }
            _physicsGrabbable.ApplyVelocities(linearVelocity, angularVelocity);
        }

        #region Inject

        public void InjectAllGrabInteractable(Rigidbody rigidbody, Grabbable grabbable)
        {
            InjectRigidbody(rigidbody);
            InjectGrabbable(grabbable);
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectGrabbable(Grabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public void InjectOptionalGrabSource(Transform grabSource)
        {
            _grabSource = grabSource;
        }

        public void InjectOptionalReleaseDistance(float releaseDistance)
        {
            _releaseDistance = releaseDistance;
        }

        public void InjectOptionalPhysicsGrabbable(PhysicsGrabbable physicsGrabbable)
        {
            _physicsGrabbable = physicsGrabbable;
        }

        #endregion
    }
}
