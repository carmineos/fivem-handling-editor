using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System.Xml;

namespace HandlingEditor.Client
{
    public class HandlingEditor : BaseScript
    {
        #region Events

        public static event EventHandler PresetChanged;
        public static event EventHandler PersonalPresetsListChanged;
        public static event EventHandler ServerPresetsListChanged;

        #endregion

        #region Config Fields
        public static float FloatPrecision { get; private set; } = 0.001f;
        public static float FloatStep { get; private set; } = 0.01f;
        public static float ScriptRange { get; private set; } = 150.0f;
        public static long Timer { get; private set; } = 1000;
        public static bool Debug { get; private set; } = false;
        public static int ToggleMenu { get; private set; } = 168;
        
        /// <summary>
        /// Wheter <see cref="CurrentVehicle"/> and <see cref="CurrentPreset"/> are valid
        /// </summary>
        public static bool CurrentPresetIsValid => CurrentVehicle != -1 && CurrentPreset != null;

        #endregion

        #region Fields
        public const string ScriptName = "Handling Editor";
        public const string kvpPrefix = "handling_";
        public static string ResourceName { get; private set; }
        public static Dictionary<string,HandlingPreset> ServerPresets { get; private set; }
        public static long CurrentTime { get; private set; }
        public static long LastTime { get; private set; }
        public static int PlayerPed { get; private set; }
        public static int CurrentVehicle { get; private set; }
        public static HandlingPreset CurrentPreset { get; private set; }
        public static IEnumerable<int> Vehicles { get; private set; }
        #endregion

        #region Constructor

