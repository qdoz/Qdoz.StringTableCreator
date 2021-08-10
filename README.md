# Qdoz.StringTableCreator
Converts a list of values into a string table (ASCII-styled).

Allows you to adjust the output, such as:
* Set column name
* Set text alignment
* Format output string
* Hide entire column

You can either use one of the overloads on `CreateTable` to adjust the aforementioned settings to your liking or decorate your classes with the `StringTableAppearanceAttribute`.

The parameter on `CreateTable` is prioritized over  `StringTableAppearanceAttribute`.
Example: If you have changed the column name both via the `CreateTable` parameter and via the `StringTableAppearanceAttribute` attribute, the column name specified via `CreateTable` will be output in the table.


## Simple Example

```c#
// Sample data
var anonymousArray = new[]
{
	new { DocNum = 123456, CardCode = "12345", CardName = "Peter Griffin", DocTotal = 123.50m },
	new { DocNum = 123457, CardCode = "13501", CardName = "Joe Swanson", DocTotal = 82.67m },
	new { DocNum = 123458, CardCode = "67812", CardName = "Cleveland Brown", DocTotal = 209.01m },
	new { DocNum = 123459, CardCode = "56901", CardName = "Glenn Quagmire", DocTotal = 15.10m },
};
 
var table = anonymousArray.CreateTable();
```

Gives the following result (output may vary depending on the currently set CultureInfo):

```
+--------+----------+-----------------+----------+
| DocNum | CardCode | CardName        | DocTotal |
+--------+----------+-----------------+----------+
| 123456 | 12345    | Peter Griffin   | 123,50   |
| 123457 | 13501    | Joe Swanson     | 82,67    |
| 123458 | 67812    | Cleveland Brown | 209,01   |
| 123459 | 56901    | Glenn Quagmire  | 15,10    |
+--------+----------+-----------------+----------+
```

## Adjusting The Output

### Sample 1
```c#
// Same sample data from above

var table = anonymousArray.CreateTable((PropertySelector: o => o.CardName, ColumnName: "Customer Name"), (o => o.DocTotal, "Paid"));
```

Output:
```
+--------+----------+-----------------+--------+
| DocNum | CardCode | Customer Name   | Paid   |
+--------+----------+-----------------+--------+
| 123456 | 12345    | Peter Griffin   | 123,50 |
| 123457 | 13501    | Joe Swanson     | 82,67  |
| 123458 | 67812    | Cleveland Brown | 209,01 |
| 123459 | 56901    | Glenn Quagmire  | 15,10  |
+--------+----------+-----------------+--------+

```


### Sample 2
```c#
// Same sample data from above

var table = 
anonymousArray.CreateTable(
(PropertySelector: o => o.CardName, ColumnName: "Customer Name", TextAlignment: TextAlignment.Center, FormatString: "", Hide: false), 
(o => o.DocTotal, "Paid", TextAlignment.Center, "C", false));
```

Output (may vary depending on the currently set CultureInfo):
```
+--------+----------+-----------------+----------+
| DocNum | CardCode |  Customer Name  |   Paid   |
+--------+----------+-----------------+----------+
| 123456 | 12345    |  Peter Griffin  | 123,50 € |
| 123457 | 13501    |   Joe Swanson   | 82,67 €  |
| 123458 | 67812    | Cleveland Brown | 209,01 € |
| 123459 | 56901    | Glenn Quagmire  | 15,10 €  |
+--------+----------+-----------------+----------+
```

### Sample 3: StringTableAppearanceAttribute

Same sample data but this time we're using a concrete class:

```c#
public class Order
{
	[StringTableAppearance(ColumnName = "Auftragsnr.", TextAlignment = TextAlignment.Center, FormatString = "N0")]
	public int DocNum { get; set; }
	
	[StringTableAppearance(ColumnName = "CustomerNo", TextAlignment = TextAlignment.Center)]
	public string CardCode { get; set; }
	
	[StringTableAppearance(ColumnName = "Customer Name", TextAlignment = TextAlignment.Center)]
	public string CardName { get; set; }

	[StringTableAppearance(ColumnName = "Paid", TextAlignment = TextAlignment.Center, FormatString = "C", Hide = true)]
	public decimal DocTotal { get; set; }
}


var anonymousArray = new Order[]
{
	new Order { DocNum = 123456, CardCode = "12345", CardName = "Peter Griffin", DocTotal = 123.50m },
	new Order { DocNum = 123457, CardCode = "13501", CardName = "Joe Swanson", DocTotal = 82.67m },
	new Order { DocNum = 123458, CardCode = "67812", CardName = "Cleveland Brown", DocTotal = 209.01m },
	new Order { DocNum = 123459, CardCode = "56901", CardName = "Glenn Quagmire", DocTotal = 15.10m },
};
	
var table = anonymousArray.CreateTable();
```

Result:
```
+-------------+------------+-----------------+
| Auftragsnr. | CustomerNo |  Customer Name  |
+-------------+------------+-----------------+
|   123.456   |   12345    |  Peter Griffin  |
|   123.457   |   13501    |   Joe Swanson   |
|   123.458   |   67812    | Cleveland Brown |
|   123.459   |   56901    | Glenn Quagmire  |
+-------------+------------+-----------------+
```



