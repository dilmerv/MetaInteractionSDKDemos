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
using System.Runtime.InteropServices;
using UnityEngine;

using ColorMapType = OVRPlugin.InsightPassthroughColorMapType;

/// <summary>
/// A layer used for passthrough.
/// </summary>
public class OVRPassthroughLayer : MonoBehaviour
{
	#region Public Interface

	/// <summary>
	/// The passthrough projection surface type: reconstructed | user defined.
	/// </summary>
	public enum ProjectionSurfaceType
	{
		Reconstructed, ///< Reconstructed surface type will render passthrough using automatic environment depth reconstruction
		UserDefined ///< UserDefined allows you to define a surface
	}

	/// <summary>
	/// The type of the surface which passthrough textures are projected on: Automatic reconstruction or user-defined geometry.
	/// This field can only be modified immediately after the component is instantiated (e.g. using `AddComponent`).
	/// Once the backing layer has been created, changes won't be reflected unless the layer is disabled and enabled again.
	/// Default is automatic reconstruction.
	/// </summary>
	public ProjectionSurfaceType projectionSurfaceType = ProjectionSurfaceType.Reconstructed;

	/// <summary>
	/// Overlay type that defines the placement of the passthrough layer to appear on top as an overlay or beneath as an underlay of the application’s main projection layer. By default, the passthrough layer appears as an overlay.
	/// </summary>
	public OVROverlay.OverlayType overlayType = OVROverlay.OverlayType.Overlay;

	/// <summary>
	/// The compositionDepth defines the order of the layers in composition. The layer with smaller compositionDepth would be composited in the front of the layer with larger compositionDepth. The default value is zero.

	/// </summary>
	public int compositionDepth = 0;

	/// <summary>
	/// Property that can hide layers when required. Should be false when present, true when hidden. By default, the value is set to false, which means the layers are present.

	/// </summary>
	public bool hidden = false;

	/// <summary>
	/// Specify whether `colorScale` and `colorOffset` should be applied to this layer. By default, the color scale and offset are not applied to the layer.
	/// </summary>
	public bool overridePerLayerColorScaleAndOffset = false;

	/// <summary>
	/// Color scale is a factor applied to the pixel color values during compositing.
	/// The four components of the vector correspond to the R, G, B, and A values, default set to `{1,1,1,1}`.

	/// </summary>
	public Vector4 colorScale = Vector4.one;

	/// <summary>
	/// Color offset is a value which gets added to the pixel color values during compositing.
	/// The four components of the vector correspond to the R, G, B, and A values, default set to `{0,0,0,0}`.
	/// </summary>
	public Vector4 colorOffset = Vector4.zero;

	/// <summary>
	/// Add a GameObject to the Insight Passthrough projection surface. This is only applicable
	/// if the projection surface type is `UserDefined`.
	/// When `updateTransform` parameter is set to `true`, OVRPassthroughLayer will update the transform
	/// of the surface mesh every frame. Otherwise only the initial transform is recorded.
	/// </summary>
	/// <param name="obj">The Gameobject you want to add to the Insight Passthrough projection surface.</param>
	/// <param name="updateTransform">Indicate if the transform should be updated every frame</param>
	public void AddSurfaceGeometry(GameObject obj, bool updateTransform = false)
	{
		if (projectionSurfaceType != ProjectionSurfaceType.UserDefined)
		{
			Debug.LogError("Passthrough layer is not configured for surface projected passthrough.");
			return;
		}

		if (surfaceGameObjects.ContainsKey(obj))
		{
			Debug.LogError("Specified GameObject has already been added as passthrough surface.");
			return;
		}

		if (obj.GetComponent<MeshFilter>() == null)
		{
			Debug.LogError("Specified GameObject does not have a mesh component.");
			return;
		}

		// Mesh and instance can't be created immediately, because the compositor layer may not have been initialized yet (layerId = 0).
		// Queue creation and attempt to do it in the update loop.
		deferredSurfaceGameObjects.Add(
			new DeferredPassthroughMeshAddition
			{
				gameObject = obj,
				updateTransform = updateTransform
			});
	}

