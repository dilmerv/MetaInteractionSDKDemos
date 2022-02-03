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

namespace Oculus.Interaction
{
    /// <summary>
    /// A Transformer that translates the target, with optional parent-space constraints
    /// </summary>
    public class OneHandTranslateTransformer : MonoBehaviour, ITransformer
    {
        [Serializable]
        public class OneHandTranslateConstraints
        {
            public bool ConstraintsAreRelative;
            public FloatConstraint MinX;
            public FloatConstraint MaxX;
            public FloatConstraint MinY;
            public FloatConstraint MaxY;
            public FloatConstraint MinZ;
            public FloatConstraint MaxZ;
        }

        [SerializeField]
        private OneHandTranslateConstraints _constraints;

        public OneHandTranslateConstraints Constraints
        {
            get
            {
                return _constraints;
            }

            set
            {
                _constraints = value;
                GenerateParentConstraints();
            }
        }

        private OneHandTranslateConstraints _parentConstraints = null;

        private Vector3 _initialPosition = new Vector3();
        private Vector3 _positionDelta = new Vector3();

        private ITransformable _transformable;

        public void Initialize(ITransformable transformable)
        {
            _transformable = transformable;
            _initialPosition = _transformable.Transform.localPosition;
            GenerateParentConstraints();
        }
        private void GenerateParentConstraints()
        {
            if (!_constraints.ConstraintsAreRelative)
            {
                _parentConstraints = _constraints;
            }
            else
            {
                _parentConstraints = new OneHandTranslateConstraints();

                _parentConstraints.MinX = new FloatConstraint();
                _parentConstraints.MinY = new FloatConstraint();
                _parentConstraints.MinZ = new FloatConstraint();
                _parentConstraints.MaxX = new FloatConstraint();
                _parentConstraints.MaxY = new FloatConstraint();
                _parentConstraints.MaxZ = new FloatConstraint();

                if (_constraints.MinX.Constrain)
                {
                    _parentConstraints.MinX.Constrain = true;
                    _parentConstraints.MinX.Value = _constraints.MinX.Value + _initialPosition.x;
                }
                if (_constraints.MaxX.Constrain)
                {
                    _parentConstraints.MaxX.Constrain = true;
                    _parentConstraints.MaxX.Value = _constraints.MaxX.Value + _initialPosition.x;
                }
                if (_constraints.MinY.Constrain)
                {
                    _parentConstraints.MinY.Constrain = true;
                    _parentConstraints.MinY.Value = _constraints.MinY.Value + _initialPosition.y;
                }
                if (_constraints.MaxY.Constrain)
                {
                    _parentConstraints.MaxY.Constrain = true;
                    _parentConstraints.MaxY.Value = _constraints.MaxY.Value + _initialPosition.y;
                }
                if (_constraints.MinZ.Constrain)
                {
                    _parentConstraints.MinZ.Constrain = true;
                    _parentConstraints.MinZ.Value = _constraints.MinZ.Value + _initialPosition.z;
                }
                if (_constraints.MaxZ.Constrain)
                {
                    _parentConstraints.MaxZ.Constrain = true;
                    _parentConstraints.MaxZ.Value = _constraints.MaxZ.Value + _initialPosition.z;
                }
            }
        }

        public void BeginTransform()
        {
            // Save initial position in parent space
            Transform targetTransform = _transformable.Transform;
            _initialPosition = targetTransform.localPosition;
            _positionDelta = Vector3.zero;
        }

        public void UpdateTransform()
        {
            var grabPoint = _transformable.GrabPoints[0];
            var targetTransform = _transformable.Transform;

            var initialPositionWorldSpace = _initialPosition;
            if (targetTransform.parent != null)
            {
                initialPositionWorldSpace = targetTransform.parent.TransformPoint(_initialPosition);
            }

            _positionDelta += grabPoint.GrabPosition - grabPoint.PreviousGrabPosition;
            var constrainedPosition = _positionDelta + initialPositionWorldSpace;

            // the translation constraints occur in parent space
            if (targetTransform.parent != null)
            {
                constrainedPosition = targetTransform.parent.InverseTransformPoint(constrainedPosition);
            }

            if (_parentConstraints.MinX.Constrain)
            {
                constrainedPosition.x = Mathf.Max(constrainedPosition.x, _parentConstraints.MinX.Value);
            }
            if (_parentConstraints.MaxX.Constrain)
            {
                constrainedPosition.x = Mathf.Min(constrainedPosition.x, _parentConstraints.MaxX.Value);
            }
            if (_parentConstraints.MinY.Constrain)
            {
                constrainedPosition.y = Mathf.Max(constrainedPosition.y, _parentConstraints.MinY.Value);
            }
            if (_parentConstraints.MaxY.Constrain)
            {
                constrainedPosition.y = Mathf.Min(constrainedPosition.y, _parentConstraints.MaxY.Value);
            }
            if (_parentConstraints.MinZ.Constrain)
            {
                constrainedPosition.z = Mathf.Max(constrainedPosition.z, _parentConstraints.MinZ.Value);
            }
            if (_parentConstraints.MaxZ.Constrain)
            {
                constrainedPosition.z = Mathf.Min(constrainedPosition.z, _parentConstraints.MaxZ.Value);
            }

            // Convert the constrained position back to world space
            if (targetTransform.parent != null)
            {
                constrainedPosition = targetTransform.parent.TransformPoint(constrainedPosition);
            }

            targetTransform.position = constrainedPosition;
        }

        public void EndTransform() { }

        #region Inject

        public void InjectOptionalTranslateConstraints(OneHandTranslateConstraints constraints)
        {
            _constraints = constraints;
        }

        #endregion
    }
}
