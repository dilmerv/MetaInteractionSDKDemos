using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OVRRuntimeController : MonoBehaviour
{
	/// <summary>
	/// The controller that determines whether or not to enable rendering of the controller model.
	/// </summary>
	public OVRInput.Controller m_controller;

	/// <summary>
	/// Shader that will be used for the controller model
	/// </summary>
	public Shader m_controllerModelShader;

	private GameObject m_controllerObject;

	private static string leftControllerModelPath = "/model_fb/controller/left";
	private static string rightControllerModelPath = "/model_fb/controller/right";
	private string m_controllerModelPath;

	private bool m_modelSupported = false;

	private bool m_hasInputFocus = true;
	private bool m_hasInputFocusPrev = false;
	private bool m_controllerConnectedPrev = false;

	// Start is called before the first frame update
	void Start()
	{
		if (m_controller == OVRInput.Controller.LTouch)
			m_controllerModelPath = leftControllerModelPath;
		else if (m_controller == OVRInput.Controller.RTouch)
			m_controllerModelPath = rightControllerModelPath;

		m_modelSupported = IsModelSupported(m_controllerModelPath);

		if (m_modelSupported)
		{
			StartCoroutine(UpdateControllerModel());
		}

		OVRManager.InputFocusAcquired += InputFocusAquired;
		OVRManager.InputFocusLost += InputFocusLost;
	}

	// Update is called once per frame
	void Update()
	{
		bool controllerConnected = OVRInput.IsControllerConnected(m_controller);
		if (m_hasInputFocus != m_hasInputFocusPrev || controllerConnected != m_controllerConnectedPrev)
		{
			if (m_controllerObject != null)
			{
				m_controllerObject.SetActive(controllerConnected && m_hasInputFocus);
			}
			m_hasInputFocusPrev = m_hasInputFocus;
			m_controllerConnectedPrev = controllerConnected;
		}
	}

	private bool IsModelSupported(string modelPath)
	{
		string[] modelPaths = OVRPlugin.GetRenderModelPaths();
		if (modelPaths.Length == 0)
		{
			Debug.LogError("Failed to enumerate model paths from the runtime. Check that the render model feature is enabled in OVRManager.");
			return false;
		}

		for (int i = 0; i < modelPaths.Length; i++)
		{
			if (modelPaths[i].Equals(modelPath))
				return true;
		}
		Debug.LogError("Render model path not supported by this device.");
		return false;
	}
	private bool LoadControllerModel(string modelPath)
	{
		var modelProperties = new OVRPlugin.RenderModelProperties();
		if (OVRPlugin.GetRenderModelProperties(modelPath, ref modelProperties))
		{
			if (modelProperties.ModelKey != OVRPlugin.RENDER_MODEL_NULL_KEY)
			{
				byte[] modelData = OVRPlugin.LoadRenderModel(modelProperties.ModelKey);

				if (modelData != null)
				{
					OVRGLTFLoader loader = new OVRGLTFLoader(modelData);
					loader.SetModelShader(m_controllerModelShader);
					m_controllerObject = loader.LoadGLB().root;

					if (m_controllerObject != null)
					{
						m_controllerObject.transform.SetParent(transform, false);

						// Apply the OpenXR grip pose offset so runtime controller models are in the right position
						m_controllerObject.transform.parent.localPosition = new Vector3(0.0f, -0.03f, -0.04f);
						m_controllerObject.transform.parent.localRotation = Quaternion.AngleAxis(-60.0f, new Vector3(1.0f, 0.0f, 0.0f));
						return true;
					}
				}
			}
			Debug.LogError("Retrived a null model key.");
		}
		Debug.LogError("Failed to load controller model");
		return false;
	}

	private IEnumerator UpdateControllerModel()
	{
		while (true)
		{
			bool controllerConnected = OVRInput.IsControllerConnected(m_controller);
			if (m_controllerObject == null && controllerConnected)
			{
				LoadControllerModel(m_controllerModelPath);
			}
			yield return new WaitForSeconds(.5f);
		}
	}

	public void InputFocusAquired()
	{
		m_hasInputFocus = true;
	}

	public void InputFocusLost()
	{
		m_hasInputFocus = false;
	}
}
