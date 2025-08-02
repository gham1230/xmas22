using System.Collections.Generic;
using DigitalOpus.MB.Core;
using UnityEngine;

public abstract class MB3_MeshBakerCommon : MB3_MeshBakerRoot
{
	public List<GameObject> objsToMesh;

	public bool useObjsToMeshFromTexBaker = true;

	public bool clearBuffersAfterBake = true;

	public string bakeAssetsInPlaceFolderPath;

	[HideInInspector]
	public GameObject resultPrefab;

	[HideInInspector]
	public bool resultPrefabLeaveInstanceInSceneAfterBake;

	[HideInInspector]
	public Transform parentSceneObject;

	public abstract MB3_MeshCombiner meshCombiner { get; }

	public override MB2_TextureBakeResults textureBakeResults
	{
		get
		{
			return meshCombiner.textureBakeResults;
		}
		set
		{
			meshCombiner.textureBakeResults = value;
		}
	}

	public List<GameObject> GetObjectsToCombineDirty()
	{
		if (useObjsToMeshFromTexBaker)
		{
			MB3_TextureBaker component = base.gameObject.GetComponent<MB3_TextureBaker>();
			if (component == null)
			{
				component = base.gameObject.transform.parent.GetComponent<MB3_TextureBaker>();
			}
			if (component != null)
			{
				return component.GetObjectsToCombine();
			}
			Debug.LogWarning("Use Objects To Mesh From Texture Baker was checked but no texture baker");
			return new List<GameObject>();
		}
		if (objsToMesh == null)
		{
			objsToMesh = new List<GameObject>();
		}
		return objsToMesh;
	}

	public override List<GameObject> GetObjectsToCombine()
	{
		List<GameObject> list = new List<GameObject>();
		if (useObjsToMeshFromTexBaker)
		{
			MB3_TextureBaker component = base.gameObject.GetComponent<MB3_TextureBaker>();
			if (component == null)
			{
				component = base.gameObject.transform.parent.GetComponent<MB3_TextureBaker>();
			}
			if (component != null)
			{
				list = new List<GameObject>(component.GetObjectsToCombine());
			}
			else
			{
				Debug.LogWarning("Use Objects To Mesh From Texture Baker was checked but no texture baker");
			}
		}
		else
		{
			if (objsToMesh == null)
			{
				objsToMesh = new List<GameObject>();
			}
			list = new List<GameObject>(objsToMesh);
		}
		if (list.Count > 0)
		{
			for (int num = list.Count - 1; num >= 0; num--)
			{
			}
			return list;
		}
		return objsToMesh;
	}

	[ContextMenu("Purge Objects to Combine of null references")]
	public override void PurgeNullsFromObjectsToCombine()
	{
		if (useObjsToMeshFromTexBaker)
		{
			MB3_TextureBaker component = base.gameObject.GetComponent<MB3_TextureBaker>();
			if (component == null)
			{
				component = base.gameObject.transform.parent.GetComponent<MB3_TextureBaker>();
			}
			if (component != null)
			{
				component.PurgeNullsFromObjectsToCombine();
			}
			else
			{
				Debug.LogWarning("Use Objects To Mesh From Texture Baker was checked but no texture baker, could not purge");
			}
		}
		else
		{
			if (objsToMesh == null)
			{
				objsToMesh = new List<GameObject>();
			}
			Debug.Log($"Purged {objsToMesh.RemoveAll((GameObject obj) => obj == null)} null references from objects to combine list.");
		}
	}

