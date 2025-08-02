using System;
using System.Diagnostics;

namespace Sirenix.OdinInspector
{
	[DontApplyToListElements]
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	[Conditional("UNITY_EDITOR")]
	public sealed class InfoBoxAttribute : Attribute
	{
		public string Message;

		public InfoMessageType InfoMessageType;

		public string VisibleIf;

		public bool GUIAlwaysEnabled;

		public InfoBoxAttribute(string message, InfoMessageType infoMessageType = InfoMessageType.Info, string visibleIfMemberName = null)
		{
			Message = message;
			InfoMessageType = infoMessageType;
			VisibleIf = visibleIfMemberName;
		}

		public InfoBoxAttribute(string message, string visibleIfMemberName)
		{
			Message = message;
			InfoMessageType = InfoMessageType.Info;
			VisibleIf = visibleIfMemberName;
		}
	}
}
