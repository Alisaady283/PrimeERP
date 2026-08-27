using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Print;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>ExportService (CSV/Excel/PDF فعلية عبر ClosedXML/QuestPDF) لم تكن مُختبرة إطلاقاً رغم
    /// كونها منطقاً نقياً بلا حاجة لخيط WPF (لا تُنشئ أي DispatcherObject).</summary>
    [Collection("Database")]
    public class ExportServiceTests : IDisposable
    {
        private readonly IExportService _service;
        private readonly ISettingsService _settings;
        private readonly List<string> _tempFiles = new();

        public ExportServiceTests(TestDatabaseFixture db)
        {
            _service = db.Services.GetRequiredService<IExportService>();
            _settings = db.Services.GetRequiredService<ISettingsService>();
            AppSession.DevMode = true;
        }

        public void Dispose()
        {
            foreach (var path in _tempFiles)
                try { File.Delete(path); } catch { /* لا يهم في بيئة الاختبار */ }
        }

        private string TempFile(string extension) =>
            _tempFiles.AddAndReturn(Path.Combine(Path.GetTempPath(), $"PrimeERP.Tests.Export.{Guid.NewGuid():N}.{extension}"));

        private class Row
        {
            public string Name { get; set; }
            public decimal Balance { get; set; }
        }

        private static List<GridColumn> Columns() => new()
        {
            new() { Header = "الاسم", Binding = nameof(Row.Name) },
            new() { Header = "الرصيد", Binding = nameof(Row.Balance), Format = "N2" }
        };

        [Fact]
        public void ExportToCsv_WritesHeaderAndRows()
        {
            var path = TempFile("csv");
            var data = new List<Row> { new() { Name = "أحمد", Balance = 150.5m }, new() { Name = "سارة", Balance = 0m } };

            _service.ExportToCsv(data, Columns(), path);

            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length);
            Assert.Equal("الاسم,الرصيد", lines[0]);
            Assert.Equal("أحمد,150.50", lines[1]);
        }

        [Fact]
        public void ExportToCsv_QuotesValuesContainingComma()
        {
            var path = TempFile("csv");
            var data = new List<Row> { new() { Name = "أحمد, محمد", Balance = 10m } };

            _service.ExportToCsv(data, Columns(), path);

            var lines = File.ReadAllLines(path);
            Assert.Equal("\"أحمد, محمد\",10.00", lines[1]);
        }

        [Fact]
        public void ExportToCsv_SkipsColumnsNotVisible()
        {
            var path = TempFile("csv");
            var columns = Columns();
            columns[1].IsVisible = false;

            _service.ExportToCsv(new List<Row> { new() { Name = "أحمد", Balance = 10m } }, columns, path);

            var lines = File.ReadAllLines(path);
            Assert.Equal("الاسم", lines[0]);
        }

        [Fact]
        public void ExportToCsv_SkipsColumnsWithoutPermission()
        {
            var permissions = new FakePermissionService { Allowed = false };
            var service = new ExportService(permissions, _settings);
            var columns = Columns();
            columns[1].PermissionKey = "Balance.View";

            var path = TempFile("csv");
            service.ExportToCsv(new List<Row> { new() { Name = "أحمد", Balance = 10m } }, columns, path);

            var lines = File.ReadAllLines(path);
            Assert.Equal("الاسم", lines[0]);
        }

        [Fact]
        public void ExportToExcel_WritesReadableWorkbook()
        {
            var path = TempFile("xlsx");
            var data = new List<Row> { new() { Name = "أحمد", Balance = 200m } };

            _service.ExportToExcel(data, Columns(), path);

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet(1);

            Assert.Equal("الاسم", sheet.Cell(1, 1).GetString());
            Assert.Equal("أحمد", sheet.Cell(2, 1).GetString());
            Assert.Equal("200.00", sheet.Cell(2, 2).GetString());
        }

        [Fact]
        public void ExportToPdf_ProducesNonEmptyFile()
        {
            var path = TempFile("pdf");
            var data = new List<Row> { new() { Name = "أحمد", Balance = 200m } };

            _service.ExportToPdf(data, Columns(), path, "تقرير اختباري");

            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
        }

        [Fact]
        public void ExportPrintableToPdf_ProducesNonEmptyFile()
        {
            var path = TempFile("pdf");
            var printable = PrintTemplates.JournalEntryPrint(new JournalEntry
            {
                EntryNo = "JE-TEST-1",
                EntryDate = "2026-01-01",
                Description = "قيد اختباري للتصدير",
                Source = "يدوي",
                CreatedBy = "tester",
                TotalDebit = 100m,
                TotalCredit = 100m,
                Lines = new List<JournalLine>
                {
                    new() { AccountCode = "1204", AccountName = "الصندوق", Debit = 100m, Credit = 0m },
                    new() { AccountCode = "41", AccountName = "إيرادات المبيعات", Debit = 0m, Credit = 100m }
                }
            });

            var result = _service.ExportPrintableToPdf(printable, path);

            Assert.True(result.IsSuccess);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
        }

        [Fact]
        public void ExportPrintableToPdf_Fails_WhenDocumentNull()
        {
            var result = _service.ExportPrintableToPdf(null, TempFile("pdf"));
            Assert.False(result.IsSuccess);
        }

        private class FakePermissionService : IPermissionService
        {
            public bool Allowed;
            public bool Can(string key) => Allowed;
            public bool CanAny(params string[] keys) => Allowed;
            public bool CanAll(params string[] keys) => Allowed;
            public void LoadForUser(int userId) { }
            public IEnumerable<string> GetUserPermissions(int userId) => Array.Empty<string>();
        }
    }

    internal static class ListExtensions
    {
        public static string AddAndReturn(this List<string> list, string value)
        {
            list.Add(value);
            return value;
        }
    }
}
