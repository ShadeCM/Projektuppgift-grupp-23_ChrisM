using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Personalregister
{
    public class AppConfig
    {
        public bool UseInMemory { get; set; } = true;
        public Dictionary<string, double> TaxRates { get; set; } = new Dictionary<string, double> { { "DAY", 0.30 }, { "NIGHT", 0.45 } };
        public int SearchLimit { get; set; } = 200;
        public string SnapshotPath { get; set; } = "snapshot.jsonl";

        public static AppConfig Load(string path = "config.json")
        {
            try
            {
                if (!File.Exists(path)) return new AppConfig();
                var txt = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<AppConfig>(txt);
                Current = cfg ?? new AppConfig();
                return Current;
            }
            catch
            {
                Current = new AppConfig();
                return Current;
            }
        }

        // Runtime singleton for easy access from models/repo
        public static AppConfig Current { get; private set; } = new AppConfig();
    }
}
