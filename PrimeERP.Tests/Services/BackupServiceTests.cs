using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Backup;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>النسخ الاحتياطي والاستعادة</summary>
    [Collection("Database")]
    public class BackupServiceTests : IDisposable
    {
        private readonly IBackupService _service;
        private readonly string _folder = Path.Combine(Path.GetTempPath(), $"PrimeERP.Tests.Backups.{Guid.NewGuid():N}");

        public BackupServiceTests(TestDatabaseFixture db) => _service = db.Services.GetRequiredService<IBackupService>();

        public void Dispose()
        {
            try { if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true); } catch { /* لا يهم في بيئة الاختبار */ }
        }

        [Fact]
        public void Create_ProducesAnExistingFile()
        {
            var result = _service.Create(_folder, note: "test");

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.Exists);
        }

        [Fact]
        public void Create_RecordsSizeGreaterThanZero()
        {
            var result = _service.Create(_folder);
            Assert.True(result.Value.SizeBytes > 0);
        }

        [Fact]
        public void Validate_MissingFile_Fails()
        {
            var result = _service.Validate(Path.Combine(_folder, "no-such-file.db"));
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Validate_EmptyFile_Fails()
        {
            Directory.CreateDirectory(_folder);
            var emptyPath = Path.Combine(_folder, "empty.db");
            File.WriteAllBytes(emptyPath, Array.Empty<byte>());

            var result = _service.Validate(emptyPath);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Validate_FreshlyCreatedBackup_Succeeds()
        {
            var created = _service.Create(_folder);
            var result = _service.Validate(created.Value.FilePath);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
        }

        [Fact]
        public void ApplyRetention_KeepsOnlyRequestedCount()
        {
            for (int i = 0; i < 3; i++)
            {
                _service.Create(_folder, note: $"backup {i}");
                Thread.Sleep(1100); // اسم الملف بدقة الثانية — يمنع تصادم أسماء نسخ متتالية سريعة
            }

            _service.ApplyRetention(_folder, 1);

            var remaining = _service.List(_folder);
            Assert.Single(remaining);
        }
    }
}
