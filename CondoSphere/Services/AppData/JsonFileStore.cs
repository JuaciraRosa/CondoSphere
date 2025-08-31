using System.Text.Json;

namespace CondoSphere.Services.AppData
{

    public class JsonFileStore<T> where T : class, new()
    {
        private readonly string _filePath;

        public JsonFileStore(IWebHostEnvironment env, string relativePath)
        {
            var root = env.WebRootPath ?? throw new Exception("WebRootPath not found");
            var full = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            _filePath = full;
        }

        public async Task<List<T>> ReadAllAsync()
        {
            if (!File.Exists(_filePath)) return new List<T>();
            using var fs = File.OpenRead(_filePath);
            var data = await JsonSerializer.DeserializeAsync<List<T>>(fs, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return data ?? new List<T>();
        }

        public async Task WriteAllAsync(List<T> items)
        {
            using var fs = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(fs, items, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
