using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Web.Services;

public sealed class StudentAccountReportExcelExporter
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public byte[] Export(StudentAccountReportExport report)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypes());
            WriteEntry(archive, "_rels/.rels", PackageRelationships());
            WriteEntry(archive, "xl/workbook.xml", Workbook(report.SheetName));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
            WriteEntry(archive, "xl/styles.xml", Styles());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", Worksheet(report));
        }

        return output.ToArray();
    }

    public byte[] Export(BriefAccountReportExport report)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypes());
            WriteEntry(archive, "_rels/.rels", PackageRelationships());
            WriteEntry(archive, "xl/workbook.xml", Workbook(report.SheetName));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
            WriteEntry(archive, "xl/styles.xml", Styles());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", BriefWorksheet(report));
        }

        return output.ToArray();
    }

    public byte[] Export(DetailedAccountReportExport report)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypes());
            WriteEntry(archive, "_rels/.rels", PackageRelationships());
            WriteEntry(archive, "xl/workbook.xml", Workbook(report.SheetName));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
            WriteEntry(archive, "xl/styles.xml", Styles());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", DetailedWorksheet(report));
        }

        return output.ToArray();
    }

    public byte[] Export(GenericReportExport report)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypes());
            WriteEntry(archive, "_rels/.rels", PackageRelationships());
            WriteEntry(archive, "xl/workbook.xml", Workbook(report.SheetName));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
            WriteEntry(archive, "xl/styles.xml", Styles());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", GenericWorksheet(report));
        }

        return output.ToArray();
    }

    private static XDocument GenericWorksheet(GenericReportExport report)
    {
        var columnCount = Math.Max(1, report.Headers.Count);
        var lastColumn = CellReference(columnCount, 1).TrimEnd('1');
        var rows = new List<XElement>
        {
            Row(1, 32, TextCell("A1", report.Title, 1)),
            Row(2, 24, TextCell("A2", report.SchoolName, 2)),
            Row(3, 22, TextCell("A3", report.FilterSummary, 3)),
            Row(4, 20, TextCell("A4", $"{report.PrintDateLabel}: {ReportFormatter.DateTime(report.PrintedOn)}", 3)),
            Row(6, 36, report.Headers.Select((header, index) => TextCell(CellReference(index + 1, 6), header, 4)).ToArray())
        };

        var excelRow = 7;
        foreach (var reportRow in report.Rows)
        {
            var cells = reportRow.Cells.Select((cell, index) => cell.Number.HasValue
                ? NumberCell(CellReference(index + 1, excelRow), cell.Number.Value, 7)
                : TextCell(CellReference(index + 1, excelRow), cell.Text, 5)).ToArray();
            rows.Add(Row(excelRow, 28, cells));
            excelRow++;
        }

        var merges = new XElement(Spreadsheet + "mergeCells", new XAttribute("count", 4),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", $"A1:{lastColumn}1")),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", $"A2:{lastColumn}2")),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", $"A3:{lastColumn}3")),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", $"A4:{lastColumn}4")));

        var worksheet = new XElement(Spreadsheet + "worksheet",
            new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Spreadsheet + "sheetPr", new XElement(Spreadsheet + "pageSetUpPr", new XAttribute("fitToPage", 1))),
            new XElement(Spreadsheet + "sheetViews",
                new XElement(Spreadsheet + "sheetView",
                    new XAttribute("workbookViewId", 0),
                    new XAttribute("showGridLines", 0),
                    new XAttribute("rightToLeft", report.RightToLeft ? 1 : 0),
                    new XElement(Spreadsheet + "pane", new XAttribute("ySplit", 6), new XAttribute("topLeftCell", "A7"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
            new XElement(Spreadsheet + "sheetFormatPr", new XAttribute("defaultRowHeight", 18)),
            new XElement(Spreadsheet + "cols", Enumerable.Range(1, columnCount).Select(index =>
                Column(index, index, Math.Clamp(report.Headers[index - 1].Length + 7, 13, 32)))),
            new XElement(Spreadsheet + "sheetData", rows),
            new XElement(Spreadsheet + "autoFilter", new XAttribute("ref", $"A6:{lastColumn}{Math.Max(6, excelRow - 1)}")),
            merges,
            new XElement(Spreadsheet + "pageMargins", new XAttribute("left", .2), new XAttribute("right", .2), new XAttribute("top", .4), new XAttribute("bottom", .4), new XAttribute("header", .2), new XAttribute("footer", .2)),
            new XElement(Spreadsheet + "pageSetup", new XAttribute("orientation", "landscape"), new XAttribute("paperSize", 9), new XAttribute("fitToWidth", 1), new XAttribute("fitToHeight", 0)));

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), worksheet);
    }

    private static XDocument Worksheet(StudentAccountReportExport report)
    {
        var rows = new List<XElement>
        {
            Row(1, 30, TextCell("A1", report.Title, 1)),
            Row(2, 24, TextCell("A2", $"{report.StudentCode} : {report.StudentName}", 2)),
            Row(3, 20, TextCell("A3", $"{report.PrintDateLabel}: {ReportFormatter.DateTime(report.PrintedOn)}", 3)),
            Row(5, 30, report.Headers.Select((header, index) => TextCell(CellReference(index + 1, 5), header, 4)).ToArray())
        };

        var excelRow = 6;
        foreach (var item in report.Rows)
        {
            var cells = new[]
            {
                NumberCell(CellReference(1, excelRow), item.Number, 5),
                TextCell(CellReference(2, excelRow), item.Item, 5),
                TextCell(CellReference(3, excelRow), item.Service, 5),
                TextCell(CellReference(4, excelRow), item.Semester, 5),
                TextCell(CellReference(5, excelRow), item.Description, 5),
                TextCell(CellReference(6, excelRow), item.Level, 5),
                TextCell(CellReference(7, excelRow), item.Class, 5),
                DateCell(CellReference(8, excelRow), item.TransactionDate, 6),
                NumberCell(CellReference(9, excelRow), item.Debit, 7),
                NumberCell(CellReference(10, excelRow), item.Credit, 7),
                NumberCell(CellReference(11, excelRow), item.Balance, 7),
                DateTimeCell(CellReference(12, excelRow), item.AddedOn, 8)
            };
            rows.Add(Row(excelRow, 28, cells));
            excelRow++;
        }

        rows.Add(Row(excelRow, 24,
            TextCell(CellReference(1, excelRow), report.TotalLabel, 9),
            NumberCell(CellReference(9, excelRow), report.TotalDebit, 10),
            NumberCell(CellReference(10, excelRow), report.TotalCredit, 10),
            NumberCell(CellReference(11, excelRow), report.FinalBalance, 10)));

        var mergeCells = new XElement(Spreadsheet + "mergeCells", new XAttribute("count", 4),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", "A1:L1")),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", "A2:L2")),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", "A3:L3")),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", $"A{excelRow}:H{excelRow}")));

        var worksheet = new XElement(Spreadsheet + "worksheet",
            new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Spreadsheet + "sheetPr", new XElement(Spreadsheet + "pageSetUpPr", new XAttribute("fitToPage", 1))),
            new XElement(Spreadsheet + "sheetViews",
                new XElement(Spreadsheet + "sheetView",
                    new XAttribute("workbookViewId", 0),
                    new XAttribute("rightToLeft", report.RightToLeft ? 1 : 0),
                    new XElement(Spreadsheet + "pane", new XAttribute("ySplit", 5), new XAttribute("topLeftCell", "A6"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
            new XElement(Spreadsheet + "sheetFormatPr", new XAttribute("defaultRowHeight", 18)),
            new XElement(Spreadsheet + "cols",
                Column(1, 1, 6), Column(2, 2, 17), Column(3, 3, 21), Column(4, 4, 16),
                Column(5, 5, 35), Column(6, 7, 15), Column(8, 8, 14), Column(9, 11, 15), Column(12, 12, 21)),
            new XElement(Spreadsheet + "sheetData", rows),
            // Worksheet children are schema-ordered: autoFilter must precede mergeCells.
            new XElement(Spreadsheet + "autoFilter", new XAttribute("ref", $"A5:L{Math.Max(5, excelRow - 1)}")),
            mergeCells,
            new XElement(Spreadsheet + "pageMargins", new XAttribute("left", .25), new XAttribute("right", .25), new XAttribute("top", .5), new XAttribute("bottom", .5), new XAttribute("header", .2), new XAttribute("footer", .2)),
            new XElement(Spreadsheet + "pageSetup", new XAttribute("orientation", "landscape"), new XAttribute("paperSize", 9), new XAttribute("fitToWidth", 1), new XAttribute("fitToHeight", 0)));

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), worksheet);
    }

    private static XDocument BriefWorksheet(BriefAccountReportExport report)
    {
        var rows = new List<XElement>
        {
            Row(1, 30, TextCell("A1", report.Title, 1)),
            Row(2, 24, TextCell("A2", report.SchoolName, 2)),
            Row(3, 20, TextCell("A3", $"{report.PrintDateLabel}: {ReportFormatter.DateTime(report.PrintedOn)}", 3)),
            Row(5, 36, report.Headers.Select((header, index) => TextCell(CellReference(index + 1, 5), header, 4)).ToArray())
        };

        var excelRow = 6;
        foreach (var item in report.Rows)
        {
            // Previous-balance adjustments are disclosed separately but are already
            // reflected in the previous-balance figure, matching the source report.
            var netDueFormula = $"G{excelRow}+I{excelRow}-J{excelRow}-K{excelRow}+L{excelRow}";
            var remainingFormula = $"M{excelRow}-N{excelRow}";
            rows.Add(Row(excelRow, 30,
                NumberCell(CellReference(1, excelRow), item.Number, 5),
                TextCell(CellReference(2, excelRow), item.StudentNumber, 5),
                TextCell(CellReference(3, excelRow), item.StudentName, 5),
                TextCell(CellReference(4, excelRow), item.Level, 5),
                TextCell(CellReference(5, excelRow), item.Status, 5),
                TextCell(CellReference(6, excelRow), item.ParentMobile, 5),
                NumberCell(CellReference(7, excelRow), item.PreviousBalance, 7),
                NumberCell(CellReference(8, excelRow), item.PreviousBalanceAdjustment, 7),
                NumberCell(CellReference(9, excelRow), item.Dues, 7),
                NumberCell(CellReference(10, excelRow), item.DuesWithdrawal, 7),
                NumberCell(CellReference(11, excelRow), item.Discounts, 7),
                NumberCell(CellReference(12, excelRow), item.Tax, 7),
                FormulaCell(CellReference(13, excelRow), netDueFormula, item.NetDue, 7),
                NumberCell(CellReference(14, excelRow), item.Paid, 7),
                FormulaCell(CellReference(15, excelRow), remainingFormula, item.Remaining, 7)));
            excelRow++;
        }

        var firstDataRow = 6;
        var lastDataRow = Math.Max(firstDataRow, excelRow - 1);
        var totalCells = new List<XElement> { TextCell(CellReference(1, excelRow), report.TotalLabel, 9) };
        for (var column = 7; column <= 15; column++)
        {
            var letter = CellReference(column, 1).TrimEnd('1');
            var total = report.Totals[column - 7];
            totalCells.Add(report.Rows.Count == 0
                ? NumberCell(CellReference(column, excelRow), 0, 10)
                : FormulaCell(CellReference(column, excelRow), $"SUM({letter}{firstDataRow}:{letter}{lastDataRow})", total, 10));
        }
        rows.Add(Row(excelRow, 25, totalCells.ToArray()));

        var mergeCells = new XElement(Spreadsheet + "mergeCells", new XAttribute("count", 4),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", "A1:O1")),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", "A2:O2")),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", "A3:O3")),
            new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", $"A{excelRow}:F{excelRow}")));

        var worksheet = new XElement(Spreadsheet + "worksheet",
            new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Spreadsheet + "sheetPr", new XElement(Spreadsheet + "pageSetUpPr", new XAttribute("fitToPage", 1))),
            new XElement(Spreadsheet + "sheetViews",
                new XElement(Spreadsheet + "sheetView",
                    new XAttribute("workbookViewId", 0),
                    new XAttribute("showGridLines", 0),
                    new XAttribute("rightToLeft", report.RightToLeft ? 1 : 0),
                    new XElement(Spreadsheet + "pane", new XAttribute("ySplit", 5), new XAttribute("topLeftCell", "A6"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
            new XElement(Spreadsheet + "sheetFormatPr", new XAttribute("defaultRowHeight", 18)),
            new XElement(Spreadsheet + "cols",
                Column(1, 1, 6), Column(2, 2, 14), Column(3, 3, 31), Column(4, 4, 18),
                Column(5, 5, 13), Column(6, 6, 16), Column(7, 15, 15)),
            new XElement(Spreadsheet + "sheetData", rows),
            // Worksheet children are schema-ordered: autoFilter must precede mergeCells.
            new XElement(Spreadsheet + "autoFilter", new XAttribute("ref", $"A5:O{Math.Max(5, excelRow - 1)}")),
            mergeCells,
            new XElement(Spreadsheet + "pageMargins", new XAttribute("left", .2), new XAttribute("right", .2), new XAttribute("top", .4), new XAttribute("bottom", .4), new XAttribute("header", .2), new XAttribute("footer", .2)),
            new XElement(Spreadsheet + "pageSetup", new XAttribute("orientation", "landscape"), new XAttribute("paperSize", 9), new XAttribute("fitToWidth", 1), new XAttribute("fitToHeight", 0)));

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), worksheet);
    }

    private static XDocument DetailedWorksheet(DetailedAccountReportExport report)
    {
        var rows = new List<XElement>
        {
            Row(1, 30, TextCell("A1", report.Title, 1)),
            Row(2, 24, TextCell("A2", report.SchoolName, 2)),
            Row(3, 20, TextCell("A3", $"{report.StudentNameLabel}: {report.StudentName}", 3)),
            Row(4, 20, TextCell("A4", $"{report.PrintDateLabel}: {ReportFormatter.DateTime(report.PrintedOn)}", 3)),
            Row(6, 30, report.Headers.Select((header, index) => TextCell(CellReference(index + 1, 6), header, 4)).ToArray())
        };

        var merges = new List<XElement>
        {
            new(Spreadsheet + "mergeCell", new XAttribute("ref", "A1:H1")),
            new(Spreadsheet + "mergeCell", new XAttribute("ref", "A2:H2")),
            new(Spreadsheet + "mergeCell", new XAttribute("ref", "A3:H3")),
            new(Spreadsheet + "mergeCell", new XAttribute("ref", "A4:H4"))
        };

        var excelRow = 7;
        foreach (var group in report.Groups)
        {
            rows.Add(Row(excelRow, 24, TextCell(CellReference(1, excelRow), group.Name, 9)));
            merges.Add(new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", $"A{excelRow}:H{excelRow}")));
            excelRow++;

            foreach (var item in group.Rows)
            {
                rows.Add(Row(excelRow, 26,
                    NumberCell(CellReference(1, excelRow), item.Number, 5),
                    TextCell(CellReference(2, excelRow), item.Service, 5),
                    TextCell(CellReference(3, excelRow), item.Semester, 5),
                    TextCell(CellReference(4, excelRow), item.Operation, 5),
                    DateCell(CellReference(5, excelRow), item.TransactionDate, 6),
                    NumberCell(CellReference(6, excelRow), item.Debit, 7),
                    NumberCell(CellReference(7, excelRow), item.Credit, 7),
                    NumberCell(CellReference(8, excelRow), item.Remaining, 7)));
                excelRow++;
            }

            rows.Add(Row(excelRow, 24,
                TextCell(CellReference(1, excelRow), report.TotalLabel, 9),
                NumberCell(CellReference(6, excelRow), group.TotalDebit, 10),
                NumberCell(CellReference(7, excelRow), group.TotalCredit, 10),
                NumberCell(CellReference(8, excelRow), group.Remaining, 10)));
            merges.Add(new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", $"A{excelRow}:E{excelRow}")));
            excelRow++;
        }

        rows.Add(Row(excelRow, 26,
            TextCell(CellReference(1, excelRow), report.AccountSummaryLabel, 9),
            NumberCell(CellReference(6, excelRow), report.TotalDebit, 10),
            NumberCell(CellReference(7, excelRow), report.TotalCredit, 10),
            NumberCell(CellReference(8, excelRow), report.Remaining, 10)));
        merges.Add(new XElement(Spreadsheet + "mergeCell", new XAttribute("ref", $"A{excelRow}:E{excelRow}")));

        var worksheet = new XElement(Spreadsheet + "worksheet",
            new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Spreadsheet + "sheetPr", new XElement(Spreadsheet + "pageSetUpPr", new XAttribute("fitToPage", 1))),
            new XElement(Spreadsheet + "sheetViews",
                new XElement(Spreadsheet + "sheetView",
                    new XAttribute("workbookViewId", 0),
                    new XAttribute("showGridLines", 0),
                    new XAttribute("rightToLeft", report.RightToLeft ? 1 : 0),
                    new XElement(Spreadsheet + "pane", new XAttribute("ySplit", 6), new XAttribute("topLeftCell", "A7"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
            new XElement(Spreadsheet + "sheetFormatPr", new XAttribute("defaultRowHeight", 18)),
            new XElement(Spreadsheet + "cols",
                Column(1, 1, 7), Column(2, 2, 28), Column(3, 4, 20), Column(5, 5, 15), Column(6, 8, 16)),
            new XElement(Spreadsheet + "sheetData", rows),
            new XElement(Spreadsheet + "mergeCells", new XAttribute("count", merges.Count), merges),
            new XElement(Spreadsheet + "pageMargins", new XAttribute("left", .25), new XAttribute("right", .25), new XAttribute("top", .5), new XAttribute("bottom", .5), new XAttribute("header", .2), new XAttribute("footer", .2)),
            new XElement(Spreadsheet + "pageSetup", new XAttribute("orientation", "portrait"), new XAttribute("paperSize", 9), new XAttribute("fitToWidth", 1), new XAttribute("fitToHeight", 0)));

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), worksheet);
    }

    private static XDocument Styles() => new(new XDeclaration("1.0", "utf-8", "yes"),
        new XElement(Spreadsheet + "styleSheet",
            new XElement(Spreadsheet + "numFmts", new XAttribute("count", 3),
                new XElement(Spreadsheet + "numFmt", new XAttribute("numFmtId", 164), new XAttribute("formatCode", "dd/mm/yyyy")),
                new XElement(Spreadsheet + "numFmt", new XAttribute("numFmtId", 165), new XAttribute("formatCode", "#,##0.##;[Red]-#,##0.##")),
                new XElement(Spreadsheet + "numFmt", new XAttribute("numFmtId", 166), new XAttribute("formatCode", "dd/mm/yyyy hh:mm AM/PM"))),
            new XElement(Spreadsheet + "fonts", new XAttribute("count", 4),
                Font(11, false, "FF000000"), Font(18, true, "FF111827"), Font(13, true, "FF111827"), Font(11, true, "FFFFFFFF")),
            new XElement(Spreadsheet + "fills", new XAttribute("count", 4),
                new XElement(Spreadsheet + "fill", new XElement(Spreadsheet + "patternFill", new XAttribute("patternType", "none"))),
                new XElement(Spreadsheet + "fill", new XElement(Spreadsheet + "patternFill", new XAttribute("patternType", "gray125"))),
                Fill("FF15394C"), Fill("FFE8EEF4")),
            new XElement(Spreadsheet + "borders", new XAttribute("count", 2),
                new XElement(Spreadsheet + "border", new XElement(Spreadsheet + "left"), new XElement(Spreadsheet + "right"), new XElement(Spreadsheet + "top"), new XElement(Spreadsheet + "bottom"), new XElement(Spreadsheet + "diagonal")),
                new XElement(Spreadsheet + "border", BorderSide("left"), BorderSide("right"), BorderSide("top"), BorderSide("bottom"), new XElement(Spreadsheet + "diagonal"))),
            new XElement(Spreadsheet + "cellStyleXfs", new XAttribute("count", 1), new XElement(Spreadsheet + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 0), new XAttribute("fillId", 0), new XAttribute("borderId", 0))),
            new XElement(Spreadsheet + "cellXfs", new XAttribute("count", 11),
                Xf(0, 0, 0, 0), Xf(0, 1, 0, 0, horizontal: "center"), Xf(0, 2, 0, 0, horizontal: "center"),
                Xf(0, 0, 0, 0, horizontal: "left"), Xf(0, 3, 2, 1, horizontal: "center", wrap: true),
                Xf(0, 0, 0, 1, horizontal: "center", wrap: true), Xf(164, 0, 0, 1, horizontal: "center"),
                Xf(165, 0, 0, 1, horizontal: "center"), Xf(166, 0, 0, 1, horizontal: "center", wrap: true),
                Xf(0, 2, 3, 1, horizontal: "center"), Xf(165, 2, 3, 1, horizontal: "center")),
            new XElement(Spreadsheet + "cellStyles", new XAttribute("count", 1), new XElement(Spreadsheet + "cellStyle", new XAttribute("name", "Normal"), new XAttribute("xfId", 0), new XAttribute("builtinId", 0)))));

    private static XElement Xf(int numberFormat, int font, int fill, int border, string? horizontal = null, bool wrap = false)
    {
        var alignment = new XElement(Spreadsheet + "alignment", new XAttribute("vertical", "center"));
        if (horizontal != null) alignment.Add(new XAttribute("horizontal", horizontal));
        if (wrap) alignment.Add(new XAttribute("wrapText", 1));

        return new XElement(Spreadsheet + "xf", new XAttribute("numFmtId", numberFormat), new XAttribute("fontId", font),
            new XAttribute("fillId", fill), new XAttribute("borderId", border), new XAttribute("xfId", 0),
            new XAttribute("applyAlignment", 1), new XAttribute("applyNumberFormat", numberFormat == 0 ? 0 : 1), alignment);
    }

    private static XElement Font(int size, bool bold, string color)
    {
        var font = new XElement(Spreadsheet + "font");
        if (bold) font.Add(new XElement(Spreadsheet + "b"));
        font.Add(new XElement(Spreadsheet + "sz", new XAttribute("val", size)), new XElement(Spreadsheet + "color", new XAttribute("rgb", color)),
            new XElement(Spreadsheet + "name", new XAttribute("val", "Arial")), new XElement(Spreadsheet + "family", new XAttribute("val", 2)));
        return font;
    }

    private static XElement Fill(string color) => new(Spreadsheet + "fill", new XElement(Spreadsheet + "patternFill", new XAttribute("patternType", "solid"), new XElement(Spreadsheet + "fgColor", new XAttribute("rgb", color)), new XElement(Spreadsheet + "bgColor", new XAttribute("indexed", 64))));
    private static XElement BorderSide(string name) => new(Spreadsheet + name, new XAttribute("style", "thin"), new XElement(Spreadsheet + "color", new XAttribute("rgb", "FF1F2937")));
    private static XElement Column(int min, int max, double width) => new(Spreadsheet + "col", new XAttribute("min", min), new XAttribute("max", max), new XAttribute("width", width), new XAttribute("customWidth", 1));
    private static XElement Row(int number, double height, params XElement[] cells) => new(Spreadsheet + "row", new XAttribute("r", number), new XAttribute("ht", height), new XAttribute("customHeight", 1), cells);
    private static XElement TextCell(string reference, string? value, int style) => new(Spreadsheet + "c", new XAttribute("r", reference), new XAttribute("s", style), new XAttribute("t", "inlineStr"), new XElement(Spreadsheet + "is", new XElement(Spreadsheet + "t", value ?? string.Empty)));
    private static XElement NumberCell(string reference, double value, int style) => new(Spreadsheet + "c", new XAttribute("r", reference), new XAttribute("s", style), new XElement(Spreadsheet + "v", value.ToString(CultureInfo.InvariantCulture)));
    private static XElement FormulaCell(string reference, string formula, double cachedValue, int style) => new(Spreadsheet + "c", new XAttribute("r", reference), new XAttribute("s", style), new XElement(Spreadsheet + "f", formula), new XElement(Spreadsheet + "v", cachedValue.ToString(CultureInfo.InvariantCulture)));
    private static XElement DateCell(string reference, DateTime value, int style) => NumberCell(reference, value.ToOADate(), style);
    private static XElement DateTimeCell(string reference, DateTime value, int style) => NumberCell(reference, value.ToOADate(), style);

    private static string CellReference(int column, int row)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }
        return $"{name}{row}";
    }

    private static void WriteEntry(ZipArchive archive, string path, XDocument document)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        document.Save(writer, SaveOptions.DisableFormatting);
    }

    private static XDocument ContentTypes() => XDocument.Parse("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>""");
    private static XDocument PackageRelationships() => XDocument.Parse("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
    private static XDocument WorkbookRelationships() => XDocument.Parse("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""");

    private static XDocument Workbook(string sheetName) => new(new XDeclaration("1.0", "utf-8", "yes"),
        new XElement(Spreadsheet + "workbook", new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Spreadsheet + "sheets", new XElement(Spreadsheet + "sheet", new XAttribute("name", SanitizeSheetName(sheetName)), new XAttribute("sheetId", 1), new XAttribute(Relationships + "id", "rId1")))));

    private static string SanitizeSheetName(string name)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var cleaned = string.Concat(name.Select(c => invalid.Contains(c) ? ' ' : c)).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Report" : cleaned[..Math.Min(31, cleaned.Length)];
    }
}

