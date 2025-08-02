using UnityEngine;

public static class ShaderPlatformSetter
{
	[RuntimeInitializeOnLoadMethod]
	public static void HandleRuntimeInitializeOnLoad()
	{
		Shader.DisableKeyword("PLATFORM_IS_ANDROID");
	}
}
