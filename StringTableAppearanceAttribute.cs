using System;
&&
namespace Qdoz.StringTableCreator
{
    [AttributeUsage(AttributeTargets.Property)]
	public sealed class StringTableAppearanceAttribute : Attribute
	{
		public string ColumnName { get; set; }
		public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
		public string FormatString { get; set; }
		public bool Hide { get; set; }
	}

}
