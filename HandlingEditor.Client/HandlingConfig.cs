using Newtonsoft.Json;

namespace HandlingEditor.Client
{
    public class HandlingConfig
    {
        public LogLevel LogLevel { get; set; }
        public float FloatStep { get; set; }
        public float ScriptRange { get; set; }
        public long Timer { get; set; }
        public int ToggleMenuControl { get; set; }

        public HandlingConfig()
        {
            LogLevel = LogLevel.Information;
            FloatStep = 0.01f;
            ScriptRange = 150.0f;
            Timer = 1000;
            ToggleMenuControl = 168;
        }
    }
}