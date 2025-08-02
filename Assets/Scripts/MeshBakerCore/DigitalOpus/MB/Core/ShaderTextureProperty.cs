using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalOpus.MB.Core
{
	[Serializable]
	public class ShaderTextureProperty
	{
		public string name;

		public bool isNormalMap;

		[HideInInspector]
		public bool isNormalDontKnow;

		public ShaderTextureProperty(string n, bool norm)
		{
			name = n;
			isNormalMap = norm;
			isNormalDontKnow = false;
		}

		public ShaderTextureProperty(string n, bool norm, bool isNormalDontKnow)
		{
			name = n;
			isNormalMap = norm;
			this.isNormalDontKnow = isNormalDontKnow;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ShaderTextureProperty))
			{
				return false;
			}
			ShaderTextureProperty shaderTextureProperty = (ShaderTextureProperty)obj;
			if (!name.Equals(shaderTextureProperty.name))
			{
				return false;
			}
			if (isNormalMap != shaderTextureProperty.isNormalMap)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public static string[] GetNames(List<ShaderTextureProperty> props)
		{
			string[] array = new string[props.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = props[i].name;
			}
			return array;
		}
	}
}
