using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using MenuAPI;

namespace HandlingEditor.Client
{
    internal class HandlingMenu
    {
        private readonly INotificationHandler notifier;
        private readonly HandlingEditor handlingEditor;

        #region Private Fields

        private MenuController m_menuController;
        private Menu m_mainMenu;
        private Menu m_editorMenu;
        private Menu m_personalPresetsMenu;
        private Menu m_serverPresetsMenu;
        private Menu m_settingsMenu;
        private bool m_showLockedFields = true;

        #endregion

        #region Delegates

        public delegate void EditorMenuPresetValueChangedEvent(string id, string value, string text);

        #endregion

        #region Public Events

        public event EditorMenuPresetValueChangedEvent MenuPresetValueChanged;

        public event EventHandler MenuResetPresetButtonPressed;
        public event EventHandler<string> MenuApplyPersonalPresetButtonPressed;
        public event EventHandler<string> MenuApplyServerPresetButtonPressed;
        public event EventHandler<string> MenuSavePersonalPresetButtonPressed;
        public event EventHandler<string> MenuSaveServerPresetButtonPressed;
        public event EventHandler<string> MenuDeletePersonalPresetButtonPressed;
        public event EventHandler<string> MenuDeleteServerPresetButtonPressed;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor with the <see cref="HandlingEditor"/> script
        /// </summary>
        internal HandlingMenu(HandlingEditor script)
        {
            notifier = Framework.Notifier;

            if (script == null)
                return;

            handlingEditor = script;

            // Used for the on screen keyboard
            AddTextEntry("HANDLING_EDITOR_ENTER_VALUE", "Enter value (without spaces)");
            InitializeMenu();

            handlingEditor.PresetChanged += new EventHandler((sender, args) => UpdateEditorMenu());
            handlingEditor.PersonalPresetsListChanged += new EventHandler((sender, args) => UpdatePersonalPresetsMenu());
            handlingEditor.ServerPresetsListChanged += new EventHandler((sender, args) => UpdateServerPresetsMenu());
        }

        #endregion

        #region Private Methods

        public void HideUI()
        {
            if (MenuController.IsAnyMenuOpen())
                MenuController.CloseAllMenus();
        }

