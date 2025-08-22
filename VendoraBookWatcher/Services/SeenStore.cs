using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace VendoraBookWatcher.Services
{
    public class SeenStore
    {
        private readonly string _path;
        private readonly HashSet<string> _ids = new();

        public SeenStore(string? path = null)
        {
            _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VendoraBookWatcher", "seen.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            Load();
        }

        public bool HasSeen(string id) => _ids.Contains(id);
        public void MarkSeen(string id) => _ids.Add(id);

        public void Save()
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(_ids.ToArray()));
        }

        private void Load()
        {
            if (File.Exists(_path))
            {
                try
                {
                    var arr = JsonSerializer.Deserialize<string[]>(File.ReadAllText(_path));
                    if (arr != null) foreach (var a in arr) _ids.Add(a);
                }
                catch { }
            }
        }
    }
}