using HandlingEditor.Client.Scripts;
using MenuAPI;

namespace HandlingEditor.Client.UI
{
    internal class ClientPresetsMenu : Menu
    {
        private readonly ClientPresetsScript _script;

        internal ClientPresetsMenu(ClientPresetsScript script, string name = Globals.ScriptName, string subtitle = "Client Presets Menu") : base(name, subtitle)
        {
            _script = script;
        }
    }
}