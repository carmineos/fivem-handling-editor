using HandlingEditor.Client.UI;

namespace HandlingEditor.Client.Scripts
{
    internal class ClientSettingsScript
    {
        private readonly MainScript _mainScript;

        internal ClientSettingsMenu Menu { get; private set; }

        internal ClientSettingsScript(MainScript mainScript)
        {
            _mainScript = mainScript;
        }
    }
}