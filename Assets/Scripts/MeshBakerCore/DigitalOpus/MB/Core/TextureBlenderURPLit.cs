using System;
using UnityEngine;

namespace DigitalOpus.MB.Core
{
	public class TextureBlenderURPLit : TextureBlender
	{
		private enum Prop
		{
			doColor = 0,
			doSpecular = 1,
			doMetallic = 2,
			doEmission = 3,
			doBump = 4,
			doNone = 5
		}

		private enum WorkflowMode
		{
			unknown = 0,
			metallic = 1,
			specular = 2
		}

		private enum SmoothnessTextureChannel
		{
			unknown = 0,
			albedo = 1,
			metallicSpecular = 2
		}

		private static Color NeutralNormalMap = new Color(0.5f, 0.5f, 1f);

		private TextureBlenderMaterialPropertyCacheHelper sourceMaterialPropertyCache = new TextureBlenderMaterialPropertyCacheHelper();

		private WorkflowMode m_workflowMode;

		private SmoothnessTextureChannel m_smoothnessTextureChannel;

		private Color m_tintColor;

		private float m_smoothness;

		private Color m_specColor;

		private bool m_hasSpecGlossMap;

		private float m_metallic;

		private bool m_hasMetallicGlossMap;

		private float m_bumpScale;

		private bool m_shaderDoesEmission;

		private Color m_emissionColor;

		private Prop propertyToDo = Prop.doNone;

		private Color m_generatingTintedAtlaColor = Color.white;

		private float m_generatingTintedAtlasMetallic;

		private Color m_generatingTintedAtlaSpecular = Color.black;

		private float m_generatingTintedAtlasMetallic_smoothness = 1f;

		private float m_generatingTintedAtlasSpecular_somoothness = 1f;

		private float m_generatingTintedAtlaBumpScale = 1f;

		private Color m_generatingTintedAtlaEmission = Color.white;

		private Color m_notGeneratingAtlasDefaultColor = Color.white;

		private float m_notGeneratingAtlasDefaultMetallic;

		private float m_notGeneratingAtlasDefaultSmoothness_MetallicWorkflow;

		private float m_notGeneratingAtlasDefaultSmoothness_SpecularWorkflow = 1f;

		private Color m_notGeneratingAtlasDefaultSpecularColor = new Color(0.2f, 0.2f, 0.2f, 1f);

		private Color m_notGeneratingAtlasDefaultEmisionColor = Color.black;

		public bool DoesShaderNameMatch(string shaderName)
		{
			if (!shaderName.Equals("Universal Render Pipeline/Lit") && !shaderName.Equals("Universal Render Pipeline/Simple Lit"))
			{
				return shaderName.Equals("Universal Render Pipeline/Baked Lit");
			}
			return true;
		}

		private WorkflowMode _MapFloatToWorkflowMode(float workflowMode)
		{
			if (workflowMode == 0f)
			{
				return WorkflowMode.specular;
			}
			return WorkflowMode.metallic;
		}

		private float _MapWorkflowModeToFloat(WorkflowMode workflowMode)
		{
			if (workflowMode == WorkflowMode.specular)
			{
				return 0f;
			}
			return 1f;
		}

		private SmoothnessTextureChannel _MapFloatToTextureChannel(float texChannel)
		{
			if (texChannel == 0f)
			{
				return SmoothnessTextureChannel.metallicSpecular;
			}
			return SmoothnessTextureChannel.albedo;
		}

		private float _MapTextureChannelToFloat(SmoothnessTextureChannel workflowMode)
		{
			if (workflowMode == SmoothnessTextureChannel.metallicSpecular)
			{
				return 0f;
			}
			return 1f;
		}

