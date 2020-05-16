using HandlingEditor.Client.UI;

namespace HandlingEditor.Client.Scripts
{
    internal class ClientPresetsScript
    {
        private readonly MainScript _mainScript;

        internal ClientPresetsMenu Menu { get; private set; }

        internal ClientPresetsScript(MainScript mainScript)
        {
            _mainScript = mainScript;
        }
    }
}