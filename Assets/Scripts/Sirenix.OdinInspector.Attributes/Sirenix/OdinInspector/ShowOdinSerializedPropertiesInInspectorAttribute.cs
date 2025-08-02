using System;
using System.Diagnostics;

namespace Sirenix.OdinInspector
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	[Conditional("UNITY_EDITOR")]
	public class ShowOdinSerializedPropertiesInInspectorAttribute : Attribute
	{
	}
}
