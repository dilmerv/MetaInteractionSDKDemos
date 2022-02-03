/************************************************************************************

Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK License Version 3.4.1 (the "License");
you may not use the Oculus SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.4.1

Unless required by applicable law or agreed to in writing, the Oculus SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

[System.Serializable]
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public class OVRProjectConfig : ScriptableObject
{
	public enum DeviceType
	{
		//GearVrOrGo = 0, // DEPRECATED
		Quest = 1,
		Quest2 = 2
	}

	public enum HandTrackingSupport
	{
		ControllersOnly = 0,
		ControllersAndHands = 1,
		HandsOnly = 2
	}

	public enum HandTrackingFrequency
	{
		LOW = 0,
		HIGH = 1,
		MAX = 2
	}

	public enum SpatialAnchorsSupport
	{
		Disabled = 0,
		Enabled = 1,
	}

	public enum RenderModelSupport
	{
		Disabled = 0,
		Enabled = 1,
	}

	public enum TrackedKeyboardSupport
	{
		None = 0,
		Supported = 1,
		Required = 2
	}


	public List<DeviceType> targetDeviceTypes = new List<DeviceType> {DeviceType.Quest, DeviceType.Quest2};
	public bool allowOptional3DofHeadTracking = false;
	public HandTrackingSupport handTrackingSupport = HandTrackingSupport.ControllersOnly;
	public HandTrackingFrequency handTrackingFrequency = HandTrackingFrequency.LOW;
	public SpatialAnchorsSupport spatialAnchorsSupport = SpatialAnchorsSupport.Disabled;
	public RenderModelSupport renderModelSupport = RenderModelSupport.Disabled;
	public TrackedKeyboardSupport trackedKeyboardSupport = TrackedKeyboardSupport.None;

	public bool disableBackups = true;
	public bool enableNSCConfig = true;
	public string securityXmlPath;

	public bool skipUnneededShaders = false;

	[System.Obsolete("Focus awareness is now required. The option will be deprecated.", false)]
	public bool focusAware = true;

	public bool requiresSystemKeyboard = false;
	public bool experimentalFeaturesEnabled = false;
	public bool insightPassthroughEnabled = false;
	public Texture2D systemSplashScreen;

	//public const string OculusProjectConfigAssetPath = "Assets/Oculus/OculusProjectConfig.asset";

	static OVRProjectConfig()
	{
		// BuildPipeline.isBuildingPlayer cannot be called in a static constructor
		// Run Update once to call GetProjectConfig then remove delegate
		EditorApplication.update += Update;
	}

	static void Update()
	{
		// Initialize the asset if it doesn't exist
		GetProjectConfig();
		// Stop running Update
		EditorApplication.update -= Update;
	}

	private static string GetOculusProjectConfigAssetPath()
	{
		var so = ScriptableObject.CreateInstance(typeof(OVRPluginUpdaterStub));
		var script = MonoScript.FromScriptableObject(so);
		string assetPath = AssetDatabase.GetAssetPath(script);
		string editorDir = Directory.GetParent(assetPath).FullName;
		string ovrDir = Directory.GetParent(editorDir).FullName;
		string oculusDir = Directory.GetParent(ovrDir).FullName;

		if (OVRPluginUpdaterStub.IsInsidePackageDistribution())
		{
			oculusDir = Path.GetFullPath(Path.Combine(Application.dataPath, "Oculus"));
			if (!Directory.Exists(oculusDir))
			{
				Directory.CreateDirectory(oculusDir);
			}
		}

		string configAssetPath = Path.GetFullPath(Path.Combine(oculusDir, "OculusProjectConfig.asset"));
		Uri configUri = new Uri(configAssetPath);
		Uri projectUri = new Uri(Application.dataPath);
		Uri relativeUri = projectUri.MakeRelativeUri(configUri);

		return relativeUri.ToString();
	}

	public static OVRProjectConfig GetProjectConfig()
	{
		OVRProjectConfig projectConfig = null;
		string oculusProjectConfigAssetPath = GetOculusProjectConfigAssetPath();
		try
		{
			projectConfig = AssetDatabase.LoadAssetAtPath(oculusProjectConfigAssetPath, typeof(OVRProjectConfig)) as OVRProjectConfig;
		}
		catch (System.Exception e)
		{
			Debug.LogWarningFormat("Unable to load ProjectConfig from {0}, error {1}", oculusProjectConfigAssetPath, e.Message);
		}
		// Initialize the asset only if a build is not currently running.
		if (projectConfig == null && !BuildPipeline.isBuildingPlayer)
		{
			projectConfig = ScriptableObject.CreateInstance<OVRProjectConfig>();
			projectConfig.targetDeviceTypes = new List<DeviceType>();
			projectConfig.targetDeviceTypes.Add(DeviceType.Quest);
			projectConfig.targetDeviceTypes.Add(DeviceType.Quest2);
			projectConfig.allowOptional3DofHeadTracking = false;
			projectConfig.handTrackingSupport = HandTrackingSupport.ControllersOnly;
			projectConfig.handTrackingFrequency = HandTrackingFrequency.LOW;
			projectConfig.spatialAnchorsSupport = SpatialAnchorsSupport.Disabled;
			projectConfig.disableBackups = true;
			projectConfig.enableNSCConfig = true;
			projectConfig.skipUnneededShaders = false;
			projectConfig.requiresSystemKeyboard = false;
			projectConfig.experimentalFeaturesEnabled = false;
			projectConfig.insightPassthroughEnabled = false;
			AssetDatabase.CreateAsset(projectConfig, oculusProjectConfigAssetPath);
		}
		// Force migration to Quest device if still on legacy GearVR/Go device type
		if (projectConfig.targetDeviceTypes.Contains((DeviceType)0)) // deprecated GearVR/Go device
		{
			projectConfig.targetDeviceTypes.Remove((DeviceType)0); // deprecated GearVR/Go device
			if (!projectConfig.targetDeviceTypes.Contains(DeviceType.Quest))
			{
				projectConfig.targetDeviceTypes.Add(DeviceType.Quest);
			}
			if (!projectConfig.targetDeviceTypes.Contains(DeviceType.Quest2))
			{
				projectConfig.targetDeviceTypes.Add(DeviceType.Quest2);
			}
		}
		return projectConfig;
	}

	public static void CommitProjectConfig(OVRProjectConfig projectConfig)
	{
		string oculusProjectConfigAssetPath = GetOculusProjectConfigAssetPath();
		if (AssetDatabase.GetAssetPath(projectConfig) != oculusProjectConfigAssetPath)
		{
			Debug.LogWarningFormat("The asset path of ProjectConfig is wrong. Expect {0}, get {1}", oculusProjectConfigAssetPath, AssetDatabase.GetAssetPath(projectConfig));
		}
		EditorUtility.SetDirty(projectConfig);
	}
}
