using HandlingEditor.Client.Scripts;
using MenuAPI;

namespace HandlingEditor.Client.UI
{
    internal class ServerPresetsMenu : Menu
    {
        private readonly ServerPresetsScript _script;

        internal ServerPresetsMenu(ServerPresetsScript script, string name = Globals.ScriptName, string subtitle = "Server Presets Menu") : base(name, subtitle)
        {
            _script = script;
        }
    }
}