using Newtonsoft.Json;

namespace HandlingEditor.Client
{
    public class HandlingConfig
    {
        public float FloatStep { get; set; }
        public float ScriptRange { get; set; }
        public long Timer { get; set; }
        public int ToggleMenuControl { get; set; }
        public bool ShowLockedFields { get; set; }
        public bool CopyOnlySharedFields { get; set; }
        public bool EnableClientPresets { get; set; }
        public bool EnableServerPresets { get; set; }
        public bool EnableSettings { get; set; }
        public bool DisableMenu { get; set; }
        public bool ExposeCommand { get; set; }
        public bool ExposeEvent { get; set; }

        public HandlingConfig()
        {
            FloatStep = 0.01f;
            ScriptRange = 150.0f;
            Timer = 1000;
            ToggleMenuControl = 168;
            ShowLockedFields = true;
            CopyOnlySharedFields = true;

            EnableClientPresets = true;
            EnableServerPresets = true;
            EnableSettings = true;
            DisableMenu = false;
            ExposeCommand = true;
            ExposeEvent = true;
        }
    }
}