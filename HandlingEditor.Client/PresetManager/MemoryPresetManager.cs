using System;
using System.Collections.Generic;

namespace HandlingEditor.Client
{
    public class MemoryPresetManager : IPresetManager<string, HandlingPreset>
    {
        private Dictionary<string, HandlingPreset> _presets;

        public event EventHandler PresetsListChanged;

        public MemoryPresetManager()
        {
            _presets = new Dictionary<string, HandlingPreset>();
        }

        public bool Delete(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return _presets.Remove(name);
        }

        public IEnumerable<string> GetKeys()
        {
            return _presets.Keys;
        }

        public HandlingPreset Load(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if(_presets.TryGetValue(name, out HandlingPreset preset))
                return preset;

            return null;
        }

        public bool Save(string name, HandlingPreset preset)
        {
            if (string.IsNullOrEmpty(name) || preset == null)
                return false;

            if (!_presets.ContainsKey(name))
            {
                _presets.Add(name, preset);
                return true;
            }
            else
                return false;
        }
    }
}
