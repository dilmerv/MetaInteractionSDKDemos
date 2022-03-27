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
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction
{
    public class RayInteractable : Interactable<RayInteractor, RayInteractable>, IPointable
    {
        [SerializeField]
        private Collider _collider;
        public Collider Collider { get => _collider; }

        [SerializeField, Optional, Interface(typeof(IPointableSurface))]
        private MonoBehaviour _surface = null;

        private IPointableSurface Surface;

        public event Action<PointerArgs> OnPointerEvent = delegate { };
        private PointableDelegate<RayInteractor> _pointableDelegate;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Surface = _surface as IPointableSurface;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(_collider);
            _pointableDelegate = new PointableDelegate<RayInteractor>(this, ComputePointer);
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                _pointableDelegate.OnPointerEvent += InvokePointerEvent;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                _pointableDelegate.OnPointerEvent -= InvokePointerEvent;
            }
            base.OnDisable();
        }

        private void InvokePointerEvent(PointerArgs args)
        {
            OnPointerEvent(args);
        }

        public bool Raycast(Ray ray, out SurfaceHit hit, in float maxDistance, in bool useSurface)
        {
            hit = new SurfaceHit();
            if (Collider.Raycast(ray, out RaycastHit raycastHit, maxDistance))
            {
                hit.Point = raycastHit.point;
                hit.Normal = raycastHit.normal;
                hit.Distance = raycastHit.distance;
                return true;
            }
            else if (useSurface && Surface != null)
            {
                return Surface.Raycast(ray, out hit, maxDistance);
            }
            return false;
        }

        private void ComputePointer(RayInteractor rayInteractor, out Vector3 position, out Quaternion rotation)
        {
            if (rayInteractor.CollisionInfo != null)
            {
                position = rayInteractor.CollisionInfo.Value.Point;
                rotation = Quaternion.LookRotation(rayInteractor.CollisionInfo.Value.Normal);
                return;
            }
            else
            {
                position = Vector3.zero;
                rotation = rayInteractor.Rotation;
            }
        }

        protected virtual void OnDestroy()
        {
            _pointableDelegate = null;
        }

        #region Inject

        public void InjectAllRayInteractable(Collider collider)
        {
            InjectCollider(collider);
        }

        public void InjectCollider(Collider collider)
        {
            _collider = collider;
        }

        public void InjectOptionalSurface(IPointableSurface surface)
        {
            Surface = surface;
            _surface = surface as MonoBehaviour;
        }

        #endregion
    }
}
