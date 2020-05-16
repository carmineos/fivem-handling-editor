using CitizenFX.Core;
using HandlingEditor.Client.UI;

namespace HandlingEditor.Client.Scripts
{
    internal class HandlingEditorScript : BaseScript
    {
        private readonly MainScript _mainScript;

        internal HandlingEditorMenu Menu { get; private set; }

        internal HandlingEditorScript(MainScript mainScript)
        {
            _mainScript = mainScript;
        }
    }
}