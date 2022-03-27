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

namespace Oculus.Interaction
{
    /// <summary>
    /// InteractorDriverGroup coordinates between a set of InteractorDrivers to
    /// determine which InteractorDriver(s) should be enabled at a time.
    ///
    /// By default, InteractorDrivers are prioritized in list order (first = highest priority).
    /// InteractorDrivers can also be prioritized with an optional IInteractorComparer
    /// </summary>
    public class InteractorDriverGroup : MonoBehaviour, IInteractorDriver
    {
        [SerializeField, Interface(typeof(IInteractorDriver))]
        private List<MonoBehaviour> _interactorDrivers;

        protected List<IInteractorDriver> InteractorDrivers;

        /// <summary>
        /// InteractorGroupStrategy defines how an InteractorDriverGroup coordinates hovering
        /// between InteractorDrivers when no InteractorDriver is yet selected.
        /// Upon selection, all other drivers are disabled.
        /// </summary>
        public enum InteractorDriverGroupStrategy
        {
            PRIORITY = 0, ///< Until selecting, reevaluate drivers in priority order
            FIRST = 1, ///< Until hovering, reevaluate drivers in priority order
            MULTIPLE = 2 ///< Until selecting, allow multiple hovered drivers
        }

        [SerializeField]
        private InteractorDriverGroupStrategy _interactorDriverGroupStrategy =
            InteractorDriverGroupStrategy.PRIORITY;

        public bool IsRootInteractorDriver { get; set; } = true;

        private IInteractorDriver _candidateDriver = null;
        private IInteractorDriver _currentDriver = null;
        private IInteractorDriver _selectingDriver = null;

        [SerializeField]
        private bool _selectionCanBeEmpty = true;

        [SerializeField, Interface(typeof(IInteractorComparer)), Optional]
        private MonoBehaviour _interactorComparer;

        protected IInteractorComparer InteractorComparer = null;

        protected virtual void Awake()
        {
            InteractorDrivers = _interactorDrivers.ConvertAll(mono => mono as IInteractorDriver);
            InteractorComparer = _interactorComparer as IInteractorComparer;
        }

        protected virtual void Start()
        {
            foreach (IInteractorDriver interactorDriver in InteractorDrivers)
            {
                Assert.IsNotNull(interactorDriver);
            }

            foreach (IInteractorDriver interactorDriver in InteractorDrivers)
            {
                interactorDriver.IsRootInteractorDriver = false;
            }

            if (_interactorComparer != null)
            {
                Assert.IsNotNull(InteractorComparer);
            }
        }

        protected virtual void Update()
        {
            if (!IsRootInteractorDriver) return;
            UpdateInteraction();
            UpdateHover();
            UpdateSelection(_selectionCanBeEmpty);
        }

        public void UpdateInteraction()
        {
            if (_selectingDriver != null)
            {
                _selectingDriver.UpdateInteraction();
                return;
            }

            _candidateDriver = null;

            foreach(IInteractorDriver driver in InteractorDrivers)
            {
                driver.Enable();
                driver.UpdateInteraction();

                if (driver.HasCandidate)
                {
                    if (_candidateDriver == null)
                    {
                        _candidateDriver = driver;
                    }
                    else if(Compare(_candidateDriver, driver) > 0)
                    {
                        _candidateDriver = driver;
                    }
                }
            }

            switch (_interactorDriverGroupStrategy)
            {
                case InteractorDriverGroupStrategy.PRIORITY:
                    PriorityHoverStrategy();
                    break;

                case InteractorDriverGroupStrategy.FIRST:
                    FirstHoverStrategy();
                    break;

                case InteractorDriverGroupStrategy.MULTIPLE:
                    MultiHoverStrategy();
                    break;
            }
        }

        public void UpdateHover()
        {
            if (_selectingDriver != null || _currentDriver == null)
            {
                return;
            }

            _currentDriver.UpdateHover();
            switch (_interactorDriverGroupStrategy)
            {
                case InteractorDriverGroupStrategy.PRIORITY:
                case InteractorDriverGroupStrategy.FIRST:
                    if (_currentDriver != null)
                    {
                        _currentDriver.UpdateHover();
                    }
                    break;
                case InteractorDriverGroupStrategy.MULTIPLE:
                    foreach(IInteractorDriver driver in InteractorDrivers)
                    {
                        if (!driver.HasCandidate)
                        {
                            continue;
                        }
                        driver.UpdateHover();
                    }
                    break;
            }
        }

