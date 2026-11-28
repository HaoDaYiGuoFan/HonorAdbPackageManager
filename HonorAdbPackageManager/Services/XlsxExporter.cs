using System.IO.Compression;
using System.Text;

namespace HonorAdbPackageManager.Services;

/// <summary>
/// 最小化 .xlsx 写出器（OpenXML SpreadsheetML，无第三方依赖）。
/// 支持多工作表、首行加粗、字符串/数值单元格；Excel 与 WPS 均可直接打开。
/// </summary>
public static class XlsxExporter
{
    /// <summary>写出工作簿。sheets: (表名, 行数据)；行内值 string→文本单元格，double/long/int→数值单元格。</summary>
    public static void Write(string path, params (string Name, IReadOnlyList<IReadOnlyList<object?>> Rows)[] sheets)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

        Add(zip, "[Content_Types].xml", ContentTypes(sheets.Length));
        Add(zip, "_rels/.rels",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
            </Relationships>
            """);
        Add(zip, "xl/workbook.xml", Workbook(sheets));
        Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Length));
        Add(zip, "xl/styles.xml",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <fonts count="2">
                <font><sz val="11"/><name val="Calibri"/></font>
                <font><b/><sz val="11"/><name val="Calibri"/></font>
              </fonts>
              <fills count="2">
                <fill><patternFill patternType="none"/></fill>
                <fill><patternFill patternType="gray125"/></fill>
              </fills>
              <borders count="1"><border/></borders>
              <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
              <cellXfs count="2">
                <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>
              </cellXfs>
            </styleSheet>
            """);
        for (int i = 0; i < sheets.Length; i++)
            Add(zip, $"xl/worksheets/sheet{i + 1}.xml", Sheet(sheets[i].Rows));
    }

    private static void Add(ZipArchive zip, string name, string content)
    {
        var e = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
        w.Write(content);
    }

    private static string ContentTypes(int sheetCount)
    {
        var sb = new StringBuilder(
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
              <Default Extension="xml" ContentType="application/xml"/>
              <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
              <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
            """);
        for (int i = 1; i <= sheetCount; i++)
            sb.Append($"\n  <Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        sb.Append("\n</Types>");
        return sb.ToString();
    }

    private static string Workbook((string Name, IReadOnlyList<IReadOnlyList<object?>> Rows)[] sheets)
    {
        var sb = new StringBuilder(
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets>

            """);
        for (int i = 0; i < sheets.Length; i++)
            sb.Append($"    <sheet name=\"{Esc(sheets[i].Name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>\n");
        sb.Append("  </sheets>\n</workbook>");
        return sb.ToString();
    }

    private static string WorkbookRels(int sheetCount)
    {
        var sb = new StringBuilder(
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">

            """);
        for (int i = 1; i <= sheetCount; i++)
            sb.Append($"  <Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>\n");
        sb.Append($"  <Relationship Id=\"rId{sheetCount + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>\n");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private static string Sheet(IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        var sb = new StringBuilder(
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>

            """);
        for (int r = 0; r < rows.Count; r++)
        {
            sb.Append($"    <row r=\"{r + 1}\">");
            var cells = rows[r];
            for (int c = 0; c < cells.Count; c++)
            {
                var cellRef = $"{ColumnName(c + 1)}{r + 1}";
                var v = cells[c];
                var style = r == 0 ? " s=\"1\"" : "";
                if (v is double d) sb.Append($"<c r=\"{cellRef}\"{style}><v>{d.ToString("0.######", CultureInfo.InvariantCulture)}</v></c>");
                else if (v is long l) sb.Append($"<c r=\"{cellRef}\"{style}><v>{l}</v></c>");
                else if (v is int i2) sb.Append($"<c r=\"{cellRef}\"{style}><v>{i2}</v></c>");
                else
                {
                    var s = v?.ToString() ?? string.Empty;
                    sb.Append($"<c r=\"{cellRef}\" t=\"inlineStr\"{style}><is><t xml:space=\"preserve\">{Esc(s)}</t></is></c>");
                }
            }
            sb.Append("</row>\n");
        }
        sb.Append("  </sheetData></worksheet>");
        return sb.ToString();
    }

    /// <summary>列号 → 列字母（1=A, 27=AA）</summary>
    private static string ColumnName(int index)
    {
        var sb = new StringBuilder();
        while (index > 0)
        {
            var rem = (index - 1) % 26;
            sb.Insert(0, (char)('A' + rem));
            index = (index - 1) / 26;
        }
        return sb.ToString();
    }

    private static string Esc(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
        .Replace("\"", "&quot;").Replace("'", "&apos;");
}
