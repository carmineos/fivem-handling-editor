using System.Collections.Generic;
using System.Linq;

namespace HandlingEditor.Client
{
    public class Config
    {
        protected Dictionary<string, string> Entries { get; set; }

        public Config(string content)
        {
            Entries = new Dictionary<string, string>();

            if (content == null || content.Length == 0)
            {
                return;
            }

            var splitted = content
                .Split('\n')
                .Where((line) => !line.Trim().StartsWith("#"))
                .Select((line) => line.Trim().Split('='))
                .Where((line) => line.Length == 2);

            foreach (var tuple in splitted)
            {
                Entries.Add(tuple[0], tuple[1]);
            }
        }

        public string Get(string key, string defaultValue = null)
        {
            if (Entries.ContainsKey(key))
            {
                return Entries[key];
            }

            return defaultValue;
        }

        public int GetIntValue(string key, int fallback)
        {
            if (Entries.ContainsKey(key))
            {
                var value = Entries[key];

                if (int.TryParse(value, out int tmp))
                    return tmp;
            }
            return fallback;
        }

        public float GetFloatValue(string key, float fallback)
        {
            if (Entries.ContainsKey(key))
            {
                var value = Entries[key];

                if (float.TryParse(value, out float tmp))
                    return tmp;
            }
            return fallback;
        }

        public bool GetBoolValue(string key, bool fallback)
        {
            if (Entries.ContainsKey(key))
            {
                var value = Entries[key];

                if (bool.TryParse(value, out bool tmp))
                    return tmp;
            }
            return fallback;
        }

        public long GetLongValue(string key, long fallback)
        {
            if (Entries.ContainsKey(key))
            {
                var value = Entries[key];

                if (long.TryParse(value, out long tmp))
                    return tmp;
            }
            return fallback;
        }
    }
}