	/// <summary>
	/// Removes a GameObject that was previously added using `AddSurfaceGeometry` from the projection surface.
	/// </summary>
	/// <param name="obj">The Gameobject to remove. </param>
	public void RemoveSurfaceGeometry(GameObject obj)
	{
		PassthroughMeshInstance passthroughMeshInstance;
		if (surfaceGameObjects.TryGetValue(obj, out passthroughMeshInstance))
		{
			if (OVRPlugin.DestroyInsightPassthroughGeometryInstance(passthroughMeshInstance.instanceHandle) &&
					OVRPlugin.DestroyInsightTriangleMesh(passthroughMeshInstance.meshHandle))
			{
				surfaceGameObjects.Remove(obj);
			}
			else
			{
				Debug.LogError("GameObject could not be removed from passthrough surface.");
			}
		}
		else
		{
			int count = deferredSurfaceGameObjects.RemoveAll(x => x.gameObject == obj);
			if (count == 0)
			{
				Debug.LogError("Specified GameObject has not been added as passthrough surface.");
			}
		}
	}

	/// <summary>
	/// Checks if the given gameobject is a surface geometry (If called with AddSurfaceGeometry).
	/// </summary>
	/// <returns> True if the gameobject is a surface geometry. </returns>
	public bool IsSurfaceGeometry(GameObject obj)
	{
		return surfaceGameObjects.ContainsKey(obj) || deferredSurfaceGameObjects.Exists(x => x.gameObject == obj);
	}

	/// <summary>
	/// Float that defines the passthrough texture opacity.
	/// </summary>
	public float textureOpacity
	{
		get
		{
			return textureOpacity_;
		}
		set
		{
			if (value != textureOpacity_)
			{
				textureOpacity_ = value;
				styleDirty = true;
			}
		}
	}

	/// <summary>
	/// Enable or disable the Edge rendering.
	/// Use this flag to enable or disable the edge rendering but retain the previously selected color (incl. alpha)
	/// in the UI when it is disabled.
	/// </summary>
	public bool edgeRenderingEnabled
	{
		get
		{
			return edgeRenderingEnabled_;
		}
		set
		{
			if (value != edgeRenderingEnabled_)
			{
				edgeRenderingEnabled_ = value;
				styleDirty = true;
			}
		}
	}

	/// <summary>
	/// Color for the edge rendering.
	/// </summary>
	public Color edgeColor
	{
		get
		{
			return edgeColor_;
		}
		set
		{
			if (value != edgeColor_)
			{
				edgeColor_ = value;
				styleDirty = true;
			}
		}
	}

	/// <summary>
	/// This color map method allows to recolor the grayscale camera images by specifying a color lookup table.
	/// Scripts should call the designated methods to set a color map. The fields and properties
	/// are only intended for the inspector UI.
	/// </summary>
	/// <param name="values">The color map as an array of 256 color values to map each grayscale input to a color.</param>
	public void SetColorMap(Color[] values)
	{
		if (values.Length != 256)
			throw new ArgumentException("Must provide exactly 256 colors");

		colorMapType = ColorMapType.MonoToRgba;
		colorMapEditorType = ColorMapEditorType.Custom;
		AllocateColorMapData();
		for (int i = 0; i < 256; i++)
		{
			WriteColorToColorMap(i, ref values[i]);
		}

		styleDirty = true;
	}

	/// <summary>
	/// This method allows to generate a color map from a set of color controls. Contrast, brightness and posterization is
	/// applied to the grayscale passthrough value, which is finally mapped to a color according to
	/// the provided gradient. The gradient can be null, in which case no colorization takes place.
	/// </summary>
	/// <param name="contrast">The contrast value. Range from -1 (minimum) to 1 (maximum). </param>
	/// <param name="brightness">The brightness value. Range from 0 (minimum) to 1 (maximum). </param>
	/// <param name="posterize">The posterize value. Range from 0 to 1, where 0 = no posterization (no effect), 1 = reduce to two colors. </param>
	/// <param name="gradient">The gradient will be evaluated from 0 (no intensity) to 1 (maximum intensity). </param>
	public void SetColorMapControls(float contrast, float brightness = 0.0f, float posterize = 0.0f, Gradient gradient = null)
	{
		colorMapEditorType = ColorMapEditorType.Controls;
		colorMapEditorContrast = contrast;
		colorMapEditorBrightness = brightness;
		colorMapEditorPosterize = posterize;
		if (gradient != null)
		{
			colorMapEditorGradient = gradient;
		}
		else if (!colorMapEditorGradient.Equals(colorMapNeutralGradient))
		{
			// Leave gradient untouched if it's already neutral to avoid unnecessary memory allocations.
			colorMapEditorGradient = CreateNeutralColorMapGradient();
		}
	}

