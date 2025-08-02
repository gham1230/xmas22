using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MB_MultiMaterialTexArray
{
	public Material combinedMaterial;

	public List<MB_TexArraySlice> slices = new List<MB_TexArraySlice>();

	public List<MB_TexArrayForProperty> textureProperties = new List<MB_TexArrayForProperty>();
}
