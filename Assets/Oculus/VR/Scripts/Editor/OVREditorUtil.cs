/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;

public static class OVREditorUtil {

	private static GUIContent tooltipLink = new GUIContent("[?]");

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupBoolField(Object target, string name, ref bool member, ref bool modified, string docLink = "")
    {
        SetupBoolField(target, new GUIContent(name), ref member, ref modified, docLink);
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupBoolField(Object target, GUIContent name, ref bool member, ref bool modified, string docLink = "")
    {
        EditorGUILayout.BeginHorizontal();

		EditorGUI.BeginChangeCheck();
        bool value = EditorGUILayout.Toggle(name, member);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Changed " + name);
            member = value;
            modified = true;
        }

        if (!string.IsNullOrEmpty(docLink))
        {
            DisplayDocLink(docLink);
        }

        EditorGUILayout.EndHorizontal();
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupIntField(Object target, string name, ref int member, ref bool modified)
    {
        SetupIntField(target, new GUIContent(name), ref member, ref modified);
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupIntField(Object target, GUIContent name, ref int member, ref bool modified)
    {
		EditorGUI.BeginChangeCheck();
        int value = EditorGUILayout.IntField(name, member);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Changed " + name);
            member = value;
            modified = true;
        }
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupFloatField(Object target, string name, ref float member, ref bool modified)
    {
        SetupFloatField(target, new GUIContent(name), ref member, ref modified);
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupFloatField(Object target, GUIContent name, ref float member, ref bool modified)
    {
		EditorGUI.BeginChangeCheck();
        float value = EditorGUILayout.FloatField(name, member);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Changed " + name);
            member = value;
            modified = true;
        }
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupDoubleField(Object target, string name, ref double member, ref bool modified)
    {
        SetupDoubleField(target, new GUIContent(name), ref member, ref modified);
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupDoubleField(Object target, GUIContent name, ref double member, ref bool modified)
    {
        EditorGUI.BeginChangeCheck();
        double value = EditorGUILayout.DoubleField(name, member);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Changed " + name);
            member = value;
            modified = true;
        }
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupColorField(Object target, string name, ref Color member, ref bool modified)
    {
        SetupColorField(target, new GUIContent(name), ref member, ref modified);
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupColorField(Object target, GUIContent name, ref Color member, ref bool modified)
    {
        EditorGUI.BeginChangeCheck();
        Color value = EditorGUILayout.ColorField(name, member);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Changed " + name);
            member = value;
            modified = true;
        }
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupLayerMaskField(Object target, string name, ref LayerMask layerMask, string[] layerMaskOptions, ref bool modified)
    {
        SetupLayerMaskField(target, new GUIContent(name), ref layerMask, layerMaskOptions, ref modified);
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupLayerMaskField(Object target, GUIContent name, ref LayerMask layerMask, string[] layerMaskOptions, ref bool modified)
    {
        EditorGUI.BeginChangeCheck();
        int value = EditorGUILayout.MaskField(name, layerMask, layerMaskOptions);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Changed " + name);
            layerMask = value;
        }
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupEnumField<T>(Object target, string name, ref T member, ref bool modified, string docLink = "") where T : struct
    {
        SetupEnumField(target, new GUIContent(name), ref member, ref modified, docLink);
    }

    [Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void SetupEnumField<T>(Object target, GUIContent name, ref T member, ref bool modified, string docLink = "") where T : struct
    {
		GUILayout.BeginHorizontal();

		EditorGUI.BeginChangeCheck();
		T value = (T)(object)EditorGUILayout.EnumPopup(name, member as System.Enum);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Changed " + name);
            member = value;
            modified = true;
        }

		if (!string.IsNullOrEmpty(docLink))
		{
            DisplayDocLink(docLink);
		}

		GUILayout.EndHorizontal();
    }

	[Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
	public static void SetupInputField(Object target, string name, ref string member, ref bool modified, string docLink = "")
	{
		SetupInputField(target, new GUIContent(name), ref member, ref modified, docLink);
	}

	[Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
	public static void SetupInputField(Object target, GUIContent name, ref string member, ref bool modified, string docLink = "")
	{
		GUILayout.BeginHorizontal();

		EditorGUI.BeginChangeCheck();
		string value = EditorGUILayout.TextField(name, member);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			member = value;
			modified = true;
		}

		if (!string.IsNullOrEmpty(docLink))
		{
            DisplayDocLink(docLink);
		}

		GUILayout.EndHorizontal();
	}

	[Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
	public static void SetupTexture2DField(Object target, string name, ref Texture2D member, ref bool modified)
	{
		SetupTexture2DField(target, new GUIContent(name), ref member, ref modified);
	}

	[Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
	public static void SetupTexture2DField(Object target, GUIContent name, ref Texture2D member, ref bool modified, string docLink = "")
	{
        EditorGUILayout.BeginHorizontal();

		EditorGUI.BeginChangeCheck();
		Texture2D value = (Texture2D)EditorGUILayout.ObjectField(name, member, typeof(Texture2D), false);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			member = value;
			modified = true;
		}

        if (!string.IsNullOrEmpty(docLink))
        {
            DisplayDocLink(docLink);
        }

        EditorGUILayout.EndHorizontal();
	}

	[Conditional("UNITY_EDITOR_WIN"), Conditional("UNITY_STANDALONE_WIN"), Conditional("UNITY_ANDROID")]
    public static void DisplayDocLink(string docLink)
    {
#if UNITY_2021_1_OR_NEWER
			if (EditorGUILayout.LinkButton(tooltipLink))
			{
				Application.OpenURL(docLink);
			}
#else
			if (GUILayout.Button(tooltipLink, GUILayout.ExpandWidth(false)))
			{
				Application.OpenURL(docLink);
			}
#endif
    }
}