	/// <summary>
	/// This method allows to specify the color map as an array of 256 8-bit intensity values.
	/// Use this to map each grayscale input value to a grayscale output value.
	/// </summary>
	/// <param name="values">Array of 256 8-bit values.</param>
	public void SetColorMapMonochromatic(byte[] values)
	{
		if (values.Length != 256)
			throw new ArgumentException("Must provide exactly 256 values");

		colorMapType = ColorMapType.MonoToMono;
		colorMapEditorType = ColorMapEditorType.Custom;
		AllocateColorMapData();
		Buffer.BlockCopy(values, 0, colorMapData, 0, 256);

		styleDirty = true;
	}

	/// <summary>
	/// Disables color mapping. Use this to remove any effects.
	/// </summary>
	public void DisableColorMap()
	{
		colorMapEditorType = ColorMapEditorType.None;
	}

	#endregion


	#region Editor Interface
	/// <summary>
	/// Unity editor enumerator to provide a dropdown in the inspector.
	/// </summary>
	public enum ColorMapEditorType
	{
		None, ///< Will clear the colormap
		Controls, ///< Will update the colormap from the inspector controls
		Custom ///< Will not update the colormap
	}

	[SerializeField]
	private ColorMapEditorType colorMapEditorType_ = ColorMapEditorType.None;
	/// <summary>
	/// Editor attribute to get or set the selection in the inspector.
	/// Using this selection will update the `colorMapType` and `colorMapData` if needed.
	/// </summary>
	public ColorMapEditorType colorMapEditorType
	{
		get
		{
			return colorMapEditorType_;
		}
		set
		{
			if (value != colorMapEditorType_)
			{
				colorMapEditorType_ = value;

				// Update colorMapType and colorMapData to match new editor selection
				switch (value)
				{
					case ColorMapEditorType.None:
						colorMapType = ColorMapType.None;
						DeallocateColorMapData();
						styleDirty = true;
						break;
					case ColorMapEditorType.Controls:
						colorMapType = ColorMapType.MonoToRgba;
						UpdateColorMapFromControls(true);
						break;
					case ColorMapEditorType.Custom:
						// no-op
						break;
				}
			}
		}
	}

	/// <summary>
	/// This field is not intended for public scripting. Use `SetColorMapControls()` instead.
	/// </summary>
	public Gradient colorMapEditorGradient = CreateNeutralColorMapGradient();

	// Keep a private copy of the gradient value. Every frame, it is compared against the public one in UpdateColorMapFromControls() and updated if necessary.
	private Gradient colorMapEditorGradientOld = new Gradient();

	/// <summary>
	/// This field is not intended for public scripting. Use `SetColorMapControls()` instead.
	/// </summary>
	public float colorMapEditorContrast;
	// Keep a private copy of the contrast value. Every frame, it is compared against the public one in UpdateColorMapFromControls() and updated if necessary.
	private float colorMapEditorContrast_ = 0;

	/// <summary>
	/// This field is not intended for public scripting. Use `SetColorMapControls()` instead.
	/// </summary>
	public float colorMapEditorBrightness;
	// Keep a private copy of the brightness value. Every frame, it is compared against the public one in UpdateColorMapFromControls() and updated if necessary.
	private float colorMapEditorBrightness_ = 0;

	/// <summary>
	/// This field is not intended for public scripting. Use `SetColorMapControls()` instead.
	/// </summary>
	public float colorMapEditorPosterize;
	// Keep a private copy of the posterize value. Every frame, it is compared against the public one in UpdateColorMapFromControls() and updated if necessary.
	private float colorMapEditorPosterize_ = 0;

	#endregion