        public void UpdateSelection(bool selectionCanBeEmpty)
        {
            if (_selectingDriver != null)
            {
                _selectingDriver.UpdateSelection(selectionCanBeEmpty);
                if (_selectingDriver.IsSelecting)
                {
                    return;
                }

                _selectingDriver = null;
                UpdateInteraction();
                UpdateHover();
            }

            switch (_interactorDriverGroupStrategy)
            {
                case InteractorDriverGroupStrategy.PRIORITY:
                case InteractorDriverGroupStrategy.FIRST:
                    if (_currentDriver != null)
                    {
                        _currentDriver.UpdateSelection(selectionCanBeEmpty);
                        if (_currentDriver.IsSelecting)
                        {
                            SelectDriver(_currentDriver);
                        }
                    }

                    break;
                case InteractorDriverGroupStrategy.MULTIPLE:
                    foreach(IInteractorDriver driver in InteractorDrivers)
                    {
                        driver.UpdateSelection(selectionCanBeEmpty);
                        if (driver.IsSelecting)
                        {
                            _currentDriver = driver;
                            SelectDriver(_currentDriver);
                            return;
                        }
                    }

                    break;
            }
        }

        private void PriorityHoverStrategy()
        {
            _currentDriver = _candidateDriver != null
                ? _candidateDriver
                : InteractorDrivers[InteractorDrivers.Count - 1];
            DisableAllDriversExcept(_currentDriver);
        }

        private void FirstHoverStrategy()
        {
            if (_currentDriver != null)
            {
                _currentDriver.UpdateInteraction();
                if (_currentDriver.HasCandidate)
                {
                    return;
                }
                _currentDriver = null;
            }

            if (_candidateDriver != null)
            {
                _currentDriver = _candidateDriver;
                DisableAllDriversExcept(_currentDriver);
            }
        }

        private void MultiHoverStrategy()
        {
            _currentDriver = null;
            foreach (IInteractorDriver driver in InteractorDrivers)
            {
                // If we are in a hover state, we want to make the hover interactor visible
                if (driver.HasCandidate)
                {
                    if (_currentDriver == null)
                    {
                        _currentDriver = driver;
                    }
                }
                else
                {
                    driver.Disable();
                }
            }
        }

        private void SelectDriver(IInteractorDriver selectedDriver)
        {
            // Mark this as the selected driver
            _currentDriver = selectedDriver;
            _selectingDriver = selectedDriver;

            DisableAllDriversExcept(_selectingDriver);
        }

        private void DisableAllDriversExcept(IInteractorDriver enabledDriver)
        {
            foreach (IInteractorDriver driver in InteractorDrivers)
            {
                if (driver == enabledDriver) continue;
                driver.Disable();
            }
        }

        public bool IsSelectingInteractable => _selectingDriver != null && _selectingDriver.IsSelectingInteractable;
        public bool IsSelecting => _selectingDriver != null;
        public bool HasCandidate => _currentDriver != null && _currentDriver.HasCandidate;

        public IInteractor CandidateInteractor => HasCandidate ? _currentDriver.CandidateInteractor : null;

        public bool ShouldSelect =>
            _currentDriver != null && _currentDriver.ShouldSelect;
        public bool IsHovering => _currentDriver != null && _currentDriver.IsHovering;

        public void Enable()
        {
            foreach (IInteractorDriver interactorDriver in InteractorDrivers)
            {
                interactorDriver.Enable();
            }
        }

        public void Disable()
        {
            foreach (IInteractorDriver interactorDriver in InteractorDrivers)
            {
                interactorDriver.Disable();
            }
        }

        public virtual void AddInteractorDriver(IInteractorDriver interactorDriver)
        {
            InteractorDrivers.Add(interactorDriver);
            _interactorDrivers.Add(interactorDriver as MonoBehaviour);
            interactorDriver.IsRootInteractorDriver = false;
        }

        public virtual void RemoveInteractorDriver(IInteractorDriver interactorDriver)
        {
            InteractorDrivers.Remove(interactorDriver);
            _interactorDrivers.Remove(interactorDriver as MonoBehaviour);
            interactorDriver.IsRootInteractorDriver = true;
        }

        private int Compare(IInteractorDriver a, IInteractorDriver b)
        {
            if (!a.HasCandidate && !b.HasCandidate)
            {
                return -1;
            }

            if (a.HasCandidate && b.HasCandidate)
            {
                if (InteractorComparer == null)
                {
                    return -1;
                }

                int result = InteractorComparer.Compare(a.CandidateInteractor, b.CandidateInteractor);
                return result > 0 ? 1 : -1;
            }

            return a.HasCandidate ? -1 : 1;
        }

        #region Inject

        public void InjectAllInteractorDriverGroup(List<IInteractorDriver> interactorDrivers)
        {
            InjectInteractorDrivers(interactorDrivers);
        }

        public void InjectInteractorDrivers(List<IInteractorDriver> interactorDrivers)
        {
            InteractorDrivers = interactorDrivers;
            _interactorDrivers = interactorDrivers.ConvertAll(interactorDriver =>
                                                                interactorDriver as MonoBehaviour);
        }

        public void InjectOptionalInteractorDriverGroupStrategy(
            InteractorDriverGroupStrategy interactorDriverGroupStrategy)
        {
            _interactorDriverGroupStrategy = interactorDriverGroupStrategy;
        }

        public void InjectOptionalInteractorComparer(IInteractorComparer comparer)
        {
            InteractorComparer = comparer;
            _interactorComparer = comparer as MonoBehaviour;
        }

        #endregion
    }
}
