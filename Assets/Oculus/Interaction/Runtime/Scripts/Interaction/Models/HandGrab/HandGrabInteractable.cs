/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.HandPosing
{
    /// <summary>
    /// Serializable data-only version of the HandGrabInteractable so it can be stored when they
    /// are generated at Play-Mode (where Hand-tracking works).
    /// </summary>
    [Serializable]
    public struct HandGrabInteractableData
    {
        public List<HandGrabPointData> points;
        public SnapType snapType;
        public GrabTypeFlags grabType;
        public float travelSpeed;
        public bool useFixedTravelTime;

        public PoseMeasureParameters scoringModifier;
        public GrabbingRule pinchGrabRules;
        public GrabbingRule palmGrabRules;
    }

    /// <summary>
    /// A HandGrabInteractable indicates the properties about how a hand can snap to an object.
    /// The most important is the position/rotation and finger rotations for the hand,
    /// but it can also contain extra information like a valid holding surface (instead of just
    /// a single point) or a visual representation (using a hand-ghost)
    /// </summary>
    [Serializable]
    public class HandGrabInteractable : Interactable<HandGrabInteractor, HandGrabInteractable>,
        ISnappable, IRigidbodyRef, IHandGrabInteractable
    {
        [Header("Grab")]
        /// <summary>
        /// The transform of the object this HandGrabInteractable refers to.
        /// Typically the parent.
        /// </summary>
        [SerializeField]
        private Transform _relativeTo;

        [SerializeField]
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        [SerializeField]
        private Grabbable _grabbable;
        public Grabbable Grabbable => _grabbable;

        [SerializeField]
        private bool _resetGrabOnGrabsUpdated = true;
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

        [SerializeField, Optional]
        private PhysicsGrabbable _physicsGrabbable = null;

        [SerializeField]
        private PoseMeasureParameters _scoringModifier = new PoseMeasureParameters(0.1f, 0f);

        [Space]
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.All;
        [SerializeField]
        private GrabbingRule _pinchGrabRules = GrabbingRule.DefaultPinchRule;
        [SerializeField]
        private GrabbingRule _palmGrabRules = GrabbingRule.DefaultPalmRule;

        [Header("Snap")]
        /// <summary>
        /// How the snap will occur.
        /// For example the hand can artificially move to perfectly wrap the object, or the object can move to align with the hand.
        /// </summary>
        [Tooltip("How the snap will occur, for example the hand can artificially move to perfectly wrap the object, or the object can move to align with the hand")]
        [SerializeField]
        private SnapType _snapType = SnapType.ObjectToHand;

        /// <summary>
        /// When attracting the object, indicates the  rate it will take for the object to realign with the hand after a grab
        /// </summary>
        [Tooltip("When attracting the object, indicates the rate (in m/s, or seconds if UseFixedTravelTime is enabled) for the object to realign with the hand after a grab.")]
        [SerializeField]
        private float _travelSpeed = 1f;
        /// <summary>
        /// Changes the units of the TravelSpeed, disabled means m/s while enabled is fixed seconds
        /// </summary>
        [Tooltip("Changes the units of the TravelSpeed, disabled means m/s while enabled is fixed seconds")]
        [SerializeField]
        private bool _useFixedTravelTime;
        /// <summary>
        /// Animation to use in conjunction with TravelSpeed to define the traveling motion speeds.
        /// </summary>
        [Tooltip("Animation to use in conjunction with TravelSpeed to define the traveling motion.")]
        [SerializeField, Optional]
        private AnimationCurve _travelCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField, Optional]
        private List<HandGrabPoint> _handGrabPoints = new List<HandGrabPoint>();

        /// <summary>
        /// General getter for the transform of the object this interactable refers to.
        /// </summary>
        public Transform RelativeTo => _relativeTo != null ? _relativeTo : this.transform.parent;

        /// <summary>
        /// General getter indicating how the hand and object will align for the grab.
        /// </summary>
        public SnapType SnapType => _snapType;

        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;
        public GrabbingRule PinchGrabRules => _pinchGrabRules;
        public GrabbingRule PalmGrabRules => _palmGrabRules;

        public List<HandGrabPoint> GrabPoints => _handGrabPoints;
        public Collider[] Colliders { get; private set; }

        private GrabPointsPoseFinder _grabPointsPoseFinder;

        private static CollisionInteractionRegistry<HandGrabInteractor, HandGrabInteractable> _registry = null;

        protected bool _started = false;

        #region editor events
        protected virtual void Reset()
        {
            _rigidbody = this.GetComponentInParent<Rigidbody>();
            _relativeTo = _rigidbody.transform;
            _grabbable = this.GetComponentInParent<Grabbable>();
            if (_grabbable != null)
            {
                _physicsGrabbable = _grabbable.GetComponent<PhysicsGrabbable>();
            }
        }
        #endregion

        protected virtual void Awake()
        {
            if (_registry == null)
            {
                _registry = new CollisionInteractionRegistry<HandGrabInteractor, HandGrabInteractable>();
                SetRegistry(_registry);
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Rigidbody);
            Colliders = Rigidbody.GetComponentsInChildren<Collider>();
            Assert.IsTrue(Colliders.Length > 0,
                "The associated Rigidbody must have at least one Collider.");
            Assert.IsNotNull(_grabbable);
            _grabPointsPoseFinder = new GrabPointsPoseFinder(_handGrabPoints, _relativeTo, this.transform);
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

        #region pose snapping

        public Tween GenerateObjectToHandTween(in Pose from, in Pose to)
        {
            if (SnapType == SnapType.HandToObject
                || SnapType == SnapType.None)
            {
                Tween noopTween = new Tween(to, 0f);
                noopTween.TweenTo(to);
                return noopTween;
            }

            float tweenTime = _travelSpeed;
            if (!_useFixedTravelTime)
            {
                float travelDistance = PoseTravelData.PerceivedDistance(from, to);
                tweenTime = travelDistance / _travelSpeed;
            }
            Tween tween = new Tween(from, tweenTime, 0.25f, _travelCurve);
            tween.TweenTo(to);
            return tween;
        }

        public bool CalculateBestPose(Pose userPose, float handScale, Handedness handedness,
            ref HandPose result, ref Pose snapPoint, out bool usesHandPose, out float score)
        {
            return _grabPointsPoseFinder.FindBestPose(userPose, handScale, handedness,
                ref result, ref snapPoint, _scoringModifier, out usesHandPose, out score);
        }

        public bool UsesHandPose()
        {
            return _grabPointsPoseFinder.UsesHandPose();
        }

        #endregion

        #region generation
        /// <summary>
        /// Creates a new HandGrabInteractable under the given object
        /// </summary>
        /// <param name="parent">The relative object for the interactable</param>
        /// <param name="name">Name for the GameObject holding this interactable</param>
        /// <returns>An non-populated HandGrabInteractable</returns>
        public static HandGrabInteractable Create(Transform parent, string name = null)
        {
            GameObject go = new GameObject(name ?? "HandGrabInteractable");
            go.transform.SetParent(parent, false);
            HandGrabInteractable record = go.AddComponent<HandGrabInteractable>();
            record._relativeTo = parent;
            return record;
        }

        public HandGrabPoint CreatePoint()
        {
            GameObject go = this.gameObject;
            if (this.TryGetComponent(out HandGrabPoint point))
            {
                go = new GameObject("HandGrab Point");
                go.transform.SetParent(this.transform, false);
            }
            HandGrabPoint record = go.AddComponent<HandGrabPoint>();
            return record;
        }
        #endregion

        #region dataSave
        /// <summary>
        /// Serializes the data of the HandGrabInteractable so it can be stored
        /// </summary>
        /// <returns>The struct data to recreate the interactable</returns>
        public HandGrabInteractableData SaveData()
        {
            return new HandGrabInteractableData()
            {
                snapType = _snapType,
                travelSpeed = _travelSpeed,
                useFixedTravelTime = _useFixedTravelTime,
                points = _handGrabPoints.Select(p => p.SaveData()).ToList(),
                scoringModifier = _scoringModifier,
                grabType = _supportedGrabTypes,
                pinchGrabRules = _pinchGrabRules,
                palmGrabRules = _palmGrabRules
            };
        }

        /// <summary>
        /// Populates the HandGrabInteractable with the serialized data version
        /// </summary>
        /// <param name="data">The serialized data for the HandGrabInteractable.</param>
        public void LoadData(HandGrabInteractableData data)
        {
            _snapType = data.snapType;
            _supportedGrabTypes = data.grabType;
            _pinchGrabRules = data.pinchGrabRules;
            _palmGrabRules = data.palmGrabRules;
            _travelSpeed = data.travelSpeed;
            _useFixedTravelTime = data.useFixedTravelTime;
            _scoringModifier = data.scoringModifier;

            if (data.points != null)
            {
                foreach (HandGrabPointData pointData in data.points)
                {
                    LoadPoint(pointData);
                }
            }
        }

        public HandGrabPoint LoadPoint(HandGrabPointData pointData)
        {
            HandGrabPoint point = CreatePoint();
            point.LoadData(pointData, this.RelativeTo);
            _handGrabPoints.Add(point);
            return point;
        }
        #endregion

        public void ApplyVelocities(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            if (_physicsGrabbable == null)
            {
                return;
            }
            _physicsGrabbable.ApplyVelocities(linearVelocity, angularVelocity);
        }


        #region Inject

        public void InjectAllHandGrabInteractable(Transform relativeTo,
            Rigidbody rigidbody, Grabbable grabbable,
            GrabTypeFlags supportedGrabTypes, GrabbingRule pinchGrabRules, GrabbingRule palmGrabRules,
            float travelSpeed, bool useFixedTravelTime, SnapType snapType)
        {
            InjectRelativeTo(relativeTo);
            InjectRigidbody(rigidbody);
            InjectGrabbable(grabbable);
            InjectTravelSpeed(travelSpeed);
            InjectUseFixedTravelTime(useFixedTravelTime);
            InjectSnapType(snapType);
            InjectSupportedGrabTypes(supportedGrabTypes);
            InjectPinchGrabRules(pinchGrabRules);
            InjectPalmGrabRules(palmGrabRules);
        }

        public void InjectRelativeTo(Transform relativeTo)
        {
            _relativeTo = relativeTo;
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectGrabbable(Grabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }

        public void InjectPinchGrabRules(GrabbingRule pinchGrabRules)
        {
            _pinchGrabRules = pinchGrabRules;
        }

        public void InjectPalmGrabRules(GrabbingRule palmGrabRules)
        {
            _palmGrabRules = palmGrabRules;
        }

        public void InjectOptionalPhysicsGrabbable(PhysicsGrabbable physicsGrabbable)
        {
            _physicsGrabbable = physicsGrabbable;
        }

        public void InjectSnapType(SnapType snapType)
        {
            _snapType = snapType;
        }

        public void InjectTravelSpeed(float travelSpeed)
        {
            _travelSpeed = travelSpeed;
        }

        public void InjectUseFixedTravelTime(bool useFixedTravelTime)
        {
            _useFixedTravelTime = useFixedTravelTime;
        }

        public void InjectOptionalTravelCurve(AnimationCurve travelCurve)
        {
            _travelCurve = travelCurve;
        }

        public void InjectOptionalHandGrabPoints(List<HandGrabPoint> handGrabPoints)
        {
            _handGrabPoints = handGrabPoints;
        }
        #endregion

    }
}