		public void OnBeforeTintTexture(Material sourceMat, string shaderTexturePropertyName)
		{
			if (m_workflowMode == WorkflowMode.unknown)
			{
				if (sourceMat.HasProperty("_WorkflowMode"))
				{
					m_workflowMode = _MapFloatToWorkflowMode(sourceMat.GetFloat("_WorkflowMode"));
				}
			}
			else if (sourceMat.HasProperty("_WorkflowMode") && _MapFloatToWorkflowMode(sourceMat.GetFloat("_WorkflowMode")) != m_workflowMode)
			{
				Debug.LogError("Using the Universal Render Pipeline TextureBlender to blend non-texture-propertyes. Some of the source materials used different 'WorkflowModes'. These  cannot be blended properly. Results will be unpredictable.");
			}
			if (m_smoothnessTextureChannel == SmoothnessTextureChannel.unknown)
			{
				if (sourceMat.HasProperty("_SmoothnessTextureChannel"))
				{
					m_smoothnessTextureChannel = _MapFloatToTextureChannel(sourceMat.GetFloat("_SmoothnessTextureChannel"));
				}
			}
			else if (sourceMat.HasProperty("_SmoothnessTextureChannel") && _MapFloatToTextureChannel(sourceMat.GetFloat("_SmoothnessTextureChannel")) != m_smoothnessTextureChannel)
			{
				Debug.LogError("Using the Universal Render Pipeline TextureBlender to blend non-texture-properties. Some of the source materials store smoothness in the Albedo texture alpha and some source materials store smoothness in the Metallic/Specular texture alpha channel. The result material can only read smoothness from one or the other. Results will be unpredictable.");
			}
			if (shaderTexturePropertyName.Equals("_BaseMap"))
			{
				propertyToDo = Prop.doColor;
				if (sourceMat.HasProperty("_BaseColor"))
				{
					m_tintColor = sourceMat.GetColor("_BaseColor");
				}
				else
				{
					m_tintColor = m_generatingTintedAtlaColor;
				}
			}
			else if (shaderTexturePropertyName.Equals("_SpecGlossMap"))
			{
				propertyToDo = Prop.doSpecular;
				m_specColor = m_generatingTintedAtlaSpecular;
				if (sourceMat.GetTexture("_SpecGlossMap") != null)
				{
					m_hasSpecGlossMap = true;
				}
				else
				{
					m_hasSpecGlossMap = false;
				}
				if (sourceMat.HasProperty("_SpecColor"))
				{
					m_specColor = sourceMat.GetColor("_SpecColor");
				}
				else
				{
					m_specColor = new Color(0f, 0f, 0f, 1f);
				}
				if (sourceMat.HasProperty("_Smoothness") && m_workflowMode == WorkflowMode.specular)
				{
					m_smoothness = sourceMat.GetFloat("_Smoothness");
					Debug.LogError(string.Concat("TODO smooth ", sourceMat, "  ", m_smoothness));
				}
				else if (m_workflowMode == WorkflowMode.specular)
				{
					m_smoothness = 1f;
				}
			}
			else if (shaderTexturePropertyName.Equals("_MetallicGlossMap"))
			{
				propertyToDo = Prop.doMetallic;
				if (sourceMat.GetTexture("_MetallicGlossMap") != null)
				{
					m_hasMetallicGlossMap = true;
				}
				else
				{
					m_hasMetallicGlossMap = false;
				}
				if (sourceMat.HasProperty("_Metallic"))
				{
					m_metallic = sourceMat.GetFloat("_Metallic");
				}
				else
				{
					m_metallic = 0f;
				}
				if (sourceMat.HasProperty("_Smoothness") && m_workflowMode == WorkflowMode.metallic)
				{
					m_smoothness = sourceMat.GetFloat("_Smoothness");
				}
				else if (m_workflowMode == WorkflowMode.metallic)
				{
					m_smoothness = 0f;
				}
			}
			else if (shaderTexturePropertyName.Equals("_BumpMap"))
			{
				propertyToDo = Prop.doBump;
				if (sourceMat.HasProperty(shaderTexturePropertyName))
				{
					if (sourceMat.HasProperty("_BumpScale"))
					{
						m_bumpScale = sourceMat.GetFloat("_BumpScale");
					}
				}
				else
				{
					m_bumpScale = m_generatingTintedAtlaBumpScale;
				}
			}
			else if (shaderTexturePropertyName.Equals("_EmissionMap"))
			{
				propertyToDo = Prop.doEmission;
				m_shaderDoesEmission = sourceMat.IsKeywordEnabled("_EMISSION");
				if (sourceMat.HasProperty("_EmissionColor"))
				{
					m_emissionColor = sourceMat.GetColor("_EmissionColor");
				}
				else
				{
					m_generatingTintedAtlaColor = m_notGeneratingAtlasDefaultEmisionColor;
				}
			}
			else
			{
				propertyToDo = Prop.doNone;
			}
		}

