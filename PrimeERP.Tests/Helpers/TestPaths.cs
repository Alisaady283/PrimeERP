using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace PrimeERP.Tests.Helpers
{
    public static class TestPaths
    {
        public static string Folder { get; } = Create();

        public static string Unique(string prefix, string extension = "") =>
            Path.Combine(Folder, $"{prefix}.{Guid.NewGuid():N}{extension}");

        public static void DeleteDatabase(string path)
        {
            SqliteConnection.ClearAllPools();
            foreach (var file in new[] { path, path + "-wal", path + "-shm" })
                try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        private static string Create()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PrimeERP.csproj"))) dir = dir.Parent;
            var folder = Path.Combine(dir?.FullName ?? Path.GetTempPath(), "bin", "TestTemp");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
