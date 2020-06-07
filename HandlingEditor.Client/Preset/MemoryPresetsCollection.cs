using System;
using System.Collections.Generic;

namespace HandlingEditor.Client
{
    // TODO: Use a concurrent dictionary
    public class MemoryPresetsCollection : IPresetsCollection<string, HandlingData>
    {
        private readonly Dictionary<string, HandlingData> _presets;

        public event EventHandler PresetsCollectionChanged;

        public MemoryPresetsCollection()
        {
            _presets = new Dictionary<string, HandlingData>();
        }

        public bool Delete(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (_presets.Remove(name))
            {
                PresetsCollectionChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        public IEnumerable<string> GetKeys()
        {
            return _presets.Keys;
        }

        public bool Load(string name, out HandlingData preset)
        {
            preset = null;

            if (string.IsNullOrEmpty(name))
                return false;

            return _presets.TryGetValue(name, out preset);
        }

        public bool Save(string name, HandlingData preset)
        {
            if (string.IsNullOrEmpty(name) || preset == null)
                return false;

            if (!_presets.ContainsKey(name))
            {
                _presets.Add(name, preset);
                PresetsCollectionChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
                return false;
        }
    }
}