		public Color OnBlendTexturePixel(string propertyToDoshaderPropertyName, Color pixelColor)
		{
			if (propertyToDo == Prop.doColor)
			{
				return new Color(pixelColor.r * m_tintColor.r, pixelColor.g * m_tintColor.g, pixelColor.b * m_tintColor.b, pixelColor.a * m_tintColor.a);
			}
			if (propertyToDo == Prop.doMetallic)
			{
				if (m_hasMetallicGlossMap)
				{
					pixelColor = new Color(pixelColor.r, pixelColor.g, pixelColor.b, pixelColor.a * m_smoothness);
					return pixelColor;
				}
				return new Color(m_metallic, 0f, 0f, m_smoothness);
			}
			if (propertyToDo == Prop.doSpecular)
			{
				if (m_hasSpecGlossMap)
				{
					pixelColor = new Color(pixelColor.r, pixelColor.g, pixelColor.b, pixelColor.a * m_smoothness);
					return pixelColor;
				}
				Color specColor = m_specColor;
				specColor.a = m_smoothness;
				return specColor;
			}
			if (propertyToDo == Prop.doBump)
			{
				return Color.Lerp(NeutralNormalMap, pixelColor, m_bumpScale);
			}
			if (propertyToDo == Prop.doEmission)
			{
				if (m_shaderDoesEmission)
				{
					return new Color(pixelColor.r * m_emissionColor.r, pixelColor.g * m_emissionColor.g, pixelColor.b * m_emissionColor.b, pixelColor.a * m_emissionColor.a);
				}
				return Color.black;
			}
			return pixelColor;
		}

