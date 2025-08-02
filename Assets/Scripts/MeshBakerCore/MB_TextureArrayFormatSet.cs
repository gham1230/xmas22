using System;
using DigitalOpus.MB.Core;
using UnityEngine;

[Serializable]
public class MB_TextureArrayFormatSet
{
	public string name;

	public TextureFormat defaultFormat;

	public MB_TextureArrayFormat[] formatOverrides;

	public bool ValidateTextureImporterFormatsExistsForTextureFormats(MB2_EditorMethodsInterface editorMethods, int idx)
	{
		if (editorMethods == null)
		{
			return true;
		}
		if (!editorMethods.TextureImporterFormatExistsForTextureFormat(defaultFormat))
		{
			Debug.LogError("TextureImporter format does not exist for Texture Array Output Formats: " + idx + " Defaut Format " + defaultFormat);
			return false;
		}
		for (int i = 0; i < formatOverrides.Length; i++)
		{
			if (!editorMethods.TextureImporterFormatExistsForTextureFormat(formatOverrides[i].format))
			{
				Debug.LogError(string.Concat("TextureImporter format does not exist for Texture Array Output Formats: ", idx, " Format Overrides: ", i, " (", formatOverrides[i].format, ")"));
				return false;
			}
		}
		return true;
	}

	public TextureFormat GetFormatForProperty(string propName)
	{
		for (int i = 0; i < formatOverrides.Length; i++)
		{
			if (formatOverrides.Equals(formatOverrides[i].propertyName))
			{
				return formatOverrides[i].format;
			}
		}
		return defaultFormat;
	}
}