        public HandlingEditor()
        {
            ResourceName = GetCurrentResourceName();
            CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Script by Neos7");
            LoadConfig();
            
            ReadFieldInfo();
            ServerPresets = new Dictionary<string, HandlingPreset>();
            ReadServerPresets();

            RegisterDecorators();

            ReadVehiclePermissions();

            CurrentTime = GetGameTimer();
            LastTime = CurrentTime;
            CurrentPreset = null;
            CurrentVehicle = -1;
            Vehicles = Enumerable.Empty<int>();

            RegisterCommand("handling_range", new Action<int, dynamic>((source, args) =>
            {
                if(args.Count < 1)
                {
                    CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Missing float argument");
                    return;
                }

                if (float.TryParse(args[0], out float value))
                {
                    ScriptRange = value;
                    CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Received new {nameof(ScriptRange)} value {value}");
                }
                else CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error parsing {args[0]} as float");

            }), false);

            RegisterCommand("handling_debug", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Missing bool argument");
                    return;
                }

                if (bool.TryParse(args[0], out bool value))
                {
                    Debug = value;
                    CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Received new {nameof(Debug)} value {value}");
                }
                else CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error parsing {args[0]} as bool");

            }), false);

            RegisterCommand("handling_decorators", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                    PrintDecorators(CurrentVehicle);
                else
                {
                    if (int.TryParse(args[0], out int value))
                        PrintDecorators(value);
                    else CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error parsing {args[0]} as int");
                }

            }), false);

            RegisterCommand("handling_print", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators(Vehicles);
            }), false);

            RegisterCommand("handling_preset", new Action<int, dynamic>((source, args) =>
            {
                if (CurrentPreset != null)
                    CitizenFX.Core.Debug.WriteLine(CurrentPreset.ToString());
                else
                    CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Current preset doesn't exist");
            }), false);

            RegisterCommand("handling_xml", new Action<int, dynamic>((source, args) =>
            {
                if (CurrentPreset != null)
                    CitizenFX.Core.Debug.WriteLine(GetXmlFromPreset(CurrentPreset).OuterXml);
                else
                    CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Current preset doesn't exist");
            }), false);

            HandlingMenu.ApplyPersonalPresetButtonPressed += async (sender, name) =>
            {
                string key = $"{kvpPrefix}{name}";
                string value = GetResourceKvpString(key);
                if (value != null)
                {
                    value = Helpers.RemoveByteOrderMarks(value);
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(value);
                    var handling = doc["Item"];
                    GetPresetFromXml(handling, CurrentPreset);

                    Screen.ShowNotification($"{ScriptName}: Personal preset ~b~{name}~w~ applied");
                }
                else
                    Screen.ShowNotification($"{ScriptName}: ~r~ERROR~w~ Personal preset ~b~{name}~w~ corrupted");

                await Delay(200);
            };

            HandlingMenu.ApplyServerPresetButtonPressed += async (sender, name) =>
            {
                string key = name;
                if (ServerPresets.TryGetValue(key, out HandlingPreset preset))
                {
                    var presetFields = preset.Fields; 
                    foreach (var field in presetFields.Keys)
                    {
                        // TODO: Add a flag to decide if a field should be added to the preset anyway
                        if (CurrentPreset.Fields.ContainsKey(field))
                        {
                            CurrentPreset.Fields[field] = presetFields[field];
                        }
                        else CitizenFX.Core.Debug.Write($"Missing {field} field in currentPreset");
                    }

                    Screen.ShowNotification($"{ScriptName}: Server preset ~b~{key}~w~ applied");
                }
                else
                    Screen.ShowNotification($"{ScriptName}: ~r~ERROR~w~ Server preset ~b~{key}~w~ corrupted");

                await Delay(200);
            };

            HandlingMenu.SavePersonalPresetButtonPressed += async (sender, name) =>
            {
                if (SavePresetAsKVP(name, CurrentPreset))
                {
                    await Delay(200);
                    PersonalPresetsListChanged(this, EventArgs.Empty);
                    Screen.ShowNotification($"{ScriptName}: Personal preset ~g~{name}~w~ saved");
                }
                else
                    Screen.ShowNotification($"{ScriptName}: The name {name} is invalid or already used.");
            };

            HandlingMenu.DeletePersonalPresetButtonPressed += async (sender, name) =>
            {
                if (DeletePresetKVP(name))
                {
                    await Delay(200);
                    PersonalPresetsListChanged(this, EventArgs.Empty);
                    Screen.ShowNotification($"{ScriptName}: Personal preset ~r~{name}~w~ deleted");
                }
            };

            HandlingMenu.ResetPresetButtonPressed += async (sender, args) =>
            {
                CurrentPreset.Reset();
                RemoveDecorators(CurrentVehicle);
                RefreshVehicleUsingPreset(CurrentVehicle, CurrentPreset);

                await Delay(200);
                PresetChanged(this, EventArgs.Empty);
            };

            // When the UI changed a preset field
            HandlingMenu.OnEditorMenuPresetValueChanged += (fieldName, value, text) =>
            {
                if (!HandlingInfo.FieldsInfo.TryGetValue(fieldName, out BaseFieldInfo fieldInfo))
                    return;

                var fieldType = fieldInfo.Type;

                if (fieldType == FieldType.FloatType)
                    CurrentPreset.Fields[fieldName] = float.Parse(value);
                else if (fieldType == FieldType.IntType)
                    CurrentPreset.Fields[fieldName] = int.Parse(value);
                else if (fieldType == FieldType.Vector3Type)
                {
                    if (text.EndsWith("_x"))
                        CurrentPreset.Fields[fieldName].X = float.Parse(value);
                    else if (text.EndsWith("_y"))
                        CurrentPreset.Fields[fieldName].Y = float.Parse(value);
                    else if (text.EndsWith("_z"))
                        CurrentPreset.Fields[fieldName].Z = float.Parse(value);
                }
            };

            Tick += GetCurrentVehicle;
            Tick += ScriptTask;
        }

        #endregion

        #region Tasks

        /// <summary>
        /// Updates the <see cref="CurrentVehicle"/> and the <see cref="CurrentPreset"/>
        /// </summary>
        /// <returns></returns>
        private async Task GetCurrentVehicle()
        {
            PlayerPed = PlayerPedId();

            if (IsPedInAnyVehicle(PlayerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(PlayerPed, false);

                if (VehiclesPermissions.IsVehicleAllowed(vehicle) && GetPedInVehicleSeat(vehicle, -1) == PlayerPed && !IsEntityDead(vehicle))
                {
                    // Update current vehicle and get its preset
                    if (vehicle != CurrentVehicle)
                    {
                        CurrentVehicle = vehicle;
                        CurrentPreset = CreateHandlingPreset(CurrentVehicle);
                        PresetChanged.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    // If current vehicle isn't a car or player isn't driving current vehicle or vehicle is dead
                    CurrentVehicle = -1;
                    CurrentPreset = null;
                }
            }
            else
            {
                // If player isn't in any vehicle
                CurrentVehicle = -1;
                CurrentPreset = null;
            }
        }      

        /// <summary>
        /// The main task of the script
        /// </summary>
        /// <returns></returns>
        private async Task ScriptTask()
        {
            CurrentTime = (GetGameTimer() - LastTime);

            // Check if decorators needs to be updated
            if (CurrentTime > Timer)
            {
                // Current vehicle could be updated each tick to show the edited fields live
                // Check if current vehicle needs to be refreshed
                if (CurrentPresetIsValid)
                {
                    if (CurrentPreset.IsEdited)
                        RefreshVehicleUsingPreset(CurrentVehicle, CurrentPreset);

                    UpdateVehicleDecorators(CurrentVehicle, CurrentPreset);
                }
                    

                Vehicles = new VehicleEnumerable();

                // Refreshes the iterated vehicles
                RefreshVehicles(Vehicles.Except(new List<int> { CurrentVehicle }));

                LastTime = GetGameTimer();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Disable controls for controller to use the script with the controller
        /// </summary>
        public static void DisableControls()
        {
            DisableControlAction(1, 85, true); // INPUT_VEH_RADIO_WHEEL = DPAD - LEFT
            DisableControlAction(1, 74, true); // INPUT_VEH_HEADLIGHT = DPAD - RIGHT
            DisableControlAction(1, 48, true); // INPUT_HUD_SPECIAL = DPAD - DOWN
            DisableControlAction(1, 27, true); // INPUT_PHONE = DPAD - UP
            DisableControlAction(1, 80, true); // INPUT_VEH_CIN_CAM = B
            DisableControlAction(1, 73, true); // INPUT_VEH_DUCK = A
        }

        /// <summary>
        /// Disable controls for controller to use the script with the controller
        /// </summary>
        public static void DisableControls2()
        {
            DisableControlAction(1, 75, true); // INPUT_VEH_EXIT - Y
            DisableControlAction(1, 37, true); // INPUT_SELECT_WEAPON - X
        }

        /// <summary>
        /// Refreshes the handling for the <paramref name="vehicle"/> using the <paramref name="preset"/>.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="preset"></param>
        private void RefreshVehicleUsingPreset(int vehicle, HandlingPreset preset)
        {
            if (!DoesEntityExist(vehicle))
                return;

            foreach (var item in preset.Fields)
            {
                string fieldName = item.Key;
                dynamic fieldValue = item.Value;

                var fieldsInfo = HandlingInfo.FieldsInfo;
                if (!fieldsInfo.TryGetValue(fieldName, out BaseFieldInfo fieldInfo))
                {
                    if (Debug)
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: No fieldInfo definition found for {fieldName}");
                    continue;
                }

                Type fieldType = fieldInfo.Type;
                string className = fieldInfo.ClassName;

                if (fieldType == FieldType.FloatType)
                {
                    var value = GetVehicleHandlingFloat(vehicle, className, fieldName);
                    if (Math.Abs(value - fieldValue) > FloatPrecision)
                    {
                        SetVehicleHandlingFloat(vehicle, className, fieldName, fieldValue);

                        if (Debug)
                            CitizenFX.Core.Debug.WriteLine($"{ScriptName}: {fieldName} updated from {value} to {fieldValue}");
                    }
                }

                else if (fieldType == FieldType.IntType)
                {
                    var value = GetVehicleHandlingInt(vehicle, className, fieldName);
                    if (value != fieldValue)
                    {
                        SetVehicleHandlingInt(vehicle, className, fieldName, fieldValue);

                        if (Debug)
                            CitizenFX.Core.Debug.WriteLine($"{ScriptName}: {fieldName} updated from {value} to {fieldValue}");
                    }
                }

                else if (fieldType == FieldType.Vector3Type)
                {
                    var value = GetVehicleHandlingVector(vehicle, className, fieldName);
                    if (value != fieldValue) // TODO: Check why this is bugged
                    {
                        SetVehicleHandlingVector(vehicle, className, fieldName, fieldValue);

                        if (Debug)
                            CitizenFX.Core.Debug.WriteLine($"{ScriptName}: {fieldName} updated from {value} to {fieldValue}");
                    }
                }
            }
        }

        /// <summary>
        /// Refreshes the handling for the vehicles in <paramref name="vehiclesList"/> if they are close enough.
        /// </summary>
        /// <param name="vehiclesList"></param>
        private void RefreshVehicles(IEnumerable<int> vehiclesList)
        {
            Vector3 currentCoords = GetEntityCoords(PlayerPed, true);

            foreach (int entity in vehiclesList)
            {
                if (DoesEntityExist(entity))
                {
                    Vector3 coords = GetEntityCoords(entity, true);

                    if (Vector3.Distance(currentCoords, coords) <= ScriptRange)
                        RefreshVehicleUsingDecorators(entity);
                }
            }
        }

        /// <summary>
        /// Refreshes the handling for the <paramref name="vehicle"/> using the decorators attached to it.
        /// </summary>
        /// <param name="vehicle"></param>
        private void RefreshVehicleUsingDecorators(int vehicle)
        {
            foreach (var item in HandlingInfo.FieldsInfo.Where(a => a.Value.Editable))
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;
                string className = item.Value.ClassName;

                if (fieldType == FieldType.FloatType)
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        var decorValue = DecorGetFloat(vehicle, fieldName);
                        var value = GetVehicleHandlingFloat(vehicle, className, fieldName);
                        if (Math.Abs(value - decorValue) > FloatPrecision)
                        {
                            SetVehicleHandlingFloat(vehicle, className, fieldName, decorValue);

                            if (Debug)
                                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: {fieldName} updated from {value} to {decorValue} for vehicle {vehicle}");
                        }
                    }
                }
                else if (fieldType == FieldType.IntType)
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        var decorValue = DecorGetInt(vehicle, fieldName);
                        var value = GetVehicleHandlingInt(vehicle, className, fieldName);
                        if (value != decorValue)
                        {
                            SetVehicleHandlingInt(vehicle, className, fieldName, decorValue);

                            if (Debug)
                                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: {fieldName} updated from {value} to {decorValue} for vehicle {vehicle}");
                        }
                    }
                }
                else if (fieldType == FieldType.Vector3Type)
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
                    {
                        SetVehicleHandlingVector(vehicle, className, fieldName, decorValue);

                        if (Debug)
                            CitizenFX.Core.Debug.WriteLine($"{ScriptName}: {fieldName} updated from {value} to {decorValue} for vehicle {vehicle}");
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the <paramref name="vehicle"/> has any handling decorator attached to it.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        private bool HasDecorators(int vehicle)
        {
            foreach (var item in HandlingInfo.FieldsInfo)
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;

                if (fieldType == FieldType.Vector3Type)
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
        private void RegisterDecorators()
        {
            foreach (var item in HandlingInfo.FieldsInfo)
            {
                string fieldName = item.Key;
                Type type = item.Value.Type;

                if (type == FieldType.FloatType)
                {
                    DecorRegister(fieldName, 1);
                    DecorRegister($"{fieldName}_def", 1);
                }
                else if (type == FieldType.IntType)
                {
                    DecorRegister(fieldName, 3);
                    DecorRegister($"{fieldName}_def", 3);
                }
                else if (type == FieldType.Vector3Type)
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
        }

        /// <summary>
        /// Remove the handling decorators attached to the <paramref name="vehicle"/>.
        /// </summary>
        /// <param name="vehicle"></param>
        private void RemoveDecorators(int vehicle)
        {
            foreach (var item in HandlingInfo.FieldsInfo)
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;

                if (fieldType == FieldType.IntType || fieldType == FieldType.FloatType)
                {
                    string defDecorName = $"{fieldName}_def";

                    if (DecorExistOn(vehicle, fieldName))
                        DecorRemove(vehicle, fieldName);
                    if (DecorExistOn(vehicle, defDecorName))
                        DecorRemove(vehicle, defDecorName);
                }
                else if (fieldType == FieldType.Vector3Type)
                {
                    string decorX = $"{fieldName}_x";
                    string decorY = $"{fieldName}_y";
                    string decorZ = $"{fieldName}_z";
                    string defDecorX = $"{decorX}_def";
                    string defDecorY = $"{decorY}_def";
                    string defDecorZ = $"{decorZ}_def";

                    if (DecorExistOn(vehicle, decorX)) DecorRemove(vehicle, decorX);
                    if (DecorExistOn(vehicle, decorY)) DecorRemove(vehicle, decorY);
                    if (DecorExistOn(vehicle, decorZ)) DecorRemove(vehicle, decorZ);

                    if (DecorExistOn(vehicle, defDecorX)) DecorRemove(vehicle, defDecorX);
                    if (DecorExistOn(vehicle, defDecorY)) DecorRemove(vehicle, defDecorY);
                    if (DecorExistOn(vehicle, defDecorZ)) DecorRemove(vehicle, defDecorZ);
                }
            }

            if(Debug)
            {
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Removed all decorators on vehicle {vehicle}");
            }
        }

        /// <summary>
        /// It checks if the <paramref name="vehicle"/> has a decorator named <paramref name="name"/> and updates its value with <paramref name="currentValue"/>, otherwise if <paramref name="currentValue"/> isn't equal to <paramref name="defaultValue"/> it adds the decorator <paramref name="name"/>
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="name"></param>
        /// <param name="currentValue"></param>
        /// <param name="defaultValue"></param>
        private void UpdateFloatDecorator(int vehicle, string name, float currentValue, float defaultValue)
        {
            // Decorator exists but needs to be updated
            if (DecorExistOn(vehicle, name))
            {
                float decorValue = DecorGetFloat(vehicle, name);
                if (Math.Abs(currentValue - decorValue) > FloatPrecision)
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (Debug)
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Updated decorator {name} updated from {decorValue} to {currentValue} for vehicle {vehicle}");
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (Math.Abs(currentValue - defaultValue) > FloatPrecision)
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (Debug)
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Added decorator {name} with value {currentValue} to vehicle {vehicle}");
                }
            }
        }

        /// <summary>
        /// It checks if the <paramref name="vehicle"/> has a decorator named <paramref name="name"/> and updates its value with <paramref name="currentValue"/>, otherwise if <paramref name="currentValue"/> isn't equal to <paramref name="defaultValue"/> it adds the decorator <paramref name="name"/>
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="name"></param>
        /// <param name="currentValue"></param>
        /// <param name="defaultValue"></param>
        private void UpdateIntDecorator(int vehicle, string name, int currentValue, int defaultValue)
        {
            // Decorator exists but needs to be updated
            if (DecorExistOn(vehicle, name))
            {
                int decorValue = DecorGetInt(vehicle, name);
                if (currentValue != decorValue)
                {
                    DecorSetInt(vehicle, name, currentValue);
                    if (Debug)
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Updated decorator {name} updated from {decorValue} to {currentValue} for vehicle {vehicle}");
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (currentValue != defaultValue)
                {
                    DecorSetInt(vehicle, name, currentValue);
                    if (Debug)
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Added decorator {name} with value {currentValue} to vehicle {vehicle}");
                }
            }
        }

        /// <summary>
        /// Updates the decorators on the <paramref name="vehicle"/> with updated values from the <paramref name="preset"/>
        /// </summary>
        /// <param name="vehicle"></param>
        private void UpdateVehicleDecorators(int vehicle, HandlingPreset preset)
        {
            foreach (var item in preset.Fields)
            {
                string fieldName = item.Key;
                Type fieldType = HandlingInfo.FieldsInfo[fieldName].Type;
                dynamic fieldValue = item.Value;

                string defDecorName = $"{fieldName}_def";
                dynamic defaultValue = preset.DefaultFields[fieldName];

                if (fieldType == FieldType.FloatType)
                {
                    UpdateFloatDecorator(vehicle, fieldName, fieldValue, defaultValue);
                    UpdateFloatDecorator(vehicle, defDecorName, defaultValue, fieldValue);
                }
                else if(fieldType == FieldType.IntType)
                {
                    UpdateIntDecorator(vehicle, fieldName, fieldValue, defaultValue);
                    UpdateIntDecorator(vehicle, defDecorName, defaultValue, fieldValue);
                }
                else if (fieldType == FieldType.Vector3Type)
                {
                    fieldValue = (Vector3)fieldValue;
                    defaultValue = (Vector3)defaultValue;

                    string decorX = $"{fieldName}_x";
                    string defDecorNameX = $"{decorX}_def";
                    string decorY = $"{fieldName}_y";
                    string defDecorNameY = $"{decorY}_def";
                    string decorZ = $"{fieldName}_z";
                    string defDecorNameZ = $"{decorZ}_def";

                    UpdateFloatDecorator(vehicle, decorX, fieldValue.X, defaultValue.X);
                    UpdateFloatDecorator(vehicle, defDecorNameX, defaultValue.X, fieldValue.X);

                    UpdateFloatDecorator(vehicle, decorY, fieldValue.Y, defaultValue.Y);
                    UpdateFloatDecorator(vehicle, defDecorNameY, defaultValue.Y, fieldValue.Y);

                    UpdateFloatDecorator(vehicle, decorZ, fieldValue.Z, defaultValue.Z);
                    UpdateFloatDecorator(vehicle, defDecorNameZ, defaultValue.Z, fieldValue.Z);
                }
            }
        }

        /// <summary>
        /// Creates a preset for the <paramref name="vehicle"/> to edit it locally
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        private HandlingPreset CreateHandlingPreset(int vehicle)
        {
            Dictionary<string, dynamic> defaultFields = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> fields = new Dictionary<string, dynamic>();
            
            foreach(var item in HandlingInfo.FieldsInfo)
            {
                string fieldName = item.Key;
                string className = item.Value.ClassName;
                Type fieldType = item.Value.Type;
                string defDecorName = $"{fieldName}_def";

                if (fieldType == FieldType.FloatType)
                {
                    var defaultValue = DecorExistOn(vehicle, defDecorName) ? DecorGetFloat(vehicle, defDecorName) : GetVehicleHandlingFloat(vehicle, className, fieldName);
                    defaultFields[fieldName] = defaultValue;
                    fields[fieldName] = DecorExistOn(vehicle, fieldName) ? DecorGetFloat(vehicle, fieldName) : defaultValue;
                }/*
                else if (fieldType == FieldType.IntType)
                {
                    var defaultValue = DecorExistOn(vehicle, defDecorName) ? DecorGetInt(vehicle, defDecorName) : GetVehicleHandlingInt(vehicle, className, fieldName);
                    defaultFields[fieldName] = defaultValue;
                    fields[fieldName] = DecorExistOn(vehicle, fieldName) ? DecorGetInt(vehicle, fieldName) : defaultValue;
                }*/
                else if (fieldType == FieldType.Vector3Type)
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
                    if (DecorExistOn(vehicle, defDecorNameY))
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

        /// <summary>
        /// Prints the values of the decorators used on the <paramref name="vehicle"/>
        /// </summary>
        private void PrintDecorators(int vehicle)
        {
            if (DoesEntityExist(vehicle))
            {
                int netID = NetworkGetNetworkIdFromEntity(vehicle);
                StringBuilder s = new StringBuilder();
                s.AppendLine($"{ScriptName}: Vehicle:{vehicle} netID:{netID}");
                s.AppendLine("Decorators List:");

                foreach (var item in HandlingInfo.FieldsInfo)
                {
                    string fieldName = item.Key;
                    Type fieldType = item.Value.Type;
                    string defDecorName = $"{fieldName}_def";

                    dynamic value = 0, defaultValue = 0;

                    if (fieldType == FieldType.FloatType)
                    {
                        if (DecorExistOn(vehicle, item.Key))
                        {
                            value = DecorGetFloat(vehicle, fieldName);
                            defaultValue = DecorGetFloat(vehicle, defDecorName);
                            s.AppendLine($"{fieldName}: {value}({defaultValue})");
                        }
                    }
                    else if (fieldType == FieldType.IntType)
                    {
                        if (DecorExistOn(vehicle, item.Key))
                        {
                            value = DecorGetInt(vehicle, fieldName);
                            defaultValue = DecorGetInt(vehicle, defDecorName);
                            s.AppendLine($"{fieldName}: {value}({defaultValue})");
                        }
                    }
                    else if (fieldType == FieldType.Vector3Type)
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
                CitizenFX.Core.Debug.Write(s.ToString());
            }
            else CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Can't find vehicle with handle {vehicle}");
        }

        /// <summary>
        /// Prints the list of vehicles using any decorator for this script.
        /// </summary>
        private void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
        {
            IEnumerable<int> entities = vehiclesList.Where(entity => HasDecorators(entity));

            CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Vehicles with decorators: {entities.Count()}");

            StringBuilder s = new StringBuilder();
            foreach (var vehicle in entities)
            {
                int netID = NetworkGetNetworkIdFromEntity(vehicle);      
                s.AppendLine($"Vehicle:{vehicle} netID:{netID}");
            }
            CitizenFX.Core.Debug.WriteLine(s.ToString());
        }

        private static XmlDocument GetXmlFromPreset(HandlingPreset preset)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement handlingItem = doc.CreateElement("Item");
            handlingItem.SetAttribute("type", "CHandlingData");

            foreach (var item in preset.Fields)
            {
                string fieldName = item.Key;
                dynamic fieldValue = item.Value;
                XmlElement field = doc.CreateElement(fieldName);

                Type fieldType = HandlingInfo.FieldsInfo[fieldName].Type;
                if(fieldType == FieldType.FloatType)
                {
                    var value = (float)fieldValue;
                    field.SetAttribute("value", value.ToString());
                }
                else if (fieldType == FieldType.IntType)
                {
                    var value = (int)fieldValue;
                    field.SetAttribute("value", value.ToString());
                }
                else if (fieldType == FieldType.Vector3Type)
                {
                    var value = (Vector3)(fieldValue);
                    field.SetAttribute("x", value.X.ToString());
                    field.SetAttribute("y", value.Y.ToString());
                    field.SetAttribute("z", value.Z.ToString());
                }
                else if (fieldType == FieldType.StringType)
                {
                    field.InnerText = fieldValue;
                }
                else
                {

                }
                handlingItem.AppendChild(field);
            }
            doc.AppendChild(handlingItem);

            return doc;
        }

        private static bool SavePresetAsKVP(string name, HandlingPreset preset)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            string kvpName = $"{kvpPrefix}{name}";

            //Key already used
            if(GetResourceKvpString(kvpName) != null)
                return false;

            var xml = GetXmlFromPreset(preset);
            xml["Item"].SetAttribute("presetName", name);
            SetResourceKvp(kvpName, xml.OuterXml);
            return true;
        }

        private static bool DeletePresetKVP(string name)
        {
            string key = $"{kvpPrefix}{name}";

            //Nothing to delete
            if (GetResourceKvpString(key) == null)
                return false;

            DeleteResourceKvp(key);
            return true;
        }

        private void GetPresetFromXml(XmlNode node, HandlingPreset preset)
        {
            foreach (XmlNode item in node.ChildNodes)
            {
                if (item.NodeType != XmlNodeType.Element)
                    continue;

                string fieldName = item.Name;
                Type fieldType = FieldType.GetFieldType(fieldName);

                XmlElement elem = (XmlElement)item;

                if (fieldType == FieldType.FloatType)
                {
                    preset.Fields[fieldName] = float.Parse(elem.GetAttribute("value"));
                }/*
                else if (fieldType == FieldType.IntType)
                {
                    preset.Fields[fieldName] = int.Parse(elem.GetAttribute("value"));
                }*/
                else if (fieldType == FieldType.Vector3Type)
                {
                    float x = float.Parse(elem.GetAttribute("x"));
                    float y = float.Parse(elem.GetAttribute("y"));
                    float z = float.Parse(elem.GetAttribute("z"));
                    preset.Fields[fieldName] = new Vector3(x, y, z);
                }/*
                else if (fieldType == FieldType.StringType)
                {
                    preset.Fields[fieldName] = elem.InnerText;
                }*/
            }
        }

        private void ReadFieldInfo(string filename = "HandlingInfo.xml")
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile(ResourceName, filename);
                HandlingInfo.ParseXml(strings);
                var editableFields = HandlingInfo.FieldsInfo.Where(a => a.Value.Editable);
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Loaded {filename}, found {HandlingInfo.FieldsInfo.Count} fields info, {editableFields.Count()} editable.");
            }
            catch (Exception e)
            {
                CitizenFX.Core.Debug.WriteLine(e.Message);
                CitizenFX.Core.Debug.WriteLine(e.StackTrace);
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error loading {filename}");
            }
        }

        private void ReadVehiclePermissions(string filename = "VehiclesPermissions.xml")
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile(ResourceName, filename);
                VehiclesPermissions.ParseXml(strings);
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Loaded {filename}, found {VehiclesPermissions.Classes.Count} class rules and {VehiclesPermissions.Vehicles.Count} vehicle rules");
            }
            catch (Exception e)
            {
                CitizenFX.Core.Debug.WriteLine(e.Message);
                CitizenFX.Core.Debug.WriteLine(e.StackTrace);
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error loading {filename}");
            }
        }

        private void ReadServerPresets(string filename = "HandlingPresets.xml")
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile(ResourceName, filename);
                strings = Helpers.RemoveByteOrderMarks(strings);
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
                        ServerPresets[name] = preset;
                    }
                }
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Loaded {filename}, found {ServerPresets.Count} server presets.");
            }
            catch (Exception e)
            {
                CitizenFX.Core.Debug.WriteLine(e.Message);
                CitizenFX.Core.Debug.WriteLine(e.StackTrace);
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error loading {filename}");
            }
        }

        private void LoadConfig(string filename = "config.ini")
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile(ResourceName, filename);

                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Loaded settings from {filename}");
            }
            catch (Exception e)
            {
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Impossible to load {filename}");
                CitizenFX.Core.Debug.WriteLine(e.StackTrace);
            }
            finally
            {
                Config config = new Config(strings);

                ToggleMenu = config.GetIntValue("toggleMenu", ToggleMenu);
                FloatStep = config.GetFloatValue("FloatStep", FloatStep);
                ScriptRange = config.GetFloatValue("ScriptRange", ScriptRange);
                Timer = config.GetLongValue("timer", Timer);
                Debug = config.GetBoolValue("debug", Debug);

                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Settings {nameof(Timer)}={Timer} {nameof(Debug)}={Debug} {nameof(ScriptRange)}={ScriptRange}");
            }
        }

        #endregion
    }
}