/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEditor;

namespace Oculus.Interaction.Input.Editor
{
    [CustomEditor(typeof(InputDataProviderUpdateTriggerOVR))]
    public class InputDataProviderUpdateTriggerOVREditor : UnityEditor.Editor
    {
        private SerializedProperty _cameraRigRefProperty;
        private SerializedProperty _enableUpdateProperty;
        private SerializedProperty _enableFixedUpdateProperty;

        private void Awake()
        {
            _cameraRigRefProperty = serializedObject.FindProperty("_cameraRigRef");
            _enableUpdateProperty = serializedObject.FindProperty("_enableUpdate");
            _enableFixedUpdateProperty = serializedObject.FindProperty("_enableFixedUpdate");
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (_cameraRigRefProperty.objectReferenceValue is IOVRCameraRigRef)
            {
                if (_enableUpdateProperty.boolValue
                    || _enableFixedUpdateProperty.boolValue)
                {
                    EditorGUILayout.HelpBox(
                        "Using Camera Rig Ref will already trigger an update whenever OVR updates.\n" +
                        "Activating Enable Update or Enable Fixed Update might cause redundant triggers",
                        MessageType.Warning);
                }
            }
        }
    }
}