public sealed record StudentAccountReportExport(
    string SheetName,
    string Title,
    string StudentCode,
    string StudentName,
    string PrintDateLabel,
    DateTime PrintedOn,
    bool RightToLeft,
    IReadOnlyList<string> Headers,
    IReadOnlyList<StudentAccountReportExportRow> Rows,
    string TotalLabel,
    double TotalDebit,
    double TotalCredit,
    double FinalBalance);

public sealed record StudentAccountReportExportRow(
    int Number,
    string Item,
    string Service,
    string Semester,
    string Description,
    string Level,
    string Class,
    DateTime TransactionDate,
    double Debit,
    double Credit,
    double Balance,
    DateTime AddedOn);

public sealed record BriefAccountReportExport(
    string SheetName,
    string Title,
    string SchoolName,
    string PrintDateLabel,
    DateTime PrintedOn,
    bool RightToLeft,
    IReadOnlyList<string> Headers,
    IReadOnlyList<BriefAccountReportExportRow> Rows,
    string TotalLabel,
    IReadOnlyList<double> Totals);

public sealed record BriefAccountReportExportRow(
    int Number,
    string StudentNumber,
    string StudentName,
    string Level,
    string Status,
    string ParentMobile,
    double PreviousBalance,
    double PreviousBalanceAdjustment,
    double Dues,
    double DuesWithdrawal,
    double Discounts,
    double Tax,
    double NetDue,
    double Paid,
    double Remaining);