		public bool NonTexturePropertiesAreEqual(Material a, Material b)
		{
			if (!TextureBlenderFallback._compareColor(a, b, m_generatingTintedAtlaColor, "_BaseColor"))
			{
				return false;
			}
			if (!TextureBlenderFallback._compareColor(a, b, m_generatingTintedAtlaSpecular, "_SpecColor"))
			{
				return false;
			}
			if (m_workflowMode == WorkflowMode.specular)
			{
				bool flag = a.HasProperty("_SpecGlossMap") && a.GetTexture("_SpecGlossMap") != null;
				bool flag2 = b.HasProperty("_SpecGlossMap") && b.GetTexture("_SpecGlossMap") != null;
				if (flag && flag2)
				{
					if (!TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultSmoothness_SpecularWorkflow, "_Smoothness"))
					{
						Debug.LogError("Are equal A");
						return false;
					}
				}
				else
				{
					if (flag || flag2)
					{
						Debug.LogError("Are equal C");
						return false;
					}
					if (!TextureBlenderFallback._compareColor(a, b, m_notGeneratingAtlasDefaultSpecularColor, "_SpecColor") && !TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultSmoothness_SpecularWorkflow, "_Smoothness"))
					{
						Debug.LogError("Are equal B");
						return false;
					}
				}
			}
			if (m_workflowMode == WorkflowMode.metallic)
			{
				bool flag3 = a.HasProperty("_MetallicGlossMap") && a.GetTexture("_MetallicGlossMap") != null;
				bool flag4 = b.HasProperty("_MetallicGlossMap") && b.GetTexture("_MetallicGlossMap") != null;
				if (flag3 && flag4)
				{
					if (!TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultSmoothness_MetallicWorkflow, "_Smoothness"))
					{
						return false;
					}
				}
				else
				{
					if (flag3 || flag4)
					{
						return false;
					}
					if (!TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultMetallic, "_Metallic") && !TextureBlenderFallback._compareFloat(a, b, m_notGeneratingAtlasDefaultSmoothness_MetallicWorkflow, "_Smoothness"))
					{
						return false;
					}
				}
			}
			if (!TextureBlenderFallback._compareFloat(a, b, m_generatingTintedAtlaBumpScale, "_BumpScale"))
			{
				return false;
			}
			if (a.IsKeywordEnabled("_EMISSION") != b.IsKeywordEnabled("_EMISSION"))
			{
				return false;
			}
			if (a.IsKeywordEnabled("_EMISSION") && !TextureBlenderFallback._compareColor(a, b, m_generatingTintedAtlaEmission, "_EmissionColor"))
			{
				return false;
			}
			return true;
		}

		public void SetNonTexturePropertyValuesOnResultMaterial(Material resultMaterial)
		{
			if (m_workflowMode != 0)
			{
				resultMaterial.SetFloat("_WorkflowMode", _MapWorkflowModeToFloat(m_workflowMode));
			}
			if (m_smoothnessTextureChannel != 0)
			{
				resultMaterial.SetFloat("_SmoothnessTextureChannel", _MapTextureChannelToFloat(m_smoothnessTextureChannel));
			}
			if (resultMaterial.GetTexture("_BaseMap") != null)
			{
				resultMaterial.SetColor("_BaseColor", m_generatingTintedAtlaColor);
			}
			else
			{
				resultMaterial.SetColor("_BaseColor", (Color)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_BaseColor", m_notGeneratingAtlasDefaultColor));
			}
			if (m_workflowMode == WorkflowMode.specular)
			{
				if (resultMaterial.GetTexture("_SpecGlossMap") != null)
				{
					Debug.LogError("Setting A " + m_smoothness);
					resultMaterial.SetColor("_SpecColor", m_generatingTintedAtlaSpecular);
					resultMaterial.SetFloat("_Smoothness", m_generatingTintedAtlasSpecular_somoothness);
				}
				else
				{
					Debug.LogError("Setting B " + m_smoothness);
					resultMaterial.SetColor("_SpecColor", (Color)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_SpecColor", m_notGeneratingAtlasDefaultSpecularColor));
					resultMaterial.SetFloat("_Smoothness", m_smoothness);
				}
			}
			if (m_workflowMode == WorkflowMode.metallic)
			{
				if (resultMaterial.GetTexture("_MetallicGlossMap") != null)
				{
					resultMaterial.SetFloat("_Metallic", m_generatingTintedAtlasMetallic);
					resultMaterial.SetFloat("_Smoothness", m_generatingTintedAtlasMetallic_smoothness);
				}
				else
				{
					resultMaterial.SetFloat("_Metallic", (float)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_Metallic", m_notGeneratingAtlasDefaultMetallic));
					resultMaterial.SetFloat("_Smoothness", m_smoothness);
				}
			}
			if (resultMaterial.GetTexture("_BumpMap") != null)
			{
				resultMaterial.SetFloat("_BumpScale", m_generatingTintedAtlaBumpScale);
			}
			else
			{
				resultMaterial.SetFloat("_BumpScale", m_generatingTintedAtlaBumpScale);
			}
			if (resultMaterial.GetTexture("_EmissionMap") != null)
			{
				resultMaterial.EnableKeyword("_EMISSION");
				resultMaterial.SetColor("_EmissionColor", Color.white);
			}
			else
			{
				resultMaterial.DisableKeyword("_EMISSION");
				resultMaterial.SetColor("_EmissionColor", (Color)sourceMaterialPropertyCache.GetValueIfAllSourceAreTheSameOrDefault("_EmissionColor", m_notGeneratingAtlasDefaultEmisionColor));
			}
		}

		public Color GetColorIfNoTexture(Material mat, ShaderTextureProperty texPropertyName)
		{
			if (texPropertyName.name.Equals("_BumpMap"))
			{
				return new Color(0.5f, 0.5f, 1f);
			}
			if (texPropertyName.name.Equals("_BaseMap"))
			{
				if (mat != null && mat.HasProperty("_BaseColor"))
				{
					try
					{
						Color color = mat.GetColor("_BaseColor");
						sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_BaseColor", color);
					}
					catch (Exception)
					{
					}
					return Color.white;
				}
			}
			else
			{
				if (texPropertyName.name.Equals("_SpecGlossMap"))
				{
					if (mat != null && mat.HasProperty("_SpecColor"))
					{
						try
						{
							Color color2 = mat.GetColor("_SpecColor");
							sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_SpecColor", color2);
						}
						catch (Exception)
						{
						}
					}
					return new Color(0f, 0f, 0f, 0.5f);
				}
				if (texPropertyName.name.Equals("_MetallicGlossMap"))
				{
					if (mat != null && mat.HasProperty("_Metallic"))
					{
						try
						{
							float @float = mat.GetFloat("_Metallic");
							Color color3 = new Color(@float, @float, @float);
							if (mat.HasProperty("_Smoothness"))
							{
								try
								{
									color3.a = mat.GetFloat("_Smoothness");
								}
								catch (Exception)
								{
								}
							}
							sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Metallic", @float);
							sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_Smoothness", color3.a);
						}
						catch (Exception)
						{
						}
						return new Color(0f, 0f, 0f, 0.5f);
					}
					return new Color(0f, 0f, 0f, 0.5f);
				}
				if (texPropertyName.name.Equals("_OcclusionMap"))
				{
					return new Color(1f, 1f, 1f, 1f);
				}
				if (texPropertyName.name.Equals("_EmissionMap"))
				{
					if (mat != null)
					{
						if (!mat.IsKeywordEnabled("_EMISSION"))
						{
							return Color.black;
						}
						if (!mat.HasProperty("_EmissionColor"))
						{
							return Color.black;
						}
						try
						{
							Color color4 = mat.GetColor("_EmissionColor");
							sourceMaterialPropertyCache.CacheMaterialProperty(mat, "_EmissionColor", color4);
						}
						catch (Exception)
						{
						}
					}
				}
				else if (texPropertyName.name.Equals("_DetailMask"))
				{
					return new Color(0f, 0f, 0f, 0f);
				}
			}
			return new Color(1f, 1f, 1f, 0f);
		}
	}
}
