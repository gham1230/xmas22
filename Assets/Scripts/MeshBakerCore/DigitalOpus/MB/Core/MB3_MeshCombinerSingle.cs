using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;

namespace DigitalOpus.MB.Core
{
	[Serializable]
	public class MB3_MeshCombinerSingle : MB3_MeshCombiner
	{
		public class MB3_MeshCombinerSimpleBones
		{
			private MB3_MeshCombinerSingle combiner;

			private List<MB_DynamicGameObject>[] boneIdx2dgoMap;

			private HashSet<int> boneIdxsToDelete = new HashSet<int>();

			private HashSet<BoneAndBindpose> bonesToAdd = new HashSet<BoneAndBindpose>();

			private Dictionary<BoneAndBindpose, int> boneAndBindPose2idx = new Dictionary<BoneAndBindpose, int>();

			private bool _didSetup;

			public MB3_MeshCombinerSimpleBones(MB3_MeshCombinerSingle cm)
			{
				combiner = cm;
			}

			public HashSet<BoneAndBindpose> GetBonesToAdd()
			{
				return bonesToAdd;
			}

			public int GetNumBonesToDelete()
			{
				return boneIdxsToDelete.Count;
			}

			public void BuildBoneIdx2DGOMapIfNecessary(int[] _goToDelete)
			{
				_didSetup = false;
				if (combiner.settings.renderType == MB_RenderType.skinnedMeshRenderer)
				{
					if (_goToDelete.Length != 0)
					{
						boneIdx2dgoMap = _buildBoneIdx2dgoMap();
					}
					for (int i = 0; i < combiner.bones.Length; i++)
					{
						BoneAndBindpose key = new BoneAndBindpose(combiner.bones[i], combiner.bindPoses[i]);
						boneAndBindPose2idx.Add(key, i);
					}
					_didSetup = true;
				}
			}

			public void FindBonesToDelete(MB_DynamicGameObject dgo)
			{
				for (int i = 0; i < dgo.indexesOfBonesUsed.Length; i++)
				{
					int num = dgo.indexesOfBonesUsed[i];
					List<MB_DynamicGameObject> list = boneIdx2dgoMap[num];
					if (list.Contains(dgo))
					{
						list.Remove(dgo);
						if (list.Count == 0)
						{
							boneIdxsToDelete.Add(num);
						}
					}
				}
			}

			public int GetNewBonesLength()
			{
				return combiner.bindPoses.Length + bonesToAdd.Count - boneIdxsToDelete.Count;
			}

			public bool CollectBonesToAddForDGO(MB_DynamicGameObject dgo, Renderer r, bool noExtraBonesForMeshRenderers, MeshChannelsCache meshChannelCache)
			{
				bool flag = true;
				Matrix4x4[] array = (dgo._tmpSMR_CachedBindposes = meshChannelCache.GetBindposes(r, out dgo.isSkinnedMeshWithBones));
				dgo._tmpSMR_CachedBoneWeights = meshChannelCache.GetBoneWeights(r, dgo.numVerts, dgo.isSkinnedMeshWithBones);
				Transform[] array2 = (dgo._tmpSMR_CachedBones = combiner._getBones(r, dgo.isSkinnedMeshWithBones));
				for (int i = 0; i < array2.Length; i++)
				{
					if (array2[i] == null)
					{
						UnityEngine.Debug.LogError("Source mesh r had a 'null' bone. Bones must not be null: " + r);
						flag = false;
					}
				}
				if (!flag)
				{
					return flag;
				}
				if (noExtraBonesForMeshRenderers && MB_Utility.GetRenderer(dgo.gameObject) is MeshRenderer)
				{
					bool flag2 = false;
					BoneAndBindpose boneAndBindpose = default(BoneAndBindpose);
					Transform parent = dgo.gameObject.transform.parent;
					while (parent != null)
					{
						foreach (BoneAndBindpose key in boneAndBindPose2idx.Keys)
						{
							if (key.bone == parent)
							{
								boneAndBindpose = key;
								flag2 = true;
								break;
							}
						}
						foreach (BoneAndBindpose item in bonesToAdd)
						{
							if (item.bone == parent)
							{
								boneAndBindpose = item;
								flag2 = true;
								break;
							}
						}
						if (flag2)
						{
							break;
						}
						parent = parent.parent;
					}
					if (flag2)
					{
						array2[0] = boneAndBindpose.bone;
						array[0] = boneAndBindpose.bindPose;
					}
				}
				int[] array3 = new int[array2.Length];
				for (int j = 0; j < array3.Length; j++)
				{
					array3[j] = j;
				}
				for (int k = 0; k < array2.Length; k++)
				{
					bool flag3 = false;
					int num = array3[k];
					BoneAndBindpose boneAndBindpose2 = new BoneAndBindpose(array2[num], array[num]);
					if (boneAndBindPose2idx.TryGetValue(boneAndBindpose2, out var value) && array2[num] == combiner.bones[value] && !boneIdxsToDelete.Contains(value) && array[num] == combiner.bindPoses[value])
					{
						flag3 = true;
					}
					if (!flag3 && !bonesToAdd.Contains(boneAndBindpose2))
					{
						bonesToAdd.Add(boneAndBindpose2);
					}
				}
				dgo._tmpSMRIndexesOfSourceBonesUsed = array3;
				return flag;
			}

			private List<MB_DynamicGameObject>[] _buildBoneIdx2dgoMap()
			{
				List<MB_DynamicGameObject>[] array = new List<MB_DynamicGameObject>[combiner.bones.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = new List<MB_DynamicGameObject>();
				}
				for (int j = 0; j < combiner.mbDynamicObjectsInCombinedMesh.Count; j++)
				{
					MB_DynamicGameObject mB_DynamicGameObject = combiner.mbDynamicObjectsInCombinedMesh[j];
					for (int k = 0; k < mB_DynamicGameObject.indexesOfBonesUsed.Length; k++)
					{
						array[mB_DynamicGameObject.indexesOfBonesUsed[k]].Add(mB_DynamicGameObject);
					}
				}
				return array;
			}

			public void CopyBonesWeAreKeepingToNewBonesArrayAndAdjustBWIndexes(Transform[] nbones, Matrix4x4[] nbindPoses, BoneWeight[] nboneWeights, int totalDeleteVerts)
			{
				if (boneIdxsToDelete.Count > 0)
				{
					int[] array = new int[boneIdxsToDelete.Count];
					boneIdxsToDelete.CopyTo(array);
					Array.Sort(array);
					int[] array2 = new int[combiner.bones.Length];
					int num = 0;
					int num2 = 0;
					for (int i = 0; i < combiner.bones.Length; i++)
					{
						if (num2 < array.Length && array[num2] == i)
						{
							num2++;
							array2[i] = -1;
							continue;
						}
						array2[i] = num;
						nbones[num] = combiner.bones[i];
						nbindPoses[num] = combiner.bindPoses[i];
						num++;
					}
					int num3 = combiner.boneWeights.Length - totalDeleteVerts;
					for (int j = 0; j < num3; j++)
					{
						BoneWeight boneWeight = nboneWeights[j];
						boneWeight.boneIndex0 = array2[boneWeight.boneIndex0];
						boneWeight.boneIndex1 = array2[boneWeight.boneIndex1];
						boneWeight.boneIndex2 = array2[boneWeight.boneIndex2];
						boneWeight.boneIndex3 = array2[boneWeight.boneIndex3];
						nboneWeights[j] = boneWeight;
					}
					for (int k = 0; k < combiner.mbDynamicObjectsInCombinedMesh.Count; k++)
					{
						MB_DynamicGameObject mB_DynamicGameObject = combiner.mbDynamicObjectsInCombinedMesh[k];
						for (int l = 0; l < mB_DynamicGameObject.indexesOfBonesUsed.Length; l++)
						{
							mB_DynamicGameObject.indexesOfBonesUsed[l] = array2[mB_DynamicGameObject.indexesOfBonesUsed[l]];
						}
					}
				}
				else
				{
					Array.Copy(combiner.bones, nbones, combiner.bones.Length);
					Array.Copy(combiner.bindPoses, nbindPoses, combiner.bindPoses.Length);
				}
			}

			public static void AddBonesToNewBonesArrayAndAdjustBWIndexes(MB3_MeshCombinerSingle combiner, MB_DynamicGameObject dgo, Renderer r, int vertsIdx, Transform[] nbones, BoneWeight[] nboneWeights, MeshChannelsCache meshChannelCache)
			{
				Transform[] tmpSMR_CachedBones = dgo._tmpSMR_CachedBones;
				Matrix4x4[] tmpSMR_CachedBindposes = dgo._tmpSMR_CachedBindposes;
				BoneWeight[] tmpSMR_CachedBoneWeights = dgo._tmpSMR_CachedBoneWeights;
				int[] array = new int[tmpSMR_CachedBones.Length];
				for (int i = 0; i < dgo._tmpSMRIndexesOfSourceBonesUsed.Length; i++)
				{
					int num = dgo._tmpSMRIndexesOfSourceBonesUsed[i];
					for (int j = 0; j < nbones.Length; j++)
					{
						if (tmpSMR_CachedBones[num] == nbones[j] && tmpSMR_CachedBindposes[num] == combiner.bindPoses[j])
						{
							array[num] = j;
							break;
						}
					}
				}
				for (int k = 0; k < tmpSMR_CachedBoneWeights.Length; k++)
				{
					int num2 = vertsIdx + k;
					nboneWeights[num2].boneIndex0 = array[tmpSMR_CachedBoneWeights[k].boneIndex0];
					nboneWeights[num2].boneIndex1 = array[tmpSMR_CachedBoneWeights[k].boneIndex1];
					nboneWeights[num2].boneIndex2 = array[tmpSMR_CachedBoneWeights[k].boneIndex2];
					nboneWeights[num2].boneIndex3 = array[tmpSMR_CachedBoneWeights[k].boneIndex3];
					nboneWeights[num2].weight0 = tmpSMR_CachedBoneWeights[k].weight0;
					nboneWeights[num2].weight1 = tmpSMR_CachedBoneWeights[k].weight1;
					nboneWeights[num2].weight2 = tmpSMR_CachedBoneWeights[k].weight2;
					nboneWeights[num2].weight3 = tmpSMR_CachedBoneWeights[k].weight3;
				}
				for (int l = 0; l < dgo._tmpSMRIndexesOfSourceBonesUsed.Length; l++)
				{
					dgo._tmpSMRIndexesOfSourceBonesUsed[l] = array[dgo._tmpSMRIndexesOfSourceBonesUsed[l]];
				}
				dgo.indexesOfBonesUsed = dgo._tmpSMRIndexesOfSourceBonesUsed;
				dgo._tmpSMRIndexesOfSourceBonesUsed = null;
				dgo._tmpSMR_CachedBones = null;
				dgo._tmpSMR_CachedBindposes = null;
				dgo._tmpSMR_CachedBoneWeights = null;
			}

			internal void CopyVertsNormsTansToBuffers(MB_DynamicGameObject dgo, MB_IMeshBakerSettings settings, int vertsIdx, Vector3[] nnorms, Vector4[] ntangs, Vector3[] nverts, Vector3[] normals, Vector4[] tangents, Vector3[] verts)
			{
				bool flag = dgo.gameObject.GetComponent<Renderer>() is MeshRenderer;
				if (settings.smrNoExtraBonesWhenCombiningMeshRenderers && flag && dgo._tmpSMR_CachedBones[0] != dgo.gameObject.transform)
				{
					Matrix4x4 matrix4x = dgo._tmpSMR_CachedBindposes[0].inverse * dgo._tmpSMR_CachedBones[0].worldToLocalMatrix * dgo.gameObject.transform.localToWorldMatrix;
					Matrix4x4 matrix4x2 = matrix4x;
					float num2 = (matrix4x2[2, 3] = 0f);
					float value = (matrix4x2[1, 3] = num2);
					matrix4x2[0, 3] = value;
					matrix4x2 = matrix4x2.inverse.transpose;
					for (int i = 0; i < nverts.Length; i++)
					{
						int num4 = vertsIdx + i;
						verts[vertsIdx + i] = matrix4x.MultiplyPoint3x4(nverts[i]);
						if (settings.doNorm)
						{
							normals[num4] = matrix4x2.MultiplyPoint3x4(nnorms[i]).normalized;
						}
						if (settings.doTan)
						{
							float w = ntangs[i].w;
							tangents[num4] = matrix4x2.MultiplyPoint3x4(ntangs[i]).normalized;
							tangents[num4].w = w;
						}
					}
				}
				else
				{
					if (settings.doNorm)
					{
						nnorms.CopyTo(normals, vertsIdx);
					}
					if (settings.doTan)
					{
						ntangs.CopyTo(tangents, vertsIdx);
					}
					nverts.CopyTo(verts, vertsIdx);
				}
			}
		}

		public enum MeshCreationConditions
		{
			NoMesh = 0,
			CreatedInEditor = 1,
			CreatedAtRuntime = 2,
			AssignedByUser = 3
		}

		[Serializable]
		public class SerializableIntArray
		{
			public int[] data;

			public SerializableIntArray()
			{
			}

			public SerializableIntArray(int len)
			{
				data = new int[len];
			}
		}

		[Serializable]
		public class MB_DynamicGameObject : IComparable<MB_DynamicGameObject>
		{
			public int instanceID;

			public GameObject gameObject;

			public string name;

			public int vertIdx;

			public int blendShapeIdx;

			public int numVerts;

			public int numBlendShapes;

			public bool isSkinnedMeshWithBones;

			public int[] indexesOfBonesUsed = new int[0];

			public int lightmapIndex = -1;

			public Vector4 lightmapTilingOffset = new Vector4(1f, 1f, 0f, 0f);

			public Vector3 meshSize = Vector3.one;

			public bool show = true;

			public bool invertTriangles;

			public int[] submeshTriIdxs;

			public int[] submeshNumTris;

			public int[] targetSubmeshIdxs;

			public Rect[] uvRects;

			public Rect[] encapsulatingRect;

			public Rect[] sourceMaterialTiling;

			public Rect[] obUVRects;

			public int[] textureArraySliceIdx;

			public Material[] sourceSharedMaterials;

			public bool _beingDeleted;

			public int _triangleIdxAdjustment;

			[NonSerialized]
			public SerializableIntArray[] _tmpSubmeshTris;

			[NonSerialized]
			public Transform[] _tmpSMR_CachedBones;

			[NonSerialized]
			public Matrix4x4[] _tmpSMR_CachedBindposes;

			[NonSerialized]
			public BoneWeight[] _tmpSMR_CachedBoneWeights;

			[NonSerialized]
			public int[] _tmpSMRIndexesOfSourceBonesUsed;

			public int CompareTo(MB_DynamicGameObject b)
			{
				return vertIdx - b.vertIdx;
			}
		}

		public class MeshChannels
		{
			public Vector3[] vertices;

			public Vector3[] normals;

			public Vector4[] tangents;

			public Vector2[] uv0raw;

			public Vector2[] uv0modified;

			public Vector2[] uv2raw;

			public Vector2[] uv2modified;

			public Vector2[] uv3;

			public Vector2[] uv4;

			public Vector2[] uv5;

			public Vector2[] uv6;

			public Vector2[] uv7;

			public Vector2[] uv8;

			public Color[] colors;

			public BoneWeight[] boneWeights;

			public Matrix4x4[] bindPoses;

			public int[] triangles;

			public MBBlendShape[] blendShapes;
		}

		[Serializable]
		public class MBBlendShapeFrame
		{
			public float frameWeight;

			public Vector3[] vertices;

			public Vector3[] normals;

			public Vector3[] tangents;
		}

		[Serializable]
		public class MBBlendShape
		{
			public int gameObjectID;

			public GameObject gameObject;

			public string name;

			public int indexInSource;

			public MBBlendShapeFrame[] frames;
		}

		public class MeshChannelsCache
		{
			private MB2_LogLevel LOG_LEVEL;

			private MB2_LightmapOptions lightmapOption;

			protected Dictionary<int, MeshChannels> meshID2MeshChannels = new Dictionary<int, MeshChannels>();

			private Vector2 _HALF_UV = new Vector2(0.5f, 0.5f);

			public MeshChannelsCache(MB2_LogLevel ll, MB2_LightmapOptions lo)
			{
				LOG_LEVEL = ll;
				lightmapOption = lo;
			}

			internal Vector3[] GetVertices(Mesh m)
			{
				if (!meshID2MeshChannels.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(m.GetInstanceID(), value);
				}
				if (value.vertices == null)
				{
					value.vertices = m.vertices;
				}
				return value.vertices;
			}

			internal Vector3[] GetNormals(Mesh m)
			{
				if (!meshID2MeshChannels.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(m.GetInstanceID(), value);
				}
				if (value.normals == null)
				{
					value.normals = _getMeshNormals(m);
				}
				return value.normals;
			}

			internal Vector4[] GetTangents(Mesh m)
			{
				if (!meshID2MeshChannels.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(m.GetInstanceID(), value);
				}
				if (value.tangents == null)
				{
					value.tangents = _getMeshTangents(m);
				}
				return value.tangents;
			}

			internal Vector2[] GetUv0Raw(Mesh m)
			{
				if (!meshID2MeshChannels.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(m.GetInstanceID(), value);
				}
				if (value.uv0raw == null)
				{
					value.uv0raw = _getMeshUVs(m);
				}
				return value.uv0raw;
			}

			internal Vector2[] GetUv0Modified(Mesh m)
			{
				if (!meshID2MeshChannels.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(m.GetInstanceID(), value);
				}
				if (value.uv0modified == null)
				{
					value.uv0modified = null;
				}
				return value.uv0modified;
			}

			internal Vector2[] GetUv2Modified(Mesh m)
			{
				if (!meshID2MeshChannels.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(m.GetInstanceID(), value);
				}
				if (value.uv2modified == null && value.uv2raw == null)
				{
					value.uv2raw = _getMeshUV2s(m, ref value.uv2modified);
				}
				return value.uv2modified;
			}

			internal Vector2[] GetUVChannel(int channel, Mesh m)
			{
				if (!meshID2MeshChannels.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(m.GetInstanceID(), value);
				}
				switch (channel)
				{
				case 0:
					if (value.uv0raw == null)
					{
						value.uv0raw = GetUv0Raw(m);
					}
					return value.uv0raw;
				case 2:
					if (value.uv2raw == null)
					{
						value.uv2raw = _getMeshUV2s(m, ref value.uv2modified);
					}
					return value.uv2raw;
				case 3:
					if (value.uv3 == null)
					{
						value.uv3 = MBVersion.GetMeshChannel(channel, m, LOG_LEVEL);
					}
					return value.uv3;
				case 4:
					if (value.uv4 == null)
					{
						value.uv4 = MBVersion.GetMeshChannel(channel, m, LOG_LEVEL);
					}
					return value.uv4;
				case 5:
					if (value.uv5 == null)
					{
						value.uv5 = MBVersion.GetMeshChannel(channel, m, LOG_LEVEL);
					}
					return value.uv5;
				case 6:
					if (value.uv6 == null)
					{
						value.uv6 = MBVersion.GetMeshChannel(channel, m, LOG_LEVEL);
					}
					return value.uv6;
				case 7:
					if (value.uv7 == null)
					{
						value.uv7 = MBVersion.GetMeshChannel(channel, m, LOG_LEVEL);
					}
					return value.uv7;
				case 8:
					if (value.uv8 == null)
					{
						value.uv8 = MBVersion.GetMeshChannel(channel, m, LOG_LEVEL);
					}
					return value.uv8;
				default:
					UnityEngine.Debug.LogError("Error mesh channel " + channel + " not supported");
					return null;
				}
			}

