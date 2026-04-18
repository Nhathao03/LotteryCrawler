using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LotteryCrawler.Services
{
    /// <summary>
    /// Helper to save objects as pretty JSON into the repository-level `result/` folder.
    /// </summary>
    public static class FileSaver
    {
        /// <summary>
        /// Serializes <paramref name="data"/> to JSON and writes it to `result/{fileName}`. If
        /// <paramref name="fileName"/> is null or empty a timestamped file name will be used.
        /// Returns the full path to the written file.
        /// </summary>
        public static async Task<string> SaveJsonAsync<T>(T data, string? fileName = null, string? subFolder = null)
        {
                if (data == null) throw new ArgumentNullException(nameof(data));

            var root = Directory.GetCurrentDirectory();
            var dir = Path.Combine(root, "result");
            if (!string.IsNullOrWhiteSpace(subFolder)) dir = Path.Combine(dir, subFolder);
            Directory.CreateDirectory(dir);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"run-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            }

            var path = Path.Combine(dir, fileName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Serialize to string using the runtime type so collections/anonymous types serialize correctly,
            // then write explicitly using UTF-8 encoding (without BOM).
            var json = JsonSerializer.Serialize(data, data.GetType(), options);
            var utf8 = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            await File.WriteAllTextAsync(path, json, utf8).ConfigureAwait(false);

            return Path.GetFullPath(path);
        }
    }
}
