/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction
{
    public class RayInteractor : Interactor<RayInteractor, RayInteractable>,
        ICandidatePosition
    {
        [SerializeField, Interface(typeof(ISelector))]
        private MonoBehaviour _selector;

        [SerializeField]
        private Transform _rayOrigin;

        [SerializeField]
        private float _maxRayLength = 5f;

        public Vector3 Origin { get; protected set; }
        public Quaternion Rotation { get; protected set; }
        public Vector3 Forward { get; protected set; }
        public Vector3 End { get; set; }

        public float MaxRayLength
        {
            get
            {
                return _maxRayLength;
            }
            set
            {
                _maxRayLength = value;
            }
        }

        public SurfaceHit? CollisionInfo { get; protected set; }
        public Ray Ray { get; protected set; }

        private Vector3 _candidatePosition;
        public Vector3 CandidatePosition => _candidatePosition;

        protected override void Awake()
        {
            base.Awake();
            Selector = _selector as ISelector;
        }

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(Selector);
            Assert.IsNotNull(_rayOrigin);
        }

        protected override void DoEveryUpdate()
        {
            Origin = _rayOrigin.transform.position;
            Rotation = _rayOrigin.transform.rotation;
            Forward = Rotation * Vector3.forward;
            Ray = new Ray(Origin, Forward);
        }

        protected override RayInteractable ComputeCandidate()
        {
            CollisionInfo = null;

            RayInteractable closestInteractable = null;
            float closestDist = float.MaxValue;
            IEnumerable<RayInteractable> interactables = RayInteractable.Registry.List(this);

            foreach (RayInteractable interactable in interactables)
            {
                if (interactable.Raycast(Ray, out SurfaceHit hit, MaxRayLength, false))
                {
                    if (hit.Distance < closestDist)
                    {
                        closestDist = hit.Distance;
                        closestInteractable = interactable;
                        CollisionInfo = hit;
                        _candidatePosition = hit.Point;
                    }
                }
            }

            float rayDist = (closestInteractable != null ? closestDist : MaxRayLength);
            End = Origin + rayDist * Forward;

            return closestInteractable;
        }

        protected override void DoSelectUpdate(RayInteractable interactable)
        {
            CollisionInfo = null;

            if (interactable != null &&
                interactable.Raycast(Ray, out SurfaceHit hit, MaxRayLength, true))
            {
                End = hit.Point;
                CollisionInfo = hit;
            }
            else
            {
                End = Origin + MaxRayLength * Forward;
            }
        }

        #region Inject
        public void InjectAllRayInteractor(ISelector selector, Transform rayOrigin)
        {
            InjectSelector(selector);
            InjectRayOrigin(rayOrigin);
        }

        public void InjectSelector(ISelector selector)
        {
            _selector = selector as MonoBehaviour;
            Selector = selector;
        }

        public void InjectRayOrigin(Transform rayOrigin)
        {
            _rayOrigin = rayOrigin;
        }
        #endregion
    }
}