	#region Internal Methods
	private void AddDeferredSurfaceGeometries()
	{
		for (int i = 0; i < deferredSurfaceGameObjects.Count; ++i)
		{
			var entry = deferredSurfaceGameObjects[i];
			bool entryIsPasthroughObject = false;
			if (surfaceGameObjects.ContainsKey(entry.gameObject))
			{
				entryIsPasthroughObject = true;
			}
			else
			{
				ulong meshHandle;
				ulong instanceHandle;
				if (CreateAndAddMesh(entry.gameObject, out meshHandle, out instanceHandle))
				{
					surfaceGameObjects.Add(entry.gameObject, new PassthroughMeshInstance
					{
						meshHandle = meshHandle,
						instanceHandle = instanceHandle,
						updateTransform = entry.updateTransform
					});
					entryIsPasthroughObject = true;
				}
				else
				{
					Debug.LogWarning("Failed to create internal resources for GameObject added to passthrough surface.");
				}
			}

			if (entryIsPasthroughObject)
			{
				deferredSurfaceGameObjects.RemoveAt(i--);
			}
		}
	}

	private Matrix4x4 GetTransformMatrixForPassthroughSurfaceObject(GameObject obj)
	{
		Matrix4x4 worldFromObj = obj.transform.localToWorldMatrix;

		if (!cameraRigInitialized)
		{
			cameraRig = OVRManager.instance.GetComponentInParent<OVRCameraRig>();
			cameraRigInitialized = true;
		}

		Matrix4x4 trackingSpaceFromWorld = (cameraRig != null) ?
			cameraRig.trackingSpace.worldToLocalMatrix :
			Matrix4x4.identity;

		// Use model matrix to switch from left-handed coordinate system (Unity)
		// to right-handed (Open GL/Passthrough API): reverse z-axis
		Matrix4x4 rightHandedFromLeftHanded = Matrix4x4.Scale(new Vector3(1, 1, -1));
		return rightHandedFromLeftHanded * trackingSpaceFromWorld * worldFromObj;
	}

	private bool CreateAndAddMesh(
		GameObject obj,
		out ulong meshHandle,
		out ulong instanceHandle)
	{
		Debug.Assert(passthroughOverlay != null);
		Debug.Assert(passthroughOverlay.layerId > 0);
		meshHandle = 0;
		instanceHandle = 0;

		MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
		if (meshFilter == null)
		{
			Debug.LogError("Passthrough surface GameObject does not have a mesh component.");
			return false;
		}

		Mesh mesh = meshFilter.sharedMesh;

		// TODO: evaluate using GetNativeVertexBufferPtr() instead to avoid copy
		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;
		Matrix4x4 T_worldInsight_model = GetTransformMatrixForPassthroughSurfaceObject(obj);

		if (!OVRPlugin.CreateInsightTriangleMesh(passthroughOverlay.layerId, vertices, triangles, out meshHandle))
		{
			Debug.LogWarning("Failed to create triangle mesh handle.");
			return false;
		}

		if (!OVRPlugin.AddInsightPassthroughSurfaceGeometry(passthroughOverlay.layerId, meshHandle, T_worldInsight_model, out instanceHandle))
		{
			Debug.LogWarning("Failed to add mesh to passthrough surface.");
			return false;
		}

		return true;
	}

	private void DestroySurfaceGeometries(bool addBackToDeferredQueue = false)
	{
		foreach (KeyValuePair<GameObject, PassthroughMeshInstance> el in surfaceGameObjects)
		{
			if (el.Value.meshHandle != 0)
			{
				OVRPlugin.DestroyInsightPassthroughGeometryInstance(el.Value.instanceHandle);
				OVRPlugin.DestroyInsightTriangleMesh(el.Value.meshHandle);

				// When DestroySurfaceGeometries is called from OnDisable, we want to keep track of the existing
				// surface geometries so we can add them back when the script gets enabled again. We simply reinsert
				// them into deferredSurfaceGameObjects for that purpose.
				if (addBackToDeferredQueue)
				{
					deferredSurfaceGameObjects.Add(
						new DeferredPassthroughMeshAddition
						{
							gameObject = el.Key,
							updateTransform = el.Value.updateTransform
						});
				}
			}
		}
		surfaceGameObjects.Clear();
	}

