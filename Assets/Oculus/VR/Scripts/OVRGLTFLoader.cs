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
using System.IO;
using System;
using UnityEngine;
using OVRSimpleJSON;

using System.Threading.Tasks;

/// <summary>
/// This is a lightweight glTF model loader that is guaranteed to work with models loaded from the Oculus runtime
/// using OVRPlugin.LoadRenderModel. It is not recommended to be used as a general purpose glTF loader.
/// </summary>

public enum OVRChunkType
{
	JSON = 0x4E4F534A,
	BIN = 0x004E4942,
}

public enum OVRTextureFormat
{
	NONE,
	KTX2,
	PNG,
	JPEG,
}

public struct OVRBinaryChunk
{
	public Stream chunkStream;
	public uint chunkLength;
	public long chunkStart;
}

public struct OVRMeshData
{
	public Mesh mesh;
	public Material material;
}

public struct OVRMaterialData
{
	public Shader shader;
	public int textureId;
	public OVRTextureData texture;
}

public struct OVRGLTFScene
{
	public GameObject root;
	public List<GameObject> nodes;
}

public struct OVRTextureData
{
	public byte[] data;
	public int width;
	public int height;
	public OVRTextureFormat format;
	public TextureFormat transcodedFormat;
}

public class OVRGLTFLoader
{
	private JSONNode m_jsonData;
	private Stream m_glbStream;
	private OVRBinaryChunk m_binaryChunk;

	private List<GameObject> m_Nodes;

	private static readonly Vector3 GLTFToUnitySpace = new Vector3(-1, 1, 1);
	private static readonly Vector3 GLTFToUnityTangent = new Vector4(-1, 1, 1, -1);

	private Shader m_Shader = null;

	public OVRGLTFLoader(string fileName)
	{
		m_glbStream = File.Open(fileName, FileMode.Open);
	}

	public OVRGLTFLoader(byte[] data)
	{
		m_glbStream = new MemoryStream(data, 0, data.Length, false, true);
	}

	public OVRGLTFScene LoadGLB(bool loadMips = true)
	{
		OVRGLTFScene scene = new OVRGLTFScene();
		m_Nodes = new List<GameObject>();

		if (ValidateGLB(m_glbStream))
		{
			byte[] jsonChunkData = ReadChunk(m_glbStream, OVRChunkType.JSON);
			if (jsonChunkData != null)
			{
				string json = System.Text.Encoding.ASCII.GetString(jsonChunkData);
				m_jsonData = JSON.Parse(json);
			}

			uint binChunkLength = 0;
			bool validBinChunk = ValidateChunk(m_glbStream, OVRChunkType.BIN, out binChunkLength);
			if (validBinChunk && m_jsonData != null)
			{
				m_binaryChunk.chunkLength = binChunkLength;
				m_binaryChunk.chunkStart = m_glbStream.Position;
				m_binaryChunk.chunkStream = m_glbStream;

				if (m_Shader == null)
				{
					Debug.LogWarning("A shader was not set before loading the model. Using default mobile shader.");
					m_Shader = Shader.Find("Legacy Shaders/Diffuse");
				}

				LoadGLTF(loadMips);
			}
		}
		m_glbStream.Close();

		scene.nodes = m_Nodes;
		scene.root = m_Nodes[0];

		scene.root.transform.Rotate(Vector3.up, 180.0f);

		return scene;
	}

	public void SetModelShader(Shader shader)
	{
		m_Shader = shader;
	}

	private bool ValidateGLB(Stream glbStream)
	{
		// Read the magic entry and ensure value matches the glTF value
		int uint32Size = sizeof(uint);
		byte[] buffer = new byte[uint32Size];
		glbStream.Read(buffer, 0, uint32Size);
		uint magic = BitConverter.ToUInt32(buffer, 0);

		if (magic != 0x46546C67)
		{
			Debug.LogError("Data stream was not a valid glTF format");
			return false;
		}

		// Read glTF version
		glbStream.Read(buffer, 0, uint32Size);
		uint version = BitConverter.ToUInt32(buffer, 0);

		if (version != 2)
		{
			Debug.LogError("Only glTF 2.0 is supported");
			return false;
		}

		// Read glTF file size
		glbStream.Read(buffer, 0, uint32Size);
		uint length = BitConverter.ToUInt32(buffer, 0);
		if (length != glbStream.Length)
		{
			Debug.LogError("glTF header length does not match file length");
			return false;
		}
		return true;
	}

