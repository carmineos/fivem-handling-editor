using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using NativeUI;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System.Collections;
using HandlingEditor;
using static NativeUI.UIMenuDynamicListItem;
using System.Text;
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
                if (direction == ChangeDirection.Left)
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
                if (direction == ChangeDirection.Left)
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

            presetsMenu.OnItemSelect += (sender, item, index) =>
            {
                string key = $"{kvpPrefix}{item.Text}";
                string value = GetResourceKvpString(key);
                if (value != null)
                {
                    currentPreset = GetPresetFromXml(value, currentPreset);
                    CitizenFX.Core.UI.Screen.ShowNotification("Preset applied");
                    InitialiseMenu();
                    presetsMenu.Visible = true;
                }
                else
                    CitizenFX.Core.UI.Screen.ShowNotification("~r~ERROR~w~: Preset corrupted");
            };

            EditorMenu.AddItem(new UIMenuItem("Server Presets", "The handling presets loaded from the server."));

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

            RegisterCommand("handling_info", new Action<int, dynamic>((source, args) =>
            {
                PrintDecoratorsInfo(currentVehicle);
            }), false);

            RegisterCommand("handling_print", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators(vehicles);
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
                        SetVehicleHandlingFloat(vehicle, className, fieldName, fieldValue);
                    
                    else if (fieldType == typeof(int))
                        SetVehicleHandlingInt(vehicle, className, fieldName, fieldValue);
                    
                    else if (fieldType == typeof(Vector3))
                        SetVehicleHandlingVector(vehicle, className, fieldName, fieldValue);
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
                        var value = DecorGetFloat(vehicle, fieldName);
                        SetVehicleHandlingFloat(vehicle, className, fieldName, value);
                    }
                }
                else if (fieldType == typeof(int))
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        var value = DecorGetInt(vehicle, fieldName);
                        SetVehicleHandlingInt(vehicle, className, fieldName, value);
                    }
                }
                else if (fieldType == typeof(Vector3))
                {
                    string decorX = $"{fieldName}_x";
                    string decorY = $"{fieldName}_y";
                    string decorZ = $"{fieldName}_z";

                    Vector3 vector = GetVehicleHandlingVector(vehicle, className, fieldName);

                    if (DecorExistOn(vehicle, decorX))
                        vector.X = DecorGetFloat(vehicle, decorX);

                    if (DecorExistOn(vehicle, decorY))
                        vector.Y = DecorGetFloat(vehicle, decorY);

                    if (DecorExistOn(vehicle, decorZ))
                        vector.Z = DecorGetFloat(vehicle, decorZ);

                    SetVehicleHandlingVector(vehicle, className, fieldName, vector);
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
                }
                else if(fieldType == typeof(int))
                {

                }
                else if(fieldType == typeof(Vector3))
                {

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
                    string decorX = $"{defDecorName}_x";
                    string decorY = $"{defDecorName}_y";
                    string decorZ = $"{defDecorName}_z";

                    if (DecorExistOn(vehicle, decorX) && DecorExistOn(vehicle, decorY) && DecorExistOn(vehicle, decorZ))
                    {
                        var x = DecorGetFloat(vehicle, decorX);
                        var y = DecorGetFloat(vehicle, decorY);
                        var z = DecorGetFloat(vehicle, decorZ);
                        defaultFields[fieldName] = new Vector3(x, y, z);
                    }
                    else defaultFields[fieldName] = GetVehicleHandlingVector(vehicle, className, fieldName);
                }
            }

            HandlingPreset preset = new HandlingPreset(defaultFields, fields);

            return preset;
        }

        private async void PrintDecoratorsInfo(int vehicle)
        {
            if (DoesEntityExist(vehicle))
            {
                int netID = NetworkGetNetworkIdFromEntity(vehicle);
                StringBuilder s = new StringBuilder();
                s.Append($"HANDLING EDITOR: Vehicle:{vehicle} netID:{netID} ");

                foreach (var item in handlingInfo.FieldsInfo)
                {
                    if (DecorExistOn(vehicle, item.Key))
                    {
                        string fieldName = item.Key;
                        Type fieldType = item.Value.Type;
                        string defDecorName = $"{fieldName}_def";

                        dynamic value = 0, defaultValue = 0;

                        if (fieldType == typeof(float))
                        {
                            value = DecorGetFloat(vehicle, fieldName);
                            defaultValue = DecorGetFloat(vehicle, defDecorName);
                        }
                        if (fieldType == typeof(int))
                        {
                            value = DecorGetInt(vehicle, fieldName);
                            defaultValue = DecorGetInt(vehicle, defDecorName);
                        }
                        s.Append($"{fieldName}:{value}({defaultValue}) ");
                    }
                        
                }
                Debug.WriteLine(s.ToString());
            }
            else Debug.WriteLine("HANDLING_EDITOR: Current vehicle doesn't exist");

            await Delay(0);
        }

        private async void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
        {
            IEnumerable<int> entities = vehiclesList.Where(entity => HasDecorators(entity));

            Debug.WriteLine($"HANDLING EDITOR: Vehicles with decorators: {entities.Count()}");

            foreach (var item in entities)
                PrintDecoratorsInfo(item);

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
        
        private HandlingPreset GetPresetFromXml(string xml, HandlingPreset preset)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            var handling = doc["HandlingData"]["Item"];
            string name = handling.GetAttribute("presetName");

            foreach (XmlNode item in handling.ChildNodes)
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
                }
                else if (fieldType == typeof(Vector3))
                {
                    float x = float.Parse(elem.GetAttribute("x"));
                    float y = float.Parse(elem.GetAttribute("y"));
                    float z = float.Parse(elem.GetAttribute("z"));
                    preset.Fields[fieldName] = new Vector3(x, y, z);
                }
                else if (fieldType == typeof(string))
                {
                    preset.Fields[fieldName] = elem.InnerText;
                }
                else { }*/
            }
            return preset;
        }

        private void ReadFieldInfo()
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile("handling_editor", "HandlingInfo.xml");
                //handlingInfo.ParseXMLLinq(strings);           
                handlingInfo.ParseXML(strings);
                Debug.WriteLine($"Loaded HandlingInfo.xml, found {handlingInfo.FieldsInfo.Count} fields info");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                Debug.WriteLine("HANDLING_EDITOR: Error loading HandlingInfo.xml");
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

    public class Config
    {
        public float editingFactor { get; set; }
        public float maxSyncDistance { get; set; }
        public int toggleMenu { get; set; }
        public long timer { get; set; }
        public bool debug { get; set; }
        public float screenPosX { get; set; }
        public float screenPosY { get; set; }

        public Config()
        {
            editingFactor = 0.01f;
            maxSyncDistance = 150.0f;
            toggleMenu = 167;
            timer = 1000;
            debug = false;
            screenPosX = 1.0f;
            screenPosY = 0.0f;
        }

        public void ParseConfigFile(string content)
        {
            Dictionary<string, string> Entries = new Dictionary<string, string>();

            if (content?.Any() ?? false)
            {
                var splitted = content
                 .Split('\n')
                 .Where((line) => !line.Trim().StartsWith("#"))
                 .Select((line) => line.Trim().Split('='))
                 .Where((line) => line.Length == 2);

                foreach (var tuple in splitted)
                    Entries.Add(tuple[0], tuple[1]);
            }

            if (Entries.ContainsKey("editingFactor"))
                editingFactor = float.Parse(Entries["editingFactor"]);

            if (Entries.ContainsKey("maxSyncDistance"))
                maxSyncDistance = float.Parse(Entries["maxSyncDistance"]);

            if (Entries.ContainsKey("toggleMenu"))
                toggleMenu = int.Parse(Entries["toggleMenu"]);

            if (Entries.ContainsKey("timer"))
                timer = long.Parse(Entries["timer"]);

            if (Entries.ContainsKey("debug"))
                debug = bool.Parse(Entries["debug"]);

            if (Entries.ContainsKey("screenPosX"))
                screenPosX = float.Parse(Entries["screenPosX"]);

            if (Entries.ContainsKey("screenPosY"))
                screenPosY = float.Parse(Entries["screenPosY"]);
        }
    }
}
