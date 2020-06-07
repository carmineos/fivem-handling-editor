using HandlingEditor.Client.Scripts;
using MenuAPI;

namespace HandlingEditor.Client.UI
{
    internal class ClientSettingsMenu : Menu
    {
        private readonly ClientSettingsScript _script;

        internal ClientSettingsMenu(ClientSettingsScript script, string name = Globals.ScriptName, string subtitle = "Settings Menu") : base(name, subtitle)
        {
            _script = script;
        }
    }
}