	private byte[] ReadChunk(Stream glbStream, OVRChunkType type)
	{
		uint chunkLength;
		if (ValidateChunk(glbStream, type, out chunkLength))
		{
			byte[] chunkBuffer = new byte[chunkLength];
			glbStream.Read(chunkBuffer, 0, (int)chunkLength);
			return chunkBuffer;
		}
		return null;
	}

	private bool ValidateChunk(Stream glbStream, OVRChunkType type, out uint chunkLength)
	{
		int uint32Size = sizeof(uint);
		byte[] buffer = new byte[uint32Size];
		glbStream.Read(buffer, 0, uint32Size);
		chunkLength = BitConverter.ToUInt32(buffer, 0);

		glbStream.Read(buffer, 0, uint32Size);
		uint chunkType = BitConverter.ToUInt32(buffer, 0);

		if (chunkType != (uint)type)
		{
			Debug.LogError("Read chunk does not match type.");
			return false;
		}
		return true;
	}

	private void LoadGLTF(bool loadMips)
	{
		if (m_jsonData == null)
		{
			Debug.LogError("m_jsonData was null");
		}

		var scenes = m_jsonData["scenes"];
		if (scenes.Count == 0)
		{
			Debug.LogError("No valid scenes in this glTF.");
		}

		// Create GameObjects for each node in the model so that they can be referenced during processing
		var nodes = m_jsonData["nodes"].AsArray;
		for (int i = 0; i < nodes.Count; i++)
		{
			var jsonNode = m_jsonData["nodes"][i];
			GameObject go = new GameObject(jsonNode["name"]);
			m_Nodes.Add(go);
		}

		// Limit loading to just the first scene in the glTF
		var mainScene = scenes[0];
		var rootNodes = mainScene["nodes"].AsArray;
		for (int i = 0; i < rootNodes.Count; i++)
		{
			int nodeId = rootNodes[i].AsInt;
			ProcessNode(m_jsonData["nodes"][nodeId], nodeId, loadMips);
		}
	}

	private void ProcessNode(JSONNode node, int nodeId, bool loadMips)
	{
		// Process the child nodes first
		var childNodes = node["children"];
		if (childNodes.Count > 0)
		{
			for (int i = 0; i < childNodes.Count; i++)
			{
				int childId = childNodes[i].AsInt;
				m_Nodes[childId].transform.SetParent(m_Nodes[nodeId].transform);
				ProcessNode(m_jsonData["nodes"][childId], childId, loadMips);
			}
		}

		string nodeName = node["name"].ToString();
		if (nodeName.Contains("batteryIndicator"))
		{
			GameObject.Destroy(m_Nodes[nodeId]);
			return;
		}

		if (node["mesh"] != null)
		{
			var meshId = node["mesh"].AsInt;
			OVRMeshData meshData = ProcessMesh(m_jsonData["meshes"][meshId], loadMips);

			if (node["skin"] != null)
			{
				var renderer = m_Nodes[nodeId].AddComponent<SkinnedMeshRenderer>();
				renderer.sharedMesh = meshData.mesh;
				renderer.sharedMaterial = meshData.material;

				var skinId = node["skin"].AsInt;
				ProcessSkin(m_jsonData["skins"][skinId], renderer);
			}
			else
			{
				var filter = m_Nodes[nodeId].AddComponent<MeshFilter>();
				filter.sharedMesh = meshData.mesh;
				var renderer = m_Nodes[nodeId].AddComponent<MeshRenderer>();
				renderer.sharedMaterial = meshData.material;
			}
		}

		var translation = node["translation"].AsArray;
		var rotation = node["rotation"].AsArray;
		var scale = node["scale"].AsArray;

		if (translation.Count > 0)
		{
			Vector3 position = new Vector3(
				translation[0] * GLTFToUnitySpace.x,
				translation[1] * GLTFToUnitySpace.y,
				translation[2] * GLTFToUnitySpace.z);
			m_Nodes[nodeId].transform.position = position;
		}

		if (rotation.Count > 0)
		{
			Vector3 rotationAxis = new Vector3(
				rotation[0] * GLTFToUnitySpace.x,
				rotation[1] * GLTFToUnitySpace.y,
				rotation[2] * GLTFToUnitySpace.z);
			rotationAxis *= -1.0f;
			m_Nodes[nodeId].transform.rotation = new Quaternion(rotationAxis.x, rotationAxis.y, rotationAxis.z, rotation[3]);
		}

		if (scale.Count > 0)
		{
			Vector3 scaleVec = new Vector3(scale[0], scale[1], scale[2]);
			m_Nodes[nodeId].transform.localScale = scaleVec;
		}
	}