			internal Color[] GetColors(Mesh m)
			{
				if (!meshID2MeshChannels.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(m.GetInstanceID(), value);
				}
				if (value.colors == null)
				{
					value.colors = _getMeshColors(m);
				}
				return value.colors;
			}

			internal Matrix4x4[] GetBindposes(Renderer r, out bool isSkinnedMeshWithBones)
			{
				Mesh mesh = MB_Utility.GetMesh(r.gameObject);
				if (!meshID2MeshChannels.TryGetValue(mesh.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(mesh.GetInstanceID(), value);
				}
				if (value.bindPoses == null)
				{
					value.bindPoses = _getBindPoses(r, out isSkinnedMeshWithBones);
				}
				else if (r is SkinnedMeshRenderer && value.bindPoses.Length != 0)
				{
					isSkinnedMeshWithBones = true;
				}
				else
				{
					isSkinnedMeshWithBones = false;
					_ = r is SkinnedMeshRenderer;
				}
				return value.bindPoses;
			}

			internal BoneWeight[] GetBoneWeights(Renderer r, int numVertsInMeshBeingAdded, bool isSkinnedMeshWithBones)
			{
				Mesh mesh = MB_Utility.GetMesh(r.gameObject);
				if (!meshID2MeshChannels.TryGetValue(mesh.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(mesh.GetInstanceID(), value);
				}
				if (value.boneWeights == null)
				{
					value.boneWeights = _getBoneWeights(r, numVertsInMeshBeingAdded, isSkinnedMeshWithBones);
				}
				return value.boneWeights;
			}

			internal int[] GetTriangles(Mesh m)
			{
				if (!meshID2MeshChannels.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(m.GetInstanceID(), value);
				}
				if (value.triangles == null)
				{
					value.triangles = m.triangles;
				}
				return value.triangles;
			}

			internal MBBlendShape[] GetBlendShapes(Mesh m, int gameObjectID, GameObject gameObject)
			{
				return MB3_MeshCombinerSingle.GetBlendShapes(m, gameObjectID, gameObject, meshID2MeshChannels);
			}

			private Color[] _getMeshColors(Mesh m)
			{
				Color[] array = m.colors;
				if (array.Length == 0)
				{
					if (LOG_LEVEL >= MB2_LogLevel.debug)
					{
						MB2_Log.LogDebug(string.Concat("Mesh ", m, " has no colors. Generating"));
					}
					if (LOG_LEVEL >= MB2_LogLevel.warn)
					{
						UnityEngine.Debug.LogWarning(string.Concat("Mesh ", m, " didn't have colors. Generating an array of white colors"));
					}
					array = new Color[m.vertexCount];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = Color.white;
					}
				}
				return array;
			}

			private Vector3[] _getMeshNormals(Mesh m)
			{
				Vector3[] normals = m.normals;
				if (normals.Length == 0)
				{
					if (LOG_LEVEL >= MB2_LogLevel.debug)
					{
						MB2_Log.LogDebug(string.Concat("Mesh ", m, " has no normals. Generating"));
					}
					if (LOG_LEVEL >= MB2_LogLevel.warn)
					{
						UnityEngine.Debug.LogWarning(string.Concat("Mesh ", m, " didn't have normals. Generating normals."));
					}
					Mesh mesh = UnityEngine.Object.Instantiate(m);
					mesh.RecalculateNormals();
					normals = mesh.normals;
					MB_Utility.Destroy(mesh);
				}
				return normals;
			}

			private Vector4[] _getMeshTangents(Mesh m)
			{
				Vector4[] array = m.tangents;
				if (array.Length == 0)
				{
					if (LOG_LEVEL >= MB2_LogLevel.debug)
					{
						MB2_Log.LogDebug(string.Concat("Mesh ", m, " has no tangents. Generating"));
					}
					if (LOG_LEVEL >= MB2_LogLevel.warn)
					{
						UnityEngine.Debug.LogWarning(string.Concat("Mesh ", m, " didn't have tangents. Generating tangents."));
					}
					Vector3[] vertices = m.vertices;
					Vector2[] uv0Raw = GetUv0Raw(m);
					Vector3[] normals = _getMeshNormals(m);
					array = new Vector4[m.vertexCount];
					for (int i = 0; i < m.subMeshCount; i++)
					{
						int[] triangles = m.GetTriangles(i);
						_generateTangents(triangles, vertices, uv0Raw, normals, array);
					}
				}
				return array;
			}

			private Vector2[] _getMeshUVs(Mesh m)
			{
				Vector2[] array = m.uv;
				if (array.Length == 0)
				{
					array = new Vector2[m.vertexCount];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = _HALF_UV;
					}
				}
				return array;
			}

			private Vector2[] _getMeshUV2s(Mesh m, ref Vector2[] uv2modified)
			{
				Vector2[] uv = m.uv2;
				if (uv.Length == 0)
				{
					uv2modified = new Vector2[m.vertexCount];
					for (int i = 0; i < uv2modified.Length; i++)
					{
						uv2modified[i] = _HALF_UV;
					}
				}
				return uv;
			}

			public static Matrix4x4[] _getBindPoses(Renderer r, out bool isSkinnedMeshWithBones)
			{
				Matrix4x4[] array = null;
				isSkinnedMeshWithBones = r is SkinnedMeshRenderer;
				if (r is SkinnedMeshRenderer)
				{
					array = ((SkinnedMeshRenderer)r).sharedMesh.bindposes;
					if (array.Length == 0)
					{
						if (MB_Utility.GetMesh(r.gameObject).blendShapeCount > 0)
						{
							isSkinnedMeshWithBones = false;
						}
						else
						{
							UnityEngine.Debug.LogError(string.Concat("Skinned mesh ", r, " had no bindposes AND no blend shapes"));
						}
					}
				}
				if (r is MeshRenderer || (r is SkinnedMeshRenderer && !isSkinnedMeshWithBones))
				{
					Matrix4x4 identity = Matrix4x4.identity;
					array = new Matrix4x4[1] { identity };
				}
				if (array == null)
				{
					UnityEngine.Debug.LogError("Could not _getBindPoses. Object does not have a renderer");
					return null;
				}
				return array;
			}

			public static BoneWeight[] _getBoneWeights(Renderer r, int numVertsInMeshBeingAdded, bool isSkinnedMeshWithBones)
			{
				if (isSkinnedMeshWithBones)
				{
					return ((SkinnedMeshRenderer)r).sharedMesh.boneWeights;
				}
				if (r is MeshRenderer || (r is SkinnedMeshRenderer && !isSkinnedMeshWithBones))
				{
					BoneWeight boneWeight = default(BoneWeight);
					int num2 = (boneWeight.boneIndex3 = 0);
					int num4 = (boneWeight.boneIndex2 = num2);
					int boneIndex = (boneWeight.boneIndex1 = num4);
					boneWeight.boneIndex0 = boneIndex;
					boneWeight.weight0 = 1f;
					float num7 = (boneWeight.weight3 = 0f);
					float weight = (boneWeight.weight2 = num7);
					boneWeight.weight1 = weight;
					BoneWeight[] array = new BoneWeight[numVertsInMeshBeingAdded];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = boneWeight;
					}
					return array;
				}
				UnityEngine.Debug.LogError("Could not _getBoneWeights. Object does not have a renderer");
				return null;
			}

			private void _generateTangents(int[] triangles, Vector3[] verts, Vector2[] uvs, Vector3[] normals, Vector4[] outTangents)
			{
				int num = triangles.Length;
				int num2 = verts.Length;
				Vector3[] array = new Vector3[num2];
				Vector3[] array2 = new Vector3[num2];
				for (int i = 0; i < num; i += 3)
				{
					int num3 = triangles[i];
					int num4 = triangles[i + 1];
					int num5 = triangles[i + 2];
					Vector3 vector = verts[num3];
					Vector3 vector2 = verts[num4];
					Vector3 vector3 = verts[num5];
					Vector2 vector4 = uvs[num3];
					Vector2 vector5 = uvs[num4];
					Vector2 vector6 = uvs[num5];
					float num6 = vector2.x - vector.x;
					float num7 = vector3.x - vector.x;
					float num8 = vector2.y - vector.y;
					float num9 = vector3.y - vector.y;
					float num10 = vector2.z - vector.z;
					float num11 = vector3.z - vector.z;
					float num12 = vector5.x - vector4.x;
					float num13 = vector6.x - vector4.x;
					float num14 = vector5.y - vector4.y;
					float num15 = vector6.y - vector4.y;
					float num16 = num12 * num15 - num13 * num14;
					if (num16 == 0f)
					{
						UnityEngine.Debug.LogError("Could not compute tangents. All UVs need to form a valid triangles in UV space. If any UV triangles are collapsed, tangents cannot be generated.");
						return;
					}
					float num17 = 1f / num16;
					Vector3 vector7 = new Vector3((num15 * num6 - num14 * num7) * num17, (num15 * num8 - num14 * num9) * num17, (num15 * num10 - num14 * num11) * num17);
					Vector3 vector8 = new Vector3((num12 * num7 - num13 * num6) * num17, (num12 * num9 - num13 * num8) * num17, (num12 * num11 - num13 * num10) * num17);
					array[num3] += vector7;
					array[num4] += vector7;
					array[num5] += vector7;
					array2[num3] += vector8;
					array2[num4] += vector8;
					array2[num5] += vector8;
				}
				for (int j = 0; j < num2; j++)
				{
					Vector3 vector9 = normals[j];
					Vector3 vector10 = array[j];
					Vector3 normalized = (vector10 - vector9 * Vector3.Dot(vector9, vector10)).normalized;
					outTangents[j] = new Vector4(normalized.x, normalized.y, normalized.z);
					outTangents[j].w = ((Vector3.Dot(Vector3.Cross(vector9, vector10), array2[j]) < 0f) ? (-1f) : 1f);
				}
			}
		}

		public struct BoneAndBindpose
		{
			public Transform bone;

			public Matrix4x4 bindPose;

			public BoneAndBindpose(Transform t, Matrix4x4 bp)
			{
				bone = t;
				bindPose = bp;
			}

			public override bool Equals(object obj)
			{
				if (obj is BoneAndBindpose && bone == ((BoneAndBindpose)obj).bone && bindPose == ((BoneAndBindpose)obj).bindPose)
				{
					return true;
				}
				return false;
			}

			public override int GetHashCode()
			{
				return (bone.GetInstanceID() % int.MaxValue) ^ (int)bindPose[0, 0];
			}
		}

		public class UVAdjuster_Atlas
		{
			private MB2_TextureBakeResults textureBakeResults;

			private MB2_LogLevel LOG_LEVEL;

			private int[] numTimesMatAppearsInAtlas;

			private MB_MaterialAndUVRect[] matsAndSrcUVRect;

			private bool compareNamesWhenComparingMaterials;

			public UVAdjuster_Atlas(MB2_TextureBakeResults tbr, MB2_LogLevel ll)
			{
				textureBakeResults = tbr;
				LOG_LEVEL = ll;
				matsAndSrcUVRect = tbr.materialsAndUVRects;
				compareNamesWhenComparingMaterials = false;
				if (MBVersion.IsUsingAddressables() && Application.isPlaying)
				{
					compareNamesWhenComparingMaterials = true;
				}
				else
				{
					compareNamesWhenComparingMaterials = false;
				}
				numTimesMatAppearsInAtlas = new int[matsAndSrcUVRect.Length];
				for (int i = 0; i < matsAndSrcUVRect.Length; i++)
				{
					if (numTimesMatAppearsInAtlas[i] > 1)
					{
						continue;
					}
					int num = 1;
					for (int j = i + 1; j < matsAndSrcUVRect.Length; j++)
					{
						if (matsAndSrcUVRect[i].material == matsAndSrcUVRect[j].material)
						{
							num++;
						}
					}
					numTimesMatAppearsInAtlas[i] = num;
					if (num <= 1)
					{
						continue;
					}
					for (int k = i + 1; k < matsAndSrcUVRect.Length; k++)
					{
						if (matsAndSrcUVRect[i].material == matsAndSrcUVRect[k].material)
						{
							numTimesMatAppearsInAtlas[k] = num;
						}
					}
				}
			}

			public bool MapSharedMaterialsToAtlasRects(Material[] sharedMaterials, bool checkTargetSubmeshIdxsFromPreviousBake, Mesh m, MeshChannelsCache meshChannelsCache, Dictionary<int, MB_Utility.MeshAnalysisResult[]> meshAnalysisResultsCache, OrderedDictionary sourceMats2submeshIdx_map, GameObject go, MB_DynamicGameObject dgoOut)
			{
				MB_TextureTilingTreatment[] array = new MB_TextureTilingTreatment[sharedMaterials.Length];
				Rect[] array2 = new Rect[sharedMaterials.Length];
				Rect[] array3 = new Rect[sharedMaterials.Length];
				Rect[] array4 = new Rect[sharedMaterials.Length];
				int[] array5 = new int[sharedMaterials.Length];
				string errorMsg = "";
				for (int i = 0; i < sharedMaterials.Length; i++)
				{
					object obj = null;
					foreach (DictionaryEntry item in sourceMats2submeshIdx_map)
					{
						if (IsSameMaterialInTextureBakeResult(sharedMaterials[i], (Material)item.Key))
						{
							obj = (int)item.Value;
						}
					}
					if (obj == null)
					{
						UnityEngine.Debug.LogError(string.Concat("Source object ", go.name, " used a material ", sharedMaterials[i], " that was not in the baked materials."));
						return false;
					}
					int num = (int)obj;
					if (checkTargetSubmeshIdxsFromPreviousBake && num != dgoOut.targetSubmeshIdxs[i])
					{
						UnityEngine.Debug.LogError($"Update failed for object {go.name}. Material {sharedMaterials[i]} is mapped to a different submesh in the combined mesh than the previous material. This is not supported. Try using AddDelete.");
						return false;
					}
					if (!TryMapMaterialToUVRect(sharedMaterials[i], m, i, num, meshChannelsCache, meshAnalysisResultsCache, out array[i], out array2[i], out array3[i], out array4[i], out array5[i], ref errorMsg, LOG_LEVEL))
					{
						UnityEngine.Debug.LogError(errorMsg);
						return false;
					}
				}
				dgoOut.uvRects = array2;
				dgoOut.encapsulatingRect = array3;
				dgoOut.sourceMaterialTiling = array4;
				dgoOut.textureArraySliceIdx = array5;
				return true;
			}

			public bool IsSameMaterialInTextureBakeResult(Material a, Material b)
			{
				if (a == b)
				{
					return true;
				}
				if (compareNamesWhenComparingMaterials && a != null && b != null && a.name.Equals(b.name))
				{
					return true;
				}
				return false;
			}

			public void _copyAndAdjustUVsFromMesh(MB2_TextureBakeResults tbr, MB_DynamicGameObject dgo, Mesh mesh, int uvChannel, int vertsIdx, Vector2[] uvsOut, float[] uvsSliceIdx, MeshChannelsCache meshChannelsCache)
			{
				Vector2[] uVChannel = meshChannelsCache.GetUVChannel(uvChannel, mesh);
				int[] array = new int[uVChannel.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = -1;
				}
				bool flag = false;
				bool flag2 = tbr.resultType == MB2_TextureBakeResults.ResultType.textureArray;
				for (int j = 0; j < dgo.targetSubmeshIdxs.Length; j++)
				{
					int[] array2 = ((dgo._tmpSubmeshTris == null) ? mesh.GetTriangles(j) : dgo._tmpSubmeshTris[j].data);
					float num = dgo.textureArraySliceIdx[j];
					int idxInSrcMats = dgo.targetSubmeshIdxs[j];
					if (LOG_LEVEL >= MB2_LogLevel.trace)
					{
						UnityEngine.Debug.Log($"Build UV transform for mesh {dgo.name} submesh {j} encapsulatingRect {dgo.encapsulatingRect[j]}");
					}
					Rect rect = MB3_TextureCombinerMerging.BuildTransformMeshUV2AtlasRect(textureBakeResults.GetConsiderMeshUVs(idxInSrcMats, dgo.sourceSharedMaterials[j]), dgo.uvRects[j], (dgo.obUVRects == null || dgo.obUVRects.Length == 0) ? new Rect(0f, 0f, 1f, 1f) : dgo.obUVRects[j], dgo.sourceMaterialTiling[j], dgo.encapsulatingRect[j]);
					foreach (int num2 in array2)
					{
						if (array[num2] == -1)
						{
							array[num2] = j;
							Vector2 vector = uVChannel[num2];
							vector.x = rect.x + vector.x * rect.width;
							vector.y = rect.y + vector.y * rect.height;
							int num3 = vertsIdx + num2;
							uvsOut[num3] = vector;
							if (flag2)
							{
								uvsSliceIdx[num3] = num;
							}
						}
						if (array[num2] != j)
						{
							flag = true;
						}
					}
				}
				if (flag && LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning(dgo.name + "has submeshes which share verticies. Adjusted uvs may not map correctly in combined atlas.");
				}
				if (LOG_LEVEL >= MB2_LogLevel.trace)
				{
					UnityEngine.Debug.Log($"_copyAndAdjustUVsFromMesh copied {uVChannel.Length} verts");
				}
			}

