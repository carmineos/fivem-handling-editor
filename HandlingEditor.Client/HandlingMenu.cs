using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using MenuAPI;

namespace HandlingEditor.Client
{
    public class HandlingMenu : BaseScript
    {
        #region Editor Properties

        public string ScriptName => HandlingEditor.ScriptName;
        public string kvpPrefix => HandlingEditor.kvpPrefix;
        public float FloatStep => HandlingEditor.FloatStep;
        public int ToggleMenu => HandlingEditor.ToggleMenu;
        public bool CurrentPresetIsValid => HandlingEditor.CurrentPresetIsValid;
        public HandlingPreset CurrentPreset => HandlingEditor.CurrentPreset;
        public Dictionary<string, HandlingPreset> ServerPresets => HandlingEditor.ServerPresets;

        #endregion

        #region Menu Fields

        public MenuController menuController;
        public Menu EditorMenu;
        public Menu PersonalPresetsMenu;
        public Menu ServerPresetsMenu;
        public List<MenuDynamicListItem> HandlingListItems;

        #endregion

        #region Events

        public static event EventHandler ResetPresetButtonPressed;
        public static event EventHandler<string> ApplyPersonalPresetButtonPressed;
        public static event EventHandler<string> ApplyServerPresetButtonPressed;
        public static event EventHandler<string> SavePersonalPresetButtonPressed;
        public static event EventHandler<string> SaveServerPresetButtonPressed;
        public static event EventHandler<string> DeletePersonalPresetButtonPressed;
        public static event EventHandler<string> DeleteServerPresetButtonPressed;

        #endregion

        #region Constructor

        public HandlingMenu()
        {
            Tick += OnTick;
            HandlingEditor.PresetChanged += new EventHandler((sender,args) => InitializeMenu());
            HandlingEditor.PersonalPresetsListChanged += new EventHandler((sender,args) => UpdatePersonalPresetsMenu());
            HandlingEditor.ServerPresetsListChanged += new EventHandler((sender,args) => UpdateServerPresetsMenu());
        }

        #endregion

        #region Tasks
        
        private async Task OnTick()
        {
            if (!CurrentPresetIsValid)
            {
                if (MenuController.IsAnyMenuOpen())
                    MenuController.CloseAllMenus();
            }
        }
        
        #endregion

        #region Menu Methods