	private OVRMeshData ProcessMesh(JSONNode meshNode, bool loadMips)
	{
		OVRMeshData meshData = new OVRMeshData();

		int totalVertexCount = 0;
		var primitives = meshNode["primitives"];
		int[] primitiveVertexCounts = new int[primitives.Count];
		for (int i = 0; i < primitives.Count; i++)
		{
			var jsonPrimitive = primitives[i];
			var jsonAttrbite = jsonPrimitive["attributes"]["POSITION"];
			var jsonAccessor = m_jsonData["accessors"][jsonAttrbite.AsInt];

			primitiveVertexCounts[i] = jsonAccessor["count"];
			totalVertexCount += primitiveVertexCounts[i];
		}

		int[][] indicies = new int[primitives.Count][];
		Vector3[] vertices = new Vector3[totalVertexCount];

		Vector3[] normals = null;
		if (primitives[0]["attributes"]["NORMAL"] != null)
		{
			normals = new Vector3[totalVertexCount];
		}

		Vector4[] tangents = null;
		if (primitives[0]["attributes"]["TANGENT"] != null)
		{
			tangents = new Vector4[totalVertexCount];
		}

		Vector2[] texcoords = null;
		if (primitives[0]["attributes"]["TEXCOORD_0"] != null)
		{
			texcoords = new Vector2[totalVertexCount];
		}

		Color[] colors = null;
		if (primitives[0]["attributes"]["COLOR_0"] != null)
		{
			colors = new Color[totalVertexCount];
		}

		BoneWeight[] boneWeights = null;
		if (primitives[0]["attributes"]["WEIGHTS_0"] != null)
		{
			boneWeights = new BoneWeight[totalVertexCount];
		}

		// Begin async processing of material and its texture
		OVRMaterialData matData = default(OVRMaterialData);
		Task transcodeTask = null;
		var jsonMaterial = primitives[0]["material"];
		if (jsonMaterial != null)
		{
			matData = ProcessMaterial(jsonMaterial.AsInt);
			matData.texture = ProcessTexture(matData.textureId);
			transcodeTask = Task.Run(() => { TranscodeTexture(ref matData.texture); });
		}

		int vertexOffset = 0;
		for (int i = 0; i < primitives.Count; i++)
		{
			var jsonPrimitive = primitives[i];

			int indicesAccessorId = jsonPrimitive["indices"].AsInt;
			var jsonAccessor = m_jsonData["accessors"][indicesAccessorId];
			OVRGLTFAccessor indicesReader = new OVRGLTFAccessor(jsonAccessor, m_jsonData);

			indicies[i] = new int[indicesReader.GetDataCount()];
			indicesReader.ReadAsInt(m_binaryChunk, ref indicies[i], 0);
			FlipTraingleIndices(ref indicies[i]);

			var jsonAttribute = jsonPrimitive["attributes"]["POSITION"];
			if (jsonAttribute != null)
			{
				jsonAccessor = m_jsonData["accessors"][jsonAttribute.AsInt];
				OVRGLTFAccessor dataReader = new OVRGLTFAccessor(jsonAccessor, m_jsonData);
				dataReader.ReadAsVector3(m_binaryChunk, ref vertices, vertexOffset, GLTFToUnitySpace);
			}

			jsonAttribute = jsonPrimitive["attributes"]["NORMAL"];
			if (jsonAttribute != null)
			{
				jsonAccessor = m_jsonData["accessors"][jsonAttribute.AsInt];
				OVRGLTFAccessor dataReader = new OVRGLTFAccessor(jsonAccessor, m_jsonData);
				dataReader.ReadAsVector3(m_binaryChunk, ref normals, vertexOffset, GLTFToUnitySpace);
			}

			jsonAttribute = jsonPrimitive["attributes"]["TANGENT"];
			if (jsonAttribute != null)
			{
				jsonAccessor = m_jsonData["accessors"][jsonAttribute.AsInt];
				OVRGLTFAccessor dataReader = new OVRGLTFAccessor(jsonAccessor, m_jsonData);
				dataReader.ReadAsVector4(m_binaryChunk, ref tangents, vertexOffset, GLTFToUnityTangent);
			}

			jsonAttribute = jsonPrimitive["attributes"]["TEXCOORD_0"];
			if (jsonAttribute != null)
			{
				jsonAccessor = m_jsonData["accessors"][jsonAttribute.AsInt];
				OVRGLTFAccessor dataReader = new OVRGLTFAccessor(jsonAccessor, m_jsonData);
				dataReader.ReadAsVector2(m_binaryChunk, ref texcoords, vertexOffset);
			}

			jsonAttribute = jsonPrimitive["attributes"]["COLOR_0"];
			if (jsonAttribute != null)
			{
				jsonAccessor = m_jsonData["accessors"][jsonAttribute.AsInt];
				OVRGLTFAccessor dataReader = new OVRGLTFAccessor(jsonAccessor, m_jsonData);
				dataReader.ReadAsColor(m_binaryChunk, ref colors, vertexOffset);
			}

			jsonAttribute = jsonPrimitive["attributes"]["WEIGHTS_0"];
			if (jsonAttribute != null)
			{
				jsonAccessor = m_jsonData["accessors"][jsonAttribute.AsInt];
				OVRGLTFAccessor weightReader = new OVRGLTFAccessor(jsonAccessor, m_jsonData);

				var jointAttribute = jsonPrimitive["attributes"]["JOINTS_0"];
				var jointAccessor = m_jsonData["accessors"][jointAttribute.AsInt];
				OVRGLTFAccessor jointReader = new OVRGLTFAccessor(jointAccessor, m_jsonData);

				Vector4[] weights = new Vector4[weightReader.GetDataCount()];
				Vector4[] joints = new Vector4[jointReader.GetDataCount()];

				weightReader.ReadAsBoneWeights(m_binaryChunk, ref weights, 0);
				jointReader.ReadAsVector4(m_binaryChunk, ref joints, 0, Vector4.one);

				for (int w = 0; w < weights.Length; w++)
				{
					boneWeights[vertexOffset + w].boneIndex0 = (int)joints[w].x;
					boneWeights[vertexOffset + w].boneIndex1 = (int)joints[w].y;
					boneWeights[vertexOffset + w].boneIndex2 = (int)joints[w].z;
					boneWeights[vertexOffset + w].boneIndex3 = (int)joints[w].w;

					boneWeights[vertexOffset + w].weight0 = weights[w].x;
					boneWeights[vertexOffset + w].weight1 = weights[w].y;
					boneWeights[vertexOffset + w].weight2 = weights[w].z;
					boneWeights[vertexOffset + w].weight3 = weights[w].w;
				}
			}

			vertexOffset += primitiveVertexCounts[i];
		}

		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.tangents = tangents;
		mesh.colors = colors;
		mesh.uv = texcoords;
		mesh.boneWeights = boneWeights;
		mesh.subMeshCount = primitives.Count;

		int baseVertex = 0;
		for(int i = 0; i < primitives.Count; i++)
		{
			mesh.SetIndices(indicies[i], MeshTopology.Triangles, i, false, baseVertex);
			baseVertex += primitiveVertexCounts[i];
		}

		mesh.RecalculateBounds();
		meshData.mesh = mesh;

		if (transcodeTask != null)
		{
			transcodeTask.Wait();
			meshData.material = CreateUnityMaterial(matData, loadMips);
		}
		return meshData;
	}

