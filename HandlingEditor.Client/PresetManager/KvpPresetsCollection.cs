using System;
using System.Collections.Generic;

using static CitizenFX.Core.Native.API;
using Newtonsoft.Json;
using System.Linq;

namespace HandlingEditor.Client
{
    /// <summary>
    /// The vstancer preset manager which saves the presets as key-value pairs built-in FiveM
    /// </summary>
    public class KvpPresetsCollection : IPresetsCollection<string, HandlingData>
    {
        private readonly string mKvpPrefix;

        public event EventHandler PresetsCollectionChanged;

        public KvpPresetsCollection(string prefix)
        {
            mKvpPrefix = prefix;
        }

        public bool Delete(string name)
        {
            // Check if the preset ID is valid
            if (string.IsNullOrEmpty(name))
                return false;

            // Get the KVP key
            string key = string.Concat(mKvpPrefix, name);

            // Check if a KVP with the given key exists
            if (GetResourceKvpString(key) == null)
                return false;

            // Delete the KVP
            DeleteResourceKvp(key);

            // Invoke the event
            PresetsCollectionChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }

        public bool Save(string name, HandlingData preset)
        {
            // Check if the preset and the ID are valid
            if (string.IsNullOrEmpty(name) || preset == null)
                return false;

            // Get the KVP key
            string key = string.Concat(mKvpPrefix, name);

            // Be sure the key isn't already used
            if (GetResourceKvpString(key) != null)
                return false;

            // Get the Json
            var json = JsonConvert.SerializeObject(preset);

            // Save the KVP
            SetResourceKvp(key, json);

            // Invoke the event
            PresetsCollectionChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }

        public bool Load(string name, out HandlingData preset)
        {
            preset = null;

            // Check if the preset ID is valid
            if (string.IsNullOrEmpty(name))
                return false;

            // Get the KVP key
            string key = string.Concat(mKvpPrefix, name);

            // Get the KVP value
            string value = GetResourceKvpString(key);

            // Check if the value is valid
            if (string.IsNullOrEmpty(value))
                return false;

            // Create a preset
            preset = JsonConvert.DeserializeObject<HandlingData>(value);
            return true;
        }

        public IEnumerable<string> GetKeys()
        {
            return ScriptUtilities.GetKeyValuePairs(mKvpPrefix).Select(key => key.Remove(0, mKvpPrefix.Length));
        }
    }
}