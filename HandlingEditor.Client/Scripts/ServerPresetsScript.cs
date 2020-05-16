using HandlingEditor.Client.UI;

namespace HandlingEditor.Client.Scripts
{
    internal class ServerPresetsScript
    {
        private readonly MainScript _mainScript;

        internal ServerPresetsMenu Menu { get; private set; }

        internal ServerPresetsScript(MainScript mainScript)
        {
            _mainScript = mainScript;
        }
    }
}