	private void UpdateSurfaceGeometryTransforms()
	{
		// Iterate through mesh instances and see if transforms need to be updated
		foreach (KeyValuePair<GameObject, PassthroughMeshInstance> el in surfaceGameObjects)
		{
			if (el.Value.updateTransform && el.Value.instanceHandle != 0)
			{
				Matrix4x4 T_worldInsight_model = GetTransformMatrixForPassthroughSurfaceObject(el.Key);
				if (!OVRPlugin.UpdateInsightPassthroughGeometryTransform(
					el.Value.instanceHandle,
					T_worldInsight_model))
				{
					Debug.LogWarning("Failed to update a transform of a passthrough surface");
				}
			}
		}
	}

	private void AllocateColorMapData()
	{
		if (colorMapData == null)
		{
			colorMapData = new byte[4096];
			if (colorMapDataHandle.IsAllocated)
			{
				Debug.LogWarning("Passthrough color map data handle is not expected to be allocated at time of buffer allocation");
			}
			colorMapDataHandle = GCHandle.Alloc(colorMapData, GCHandleType.Pinned);
		}
	}

	// Ensure that Passthrough color map data is unpinned and freed.
	private void DeallocateColorMapData()
	{
		if (colorMapData != null)
		{
			if (!colorMapDataHandle.IsAllocated)
			{
				Debug.LogWarning("Passthrough color map data handle is expected to be allocated at time of buffer deallocation");
			}
			else
			{
				colorMapDataHandle.Free();
			}
			colorMapData = null;
		}
	}

	// Returns a gradient from black to white.
	private static Gradient CreateNeutralColorMapGradient()
	{
		return new Gradient()
		{
			colorKeys = new GradientColorKey[2] {
				new GradientColorKey(new Color(0, 0, 0), 0),
				new GradientColorKey(new Color(1, 1, 1), 1)
			},
			alphaKeys = new GradientAlphaKey[2] {
				new GradientAlphaKey(1, 0),
				new GradientAlphaKey(1, 1)
			}
		};
	}

	private void UpdateColorMapFromControls(bool forceUpdate = false)
	{
		if (colorMapEditorType != ColorMapEditorType.Controls)
			return;

		AllocateColorMapData();

		if (forceUpdate ||
			!colorMapEditorGradient.Equals(colorMapEditorGradientOld) ||
			colorMapEditorContrast_ != colorMapEditorContrast ||
			colorMapEditorBrightness_ != colorMapEditorBrightness ||
			colorMapEditorPosterize_ != colorMapEditorPosterize)
		{
			colorMapEditorGradientOld.CopyFrom(colorMapEditorGradient);
			colorMapEditorContrast_ = colorMapEditorContrast;
			colorMapEditorBrightness_ = colorMapEditorBrightness;
			colorMapEditorPosterize_ = colorMapEditorPosterize;

			AllocateColorMapData();

			// Populate colorMapData
			for (int i = 0; i < 256; i++)
			{
				// Apply contrast, brightness and posterization on the grayscale value
				double value = (double)i / 255.0;
				// Constrast and brightness
				double contrastFactor = colorMapEditorContrast + 1; // UI runs from -1 to 1
				value = (value - 0.5) * contrastFactor + 0.5 + colorMapEditorBrightness;
				// Posterization
				if (colorMapEditorPosterize > 0.0f)
				{
					// The posterization slider feels more useful if the progression is exponential. The function is emprically tuned.
					const double posterizationBase = 50.0;
					double posterize = (Math.Pow(posterizationBase, colorMapEditorPosterize) - 1.0) / (posterizationBase - 1.0);
					value = Math.Round(value / posterize) * posterize;
				}

				// Clamp to [0, 1]
				value = Math.Min(Math.Max(value, 0.0), 1.0);

				// Map to value to color
				Color color = colorMapEditorGradient.Evaluate((float)value);

				WriteColorToColorMap(i, ref color);
			}

			styleDirty = true;
		}
	}

	// Write a single color value to the Passthrough color map at the given position.
	private void WriteColorToColorMap(int colorIndex, ref Color color)
	{
		for (int c = 0; c < 4; c++)
		{
			byte[] bytes = BitConverter.GetBytes(color[c]);
			Buffer.BlockCopy(bytes, 0, colorMapData, colorIndex * 16 + c * 4, 4);
		}
	}

