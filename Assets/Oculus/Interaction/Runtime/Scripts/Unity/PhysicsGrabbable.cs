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
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class PhysicsGrabbable : MonoBehaviour
    {
        [SerializeField]
        private Grabbable _grabbable;

        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        [Tooltip("If enabled, the object's mass will scale appropriately as the scale of the object changes.")]
        private bool _scaleMassWithSize = true;

        private bool _savedIsKinematicState = false;
        private bool _isBeingTransformed = false;
        private Vector3 _initialScale;
        private bool _hasPendingForce;
        private Vector3 _linearVelocity;
        private Vector3 _angularVelocity;

        protected bool _started = false;

        public event Action<Vector3, Vector3> WhenVelocitiesApplied = delegate { };

        private void Reset()
        {
            _grabbable = this.GetComponent<Grabbable>();
            _rigidbody = this.GetComponent<Rigidbody>();
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(_grabbable);
            Assert.IsNotNull(_rigidbody);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _grabbable.WhenGrabbableUpdated += HandleGrabbableUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _grabbable.WhenGrabbableUpdated -= HandleGrabbableUpdated;
            }
        }

        private void HandleGrabbableUpdated(GrabbableArgs args)
        {
            switch (args.GrabbableEvent)
            {
                case GrabbableEvent.Add:
                    if (_grabbable.GrabPointsCount == 1 && !_isBeingTransformed)
                    {
                        DisablePhysics();
                    }

                    break;
                case GrabbableEvent.Remove:
                    if (_grabbable.GrabPointsCount == 0)
                    {
                        ReenablePhysics();
                    }

                    break;
            }
        }

        private void DisablePhysics()
        {
            _isBeingTransformed = true;
            CachePhysicsState();
            _rigidbody.isKinematic = true;
        }

        private void ReenablePhysics()
        {
            _isBeingTransformed = false;
            // update the mass based on the scale change
            if (_scaleMassWithSize)
            {
                float initialScaledVolume = _initialScale.x * _initialScale.y * _initialScale.z;

                Vector3 currentScale = _rigidbody.transform.localScale;
                float currentScaledVolume = currentScale.x * currentScale.y * currentScale.z;

                float changeInMassFactor = currentScaledVolume / initialScaledVolume;
                _rigidbody.mass *= changeInMassFactor;
            }

            // revert the original kinematic state
            _rigidbody.isKinematic = _savedIsKinematicState;
        }

        public void ApplyVelocities(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            _hasPendingForce = true;
            _linearVelocity = linearVelocity;
            _angularVelocity = angularVelocity;
        }

        private void FixedUpdate()
        {
            if (_hasPendingForce)
            {
                _hasPendingForce = false;
                _rigidbody.AddForce(_linearVelocity, ForceMode.VelocityChange);
                _rigidbody.AddTorque(_angularVelocity, ForceMode.VelocityChange);
                WhenVelocitiesApplied(_linearVelocity, _angularVelocity);
            }
        }

        private void CachePhysicsState()
        {
            _savedIsKinematicState = _rigidbody.isKinematic;
            _initialScale = _rigidbody.transform.localScale;
        }

        #region Inject

        public void InjectAllPhysicsGrabbable(Grabbable grabbable, Rigidbody rigidbody)
        {
            InjectGrabbable(grabbable);
            InjectRigidbody(rigidbody);
        }

        public void InjectGrabbable(Grabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectOptionalScaleMassWithSize(bool scaleMassWithSize)
        {
            _scaleMassWithSize = scaleMassWithSize;
        }

        #endregion
    }
}