			public bool TryMapMaterialToUVRect(Material mat, Mesh m, int submeshIdx, int idxInResultMats, MeshChannelsCache meshChannelCache, Dictionary<int, MB_Utility.MeshAnalysisResult[]> meshAnalysisCache, out MB_TextureTilingTreatment tilingTreatment, out Rect rectInAtlas, out Rect encapsulatingRectOut, out Rect sourceMaterialTilingOut, out int sliceIdx, ref string errorMsg, MB2_LogLevel logLevel)
			{
				if (textureBakeResults.version < MB2_TextureBakeResults.VERSION)
				{
					textureBakeResults.UpgradeToCurrentVersion(textureBakeResults);
				}
				tilingTreatment = MB_TextureTilingTreatment.unknown;
				if (textureBakeResults.materialsAndUVRects.Length == 0)
				{
					errorMsg = "The 'Texture Bake Result' needs to be re-baked to be compatible with this version of Mesh Baker. Please re-bake using the MB3_TextureBaker.";
					rectInAtlas = default(Rect);
					encapsulatingRectOut = default(Rect);
					sourceMaterialTilingOut = default(Rect);
					sliceIdx = -1;
					return false;
				}
				if (mat == null)
				{
					rectInAtlas = default(Rect);
					encapsulatingRectOut = default(Rect);
					sourceMaterialTilingOut = default(Rect);
					sliceIdx = -1;
					errorMsg = $"Mesh {m.name} Had no material on submesh {submeshIdx} cannot map to a material in the atlas";
					return false;
				}
				if (submeshIdx >= m.subMeshCount)
				{
					errorMsg = "Submesh index is greater than the number of submeshes";
					rectInAtlas = default(Rect);
					encapsulatingRectOut = default(Rect);
					sourceMaterialTilingOut = default(Rect);
					sliceIdx = -1;
					return false;
				}
				int num = -1;
				for (int i = 0; i < matsAndSrcUVRect.Length; i++)
				{
					if (IsSameMaterialInTextureBakeResult(mat, matsAndSrcUVRect[i].material))
					{
						num = i;
						break;
					}
				}
				if (num == -1)
				{
					rectInAtlas = default(Rect);
					encapsulatingRectOut = default(Rect);
					sourceMaterialTilingOut = default(Rect);
					sliceIdx = -1;
					errorMsg = $"Material {mat.name} could not be found in the Texture Bake Result";
					return false;
				}
				if (!textureBakeResults.GetConsiderMeshUVs(idxInResultMats, mat))
				{
					if (numTimesMatAppearsInAtlas[num] != 1)
					{
						UnityEngine.Debug.LogError(string.Concat("There is a problem with this TextureBakeResults. FixOutOfBoundsUVs is false and a material appears more than once: ", matsAndSrcUVRect[num].material, " appears: ", numTimesMatAppearsInAtlas[num]));
					}
					MB_MaterialAndUVRect mB_MaterialAndUVRect = matsAndSrcUVRect[num];
					rectInAtlas = mB_MaterialAndUVRect.atlasRect;
					tilingTreatment = mB_MaterialAndUVRect.tilingTreatment;
					encapsulatingRectOut = mB_MaterialAndUVRect.GetEncapsulatingRect();
					sourceMaterialTilingOut = mB_MaterialAndUVRect.GetMaterialTilingRect();
					sliceIdx = mB_MaterialAndUVRect.textureArraySliceIdx;
					return true;
				}
				if (!meshAnalysisCache.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MB_Utility.MeshAnalysisResult[m.subMeshCount];
					for (int j = 0; j < m.subMeshCount; j++)
					{
						MB_Utility.hasOutOfBoundsUVs(meshChannelCache.GetUv0Raw(m), m, ref value[j], j);
					}
					meshAnalysisCache.Add(m.GetInstanceID(), value);
				}
				bool flag = false;
				Rect rect = new Rect(0f, 0f, 0f, 0f);
				Rect rect2 = new Rect(0f, 0f, 0f, 0f);
				if (logLevel >= MB2_LogLevel.trace)
				{
					UnityEngine.Debug.Log(string.Format("Trying to find a rectangle in atlas capable of holding tiled sampling rect for mesh {0} using material {1} meshUVrect={2}", m, mat, value[submeshIdx].uvRect.ToString("f5")));
				}
				for (int k = num; k < matsAndSrcUVRect.Length; k++)
				{
					MB_MaterialAndUVRect mB_MaterialAndUVRect2 = matsAndSrcUVRect[k];
					if (!IsSameMaterialInTextureBakeResult(mat, mB_MaterialAndUVRect2.material))
					{
						continue;
					}
					if (mB_MaterialAndUVRect2.allPropsUseSameTiling)
					{
						rect = mB_MaterialAndUVRect2.allPropsUseSameTiling_samplingEncapsulatinRect;
						rect2 = mB_MaterialAndUVRect2.allPropsUseSameTiling_sourceMaterialTiling;
					}
					else
					{
						rect = mB_MaterialAndUVRect2.propsUseDifferntTiling_srcUVsamplingRect;
						rect2 = new Rect(0f, 0f, 1f, 1f);
					}
					if (MB2_TextureBakeResults.IsMeshAndMaterialRectEnclosedByAtlasRect(mB_MaterialAndUVRect2.tilingTreatment, value[submeshIdx].uvRect, rect2, rect, logLevel))
					{
						if (logLevel >= MB2_LogLevel.trace)
						{
							UnityEngine.Debug.Log(string.Concat("Found rect in atlas capable of containing tiled sampling rect for mesh ", m, " at idx=", k));
						}
						num = k;
						flag = true;
						break;
					}
				}
				if (flag)
				{
					MB_MaterialAndUVRect mB_MaterialAndUVRect3 = matsAndSrcUVRect[num];
					rectInAtlas = mB_MaterialAndUVRect3.atlasRect;
					tilingTreatment = mB_MaterialAndUVRect3.tilingTreatment;
					encapsulatingRectOut = mB_MaterialAndUVRect3.GetEncapsulatingRect();
					sourceMaterialTilingOut = mB_MaterialAndUVRect3.GetMaterialTilingRect();
					sliceIdx = mB_MaterialAndUVRect3.textureArraySliceIdx;
					return true;
				}
				rectInAtlas = default(Rect);
				encapsulatingRectOut = default(Rect);
				sourceMaterialTilingOut = default(Rect);
				sliceIdx = -1;
				errorMsg = $"Could not find a tiled rectangle in the atlas capable of containing the uv and material tiling on mesh {m.name} for material {mat}. Was this mesh included when atlases were baked?";
				return false;
			}
		}

		[SerializeField]
		protected List<GameObject> objectsInCombinedMesh = new List<GameObject>();

		[SerializeField]
		private int lightmapIndex = -1;

		[SerializeField]
		private List<MB_DynamicGameObject> mbDynamicObjectsInCombinedMesh = new List<MB_DynamicGameObject>();

		private Dictionary<GameObject, MB_DynamicGameObject> _instance2combined_map = new Dictionary<GameObject, MB_DynamicGameObject>();

		[SerializeField]
		private Vector3[] verts = new Vector3[0];

		[SerializeField]
		private Vector3[] normals = new Vector3[0];

		[SerializeField]
		private Vector4[] tangents = new Vector4[0];

		[SerializeField]
		private Vector2[] uvs = new Vector2[0];

		[SerializeField]
		private float[] uvsSliceIdx = new float[0];

		[SerializeField]
		private Vector2[] uv2s = new Vector2[0];

		[SerializeField]
		private Vector2[] uv3s = new Vector2[0];

		[SerializeField]
		private Vector2[] uv4s = new Vector2[0];

		[SerializeField]
		private Vector2[] uv5s = new Vector2[0];

		[SerializeField]
		private Vector2[] uv6s = new Vector2[0];

		[SerializeField]
		private Vector2[] uv7s = new Vector2[0];

		[SerializeField]
		private Vector2[] uv8s = new Vector2[0];

		[SerializeField]
		private Color[] colors = new Color[0];

		[SerializeField]
		private Matrix4x4[] bindPoses = new Matrix4x4[0];

		[SerializeField]
		private Transform[] bones = new Transform[0];

		[SerializeField]
		internal MBBlendShape[] blendShapes = new MBBlendShape[0];

		[SerializeField]
		internal MBBlendShape[] blendShapesInCombined = new MBBlendShape[0];

		[SerializeField]
		private SerializableIntArray[] submeshTris = new SerializableIntArray[0];

		[SerializeField]
		private MeshCreationConditions _meshBirth;

		[SerializeField]
		private Mesh _mesh;

		private BoneWeight[] boneWeights = new BoneWeight[0];

		private GameObject[] empty = new GameObject[0];

		private int[] emptyIDs = new int[0];

		public override MB2_TextureBakeResults textureBakeResults
		{
			set
			{
				if (mbDynamicObjectsInCombinedMesh.Count > 0 && _textureBakeResults != value && _textureBakeResults != null && LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("If Texture Bake Result is changed then objects currently in combined mesh may be invalid.");
				}
				_textureBakeResults = value;
			}
		}

		public override MB_RenderType renderType
		{
			set
			{
				if (value == MB_RenderType.skinnedMeshRenderer && _renderType == MB_RenderType.meshRenderer && boneWeights.Length != verts.Length)
				{
					UnityEngine.Debug.LogError("Can't set the render type to SkinnedMeshRenderer without clearing the mesh first. Try deleteing the CombinedMesh scene object.");
				}
				_renderType = value;
			}
		}

		public override GameObject resultSceneObject
		{
			set
			{
				if (_resultSceneObject != value && _resultSceneObject != null)
				{
					_targetRenderer = null;
					if (_mesh != null && LOG_LEVEL >= MB2_LogLevel.warn)
					{
						UnityEngine.Debug.LogWarning("Result Scene Object was changed when this mesh baker component had a reference to a mesh. If mesh is being used by another object make sure to reset the mesh to none before baking to avoid overwriting the other mesh.");
					}
				}
				_resultSceneObject = value;
			}
		}

		private MB_DynamicGameObject instance2Combined_MapGet(GameObject gameObjectID)
		{
			return _instance2combined_map[gameObjectID];
		}

		private void instance2Combined_MapAdd(GameObject gameObjectID, MB_DynamicGameObject dgo)
		{
			_instance2combined_map.Add(gameObjectID, dgo);
		}

		private void instance2Combined_MapRemove(GameObject gameObjectID)
		{
			_instance2combined_map.Remove(gameObjectID);
		}

		private bool instance2Combined_MapTryGetValue(GameObject gameObjectID, out MB_DynamicGameObject dgo)
		{
			return _instance2combined_map.TryGetValue(gameObjectID, out dgo);
		}

		private int instance2Combined_MapCount()
		{
			return _instance2combined_map.Count;
		}

		private void instance2Combined_MapClear()
		{
			_instance2combined_map.Clear();
		}

		private bool instance2Combined_MapContainsKey(GameObject gameObjectID)
		{
			return _instance2combined_map.ContainsKey(gameObjectID);
		}

		private bool InstanceID2DGO(int instanceID, out MB_DynamicGameObject dgoGameObject)
		{
			for (int i = 0; i < mbDynamicObjectsInCombinedMesh.Count; i++)
			{
				if (mbDynamicObjectsInCombinedMesh[i].instanceID == instanceID)
				{
					dgoGameObject = mbDynamicObjectsInCombinedMesh[i];
					return true;
				}
			}
			dgoGameObject = null;
			return false;
		}

		public override int GetNumObjectsInCombined()
		{
			return mbDynamicObjectsInCombinedMesh.Count;
		}

		public override List<GameObject> GetObjectsInCombined()
		{
			List<GameObject> list = new List<GameObject>();
			list.AddRange(objectsInCombinedMesh);
			return list;
		}

		public Mesh GetMesh()
		{
			if (_mesh == null)
			{
				_mesh = NewMesh();
			}
			return _mesh;
		}

		public void SetMesh(Mesh m)
		{
			if (m == null)
			{
				_meshBirth = MeshCreationConditions.AssignedByUser;
			}
			else
			{
				_meshBirth = MeshCreationConditions.NoMesh;
			}
			_mesh = m;
		}

		public Transform[] GetBones()
		{
			return bones;
		}

		public override int GetLightmapIndex()
		{
			if (base.settings.lightmapOption == MB2_LightmapOptions.generate_new_UV2_layout || base.settings.lightmapOption == MB2_LightmapOptions.preserve_current_lightmapping)
			{
				return lightmapIndex;
			}
			return -1;
		}

		public override int GetNumVerticesFor(GameObject go)
		{
			return GetNumVerticesFor(go.GetInstanceID());
		}

		public override int GetNumVerticesFor(int instanceID)
		{
			MB_DynamicGameObject dgoGameObject = null;
			InstanceID2DGO(instanceID, out dgoGameObject);
			return dgoGameObject?.numVerts ?? (-1);
		}

		private bool _Initialize(int numResultMats)
		{
			if (mbDynamicObjectsInCombinedMesh.Count == 0)
			{
				lightmapIndex = -1;
			}
			if (_mesh == null)
			{
				if (LOG_LEVEL >= MB2_LogLevel.debug)
				{
					MB2_Log.LogDebug("_initialize Creating new Mesh");
				}
				_mesh = GetMesh();
			}
			if (instance2Combined_MapCount() != mbDynamicObjectsInCombinedMesh.Count)
			{
				instance2Combined_MapClear();
				for (int i = 0; i < mbDynamicObjectsInCombinedMesh.Count; i++)
				{
					if (mbDynamicObjectsInCombinedMesh[i] != null)
					{
						if (mbDynamicObjectsInCombinedMesh[i].gameObject == null)
						{
							UnityEngine.Debug.LogError("This MeshBaker contains information from a previous bake that is incomlete. It may have been baked by a previous version of Mesh Baker. If you are trying to update/modify a previously baked combined mesh. Try doing the original bake.");
							return false;
						}
						instance2Combined_MapAdd(mbDynamicObjectsInCombinedMesh[i].gameObject, mbDynamicObjectsInCombinedMesh[i]);
					}
				}
				boneWeights = _mesh.boneWeights;
			}
			if (objectsInCombinedMesh.Count == 0 && submeshTris.Length != numResultMats)
			{
				submeshTris = new SerializableIntArray[numResultMats];
				for (int j = 0; j < submeshTris.Length; j++)
				{
					submeshTris[j] = new SerializableIntArray(0);
				}
			}
			if (mbDynamicObjectsInCombinedMesh.Count > 0 && mbDynamicObjectsInCombinedMesh[0].indexesOfBonesUsed.Length == 0 && base.settings.renderType == MB_RenderType.skinnedMeshRenderer && boneWeights.Length != 0)
			{
				for (int k = 0; k < mbDynamicObjectsInCombinedMesh.Count; k++)
				{
					MB_DynamicGameObject mB_DynamicGameObject = mbDynamicObjectsInCombinedMesh[k];
					HashSet<int> hashSet = new HashSet<int>();
					for (int l = mB_DynamicGameObject.vertIdx; l < mB_DynamicGameObject.vertIdx + mB_DynamicGameObject.numVerts; l++)
					{
						if (boneWeights[l].weight0 > 0f)
						{
							hashSet.Add(boneWeights[l].boneIndex0);
						}
						if (boneWeights[l].weight1 > 0f)
						{
							hashSet.Add(boneWeights[l].boneIndex1);
						}
						if (boneWeights[l].weight2 > 0f)
						{
							hashSet.Add(boneWeights[l].boneIndex2);
						}
						if (boneWeights[l].weight3 > 0f)
						{
							hashSet.Add(boneWeights[l].boneIndex3);
						}
					}
					mB_DynamicGameObject.indexesOfBonesUsed = new int[hashSet.Count];
					hashSet.CopyTo(mB_DynamicGameObject.indexesOfBonesUsed);
				}
				if (LOG_LEVEL >= MB2_LogLevel.debug)
				{
					UnityEngine.Debug.Log("Baker used old systems that duplicated bones. Upgrading to new system by building indexesOfBonesUsed");
				}
			}
			if (LOG_LEVEL >= MB2_LogLevel.trace)
			{
				UnityEngine.Debug.Log($"_initialize numObjsInCombined={mbDynamicObjectsInCombinedMesh.Count}");
			}
			return true;
		}

		private bool _collectMaterialTriangles(Mesh m, MB_DynamicGameObject dgo, Material[] sharedMaterials, OrderedDictionary sourceMats2submeshIdx_map)
		{
			int num = m.subMeshCount;
			if (sharedMaterials.Length < num)
			{
				num = sharedMaterials.Length;
			}
			dgo._tmpSubmeshTris = new SerializableIntArray[num];
			dgo.targetSubmeshIdxs = new int[num];
			for (int i = 0; i < num; i++)
			{
				if (_textureBakeResults.doMultiMaterial || _textureBakeResults.resultType == MB2_TextureBakeResults.ResultType.textureArray)
				{
					if (!sourceMats2submeshIdx_map.Contains(sharedMaterials[i]))
					{
						UnityEngine.Debug.LogError("Object " + dgo.name + " has a material that was not found in the result materials maping. " + sharedMaterials[i]);
						return false;
					}
					dgo.targetSubmeshIdxs[i] = (int)sourceMats2submeshIdx_map[sharedMaterials[i]];
				}
				else
				{
					dgo.targetSubmeshIdxs[i] = 0;
				}
				dgo._tmpSubmeshTris[i] = new SerializableIntArray();
				dgo._tmpSubmeshTris[i].data = m.GetTriangles(i);
				if (LOG_LEVEL >= MB2_LogLevel.debug)
				{
					MB2_Log.LogDebug("Collecting triangles for: " + dgo.name + " submesh:" + i + " maps to submesh:" + dgo.targetSubmeshIdxs[i] + " added:" + dgo._tmpSubmeshTris[i].data.Length, LOG_LEVEL);
				}
			}
			return true;
		}

		private bool _collectOutOfBoundsUVRects2(Mesh m, MB_DynamicGameObject dgo, Material[] sharedMaterials, OrderedDictionary sourceMats2submeshIdx_map, Dictionary<int, MB_Utility.MeshAnalysisResult[]> meshAnalysisResults, MeshChannelsCache meshChannelCache)
		{
			if (_textureBakeResults == null)
			{
				UnityEngine.Debug.LogError("Need to bake textures into combined material");
				return false;
			}
			if (!meshAnalysisResults.TryGetValue(m.GetInstanceID(), out var value))
			{
				int subMeshCount = m.subMeshCount;
				value = new MB_Utility.MeshAnalysisResult[subMeshCount];
				Vector2[] uv0Raw = meshChannelCache.GetUv0Raw(m);
				for (int i = 0; i < subMeshCount; i++)
				{
					MB_Utility.hasOutOfBoundsUVs(uv0Raw, m, ref value[i], i);
				}
				meshAnalysisResults.Add(m.GetInstanceID(), value);
			}
			int num = sharedMaterials.Length;
			if (num > m.subMeshCount)
			{
				num = m.subMeshCount;
			}
			dgo.obUVRects = new Rect[num];
			for (int j = 0; j < num; j++)
			{
				int idxInSrcMats = dgo.targetSubmeshIdxs[j];
				if (_textureBakeResults.GetConsiderMeshUVs(idxInSrcMats, sharedMaterials[j]))
				{
					dgo.obUVRects[j] = value[j].uvRect;
				}
			}
			return true;
		}

		private bool _validateTextureBakeResults()
		{
			if (_textureBakeResults == null)
			{
				UnityEngine.Debug.LogError("Texture Bake Results is null. Can't combine meshes.");
				return false;
			}
			if (_textureBakeResults.materialsAndUVRects == null || _textureBakeResults.materialsAndUVRects.Length == 0)
			{
				UnityEngine.Debug.LogError("Texture Bake Results has no materials in material to sourceUVRect map. Try baking materials. Can't combine meshes. If you are trying to combine meshes without combining materials, try removing the Texture Bake Result.");
				return false;
			}
			if (_textureBakeResults.NumResultMaterials() == 0)
			{
				UnityEngine.Debug.LogError("Texture Bake Results has no result materials. Try baking materials. Can't combine meshes.");
				return false;
			}
			if (base.settings.doUV && textureBakeResults.resultType == MB2_TextureBakeResults.ResultType.textureArray && uvs.Length != uvsSliceIdx.Length)
			{
				UnityEngine.Debug.LogError("uvs buffer and sliceIdx buffer are different sizes. Did you switch texture bake result from atlas to texture array result?");
				return false;
			}
			return true;
		}

		private bool _showHide(GameObject[] goToShow, GameObject[] goToHide)
		{
			if (goToShow == null)
			{
				goToShow = empty;
			}
			if (goToHide == null)
			{
				goToHide = empty;
			}
			int numResultMats = _textureBakeResults.NumResultMaterials();
			if (!_Initialize(numResultMats))
			{
				return false;
			}
			for (int i = 0; i < goToHide.Length; i++)
			{
				if (!instance2Combined_MapContainsKey(goToHide[i]))
				{
					if (LOG_LEVEL >= MB2_LogLevel.warn)
					{
						UnityEngine.Debug.LogWarning(string.Concat("Trying to hide an object ", goToHide[i], " that is not in combined mesh. Did you initially bake with 'clear buffers after bake' enabled?"));
					}
					return false;
				}
			}
			for (int j = 0; j < goToShow.Length; j++)
			{
				if (!instance2Combined_MapContainsKey(goToShow[j]))
				{
					if (LOG_LEVEL >= MB2_LogLevel.warn)
					{
						UnityEngine.Debug.LogWarning(string.Concat("Trying to show an object ", goToShow[j], " that is not in combined mesh. Did you initially bake with 'clear buffers after bake' enabled?"));
					}
					return false;
				}
			}
			for (int k = 0; k < goToHide.Length; k++)
			{
				_instance2combined_map[goToHide[k]].show = false;
			}
			for (int l = 0; l < goToShow.Length; l++)
			{
				_instance2combined_map[goToShow[l]].show = true;
			}
			return true;
		}

