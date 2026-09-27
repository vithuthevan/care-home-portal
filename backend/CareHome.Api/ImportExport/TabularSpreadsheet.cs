using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace CareHome.Api.ImportExport;

public static class TabularSpreadsheet
{
    public static bool IsCsv(string fileName) =>
        fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

    public static bool IsXlsx(string fileName) =>
        fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);

    public static bool IsSupported(string fileName) => IsCsv(fileName) || IsXlsx(fileName);

    public static List<Dictionary<string, string>> ReadRows(Stream stream, string fileName)
    {
        if (IsXlsx(fileName))
        {
            return ReadXlsx(stream);
        }

        if (IsCsv(fileName))
        {
            return ReadCsv(stream);
        }

        throw new InvalidOperationException("Only .csv and .xlsx files are supported.");
    }

    public static byte[] Write(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyDictionary<string, string>> rows, string format)
    {
        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return WriteXlsx(headers, rows);
        }

        return Encoding.UTF8.GetBytes(WriteCsv(headers, rows));
    }

    public static string ContentType(string format) =>
        string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase)
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv";

    public static string FileExtension(string format) =>
        string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) ? "xlsx" : "csv";

    private static List<Dictionary<string, string>> ReadCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        if (lines.Count == 0)
        {
            return [];
        }

        var headers = SplitCsvLine(lines[0]).Select(NormalizeHeader).ToList();
        var rows = new List<Dictionary<string, string>>();
        for (var i = 1; i < lines.Count; i++)
        {
            var parts = SplitCsvLine(lines[i]);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
            {
                var value = c < parts.Length ? parts[c].Trim() : string.Empty;
                dict[headers[c]] = value;
            }

            rows.Add(dict);
        }

        return rows;
    }

    private static List<Dictionary<string, string>> ReadXlsx(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var used = sheet.RangeUsed();
        if (used is null)
        {
            return [];
        }

        var firstRow = used.FirstRow().RowNumber();
        var lastRow = used.LastRow().RowNumber();
        var firstCol = used.FirstColumn().ColumnNumber();
        var lastCol = used.LastColumn().ColumnNumber();

        var headers = new List<string>();
        for (var col = firstCol; col <= lastCol; col++)
        {
            headers.Add(NormalizeHeader(sheet.Cell(firstRow, col).GetString()));
        }

        var rows = new List<Dictionary<string, string>>();
        for (var row = firstRow + 1; row <= lastRow; row++)
        {
            if (RowIsEmpty(sheet, row, firstCol, lastCol))
            {
                continue;
            }

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var col = firstCol; col <= lastCol; col++)
            {
                var header = headers[col - firstCol];
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                dict[header] = sheet.Cell(row, col).GetFormattedString().Trim();
            }

            rows.Add(dict);
        }

        return rows;
    }

    private static bool RowIsEmpty(IXLWorksheet sheet, int row, int firstCol, int lastCol)
    {
        for (var col = firstCol; col <= lastCol; col++)
        {
            if (!string.IsNullOrWhiteSpace(sheet.Cell(row, col).GetFormattedString()))
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] WriteXlsx(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Data");
        for (var c = 0; c < headers.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = headers[c];
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (var c = 0; c < headers.Count; c++)
            {
                row.TryGetValue(headers[c], out var value);
                sheet.Cell(r + 2, c + 1).Value = value ?? string.Empty;
            }
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string WriteCsv(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            var values = headers.Select(h => row.TryGetValue(h, out var v) ? v : string.Empty);
            sb.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"', StringComparison.Ordinal) || value.Contains(',', StringComparison.Ordinal))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        foreach (var ch in line)
        {
            if (ch == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (ch == ',' && !quoted)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

    private static string NormalizeHeader(string header) =>
        header.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
}
