using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NativeUI;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System.Drawing;

namespace HandlingEditor.Client
{
    public class HandlingMenu : BaseScript
    {
        #region EDITOR PROPERTIES

        public string ScriptName => HandlingEditor.ScriptName;
        public string kvpPrefix => HandlingEditor.kvpPrefix;
        public float FloatStep => HandlingEditor.FloatStep;
        public float FloatPrecision => HandlingEditor.FloatPrecision;
        public int ToggleMenu => HandlingEditor.ToggleMenu;
        public static float ScreenPosX => HandlingEditor.ScreenPosX;
        public static float ScreenPosY => HandlingEditor.ScreenPosY;
        public int CurrentVehicle => HandlingEditor.CurrentVehicle;
        public HandlingPreset CurrentPreset => HandlingEditor.CurrentPreset;
        public Dictionary<string, HandlingPreset> ServerPresets => HandlingEditor.ServerPresets;

        #endregion

        #region MENU FIELDS

        public MenuPool _menuPool { get; private set; }
        public UIMenu EditorMenu { get; private set; }
        public UIMenu PersonalPresetsMenu { get; private set; }
        public UIMenu ServerPresetsMenu { get; private set; }
        public List<UIMenuDynamicListItem> HandlingListItems { get; private set; }

        #endregion

        #region EVENTS

        public static event EventHandler ResetPreset_Pressed;
        public static event EventHandler<string> ApplyPersonalPreset_Pressed;
        public static event EventHandler<string> ApplyServerPreset_Pressed;
        public static event EventHandler<string> SavePersonalPreset_Pressed;
        public static event EventHandler<string> SaveServerPreset_Pressed;
        public static event EventHandler<string> DeletePersonalPreset_Pressed;
        public static event EventHandler<string> DeleteServerPreset_Pressed;

        #endregion

        #region CONSTRUCTOR

        public HandlingMenu()
        {
            Tick += OnTick;
            HandlingEditor.Menu_Outdated += new EventHandler((sender,args) => InitializeMenu());
            HandlingEditor.PersonalPresetsMenu_Outdated += new EventHandler((sender,args) => UpdatePersonalPresetsMenu());
            HandlingEditor.ServerPresetsMenu_Outdated += new EventHandler((sender,args) => UpdateServerPresetsMenu());
        }

        #endregion

        #region TASKS

        private async Task OnTick()
        {
            if (_menuPool != null)
            {
                _menuPool.ProcessMenus();

                if (_menuPool.IsAnyMenuOpen())
                    HandlingEditor.DisableControls();

                if (CurrentVehicle != -1 && CurrentPreset != null)
                {
                    if (IsControlJustPressed(1, ToggleMenu)/* || IsDisabledControlJustPressed(1, toggleMenu)*/) // TOGGLE MENU VISIBLE
                    {
                        if (_menuPool.IsAnyMenuOpen())
                            _menuPool.CloseAllMenus();
                        else
                        {
                            if (!EditorMenu.Visible)
                                EditorMenu.Visible = true;
                        }

                    }

                    if (PersonalPresetsMenu.Visible)
                    {
                        HandlingEditor.DisableControls2();

                        // Save Button pressed
                        if (IsControlJustPressed(1, 179))
                        {
                            string kvpName = await GetOnScreenString("");
                            SavePersonalPreset_Pressed(PersonalPresetsMenu, kvpName);
                        }
                        // Delete Button pressed
                        else if (IsControlJustPressed(1, 178))
                        {
                            if (PersonalPresetsMenu.MenuItems.Count > 0)
                            {
                                string kvpName = PersonalPresetsMenu.MenuItems[PersonalPresetsMenu.CurrentSelection].Text;
                                DeletePersonalPreset_Pressed(PersonalPresetsMenu, kvpName);
                            }
                        }
                    }
                }
                else
                {
                    _menuPool.CloseAllMenus();
                }
            }
        }

        #endregion

        #region MENU METHODS