	private static void FlipTraingleIndices(ref int[] indices)
	{
		for (int i = 0; i < indices.Length; i += 3)
		{
			int a = indices[i];
			indices[i] = indices[i + 2];
			indices[i + 2] = a;
		}
	}

	private void ProcessSkin(JSONNode skinNode, SkinnedMeshRenderer renderer)
	{
		Matrix4x4[] inverseBindMatrices = null;
		if (skinNode["inverseBindMatrices"] != null)
		{
			int inverseBindMatricesId = skinNode["inverseBindMatrices"].AsInt;
			var jsonInverseBindMatrices = m_jsonData["accessors"][inverseBindMatricesId];

			OVRGLTFAccessor dataReader = new OVRGLTFAccessor(jsonInverseBindMatrices, m_jsonData);
			inverseBindMatrices = new Matrix4x4[dataReader.GetDataCount()];
			dataReader.ReadAsMatrix4x4(m_binaryChunk, ref inverseBindMatrices, 0, GLTFToUnitySpace);
		}

		if (skinNode["skeleton"] != null)
		{
			var skeletonRootId = skinNode["skeleton"].AsInt;
			renderer.rootBone = m_Nodes[skeletonRootId].transform;
		}

		Transform[] bones = null;
		if (skinNode["joints"] != null)
		{
			var joints = skinNode["joints"].AsArray;

			bones = new Transform[joints.Count];
			for (int i = 0; i < joints.Count; i++)
			{
				bones[i] = m_Nodes[joints[i]].transform;
			}
		}

		renderer.sharedMesh.bindposes = inverseBindMatrices;
		renderer.bones = bones;
	}

