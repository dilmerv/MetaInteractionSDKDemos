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
using Oculus.Interaction.Input;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// HandPokeInteractorVisual forwards the finger state of an associated
    /// HandPokeInteractor to a HandGrabModifier to lock and unlock
    /// finger joints in the modifier's target hand data.
    /// </summary>
    public class HandPokeLimiterVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        private IHand Hand;

        [FormerlySerializedAs("_interactor")]
        [SerializeField]
        private PokeInteractor _pokeInteractor;

        [FormerlySerializedAs("_modifier")]
        [SerializeField]
        private SyntheticHandModifier _syntheticHand;

        [SerializeField]
        private float _maxDistanceFromTouchPoint = 0.1f;

        private bool _isTouching;
        private Vector3 _initialTouchPoint;
        private float _maxDeltaFromTouchPoint;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(_pokeInteractor);
            Assert.IsNotNull(_syntheticHand);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _pokeInteractor.WhenInteractableSelected.Action += HandleLock;
                _pokeInteractor.WhenInteractableUnselected.Action += HandleUnlock;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (_isTouching)
                {
                    HandleUnlock(_pokeInteractor.SelectedInteractable);
                }

                _pokeInteractor.WhenInteractableSelected.Action -= HandleLock;
                _pokeInteractor.WhenInteractableUnselected.Action -= HandleUnlock;
            }
        }

        protected virtual void LateUpdate()
        {
            UpdateWrist();
        }

        private void HandleLock(PokeInteractable pokeInteractable)
        {
            _isTouching = true;
            _initialTouchPoint = _pokeInteractor.TouchPoint;
        }

        private void HandleUnlock(PokeInteractable pokeInteractable)
        {
            _syntheticHand.FreeWrist();
            _isTouching = false;
        }

        private Vector3 ComputePlanePosition(Vector3 point, PokeInteractable interactable)
        {
            Vector3 planeToPoint = point - interactable.TriggerPlaneTransform.position;
            Vector3 projectOnNormal = Vector3.Project(planeToPoint, -1f * interactable.TriggerPlaneTransform.forward);
            return point - projectOnNormal;
        }

        private void UpdateWrist()
        {
            if (!_isTouching) return;

            if (!Hand.GetRootPose(out Pose rootPose))
            {
                return;
            }

            Vector3 planePosition = ComputePlanePosition(_pokeInteractor.Origin, _pokeInteractor.SelectedInteractable);
            _maxDeltaFromTouchPoint = Mathf.Max((planePosition - _initialTouchPoint).magnitude, _maxDeltaFromTouchPoint);

            float deltaAsPercent =
                Mathf.Clamp01(_maxDeltaFromTouchPoint / _maxDistanceFromTouchPoint);

            Vector3 fullDelta = planePosition - _initialTouchPoint;
            Vector3 easedPosition = _initialTouchPoint + fullDelta * deltaAsPercent;

            Vector3 positionDelta = rootPose.position - _pokeInteractor.Origin;
            Vector3 targetPosePosition = easedPosition + positionDelta;
            Pose wristPoseOverride = new Pose(targetPosePosition, rootPose.rotation);

            _syntheticHand.LockWristPose(wristPoseOverride, 1.0f, SyntheticHandModifier.WristLockMode.Full, true, true);
            _syntheticHand.MarkInputDataRequiresUpdate();
        }

        #region Inject

        public void InjectAllHandPokeLimiterVisual(IHand hand, PokeInteractor pokeInteractor,
            SyntheticHandModifier syntheticHand)
        {
            InjectHand(hand);
            InjectPokeInteractor(pokeInteractor);
            InjectSyntheticHand(syntheticHand);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectPokeInteractor(PokeInteractor pokeInteractor)
        {
            _pokeInteractor = pokeInteractor;
        }

        public void InjectSyntheticHand(SyntheticHandModifier syntheticHand)
        {
            _syntheticHand = syntheticHand;
        }

        #endregion
    }
}
