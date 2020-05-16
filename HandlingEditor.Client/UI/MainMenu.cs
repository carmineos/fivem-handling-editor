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
        private ClientPresetsMenu PersonalPresetsMenu { get; set; }
        private ServerPresetsMenu ServerPresetsMenu { get; set; }
        private SettingsMenu SettingsMenu { get; set; }

        private MenuItem HandlingEditorMenuMenuItem { get; set; }
        private MenuItem PersonalPresetsMenuMenuItem { get; set; }
        private MenuItem ServerPresetsMenuMenuItem { get; set; }
        private MenuItem SettingsMenuMenuItem { get; set; }


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
                PersonalPresetsMenu = _script.ClientPresetsScript.Menu;

            if (_script.ServerPresetsScript != null)
                ServerPresetsMenu = _script.ServerPresetsScript.Menu;

            if (_script.SettingsScript != null)
                SettingsMenu = _script.SettingsScript.Menu;

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

            if (PersonalPresetsMenu != null)
            {
                PersonalPresetsMenuMenuItem = new MenuItem("Personal Presets Menu", "The menu to manage the presets saved by you.")
                {
                    Label = "→→→"
                };

                AddMenuItem(PersonalPresetsMenuMenuItem);

                MenuController.AddSubmenu(this, PersonalPresetsMenu);
                MenuController.BindMenuItem(this, PersonalPresetsMenu, PersonalPresetsMenuMenuItem);
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

            if (SettingsMenu != null)
            {
                SettingsMenuMenuItem = new MenuItem("Settings Menu", "The menu to change available settings.")
                {
                    Label = "→→→"
                };

                AddMenuItem(SettingsMenuMenuItem);

                MenuController.AddSubmenu(this, SettingsMenu);
                MenuController.BindMenuItem(this, SettingsMenu, SettingsMenuMenuItem);
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
