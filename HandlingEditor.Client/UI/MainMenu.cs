using System;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using MenuAPI;
using HandlingEditor.Client.Scripts;

namespace HandlingEditor.Client.UI
{
    internal class MainMenu : Menu
    {
        private readonly MainScript _script;

        private HandlingEditorMenu HandlingEditorMenu { get; set; }
        private ClientPresetsMenu ClientPresetsMenu { get; set; }
        private ServerPresetsMenu ServerPresetsMenu { get; set; }
        private ClientSettingsMenu ClientSettingsMenu { get; set; }

        private MenuItem HandlingEditorMenuMenuItem { get; set; }
        private MenuItem ClientPresetsMenuMenuItem { get; set; }
        private MenuItem ServerPresetsMenuMenuItem { get; set; }
        private MenuItem ClientSettingsMenuMenuItem { get; set; }


        internal MainMenu(MainScript script, string name = Globals.ScriptName, string subtitle = "Main Menu") : base(name, subtitle)
        {
            _script = script;

            _script.ToggleMenuVisibility += new EventHandler((sender, args) =>
            {
                var currentMenu = MenuController.MainMenu;

                if (currentMenu == null)
                    return;

                currentMenu.Visible = !currentMenu.Visible;
            });

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.MenuToggleKey = (Control)_script.Config.ToggleMenuControl;
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.DontOpenAnyMenu = true;
            MenuController.MainMenu = this;

            if (_script.HandlingEditorScript != null)
                HandlingEditorMenu = _script.HandlingEditorScript.Menu;

            if (_script.ClientPresetsScript != null)
                ClientPresetsMenu = _script.ClientPresetsScript.Menu;

            if (_script.ServerPresetsScript != null)
                ServerPresetsMenu = _script.ServerPresetsScript.Menu;

            if (_script.ClientSettingsScript != null)
                ClientSettingsMenu = _script.ClientSettingsScript.Menu;

            Update();
        }

        internal void Update()
        {
            ClearMenuItems();

            MenuController.Menus.Clear();
            MenuController.AddMenu(this);

            if (HandlingEditorMenu != null)
            {
                HandlingEditorMenuMenuItem = new MenuItem("Handling Editor Menu", "The menu to edit handling properties.")
                {
                    Label = "→→→"
                };

                AddMenuItem(HandlingEditorMenuMenuItem);

                MenuController.AddSubmenu(this, HandlingEditorMenu);
                MenuController.BindMenuItem(this, HandlingEditorMenu, HandlingEditorMenuMenuItem);
            }

            if (ClientPresetsMenu != null)
            {
                ClientPresetsMenuMenuItem = new MenuItem("Personal Presets Menu", "The menu to manage the presets saved by you.")
                {
                    Label = "→→→"
                };

                AddMenuItem(ClientPresetsMenuMenuItem);

                MenuController.AddSubmenu(this, ClientPresetsMenu);
                MenuController.BindMenuItem(this, ClientPresetsMenu, ClientPresetsMenuMenuItem);
            }

            if (ServerPresetsMenu != null)
            {
                ServerPresetsMenuMenuItem = new MenuItem("Server Presets Menu", "The menu to manage the presets shared by the server.")
                {
                    Label = "→→→"
                };

                AddMenuItem(ServerPresetsMenuMenuItem);

                MenuController.AddSubmenu(this, ServerPresetsMenu);
                MenuController.BindMenuItem(this, ServerPresetsMenu, ServerPresetsMenuMenuItem);
            }

            if (ClientSettingsMenu != null)
            {
                ClientSettingsMenuMenuItem = new MenuItem("Settings Menu", "The menu to change available settings.")
                {
                    Label = "→→→"
                };

                AddMenuItem(ClientSettingsMenuMenuItem);

                MenuController.AddSubmenu(this, ClientSettingsMenu);
                MenuController.BindMenuItem(this, ClientSettingsMenu, ClientSettingsMenuMenuItem);
            }
        }

        internal bool HideMenu
        {
            get => MenuController.DontOpenAnyMenu;
            set
            {
                MenuController.DontOpenAnyMenu = value;
            }
        }
    }
}