        /// <summary>
        /// Setup the Menu to be used with the script
        /// </summary>
        private void InitializeMenu()
        {
            if(m_mainMenu == null)
            {
                m_mainMenu = new Menu(Globals.ScriptName, "Main Menu");
            }

            if (m_editorMenu == null)
            {
                m_editorMenu = new Menu(Globals.ScriptName, "Editor Menu");

                m_editorMenu.OnItemSelect += EditorMenu_OnItemSelect;
                m_editorMenu.OnDynamicListItemSelect += EditorMenu_OnDynamicListItemSelect;
                m_editorMenu.OnDynamicListItemCurrentItemChange += EditorMenu_OnDynamicListItemCurrentItemChange;
            }
            
            if (m_personalPresetsMenu == null)
            {
                m_personalPresetsMenu = new Menu(Globals.ScriptName, "Personal Presets Menu");

                m_personalPresetsMenu.OnItemSelect += PersonalPresetsMenu_OnItemSelect;

                #region Save/Delete Handler

                m_personalPresetsMenu.InstructionalButtons.Add(Control.PhoneExtraOption, GetLabelText("ITEM_SAVE"));
                m_personalPresetsMenu.InstructionalButtons.Add(Control.PhoneOption, GetLabelText("ITEM_DEL"));

                // Disable Controls binded on the same key
                m_personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.SelectWeapon, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));
                m_personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.VehicleExit, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));

                m_personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneExtraOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>(async (sender, control) =>
                {
                    string kvpName = await GetOnScreenString("");
                    MenuSavePersonalPresetButtonPressed?.Invoke(m_personalPresetsMenu, kvpName);
                }), true));
                m_personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) =>
                {
                    if (m_personalPresetsMenu.GetMenuItems().Count > 0)
                    {
                        string kvpName = m_personalPresetsMenu.GetMenuItems()[m_personalPresetsMenu.CurrentIndex].Text;
                        MenuDeletePersonalPresetButtonPressed?.Invoke(m_personalPresetsMenu, kvpName);
                    }
                }), true));

                #endregion
            }
            if (m_serverPresetsMenu == null)
            {
                m_serverPresetsMenu = new Menu(Globals.ScriptName, "Server Presets Menu");

                m_serverPresetsMenu.OnItemSelect += ServerPresetsMenu_OnItemSelect;
            }

            if(m_settingsMenu == null)
            {
                m_settingsMenu = new Menu(Globals.ScriptName, "Settings Menu");
            }

            UpdateSettingsMenu();
            UpdatePersonalPresetsMenu();
            UpdateServerPresetsMenu();
            UpdateEditorMenu();

            // Create Editor sub menu and bind item to a button
            var editorMenuItem = new MenuItem("Edit Preset", "The menu to edit the handling fields.")
            {
                Label = "→→→"
            };
            m_mainMenu.AddMenuItem(editorMenuItem);
            MenuController.BindMenuItem(m_mainMenu, m_editorMenu, editorMenuItem);

            // Create Personal Presets sub menu and bind item to a button
            var PersonalPresetsItem = new MenuItem("Personal Presets", "The menu containing the handling presets saved by you.")
            {
                Label = "→→→"
            };
            m_mainMenu.AddMenuItem(PersonalPresetsItem);
            MenuController.BindMenuItem(m_mainMenu, m_personalPresetsMenu, PersonalPresetsItem);

            // Create Server Presets sub menu and bind item to a button
            var ServerPresetsItem = new MenuItem("Server Presets", "The menu containing the handling presets loaded from the server.")
            {
                Label = "→→→"
            };
            m_mainMenu.AddMenuItem(ServerPresetsItem);
            MenuController.BindMenuItem(m_mainMenu, m_serverPresetsMenu, ServerPresetsItem);

            // Create Settings sub menu and bind item to a button
            var settingsMenuItem = new MenuItem("Settings", "The menu containing the handling editor settings.")
            {
                Label = "→→→"
            };
            m_mainMenu.AddMenuItem(settingsMenuItem);
            MenuController.BindMenuItem(m_mainMenu, m_settingsMenu, settingsMenuItem);

            if (m_menuController == null)
            {
                m_menuController = new MenuController();
                MenuController.AddMenu(m_mainMenu);
                MenuController.AddMenu(m_editorMenu);
                MenuController.AddMenu(m_personalPresetsMenu);
                MenuController.AddMenu(m_serverPresetsMenu);
                MenuController.AddMenu(m_settingsMenu);
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
                MenuController.MenuToggleKey = (Control)handlingEditor.Config.ToggleMenuControl;
                MenuController.EnableMenuToggleKeyOnController = false;
                MenuController.MainMenu = m_mainMenu;
            }
        }

        /// <summary>
        /// Invoked when the an item from the personal presets menu is selected
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="menuItem"></param>
        /// <param name="itemIndex"></param>
        private void PersonalPresetsMenu_OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex) => MenuApplyPersonalPresetButtonPressed?.Invoke(menu, menuItem.Text);

        /// <summary>
        /// Invoked when the an item from the server presets menu is selected
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="menuItem"></param>
        /// <param name="itemIndex"></param>
        private void ServerPresetsMenu_OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex) => MenuApplyServerPresetButtonPressed?.Invoke(menu, menuItem.Text);

        /// <summary>
        /// Invoked when an item from the main editor menu is selected
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="menuItem"></param>
        /// <param name="itemIndex"></param>
        private void EditorMenu_OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
        {
            // If the sender isn't the main editor menu...
            if (menu != m_editorMenu)
                return;

            if ((menuItem.ItemData as string) == "handling_reset")
            {
                MenuResetPresetButtonPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Invoked when a <see cref="MenuDynamicListItem"/> from the main editor menu is selected
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="dynamicListItem"></param>
        /// <param name="currentItem"></param>
        private async void EditorMenu_OnDynamicListItemSelect(Menu menu, MenuDynamicListItem dynamicListItem, string currentItem)
        {
            // If the item doesn't control any preset field...
            if (!(dynamicListItem.ItemData is HandlingFieldInfo fieldInfo))
                return;

            //var currentItem = dynamicListItem.CurrentItem;
            var itemText = dynamicListItem.Text;
            string fieldName = fieldInfo.Name;
            var fieldType = fieldInfo.Type;

            // Get the user input value
            string text = await GetOnScreenString(currentItem);

            // Check if the value can be accepted
            if (fieldType == HandlingFieldTypes.FloatType)
            {
                var min = (fieldInfo as HandlingFieldInfo<float>).Min;
                var max = (fieldInfo as HandlingFieldInfo<float>).Max;

                if (float.TryParse(text, out float newvalue))
                {
                    if (newvalue >= min && newvalue <= max)
                    {
                        dynamicListItem.CurrentItem = newvalue.ToString();
                        // Notify the value is changed so the preset can update...
                        MenuPresetValueChanged?.Invoke(fieldName, newvalue.ToString("F3"), itemText);
                    }
                    else
                        notifier.Notify($"Value out of allowed limits for ~b~{fieldName}~w~, Min:{min}, Max:{max}");
                }
                else
                    notifier.Notify($"Invalid value for ~b~{fieldName}~w~");
            }
            else if (fieldType == HandlingFieldTypes.IntType)
            {
                var min = (fieldInfo as HandlingFieldInfo<int>).Min;
                var max = (fieldInfo as HandlingFieldInfo<int>).Max;

                if (int.TryParse(text, out int newvalue))
                {
                    if (newvalue >= min && newvalue <= max)
                    {
                        dynamicListItem.CurrentItem = newvalue.ToString();
                        // Notify the value is changed so the preset can update...
                        MenuPresetValueChanged?.Invoke(fieldName, newvalue.ToString(), itemText);
                    }
                    else
                        notifier.Notify($"Value out of allowed limits for ~b~{fieldName}~w~, Min:{min}, Max:{max}");
                }
                else
                    notifier.Notify($"Invalid value for ~b~{fieldName}~w~");
            }
            else if (fieldType == HandlingFieldTypes.Vector3Type)
            {
                var min = (fieldInfo as HandlingFieldInfo<Vector3>).Min;
                var max = (fieldInfo as HandlingFieldInfo<Vector3>).Max;

                var minValueX = min.X;
                var minValueY = min.Y;
                var minValueZ = min.Z;
                var maxValueX = max.X;
                var maxValueY = max.Y;
                var maxValueZ = max.Z;

                if (itemText.EndsWith("_x"))
                {
                    if (float.TryParse(text, out float newvalue))
                    {
                        if (newvalue >= minValueX && newvalue <= maxValueX)
                        {
                            dynamicListItem.CurrentItem = newvalue.ToString("F3");
                            // Notify the value is changed so the preset can update...
                            MenuPresetValueChanged?.Invoke(fieldName, newvalue.ToString("F3"), itemText);
                        }
                        else
                            notifier.Notify($"Value out of allowed limits for ~b~{itemText}~w~, Min:{minValueX}, Max:{maxValueX}");
                    }
                    else
                        notifier.Notify($"Invalid value for ~b~{itemText}~w~");
                }
                else if (itemText.EndsWith("_y"))
                {
                    if (float.TryParse(text, out float newvalue))
                    {
                        if (newvalue >= minValueY && newvalue <= maxValueY)
                        {
                            dynamicListItem.CurrentItem = newvalue.ToString("F3");
                            // Notify the value is changed so the preset can update...
                            MenuPresetValueChanged?.Invoke(fieldName, newvalue.ToString("F3"), itemText);
                        }
                        else
                            notifier.Notify($"Value out of allowed limits for ~b~{itemText}~w~, Min:{minValueY}, Max:{maxValueY}");
                    }
                    else
                        notifier.Notify($"Invalid value for ~b~{itemText}~w~");
                }
                else if (itemText.EndsWith("_z"))
                {
                    if (float.TryParse(text, out float newvalue))
                    {
                        if (newvalue >= minValueZ && newvalue <= maxValueZ)
                        {
                            dynamicListItem.CurrentItem = newvalue.ToString("F3");
                            // Notify the value is changed so the preset can update...
                            MenuPresetValueChanged?.Invoke(fieldName, newvalue.ToString("F3"), itemText);
                        }
                        else
                            notifier.Notify($"Value out of allowed limits for ~b~{itemText}~w~, Min:{minValueZ}, Max:{maxValueZ}");
                    }
                    else
                        notifier.Notify($"Invalid value for ~b~{itemText}~w~");
                }
            }
        }

        /// <summary>
        /// Invoked when the value of a dynamic list item from the main editor menu is changed
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="dynamicListItem"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void EditorMenu_OnDynamicListItemCurrentItemChange(Menu menu, MenuDynamicListItem dynamicListItem, string oldValue, string newValue)
        {
            // If the sender isn't the main editor menu...
            if (menu != m_editorMenu)
                return;

            // If item data is not the expected one...
            if (!(dynamicListItem.ItemData is HandlingFieldInfo fieldInfo))
                return;

            // Get field name which is controlled by this dynamic list item
            string fieldName = fieldInfo.Name;

            // Notify the value is changed so the preset can update...
            MenuPresetValueChanged?.Invoke(fieldName, newValue, dynamicListItem.Text);
        }

        private void UpdateSettingsMenu()
        {
            if (m_settingsMenu == null)
                return;

            m_settingsMenu.ClearMenuItems();

            var showLockedFieldsCheckboxItem = new MenuCheckboxItem("Show Locked Fields", "Whether the editor menu should show or not the fields you can't edit.", m_showLockedFields)
            {
                ItemData = "handling_settings_show_locked_fields"
            };
            m_settingsMenu.AddMenuItem(showLockedFieldsCheckboxItem);

            m_settingsMenu.OnCheckboxChange += SettingsMenu_OnCheckboxChange;
        }

        private void SettingsMenu_OnCheckboxChange(Menu menu, MenuCheckboxItem menuItem, int itemIndex, bool newCheckedState)
        {
            // If the sender isn't the settings menu...
            if (menu != m_settingsMenu)
                return;

            if ((menuItem.ItemData as string) == "handling_settings_show_locked_fields")
            {
                m_showLockedFields = newCheckedState;
                UpdateEditorMenu();
            }
        }

        /// <summary>
        /// Rebuild the main editor menu
        /// </summary>
        private void UpdateEditorMenu()
        {
            if (m_editorMenu == null)
                return;

            m_editorMenu.ClearMenuItems();

            if (!handlingEditor.CurrentPresetIsValid)
                return;

            // Add all the controllers
            foreach (var item in handlingEditor.HandlingInfo.Fields)
            {
                var fieldInfo = item.Value;

                if (fieldInfo.Editable)
                {

                    //string fieldName = fieldInfo.Name;
                    //string fieldDescription = fieldInfo.Description;
                    Type fieldType = fieldInfo.Type;

                    if (fieldType == HandlingFieldTypes.FloatType)
                        AddDynamicFloatList(m_editorMenu, (HandlingFieldInfo<float>)item.Value);
                    else if (fieldType == HandlingFieldTypes.IntType)
                        AddDynamicIntList(m_editorMenu, (HandlingFieldInfo<int>)item.Value);
                    else if (fieldType == HandlingFieldTypes.Vector3Type)
                        AddDynamicVector3List(m_editorMenu, (HandlingFieldInfo<Vector3>)item.Value);
                }
                else
                {
                    if(m_showLockedFields)
                        AddLockedItem(m_editorMenu, item.Value);
                }
            }

            var resetItem = new MenuItem("Reset", "Restores the default values")
            {
                ItemData = "handling_reset",
            };
            m_editorMenu.AddMenuItem(resetItem);
        }

        /// <summary>
        /// Rebuild the personal presets menu
        /// </summary>
        private void UpdatePersonalPresetsMenu()
        {
            if (m_personalPresetsMenu == null)
                return;

            m_personalPresetsMenu.ClearMenuItems();

            foreach (var key in handlingEditor.LocalPresetsManager.GetKeys())
            {
                m_personalPresetsMenu.AddMenuItem(new MenuItem(key.Remove(0, Globals.KvpPrefix.Length)) { ItemData = key });
            }
        }

        /// <summary>
        /// Rebuild the server presets menu
        /// </summary>
        private void UpdateServerPresetsMenu()
        {
            if (m_serverPresetsMenu == null)
                return;

            m_serverPresetsMenu.ClearMenuItems();

            foreach (var key in handlingEditor.ServerPresetsManager.GetKeys())
                m_serverPresetsMenu.AddMenuItem(new MenuItem(key) { ItemData = key });
        }

        /// <summary>
        /// Get a string from the user using the on screen keyboard
        /// </summary>
        /// <param name="defaultText">The default value to display</param>
        /// <returns></returns>
        private async Task<string> GetOnScreenString(string defaultText)
        {
            //var currentMenu = MenuController.GetCurrentMenu();
            //currentMenu.Visible = false;
            //MenuController.DisableMenuButtons = true;

            //DisableAllControlActions(1);
   
            DisplayOnscreenKeyboard(1, "HANDLING_EDITOR_ENTER_VALUE", "", defaultText, "", "", "", 128);
            while (UpdateOnscreenKeyboard() != 1 && UpdateOnscreenKeyboard() != 2) await BaseScript.Delay(100);

            //EnableAllControlActions(1);

            //MenuController.DisableMenuButtons = false;
            //currentMenu.Visible = true;

            return GetOnscreenKeyboardResult();
        }

        /// <summary>
        /// The method that defines how a dynamic list item changes its value when you press left/right arrow
        /// </summary>
        /// <param name="item"></param>
        /// <param name="left"></param>
        /// <returns></returns>
        private string DynamicListChangeCallback(MenuDynamicListItem item, bool left)
        {
            var currentItem = item.CurrentItem;

            if (!(item.ItemData is HandlingFieldInfo fieldInfo))
                return currentItem;

            var itemText = item.Text;
            var fieldName = fieldInfo.Name;
            var fieldType = fieldInfo.Type;

            if (fieldType == HandlingFieldTypes.IntType)
            {
                int.TryParse(currentItem, out int value);
                var min = (fieldInfo as HandlingFieldInfo<int>).Min;
                var max = (fieldInfo as HandlingFieldInfo<int>).Max;

                if (left)
                {
                    var newvalue = value - 1;
                    if (newvalue < min)
                        notifier.Notify($"Min value allowed for ~b~{fieldName}~w~ is {min}");
                    else
                    {
                        value = newvalue;
                    }
                }
                else
                {
                    var newvalue = value + 1;
                    if (newvalue > max)
                        notifier.Notify($"Max value allowed for ~b~{fieldName}~w~ is {max}");
                    else
                    {
                        value = newvalue;
                    }
                }
                return value.ToString();
            }
            else if (fieldType == HandlingFieldTypes.FloatType)
            {
                float.TryParse(currentItem, out float value);
                var min = (fieldInfo as HandlingFieldInfo<float>).Min;
                var max = (fieldInfo as HandlingFieldInfo<float>).Max;

                if (left)
                {
                    var newvalue = value - handlingEditor.Config.FloatStep;
                    if (newvalue < min)
                        notifier.Notify($"Min value allowed for ~b~{fieldName}~w~ is {min}");
                    else
                    {
                        value = newvalue;
                    }
                }
                else
                {
                    var newvalue = value + handlingEditor.Config.FloatStep;
                    if (newvalue > max)
                        notifier.Notify($"Max value allowed for ~b~{fieldName}~w~ is {max}");
                    else
                    {
                        value = newvalue;
                    }
                }
                return value.ToString("F3");
            }
            else if (fieldType == HandlingFieldTypes.Vector3Type)
            {
                float.TryParse(currentItem, out float value);
                var min = (fieldInfo as HandlingFieldInfo<Vector3>).Min;
                var max = (fieldInfo as HandlingFieldInfo<Vector3>).Max;

                var minValueX = min.X;
                var minValueY = min.Y;
                var minValueZ = min.Z;
                var maxValueX = max.X;
                var maxValueY = max.Y;
                var maxValueZ = max.Z;

                if (itemText.EndsWith("_x"))
                {
                    if (left)
                    {
                        var newvalue = value - handlingEditor.Config.FloatStep;
                        if (newvalue < minValueX)
                            notifier.Notify($"Min value allowed for ~b~{itemText}~w~ is {minValueX}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    else
                    {
                        var newvalue = value + handlingEditor.Config.FloatStep;
                        if (newvalue > maxValueX)
                            notifier.Notify($"Max value allowed for ~b~{itemText}~w~ is {maxValueX}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    return value.ToString("F3");
                }
                else if (itemText.EndsWith("_y"))
                {
                    if (left)
                    {
                        var newvalue = value - handlingEditor.Config.FloatStep;
                        if (newvalue < minValueY)
                            notifier.Notify($"Min value allowed for ~b~{itemText}~w~ is {minValueY}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    else
                    {
                        var newvalue = value + handlingEditor.Config.FloatStep;
                        if (newvalue > maxValueY)
                            notifier.Notify($"Max value allowed for ~b~{itemText}~w~ is {maxValueY}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    return value.ToString("F3");
                }
                else if (itemText.EndsWith("_z"))
                {
                    if (left)
                    {
                        var newvalue = value - handlingEditor.Config.FloatStep;
                        if (newvalue < minValueZ)
                            notifier.Notify($"Min value allowed for ~b~{itemText}~w~ is {minValueZ}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    else
                    {
                        var newvalue = value + handlingEditor.Config.FloatStep;
                        if (newvalue > maxValueZ)
                            notifier.Notify($"Max value allowed for ~b~{itemText}~w~ is {maxValueZ}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    return value.ToString("F3");
                }
            }

            return currentItem;
        }

        private MenuDynamicListItem AddDynamicFloatList(Menu menu, HandlingFieldInfo<float> fieldInfo)
        {
            string fieldName = fieldInfo.Name;
            string description = fieldInfo.Description;

            if (!handlingEditor.CurrentPreset.Fields.TryGetValue(fieldName, out dynamic tmp))
                return null;

            var value = (float)tmp;
            var newitem = new MenuDynamicListItem(fieldName, value.ToString("F3"), DynamicListChangeCallback, description)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitem);

            return newitem;
        }

        private MenuDynamicListItem AddDynamicIntList(Menu menu, HandlingFieldInfo<int> fieldInfo)
        {
            string fieldName = fieldInfo.Name;
            string description = fieldInfo.Description;

            if (!handlingEditor.CurrentPreset.Fields.TryGetValue(fieldName, out dynamic tmp))
                return null;

            var value = (int)tmp;
            var newitem = new MenuDynamicListItem(fieldName, value.ToString(), DynamicListChangeCallback, description)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitem);

            return newitem;
        }

        private MenuDynamicListItem[] AddDynamicVector3List(Menu menu, HandlingFieldInfo<Vector3> fieldInfo)
        {
            string fieldName = fieldInfo.Name;

            if (!handlingEditor.CurrentPreset.Fields.TryGetValue(fieldName, out dynamic tmp))
                return null;

            var value = (Vector3)tmp;

            string fieldDescription = fieldInfo.Description;

            string fieldNameX = $"{fieldName}_x";
            string fieldNameY = $"{fieldName}_y";
            string fieldNameZ = $"{fieldName}_z";

            var newitemX = new MenuDynamicListItem(fieldNameX, value.X.ToString("F3"), DynamicListChangeCallback, fieldDescription)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitemX);

            var newitemY = new MenuDynamicListItem(fieldNameY, value.Y.ToString("F3"), DynamicListChangeCallback, fieldDescription)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitemY);

            var newitemZ = new MenuDynamicListItem(fieldNameZ, value.Z.ToString("F3"), DynamicListChangeCallback, fieldDescription)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitemZ);

            return new MenuDynamicListItem[3] { newitemX, newitemY, newitemZ };
        }

        private MenuItem AddLockedItem(Menu menu, HandlingFieldInfo fieldInfo)
        {
            var newitem = new MenuItem(fieldInfo.Name, fieldInfo.Description)
            {
                Enabled = false,
                RightIcon = MenuItem.Icon.LOCK,
                ItemData = fieldInfo,
            };

            menu.AddMenuItem(newitem);
            return newitem;
        }

        #endregion
    }
}