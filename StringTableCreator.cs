using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Qdoz.StringTableCreator.Extensions;

namespace Qdoz.StringTableCreator
{
	public static class StringTableCreator
	{
		private record Cell
		{
			public Cell()
			{

			}

			public Cell(string PropertyName, string ColumnName, string Value) =>
			(this.PropertyName, this.ColumnName, this.Value) = (PropertyName, ColumnName, Value);

			public string PropertyName { get; init; }
			public string ColumnName { get; init; }
			public string Value { get; init; }
			public int Width => (Value?.Length ?? 0) < (ColumnName?.Length ?? 0) ? (ColumnName?.Length ?? 0) : (Value?.Length ?? 0);
		}

		private static PropertyInfo GetPropertyInfo<TSource, TProperty>(TSource source, Expression<Func<TSource, TProperty>> propertyLambda)
		{
			var type = typeof(TSource);

			var member = propertyLambda.Body as MemberExpression;
			if (member is null)
			{
				var unaryExpression = propertyLambda.Body as UnaryExpression;
				if (unaryExpression?.Operand is MemberExpression memberExpressionByOperator)
					member = memberExpressionByOperator;
				else
					throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");
			}



			PropertyInfo propInfo = member.Member as PropertyInfo;
			if (propInfo == null)
				throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");

			if (type != propInfo.ReflectedType &&
				!type.IsSubclassOf(propInfo.ReflectedType))
				throw new ArgumentException($"Expression '{propertyLambda}' refers to a property that is not from type {type}.");

			return propInfo;
		}

		private static TAttribute GetAttribute<TAttribute, TSource>(string propertyName)
			where TAttribute : class
			where TSource : class =>
			typeof(TSource).GetProperties()
					 .FirstOrDefault(p => p.Name == propertyName)
					?.GetCustomAttributes(typeof(TAttribute), false)
					.FirstOrDefault() as TAttribute;


		private static string GetPropertyValue(object obj, string propertyName, string format = "")
		{
			if (obj is null)
				return "";

			if (format is null)
				format = "";

			var value = obj.GetType().GetProperty(propertyName) is null
				? null
				: obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);

			if (value is null)
				return "";

			return string.IsNullOrEmpty(format) ? value.ToString() : string.Format($"{{0:{format}}}", value);
		}


		private static string PadBoth(string source, int length)
		{
			var spaces = length - source.Length;
			var padLeft = spaces / 2 + source.Length;
			return source.PadLeft(padLeft).PadRight(length);
		}


		private static string Pad(string source, int length, TextAlignment textAlignment) => textAlignment switch
		{
			TextAlignment.Left => source.PadRight(length),
			TextAlignment.Right => source.PadLeft(length),
			TextAlignment.Center => PadBoth(source, length),
			_ => Pad(source, length, TextAlignment.Left),
		};