	public void EnableDisableSourceObjectRenderers(bool show)
	{
		for (int i = 0; i < GetObjectsToCombine().Count; i++)
		{
			GameObject gameObject = GetObjectsToCombine()[i];
			if (!(gameObject != null))
			{
				continue;
			}
			Renderer renderer = MB_Utility.GetRenderer(gameObject);
			if (renderer != null)
			{
				renderer.enabled = show;
			}
			Debug.Log(renderer, base.gameObject);
			if (!(renderer != null))
			{
				continue;
			}
			LODGroup componentInParent = renderer.GetComponentInParent<LODGroup>();
			if (!(componentInParent != null))
			{
				continue;
			}
			bool flag = true;
			LOD[] lODs = componentInParent.GetLODs();
			for (int j = 0; j < lODs.Length; j++)
			{
				for (int k = 0; k < lODs[j].renderers.Length; k++)
				{
					if (lODs[j].renderers[k] != renderer)
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				componentInParent.enabled = show;
			}
		}
	}

	public virtual void ClearMesh()
	{
		meshCombiner.ClearMesh();
	}

	public virtual void ClearMesh(MB2_EditorMethodsInterface editorMethods)
	{
		meshCombiner.ClearMesh(editorMethods);
	}

	public virtual void DestroyMesh()
	{
		meshCombiner.DestroyMesh();
	}

	public virtual void DestroyMeshEditor(MB2_EditorMethodsInterface editorMethods)
	{
		meshCombiner.DestroyMeshEditor(editorMethods);
	}

	public virtual int GetNumObjectsInCombined()
	{
		return meshCombiner.GetNumObjectsInCombined();
	}

	public virtual int GetNumVerticesFor(GameObject go)
	{
		return meshCombiner.GetNumVerticesFor(go);
	}

	public MB3_TextureBaker GetTextureBaker()
	{
		MB3_TextureBaker component = GetComponent<MB3_TextureBaker>();
		if (component != null)
		{
			return component;
		}
		if (base.transform.parent != null)
		{
			return base.transform.parent.GetComponent<MB3_TextureBaker>();
		}
		return null;
	}

	public abstract bool AddDeleteGameObjects(GameObject[] gos, GameObject[] deleteGOs, bool disableRendererInSource = true);

	public abstract bool AddDeleteGameObjectsByID(GameObject[] gos, int[] deleteGOinstanceIDs, bool disableRendererInSource = true);

	public virtual void Apply(MB3_MeshCombiner.GenerateUV2Delegate uv2GenerationMethod = null)
	{
		meshCombiner.name = base.name + "-mesh";
		meshCombiner.Apply(uv2GenerationMethod);
		if (parentSceneObject != null && meshCombiner.resultSceneObject != null)
		{
			meshCombiner.resultSceneObject.transform.parent = parentSceneObject;
		}
	}

	public virtual void Apply(bool triangles, bool vertices, bool normals, bool tangents, bool uvs, bool uv2, bool uv3, bool uv4, bool colors, bool bones = false, bool blendShapesFlag = false, MB3_MeshCombiner.GenerateUV2Delegate uv2GenerationMethod = null)
	{
		meshCombiner.name = base.name + "-mesh";
		meshCombiner.Apply(triangles, vertices, normals, tangents, uvs, uv2, uv3, uv4, colors, bones, blendShapesFlag, uv2GenerationMethod);
		if (parentSceneObject != null && meshCombiner.resultSceneObject != null)
		{
			meshCombiner.resultSceneObject.transform.parent = parentSceneObject;
		}
	}

	public virtual bool CombinedMeshContains(GameObject go)
	{
		return meshCombiner.CombinedMeshContains(go);
	}

	public virtual void UpdateGameObjects(GameObject[] gos)
	{
		meshCombiner.name = base.name + "-mesh";
		meshCombiner.UpdateGameObjects(gos, recalcBounds: true, updateVertices: true, updateNormals: true, updateTangents: true, updateUV: true, updateUV2: false, updateUV3: false, updateUV4: false, updateUV5: false, updateUV6: false, updateUV7: false, updateUV8: false, updateColors: false, updateSkinningInfo: false);
	}

	public virtual void UpdateGameObjects(GameObject[] gos, bool updateBounds)
	{
		meshCombiner.name = base.name + "-mesh";
		meshCombiner.UpdateGameObjects(gos, recalcBounds: true, updateVertices: true, updateNormals: true, updateTangents: true, updateUV: true, updateUV2: false, updateUV3: false, updateUV4: false, updateUV5: false, updateUV6: false, updateUV7: false, updateUV8: false, updateColors: false, updateSkinningInfo: false);
	}

	public virtual void UpdateGameObjects(GameObject[] gos, bool recalcBounds, bool updateVertices, bool updateNormals, bool updateTangents, bool updateUV, bool updateUV1, bool updateUV2, bool updateColors, bool updateSkinningInfo)
	{
		meshCombiner.name = base.name + "-mesh";
		meshCombiner.UpdateGameObjects(gos, recalcBounds, updateVertices, updateNormals, updateTangents, updateUV, updateUV2, updateUV3: false, updateUV4: false, updateColors, updateSkinningInfo);
	}

	public virtual bool UpdateGameObjects(GameObject[] gos, bool recalcBounds, bool updateVertices, bool updateNormals, bool updateTangents, bool updateUV, bool updateUV2, bool updateUV3, bool updateUV4, bool updateUV5, bool updateUV6, bool updateUV7, bool updateUV8, bool updateColors, bool updateSkinningInfo)
	{
		meshCombiner.name = base.name + "-mesh";
		return meshCombiner.UpdateGameObjects(gos, recalcBounds, updateVertices, updateNormals, updateTangents, updateUV, updateUV2, updateUV3, updateUV4, updateUV5, updateUV6, updateUV7, updateUV8, updateColors, updateSkinningInfo);
	}

	public virtual void UpdateSkinnedMeshApproximateBounds()
	{
		if (_ValidateForUpdateSkinnedMeshBounds())
		{
			meshCombiner.UpdateSkinnedMeshApproximateBounds();
		}
	}

	public virtual void UpdateSkinnedMeshApproximateBoundsFromBones()
	{
		if (_ValidateForUpdateSkinnedMeshBounds())
		{
			meshCombiner.UpdateSkinnedMeshApproximateBoundsFromBones();
		}
	}

	public virtual void UpdateSkinnedMeshApproximateBoundsFromBounds()
	{
		if (_ValidateForUpdateSkinnedMeshBounds())
		{
			meshCombiner.UpdateSkinnedMeshApproximateBoundsFromBounds();
		}
	}

	protected virtual bool _ValidateForUpdateSkinnedMeshBounds()
	{
		if (meshCombiner.outputOption == MB2_OutputOptions.bakeMeshAssetsInPlace)
		{
			Debug.LogWarning("Can't UpdateSkinnedMeshApproximateBounds when output type is bakeMeshAssetsInPlace");
			return false;
		}
		if (meshCombiner.resultSceneObject == null)
		{
			Debug.LogWarning("Result Scene Object does not exist. No point in calling UpdateSkinnedMeshApproximateBounds.");
			return false;
		}
		if (meshCombiner.resultSceneObject.GetComponentInChildren<SkinnedMeshRenderer>() == null)
		{
			Debug.LogWarning("No SkinnedMeshRenderer on result scene object.");
			return false;
		}
		return true;
	}
}
