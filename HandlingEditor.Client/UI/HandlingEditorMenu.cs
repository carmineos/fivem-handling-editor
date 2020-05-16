using HandlingEditor.Client.Scripts;
using MenuAPI;

namespace HandlingEditor.Client.UI
{
    internal class HandlingEditorMenu : Menu
    {
        private readonly HandlingEditorScript _script;

        internal HandlingEditorMenu(HandlingEditorScript script, string name = Globals.ScriptName, string subtitle = "Handling Editor Menu") : base(name, subtitle)
        {
            _script = script;
        }
    }
}