using System;
using System.Diagnostics;

namespace Sirenix.OdinInspector
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
	[Conditional("UNITY_EDITOR")]
	public class EnumPagingAttribute : Attribute
	{
	}
}
