using HandlingEditor.Client.UI;

namespace HandlingEditor.Client.Scripts
{
    internal class SettingsScript
    {
        private readonly MainScript _mainScript;

        internal SettingsMenu Menu { get; private set; }

        internal SettingsScript(MainScript mainScript)
        {
            _mainScript = mainScript;
        }
    }
}