        private void InitializeMenu()
        {
            if (EditorMenu == null)
            {
                EditorMenu = new UIMenu(ScriptName, "Editor", new PointF(ScreenPosX * Screen.Width, ScreenPosY * Screen.Height))
                {
                    MouseEdgeEnabled = false,
                    ControlDisablingEnabled = false,
                    MouseControlsEnabled = false
                };
            }
            if (PersonalPresetsMenu == null)
            {
                PersonalPresetsMenu = new UIMenu(ScriptName, "Personal Presets", new PointF(ScreenPosX * Screen.Width, ScreenPosY * Screen.Height))
                {
                    MouseEdgeEnabled = false,
                    ControlDisablingEnabled = false,
                    MouseControlsEnabled = false,
                };

                PersonalPresetsMenu.AddInstructionalButton(new InstructionalButton(Control.PhoneExtraOption, GetLabelText("ITEM_SAVE")));
                PersonalPresetsMenu.AddInstructionalButton(new InstructionalButton(Control.PhoneOption, GetLabelText("ITEM_DEL")));


                PersonalPresetsMenu.OnItemSelect += (sender, item, index) =>
                {
                    ApplyPersonalPreset_Pressed.Invoke(sender, item.Text);

                    int currentSelection = PersonalPresetsMenu.CurrentSelection;
                    UpdateEditorMenu();
                    PersonalPresetsMenu.CurrentSelection = currentSelection;
                };

            }
            if (ServerPresetsMenu == null)
            {
                ServerPresetsMenu = new UIMenu(ScriptName, "Server Presets", new PointF(ScreenPosX * Screen.Width, ScreenPosY * Screen.Height))
                {
                    MouseEdgeEnabled = false,
                    ControlDisablingEnabled = false,
                    MouseControlsEnabled = false,
                };

                ServerPresetsMenu.OnItemSelect += (sender, item, index) =>
                {
                    ApplyServerPreset_Pressed.Invoke(sender, item.Text);

                    int currentSelection = ServerPresetsMenu.CurrentSelection;
                    UpdateEditorMenu();
                    ServerPresetsMenu.CurrentSelection = currentSelection;
                };

            }

            UpdateEditorMenu();

            if (_menuPool == null)
            {
                _menuPool = new MenuPool()
                {
                    ResetCursorOnOpen = true
                };
            }
            else _menuPool.ToList().Clear();

            _menuPool.Add(EditorMenu);
            _menuPool.Add(PersonalPresetsMenu);
            _menuPool.Add(ServerPresetsMenu);

            _menuPool.RefreshIndex();
        }

        private void UpdateEditorMenu()
        {
            if (EditorMenu == null)
                return;

            EditorMenu.Clear();

            var PersonalPresetsItem = new UIMenuItem("Personal Presets", "The handling presets saved by you.");
            PersonalPresetsItem.SetRightLabel("→→→");
            EditorMenu.AddItem(PersonalPresetsItem);
            EditorMenu.BindMenuToItem(PersonalPresetsMenu, PersonalPresetsItem);

            var ServerPresetsItem = new UIMenuItem("Server Presets", "The handling presets loaded from the server.");
            ServerPresetsItem.SetRightLabel("→→→");
            EditorMenu.AddItem(ServerPresetsItem);
            EditorMenu.BindMenuToItem(ServerPresetsMenu, ServerPresetsItem);

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
                    //AddLockedItem(EditorMenu, item.Value);
                }
            }

            var resetItem = new UIMenuItem("Reset", "Restores the default values");
            EditorMenu.AddItem(resetItem);

            EditorMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == resetItem)
                {
                    ResetPreset_Pressed.Invoke(this, EventArgs.Empty);
                }
            };

            UpdatePersonalPresetsMenu();
            UpdateServerPresetsMenu();

            EditorMenu.RefreshIndex();
        }

        private void UpdatePersonalPresetsMenu()
        {
            if (PersonalPresetsMenu == null)
                return;
            PersonalPresetsMenu.Clear();

            KvpEnumerable kvpList = new KvpEnumerable(kvpPrefix);
            foreach (var key in kvpList)
            {
                string value = GetResourceKvpString(key);
                PersonalPresetsMenu.AddItem(new UIMenuItem(key.Remove(0, kvpPrefix.Length)));
            }

            PersonalPresetsMenu.RefreshIndex();
        }

        private void UpdateServerPresetsMenu()
        {
            if (ServerPresetsMenu == null)
                return;

            ServerPresetsMenu.Clear();

            foreach (var preset in ServerPresets)
                ServerPresetsMenu.AddItem(new UIMenuItem(preset.Key));

            ServerPresetsMenu.RefreshIndex();
        }

        private async Task<string> GetOnScreenString(string defaultText)
        {
            DisableAllControlActions(1);
            AddTextEntry("ENTER_VALUE", "Enter value");
            DisplayOnscreenKeyboard(1, "ENTER_VALUE", "", defaultText, "", "", "", 128);
            while (UpdateOnscreenKeyboard() != 1 && UpdateOnscreenKeyboard() != 2) await Delay(100);
            EnableAllControlActions(1);
            return GetOnscreenKeyboardResult();
        }

        private UIMenuDynamicListItem AddDynamicFloatList(UIMenu menu, FieldInfo<float> fieldInfo)
        {
            string name = fieldInfo.Name;
            string description = fieldInfo.Description;
            float min = fieldInfo.Min;
            float max = fieldInfo.Max;

            if (!CurrentPreset.Fields.ContainsKey(name))
                return null;

            float value = CurrentPreset.Fields[name];
            string FloatChangeCallBack(UIMenuDynamicListItem item, UIMenuDynamicListItem.ChangeDirection direction)
            {
                if (direction == UIMenuDynamicListItem.ChangeDirection.Left)
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
            var newitem = new UIMenuDynamicListItem(name, description, value.ToString("F3"), FloatChangeCallBack);

            menu.AddItem(newitem);

            EditorMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == newitem)
                {
                    EditorMenu.Visible = false;

                    string text = await GetOnScreenString(value.ToString());
                    float newvalue = value;

                    if (float.TryParse(text, out newvalue))
                    {
                        if (newvalue >= min && newvalue <= max)
                            CurrentPreset.Fields[name] = newvalue;
                        else
                            Screen.ShowNotification($"{ScriptName}:  Value out of allowed limits for ~b~{name}~w~, Min:{min}, Max:{max}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}:  Invalid value for ~b~{name}~w~");

                    int currentSelection = EditorMenu.CurrentSelection;
                    UpdateEditorMenu(); //Should just update the current item instead
                    EditorMenu.CurrentSelection = currentSelection;
                    EditorMenu.Visible = true;
                }
            };

            return newitem;
        }

        private UIMenuDynamicListItem AddDynamicIntList(UIMenu menu, FieldInfo<int> fieldInfo)
        {
            string name = fieldInfo.Name;
            string description = fieldInfo.Description;
            int min = fieldInfo.Min;
            int max = fieldInfo.Max;

            if (!CurrentPreset.Fields.ContainsKey(name))
                return null;

            int value = CurrentPreset.Fields[name];
            string IntChangeCallBack(UIMenuDynamicListItem item, UIMenuDynamicListItem.ChangeDirection direction)
            {
                if (direction == UIMenuDynamicListItem.ChangeDirection.Left)
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
            var newitem = new UIMenuDynamicListItem(name, description, value.ToString(), IntChangeCallBack);

            menu.AddItem(newitem);

            EditorMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == newitem)
                {
                    EditorMenu.Visible = false;

                    string text = await GetOnScreenString(value.ToString());
                    int newvalue = value;

                    if (int.TryParse(text, out newvalue))
                    {
                        if (newvalue >= min && newvalue <= max)
                            CurrentPreset.Fields[name] = newvalue;
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{name}~w~, Min:{min}, Max:{max}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{name}~w~");

                    int currentSelection = EditorMenu.CurrentSelection;
                    UpdateEditorMenu();  //Should just update the current item instead
                    EditorMenu.CurrentSelection = currentSelection;
                    EditorMenu.Visible = true;
                }
            };

            return newitem;
        }

        private UIMenuDynamicListItem[] AddDynamicVector3List(UIMenu menu, FieldInfo<Vector3> fieldInfo)
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

            var newitemX = new UIMenuDynamicListItem(fieldNameX, fieldDescription, valueX.ToString("F3"), (sender, direction) =>
            {
                if (direction == UIMenuDynamicListItem.ChangeDirection.Left)
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
            });

            menu.AddItem(newitemX);

            string fieldNameY = $"{fieldName}_y";
            float valueY = CurrentPreset.Fields[fieldName].Y;
            float minValueY = fieldMin.Y;
            float maxValueY = fieldMax.Y;

            var newitemY = new UIMenuDynamicListItem(fieldNameY, fieldDescription, valueY.ToString("F3"), (sender, direction) =>
            {
                if (direction == UIMenuDynamicListItem.ChangeDirection.Left)
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
            });

            menu.AddItem(newitemY);

            string fieldNameZ = $"{fieldName}_z";
            float valueZ = CurrentPreset.Fields[fieldName].Z;
            float minValueZ = fieldMin.Z;
            float maxValueZ = fieldMax.Z;

            var newitemZ = new UIMenuDynamicListItem(fieldNameZ, fieldDescription, valueZ.ToString("F3"), (sender, direction) =>
            {
                if (direction == UIMenuDynamicListItem.ChangeDirection.Left)
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
            });

            menu.AddItem(newitemZ);

            EditorMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == newitemX)
                {
                    EditorMenu.Visible = false;

                    string text = await GetOnScreenString(valueX.ToString());
                    float newvalue = valueX;

                    if (float.TryParse(text, out newvalue))
                    {
                        if (newvalue >= minValueX && newvalue <= maxValueX)
                            CurrentPreset.Fields[fieldName].X = newvalue;
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{fieldNameX}~w~, Min:{minValueX}, Max:{maxValueX}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{fieldNameX}~w~");

                    int currentSelection = EditorMenu.CurrentSelection;
                    UpdateEditorMenu();  //Should just update the current item instead
                    EditorMenu.CurrentSelection = currentSelection;
                    EditorMenu.Visible = true;
                }
                else if (item == newitemY)
                {
                    EditorMenu.Visible = false;

                    string text = await GetOnScreenString(valueY.ToString());
                    float newvalue = valueY;

                    if (float.TryParse(text, out newvalue))
                    {
                        if (newvalue >= minValueY && newvalue <= maxValueY)
                            CurrentPreset.Fields[fieldName].Y = newvalue;
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{fieldNameY}~w~, Min:{minValueY}, Max:{maxValueY}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{fieldNameY}~w~");

                    int currentSelection = EditorMenu.CurrentSelection;
                    UpdateEditorMenu();  //Should just update the current item instead
                    EditorMenu.CurrentSelection = currentSelection;
                    EditorMenu.Visible = true;
                }
                else if (item == newitemZ)
                {
                    EditorMenu.Visible = false;

                    string text = await GetOnScreenString(valueZ.ToString());
                    float newvalue = valueZ;

                    if (float.TryParse(text, out newvalue))
                    {
                        if (newvalue >= minValueZ && newvalue <= maxValueZ)
                            CurrentPreset.Fields[fieldName].Z = newvalue;
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{fieldNameZ}~w~, Min:{minValueZ}, Max:{maxValueZ}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{fieldNameZ}~w~");

                    int currentSelection = EditorMenu.CurrentSelection;
                    UpdateEditorMenu();  //Should just update the current item instead
                    EditorMenu.CurrentSelection = currentSelection;
                    EditorMenu.Visible = true;
                }
            };

            return new UIMenuDynamicListItem[3] { newitemX, newitemY, newitemZ };
        }

        private UIMenuItem AddLockedItem(UIMenu menu, BaseFieldInfo fieldInfo)
        {
            var newitem = new UIMenuItem(fieldInfo.Name, fieldInfo.Description);
            newitem.Enabled = false;
            newitem.SetRightBadge(UIMenuItem.BadgeStyle.Lock);

            menu.AddItem(newitem);

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