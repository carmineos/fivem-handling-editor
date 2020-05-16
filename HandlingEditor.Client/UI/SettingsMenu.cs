using HandlingEditor.Client.Scripts;
using MenuAPI;

namespace HandlingEditor.Client.UI
{
    internal class SettingsMenu : Menu
    {
        private readonly SettingsScript _script;

        internal SettingsMenu(SettingsScript script, string name = Globals.ScriptName, string subtitle = "Settings Menu") : base(name, subtitle)
        {
            _script = script;
        }
    }
}