public sealed record DetailedAccountReportExport(
    string SheetName,
    string Title,
    string SchoolName,
    string StudentNameLabel,
    string StudentName,
    string PrintDateLabel,
    DateTime PrintedOn,
    bool RightToLeft,
    IReadOnlyList<string> Headers,
    IReadOnlyList<DetailedAccountGroupExport> Groups,
    string TotalLabel,
    string AccountSummaryLabel,
    double TotalDebit,
    double TotalCredit,
    double Remaining);

public sealed record DetailedAccountGroupExport(
    string Name,
    IReadOnlyList<DetailedAccountReportExportRow> Rows,
    double TotalDebit,
    double TotalCredit,
    double Remaining);

public sealed record DetailedAccountReportExportRow(
    int Number,
    string Service,
    string Semester,
    string Operation,
    DateTime TransactionDate,
    double Debit,
    double Credit,
    double Remaining);

public sealed record GenericReportExport(
    string SheetName,
    string Title,
    string SchoolName,
    string FilterSummary,
    string PrintDateLabel,
    DateTime PrintedOn,
    bool RightToLeft,
    IReadOnlyList<string> Headers,
    IReadOnlyList<GenericReportExportRow> Rows);

public sealed record GenericReportExportRow(IReadOnlyList<GenericReportCell> Cells);

public sealed record GenericReportCell(string Text, double? Number = null);
