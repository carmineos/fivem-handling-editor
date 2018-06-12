using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Text;
using System.Collections;
using NativeUI;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using HandlingEditor;
using System.Xml;

namespace handling_editor
{
    public class HandlingEditor : BaseScript
    {
        #region CONFIG_FIEDS
        private static float editingFactor;
        private static float maxSyncDistance;
        private static long timer;
        private static bool debug;
        private static int toggleMenu;
        private static float screenPosX;
        private static float screenPosY;
        internal static string kvpPrefix = "handling_";
        #endregion

        #region FIELDS
        private static HandlingInfo handlingInfo;
        private Dictionary<string,HandlingPreset> serverPresets;
        private long currentTime;
        private long lastTime;
        private int playerPed;
        private int currentVehicle;
        private HandlingPreset currentPreset;
        private IEnumerable<int> vehicles;
        #endregion

        #region GUI_FIELDS
        private MenuPool _menuPool;
        private UIMenu EditorMenu;
        private UIMenu presetsMenu;
        private UIMenu serverPresetsMenu;
        #endregion

        private async Task<string> GetOnScreenValue(string defaultText)
        {
            DisableAllControlActions(1);
            AddTextEntry("ENTER_VALUE", "Enter value");
            DisplayOnscreenKeyboard(1, "ENTER_VALUE", "", defaultText, "", "", "", 128);
            while (UpdateOnscreenKeyboard() != 1 && UpdateOnscreenKeyboard() != 2) await Delay(0);
            EnableAllControlActions(1);
            return GetOnscreenKeyboardResult();
        }

        private UIMenuDynamicListItem AddDynamicFloatList(UIMenu menu, FloatFieldInfo fieldInfo)
        {
            if (!currentPreset.Fields.ContainsKey(fieldInfo.Name))
                return null;

            float value = currentPreset.Fields[fieldInfo.Name];
            var newitem = new UIMenuDynamicListItem(fieldInfo.Name, fieldInfo.Description, value.ToString("F3"), (sender, direction) =>
            {
                if (direction == UIMenuDynamicListItem.ChangeDirection.Left)
                {
                    var newvalue = value - editingFactor;
                    if (newvalue < fieldInfo.Min)
                        CitizenFX.Core.UI.Screen.ShowNotification($"Min value allowed for ~b~{fieldInfo.Name}~w~ is {fieldInfo.Min}");
                    else
                    {
                        value = newvalue;
                        currentPreset.Fields[fieldInfo.Name] = newvalue;
                    }
                }
                else
                {
                    var newvalue = value + editingFactor;
                    if (newvalue > fieldInfo.Max)
                        CitizenFX.Core.UI.Screen.ShowNotification($"Max value allowed for ~b~{fieldInfo.Name}~w~ is {fieldInfo.Max}");
                    else
                    {
                        value = newvalue;
                        currentPreset.Fields[fieldInfo.Name] = newvalue;
                    }
                }
                return value.ToString("F3");
            });

            menu.AddItem(newitem);

            EditorMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == newitem)
                {
                    EditorMenu.Visible = false;

                    string text = await GetOnScreenValue(value.ToString());
                    float newvalue = value;

                    if (float.TryParse(text, out newvalue))
                    {
                        if(newvalue >= fieldInfo.Min && newvalue <= fieldInfo.Max)
                            currentPreset.Fields[fieldInfo.Name] = newvalue;
                        else
                            CitizenFX.Core.UI.Screen.ShowNotification($"Value out of allowed limits for ~b~{fieldInfo.Name}~w~, Min:{fieldInfo.Min}, Max:{fieldInfo.Max}");
                    }else
                        CitizenFX.Core.UI.Screen.ShowNotification($"Invalid value for ~b~{fieldInfo.Name}~w~");

                    InitialiseMenu(); //Should just update the current item instead
                    EditorMenu.Visible = true;
                }
            };