        private void InitializeMenu()
        {
            if (EditorMenu == null)
            {
                EditorMenu = new Menu(ScriptName, "Editor");
            }
            
            if (PersonalPresetsMenu == null)
            {
                PersonalPresetsMenu = new Menu(ScriptName, "Personal Presets");

                PersonalPresetsMenu.InstructionalButtons.Add(Control.PhoneExtraOption, GetLabelText("ITEM_SAVE"));
                PersonalPresetsMenu.InstructionalButtons.Add(Control.PhoneOption, GetLabelText("ITEM_DEL"));

                // Disable Controls binded on the same key
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.SelectWeapon, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.VehicleExit, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));
                
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneExtraOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control> (async (sender, control) =>
                {
                    string kvpName = await GetOnScreenString("");
                    SavePersonalPresetButtonPressed(PersonalPresetsMenu, kvpName);
                }) , true));
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) =>
                {
                    if (PersonalPresetsMenu.GetMenuItems().Count > 0)
                    {
                        string kvpName = PersonalPresetsMenu.GetMenuItems()[PersonalPresetsMenu.CurrentIndex].Text;
                        DeletePersonalPresetButtonPressed(PersonalPresetsMenu, kvpName);
                    }
                }), true));

                PersonalPresetsMenu.OnItemSelect += (sender, item, index) =>
                {
                    ApplyPersonalPresetButtonPressed.Invoke(sender, item.Text);

                    UpdateEditorMenu();
                };

            }
            if (ServerPresetsMenu == null)
            {
                ServerPresetsMenu = new Menu(ScriptName, "Server Presets");
                
                ServerPresetsMenu.OnItemSelect += (sender, item, index) =>
                {
                    ApplyServerPresetButtonPressed.Invoke(sender, item.Text);

                    
                    UpdateEditorMenu();

                };
            }

            UpdatePersonalPresetsMenu();
            UpdateServerPresetsMenu();
            UpdateEditorMenu();

            if (menuController == null)
            {
                menuController = new MenuController();
                MenuController.AddMenu(EditorMenu);
                MenuController.AddMenu(PersonalPresetsMenu);
                MenuController.AddMenu(ServerPresetsMenu);
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
                MenuController.MenuToggleKey = (Control)ToggleMenu;
                MenuController.EnableMenuToggleKeyOnController = false;
                MenuController.MainMenu = EditorMenu;
            }
        }

        private void UpdateEditorMenu()
        {
            if (EditorMenu == null)
                return;

            EditorMenu.ClearMenuItems();

            // Create Personal Presets sub menu and bind item to a button
            var PersonalPresetsItem = new MenuItem("Personal Presets", "The handling presets saved by you.")
            {
                Label = "→→→"
            };
            EditorMenu.AddMenuItem(PersonalPresetsItem);
            MenuController.BindMenuItem(EditorMenu, PersonalPresetsMenu, PersonalPresetsItem);

            // Create Server Presets sub menu and bind item to a button
            var ServerPresetsItem = new MenuItem("Server Presets", "The handling presets loaded from the server.")
            {
                Label = "→→→"
            };
            EditorMenu.AddMenuItem(ServerPresetsItem);
            MenuController.BindMenuItem(EditorMenu, ServerPresetsMenu, ServerPresetsItem);

            // Add all the controllers
            foreach (var item in HandlingInfo.FieldsInfo)
            {
                var fieldInfo = item.Value;

                if (fieldInfo.Editable)
                {

                    //string fieldName = fieldInfo.Name;
                    //string fieldDescription = fieldInfo.Description;
                    Type fieldType = fieldInfo.Type;

                    if (fieldType == FieldType.FloatType)
                        AddDynamicFloatList(EditorMenu, (FieldInfo<float>)item.Value);
                    else if (fieldType == FieldType.IntType)
                        AddDynamicIntList(EditorMenu, (FieldInfo<int>)item.Value);
                    else if (fieldType == FieldType.Vector3Type)
                        AddDynamicVector3List(EditorMenu, (FieldInfo<Vector3>)item.Value);
                }
                else
                {
                    AddLockedItem(EditorMenu, item.Value);
                }
            }

            var resetItem = new MenuItem("Reset", "Restores the default values");
            EditorMenu.AddMenuItem(resetItem);

            EditorMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == resetItem)
                {
                    ResetPresetButtonPressed(this, EventArgs.Empty);
                }
            };

            //UpdatePersonalPresetsMenu();
            //UpdateServerPresetsMenu();

        }

        private void UpdatePersonalPresetsMenu()
        {
            if (PersonalPresetsMenu == null)
                return;
            PersonalPresetsMenu.ClearMenuItems();

            KvpEnumerable kvpList = new KvpEnumerable(kvpPrefix);
            foreach (var key in kvpList)
            {
                string value = GetResourceKvpString(key);
                PersonalPresetsMenu.AddMenuItem(new MenuItem(key.Remove(0, kvpPrefix.Length)));
            }
        }

        private void UpdateServerPresetsMenu()
        {
            if (ServerPresetsMenu == null)
                return;

            ServerPresetsMenu.ClearMenuItems();

            foreach (var preset in ServerPresets)
                ServerPresetsMenu.AddMenuItem(new MenuItem(preset.Key));
        }

        private async Task<string> GetOnScreenString(string defaultText)
        {
            var currentMenu = MenuController.GetCurrentMenu();
            currentMenu.Visible = false;

            MenuController.DisableMenuButtons = true;
            DisableAllControlActions(1);

            AddTextEntry("HANDLING_EDITOR_ENTER_VALUE", "Enter value (without spaces)");
            DisplayOnscreenKeyboard(1, "HANDLING_EDITOR_ENTER_VALUE", "", defaultText, "", "", "", 128);
            while (UpdateOnscreenKeyboard() != 1 && UpdateOnscreenKeyboard() != 2) await Delay(200);

            EnableAllControlActions(1);
            MenuController.DisableMenuButtons = false;
            currentMenu.Visible = true;

            return GetOnscreenKeyboardResult();
        }

        private MenuDynamicListItem AddDynamicFloatList(Menu menu, FieldInfo<float> fieldInfo)
        {
            string name = fieldInfo.Name;
            string description = fieldInfo.Description;
            float min = fieldInfo.Min;
            float max = fieldInfo.Max;

            if (!CurrentPreset.Fields.ContainsKey(name))
                return null;

            float value = CurrentPreset.Fields[name];
            string FloatChangeCallBack(MenuDynamicListItem item, bool left)
            {
                if (left)
                {
                    var newvalue = value - FloatStep;
                    if (newvalue < min)
                        Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{name}~w~ is {min}");
                    else
                    {
                        value = newvalue;
                        CurrentPreset.Fields[name] = newvalue;
                    }
                }
                else
                {
                    var newvalue = value + FloatStep;
                    if (newvalue > max)
                        Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{name}~w~ is {max}");
                    else
                    {
                        value = newvalue;
                        CurrentPreset.Fields[name] = newvalue;
                    }
                }
                return value.ToString("F3");
            };
            var newitem = new MenuDynamicListItem(name, value.ToString("F3"), FloatChangeCallBack, description)
            {
                ItemData = name
            };
            menu.AddMenuItem(newitem);

            EditorMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item.ItemData == newitem.ItemData)
                {
                    string text = await GetOnScreenString(value.ToString());

                    float newvalue = value;

                    if (float.TryParse(text, out newvalue))
                    {
                        if (newvalue >= min && newvalue <= max)
                        {
                            ((MenuDynamicListItem)item).CurrentItem = newvalue.ToString();
                            CurrentPreset.Fields[name] = newvalue;
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}:  Value out of allowed limits for ~b~{name}~w~, Min:{min}, Max:{max}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}:  Invalid value for ~b~{name}~w~");
                }
            };

            return newitem;
        }

        private MenuDynamicListItem AddDynamicIntList(Menu menu, FieldInfo<int> fieldInfo)
        {
            string name = fieldInfo.Name;
            string description = fieldInfo.Description;
            int min = fieldInfo.Min;
            int max = fieldInfo.Max;

            if (!CurrentPreset.Fields.ContainsKey(name))
                return null;

            int value = CurrentPreset.Fields[name];
            string IntChangeCallBack(MenuDynamicListItem item, bool left)
            {
                if (left)
                {
                    var newvalue = value - 1;
                    if (newvalue < min)
                        Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{name}~w~ is {min}");
                    else
                    {
                        value = newvalue;
                        CurrentPreset.Fields[name] = newvalue;
                    }
                }
                else
                {
                    var newvalue = value + 1;
                    if (newvalue > max)
                        Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{name}~w~ is {max}");
                    else
                    {
                        value = newvalue;
                        CurrentPreset.Fields[name] = newvalue;
                    }
                }
                return value.ToString();
            };
            var newitem = new MenuDynamicListItem(name, value.ToString(), IntChangeCallBack, description)
            {
                ItemData = name
            };
            menu.AddMenuItem(newitem);

            EditorMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == newitem)
                {
                    string text = await GetOnScreenString(value.ToString());

                    int newvalue = value;

                    if (int.TryParse(text, out newvalue))
                    {
                        if (newvalue >= min && newvalue <= max)
                        {
                            ((MenuDynamicListItem)item).CurrentItem = newvalue.ToString();
                            CurrentPreset.Fields[name] = newvalue;
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{name}~w~, Min:{min}, Max:{max}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{name}~w~");                    
                }
            };

            return newitem;
        }

        private MenuDynamicListItem[] AddDynamicVector3List(Menu menu, FieldInfo<Vector3> fieldInfo)
        {
            string fieldName = fieldInfo.Name;

            if (!CurrentPreset.Fields.ContainsKey(fieldName))
                return null;

            string fieldDescription = fieldInfo.Description;
            Vector3 fieldMin = fieldInfo.Min;
            Vector3 fieldMax = fieldInfo.Max;

            string fieldNameX = $"{fieldName}_x";
            float valueX = CurrentPreset.Fields[fieldName].X;
            float minValueX = fieldMin.X;
            float maxValueX = fieldMax.X;
            string XChangeCallback(MenuDynamicListItem item, bool left)
            {
                if (left)
                {
                    var newvalue = valueX - FloatStep;
                    if (newvalue < minValueX)
                        Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{fieldNameX}~w~ is {minValueX}");
                    else
                    {
                        valueX = newvalue;
                        CurrentPreset.Fields[fieldInfo.Name].X = newvalue;
                    }
                }
                else
                {
                    var newvalue = valueX + FloatStep;
                    if (newvalue > maxValueX)
                        Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{fieldNameX}~w~ is {maxValueX}");
                    else
                    {
                        valueX = newvalue;
                        CurrentPreset.Fields[fieldInfo.Name].X = newvalue;
                    }
                }
                return valueX.ToString("F3");
            }
            var newitemX = new MenuDynamicListItem(fieldNameX, valueX.ToString("F3"), XChangeCallback, fieldDescription)
            {
                ItemData = fieldNameX
            };
            menu.AddMenuItem(newitemX);

            string fieldNameY = $"{fieldName}_y";
            float valueY = CurrentPreset.Fields[fieldName].Y;
            float minValueY = fieldMin.Y;
            float maxValueY = fieldMax.Y;
            string YChangeCallback(MenuDynamicListItem item, bool left)
            {
                if (left)
                {
                    var newvalue = valueY - FloatStep;
                    if (newvalue < minValueY)
                        Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{fieldNameY}~w~ is {minValueY}");
                    else
                    {
                        valueY = newvalue;
                        CurrentPreset.Fields[fieldInfo.Name].Y = newvalue;
                    }
                }
                else
                {
                    var newvalue = valueY + FloatStep;
                    if (newvalue > maxValueY)
                        Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{fieldNameY}~w~ is {maxValueY}");
                    else
                    {
                        valueY = newvalue;
                        CurrentPreset.Fields[fieldInfo.Name].Y = newvalue;
                    }
                }
                return valueY.ToString("F3");
            }
            var newitemY = new MenuDynamicListItem(fieldNameY, valueY.ToString("F3"), YChangeCallback, fieldDescription)
            {
                ItemData = fieldNameY
            };
            menu.AddMenuItem(newitemY);

            string fieldNameZ = $"{fieldName}_z";
            float valueZ = CurrentPreset.Fields[fieldName].Z;
            float minValueZ = fieldMin.Z;
            float maxValueZ = fieldMax.Z;
            string ZChangeCallBack(MenuDynamicListItem item, bool left)
            {
                if (left)
                {
                    var newvalue = valueZ - FloatStep;
                    if (newvalue < minValueZ)
                        Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{fieldNameZ}~w~ is {minValueZ}");
                    else
                    {
                        valueZ = newvalue;
                        CurrentPreset.Fields[fieldInfo.Name].Z = newvalue;
                    }
                }
                else
                {
                    var newvalue = valueZ + FloatStep;
                    if (newvalue > maxValueZ)
                        Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{fieldNameZ}~w~ is {maxValueZ}");
                    else
                    {
                        valueZ = newvalue;
                        CurrentPreset.Fields[fieldInfo.Name].Z = newvalue;
                    }
                }
                return valueZ.ToString("F3");
            }
            var newitemZ = new MenuDynamicListItem(fieldNameZ, valueZ.ToString("F3"), ZChangeCallBack, fieldDescription)
            {
                ItemData = fieldNameZ
            };
            menu.AddMenuItem(newitemZ);

            EditorMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == newitemX)
                {
                    string text = await GetOnScreenString(valueX.ToString());

                    float newvalue = valueX;

                    if (float.TryParse(text, out newvalue))
                    {
                        if (newvalue >= minValueX && newvalue <= maxValueX)
                        {
                            ((MenuDynamicListItem)item).CurrentItem = newvalue.ToString();
                            CurrentPreset.Fields[fieldName].X = newvalue;
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{fieldNameX}~w~, Min:{minValueX}, Max:{maxValueX}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{fieldNameX}~w~");
                }
                else if (item == newitemY)
                {
                    string text = await GetOnScreenString(valueY.ToString());

                    float newvalue = valueY;

                    if (float.TryParse(text, out newvalue))
                    {
                        if (newvalue >= minValueY && newvalue <= maxValueY)
                        {
                            ((MenuDynamicListItem)item).CurrentItem = newvalue.ToString();
                            CurrentPreset.Fields[fieldName].Y = newvalue;
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{fieldNameY}~w~, Min:{minValueY}, Max:{maxValueY}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{fieldNameY}~w~");
                }
                else if (item == newitemZ)
                {
                    string text = await GetOnScreenString(valueZ.ToString());

                    float newvalue = valueZ;

                    if (float.TryParse(text, out newvalue))
                    {
                        if (newvalue >= minValueZ && newvalue <= maxValueZ)
                        {
                            ((MenuDynamicListItem)item).CurrentItem = newvalue.ToString();
                            CurrentPreset.Fields[fieldName].Z = newvalue;
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{fieldNameZ}~w~, Min:{minValueZ}, Max:{maxValueZ}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{fieldNameZ}~w~");
                }
            };

            return new MenuDynamicListItem[3] { newitemX, newitemY, newitemZ };
        }

        private MenuItem AddLockedItem(Menu menu, BaseFieldInfo fieldInfo)
        {
            var newitem = new MenuItem(fieldInfo.Name, fieldInfo.Description)
            {
                Enabled = false,
                RightIcon = MenuItem.Icon.LOCK,
                ItemData = fieldInfo.Name,
            };

            menu.AddMenuItem(newitem);

            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    Screen.ShowNotification($"{ScriptName}: The server doesn't allow to edit this field.");
                }
            };
            return newitem;
        }

        #endregion
    }
}