		private bool _addToCombined(GameObject[] goToAdd, int[] goToDelete, bool disableRendererInSource)
		{
			Stopwatch stopwatch = null;
			if (LOG_LEVEL >= MB2_LogLevel.debug)
			{
				stopwatch = new Stopwatch();
				stopwatch.Start();
			}
			if (!_validateTextureBakeResults())
			{
				return false;
			}
			if (!ValidateTargRendererAndMeshAndResultSceneObj())
			{
				return false;
			}
			if (outputOption != MB2_OutputOptions.bakeMeshAssetsInPlace && base.settings.renderType == MB_RenderType.skinnedMeshRenderer && (_targetRenderer == null || !(_targetRenderer is SkinnedMeshRenderer)))
			{
				UnityEngine.Debug.LogError("Target renderer must be set and must be a SkinnedMeshRenderer");
				return false;
			}
			if (base.settings.doBlendShapes && base.settings.renderType != MB_RenderType.skinnedMeshRenderer)
			{
				UnityEngine.Debug.LogError("If doBlendShapes is set then RenderType must be skinnedMeshRenderer.");
				return false;
			}
			GameObject[] _goToAdd;
			if (goToAdd == null)
			{
				_goToAdd = empty;
			}
			else
			{
				_goToAdd = (GameObject[])goToAdd.Clone();
			}
			int[] array = ((goToDelete != null) ? ((int[])goToDelete.Clone()) : emptyIDs);
			if (_mesh == null)
			{
				DestroyMesh();
			}
			UVAdjuster_Atlas uVAdjuster_Atlas = new UVAdjuster_Atlas(textureBakeResults, LOG_LEVEL);
			int num = _textureBakeResults.NumResultMaterials();
			if (!_Initialize(num))
			{
				return false;
			}
			if (submeshTris.Length != num)
			{
				UnityEngine.Debug.LogError("The number of submeshes " + submeshTris.Length + " in the combined mesh was not equal to the number of result materials " + num + " in the Texture Bake Result");
				return false;
			}
			if (_mesh.vertexCount > 0 && _instance2combined_map.Count == 0)
			{
				UnityEngine.Debug.LogWarning("There were vertices in the combined mesh but nothing in the MeshBaker buffers. If you are trying to bake in the editor and modify at runtime, make sure 'Clear Buffers After Bake' is unchecked.");
			}
			if (LOG_LEVEL >= MB2_LogLevel.debug)
			{
				MB2_Log.LogDebug("==== Calling _addToCombined objs adding:" + _goToAdd.Length + " objs deleting:" + array.Length + " fixOutOfBounds:" + textureBakeResults.DoAnyResultMatsUseConsiderMeshUVs().ToString() + " doMultiMaterial:" + textureBakeResults.doMultiMaterial.ToString() + " disableRenderersInSource:" + disableRendererInSource.ToString(), LOG_LEVEL);
			}
			if (_textureBakeResults.NumResultMaterials() == 0)
			{
				UnityEngine.Debug.LogError("No resultMaterials in this TextureBakeResults. Try baking textures.");
				return false;
			}
			OrderedDictionary orderedDictionary = BuildSourceMatsToSubmeshIdxMap(num);
			if (orderedDictionary == null)
			{
				return false;
			}
			int num2 = 0;
			int[] array2 = new int[num];
			int num3 = 0;
			MB3_MeshCombinerSimpleBones mB3_MeshCombinerSimpleBones = new MB3_MeshCombinerSimpleBones(this);
			mB3_MeshCombinerSimpleBones.BuildBoneIdx2DGOMapIfNecessary(array);
			for (int j = 0; j < array.Length; j++)
			{
				MB_DynamicGameObject dgoGameObject = null;
				InstanceID2DGO(array[j], out dgoGameObject);
				if (dgoGameObject != null)
				{
					num2 += dgoGameObject.numVerts;
					num3 += dgoGameObject.numBlendShapes;
					if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
					{
						mB3_MeshCombinerSimpleBones.FindBonesToDelete(dgoGameObject);
					}
					for (int k = 0; k < dgoGameObject.submeshNumTris.Length; k++)
					{
						array2[k] += dgoGameObject.submeshNumTris[k];
					}
				}
				else if (LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("Trying to delete an object that is not in combined mesh");
				}
			}
			List<MB_DynamicGameObject> list = new List<MB_DynamicGameObject>();
			Dictionary<int, MB_Utility.MeshAnalysisResult[]> dictionary = new Dictionary<int, MB_Utility.MeshAnalysisResult[]>();
			MeshChannelsCache meshChannelsCache = new MeshChannelsCache(LOG_LEVEL, base.settings.lightmapOption);
			int num4 = 0;
			int[] array3 = new int[num];
			int num5 = 0;
			int i;
			for (i = 0; i < _goToAdd.Length; i++)
			{
				if (!instance2Combined_MapContainsKey(_goToAdd[i]) || Array.FindIndex(array, (int o) => o == _goToAdd[i].GetInstanceID()) != -1)
				{
					MB_DynamicGameObject mB_DynamicGameObject = new MB_DynamicGameObject();
					GameObject gameObject = _goToAdd[i];
					Material[] array4 = MB_Utility.GetGOMaterials(gameObject);
					if (LOG_LEVEL >= MB2_LogLevel.trace)
					{
						UnityEngine.Debug.Log($"Getting {array4.Length} shared materials for {gameObject}");
					}
					if (array4 == null)
					{
						UnityEngine.Debug.LogError("Object " + gameObject.name + " does not have a Renderer");
						_goToAdd[i] = null;
						return false;
					}
					Mesh mesh = MB_Utility.GetMesh(gameObject);
					if (array4.Length > mesh.subMeshCount)
					{
						Array.Resize(ref array4, mesh.subMeshCount);
					}
					if (mesh == null)
					{
						UnityEngine.Debug.LogError("Object " + gameObject.name + " MeshFilter or SkinedMeshRenderer had no mesh");
						_goToAdd[i] = null;
						return false;
					}
					if (MBVersion.IsRunningAndMeshNotReadWriteable(mesh))
					{
						UnityEngine.Debug.LogError("Object " + gameObject.name + " Mesh Importer has read/write flag set to 'false'. This needs to be set to 'true' in order to read data from this mesh.");
						_goToAdd[i] = null;
						return false;
					}
					if (!uVAdjuster_Atlas.MapSharedMaterialsToAtlasRects(array4, checkTargetSubmeshIdxsFromPreviousBake: false, mesh, meshChannelsCache, dictionary, orderedDictionary, gameObject, mB_DynamicGameObject))
					{
						_goToAdd[i] = null;
						return false;
					}
					if (!(_goToAdd[i] != null))
					{
						continue;
					}
					list.Add(mB_DynamicGameObject);
					mB_DynamicGameObject.name = $"{_goToAdd[i].ToString()} {_goToAdd[i].GetInstanceID()}";
					mB_DynamicGameObject.instanceID = _goToAdd[i].GetInstanceID();
					mB_DynamicGameObject.gameObject = _goToAdd[i];
					mB_DynamicGameObject.numVerts = mesh.vertexCount;
					if (base.settings.doBlendShapes)
					{
						mB_DynamicGameObject.numBlendShapes = mesh.blendShapeCount;
					}
					Renderer renderer = MB_Utility.GetRenderer(gameObject);
					if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer && !mB3_MeshCombinerSimpleBones.CollectBonesToAddForDGO(mB_DynamicGameObject, renderer, base.settings.smrNoExtraBonesWhenCombiningMeshRenderers, meshChannelsCache))
					{
						UnityEngine.Debug.LogError("Object " + gameObject.name + " could not collect bones.");
						_goToAdd[i] = null;
						return false;
					}
					if (lightmapIndex == -1)
					{
						lightmapIndex = renderer.lightmapIndex;
					}
					if (base.settings.lightmapOption == MB2_LightmapOptions.preserve_current_lightmapping)
					{
						if (lightmapIndex != renderer.lightmapIndex && LOG_LEVEL >= MB2_LogLevel.warn)
						{
							UnityEngine.Debug.LogWarning("Object " + gameObject.name + " has a different lightmap index. Lightmapping will not work.", gameObject);
						}
						if (!MBVersion.GetActive(gameObject) && LOG_LEVEL >= MB2_LogLevel.warn)
						{
							UnityEngine.Debug.LogWarning("Object " + gameObject.name + " is inactive. Can only get lightmap index of active objects.", gameObject);
						}
						if (renderer.lightmapIndex == -1 && LOG_LEVEL >= MB2_LogLevel.warn)
						{
							UnityEngine.Debug.LogWarning("Object " + gameObject.name + " does not have an index to a lightmap.", gameObject);
						}
					}
					mB_DynamicGameObject.lightmapIndex = renderer.lightmapIndex;
					mB_DynamicGameObject.lightmapTilingOffset = MBVersion.GetLightmapTilingOffset(renderer);
					if (!_collectMaterialTriangles(mesh, mB_DynamicGameObject, array4, orderedDictionary))
					{
						return false;
					}
					mB_DynamicGameObject.meshSize = renderer.bounds.size;
					mB_DynamicGameObject.submeshNumTris = new int[num];
					mB_DynamicGameObject.submeshTriIdxs = new int[num];
					mB_DynamicGameObject.sourceSharedMaterials = array4;
					bool flag = textureBakeResults.DoAnyResultMatsUseConsiderMeshUVs();
					if (flag && !_collectOutOfBoundsUVRects2(mesh, mB_DynamicGameObject, array4, orderedDictionary, dictionary, meshChannelsCache))
					{
						return false;
					}
					num4 += mB_DynamicGameObject.numVerts;
					num5 += mB_DynamicGameObject.numBlendShapes;
					for (int l = 0; l < mB_DynamicGameObject._tmpSubmeshTris.Length; l++)
					{
						array3[mB_DynamicGameObject.targetSubmeshIdxs[l]] += mB_DynamicGameObject._tmpSubmeshTris[l].data.Length;
					}
					mB_DynamicGameObject.invertTriangles = IsMirrored(gameObject.transform.localToWorldMatrix);
					if (!flag)
					{
					}
				}
				else
				{
					if (LOG_LEVEL >= MB2_LogLevel.warn)
					{
						UnityEngine.Debug.LogWarning("Object " + _goToAdd[i].name + " has already been added");
					}
					_goToAdd[i] = null;
				}
			}
			for (int m = 0; m < _goToAdd.Length; m++)
			{
				if (_goToAdd[m] != null && disableRendererInSource)
				{
					MB_Utility.DisableRendererInSource(_goToAdd[m]);
					if (LOG_LEVEL == MB2_LogLevel.trace)
					{
						UnityEngine.Debug.Log("Disabling renderer on " + _goToAdd[m].name + " id=" + _goToAdd[m].GetInstanceID());
					}
				}
			}
			int num6 = verts.Length + num4 - num2;
			int newBonesLength = mB3_MeshCombinerSimpleBones.GetNewBonesLength();
			int[] array5 = new int[num];
			int num7 = 0;
			if (base.settings.doBlendShapes)
			{
				num7 = blendShapes.Length + num5 - num3;
			}
			if (LOG_LEVEL >= MB2_LogLevel.debug)
			{
				UnityEngine.Debug.Log("Verts adding:" + num4 + " deleting:" + num2 + " submeshes:" + array5.Length + " bones:" + newBonesLength + " blendShapes:" + num7);
			}
			for (int n = 0; n < array5.Length; n++)
			{
				array5[n] = submeshTris[n].data.Length + array3[n] - array2[n];
				if (LOG_LEVEL >= MB2_LogLevel.debug)
				{
					MB2_Log.LogDebug("    submesh :" + n + " already contains:" + submeshTris[n].data.Length + " tris to be Added:" + array3[n] + " tris to be Deleted:" + array2[n]);
				}
			}
			if (num6 >= MBVersion.MaxMeshVertexCount())
			{
				UnityEngine.Debug.LogError("Cannot add objects. Resulting mesh will have more than " + MBVersion.MaxMeshVertexCount() + " vertices. Try using a Multi-MeshBaker component. This will split the combined mesh into several meshes. You don't have to re-configure the MB2_TextureBaker. Just remove the MB2_MeshBaker component and add a MB2_MultiMeshBaker component.");
				return false;
			}
			Vector3[] destinationArray = null;
			Vector4[] destinationArray2 = null;
			Vector2[] destinationArray3 = null;
			Vector2[] destinationArray4 = null;
			Vector2[] destinationArray5 = null;
			Vector2[] destinationArray6 = null;
			Vector2[] destinationArray7 = null;
			Vector2[] destinationArray8 = null;
			Vector2[] destinationArray9 = null;
			Vector2[] destinationArray10 = null;
			float[] destinationArray11 = null;
			Color[] destinationArray12 = null;
			MBBlendShape[] array6 = null;
			Vector3[] destinationArray13 = new Vector3[num6];
			if (base.settings.doNorm)
			{
				destinationArray = new Vector3[num6];
			}
			if (base.settings.doTan)
			{
				destinationArray2 = new Vector4[num6];
			}
			if (base.settings.doUV)
			{
				destinationArray3 = new Vector2[num6];
			}
			if (base.settings.doUV && textureBakeResults.resultType == MB2_TextureBakeResults.ResultType.textureArray)
			{
				destinationArray11 = new float[num6];
			}
			if (base.settings.doUV3)
			{
				destinationArray5 = new Vector2[num6];
			}
			if (base.settings.doUV4)
			{
				destinationArray6 = new Vector2[num6];
			}
			if (base.settings.doUV5)
			{
				destinationArray7 = new Vector2[num6];
			}
			if (base.settings.doUV6)
			{
				destinationArray8 = new Vector2[num6];
			}
			if (base.settings.doUV7)
			{
				destinationArray9 = new Vector2[num6];
			}
			if (base.settings.doUV8)
			{
				destinationArray10 = new Vector2[num6];
			}
			if (doUV2())
			{
				destinationArray4 = new Vector2[num6];
			}
			if (base.settings.doCol)
			{
				destinationArray12 = new Color[num6];
			}
			if (base.settings.doBlendShapes)
			{
				array6 = new MBBlendShape[num7];
			}
			BoneWeight[] array7 = new BoneWeight[num6];
			Matrix4x4[] array8 = new Matrix4x4[newBonesLength];
			Transform[] array9 = new Transform[newBonesLength];
			SerializableIntArray[] array10 = new SerializableIntArray[num];
			for (int num8 = 0; num8 < array10.Length; num8++)
			{
				array10[num8] = new SerializableIntArray(array5[num8]);
			}
			for (int num9 = 0; num9 < array.Length; num9++)
			{
				MB_DynamicGameObject dgoGameObject2 = null;
				InstanceID2DGO(array[num9], out dgoGameObject2);
				if (dgoGameObject2 != null)
				{
					dgoGameObject2._beingDeleted = true;
				}
			}
			mbDynamicObjectsInCombinedMesh.Sort();
			int num10 = 0;
			int num11 = 0;
			int[] array11 = new int[num];
			int num12 = 0;
			for (int num13 = 0; num13 < mbDynamicObjectsInCombinedMesh.Count; num13++)
			{
				MB_DynamicGameObject mB_DynamicGameObject2 = mbDynamicObjectsInCombinedMesh[num13];
				if (!mB_DynamicGameObject2._beingDeleted)
				{
					if (LOG_LEVEL >= MB2_LogLevel.debug)
					{
						MB2_Log.LogDebug("Copying obj in combined arrays idx:" + num13, LOG_LEVEL);
					}
					Array.Copy(verts, mB_DynamicGameObject2.vertIdx, destinationArray13, num10, mB_DynamicGameObject2.numVerts);
					if (base.settings.doNorm)
					{
						Array.Copy(normals, mB_DynamicGameObject2.vertIdx, destinationArray, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doTan)
					{
						Array.Copy(tangents, mB_DynamicGameObject2.vertIdx, destinationArray2, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doUV)
					{
						Array.Copy(uvs, mB_DynamicGameObject2.vertIdx, destinationArray3, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doUV && textureBakeResults.resultType == MB2_TextureBakeResults.ResultType.textureArray)
					{
						Array.Copy(uvsSliceIdx, mB_DynamicGameObject2.vertIdx, destinationArray11, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doUV3)
					{
						Array.Copy(uv3s, mB_DynamicGameObject2.vertIdx, destinationArray5, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doUV4)
					{
						Array.Copy(uv4s, mB_DynamicGameObject2.vertIdx, destinationArray6, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doUV5)
					{
						Array.Copy(uv5s, mB_DynamicGameObject2.vertIdx, destinationArray7, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doUV6)
					{
						Array.Copy(uv6s, mB_DynamicGameObject2.vertIdx, destinationArray8, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doUV7)
					{
						Array.Copy(uv7s, mB_DynamicGameObject2.vertIdx, destinationArray9, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doUV8)
					{
						Array.Copy(uv8s, mB_DynamicGameObject2.vertIdx, destinationArray10, num10, mB_DynamicGameObject2.numVerts);
					}
					if (doUV2())
					{
						Array.Copy(uv2s, mB_DynamicGameObject2.vertIdx, destinationArray4, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doCol)
					{
						Array.Copy(colors, mB_DynamicGameObject2.vertIdx, destinationArray12, num10, mB_DynamicGameObject2.numVerts);
					}
					if (base.settings.doBlendShapes)
					{
						Array.Copy(blendShapes, mB_DynamicGameObject2.blendShapeIdx, array6, num11, mB_DynamicGameObject2.numBlendShapes);
					}
					if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
					{
						Array.Copy(boneWeights, mB_DynamicGameObject2.vertIdx, array7, num10, mB_DynamicGameObject2.numVerts);
					}
					for (int num14 = 0; num14 < num; num14++)
					{
						int[] data = submeshTris[num14].data;
						int num15 = mB_DynamicGameObject2.submeshTriIdxs[num14];
						int num16 = mB_DynamicGameObject2.submeshNumTris[num14];
						if (LOG_LEVEL >= MB2_LogLevel.debug)
						{
							MB2_Log.LogDebug("    Adjusting submesh triangles submesh:" + num14 + " startIdx:" + num15 + " num:" + num16 + " nsubmeshTris:" + array10.Length + " targSubmeshTidx:" + array11.Length, LOG_LEVEL);
						}
						for (int num17 = num15; num17 < num15 + num16; num17++)
						{
							data[num17] -= num12;
						}
						Array.Copy(data, num15, array10[num14].data, array11[num14], num16);
					}
					mB_DynamicGameObject2.vertIdx = num10;
					mB_DynamicGameObject2.blendShapeIdx = num11;
					for (int num18 = 0; num18 < array11.Length; num18++)
					{
						mB_DynamicGameObject2.submeshTriIdxs[num18] = array11[num18];
						array11[num18] += mB_DynamicGameObject2.submeshNumTris[num18];
					}
					num11 += mB_DynamicGameObject2.numBlendShapes;
					num10 += mB_DynamicGameObject2.numVerts;
				}
				else
				{
					if (LOG_LEVEL >= MB2_LogLevel.debug)
					{
						MB2_Log.LogDebug("Not copying obj: " + num13, LOG_LEVEL);
					}
					num12 += mB_DynamicGameObject2.numVerts;
				}
			}
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				mB3_MeshCombinerSimpleBones.CopyBonesWeAreKeepingToNewBonesArrayAndAdjustBWIndexes(array9, array8, array7, num2);
			}
			for (int num19 = mbDynamicObjectsInCombinedMesh.Count - 1; num19 >= 0; num19--)
			{
				if (mbDynamicObjectsInCombinedMesh[num19]._beingDeleted)
				{
					instance2Combined_MapRemove(mbDynamicObjectsInCombinedMesh[num19].gameObject);
					objectsInCombinedMesh.RemoveAt(num19);
					mbDynamicObjectsInCombinedMesh.RemoveAt(num19);
				}
			}
			verts = destinationArray13;
			if (base.settings.doNorm)
			{
				normals = destinationArray;
			}
			if (base.settings.doTan)
			{
				tangents = destinationArray2;
			}
			if (base.settings.doUV)
			{
				uvs = destinationArray3;
			}
			if (base.settings.doUV && textureBakeResults.resultType == MB2_TextureBakeResults.ResultType.textureArray)
			{
				uvsSliceIdx = destinationArray11;
			}
			if (base.settings.doUV3)
			{
				uv3s = destinationArray5;
			}
			if (base.settings.doUV4)
			{
				uv4s = destinationArray6;
			}
			if (base.settings.doUV5)
			{
				uv5s = destinationArray7;
			}
			if (base.settings.doUV6)
			{
				uv6s = destinationArray8;
			}
			if (base.settings.doUV7)
			{
				uv7s = destinationArray9;
			}
			if (base.settings.doUV8)
			{
				uv8s = destinationArray10;
			}
			if (doUV2())
			{
				uv2s = destinationArray4;
			}
			if (base.settings.doCol)
			{
				colors = destinationArray12;
			}
			if (base.settings.doBlendShapes)
			{
				blendShapes = array6;
			}
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				boneWeights = array7;
			}
			int num20 = bones.Length - mB3_MeshCombinerSimpleBones.GetNumBonesToDelete();
			bindPoses = array8;
			bones = array9;
			submeshTris = array10;
			int num21 = 0;
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				foreach (BoneAndBindpose item in mB3_MeshCombinerSimpleBones.GetBonesToAdd())
				{
					array9[num20 + num21] = item.bone;
					array8[num20 + num21] = item.bindPose;
					num21++;
				}
			}
			for (int num22 = 0; num22 < list.Count; num22++)
			{
				MB_DynamicGameObject mB_DynamicGameObject3 = list[num22];
				GameObject gameObject2 = _goToAdd[num22];
				int num23 = num10;
				int index = num11;
				Mesh mesh2 = MB_Utility.GetMesh(gameObject2);
				Matrix4x4 localToWorldMatrix = gameObject2.transform.localToWorldMatrix;
				Matrix4x4 matrix4x = localToWorldMatrix;
				float num25 = (matrix4x[2, 3] = 0f);
				float value = (matrix4x[1, 3] = num25);
				matrix4x[0, 3] = value;
				matrix4x = matrix4x.inverse.transpose;
				destinationArray13 = meshChannelsCache.GetVertices(mesh2);
				Vector3[] array12 = null;
				Vector4[] array13 = null;
				if (base.settings.doNorm)
				{
					array12 = meshChannelsCache.GetNormals(mesh2);
				}
				if (base.settings.doTan)
				{
					array13 = meshChannelsCache.GetTangents(mesh2);
				}
				if (base.settings.renderType != MB_RenderType.skinnedMeshRenderer)
				{
					for (int num27 = 0; num27 < destinationArray13.Length; num27++)
					{
						int num28 = num23 + num27;
						verts[num23 + num27] = localToWorldMatrix.MultiplyPoint3x4(destinationArray13[num27]);
						if (base.settings.doNorm)
						{
							normals[num28] = matrix4x.MultiplyPoint3x4(array12[num27]).normalized;
						}
						if (base.settings.doTan)
						{
							float w = array13[num27].w;
							tangents[num28] = matrix4x.MultiplyPoint3x4(array13[num27]).normalized;
							tangents[num28].w = w;
						}
					}
				}
				else
				{
					mB3_MeshCombinerSimpleBones.CopyVertsNormsTansToBuffers(mB_DynamicGameObject3, base.settings, num23, array12, array13, destinationArray13, normals, tangents, verts);
				}
				int subMeshCount = mesh2.subMeshCount;
				if (mB_DynamicGameObject3.uvRects.Length < subMeshCount)
				{
					if (LOG_LEVEL >= MB2_LogLevel.debug)
					{
						MB2_Log.LogDebug("Mesh " + mB_DynamicGameObject3.name + " has more submeshes than materials");
					}
					subMeshCount = mB_DynamicGameObject3.uvRects.Length;
				}
				else if (mB_DynamicGameObject3.uvRects.Length > subMeshCount && LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("Mesh " + mB_DynamicGameObject3.name + " has fewer submeshes than materials");
				}
				if (base.settings.doUV)
				{
					uVAdjuster_Atlas._copyAndAdjustUVsFromMesh(textureBakeResults, mB_DynamicGameObject3, mesh2, 0, num23, uvs, uvsSliceIdx, meshChannelsCache);
				}
				if (doUV2())
				{
					_copyAndAdjustUV2FromMesh(mB_DynamicGameObject3, mesh2, num23, meshChannelsCache);
				}
				if (base.settings.doUV3)
				{
					destinationArray5 = meshChannelsCache.GetUVChannel(3, mesh2);
					destinationArray5.CopyTo(uv3s, num23);
				}
				if (base.settings.doUV4)
				{
					destinationArray6 = meshChannelsCache.GetUVChannel(4, mesh2);
					destinationArray6.CopyTo(uv4s, num23);
				}
				if (base.settings.doUV5)
				{
					destinationArray7 = meshChannelsCache.GetUVChannel(5, mesh2);
					destinationArray7.CopyTo(uv5s, num23);
				}
				if (base.settings.doUV6)
				{
					destinationArray8 = meshChannelsCache.GetUVChannel(6, mesh2);
					destinationArray8.CopyTo(uv6s, num23);
				}
				if (base.settings.doUV7)
				{
					destinationArray9 = meshChannelsCache.GetUVChannel(7, mesh2);
					destinationArray9.CopyTo(uv7s, num23);
				}
				if (base.settings.doUV8)
				{
					destinationArray10 = meshChannelsCache.GetUVChannel(8, mesh2);
					destinationArray10.CopyTo(uv8s, num23);
				}
				if (base.settings.doCol)
				{
					destinationArray12 = meshChannelsCache.GetColors(mesh2);
					destinationArray12.CopyTo(colors, num23);
				}
				if (base.settings.doBlendShapes)
				{
					array6 = meshChannelsCache.GetBlendShapes(mesh2, mB_DynamicGameObject3.instanceID, mB_DynamicGameObject3.gameObject);
					array6.CopyTo(blendShapes, index);
				}
				if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
				{
					Renderer renderer2 = MB_Utility.GetRenderer(gameObject2);
					MB3_MeshCombinerSimpleBones.AddBonesToNewBonesArrayAndAdjustBWIndexes(this, mB_DynamicGameObject3, renderer2, num23, array9, array7, meshChannelsCache);
				}
				for (int num29 = 0; num29 < array11.Length; num29++)
				{
					mB_DynamicGameObject3.submeshTriIdxs[num29] = array11[num29];
				}
				for (int num30 = 0; num30 < mB_DynamicGameObject3._tmpSubmeshTris.Length; num30++)
				{
					int[] data2 = mB_DynamicGameObject3._tmpSubmeshTris[num30].data;
					for (int num31 = 0; num31 < data2.Length; num31++)
					{
						data2[num31] += num23;
					}
					if (mB_DynamicGameObject3.invertTriangles)
					{
						for (int num32 = 0; num32 < data2.Length; num32 += 3)
						{
							int num33 = data2[num32];
							data2[num32] = data2[num32 + 1];
							data2[num32 + 1] = num33;
						}
					}
					int num34 = mB_DynamicGameObject3.targetSubmeshIdxs[num30];
					data2.CopyTo(submeshTris[num34].data, array11[num34]);
					mB_DynamicGameObject3.submeshNumTris[num34] += data2.Length;
					array11[num34] += data2.Length;
				}
				mB_DynamicGameObject3.vertIdx = num10;
				mB_DynamicGameObject3.blendShapeIdx = num11;
				instance2Combined_MapAdd(gameObject2, mB_DynamicGameObject3);
				objectsInCombinedMesh.Add(gameObject2);
				mbDynamicObjectsInCombinedMesh.Add(mB_DynamicGameObject3);
				num10 += destinationArray13.Length;
				if (base.settings.doBlendShapes)
				{
					num11 += array6.Length;
				}
				for (int num35 = 0; num35 < mB_DynamicGameObject3._tmpSubmeshTris.Length; num35++)
				{
					mB_DynamicGameObject3._tmpSubmeshTris[num35] = null;
				}
				mB_DynamicGameObject3._tmpSubmeshTris = null;
				if (LOG_LEVEL >= MB2_LogLevel.debug)
				{
					MB2_Log.LogDebug("Added to combined:" + mB_DynamicGameObject3.name + " verts:" + destinationArray13.Length + " bindPoses:" + array8.Length, LOG_LEVEL);
				}
			}
			if (base.settings.lightmapOption == MB2_LightmapOptions.copy_UV2_unchanged_to_separate_rects)
			{
				_copyUV2unchangedToSeparateRects();
			}
			if (LOG_LEVEL >= MB2_LogLevel.debug)
			{
				MB2_Log.LogDebug("===== _addToCombined completed. Verts in buffer: " + verts.Length + " time(ms): " + stopwatch.ElapsedMilliseconds, LOG_LEVEL);
			}
			return true;
		}

		private void _copyAndAdjustUV2FromMesh(MB_DynamicGameObject dgo, Mesh mesh, int vertsIdx, MeshChannelsCache meshChannelsCache)
		{
			Vector2[] array = meshChannelsCache.GetUVChannel(2, mesh);
			if (base.settings.lightmapOption == MB2_LightmapOptions.preserve_current_lightmapping)
			{
				if (array == null || array.Length == 0)
				{
					Vector2[] uVChannel = meshChannelsCache.GetUVChannel(0, mesh);
					if (uVChannel != null && uVChannel.Length != 0)
					{
						array = uVChannel;
					}
					else
					{
						if (LOG_LEVEL >= MB2_LogLevel.warn)
						{
							UnityEngine.Debug.LogWarning(string.Concat("Mesh ", mesh, " didn't have uv2s. Generating uv2s."));
						}
						array = meshChannelsCache.GetUv2Modified(mesh);
					}
				}
				Vector4 lightmapTilingOffset = dgo.lightmapTilingOffset;
				Vector2 vector = new Vector2(lightmapTilingOffset.x, lightmapTilingOffset.y);
				Vector2 vector2 = new Vector2(lightmapTilingOffset.z, lightmapTilingOffset.w);
				Vector2 vector3 = default(Vector2);
				for (int i = 0; i < array.Length; i++)
				{
					vector3.x = vector.x * array[i].x;
					vector3.y = vector.y * array[i].y;
					uv2s[vertsIdx + i] = vector2 + vector3;
				}
				if (LOG_LEVEL >= MB2_LogLevel.trace)
				{
					UnityEngine.Debug.Log("_copyAndAdjustUV2FromMesh copied and modify for preserve current lightmapping " + array.Length);
				}
				return;
			}
			if (array == null || array.Length == 0)
			{
				if (LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning(string.Concat("Mesh ", mesh, " didn't have uv2s. Generating uv2s."));
				}
				if (lightmapOption == MB2_LightmapOptions.copy_UV2_unchanged_to_separate_rects && (array == null || array.Length == 0))
				{
					UnityEngine.Debug.LogError(string.Concat("Mesh ", mesh, " did not have a UV2 channel. Nothing to copy when trying to copy UV2 to separate rects. The combined mesh will not lightmap properly. Try using generate new uv2 layout."));
				}
				array = meshChannelsCache.GetUv2Modified(mesh);
			}
			array.CopyTo(uv2s, vertsIdx);
			if (LOG_LEVEL >= MB2_LogLevel.trace)
			{
				UnityEngine.Debug.Log("_copyAndAdjustUV2FromMesh copied without modifying " + array.Length);
			}
		}

		private Transform[] _getBones(Renderer r, bool isSkinnedMeshWithBones)
		{
			return MBVersion.GetBones(r, isSkinnedMeshWithBones);
		}

		public override void Apply(GenerateUV2Delegate uv2GenerationMethod)
		{
			bool flag = false;
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				flag = true;
			}
			Apply(triangles: true, vertices: true, base.settings.doNorm, base.settings.doTan, base.settings.doUV, doUV2(), base.settings.doUV3, base.settings.doUV4, base.settings.doUV5, base.settings.doUV6, base.settings.doUV7, base.settings.doUV8, base.settings.doCol, flag, base.settings.doBlendShapes, uv2GenerationMethod);
		}

		public virtual void ApplyShowHide()
		{
			if (_validationLevel >= MB2_ValidationLevel.quick && !ValidateTargRendererAndMeshAndResultSceneObj())
			{
				return;
			}
			if (_mesh != null)
			{
				if (base.settings.renderType == MB_RenderType.meshRenderer)
				{
					MBVersion.MeshClear(_mesh, t: true);
					_mesh.vertices = verts;
				}
				SerializableIntArray[] submeshTrisWithShowHideApplied = GetSubmeshTrisWithShowHideApplied();
				if (textureBakeResults.doMultiMaterial)
				{
					int num2 = (_mesh.subMeshCount = _numNonZeroLengthSubmeshTris(submeshTrisWithShowHideApplied));
					int numNonZeroLengthSubmeshTris = num2;
					int num3 = 0;
					for (int i = 0; i < submeshTrisWithShowHideApplied.Length; i++)
					{
						if (submeshTrisWithShowHideApplied[i].data.Length != 0)
						{
							_mesh.SetTriangles(submeshTrisWithShowHideApplied[i].data, num3);
							num3++;
						}
					}
					_updateMaterialsOnTargetRenderer(submeshTrisWithShowHideApplied, numNonZeroLengthSubmeshTris);
				}
				else
				{
					_mesh.triangles = submeshTrisWithShowHideApplied[0].data;
				}
				if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
				{
					if (verts.Length == 0)
					{
						targetRenderer.enabled = false;
					}
					else
					{
						targetRenderer.enabled = true;
					}
					bool updateWhenOffscreen = ((SkinnedMeshRenderer)targetRenderer).updateWhenOffscreen;
					((SkinnedMeshRenderer)targetRenderer).updateWhenOffscreen = true;
					((SkinnedMeshRenderer)targetRenderer).updateWhenOffscreen = updateWhenOffscreen;
					((SkinnedMeshRenderer)targetRenderer).sharedMesh = null;
					((SkinnedMeshRenderer)targetRenderer).sharedMesh = _mesh;
				}
				if (LOG_LEVEL >= MB2_LogLevel.trace)
				{
					UnityEngine.Debug.Log("ApplyShowHide");
				}
			}
			else
			{
				UnityEngine.Debug.LogError("Need to add objects to this meshbaker before calling ApplyShowHide");
			}
		}

		public override void Apply(bool triangles, bool vertices, bool normals, bool tangents, bool uvs, bool uv2, bool uv3, bool uv4, bool colors, bool bones = false, bool blendShapesFlag = false, GenerateUV2Delegate uv2GenerationMethod = null)
		{
			Apply(triangles, vertices, normals, tangents, uvs, uv2, uv3, uv4, uv5: false, uv6: false, uv7: false, uv8: false, colors, bones, blendShapesFlag, uv2GenerationMethod);
		}

		public override void Apply(bool triangles, bool vertices, bool normals, bool tangents, bool uvs, bool uv2, bool uv3, bool uv4, bool uv5, bool uv6, bool uv7, bool uv8, bool colors, bool bones = false, bool blendShapesFlag = false, GenerateUV2Delegate uv2GenerationMethod = null)
		{
			Stopwatch stopwatch = null;
			if (LOG_LEVEL >= MB2_LogLevel.debug)
			{
				stopwatch = new Stopwatch();
				stopwatch.Start();
			}
			if (_validationLevel >= MB2_ValidationLevel.quick && !ValidateTargRendererAndMeshAndResultSceneObj())
			{
				return;
			}
			if (_mesh != null)
			{
				if (LOG_LEVEL >= MB2_LogLevel.trace)
				{
					UnityEngine.Debug.Log($"Apply called tri={triangles} vert={vertices} norm={normals} tan={tangents} uv={uvs} col={colors} uv3={uv3} uv4={uv4} uv2={uv2} bone={bones} blendShape{blendShapesFlag} meshID={_mesh.GetInstanceID()}");
				}
				if (triangles || _mesh.vertexCount != verts.Length)
				{
					bool justClearTriangles = triangles && !vertices && !normals && !tangents && !uvs && !colors && !uv3 && !uv4 && !uv2 && !bones;
					MBVersion.SetMeshIndexFormatAndClearMesh(_mesh, verts.Length, vertices, justClearTriangles);
				}
				if (vertices)
				{
					Vector3[] array = verts;
					if (verts.Length != 0)
					{
						if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
						{
							targetRenderer.transform.position = Vector3.zero;
						}
						else if (base.settings.pivotLocationType == MB_MeshPivotLocation.worldOrigin)
						{
							targetRenderer.transform.position = Vector3.zero;
						}
						else if (base.settings.pivotLocationType == MB_MeshPivotLocation.boundsCenter)
						{
							Vector3 vector = verts[0];
							Vector3 vector2 = verts[0];
							for (int i = 1; i < verts.Length; i++)
							{
								Vector3 vector3 = verts[i];
								if (vector.x < vector3.x)
								{
									vector.x = vector3.x;
								}
								if (vector.y < vector3.y)
								{
									vector.y = vector3.y;
								}
								if (vector.z < vector3.z)
								{
									vector.z = vector3.z;
								}
								if (vector2.x > vector3.x)
								{
									vector2.x = vector3.x;
								}
								if (vector2.y > vector3.y)
								{
									vector2.y = vector3.y;
								}
								if (vector2.z > vector3.z)
								{
									vector2.z = vector3.z;
								}
							}
							Vector3 vector4 = (vector + vector2) / 2f;
							array = new Vector3[verts.Length];
							for (int j = 0; j < verts.Length; j++)
							{
								array[j] = verts[j] - vector4;
							}
							targetRenderer.transform.position = vector4;
						}
						else if (base.settings.pivotLocationType == MB_MeshPivotLocation.customLocation)
						{
							Vector3 vector5 = base.settings.pivotLocation;
							for (int k = 0; k < verts.Length; k++)
							{
								array[k] = verts[k] - vector5;
							}
							targetRenderer.transform.position = vector5;
						}
					}
					_mesh.vertices = array;
				}
				if (triangles && (bool)_textureBakeResults)
				{
					if (_textureBakeResults == null)
					{
						UnityEngine.Debug.LogError("Texture Bake Result was not set.");
					}
					else
					{
						SerializableIntArray[] submeshTrisWithShowHideApplied = GetSubmeshTrisWithShowHideApplied();
						int num2 = (_mesh.subMeshCount = _numNonZeroLengthSubmeshTris(submeshTrisWithShowHideApplied));
						int numNonZeroLengthSubmeshTris = num2;
						int num3 = 0;
						for (int l = 0; l < submeshTrisWithShowHideApplied.Length; l++)
						{
							if (submeshTrisWithShowHideApplied[l].data.Length != 0)
							{
								_mesh.SetTriangles(submeshTrisWithShowHideApplied[l].data, num3);
								num3++;
							}
						}
						_updateMaterialsOnTargetRenderer(submeshTrisWithShowHideApplied, numNonZeroLengthSubmeshTris);
					}
				}
				if (normals)
				{
					if (base.settings.doNorm)
					{
						_mesh.normals = this.normals;
					}
					else
					{
						UnityEngine.Debug.LogError("normal flag was set in Apply but MeshBaker didn't generate normals");
					}
				}
				if (tangents)
				{
					if (base.settings.doTan)
					{
						_mesh.tangents = this.tangents;
					}
					else
					{
						UnityEngine.Debug.LogError("tangent flag was set in Apply but MeshBaker didn't generate tangents");
					}
				}
				if (colors)
				{
					if (base.settings.doCol)
					{
						if (base.settings.assignToMeshCustomizer == null)
						{
							_mesh.colors = this.colors;
						}
						else
						{
							base.settings.assignToMeshCustomizer.meshAssign_colors(base.settings, textureBakeResults, _mesh, this.colors, uvsSliceIdx);
						}
					}
					else
					{
						UnityEngine.Debug.LogError("color flag was set in Apply but MeshBaker didn't generate colors");
					}
				}
				if (uvs)
				{
					if (base.settings.doUV)
					{
						if (base.settings.assignToMeshCustomizer == null)
						{
							_mesh.uv = this.uvs;
						}
						else
						{
							base.settings.assignToMeshCustomizer.meshAssign_UV0(0, base.settings, textureBakeResults, _mesh, this.uvs, uvsSliceIdx);
						}
					}
					else
					{
						UnityEngine.Debug.LogError("uv flag was set in Apply but MeshBaker didn't generate uvs");
					}
				}
				if (uv2)
				{
					if (doUV2())
					{
						if (base.settings.assignToMeshCustomizer == null)
						{
							_mesh.uv2 = uv2s;
						}
						else
						{
							base.settings.assignToMeshCustomizer.meshAssign_UV2(2, base.settings, textureBakeResults, _mesh, uv2s, uvsSliceIdx);
						}
					}
					else
					{
						UnityEngine.Debug.LogError("uv2 flag was set in Apply but lightmapping option was set to " + base.settings.lightmapOption);
					}
				}
				if (uv3)
				{
					if (base.settings.doUV3)
					{
						if (base.settings.assignToMeshCustomizer == null)
						{
							MBVersion.MeshAssignUVChannel(3, _mesh, uv3s);
						}
						else
						{
							base.settings.assignToMeshCustomizer.meshAssign_UV3(3, base.settings, textureBakeResults, _mesh, uv3s, uvsSliceIdx);
						}
					}
					else
					{
						UnityEngine.Debug.LogError("uv3 flag was set in Apply but MeshBaker didn't generate uv3s");
					}
				}
				if (uv4)
				{
					if (base.settings.doUV4)
					{
						if (base.settings.assignToMeshCustomizer == null)
						{
							MBVersion.MeshAssignUVChannel(4, _mesh, uv4s);
						}
						else
						{
							base.settings.assignToMeshCustomizer.meshAssign_UV4(4, base.settings, textureBakeResults, _mesh, uv4s, uvsSliceIdx);
						}
					}
					else
					{
						UnityEngine.Debug.LogError("uv4 flag was set in Apply but MeshBaker didn't generate uv4s");
					}
				}
				if (uv5)
				{
					if (base.settings.doUV5)
					{
						if (base.settings.assignToMeshCustomizer == null)
						{
							MBVersion.MeshAssignUVChannel(5, _mesh, uv5s);
						}
						else
						{
							base.settings.assignToMeshCustomizer.meshAssign_UV5(5, base.settings, textureBakeResults, _mesh, uv5s, uvsSliceIdx);
						}
					}
					else
					{
						UnityEngine.Debug.LogError("uv5 flag was set in Apply but MeshBaker didn't generate uv5s");
					}
				}
				if (uv6)
				{
					if (base.settings.doUV6)
					{
						if (base.settings.assignToMeshCustomizer == null)
						{
							MBVersion.MeshAssignUVChannel(6, _mesh, uv6s);
						}
						else
						{
							base.settings.assignToMeshCustomizer.meshAssign_UV6(6, base.settings, textureBakeResults, _mesh, uv6s, uvsSliceIdx);
						}
					}
					else
					{
						UnityEngine.Debug.LogError("uv6 flag was set in Apply but MeshBaker didn't generate uv6s");
					}
				}
				if (uv7)
				{
					if (base.settings.doUV7)
					{
						if (base.settings.assignToMeshCustomizer == null)
						{
							MBVersion.MeshAssignUVChannel(7, _mesh, uv7s);
						}
						else
						{
							base.settings.assignToMeshCustomizer.meshAssign_UV7(7, base.settings, textureBakeResults, _mesh, uv7s, uvsSliceIdx);
						}
					}
					else
					{
						UnityEngine.Debug.LogError("uv7 flag was set in Apply but MeshBaker didn't generate uv7s");
					}
				}
				if (uv8)
				{
					if (base.settings.doUV8)
					{
						if (base.settings.assignToMeshCustomizer == null)
						{
							MBVersion.MeshAssignUVChannel(8, _mesh, uv8s);
						}
						else
						{
							base.settings.assignToMeshCustomizer.meshAssign_UV8(8, base.settings, textureBakeResults, _mesh, uv8s, uvsSliceIdx);
						}
					}
					else
					{
						UnityEngine.Debug.LogError("uv8 flag was set in Apply but MeshBaker didn't generate uv8s");
					}
				}
				bool flag = false;
				if (base.settings.renderType != MB_RenderType.skinnedMeshRenderer && base.settings.lightmapOption == MB2_LightmapOptions.generate_new_UV2_layout)
				{
					if (uv2GenerationMethod != null)
					{
						uv2GenerationMethod(_mesh, base.settings.uv2UnwrappingParamsHardAngle, base.settings.uv2UnwrappingParamsPackMargin);
						if (LOG_LEVEL >= MB2_LogLevel.trace)
						{
							UnityEngine.Debug.Log("generating new UV2 layout for the combined mesh ");
						}
					}
					else
					{
						UnityEngine.Debug.LogError("No GenerateUV2Delegate method was supplied. UV2 cannot be generated.");
					}
					flag = true;
				}
				else if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer && base.settings.lightmapOption == MB2_LightmapOptions.generate_new_UV2_layout && LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("UV2 cannot be generated for SkinnedMeshRenderer objects.");
				}
				if (base.settings.renderType != MB_RenderType.skinnedMeshRenderer && base.settings.lightmapOption == MB2_LightmapOptions.generate_new_UV2_layout && !flag)
				{
					UnityEngine.Debug.LogError("Failed to generate new UV2 layout. Only works in editor.");
				}
				if (bones)
				{
					_mesh.bindposes = bindPoses;
					_mesh.boneWeights = boneWeights;
				}
				if (blendShapesFlag)
				{
					if (base.settings.smrMergeBlendShapesWithSameNames)
					{
						ApplyBlendShapeFramesToMeshAndBuildMap_MergeBlendShapesWithTheSameName();
					}
					else
					{
						ApplyBlendShapeFramesToMeshAndBuildMap();
					}
				}
				if (triangles || vertices)
				{
					if (LOG_LEVEL >= MB2_LogLevel.trace)
					{
						UnityEngine.Debug.Log("recalculating bounds on mesh.");
					}
					_mesh.RecalculateBounds();
				}
				if (base.settings.optimizeAfterBake && !Application.isPlaying)
				{
					MBVersion.OptimizeMesh(_mesh);
				}
				if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
				{
					if (verts.Length == 0)
					{
						targetRenderer.enabled = false;
					}
					else
					{
						targetRenderer.enabled = true;
					}
					bool updateWhenOffscreen = ((SkinnedMeshRenderer)targetRenderer).updateWhenOffscreen;
					((SkinnedMeshRenderer)targetRenderer).updateWhenOffscreen = true;
					((SkinnedMeshRenderer)targetRenderer).updateWhenOffscreen = updateWhenOffscreen;
					((SkinnedMeshRenderer)targetRenderer).sharedMesh = null;
					((SkinnedMeshRenderer)targetRenderer).sharedMesh = _mesh;
				}
			}
			else
			{
				UnityEngine.Debug.LogError("Need to add objects to this meshbaker before calling Apply or ApplyAll");
			}
			if (LOG_LEVEL >= MB2_LogLevel.debug)
			{
				UnityEngine.Debug.Log("Apply Complete time: " + stopwatch.ElapsedMilliseconds + " vertices: " + _mesh.vertexCount);
			}
		}

		private int _numNonZeroLengthSubmeshTris(SerializableIntArray[] subTris)
		{
			int num = 0;
			for (int i = 0; i < subTris.Length; i++)
			{
				if (subTris[i].data.Length != 0)
				{
					num++;
				}
			}
			return num;
		}

		private void _updateMaterialsOnTargetRenderer(SerializableIntArray[] subTris, int numNonZeroLengthSubmeshTris)
		{
			if (subTris.Length != textureBakeResults.NumResultMaterials())
			{
				UnityEngine.Debug.LogError("Mismatch between number of submeshes and number of result materials");
			}
			Material[] array = new Material[numNonZeroLengthSubmeshTris];
			int num = 0;
			for (int i = 0; i < subTris.Length; i++)
			{
				if (subTris[i].data.Length != 0)
				{
					array[num] = _textureBakeResults.GetCombinedMaterialForSubmesh(i);
					num++;
				}
			}
			targetRenderer.materials = array;
		}

		public SerializableIntArray[] GetSubmeshTrisWithShowHideApplied()
		{
			bool flag = false;
			for (int i = 0; i < mbDynamicObjectsInCombinedMesh.Count; i++)
			{
				if (!mbDynamicObjectsInCombinedMesh[i].show)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				int[] array = new int[submeshTris.Length];
				SerializableIntArray[] array2 = new SerializableIntArray[submeshTris.Length];
				for (int j = 0; j < mbDynamicObjectsInCombinedMesh.Count; j++)
				{
					MB_DynamicGameObject mB_DynamicGameObject = mbDynamicObjectsInCombinedMesh[j];
					if (mB_DynamicGameObject.show)
					{
						for (int k = 0; k < mB_DynamicGameObject.submeshNumTris.Length; k++)
						{
							array[k] += mB_DynamicGameObject.submeshNumTris[k];
						}
					}
				}
				for (int l = 0; l < array2.Length; l++)
				{
					array2[l] = new SerializableIntArray(array[l]);
				}
				int[] array3 = new int[array2.Length];
				for (int m = 0; m < mbDynamicObjectsInCombinedMesh.Count; m++)
				{
					MB_DynamicGameObject mB_DynamicGameObject2 = mbDynamicObjectsInCombinedMesh[m];
					if (!mB_DynamicGameObject2.show)
					{
						continue;
					}
					for (int n = 0; n < submeshTris.Length; n++)
					{
						int[] data = submeshTris[n].data;
						int num = mB_DynamicGameObject2.submeshTriIdxs[n];
						int num2 = num + mB_DynamicGameObject2.submeshNumTris[n];
						for (int num3 = num; num3 < num2; num3++)
						{
							array2[n].data[array3[n]] = data[num3];
							array3[n]++;
						}
					}
				}
				return array2;
			}
			return submeshTris;
		}

		public override bool UpdateGameObjects(GameObject[] gos, bool recalcBounds, bool updateVertices, bool updateNormals, bool updateTangents, bool updateUV, bool updateUV2, bool updateUV3, bool updateUV4, bool updateColors, bool updateSkinningInfo)
		{
			return _updateGameObjects(gos, recalcBounds, updateVertices, updateNormals, updateTangents, updateUV, updateUV2, updateUV3, updateUV4, updateUV5: false, updateUV6: false, updateUV7: false, updateUV8: false, updateColors, updateSkinningInfo);
		}

		public override bool UpdateGameObjects(GameObject[] gos, bool recalcBounds, bool updateVertices, bool updateNormals, bool updateTangents, bool updateUV, bool updateUV2, bool updateUV3, bool updateUV4, bool updateUV5, bool updateUV6, bool updateUV7, bool updateUV8, bool updateColors, bool updateSkinningInfo)
		{
			return _updateGameObjects(gos, recalcBounds, updateVertices, updateNormals, updateTangents, updateUV, updateUV2, updateUV3, updateUV4, updateUV5, updateUV6, updateUV7, updateUV8, updateColors, updateSkinningInfo);
		}

		private bool _updateGameObjects(GameObject[] gos, bool recalcBounds, bool updateVertices, bool updateNormals, bool updateTangents, bool updateUV, bool updateUV2, bool updateUV3, bool updateUV4, bool updateUV5, bool updateUV6, bool updateUV7, bool updateUV8, bool updateColors, bool updateSkinningInfo)
		{
			if (LOG_LEVEL >= MB2_LogLevel.debug)
			{
				UnityEngine.Debug.Log("UpdateGameObjects called on " + gos.Length + " objects.");
			}
			int numResultMats = 1;
			if (textureBakeResults.doMultiMaterial)
			{
				numResultMats = textureBakeResults.NumResultMaterials();
			}
			if (!_Initialize(numResultMats))
			{
				return false;
			}
			if (_mesh.vertexCount > 0 && _instance2combined_map.Count == 0)
			{
				UnityEngine.Debug.LogWarning("There were vertices in the combined mesh but nothing in the MeshBaker buffers. If you are trying to bake in the editor and modify at runtime, make sure 'Clear Buffers After Bake' is unchecked.");
			}
			bool flag = true;
			MeshChannelsCache meshChannelCache = new MeshChannelsCache(LOG_LEVEL, base.settings.lightmapOption);
			UVAdjuster_Atlas uVAdjuster = null;
			OrderedDictionary orderedDictionary = null;
			Dictionary<int, MB_Utility.MeshAnalysisResult[]> meshAnalysisResultsCache = null;
			if (updateUV)
			{
				orderedDictionary = BuildSourceMatsToSubmeshIdxMap(numResultMats);
				if (orderedDictionary == null)
				{
					return false;
				}
				uVAdjuster = new UVAdjuster_Atlas(textureBakeResults, LOG_LEVEL);
				meshAnalysisResultsCache = new Dictionary<int, MB_Utility.MeshAnalysisResult[]>();
			}
			for (int i = 0; i < gos.Length; i++)
			{
				flag = flag && _updateGameObject(gos[i], updateVertices, updateNormals, updateTangents, updateUV, updateUV2, updateUV3, updateUV4, updateUV5, updateUV6, updateUV7, updateUV8, updateColors, updateSkinningInfo, meshChannelCache, meshAnalysisResultsCache, orderedDictionary, uVAdjuster);
			}
			if (recalcBounds)
			{
				_mesh.RecalculateBounds();
			}
			return flag;
		}

		private bool _updateGameObject(GameObject go, bool updateVertices, bool updateNormals, bool updateTangents, bool updateUV, bool updateUV2, bool updateUV3, bool updateUV4, bool updateUV5, bool updateUV6, bool updateUV7, bool updateUV8, bool updateColors, bool updateSkinningInfo, MeshChannelsCache meshChannelCache, Dictionary<int, MB_Utility.MeshAnalysisResult[]> meshAnalysisResultsCache, OrderedDictionary sourceMats2submeshIdx_map, UVAdjuster_Atlas uVAdjuster)
		{
			MB_DynamicGameObject dgo = null;
			if (!instance2Combined_MapTryGetValue(go, out dgo))
			{
				UnityEngine.Debug.LogError("Object " + go.name + " has not been added");
				return false;
			}
			Mesh mesh = MB_Utility.GetMesh(go);
			if (dgo.numVerts != mesh.vertexCount)
			{
				UnityEngine.Debug.LogError("Object " + go.name + " source mesh has been modified since being added. To update it must have the same number of verts");
				return false;
			}
			if (base.settings.doUV && updateUV)
			{
				Material[] gOMaterials = MB_Utility.GetGOMaterials(go);
				if (!uVAdjuster.MapSharedMaterialsToAtlasRects(gOMaterials, checkTargetSubmeshIdxsFromPreviousBake: true, mesh, meshChannelCache, meshAnalysisResultsCache, sourceMats2submeshIdx_map, go, dgo))
				{
					return false;
				}
				uVAdjuster._copyAndAdjustUVsFromMesh(textureBakeResults, dgo, mesh, 0, dgo.vertIdx, uvs, uvsSliceIdx, meshChannelCache);
			}
			if (doUV2() && updateUV2)
			{
				_copyAndAdjustUV2FromMesh(dgo, mesh, dgo.vertIdx, meshChannelCache);
			}
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer && updateSkinningInfo)
			{
				Renderer renderer = MB_Utility.GetRenderer(go);
				BoneWeight[] array = meshChannelCache.GetBoneWeights(renderer, dgo.numVerts, dgo.isSkinnedMeshWithBones);
				Transform[] array2 = _getBones(renderer, dgo.isSkinnedMeshWithBones);
				int num = dgo.vertIdx;
				bool flag = false;
				for (int i = 0; i < array.Length; i++)
				{
					if (array2[array[i].boneIndex0] != bones[boneWeights[num].boneIndex0])
					{
						flag = true;
						break;
					}
					boneWeights[num].weight0 = array[i].weight0;
					boneWeights[num].weight1 = array[i].weight1;
					boneWeights[num].weight2 = array[i].weight2;
					boneWeights[num].weight3 = array[i].weight3;
					num++;
				}
				if (flag)
				{
					UnityEngine.Debug.LogError("Detected that some of the boneweights reference different bones than when initial added. Boneweights must reference the same bones " + dgo.name);
				}
			}
			Matrix4x4 localToWorldMatrix = go.transform.localToWorldMatrix;
			Matrix4x4 matrix4x = localToWorldMatrix;
			float num3 = (matrix4x[2, 3] = 0f);
			float value = (matrix4x[1, 3] = num3);
			matrix4x[0, 3] = value;
			matrix4x = matrix4x.inverse.transpose;
			if (updateVertices)
			{
				Vector3[] vertices = meshChannelCache.GetVertices(mesh);
				for (int j = 0; j < vertices.Length; j++)
				{
					verts[dgo.vertIdx + j] = localToWorldMatrix.MultiplyPoint3x4(vertices[j]);
				}
			}
			num3 = (localToWorldMatrix[2, 3] = 0f);
			value = (localToWorldMatrix[1, 3] = num3);
			localToWorldMatrix[0, 3] = value;
			if (base.settings.doNorm && updateNormals)
			{
				Vector3[] array3 = meshChannelCache.GetNormals(mesh);
				for (int k = 0; k < array3.Length; k++)
				{
					int num7 = dgo.vertIdx + k;
					normals[num7] = matrix4x.MultiplyPoint3x4(array3[k]).normalized;
				}
			}
			if (base.settings.doTan && updateTangents)
			{
				Vector4[] array4 = meshChannelCache.GetTangents(mesh);
				for (int l = 0; l < array4.Length; l++)
				{
					int num8 = dgo.vertIdx + l;
					float w = array4[l].w;
					tangents[num8] = matrix4x.MultiplyPoint3x4(array4[l]).normalized;
					tangents[num8].w = w;
				}
			}
			if (base.settings.doCol && updateColors)
			{
				Color[] array5 = meshChannelCache.GetColors(mesh);
				for (int m = 0; m < array5.Length; m++)
				{
					colors[dgo.vertIdx + m] = array5[m];
				}
			}
			if (base.settings.doUV3 && updateUV3)
			{
				Vector2[] uVChannel = meshChannelCache.GetUVChannel(3, mesh);
				for (int n = 0; n < uVChannel.Length; n++)
				{
					uv3s[dgo.vertIdx + n] = uVChannel[n];
				}
			}
			if (base.settings.doUV4 && updateUV4)
			{
				Vector2[] uVChannel2 = meshChannelCache.GetUVChannel(4, mesh);
				for (int num9 = 0; num9 < uVChannel2.Length; num9++)
				{
					uv4s[dgo.vertIdx + num9] = uVChannel2[num9];
				}
			}
			if (base.settings.doUV5 && updateUV5)
			{
				Vector2[] uVChannel3 = meshChannelCache.GetUVChannel(5, mesh);
				for (int num10 = 0; num10 < uVChannel3.Length; num10++)
				{
					uv5s[dgo.vertIdx + num10] = uVChannel3[num10];
				}
			}
			if (base.settings.doUV6 && updateUV6)
			{
				Vector2[] uVChannel4 = meshChannelCache.GetUVChannel(6, mesh);
				for (int num11 = 0; num11 < uVChannel4.Length; num11++)
				{
					uv6s[dgo.vertIdx + num11] = uVChannel4[num11];
				}
			}
			if (base.settings.doUV7 && updateUV7)
			{
				Vector2[] uVChannel5 = meshChannelCache.GetUVChannel(7, mesh);
				for (int num12 = 0; num12 < uVChannel5.Length; num12++)
				{
					uv7s[dgo.vertIdx + num12] = uVChannel5[num12];
				}
			}
			if (base.settings.doUV8 && updateUV8)
			{
				Vector2[] uVChannel6 = meshChannelCache.GetUVChannel(8, mesh);
				for (int num13 = 0; num13 < uVChannel6.Length; num13++)
				{
					uv8s[dgo.vertIdx + num13] = uVChannel6[num13];
				}
			}
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				((SkinnedMeshRenderer)targetRenderer).sharedMesh = null;
				((SkinnedMeshRenderer)targetRenderer).sharedMesh = _mesh;
			}
			return true;
		}

		public bool ShowHideGameObjects(GameObject[] toShow, GameObject[] toHide)
		{
			if (textureBakeResults == null)
			{
				UnityEngine.Debug.LogError("TextureBakeResults must be set.");
				return false;
			}
			return _showHide(toShow, toHide);
		}

		public override bool AddDeleteGameObjects(GameObject[] gos, GameObject[] deleteGOs, bool disableRendererInSource = true)
		{
			int[] array = null;
			if (deleteGOs != null)
			{
				array = new int[deleteGOs.Length];
				for (int i = 0; i < deleteGOs.Length; i++)
				{
					if (deleteGOs[i] == null)
					{
						UnityEngine.Debug.LogError("The " + i + "th object on the list of objects to delete is 'Null'");
					}
					else
					{
						array[i] = deleteGOs[i].GetInstanceID();
					}
				}
			}
			return AddDeleteGameObjectsByID(gos, array, disableRendererInSource);
		}

		public override bool AddDeleteGameObjectsByID(GameObject[] gos, int[] deleteGOinstanceIDs, bool disableRendererInSource)
		{
			if (validationLevel > MB2_ValidationLevel.none)
			{
				if (gos != null)
				{
					for (int i = 0; i < gos.Length; i++)
					{
						if (gos[i] == null)
						{
							UnityEngine.Debug.LogError("The " + i + "th object on the list of objects to combine is 'None'. Use Command-Delete on Mac OS X; Delete or Shift-Delete on Windows to remove this one element.");
							return false;
						}
						if (validationLevel < MB2_ValidationLevel.robust)
						{
							continue;
						}
						for (int j = i + 1; j < gos.Length; j++)
						{
							if (gos[i] == gos[j])
							{
								UnityEngine.Debug.LogError(string.Concat("GameObject ", gos[i], " appears twice in list of game objects to add"));
								return false;
							}
						}
					}
				}
				if (deleteGOinstanceIDs != null && validationLevel >= MB2_ValidationLevel.robust)
				{
					for (int k = 0; k < deleteGOinstanceIDs.Length; k++)
					{
						for (int l = k + 1; l < deleteGOinstanceIDs.Length; l++)
						{
							if (deleteGOinstanceIDs[k] == deleteGOinstanceIDs[l])
							{
								UnityEngine.Debug.LogError("GameObject " + deleteGOinstanceIDs[k] + "appears twice in list of game objects to delete");
								return false;
							}
						}
					}
				}
			}
			if (_usingTemporaryTextureBakeResult && gos != null && gos.Length != 0)
			{
				MB_Utility.Destroy(_textureBakeResults);
				_textureBakeResults = null;
				_usingTemporaryTextureBakeResult = false;
			}
			if (_textureBakeResults == null && gos != null && gos.Length != 0 && gos[0] != null && !_CreateTemporaryTextrueBakeResult(gos, GetMaterialsOnTargetRenderer()))
			{
				return false;
			}
			BuildSceneMeshObject(gos);
			if (!_addToCombined(gos, deleteGOinstanceIDs, disableRendererInSource))
			{
				UnityEngine.Debug.LogError("Failed to add/delete objects to combined mesh");
				return false;
			}
			if (targetRenderer != null)
			{
				if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
				{
					SkinnedMeshRenderer obj = (SkinnedMeshRenderer)targetRenderer;
					obj.sharedMesh = _mesh;
					obj.bones = bones;
					UpdateSkinnedMeshApproximateBoundsFromBounds();
				}
				_SetLightmapIndexIfPreserveLightmapping(targetRenderer);
			}
			return true;
		}

		public override bool CombinedMeshContains(GameObject go)
		{
			return objectsInCombinedMesh.Contains(go);
		}

		public override void ClearBuffers()
		{
			verts = new Vector3[0];
			normals = new Vector3[0];
			tangents = new Vector4[0];
			uvs = new Vector2[0];
			uvsSliceIdx = new float[0];
			uv2s = new Vector2[0];
			uv3s = new Vector2[0];
			uv4s = new Vector2[0];
			uv5s = new Vector2[0];
			uv6s = new Vector2[0];
			uv7s = new Vector2[0];
			uv8s = new Vector2[0];
			colors = new Color[0];
			bones = new Transform[0];
			bindPoses = new Matrix4x4[0];
			boneWeights = new BoneWeight[0];
			submeshTris = new SerializableIntArray[0];
			blendShapes = new MBBlendShape[0];
			blendShapesInCombined = new MBBlendShape[0];
			mbDynamicObjectsInCombinedMesh.Clear();
			objectsInCombinedMesh.Clear();
			instance2Combined_MapClear();
			if (_usingTemporaryTextureBakeResult)
			{
				MB_Utility.Destroy(_textureBakeResults);
				_textureBakeResults = null;
				_usingTemporaryTextureBakeResult = false;
			}
			if (LOG_LEVEL >= MB2_LogLevel.trace)
			{
				MB2_Log.LogDebug("ClearBuffers called");
			}
		}

		private Mesh NewMesh()
		{
			if (Application.isPlaying)
			{
				_meshBirth = MeshCreationConditions.CreatedAtRuntime;
			}
			else
			{
				_meshBirth = MeshCreationConditions.CreatedInEditor;
			}
			return new Mesh();
		}

		public override void ClearMesh()
		{
			if (_mesh != null)
			{
				MBVersion.MeshClear(_mesh, t: false);
			}
			else
			{
				_mesh = NewMesh();
			}
			ClearBuffers();
		}

		public override void ClearMesh(MB2_EditorMethodsInterface editorMethods)
		{
			ClearMesh();
		}

		public override void DisposeRuntimeCreated()
		{
			if (Application.isPlaying)
			{
				if (_meshBirth == MeshCreationConditions.CreatedAtRuntime)
				{
					UnityEngine.Object.Destroy(_mesh);
				}
				else if (_meshBirth == MeshCreationConditions.AssignedByUser)
				{
					_mesh = null;
				}
				ClearBuffers();
			}
		}

		public override void DestroyMesh()
		{
			if (_mesh != null)
			{
				if (LOG_LEVEL >= MB2_LogLevel.debug)
				{
					MB2_Log.LogDebug("Destroying Mesh");
				}
				MB_Utility.Destroy(_mesh);
				_meshBirth = MeshCreationConditions.NoMesh;
			}
			ClearBuffers();
		}

		public override void DestroyMeshEditor(MB2_EditorMethodsInterface editorMethods)
		{
			if (_mesh != null && editorMethods != null && !Application.isPlaying)
			{
				if (LOG_LEVEL >= MB2_LogLevel.debug)
				{
					MB2_Log.LogDebug("Destroying Mesh");
				}
				editorMethods.Destroy(_mesh);
			}
			ClearBuffers();
		}

		public bool ValidateTargRendererAndMeshAndResultSceneObj()
		{
			if (_resultSceneObject == null)
			{
				if (_LOG_LEVEL >= MB2_LogLevel.error)
				{
					UnityEngine.Debug.LogError("Result Scene Object was not set.");
				}
				return false;
			}
			if (_targetRenderer == null)
			{
				if (_LOG_LEVEL >= MB2_LogLevel.error)
				{
					UnityEngine.Debug.LogError("Target Renderer was not set.");
				}
				return false;
			}
			if (_targetRenderer.transform.parent != _resultSceneObject.transform)
			{
				if (_LOG_LEVEL >= MB2_LogLevel.error)
				{
					UnityEngine.Debug.LogError("Target Renderer game object is not a child of Result Scene Object was not set.");
				}
				return false;
			}
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer && !(_targetRenderer is SkinnedMeshRenderer))
			{
				if (_LOG_LEVEL >= MB2_LogLevel.error)
				{
					UnityEngine.Debug.LogError("Render Type is skinned mesh renderer but Target Renderer is not.");
				}
				return false;
			}
			if (base.settings.renderType == MB_RenderType.meshRenderer)
			{
				if (!(_targetRenderer is MeshRenderer))
				{
					if (_LOG_LEVEL >= MB2_LogLevel.error)
					{
						UnityEngine.Debug.LogError("Render Type is mesh renderer but Target Renderer is not.");
					}
					return false;
				}
				MeshFilter component = _targetRenderer.GetComponent<MeshFilter>();
				if (_mesh != component.sharedMesh)
				{
					if (_LOG_LEVEL >= MB2_LogLevel.error)
					{
						UnityEngine.Debug.LogError("Target renderer mesh is not equal to mesh.");
					}
					return false;
				}
			}
			return true;
		}

		private OrderedDictionary BuildSourceMatsToSubmeshIdxMap(int numResultMats)
		{
			OrderedDictionary orderedDictionary = new OrderedDictionary();
			for (int i = 0; i < numResultMats; i++)
			{
				List<Material> sourceMaterialsUsedByResultMaterial = _textureBakeResults.GetSourceMaterialsUsedByResultMaterial(i);
				for (int j = 0; j < sourceMaterialsUsedByResultMaterial.Count; j++)
				{
					if (sourceMaterialsUsedByResultMaterial[j] == null)
					{
						UnityEngine.Debug.LogError("Found null material in source materials for combined mesh materials " + i);
						return null;
					}
					if (!orderedDictionary.Contains(sourceMaterialsUsedByResultMaterial[j]))
					{
						orderedDictionary.Add(sourceMaterialsUsedByResultMaterial[j], i);
					}
				}
			}
			return orderedDictionary;
		}

		internal Renderer BuildSceneHierarchPreBake(MB3_MeshCombinerSingle mom, GameObject root, Mesh m, bool createNewChild = false, GameObject[] objsToBeAdded = null)
		{
			if (mom._LOG_LEVEL >= MB2_LogLevel.trace)
			{
				UnityEngine.Debug.Log("Building Scene Hierarchy createNewChild=" + createNewChild);
			}
			MeshFilter meshFilter = null;
			MeshRenderer meshRenderer = null;
			SkinnedMeshRenderer skinnedMeshRenderer = null;
			Transform transform = null;
			if (root == null)
			{
				UnityEngine.Debug.LogError("root was null.");
				return null;
			}
			if (mom.textureBakeResults == null)
			{
				UnityEngine.Debug.LogError("textureBakeResults must be set.");
				return null;
			}
			if (root.GetComponent<Renderer>() != null)
			{
				UnityEngine.Debug.LogError("root game object cannot have a renderer component");
				return null;
			}
			if (!createNewChild)
			{
				if (mom.targetRenderer != null && mom.targetRenderer.transform.parent == root.transform)
				{
					transform = mom.targetRenderer.transform;
				}
				else
				{
					Renderer[] componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
					if (componentsInChildren.Length == 1)
					{
						if (componentsInChildren[0].transform.parent != root.transform)
						{
							UnityEngine.Debug.LogError("Target Renderer is not an immediate child of Result Scene Object. Try using a game object with no children as the Result Scene Object..");
						}
						transform = componentsInChildren[0].transform;
					}
				}
			}
			if (transform != null && transform.parent != root.transform)
			{
				transform = null;
			}
			GameObject gameObject;
			if (transform == null)
			{
				gameObject = new GameObject(mom.name + "-mesh");
				gameObject.transform.parent = root.transform;
				transform = gameObject.transform;
			}
			transform.parent = root.transform;
			gameObject = transform.gameObject;
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				MeshRenderer component = gameObject.GetComponent<MeshRenderer>();
				if (component != null)
				{
					MB_Utility.Destroy(component);
				}
				MeshFilter component2 = gameObject.GetComponent<MeshFilter>();
				if (component2 != null)
				{
					MB_Utility.Destroy(component2);
				}
				skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
				if (skinnedMeshRenderer == null)
				{
					skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
				}
			}
			else
			{
				SkinnedMeshRenderer component3 = gameObject.GetComponent<SkinnedMeshRenderer>();
				if (component3 != null)
				{
					MB_Utility.Destroy(component3);
				}
				meshFilter = gameObject.GetComponent<MeshFilter>();
				if (meshFilter == null)
				{
					meshFilter = gameObject.AddComponent<MeshFilter>();
				}
				meshRenderer = gameObject.GetComponent<MeshRenderer>();
				if (meshRenderer == null)
				{
					meshRenderer = gameObject.AddComponent<MeshRenderer>();
				}
			}
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				skinnedMeshRenderer.bones = mom.GetBones();
				bool updateWhenOffscreen = skinnedMeshRenderer.updateWhenOffscreen;
				skinnedMeshRenderer.updateWhenOffscreen = true;
				skinnedMeshRenderer.updateWhenOffscreen = updateWhenOffscreen;
			}
			_ConfigureSceneHierarch(mom, root, meshRenderer, meshFilter, skinnedMeshRenderer, m, objsToBeAdded);
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				return skinnedMeshRenderer;
			}
			return meshRenderer;
		}

		public static void BuildPrefabHierarchy(MB3_MeshCombinerSingle mom, GameObject instantiatedPrefabRoot, Mesh m, bool createNewChild = false, GameObject[] objsToBeAdded = null)
		{
			SkinnedMeshRenderer skinnedMeshRenderer = null;
			MeshRenderer meshRenderer = null;
			MeshFilter meshFilter = null;
			GameObject gameObject = new GameObject(mom.name + "-mesh");
			gameObject.transform.parent = instantiatedPrefabRoot.transform;
			Transform transform = gameObject.transform;
			transform.parent = instantiatedPrefabRoot.transform;
			gameObject = transform.gameObject;
			if (mom.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				MeshRenderer component = gameObject.GetComponent<MeshRenderer>();
				if (component != null)
				{
					MB_Utility.Destroy(component);
				}
				MeshFilter component2 = gameObject.GetComponent<MeshFilter>();
				if (component2 != null)
				{
					MB_Utility.Destroy(component2);
				}
				skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
				if (skinnedMeshRenderer == null)
				{
					skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
				}
			}
			else
			{
				SkinnedMeshRenderer component3 = gameObject.GetComponent<SkinnedMeshRenderer>();
				if (component3 != null)
				{
					MB_Utility.Destroy(component3);
				}
				meshFilter = gameObject.GetComponent<MeshFilter>();
				if (meshFilter == null)
				{
					meshFilter = gameObject.AddComponent<MeshFilter>();
				}
				meshRenderer = gameObject.GetComponent<MeshRenderer>();
				if (meshRenderer == null)
				{
					meshRenderer = gameObject.AddComponent<MeshRenderer>();
				}
			}
			if (mom.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				skinnedMeshRenderer.bones = mom.GetBones();
				bool updateWhenOffscreen = skinnedMeshRenderer.updateWhenOffscreen;
				skinnedMeshRenderer.updateWhenOffscreen = true;
				skinnedMeshRenderer.updateWhenOffscreen = updateWhenOffscreen;
				skinnedMeshRenderer.sharedMesh = m;
				MB_BlendShape2CombinedMap component4 = mom._targetRenderer.GetComponent<MB_BlendShape2CombinedMap>();
				if (component4 != null)
				{
					MB_BlendShape2CombinedMap mB_BlendShape2CombinedMap = gameObject.GetComponent<MB_BlendShape2CombinedMap>();
					if (mB_BlendShape2CombinedMap == null)
					{
						mB_BlendShape2CombinedMap = gameObject.AddComponent<MB_BlendShape2CombinedMap>();
					}
					mB_BlendShape2CombinedMap.srcToCombinedMap = component4.srcToCombinedMap;
					for (int i = 0; i < mB_BlendShape2CombinedMap.srcToCombinedMap.combinedMeshTargetGameObject.Length; i++)
					{
						mB_BlendShape2CombinedMap.srcToCombinedMap.combinedMeshTargetGameObject[i] = gameObject;
					}
				}
			}
			_ConfigureSceneHierarch(mom, instantiatedPrefabRoot, meshRenderer, meshFilter, skinnedMeshRenderer, m, objsToBeAdded);
			if (mom.targetRenderer != null)
			{
				Material[] array = new Material[mom.targetRenderer.sharedMaterials.Length];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = mom.targetRenderer.sharedMaterials[j];
				}
				if (mom.settings.renderType == MB_RenderType.skinnedMeshRenderer)
				{
					skinnedMeshRenderer.sharedMaterial = null;
					skinnedMeshRenderer.sharedMaterials = array;
				}
				else
				{
					meshRenderer.sharedMaterial = null;
					meshRenderer.sharedMaterials = array;
				}
			}
		}

		private static void _ConfigureSceneHierarch(MB3_MeshCombinerSingle mom, GameObject root, MeshRenderer mr, MeshFilter mf, SkinnedMeshRenderer smr, Mesh m, GameObject[] objsToBeAdded = null)
		{
			GameObject gameObject;
			if (mom.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				gameObject = smr.gameObject;
				smr.lightmapIndex = mom.GetLightmapIndex();
			}
			else
			{
				gameObject = mr.gameObject;
				mf.sharedMesh = m;
				mom._SetLightmapIndexIfPreserveLightmapping(mr);
			}
			if (mom.settings.lightmapOption == MB2_LightmapOptions.preserve_current_lightmapping || mom.settings.lightmapOption == MB2_LightmapOptions.generate_new_UV2_layout)
			{
				gameObject.isStatic = true;
			}
			if (objsToBeAdded == null || objsToBeAdded.Length == 0 || !(objsToBeAdded[0] != null))
			{
				return;
			}
			bool flag = true;
			bool flag2 = true;
			string tag = objsToBeAdded[0].tag;
			int layer = objsToBeAdded[0].layer;
			for (int i = 0; i < objsToBeAdded.Length; i++)
			{
				if (objsToBeAdded[i] != null)
				{
					if (!objsToBeAdded[i].tag.Equals(tag))
					{
						flag = false;
					}
					if (objsToBeAdded[i].layer != layer)
					{
						flag2 = false;
					}
				}
			}
			if (flag)
			{
				root.tag = tag;
				gameObject.tag = tag;
			}
			if (flag2)
			{
				root.layer = layer;
				gameObject.layer = layer;
			}
		}

		private void _SetLightmapIndexIfPreserveLightmapping(Renderer tr)
		{
			tr.lightmapIndex = GetLightmapIndex();
			tr.lightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);
			if (base.settings.lightmapOption == MB2_LightmapOptions.preserve_current_lightmapping)
			{
				MB_PreserveLightmapData mB_PreserveLightmapData = tr.gameObject.GetComponent<MB_PreserveLightmapData>();
				if (mB_PreserveLightmapData == null)
				{
					mB_PreserveLightmapData = tr.gameObject.AddComponent<MB_PreserveLightmapData>();
				}
				mB_PreserveLightmapData.lightmapIndex = GetLightmapIndex();
				mB_PreserveLightmapData.lightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);
			}
		}

		public void BuildSceneMeshObject(GameObject[] gos = null, bool createNewChild = false)
		{
			if (_resultSceneObject == null)
			{
				_resultSceneObject = new GameObject("CombinedMesh-" + base.name);
			}
			_targetRenderer = BuildSceneHierarchPreBake(this, _resultSceneObject, GetMesh(), createNewChild, gos);
		}

		private bool IsMirrored(Matrix4x4 tm)
		{
			Vector3 lhs = tm.GetRow(0);
			Vector3 rhs = tm.GetRow(1);
			Vector3 rhs2 = tm.GetRow(2);
			lhs.Normalize();
			rhs.Normalize();
			rhs2.Normalize();
			if (!(Vector3.Dot(Vector3.Cross(lhs, rhs), rhs2) >= 0f))
			{
				return true;
			}
			return false;
		}

		public override void CheckIntegrity()
		{
			if (!MB_Utility.DO_INTEGRITY_CHECKS)
			{
				return;
			}
			if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer)
			{
				for (int i = 0; i < mbDynamicObjectsInCombinedMesh.Count; i++)
				{
					MB_DynamicGameObject mB_DynamicGameObject = mbDynamicObjectsInCombinedMesh[i];
					HashSet<int> hashSet = new HashSet<int>();
					HashSet<int> hashSet2 = new HashSet<int>();
					for (int j = mB_DynamicGameObject.vertIdx; j < mB_DynamicGameObject.vertIdx + mB_DynamicGameObject.numVerts; j++)
					{
						hashSet.Add(boneWeights[j].boneIndex0);
						hashSet.Add(boneWeights[j].boneIndex1);
						hashSet.Add(boneWeights[j].boneIndex2);
						hashSet.Add(boneWeights[j].boneIndex3);
					}
					for (int k = 0; k < mB_DynamicGameObject.indexesOfBonesUsed.Length; k++)
					{
						hashSet2.Add(mB_DynamicGameObject.indexesOfBonesUsed[k]);
					}
					hashSet2.ExceptWith(hashSet);
					if (hashSet2.Count > 0)
					{
						UnityEngine.Debug.LogError("The bone indexes were not the same. " + hashSet.Count + " " + hashSet2.Count);
					}
					for (int l = 0; l < mB_DynamicGameObject.indexesOfBonesUsed.Length; l++)
					{
						if (l < 0 || l > bones.Length)
						{
							UnityEngine.Debug.LogError("Bone index was out of bounds.");
						}
					}
					if (base.settings.renderType == MB_RenderType.skinnedMeshRenderer && mB_DynamicGameObject.indexesOfBonesUsed.Length < 1)
					{
						UnityEngine.Debug.Log("DGO had no bones");
					}
				}
			}
			if (base.settings.doBlendShapes && base.settings.renderType != MB_RenderType.skinnedMeshRenderer)
			{
				UnityEngine.Debug.LogError("Blend shapes can only be used with skinned meshes.");
			}
		}

		private void _copyUV2unchangedToSeparateRects()
		{
			int padding = 16;
			List<Vector2> list = new List<Vector2>();
			float num = 1E+11f;
			float num2 = 0f;
			for (int i = 0; i < mbDynamicObjectsInCombinedMesh.Count; i++)
			{
				float magnitude = mbDynamicObjectsInCombinedMesh[i].meshSize.magnitude;
				if (magnitude > num2)
				{
					num2 = magnitude;
				}
				if (magnitude < num)
				{
					num = magnitude;
				}
			}
			float num3 = 1000f;
			float num4 = 10f;
			float num5 = 0f;
			float num6 = 1f;
			if (num2 - num > num3 - num4)
			{
				num6 = (num3 - num4) / (num2 - num);
				num5 = num4 - num * num6;
			}
			else
			{
				num6 = num3 / num2;
			}
			for (int j = 0; j < mbDynamicObjectsInCombinedMesh.Count; j++)
			{
				float magnitude2 = mbDynamicObjectsInCombinedMesh[j].meshSize.magnitude;
				magnitude2 = magnitude2 * num6 + num5;
				Vector2 item = Vector2.one * magnitude2;
				list.Add(item);
			}
			AtlasPackingResult[] rects = new MB2_TexturePackerRegular
			{
				atlasMustBePowerOfTwo = false
			}.GetRects(list, 8192, 8192, padding);
			for (int k = 0; k < mbDynamicObjectsInCombinedMesh.Count; k++)
			{
				MB_DynamicGameObject mB_DynamicGameObject = mbDynamicObjectsInCombinedMesh[k];
				float x;
				float num7 = (x = uv2s[mB_DynamicGameObject.vertIdx].x);
				float y;
				float num8 = (y = uv2s[mB_DynamicGameObject.vertIdx].y);
				int num9 = mB_DynamicGameObject.vertIdx + mB_DynamicGameObject.numVerts;
				for (int l = mB_DynamicGameObject.vertIdx; l < num9; l++)
				{
					if (uv2s[l].x < num7)
					{
						num7 = uv2s[l].x;
					}
					if (uv2s[l].x > x)
					{
						x = uv2s[l].x;
					}
					if (uv2s[l].y < num8)
					{
						num8 = uv2s[l].y;
					}
					if (uv2s[l].y > y)
					{
						y = uv2s[l].y;
					}
				}
				Rect rect = rects[0].rects[k];
				for (int m = mB_DynamicGameObject.vertIdx; m < num9; m++)
				{
					float num10 = x - num7;
					float num11 = y - num8;
					if (num10 == 0f)
					{
						num10 = 1f;
					}
					if (num11 == 0f)
					{
						num11 = 1f;
					}
					uv2s[m].x = (uv2s[m].x - num7) / num10 * rect.width + rect.x;
					uv2s[m].y = (uv2s[m].y - num8) / num11 * rect.height + rect.y;
				}
			}
		}

		public override List<Material> GetMaterialsOnTargetRenderer()
		{
			List<Material> list = new List<Material>();
			if (_targetRenderer != null)
			{
				list.AddRange(_targetRenderer.sharedMaterials);
			}
			return list;
		}

		public static MBBlendShape[] GetBlendShapes(Mesh m, int gameObjectID, GameObject gameObject, Dictionary<int, MeshChannels> meshID2MeshChannels)
		{
			if (MBVersion.GetMajorVersion() > 5 || (MBVersion.GetMajorVersion() == 5 && MBVersion.GetMinorVersion() >= 3))
			{
				if (!meshID2MeshChannels.TryGetValue(m.GetInstanceID(), out var value))
				{
					value = new MeshChannels();
					meshID2MeshChannels.Add(m.GetInstanceID(), value);
				}
				if (value.blendShapes == null)
				{
					MBBlendShape[] array = new MBBlendShape[m.blendShapeCount];
					int vertexCount = m.vertexCount;
					for (int i = 0; i < array.Length; i++)
					{
						MBBlendShape mBBlendShape = (array[i] = new MBBlendShape());
						mBBlendShape.frames = new MBBlendShapeFrame[MBVersion.GetBlendShapeFrameCount(m, i)];
						mBBlendShape.name = m.GetBlendShapeName(i);
						mBBlendShape.indexInSource = i;
						mBBlendShape.gameObjectID = gameObjectID;
						mBBlendShape.gameObject = gameObject;
						for (int j = 0; j < mBBlendShape.frames.Length; j++)
						{
							MBBlendShapeFrame mBBlendShapeFrame = (mBBlendShape.frames[j] = new MBBlendShapeFrame());
							mBBlendShapeFrame.frameWeight = MBVersion.GetBlendShapeFrameWeight(m, i, j);
							mBBlendShapeFrame.vertices = new Vector3[vertexCount];
							mBBlendShapeFrame.normals = new Vector3[vertexCount];
							mBBlendShapeFrame.tangents = new Vector3[vertexCount];
							MBVersion.GetBlendShapeFrameVertices(m, i, j, mBBlendShapeFrame.vertices, mBBlendShapeFrame.normals, mBBlendShapeFrame.tangents);
						}
					}
					value.blendShapes = array;
					return value.blendShapes;
				}
				MBBlendShape[] array2 = new MBBlendShape[value.blendShapes.Length];
				for (int k = 0; k < array2.Length; k++)
				{
					array2[k] = new MBBlendShape();
					array2[k].name = value.blendShapes[k].name;
					array2[k].indexInSource = value.blendShapes[k].indexInSource;
					array2[k].frames = value.blendShapes[k].frames;
					array2[k].gameObjectID = gameObjectID;
					array2[k].gameObject = gameObject;
				}
				return array2;
			}
			return new MBBlendShape[0];
		}

		private void ApplyBlendShapeFramesToMeshAndBuildMap()
		{
			if (MBVersion.GetMajorVersion() <= 5 && (MBVersion.GetMajorVersion() != 5 || MBVersion.GetMinorVersion() < 3))
			{
				return;
			}
			if (blendShapesInCombined.Length != blendShapes.Length)
			{
				blendShapesInCombined = new MBBlendShape[blendShapes.Length];
			}
			Vector3[] array = new Vector3[verts.Length];
			Vector3[] array2 = new Vector3[verts.Length];
			Vector3[] array3 = new Vector3[verts.Length];
			((SkinnedMeshRenderer)_targetRenderer).sharedMesh = null;
			MBVersion.ClearBlendShapes(_mesh);
			for (int i = 0; i < blendShapes.Length; i++)
			{
				MBBlendShape mBBlendShape = blendShapes[i];
				MB_DynamicGameObject mB_DynamicGameObject = instance2Combined_MapGet(mBBlendShape.gameObject);
				if (mB_DynamicGameObject != null)
				{
					int vertIdx = mB_DynamicGameObject.vertIdx;
					for (int j = 0; j < mBBlendShape.frames.Length; j++)
					{
						MBBlendShapeFrame mBBlendShapeFrame = mBBlendShape.frames[j];
						Array.Copy(mBBlendShapeFrame.vertices, 0, array, vertIdx, mBBlendShapeFrame.vertices.Length);
						Array.Copy(mBBlendShapeFrame.normals, 0, array2, vertIdx, mBBlendShapeFrame.normals.Length);
						Array.Copy(mBBlendShapeFrame.tangents, 0, array3, vertIdx, mBBlendShapeFrame.tangents.Length);
						MBVersion.AddBlendShapeFrame(_mesh, ConvertBlendShapeNameToOutputName(mBBlendShape.name) + mBBlendShape.gameObjectID, mBBlendShapeFrame.frameWeight, array, array2, array3);
						_ZeroArray(array, vertIdx, mBBlendShapeFrame.vertices.Length);
						_ZeroArray(array2, vertIdx, mBBlendShapeFrame.normals.Length);
						_ZeroArray(array3, vertIdx, mBBlendShapeFrame.tangents.Length);
					}
				}
				else
				{
					UnityEngine.Debug.LogError("InstanceID in blend shape that was not in instance2combinedMap");
				}
				blendShapesInCombined[i] = mBBlendShape;
			}
			((SkinnedMeshRenderer)_targetRenderer).sharedMesh = null;
			((SkinnedMeshRenderer)_targetRenderer).sharedMesh = _mesh;
			if (base.settings.doBlendShapes)
			{
				MB_BlendShape2CombinedMap mB_BlendShape2CombinedMap = _targetRenderer.GetComponent<MB_BlendShape2CombinedMap>();
				if (mB_BlendShape2CombinedMap == null)
				{
					mB_BlendShape2CombinedMap = _targetRenderer.gameObject.AddComponent<MB_BlendShape2CombinedMap>();
				}
				SerializableSourceBlendShape2Combined map = mB_BlendShape2CombinedMap.GetMap();
				BuildSrcShape2CombinedMap(map, blendShapes);
			}
		}

		private string ConvertBlendShapeNameToOutputName(string bs)
		{
			string[] array = bs.Split('.');
			return array[array.Length - 1];
		}

		private void ApplyBlendShapeFramesToMeshAndBuildMap_MergeBlendShapesWithTheSameName()
		{
			if (MBVersion.GetMajorVersion() <= 5 && (MBVersion.GetMajorVersion() != 5 || MBVersion.GetMinorVersion() < 3))
			{
				return;
			}
			Vector3[] array = new Vector3[verts.Length];
			Vector3[] array2 = new Vector3[verts.Length];
			Vector3[] array3 = new Vector3[verts.Length];
			MBVersion.ClearBlendShapes(_mesh);
			bool flag = false;
			Dictionary<string, List<MBBlendShape>> dictionary = new Dictionary<string, List<MBBlendShape>>();
			for (int i = 0; i < blendShapes.Length; i++)
			{
				MBBlendShape mBBlendShape = blendShapes[i];
				string key = ConvertBlendShapeNameToOutputName(mBBlendShape.name);
				if (!dictionary.TryGetValue(key, out var value))
				{
					value = new List<MBBlendShape>();
					dictionary.Add(key, value);
				}
				value.Add(mBBlendShape);
				if (value.Count > 1 && value[0].frames.Length != mBBlendShape.frames.Length)
				{
					UnityEngine.Debug.LogError("BlendShapes with the same name must have the same number of frames.");
					flag = true;
				}
			}
			if (flag)
			{
				return;
			}
			if (blendShapesInCombined.Length != blendShapes.Length)
			{
				blendShapesInCombined = new MBBlendShape[dictionary.Keys.Count];
			}
			int num = 0;
			foreach (string key2 in dictionary.Keys)
			{
				List<MBBlendShape> list = dictionary[key2];
				MBBlendShape mBBlendShape2 = list[0];
				int num2 = mBBlendShape2.frames.Length;
				int num3 = 0;
				int num4 = 0;
				string text = "";
				for (int j = 0; j < num2; j++)
				{
					float frameWeight = mBBlendShape2.frames[j].frameWeight;
					for (int k = 0; k < list.Count; k++)
					{
						MBBlendShape mBBlendShape3 = list[k];
						int vertIdx = instance2Combined_MapGet(mBBlendShape3.gameObject).vertIdx;
						MBBlendShapeFrame mBBlendShapeFrame = mBBlendShape3.frames[j];
						Array.Copy(mBBlendShapeFrame.vertices, 0, array, vertIdx, mBBlendShapeFrame.vertices.Length);
						Array.Copy(mBBlendShapeFrame.normals, 0, array2, vertIdx, mBBlendShapeFrame.normals.Length);
						Array.Copy(mBBlendShapeFrame.tangents, 0, array3, vertIdx, mBBlendShapeFrame.tangents.Length);
						if (j == 0)
						{
							num3 += mBBlendShapeFrame.vertices.Length;
							text = text + mBBlendShape3.gameObject.name + " " + vertIdx + ":" + (vertIdx + mBBlendShapeFrame.vertices.Length) + ", ";
						}
					}
					num4 += list.Count;
					MBVersion.AddBlendShapeFrame(_mesh, key2, frameWeight, array, array2, array3);
					_ZeroArray(array, 0, array.Length);
					_ZeroArray(array2, 0, array2.Length);
					_ZeroArray(array3, 0, array3.Length);
				}
				blendShapesInCombined[num] = mBBlendShape2;
				num++;
			}
			((SkinnedMeshRenderer)_targetRenderer).sharedMesh = null;
			((SkinnedMeshRenderer)_targetRenderer).sharedMesh = _mesh;
			if (base.settings.doBlendShapes)
			{
				MB_BlendShape2CombinedMap mB_BlendShape2CombinedMap = _targetRenderer.GetComponent<MB_BlendShape2CombinedMap>();
				if (mB_BlendShape2CombinedMap == null)
				{
					mB_BlendShape2CombinedMap = _targetRenderer.gameObject.AddComponent<MB_BlendShape2CombinedMap>();
				}
				SerializableSourceBlendShape2Combined map = mB_BlendShape2CombinedMap.GetMap();
				BuildSrcShape2CombinedMap(map, blendShapesInCombined);
			}
		}

		private void BuildSrcShape2CombinedMap(SerializableSourceBlendShape2Combined map, MBBlendShape[] bs)
		{
			GameObject[] array = new GameObject[bs.Length];
			int[] array2 = new int[bs.Length];
			GameObject[] array3 = new GameObject[bs.Length];
			int[] array4 = new int[bs.Length];
			for (int i = 0; i < blendShapesInCombined.Length; i++)
			{
				array[i] = blendShapesInCombined[i].gameObject;
				array2[i] = blendShapesInCombined[i].indexInSource;
				array3[i] = _targetRenderer.gameObject;
				array4[i] = i;
			}
			map.SetBuffers(array, array2, array3, array4);
		}

		[Obsolete("BuildSourceBlendShapeToCombinedIndexMap is deprecated. The map will be now be attached to the combined SkinnedMeshRenderer object as the MB_BlendShape2CombinedMap Component.")]
		public override Dictionary<MBBlendShapeKey, MBBlendShapeValue> BuildSourceBlendShapeToCombinedIndexMap()
		{
			if (_targetRenderer == null)
			{
				return new Dictionary<MBBlendShapeKey, MBBlendShapeValue>();
			}
			MB_BlendShape2CombinedMap component = _targetRenderer.GetComponent<MB_BlendShape2CombinedMap>();
			if (component == null)
			{
				return new Dictionary<MBBlendShapeKey, MBBlendShapeValue>();
			}
			return component.srcToCombinedMap.GenerateMapFromSerializedData();
		}

		private void _ZeroArray(Vector3[] arr, int idx, int length)
		{
			int num = idx + length;
			for (int i = idx; i < num; i++)
			{
				arr[i] = Vector3.zero;
			}
		}

		public override void UpdateSkinnedMeshApproximateBounds()
		{
			UpdateSkinnedMeshApproximateBoundsFromBounds();
		}

		public override void UpdateSkinnedMeshApproximateBoundsFromBones()
		{
			if (outputOption == MB2_OutputOptions.bakeMeshAssetsInPlace)
			{
				if (LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("Can't UpdateSkinnedMeshApproximateBounds when output type is bakeMeshAssetsInPlace");
				}
			}
			else if (bones.Length == 0)
			{
				if (verts.Length != 0 && LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("No bones in SkinnedMeshRenderer. Could not UpdateSkinnedMeshApproximateBounds.");
				}
			}
			else if (_targetRenderer == null)
			{
				if (LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("Target Renderer is not set. No point in calling UpdateSkinnedMeshApproximateBounds.");
				}
			}
			else if (!_targetRenderer.GetType().Equals(typeof(SkinnedMeshRenderer)))
			{
				if (LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("Target Renderer is not a SkinnedMeshRenderer. No point in calling UpdateSkinnedMeshApproximateBounds.");
				}
			}
			else
			{
				MB3_MeshCombiner.UpdateSkinnedMeshApproximateBoundsFromBonesStatic(bones, (SkinnedMeshRenderer)targetRenderer);
			}
		}

		public override void UpdateSkinnedMeshApproximateBoundsFromBounds()
		{
			if (outputOption == MB2_OutputOptions.bakeMeshAssetsInPlace)
			{
				if (LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("Can't UpdateSkinnedMeshApproximateBoundsFromBounds when output type is bakeMeshAssetsInPlace");
				}
			}
			else if (verts.Length == 0 || mbDynamicObjectsInCombinedMesh.Count == 0)
			{
				if (verts.Length != 0 && LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("Nothing in SkinnedMeshRenderer. CoulddoBlendShapes not UpdateSkinnedMeshApproximateBoundsFromBounds.");
				}
			}
			else if (_targetRenderer == null)
			{
				if (LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("Target Renderer is not set. No point in calling UpdateSkinnedMeshApproximateBoundsFromBounds.");
				}
			}
			else if (!_targetRenderer.GetType().Equals(typeof(SkinnedMeshRenderer)))
			{
				if (LOG_LEVEL >= MB2_LogLevel.warn)
				{
					UnityEngine.Debug.LogWarning("Target Renderer is not a SkinnedMeshRenderer. No point in calling UpdateSkinnedMeshApproximateBoundsFromBounds.");
				}
			}
			else
			{
				MB3_MeshCombiner.UpdateSkinnedMeshApproximateBoundsFromBoundsStatic(objectsInCombinedMesh, (SkinnedMeshRenderer)targetRenderer);
			}
		}
	}
}