            return newitem;
        }

        private UIMenuDynamicListItem AddDynamicIntList(UIMenu menu, IntFieldInfo fieldInfo)
        {
            if (!currentPreset.Fields.ContainsKey(fieldInfo.Name))
                return null;

            int value = currentPreset.Fields[fieldInfo.Name]; //TODO: Get value from current preset
            var newitem = new UIMenuDynamicListItem(fieldInfo.Name, fieldInfo.Description, value.ToString(), (sender, direction) =>
            {
                if (direction == UIMenuDynamicListItem.ChangeDirection.Left)
                {
                    var newvalue = value - 1;
                    if (newvalue < fieldInfo.Min)
                        CitizenFX.Core.UI.Screen.ShowNotification($"Min value allowed for ~b~{fieldInfo.Name}~w~ is {fieldInfo.Min}");
                    else
                    {
                        value = newvalue;
                        currentPreset.Fields[fieldInfo.Name] = newvalue;
                    }
                }
                else
                {
                    var newvalue = value + 1;
                    if (newvalue > fieldInfo.Max)
                        CitizenFX.Core.UI.Screen.ShowNotification($"Max value allowed for ~b~{fieldInfo.Name}~w~ is {fieldInfo.Max}");
                    else
                    {
                        value = newvalue;
                        currentPreset.Fields[fieldInfo.Name] = newvalue;
                    }
                }
                return value.ToString();
            });

            menu.AddItem(newitem);

            EditorMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == newitem)
                {
                    EditorMenu.Visible = false;

                    string text = await GetOnScreenValue(value.ToString());
                    int newvalue = value;

                    if (int.TryParse(text, out newvalue))
                    {
                        if (newvalue >= fieldInfo.Min && newvalue <= fieldInfo.Max)
                            currentPreset.Fields[fieldInfo.Name] = newvalue;
                        else
                            CitizenFX.Core.UI.Screen.ShowNotification($"Value out of allowed limits for ~b~{fieldInfo.Name}~w~, Min:{fieldInfo.Min}, Max:{fieldInfo.Max}");
                    }
                    else
                        CitizenFX.Core.UI.Screen.ShowNotification($"Invalid value for ~b~{fieldInfo.Name}~w~");

                    InitialiseMenu(); //Should just update the current item instead
                    EditorMenu.Visible = true;
                }
            };

            return newitem;
        }
        
        private UIMenuItem AddMenuReset(UIMenu menu)
        {
            var newitem = new UIMenuItem("Reset", "Restores the default values");
            menu.AddItem(newitem);

            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    currentPreset.Reset();
                    RefreshVehicleUsingPreset(currentVehicle, currentPreset);
                    RemoveDecorators(currentVehicle);

                    InitialiseMenu();
                    EditorMenu.Visible = true;
                }
            };
            return newitem;
        }

        private UIMenu AddPresetsSubMenu(UIMenu menu)
        {
            var newitem = _menuPool.AddSubMenu(menu, "Saved Presets", "The handling presets saved by you.");
            newitem.MouseEdgeEnabled = false;
            newitem.ControlDisablingEnabled = false;
            newitem.MouseControlsEnabled = false;
            newitem.AddInstructionalButton(new InstructionalButton(Control.PhoneExtraOption, "Save"));
            newitem.AddInstructionalButton(new InstructionalButton(Control.PhoneOption, "Delete"));

            KvpList kvpList = new KvpList();
            foreach(var key in kvpList)
            {
                string value = GetResourceKvpString(key);
                newitem.AddItem(new UIMenuItem(key.Remove(0, kvpPrefix.Length)));  
            }
            return newitem;
        }

        private UIMenu AddServerPresetsSubMenu(UIMenu menu)
        {
            var newitem = _menuPool.AddSubMenu(menu, "Server Presets", "The handling presets loaded from the server.");
            newitem.MouseEdgeEnabled = false;
            newitem.ControlDisablingEnabled = false;
            newitem.MouseControlsEnabled = false;

            foreach (var preset in serverPresets)
            {
                newitem.AddItem(new UIMenuItem(preset.Key));
            }
            return newitem;
        }

        private void InitialiseMenu()
        {
            _menuPool = new MenuPool();
            EditorMenu = new UIMenu("Handling Editor", "Beta", new PointF(screenPosX * Screen.Width, screenPosY * Screen.Height));

            foreach (var item in handlingInfo.FieldsInfo.Where(a => a.Value.Editable == true))
            {
                Type fieldType = item.Value.Type;

                if(fieldType == typeof(float))
                    AddDynamicFloatList(EditorMenu, (FloatFieldInfo)item.Value);
                else if(fieldType == typeof(int))
                    AddDynamicIntList(EditorMenu, (IntFieldInfo)item.Value);
                /*else if (fieldType == typeof(VectorFieldInfo))
                    AddDynamicVectorList(EditorMenu, (VectorFieldInfo)item.Value);*/
            }

            AddMenuReset(EditorMenu);
            presetsMenu = AddPresetsSubMenu(EditorMenu);
            serverPresetsMenu = AddServerPresetsSubMenu(EditorMenu);

            presetsMenu.OnItemSelect += (sender, item, index) =>
            {
                if(sender == presetsMenu)
                {
                    string key = $"{kvpPrefix}{item.Text}";
                    string value = GetResourceKvpString(key);
                    if (value != null)
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(value);
                        var handling = doc["HandlingData"]["Item"];
                        GetPresetFromXml(handling, currentPreset);

                        CitizenFX.Core.UI.Screen.ShowNotification($"Personal preset ~b~{item.Text}~w~ applied");
                        InitialiseMenu();
                        presetsMenu.Visible = true;
                    }
                    else
                        CitizenFX.Core.UI.Screen.ShowNotification($"~r~ERROR~w~: Personal preset ~b~{item.Text}~w~ corrupted");
                }
            };

            serverPresetsMenu.OnItemSelect += (sender, item, index) =>
            {
                if(sender == serverPresetsMenu)
                {
                    string key = item.Text;
                    if (serverPresets.ContainsKey(key))
                    {
                        foreach (var field in serverPresets[key].Fields.Keys)
                        {
                            if (currentPreset.Fields.ContainsKey(field))
                            {
                                currentPreset.Fields[field] = serverPresets[key].Fields[field];
                            }
                            else Debug.Write($"Missing {field} field in currentPreset");
                        }
                        CitizenFX.Core.UI.Screen.ShowNotification($"Server preset ~b~{key}~w~ applied");
                        InitialiseMenu();
                        serverPresetsMenu.Visible = true;
                    }
                    else
                        CitizenFX.Core.UI.Screen.ShowNotification($"~r~ERROR~w~: Server preset ~b~{key}~w~ corrupted");
                }
            };



            EditorMenu.MouseEdgeEnabled = false;
            EditorMenu.ControlDisablingEnabled = false;
            EditorMenu.MouseControlsEnabled = false;
            _menuPool.ResetCursorOnOpen = true;
            _menuPool.Add(EditorMenu);
            _menuPool.RefreshIndex();
        }

        public HandlingEditor()
        {
            Debug.WriteLine("HANDLING EDITOR: Script by Neos7");
            handlingInfo = new HandlingInfo();
            ReadFieldInfo();
            LoadConfig();
            RegisterDecorators();
            serverPresets = new Dictionary<string, HandlingPreset>();
            ReadServerPresets();

            currentTime = GetGameTimer();
            lastTime = GetGameTimer();
            currentPreset = new HandlingPreset();
            currentVehicle = -1;
            vehicles = Enumerable.Empty<int>();

            InitialiseMenu();

            RegisterCommand("handling_distance", new Action<int, dynamic>((source, args) =>
            {
                bool result = float.TryParse(args[0], out float value);
                if (result)
                {
                    maxSyncDistance = value;
                    Debug.WriteLine("HANDLING EDITOR: Received new maxSyncDistance value {0}", value);
                }
                else Debug.WriteLine("HANDLING EDITOR: Can't parse {0}", value);

            }), false);

            RegisterCommand("handling_debug", new Action<int, dynamic>((source, args) =>
            {
                bool result = bool.TryParse(args[0], out bool value);
                if (result)
                {
                    debug = value;
                    Debug.WriteLine("HANDLING EDITOR: Received new debug value {0}", value);
                }
                else Debug.WriteLine("HANDLING EDITOR: Can't parse {0}", value);

            }), false);

            RegisterCommand("handling_decorators", new Action<int, dynamic>((source, args) =>
            {
                PrintDecorators(currentVehicle);
            }), false);

            RegisterCommand("handling_list", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators(vehicles);
            }), false);


            RegisterCommand("handling_preset", new Action<int, dynamic>((source, args) =>
            {
                if (currentPreset != null)
                    Debug.Write(currentPreset.ToString());
                else Debug.WriteLine("Current preset doesn't exist");
            }), false);

            Tick += OnTick;
            Tick += ScriptTask;
        }

        private async Task OnTick()
        {
            _menuPool.ProcessMenus();

            if (currentVehicle != -1)
            {
                if (IsControlJustPressed(1, toggleMenu)/* || IsDisabledControlJustPressed(1, toggleMenu)*/) // TOGGLE MENU VISIBLE
                    EditorMenu.Visible = !EditorMenu.Visible;

                if (presetsMenu.Visible)
                {
                    if (IsControlJustPressed(1, 179))
                    {
                        string name = await GetOnScreenValue("");
                        if (!string.IsNullOrEmpty(name))
                        {
                            SavePreset(name, currentPreset);
                            InitialiseMenu();
                            presetsMenu.Visible = true;
                        }
                        else
                            CitizenFX.Core.UI.Screen.ShowNotification("Invalid string.");
                    }
                    else if (IsControlJustPressed(1, 178))
                    {
                        if(presetsMenu.MenuItems.Count > 0)
                        {
                            string key = $"{kvpPrefix}{presetsMenu.MenuItems[presetsMenu.CurrentSelection].Text}";
                            if (GetResourceKvpString(key) != null)
                            {
                                DeleteResourceKvp(key);
                                InitialiseMenu();
                                presetsMenu.Visible = true;
                            }
                        }
                        else
                            CitizenFX.Core.UI.Screen.ShowNotification("Nothing to delete.");
                    }
                }
                
            }
            else
            {
                if(_menuPool.IsAnyMenuOpen())
                    _menuPool.CloseAllMenus();
            }


            await Task.FromResult(0);
        }

        private async Task ScriptTask()
        {
            currentTime = (GetGameTimer() - lastTime);

            playerPed = PlayerPedId();
            //CURRENT VEHICLE/PRESET HANDLER
            if (IsPedInAnyVehicle(playerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(playerPed, false);

                if (IsThisModelACar((uint)GetEntityModel(vehicle)) && GetPedInVehicleSeat(vehicle, -1) == playerPed && !IsEntityDead(vehicle))
                {
                    // Update current vehicle and get its preset
                    if (vehicle != currentVehicle)
                    {
                        currentVehicle = vehicle;
                        currentPreset = CreateHandlingPreset(currentVehicle);                
                        InitialiseMenu();
                    }
                }
                else
                {
                    // If current vehicle isn't a car or player isn't driving current vehicle or vehicle is dead
                    currentVehicle = -1;
                    currentPreset = null;
                }
            }
            else
            {
                // If player isn't in any vehicle
                currentVehicle = -1;
                currentPreset = null;
            }

            // Check if decorators needs to be updated
            if (currentTime > timer)
            {
                // Current vehicle could be updated each tick to show the edited fields live
                // Check if current vehicle needs to be refreshed
                if (currentVehicle != -1 && currentPreset != null)
                {
                    if (currentPreset.IsEdited)
                        RefreshVehicleUsingPreset(currentVehicle, currentPreset);
                }

                if (currentVehicle != -1 && currentPreset != null)
                    UpdateVehicleDecorators(currentVehicle, currentPreset);

                vehicles = new VehicleList();

                // Refreshes the iterated vehicles
                RefreshVehicles(vehicles.Except(new List<int> { currentVehicle }));

                lastTime = GetGameTimer();
            }
            await Delay(0);
        }

        /// <summary>
        /// Refreshes the handling for the <paramref name="vehicle"/> using the <paramref name="preset"/>.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="preset"></param>
        private async void RefreshVehicleUsingPreset(int vehicle, HandlingPreset preset)
        {
            if (DoesEntityExist(vehicle))
            {
                foreach (var item in preset.Fields)
                {
                    string fieldName = item.Key;
                    dynamic fieldValue = item.Value;
                    Type fieldType = handlingInfo.FieldsInfo[fieldName].Type;
                    string className = handlingInfo.FieldsInfo[fieldName].ClassName;

                    if (fieldType == typeof(float))
                    {
                        var value = GetVehicleHandlingFloat(vehicle, className, fieldName);
                        if (value != fieldValue)
                            SetVehicleHandlingFloat(vehicle, className, fieldName, fieldValue);

                        if (debug)
                            Debug.WriteLine($"{fieldName} updated from {fieldValue} to {value}");
                    }
                    
                    else if (fieldType == typeof(int))
                    {
                        var value = GetVehicleHandlingInt(vehicle, className, fieldName);
                        if (value != fieldValue)
                            SetVehicleHandlingInt(vehicle, className, fieldName, fieldValue);

                        if (debug)
                            Debug.WriteLine($"{fieldName} updated from {fieldValue} to {value}");
                    }
                    
                    else if (fieldType == typeof(Vector3))
                    {
                        var value = GetVehicleHandlingVector(vehicle, className, fieldName);
                        if (value != fieldValue)
                            SetVehicleHandlingVector(vehicle, className, fieldName, fieldValue);

                        if (debug)
                            Debug.WriteLine($"{fieldName} updated from {fieldValue} to {value}");
                    }
                }
            }
            await Delay(0);
        }

        /// <summary>
        /// Refreshes the handling for the vehicles in <paramref name="vehiclesList"/> if they are close enough.
        /// </summary>
        /// <param name="vehiclesList"></param>
        private async void RefreshVehicles(IEnumerable<int> vehiclesList)
        {
            Vector3 currentCoords = GetEntityCoords(playerPed, true);

            foreach (int entity in vehiclesList)
            {
                if (DoesEntityExist(entity))
                {
                    Vector3 coords = GetEntityCoords(entity, true);

                    if (Vector3.Distance(currentCoords, coords) <= maxSyncDistance)
                        RefreshVehicleUsingDecorators(entity);
                }
            }
            await Delay(0);
        }

        /// <summary>
        /// Refreshes the handling for the <paramref name="vehicle"/> using the decorators attached to it.
        /// </summary>
        /// <param name="vehicle"></param>
        private async void RefreshVehicleUsingDecorators(int vehicle)
        {
            foreach (var item in handlingInfo.FieldsInfo.Where(a => a.Value.Editable))
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;
                string className = item.Value.ClassName;

                if (fieldType == typeof(float))
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        var decorValue = DecorGetFloat(vehicle, fieldName);
                        var value = GetVehicleHandlingFloat(vehicle, className, fieldName);
                        if (value != decorValue)
                            SetVehicleHandlingFloat(vehicle, className, fieldName, decorValue);

                        if (debug)
                            Debug.WriteLine($"{fieldName} updated from {value} to {decorValue} for vehicle {vehicle}");
                    }
                }
                else if (fieldType == typeof(int))
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        var decorValue = DecorGetInt(vehicle, fieldName);
                        var value = GetVehicleHandlingInt(vehicle, className, fieldName);
                        if (value != decorValue)
                            SetVehicleHandlingInt(vehicle, className, fieldName, decorValue);

                        if (debug)
                            Debug.WriteLine($"{fieldName} updated from {value} to {decorValue} for vehicle {vehicle}");
                    }
                }
                else if (fieldType == typeof(Vector3))
                {
                    string decorX = $"{fieldName}_x";
                    string decorY = $"{fieldName}_y";
                    string decorZ = $"{fieldName}_z";

                    Vector3 value = GetVehicleHandlingVector(vehicle, className, fieldName);
                    Vector3 decorValue = new Vector3(value.X, value.Y, value.Z);

                    if (DecorExistOn(vehicle, decorX))
                        decorValue.X = DecorGetFloat(vehicle, decorX);

                    if (DecorExistOn(vehicle, decorY))
                        decorValue.Y = DecorGetFloat(vehicle, decorY);

                    if (DecorExistOn(vehicle, decorZ))
                        decorValue.Z = DecorGetFloat(vehicle, decorZ);

                    if(!value.Equals(decorValue))
                        SetVehicleHandlingVector(vehicle, className, fieldName, decorValue);

                    if (debug)
                        Debug.WriteLine($"{fieldName} updated from {value} to {decorValue} for vehicle {vehicle}");
                }
            }
            await Delay(0);
        }

        /// <summary>
        /// Returns true if the <paramref name="vehicle"/> has any handling decorator attached to it.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        private bool HasDecorators(int vehicle)
        {
            foreach (var item in handlingInfo.FieldsInfo)
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;

                if (fieldType == typeof(Vector3))
                {
                    if (DecorExistOn(vehicle, $"{fieldName}_x") || DecorExistOn(vehicle, $"{fieldName}_y") || DecorExistOn(vehicle, $"{fieldName}_z"))
                        return true;
                }
                else if (DecorExistOn(vehicle, fieldName))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Registers the decorators for this script
        /// </summary>
        private async void RegisterDecorators()
        {
            foreach (var item in handlingInfo.FieldsInfo)
            {
                string fieldName = item.Key;
                Type type = item.Value.Type;

                if (type == typeof(float))
                {
                    DecorRegister(fieldName, 1);
                    DecorRegister($"{fieldName}_def", 1);
                }
                else if (type == typeof(int))
                {
                    DecorRegister(fieldName, 3);
                    DecorRegister($"{fieldName}_def", 3);
                }
                else if (type == typeof(Vector3))
                {
                    string decorX = $"{fieldName}_x";
                    string decorY = $"{fieldName}_y";
                    string decorZ = $"{fieldName}_z";

                    DecorRegister(decorX, 1);
                    DecorRegister(decorY, 1);
                    DecorRegister(decorZ, 1);

                    DecorRegister($"{decorX}_def", 1);
                    DecorRegister($"{decorY}_def", 1);
                    DecorRegister($"{decorZ}_def", 1);
                }
            }
            await Delay(0);
        }

        /// <summary>
        /// Remove the handling decorators attached to the <paramref name="vehicle"/>.
        /// </summary>
        /// <param name="vehicle"></param>
        private async void RemoveDecorators(int vehicle)
        {
            foreach (var item in handlingInfo.FieldsInfo)
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;

                if (fieldType == typeof(int) || fieldType == typeof(float))
                {
                    string defDecorName = $"{fieldName}_def";

                    if (DecorExistOn(vehicle, fieldName))
                        DecorRemove(vehicle, fieldName);
                    if (DecorExistOn(vehicle, defDecorName))
                        DecorRemove(vehicle, defDecorName);
                }
                else if (fieldType == typeof(Vector3))
                {
                    string decorX = $"{fieldName}_x";
                    string decorY = $"{fieldName}_y";
                    string decorZ = $"{fieldName}_z";

                    DecorRemove(vehicle, decorX);
                    DecorRemove(vehicle, decorY);
                    DecorRemove(vehicle, decorZ);

                    DecorRemove(vehicle, $"{decorX}_def");
                    DecorRemove(vehicle, $"{decorY}_def");
                    DecorRemove(vehicle, $"{decorZ}_def");
                }
            }
            await Delay(0);
        }

        private async void UpdateVehicleDecorators(int vehicle, HandlingPreset preset)
        {
            foreach (var item in preset.Fields)
            {
                string fieldName = item.Key;
                Type fieldType = handlingInfo.FieldsInfo[fieldName].Type;
                dynamic fieldValue = item.Value;

                string defDecorName = $"{fieldName}_def";
                dynamic defaultValue = preset.DefaultFields[fieldName];

                if (fieldType == typeof(float))
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        float value = DecorGetFloat(vehicle, fieldName);
                        if (value != fieldValue)
                            DecorSetFloat(vehicle, fieldName, fieldValue);
                    }
                    else
                    {
                        if (defaultValue != fieldValue)
                            DecorSetFloat(vehicle, fieldName, fieldValue);
                    }

                    if (DecorExistOn(vehicle, defDecorName))
                    {
                        float value = DecorGetFloat(vehicle, defDecorName);
                        if (value != defaultValue)
                            DecorSetFloat(vehicle, defDecorName, defaultValue);
                    }
                    else
                    {
                        if (defaultValue != fieldValue)
                            DecorSetFloat(vehicle, defDecorName, defaultValue);
                    }
                }/*
                else if(fieldType == typeof(int))
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        int value = DecorGetInt(vehicle, fieldName);
                        if (value != fieldValue)
                            DecorSetInt(vehicle, fieldName, fieldValue);
                    }
                    else
                    {
                        if (defaultValue != fieldValue)
                            DecorSetInt(vehicle, fieldName, fieldValue);
                    }

                    if (DecorExistOn(vehicle, defDecorName))
                    {
                        int value = DecorGetInt(vehicle, defDecorName);
                        if (value != defaultValue)
                            DecorSetInt(vehicle, defDecorName, defaultValue);
                    }
                    else
                    {
                        if (defaultValue != fieldValue)
                            DecorSetInt(vehicle, defDecorName, defaultValue);
                    }
                }*/
                else if(fieldType == typeof(Vector3))
                {
                    fieldValue = (Vector3)fieldValue;
                    defaultValue = (Vector3)defaultValue;

                    string decorX = $"{fieldName}_x";
                    if (DecorExistOn(vehicle, decorX))
                    {
                        float value = DecorGetFloat(vehicle, decorX);
                        if (value != fieldValue.X)
                            DecorSetFloat(vehicle, decorX, fieldValue.X);
                    }
                    else
                    {
                        if (defaultValue.X != fieldValue.X)
                            DecorSetFloat(vehicle, decorX, fieldValue.X);
                    }

                    string defDecorNameX = $"{decorX}_def";
                    if (DecorExistOn(vehicle, defDecorNameX))
                    {
                        float value = DecorGetFloat(vehicle, defDecorNameX);
                        if (value != defaultValue.X)
                            DecorSetFloat(vehicle, defDecorNameX, defaultValue.X);
                    }
                    else
                    {
                        if (defaultValue.X != fieldValue.X)
                            DecorSetFloat(vehicle, defDecorNameX, defaultValue.X);
                    }

                    string decorY = $"{fieldName}_y";
                    if (DecorExistOn(vehicle, decorY))
                    {
                        float value = DecorGetFloat(vehicle, decorY);
                        if (value != fieldValue.Y)
                            DecorSetFloat(vehicle, decorY, fieldValue.Y);
                    }
                    else
                    {
                        if (defaultValue.Y != fieldValue.Y)
                            DecorSetFloat(vehicle, decorY, fieldValue.Y);
                    }

                    string defDecorNameY = $"{decorY}_def";
                    if (DecorExistOn(vehicle, defDecorNameY))
                    {
                        float value = DecorGetFloat(vehicle, defDecorNameY);
                        if (value != defaultValue.Y)
                            DecorSetFloat(vehicle, defDecorNameY, defaultValue.Y);
                    }
                    else
                    {
                        if (defaultValue.Y != fieldValue.Y)
                            DecorSetFloat(vehicle, defDecorNameY, defaultValue.Y);
                    }
                    string decorZ = $"{fieldName}_z";
                    if (DecorExistOn(vehicle, decorZ))
                    {
                        float value = DecorGetFloat(vehicle, decorZ);
                        if (value != fieldValue.Z)
                            DecorSetFloat(vehicle, decorZ, fieldValue.Z);
                    }
                    else
                    {
                        if (defaultValue.Z != fieldValue.Z)
                            DecorSetFloat(vehicle, decorZ, fieldValue.Z);
                    }

                    string defDecorNameZ = $"{decorZ}_def";
                    if (DecorExistOn(vehicle, defDecorNameZ))
                    {
                        float value = DecorGetFloat(vehicle, defDecorNameZ);
                        if (value != defaultValue.Z)
                            DecorSetFloat(vehicle, defDecorNameZ, defaultValue.Z);
                    }
                    else
                    {
                        if (defaultValue.Z != fieldValue.Z)
                            DecorSetFloat(vehicle, defDecorNameZ, defaultValue.Z);
                    }
                }
            }
            await Delay(0);
        }

        private HandlingPreset CreateHandlingPreset(int vehicle)
        {
            Dictionary<string, dynamic> defaultFields = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> fields = new Dictionary<string, dynamic>();
            
            foreach(var item in handlingInfo.FieldsInfo)
            {
                /*
                if ()//vehicle hasn't such handling field
                    continue;*/
                string fieldName = item.Key;
                string className = item.Value.ClassName;
                Type fieldType = item.Value.Type;
                string defDecorName = $"{fieldName}_def";

                if (fieldType == typeof(float))
                {
                    if (DecorExistOn(vehicle, defDecorName))
                        defaultFields[fieldName] = DecorGetFloat(vehicle, defDecorName);
                    else defaultFields[fieldName] = GetVehicleHandlingFloat(vehicle, className, fieldName);

                    if (DecorExistOn(vehicle, fieldName))
                        fields[fieldName] = DecorGetFloat(vehicle, fieldName);
                    else fields[fieldName] = defaultFields[fieldName];
                }/*
                else if (fieldType == typeof(int))
                {
                    if (DecorExistOn(vehicle, defDecorName))
                        defaultFields[fieldName] = DecorGetInt(vehicle, defDecorName);
                    else defaultFields[fieldName] = GetVehicleHandlingInt(vehicle, className, fieldName);

                    if (DecorExistOn(vehicle, fieldName))
                        fields[fieldName] = DecorGetInt(vehicle, fieldName);
                    else fields[fieldName] = defaultFields[fieldName];
                }*/
                else if (fieldType == typeof(Vector3))
                {
                    Vector3 vec = GetVehicleHandlingVector(vehicle, className, fieldName);

                    string decorX = $"{fieldName}_x";
                    string decorY = $"{fieldName}_y";
                    string decorZ = $"{fieldName}_z";

                    string defDecorNameX = $"{decorX}_def";
                    string defDecorNameY = $"{decorY}_def";
                    string defDecorNameZ = $"{decorZ}_def";

                    if (DecorExistOn(vehicle, defDecorNameX))
                        vec.X = DecorGetFloat(vehicle, defDecorNameX);
                    if ( DecorExistOn(vehicle, defDecorNameY))
                        vec.Y = DecorGetFloat(vehicle, defDecorNameY);
                    if (DecorExistOn(vehicle, defDecorNameZ))
                        vec.Z = DecorGetFloat(vehicle, defDecorNameZ);

                    defaultFields[fieldName] = vec;

                    if (DecorExistOn(vehicle, decorX))
                        vec.X = DecorGetFloat(vehicle, decorX);
                    if (DecorExistOn(vehicle, decorY))
                        vec.Y = DecorGetFloat(vehicle, decorY);
                    if (DecorExistOn(vehicle, decorZ))
                        vec.Z = DecorGetFloat(vehicle, decorZ);

                    fields[fieldName] = vec;
                }
            }

            HandlingPreset preset = new HandlingPreset(defaultFields, fields);

            return preset;
        }

        private async void PrintDecorators(int vehicle)
        {
            if (DoesEntityExist(vehicle))
            {
                int netID = NetworkGetNetworkIdFromEntity(vehicle);
                StringBuilder s = new StringBuilder();
                s.AppendLine($"HANDLING EDITOR: Vehicle:{vehicle} netID:{netID}");
                s.AppendLine("DECORATORS:");

                foreach (var item in handlingInfo.FieldsInfo)
                {
                    string fieldName = item.Key;
                    Type fieldType = item.Value.Type;
                    string defDecorName = $"{fieldName}_def";

                    dynamic value = 0, defaultValue = 0;

                    if (fieldType == typeof(float))
                    {
                        if (DecorExistOn(vehicle, item.Key))
                        {
                            value = DecorGetFloat(vehicle, fieldName);
                            defaultValue = DecorGetFloat(vehicle, defDecorName);
                            s.AppendLine($"{fieldName}: {value}({defaultValue})");
                        }
                    }
                    else if (fieldType == typeof(int))
                    {
                        if (DecorExistOn(vehicle, item.Key))
                        {
                            value = DecorGetInt(vehicle, fieldName);
                            defaultValue = DecorGetInt(vehicle, defDecorName);
                            s.AppendLine($"{fieldName}: {value}({defaultValue})");
                        }
                    }
                    else if (fieldType == typeof(Vector3))
                    {
                        string decorX = $"{fieldName}_x";
                        if (DecorExistOn(vehicle, decorX))
                        {
                            string defDecorNameX = $"{decorX}_def";
                            var x = DecorGetFloat(vehicle, decorX);
                            var defX = DecorGetFloat(vehicle, defDecorNameX);
                            s.AppendLine($"{decorX}: {x}({defX})");
                        }

                        string decorY = $"{fieldName}_y";
                        if (DecorExistOn(vehicle, decorY))
                        {
                            string defDecorNameY = $"{decorY}_def";
                            var y = DecorGetFloat(vehicle, decorY);
                            var defY = DecorGetFloat(vehicle, defDecorNameY);
                            s.AppendLine($"{decorY}: {y}({defY})");
                        }

                        string decorZ = $"{fieldName}_z";
                        if (DecorExistOn(vehicle, decorZ))
                        {
                            string defDecorNameZ = $"{decorZ}_def";
                            var z = DecorGetFloat(vehicle, decorZ);
                            var defZ = DecorGetFloat(vehicle, defDecorNameZ);
                            s.AppendLine($"{decorZ}: {z}({defZ})");
                        }
                        
                    }
                }
                Debug.Write(s.ToString());
            }
            else Debug.WriteLine("HANDLING_EDITOR: Current vehicle doesn't exist");

            await Delay(0);
        }

        private async void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
        {
            IEnumerable<int> entities = vehiclesList.Where(entity => HasDecorators(entity));

            Debug.WriteLine($"HANDLING EDITOR: Vehicles with decorators: {entities.Count()}");

            StringBuilder s = new StringBuilder();
            foreach (var vehicle in entities)
            {
                int netID = NetworkGetNetworkIdFromEntity(vehicle);      
                s.AppendLine($"Vehicle:{vehicle} netID:{netID}");
            }
            Debug.WriteLine(s.ToString());

            await Delay(0);
        }

        private string GetXmlFromPreset(string name, HandlingPreset preset)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement handlingData = doc.CreateElement("HandlingData");
            XmlElement handlingItem = doc.CreateElement("Item");
            handlingItem.SetAttribute("type", "CHandlingData");
            handlingItem.SetAttribute("presetName", name);

            foreach (var item in preset.Fields)
            {
                string fieldName = item.Key;
                dynamic fieldValue = item.Value;
                XmlElement field = doc.CreateElement(fieldName);

                Type fieldType = handlingInfo.FieldsInfo[fieldName].Type;
                if(fieldType == typeof(float))
                {
                    field.SetAttribute("value", ((float)(fieldValue)).ToString());
                }
                else if (fieldType == typeof(int))
                {
                    field.SetAttribute("value", ((int)(fieldValue)).ToString());
                }
                else if (fieldType == typeof(Vector3))
                {
                    field.SetAttribute("x", ((Vector3)(fieldValue)).X.ToString());
                    field.SetAttribute("y", ((Vector3)(fieldValue)).Y.ToString());
                    field.SetAttribute("z", ((Vector3)(fieldValue)).Z.ToString());
                }
                else if (fieldType == typeof(string))
                {
                    field.InnerText = fieldValue;
                }
                else { }
                handlingItem.AppendChild(field);
            }

            handlingData.AppendChild(handlingItem);
            doc.AppendChild(handlingData);

            return doc.OuterXml;
        }

        private async void SavePreset(string name, HandlingPreset preset)
        {
            string kvpName = $"{kvpPrefix}{name}";
            if(GetResourceKvpString(kvpName) != null)
                CitizenFX.Core.UI.Screen.ShowNotification($"The name {name} is already used for another preset.");
            else
            {
                string xml = GetXmlFromPreset(name, preset);;
                SetResourceKvp(kvpName, xml);
                await Delay(0);
            }
        }
        
        private void GetPresetFromXml(XmlNode node, HandlingPreset preset)
        {
            foreach (XmlNode item in node.ChildNodes)
            {
                if (item.NodeType != XmlNodeType.Element)
                    continue;

                string fieldName = item.Name;
                Type fieldType = FieldInfo.GetFieldType(fieldName);

                XmlElement elem = (XmlElement)item;

                if (fieldType == typeof(float))
                {
                    preset.Fields[fieldName] = float.Parse(elem.GetAttribute("value"));
                }/*
                else if (fieldType == typeof(int))
                {
                    preset.Fields[fieldName] = int.Parse(elem.GetAttribute("value"));
                }*/
                else if (fieldType == typeof(Vector3))
                {
                    float x = float.Parse(elem.GetAttribute("x"));
                    float y = float.Parse(elem.GetAttribute("y"));
                    float z = float.Parse(elem.GetAttribute("z"));
                    preset.Fields[fieldName] = new Vector3(x, y, z);
                }/*
                else if (fieldType == typeof(string))
                {
                    preset.Fields[fieldName] = elem.InnerText;
                }*/
            }
        }

        private void ReadFieldInfo()
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile("handling_editor", "HandlingInfo.xml");
                handlingInfo.ParseXML(strings);
                var editableFields = handlingInfo.FieldsInfo.Where(a => a.Value.Editable);
                Debug.WriteLine($"Loaded HandlingInfo.xml, found {handlingInfo.FieldsInfo.Count} fields info, {editableFields.Count()} editable.");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                Debug.WriteLine("HANDLING_EDITOR: Error loading HandlingInfo.xml");
            }
        }

        private void ReadServerPresets()
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile("handling_editor", "HandlingPresets.xml");
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(strings);

                foreach (XmlElement node in doc["CHandlingDataMgr"]["HandlingData"].ChildNodes)
                {
                    if (node.NodeType != XmlNodeType.Element)
                        continue;
                    
                    if (node.HasAttribute("presetName"))
                    {
                        string name = node.GetAttribute("presetName");
                        HandlingPreset preset = new HandlingPreset();

                        GetPresetFromXml(node, preset);
                        serverPresets[name] = preset;
                    }
                }
                Debug.WriteLine($"Loaded HandlingPresets.xml, found {serverPresets.Count} server presets.");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                Debug.WriteLine("HANDLING_EDITOR: Error loading HandlingPresets.xml");
            }
        }

        protected void LoadConfig()
        {
            string strings = null;
            Config config = new Config();
            try
            {
                strings = LoadResourceFile("handling_editor", "config.ini");
                config.ParseConfigFile(strings);
                Debug.WriteLine("HANDLING_EDITOR: Loaded settings from config.ini");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                Debug.WriteLine("HANDLING_EDITOR: Impossible to load config.ini");
            }
            finally
            {
                toggleMenu = config.toggleMenu;
                editingFactor = config.editingFactor;
                maxSyncDistance = config.maxSyncDistance;
                timer = config.timer;
                debug = config.debug;
                screenPosX = config.screenPosX;
                screenPosY = config.screenPosY;
            }
        }
 
    }
   
    public class KvpList : IEnumerable<string>
    {
        public string prefix = HandlingEditor.kvpPrefix;

        public IEnumerator<string> GetEnumerator()
        {
            int handle = StartFindKvp(prefix);

            if (handle != -1)
            {
                string kvp;
                do
                {
                    kvp = FindKvp(handle);

                    if (kvp != null)
                        yield return kvp;
                }
                while (kvp != null);
                EndFindKvp(handle);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class VehicleList : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            int entity = -1;
            int handle = FindFirstVehicle(ref entity);

            if (handle != -1)
            {
                do
                {
                    yield return entity;
                }
                while (FindNextVehicle(handle, ref entity));

                EndFindVehicle(handle);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
