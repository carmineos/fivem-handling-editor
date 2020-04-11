using System;
using System.Collections.Generic;

namespace HandlingEditor.Client
{
    // TODO: Use a concurrent dictionary
    public class MemoryPresetManager : IPresetManager<string, HandlingPreset>
    {
        private Dictionary<string, HandlingPreset> _presets;

        public event EventHandler PresetsCollectionChanged;

        public MemoryPresetManager()
        {
            _presets = new Dictionary<string, HandlingPreset>();
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

        public bool Load(string name, out HandlingPreset preset)
        {
            preset = null;

            if (string.IsNullOrEmpty(name))
                return false;

            return _presets.TryGetValue(name, out preset);
        }

        public bool Save(string name, HandlingPreset preset)
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
