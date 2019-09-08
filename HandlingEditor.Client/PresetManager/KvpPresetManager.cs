using static CitizenFX.Core.Native.API;

namespace HandlingEditor.Client
{
    /// <summary>
    /// The handling preset manager which saves the presets as key-value pairs built-in FiveM
    /// </summary>
    public class KvpPresetManager : IPresetManager
    {
        private string mKvpPrefix;

        public KvpPresetManager(string prefix)
        {
            mKvpPrefix = prefix;
        }

        public bool Delete(string id)
        {
            // Check if the preset ID is valid
            if (string.IsNullOrEmpty(id))
                return false;

            // Get the KVP key
            string key = $"{mKvpPrefix}{id}";

            // Check if a KVP with the given key exists
            if (GetResourceKvpString(key) == null)
                return false;

            // Delete the KVP
            DeleteResourceKvp(key);

            return true;
        }

        public bool Save(string ID, HandlingPreset preset)
        {
            // Check if the preset and the ID are valid
            if (string.IsNullOrEmpty(ID) || preset == null)
                return false;

            // Get the KVP key
            string kvpName = $"{mKvpPrefix}{ID}";

            // Be sure the key isn't already used
            if (GetResourceKvpString(kvpName) != null)
                return false;

            // Get the XML
            var xml = preset.ToXml(ID);

            // Save the KVP
            SetResourceKvp(kvpName, xml);

            return true;
        }

        public HandlingPreset Load(string id)
        {
            // Check if the preset ID is valid
            if (string.IsNullOrEmpty(id))
                return null;

            // Get the KVP key
            string key = $"{mKvpPrefix}{id}";

            // Get the KVP value
            string value = GetResourceKvpString(key);

            // Check if the value is valid
            if (string.IsNullOrEmpty(value))
                return null;

            // Remove BOM from XML string
            Helpers.RemoveByteOrderMarks(ref value);
            
            // Create a preset
            HandlingPreset preset = new HandlingPreset();

            // Load the values from the XML
            preset.FromXml(value);

            return preset;
        }
    }
}