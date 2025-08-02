using UnityEngine;

[ExecuteInEditMode]
public class BakeryVolume : MonoBehaviour
{
	public enum Encoding
	{
		Half4 = 0,
		RGBA8 = 1,
		RGBA8Mono = 2
	}

	public enum ShadowmaskEncoding
	{
		RGBA8 = 0,
		A8 = 1
	}

	public bool enableBaking = true;

	public Bounds bounds = new Bounds(Vector3.zero, Vector3.one);

	public bool adaptiveRes = true;

	public float voxelsPerUnit = 0.5f;

	public int resolutionX = 16;

	public int resolutionY = 16;

	public int resolutionZ = 16;

	public Encoding encoding;

	public ShadowmaskEncoding shadowmaskEncoding;

	public bool denoise;

	public bool isGlobal;

	public Texture3D bakedTexture0;

	public Texture3D bakedTexture1;

	public Texture3D bakedTexture2;

	public Texture3D bakedTexture3;

	public Texture3D bakedMask;

	public bool supportRotationAfterBake;

	public static BakeryVolume globalVolume;

	private Transform tform;

	public Vector3 GetMin()
	{
		return bounds.min;
	}

	public Vector3 GetInvSize()
	{
		Bounds bounds = this.bounds;
		return new Vector3(1f / bounds.size.x, 1f / bounds.size.y, 1f / bounds.size.z);
	}

	public Matrix4x4 GetMatrix()
	{
		if (tform == null)
		{
			tform = base.transform;
		}
		return Matrix4x4.TRS(tform.position, tform.rotation, Vector3.one).inverse;
	}

	public void SetGlobalParams()
	{
		Shader.SetGlobalTexture("_Volume0", bakedTexture0);
		Shader.SetGlobalTexture("_Volume1", bakedTexture1);
		Shader.SetGlobalTexture("_Volume2", bakedTexture2);
		if (bakedTexture3 != null)
		{
			Shader.SetGlobalTexture("_Volume3", bakedTexture3);
		}
		Shader.SetGlobalTexture("_VolumeMask", bakedMask);
		Bounds bounds = this.bounds;
		Vector3 min = bounds.min;
		Vector3 vector = new Vector3(1f / bounds.size.x, 1f / bounds.size.y, 1f / bounds.size.z);
		Shader.SetGlobalVector("_GlobalVolumeMin", min);
		Shader.SetGlobalVector("_GlobalVolumeInvSize", vector);
		if (supportRotationAfterBake)
		{
			Shader.SetGlobalMatrix("_GlobalVolumeMatrix", GetMatrix());
		}
	}

	public void UpdateBounds()
	{
		Vector3 position = base.transform.position;
		Vector3 size = bounds.size;
		bounds = new Bounds(position, size);
	}

	public void OnEnable()
	{
		if (isGlobal)
		{
			globalVolume = this;
			SetGlobalParams();
		}
	}
}