		public static string CreateTable<T>(this IEnumerable<T> list,
											 params (Expression<Func<T, object>> PropertySelector, string ColumnName, TextAlignment TextAlignment, string FormatString, bool Hide)[] mapping) where T : class
		{
			var columnMapping =

			typeof(T).GetProperties()
			.Where(p => p.CanRead)
			.Select(p => new
			{
				PropertyName = p.Name,
				ColumnName = "",
				TextAlignment = TextAlignment.Left,
				FormatString = "",
				Hide = false,
				Appearance = GetAttribute<StringTableAppearanceAttribute, T>(p.Name),
			})

			.GroupJoin(mapping
						.Where(m => m.PropertySelector is not null)
						.Select(m => new
						{
							PropertyName = GetPropertyInfo(default(T), m.PropertySelector)?.Name,
							ColumnName = m.ColumnName,
							TextAlignment = m.TextAlignment,
							FormatString = m.FormatString,
							Hide = m.Hide,
						}), p => p.PropertyName, m => m.PropertyName,
						(p, m) => new
						{
							p.PropertyName,
							ColumnName = m?.FirstOrDefault()?.ColumnName ?? p?.Appearance?.ColumnName ?? p.PropertyName,
							TextAlignment = m?.FirstOrDefault()?.TextAlignment ?? p?.Appearance?.TextAlignment ?? TextAlignment.Left,
							FormatString = m?.FirstOrDefault()?.FormatString ?? p?.Appearance?.FormatString ?? "",
							Hide = m?.FirstOrDefault()?.Hide ?? p?.Appearance?.Hide ?? false,
						})

			.Where(m => m.Hide == false)
			.ToList();

			var rows = list.ToList();
			var cells = new List<Cell>(rows.Count * columnMapping.Count);
			for (int i = 0; i < rows.Count; i++)
			{
				foreach (var map in columnMapping)
				{
					cells.Add(new Cell { PropertyName = map.PropertyName, ColumnName = map.ColumnName, Value = GetPropertyValue(rows[i], map.PropertyName, map.FormatString) });
				}
			}

			var lines = cells.GroupBy(columnMapping.Count).ToList();
			var columnsByWidth = cells.GroupBy(f => f.PropertyName).Select(g => new { PropertyName = g.Key, MaxWidth = g.Max(x => x.Width) }).ToList();
			var header = columnMapping.Aggregate("|", (current, next) => $"{current} {Pad(next.ColumnName, columnsByWidth.FirstOrDefault(c => c.PropertyName == next.PropertyName).MaxWidth, next.TextAlignment)} |") + Environment.NewLine;


			var tableWidth = columnsByWidth.Sum(c => c.MaxWidth) + (cells.GroupBy(f => f.PropertyName).Count() * 3) + 1;
			var separatorLine = string.Join("", Enumerable.Range(0, tableWidth).Select(x => "-")) + Environment.NewLine;

			var table = new StringBuilder(separatorLine);
			table.Remove(0, 1).Insert(0, "+").Remove(separatorLine.Length - 3, 1).Insert(separatorLine.Length - 3, "+");
			var offset = 0;
			for (int i = 0; i < columnsByWidth.Count - 1; i++)
			{
				table.Remove(columnsByWidth[i].MaxWidth + offset, 1).Insert(columnsByWidth[i].MaxWidth + offset + (3 * (i + 1)), "+");
				offset += columnsByWidth[i].MaxWidth;
			}
			separatorLine = table.ToString();
			table.Append(header)
				 .Append(separatorLine);


			foreach (var line in lines)
			{
				table.Append(line.Aggregate("|", (current, next) => $"{current} {Pad(next.Value, columnsByWidth.FirstOrDefault(c => c.PropertyName == next.PropertyName).MaxWidth, columnMapping.FirstOrDefault(m => m.PropertyName == next.PropertyName).TextAlignment)} |") + Environment.NewLine);
			}

			return table.Append(separatorLine).ToString();
		}

		public static string CreateTable<T>(this IEnumerable<T> list) where T : class =>
		CreateTable(list, new[] { (PropertySelector: default(Expression<Func<T, object>>),
						  ColumnName: "",
						  TextAlignment: TextAlignment.Left,
						  FormatString: "",
						  Hide: false)});

		public static string CreateTable<T>(this IEnumerable<T> list,
										 params (Expression<Func<T, object>> PropertySelector, string ColumnName)[] mapping) where T : class =>
		CreateTable(list,
				mapping.Select(m =>
							 (PropertySelector: m.PropertySelector,
							  ColumnName: m.ColumnName,
							  TextAlignment: TextAlignment.Left,
							  FormatString: "",
							  Hide: false)).ToArray());

		public static string CreateTable<T>(this IEnumerable<T> list,
									 params (Expression<Func<T, object>> PropertySelector, string ColumnName, TextAlignment TextAlignment)[] mapping) where T : class =>
		CreateTable(list,
				mapping.Select(m =>
							 (PropertySelector: m.PropertySelector,
							  ColumnName: m.ColumnName,
							  TextAlignment: m.TextAlignment,
							  FormatString: "",
							  Hide: false)
				)
				.ToArray());

		public static string CreateTable<T>(this IEnumerable<T> list,
								 params (Expression<Func<T, object>> PropertySelector, string ColumnName, TextAlignment TextAlignment, string FormatString)[] mapping) where T : class =>
		CreateTable(list,
				mapping.Select(m =>
							 (PropertySelector: m.PropertySelector,
							  ColumnName: m.ColumnName,
							  TextAlignment: m.TextAlignment,
							  FormatString: m.FormatString,
							  Hide: false)
				)
				.ToArray());
	}

}