	private void SyncToOverlay()
	{
		Debug.Assert(passthroughOverlay != null);

		passthroughOverlay.currentOverlayType = overlayType;
		passthroughOverlay.compositionDepth = compositionDepth;
		passthroughOverlay.hidden = hidden;
		passthroughOverlay.overridePerLayerColorScaleAndOffset = overridePerLayerColorScaleAndOffset;
		passthroughOverlay.colorScale = colorScale;
		passthroughOverlay.colorOffset = colorOffset;

		if (passthroughOverlay.currentOverlayShape != overlayShape)
		{
			if (passthroughOverlay.layerId > 0)
			{
				Debug.LogWarning("Change to projectionSurfaceType won't take effect until the layer goes through a disable/enable cycle. ");
			}

			if (projectionSurfaceType == ProjectionSurfaceType.Reconstructed)
			{
				// Ensure there are no custom surface geometries when switching to reconstruction passthrough.
				Debug.Log("Removing user defined surface geometries");
				DestroySurfaceGeometries(false);
			}

			passthroughOverlay.currentOverlayShape = overlayShape;
		}

		// Disable the overlay when passthrough is disabled as a whole so the layer doesn't get submitted.
		// Both the desired (`isInsightPassthroughEnabled`) and the actual (IsInsightPassthroughInitialized()) PT
		// initialization state are taken into account s.t. the overlay gets disabled as soon as PT is flagged to be
		// disabled, and enabled only when PT is up and running again.
		passthroughOverlay.enabled = OVRManager.instance != null &&
			OVRManager.instance.isInsightPassthroughEnabled &&
			OVRManager.IsInsightPassthroughInitialized();
	}

	#endregion

	#region Internal Fields/Properties
	private OVRCameraRig cameraRig;
	private bool cameraRigInitialized = false;
	private GameObject auxGameObject;
	private OVROverlay passthroughOverlay;

	// Each GameObjects requires a MrTriangleMesh and a MrPassthroughGeometryInstance handle.
	// The structure also keeps a flag for whether transform updates should be tracked.
	private struct PassthroughMeshInstance
	{
		public ulong meshHandle;
		public ulong instanceHandle;
		public bool updateTransform;
	}

	// A structure for tracking a deferred addition of a game object to the projection surface
	private struct DeferredPassthroughMeshAddition
	{
		public GameObject gameObject;
		public bool updateTransform;
	}

	// GameObjects which are in use as Insight Passthrough projection surface.
	private Dictionary<GameObject, PassthroughMeshInstance> surfaceGameObjects =
			new Dictionary<GameObject, PassthroughMeshInstance>();

	// GameObjects which are pending addition to the Insight Passthrough projection surfaces.
	private List<DeferredPassthroughMeshAddition> deferredSurfaceGameObjects =
			new List<DeferredPassthroughMeshAddition>();

	[SerializeField]
	private float textureOpacity_ = 1;

	[SerializeField]
	private bool edgeRenderingEnabled_ = false;

	[SerializeField]
	private Color edgeColor_ = new Color(1, 1, 1, 1);

	// Internal fields which store the color map values that will be relayed to the Passthrough API in the next update.
	[SerializeField]
	private ColorMapType colorMapType = ColorMapType.None;

	// Passthrough color map data gets allocated and deallocated on demand.
	private byte[] colorMapData = null;

	// Passthrough color map data gets pinned in the GC on allocation so it can be passed to the native side safely.
	// In remains pinned for its lifecycle to avoid pinning per frame and the resulting memory allocation and GC pressure.
	private GCHandle colorMapDataHandle;


	// Flag which indicates whether the style values have changed since the last update in the Passthrough API.
	// It is set to `true` initially to ensure that the local default values are applied in the Passthrough API.
	private bool styleDirty = true;

	// Keep a copy of a neutral gradient ready for comparison.
	static readonly private Gradient colorMapNeutralGradient = CreateNeutralColorMapGradient();

	// Overlay shape derived from `projectionSurfaceType`.
	private OVROverlay.OverlayShape overlayShape
	{
		get
		{
			return projectionSurfaceType == ProjectionSurfaceType.UserDefined ?
				OVROverlay.OverlayShape.SurfaceProjectedPassthrough :
				OVROverlay.OverlayShape.ReconstructionPassthrough;
		}
	}
	#endregion

