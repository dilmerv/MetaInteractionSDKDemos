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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public enum GrabbableEvent
    {
        Add,
        Update,
        Remove
    }

    public struct GrabbableArgs
    {
        public int GrabIdentifier { get; }
        public GrabbableEvent GrabbableEvent { get; }
        public GrabbableArgs(int grabIdentifier, GrabbableEvent grabbableEvent)
        {
            this.GrabIdentifier = grabIdentifier;
            this.GrabbableEvent = grabbableEvent;
        }
    }

    public class Grabbable : MonoBehaviour, IGrabbable
    {

        [SerializeField, Interface(typeof(ITransformer)), Optional]
        private MonoBehaviour _oneGrabTransformer = null;

        [SerializeField, Interface(typeof(ITransformer)), Optional]
        private MonoBehaviour _twoGrabTransformer = null;

        [SerializeField]
        private bool _transferHandOnSecondGrab;

        [SerializeField]
        private bool _addNewGrabsToFront = false;

        [SerializeField]
        private int _maxGrabPoints = -1;

        #region Properties
        public bool TransferHandOnSecondGrab
        {
            get
            {
                return _transferHandOnSecondGrab;
            }
            set
            {
                _transferHandOnSecondGrab = value;
            }
        }

        public bool AddNewGrabsToFront
        {
            get
            {
                return _addNewGrabsToFront;
            }
            set
            {
                _addNewGrabsToFront = value;
            }
        }

        public int MaxGrabPoints
        {
            get
            {
                return _maxGrabPoints;
            }
            set
            {
                _maxGrabPoints = value;
            }
        }
        #endregion

        public event Action<GrabbableArgs> WhenGrabbableUpdated = delegate { };

        public List<Pose> GrabPoints => _grabPoints;
        public int GrabPointsCount => _grabPoints.Count;
        public Transform Transform => transform;

        protected List<Pose> _grabPoints;
        protected List<int> _grabPointIds;

        private ITransformer _activeTransformer = null;
        private ITransformer OneGrabTransformer;
        private ITransformer TwoGrabTransformer;

        protected bool _started = false;

        protected virtual void Awake()
        {
            OneGrabTransformer = _oneGrabTransformer as ITransformer;
            TwoGrabTransformer = _twoGrabTransformer as ITransformer;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            _grabPoints = new List<Pose>();
            _grabPointIds = new List<int>();

            if (OneGrabTransformer != null)
            {
                Assert.IsNotNull(OneGrabTransformer);
                OneGrabTransformer.Initialize(this);
            }

            if (TwoGrabTransformer != null)
            {
                Assert.IsNotNull(TwoGrabTransformer);
                TwoGrabTransformer.Initialize(this);
            }

            // Create a default if no transformers assigned
            if (OneGrabTransformer == null &&
                TwoGrabTransformer == null)
            {
                OneGrabFreeTransformer defaultTransformer = gameObject.AddComponent<OneGrabFreeTransformer>();
                _oneGrabTransformer = defaultTransformer;
                OneGrabTransformer = defaultTransformer;
                OneGrabTransformer.Initialize(this);
            }

            this.EndStart(ref _started);
        }

        public void AddGrabPoint(int id, Pose pose)
        {
            // If the transfer hand on second grab flag is on, we ignore any subsequent events
            if (_grabPoints.Count == 1 && _transferHandOnSecondGrab)
            {
                RemoveGrabPoint(_grabPointIds[0], _grabPoints[0]);
            }

            Pose grabPoint = pose;

            if (_addNewGrabsToFront)
            {
                _grabPointIds.Insert(0, id);
                _grabPoints.Insert(0, grabPoint);
            }
            else
            {
                _grabPointIds.Add(id);
                _grabPoints.Add(grabPoint);
            }

            WhenGrabbableUpdated(new GrabbableArgs(id, GrabbableEvent.Add));

            BeginTransform();
        }

        public void UpdateGrabPoint(int id, Pose pose)
        {
            int index = _grabPointIds.IndexOf(id);
            if (index == -1)
            {
                return;
            }

            _grabPoints[index] = pose;
            UpdateTransform();

            WhenGrabbableUpdated(new GrabbableArgs(id, GrabbableEvent.Update));
        }

        public void RemoveGrabPoint(int id, Pose pose)
        {
            int index = _grabPointIds.IndexOf(id);
            if (index == -1)
            {
                return;
            }

            _grabPoints[index] = pose;
            EndTransform();

            _grabPointIds.RemoveAt(index);
            _grabPoints.RemoveAt(index);

            WhenGrabbableUpdated(new GrabbableArgs(id, GrabbableEvent.Remove));

            BeginTransform();
        }

        public void ResetGrabPoint(int id, Pose grabSourcePose)
        {
            int index = _grabPointIds.IndexOf(id);
            if (index == -1)
            {
                return;
            }

            EndTransform();

            _grabPoints[index] = grabSourcePose;

            BeginTransform();
        }

        // Whenever we change the number of grab points, we save the
        // current transform data
        private void BeginTransform()
        {
            // End the transform on any existing transformer before we
            // begin the new one
            EndTransform();

            int useGrabPoints = _grabPoints.Count;
            if (_maxGrabPoints != -1)
            {
                useGrabPoints = Mathf.Min(useGrabPoints, _maxGrabPoints);
            }

            switch (useGrabPoints)
            {
                case 1:
                    _activeTransformer = OneGrabTransformer;
                    break;
                case 2:
                    _activeTransformer = TwoGrabTransformer;
                    break;
                default:
                    _activeTransformer = null;
                    break;
            }

            if (_activeTransformer == null)
            {
                return;
            }

            _activeTransformer.BeginTransform();
        }

        private void UpdateTransform()
        {
            if (_activeTransformer == null)
            {
                return;
            }

            _activeTransformer.UpdateTransform();
        }

        private void EndTransform()
        {
            if (_activeTransformer == null)
            {
                return;
            }
            _activeTransformer.EndTransform();
            _activeTransformer = null;
        }

        #region Inject

        public void InjectOptionalOneGrabTransformer(ITransformer transformer)
        {
            _oneGrabTransformer = transformer as MonoBehaviour;
            OneGrabTransformer = transformer;
        }

        public void InjectOptionalTwoGrabTransformer(ITransformer transformer)
        {
            _twoGrabTransformer = transformer as MonoBehaviour;
            TwoGrabTransformer = transformer;
        }

        #endregion
    }
}
