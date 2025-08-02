using System;
using System.Collections.Generic;

namespace Sirenix.OdinInspector
{
	public class SelfValidationResult
	{
		public struct ContextMenuItem
		{
			public string Path;

			public bool On;

			public bool AddSeparatorBefore;

			public Action OnClick;
		}

		public enum ResultType
		{
			Error = 0,
			Warning = 1,
			Valid = 2
		}

		public struct ResultItem
		{
			public string Message;

			public ResultType ResultType;

			public SelfFix? Fix;

			public ResultItemMetaData[] MetaData;

			public Func<IEnumerable<ContextMenuItem>> OnContextClick;

			public Action OnSceneGUI;
		}

		public struct ResultItemMetaData
		{
			public string Name;

			public object Value;

			public Attribute[] Attributes;

			public ResultItemMetaData(string name, object value, params Attribute[] attributes)
			{
				Name = name;
				Value = value;
				Attributes = attributes;
			}
		}

		private static ResultItem NoResultItem;

		private ResultItem[] items;

		private int itemsCount;

		public int Count => itemsCount;

		public ref ResultItem this[int index] => ref items[index];

		public ref ResultItem AddError(string error)
		{
			return ref Add(new ResultItem
			{
				Message = error,
				ResultType = ResultType.Error
			});
		}

		public ref ResultItem AddWarning(string warning)
		{
			return ref Add(new ResultItem
			{
				Message = warning,
				ResultType = ResultType.Warning
			});
		}

		public ref ResultItem Add(ValidatorSeverity severity, string message)
		{
			switch (severity)
			{
			case ValidatorSeverity.Error:
				return ref Add(new ResultItem
				{
					Message = message,
					ResultType = ResultType.Error
				});
			case ValidatorSeverity.Warning:
				return ref Add(new ResultItem
				{
					Message = message,
					ResultType = ResultType.Warning
				});
			default:
				NoResultItem = default(ResultItem);
				return ref NoResultItem;
			}
		}

		public ref ResultItem Add(ResultItem item)
		{
			ResultItem[] array = items;
			if (array == null)
			{
				array = (items = new ResultItem[2]);
			}
			while (array.Length <= itemsCount + 1)
			{
				ResultItem[] array2 = new ResultItem[array.Length * 2];
				for (int i = 0; i < array.Length; i++)
				{
					array2[i] = array[i];
				}
				array = array2;
				items = array2;
			}
			array[itemsCount] = item;
			return ref array[itemsCount++];
		}
	}
}