	#region Unity Messages

	void Update()
	{
		SyncToOverlay();
	}

	void LateUpdate()
	{
		Debug.Assert(passthroughOverlay != null);

		// This LateUpdate() should be called after passthroughOverlay's LateUpdate() such that the layerId has
		// become available at this point. This is achieved by setting the execution order of this script to a value
		// past the default time (in .meta).

		if (passthroughOverlay.layerId <= 0)
		{
			// Layer not initialized yet
			return;
		}

		if (projectionSurfaceType == ProjectionSurfaceType.UserDefined)
		{
			// Update the poses before adding new items to avoid redundant calls.
			UpdateSurfaceGeometryTransforms();
			// Delayed additon of passthrough surface geometries.
			AddDeferredSurfaceGeometries();
		}

		// Update passthrough color map with gradient if it was changed in the inspector.
		UpdateColorMapFromControls();

		// Passthrough style updates are buffered and committed to the API atomically here.
		if (styleDirty)
		{
			OVRPlugin.InsightPassthroughStyle style;
			style.Flags = OVRPlugin.InsightPassthroughStyleFlags.HasTextureOpacityFactor |
				OVRPlugin.InsightPassthroughStyleFlags.HasEdgeColor |
				OVRPlugin.InsightPassthroughStyleFlags.HasTextureColorMap;

			style.TextureOpacityFactor = textureOpacity;

			style.EdgeColor = edgeRenderingEnabled ? edgeColor.ToColorf() : new OVRPlugin.Colorf { r = 0, g = 0, b = 0, a = 0 };

			style.TextureColorMapType = colorMapType;
			style.TextureColorMapData = IntPtr.Zero;
			style.TextureColorMapDataSize = 0;

			if (style.TextureColorMapType != ColorMapType.None && colorMapData == null)
			{
				Debug.LogError("Color map not allocated");
				style.TextureColorMapType = ColorMapType.None;
			}

			if (style.TextureColorMapType != ColorMapType.None)
			{
				if (!colorMapDataHandle.IsAllocated)
				{
					Debug.LogError("Passthrough color map enabled but data isn't pinned");
				}
				else
				{
					style.TextureColorMapData = colorMapDataHandle.AddrOfPinnedObject();
					switch (style.TextureColorMapType)
					{
						case ColorMapType.MonoToRgba:
							style.TextureColorMapDataSize = 256 * 4 * 4; // 256 * sizeof(MrColor4f)
							break;
						case ColorMapType.MonoToMono:
							style.TextureColorMapDataSize = 256;
							break;
						default:
							Debug.LogError("Unexpected texture color map type");
							break;
					}
				}
			}

			OVRPlugin.SetInsightPassthroughStyle(passthroughOverlay.layerId, style);

			styleDirty = false;
		}
	}

	void OnEnable()
	{
		Debug.Assert(auxGameObject == null);
		Debug.Assert(passthroughOverlay == null);

		// Create auxiliary GameObject which contains the OVROverlay component for the proxy layer (and possibly other
		// auxiliary layers in the future).
		auxGameObject = new GameObject("OVRPassthroughLayer auxiliary GameObject");

		// Auxiliary GameObject must be a child of the current GameObject s.t. it survives if `DontDestroyOnLoad` is
		// called on the current GameObject.
		auxGameObject.transform.parent = this.transform;

		// Add OVROverlay component for the passthrough proxy layer.
		passthroughOverlay = auxGameObject.AddComponent<OVROverlay>();
		passthroughOverlay.currentOverlayShape = overlayShape;
		SyncToOverlay();

		// Surface geometries have been moved to the deferred additions queue in OnDisable() and will be re-added
		// in LateUpdate().

		// Flag style to be re-applied in LateUpdate()
		styleDirty = true;
	}

	void OnDisable()
	{
		if (OVRManager.loadedXRDevice == OVRManager.XRDevice.Oculus)
		{
			DestroySurfaceGeometries(true);
		}

		if (auxGameObject != null) {
			Debug.Assert(passthroughOverlay != null);
			Destroy(auxGameObject);
			auxGameObject = null;
			passthroughOverlay = null;
		}
	}

	void OnDestroy()
	{
		DestroySurfaceGeometries();
	}
#endregion
}