	private OVRMaterialData ProcessMaterial(int matId)
	{
		OVRMaterialData matData = new OVRMaterialData();

		var jsonMaterial = m_jsonData["materials"][matId];
		var jsonPbrDetails = jsonMaterial["pbrMetallicRoughness"];

		var jsonBaseColor = jsonPbrDetails["baseColorTexture"];
		if (jsonBaseColor != null)
		{
			int textureId = jsonBaseColor["index"].AsInt;
			matData.textureId = textureId;
		}
		else
		{
			var jsonTextrure = jsonMaterial["emissiveTexture"];
			if (jsonTextrure != null)
			{
				int textureId = jsonTextrure["index"].AsInt;
				matData.textureId = textureId;
			}
		}

		matData.shader = m_Shader;
		return matData;
	}

	private OVRTextureData ProcessTexture(int textureId)
	{
		var jsonTexture = m_jsonData["textures"][textureId];

		int imageSource = -1;
		var jsonExtensions = jsonTexture["extensions"];
		if (jsonExtensions != null)
		{
			var baisuExtension = jsonExtensions["KHR_texture_basisu"];
			if (baisuExtension != null)
			{
				imageSource = baisuExtension["source"].AsInt;
			}
		}
		else
		{
			imageSource = jsonTexture["source"].AsInt;
		}
		var jsonSource = m_jsonData["images"][imageSource];

		int sampler = jsonTexture["sampler"].AsInt;
		var jsonSampler = m_jsonData["samplers"][sampler];

		int bufferViewId = jsonSource["bufferView"].AsInt;
		var jsonBufferView = m_jsonData["bufferViews"][bufferViewId];
		OVRGLTFAccessor dataReader = new OVRGLTFAccessor(jsonBufferView, m_jsonData, true);

		OVRTextureData textureData = new OVRTextureData();
		if (jsonSource["mimeType"].Value == "image/ktx2")
		{
			textureData.data = dataReader.ReadAsKtxTexture(m_binaryChunk);
			textureData.format = OVRTextureFormat.KTX2;
		}
		else
		{
			Debug.LogWarning("Unsupported image mimeType.");
		}
		return textureData;
	}

	private void TranscodeTexture(ref OVRTextureData textureData)
	{
		if (textureData.format == OVRTextureFormat.KTX2)
		{
			OVRKtxTexture.Load(textureData.data, ref textureData);
		}
		else
		{
			Debug.LogWarning("Only KTX2 textures can be trascoded.");
		}
	}

	private Material CreateUnityMaterial(OVRMaterialData matData, bool loadMips)
	{
		Material mat = new Material(matData.shader);

		if (matData.texture.format == OVRTextureFormat.KTX2)
		{
			Texture2D texture;
			texture = new Texture2D(matData.texture.width, matData.texture.height, matData.texture.transcodedFormat, loadMips);
			texture.LoadRawTextureData(matData.texture.data);
			texture.Apply(false, true);
			mat.mainTexture = texture;
		}
		return mat;
	}
}
