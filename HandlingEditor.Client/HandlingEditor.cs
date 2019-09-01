using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Xml;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace HandlingEditor.Client
{
    public class HandlingEditor : BaseScript
    {
        #region Public Events

        /// <summary>
        /// An event triggered when <see cref="CurrentPreset"/> changes
        /// </summary>
        public event EventHandler PresetChanged;

        /// <summary>
        /// An event triggered when the list of the personal presets changes
        /// </summary>
        public event EventHandler PersonalPresetsListChanged;

        /// <summary>
        /// An event triggered when the list of the server presets changes
        /// </summary>
        public event EventHandler ServerPresetsListChanged;

        #endregion

        #region Public Fields

        #region Config

        /// <summary>
        /// The minimum difference to determine if two floats are equal
        /// </summary>
        public float Epsilon = 0.001f;

        /// <summary>
        /// The amount used to change a float when left/right arrows are pressed in the menu
        /// </summary>
        public float FloatStep = 0.01f;

        /// <summary>
        /// The max distance within which the script will refresh the vehicles
        /// </summary>
        public float ScriptRange = 150.0f;

        /// <summary>
        /// The timer used to determine when the script should do some tasks
        /// </summary>
        public long Timer = 1000;

        /// <summary>
        /// Wheter debug should be enabled
        /// </summary>
        public bool Debug = false;

        /// <summary>
        /// The <see cref="Control"/> used to open the menu
        /// </summary>
        public int ToggleMenu = 168;

        #endregion

        /// <summary>
        /// The name of the script
        /// </summary>
        public const string ScriptName = "Handling Editor";

        /// <summary>
        /// The prefix used for key-value pairs used to store personal presets
        /// </summary>
        public const string KvpPrefix = "handling_";

        /// <summary>
        /// The expected name of the resource
        /// </summary>
        public const string ResourceName = "handling-editor";

        /// <summary>
        /// The script which controls the menu
        /// </summary>
        private HandlingMenu _handlingMenu;

        /// <summary>
        /// The server presets
        /// </summary>
        public Dictionary<string, HandlingPreset> ServerPresets;

        /// <summary>
        /// The last game time the <see cref="ScriptTask"/> was executed
        /// </summary>
        private long LastTime;

        /// <summary>
        /// The ped of the player
        /// </summary>
        private int PlayerPed;

        /// <summary>
        /// The current vehicle the player is driving (-1 otherwise)
        /// </summary>
        private int CurrentVehicle;

        /// <summary>
        /// The handling preset for the <see cref="CurrentVehicle"/> 
        /// </summary>
        public HandlingPreset CurrentPreset;

        /// <summary>
        /// All the world vehicles
        /// </summary>
        private IEnumerable<int> Vehicles;

        #endregion

        #region Public Properties

        /// <summary>
        /// Wheter <see cref="CurrentVehicle"/> and <see cref="CurrentPreset"/> are valid
        /// </summary>
        public bool CurrentPresetIsValid => CurrentVehicle != -1 && CurrentPreset != null;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public HandlingEditor()
        {
            // If the resource name is not the expected one ...
            if (GetCurrentResourceName() != ResourceName)
            {
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Invalid resource name, be sure the resource name is {ResourceName}");
                return;
            }

            LastTime = GetGameTimer();
            CurrentPreset = null;
            CurrentVehicle = -1;
            Vehicles = Enumerable.Empty<int>();
            ServerPresets = new Dictionary<string, HandlingPreset>();

            LoadConfig();
            ReadFieldInfo();
            ReadServerPresets();
            RegisterDecorators();
            ReadVehiclePermissions();

            #region Register Commands

            RegisterCommand("handling_range", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
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

            #endregion

            // Create the script for the menu
            _handlingMenu = new HandlingMenu(this);

            if (_handlingMenu != null)
                RegisterScript(_handlingMenu);

            #region GUI Events Handling

            _handlingMenu.MenuApplyPersonalPresetButtonPressed += GUI_MenuApplyPersonalPresetButtonPressed;
            _handlingMenu.MenuApplyServerPresetButtonPressed += GUI_MenuApplyServerPresetButtonPressed;
            _handlingMenu.MenuSavePersonalPresetButtonPressed += GUI_MenuSavePersonalPresetButtonPressed;
            _handlingMenu.MenuDeletePersonalPresetButtonPressed += GUI_MenuDeletePersonalPresetButtonPressed;
            _handlingMenu.MenuResetPresetButtonPressed += GUI_MenuResetPresetButtonPressed;
            _handlingMenu.MenuPresetValueChanged += GUI_MenuPresetValueChanged;

            #endregion

            Tick += GetCurrentVehicle;
            Tick += ScriptTask;

        }

        #endregion

        #region GUI Event Handlers

        /// <summary>
        /// Updates a field of the current preset
        /// </summary>
        /// <param name="fieldName">The name of the field which needs to be updated</param>
        /// <param name="fieldValue">The new value of the field</param>
        /// <param name="fieldId">The ID of the field</param>
        private void GUI_MenuPresetValueChanged(string fieldName, string fieldValue, string fieldId)
        {
            // Be sure the field is supported

            if (!HandlingInfo.FieldsInfo.TryGetValue(fieldName, out BaseFieldInfo fieldInfo))
                return;

            // Get the field type
            var fieldType = fieldInfo.Type;

            // If it's a float field
            if (fieldType == FieldType.FloatType)
            {
                if (float.TryParse(fieldValue, out float result))
                    CurrentPreset.Fields[fieldName] = result;
            }

            // If it's a int field
            else if (fieldType == FieldType.IntType)
            {
                if (int.TryParse(fieldValue, out int result))
                    CurrentPreset.Fields[fieldName] = result;
            }

            // If it's a Vector3 field
            else if (fieldType == FieldType.Vector3Type)
            {
                // Update the correct Vector3 component
                if (fieldId.EndsWith("_x"))
                {
                    if (float.TryParse(fieldValue, out float result))
                        CurrentPreset.Fields[fieldName].X = result;
                }
                else if (fieldId.EndsWith("_y"))
                {
                    if (float.TryParse(fieldValue, out float result))
                        CurrentPreset.Fields[fieldName].Y = result;
                }
                else if (fieldId.EndsWith("_z"))
                {
                    if (float.TryParse(fieldValue, out float result))
                        CurrentPreset.Fields[fieldName].Z = result;
                }
            }
        }

        private async void GUI_MenuResetPresetButtonPressed(object sender, EventArgs e)
        {
            CurrentPreset.Reset();
            RemoveDecorators(CurrentVehicle);
            RefreshVehicleUsingPreset(CurrentVehicle, CurrentPreset);

            await Delay(200);
            PresetChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void GUI_MenuSavePersonalPresetButtonPressed(object sender, string presetName)
        {
            if (SavePresetAsKVP(presetName, CurrentPreset))
            {
                await Delay(200);
                PersonalPresetsListChanged?.Invoke(this, EventArgs.Empty);
                Screen.ShowNotification($"{ScriptName}: Personal preset ~g~{presetName}~w~ saved");
            }
            else
                Screen.ShowNotification($"{ScriptName}: The name {presetName} is invalid or already used.");
        }

        private async void GUI_MenuDeletePersonalPresetButtonPressed(object sender, string presetName)
        {
            if (DeletePresetKVP(presetName))
            {
                await Delay(200);
                PersonalPresetsListChanged?.Invoke(this, EventArgs.Empty);
                Screen.ShowNotification($"{ScriptName}: Personal preset ~r~{presetName}~w~ deleted");
            }
        }

        private async void GUI_MenuApplyServerPresetButtonPressed(object sender, string presetName)
        {
            if (ServerPresets.TryGetValue(presetName, out HandlingPreset preset))
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

                PresetChanged?.Invoke(this, EventArgs.Empty);
                Screen.ShowNotification($"{ScriptName}: Server preset ~b~{presetName}~w~ applied");
            }
            else
                Screen.ShowNotification($"{ScriptName}: ~r~ERROR~w~ Server preset ~b~{presetName}~w~ corrupted");

            await Delay(200);
        }

        private async void GUI_MenuApplyPersonalPresetButtonPressed(object sender, string presetName)
        {
            string key = $"{KvpPrefix}{presetName}";
            string value = GetResourceKvpString(key);
            if (value != null)
            {
                value = Helpers.RemoveByteOrderMarks(value);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(value);
                var handling = doc["Item"];
                GetPresetFromXml(handling, CurrentPreset);

                PresetChanged?.Invoke(this, EventArgs.Empty);
                Screen.ShowNotification($"{ScriptName}: Personal preset ~b~{presetName}~w~ applied");
            }
            else
                Screen.ShowNotification($"{ScriptName}: ~r~ERROR~w~ Personal preset ~b~{presetName}~w~ corrupted");

            await Delay(200);
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
                        PresetChanged?.Invoke(this, EventArgs.Empty);
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

            await Task.FromResult(0);
        }      

        /// <summary>
        /// The main task of the script
        /// </summary>
        /// <returns></returns>
        private async Task ScriptTask()
        {
            var CurrentTime = (GetGameTimer() - LastTime);

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

            await Task.FromResult(0);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Disable controls for controller to use the script with the controller
        /// </summary>
        private void DisableMainMenuControls()
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
        private void DisableAdditionalMainMenuControls()
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
                    if (!MathUtil.WithinEpsilon(value, fieldValue, Epsilon))
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
                        if (!MathUtil.WithinEpsilon(value, decorValue, Epsilon))
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
                if (!MathUtil.WithinEpsilon(currentValue, decorValue, Epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (Debug)
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Updated decorator {name} updated from {decorValue} to {currentValue} for vehicle {vehicle}");
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (!MathUtil.WithinEpsilon(currentValue, defaultValue, Epsilon))
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
            if (!DoesEntityExist(vehicle))
                return;

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
            HandlingPreset preset = new HandlingPreset();
            
            foreach (var item in HandlingInfo.FieldsInfo)
            {
                string fieldName = item.Key;
                string className = item.Value.ClassName;
                Type fieldType = item.Value.Type;
                string defDecorName = $"{fieldName}_def";

                if (fieldType == FieldType.FloatType)
                {
                    var defaultValue = DecorExistOn(vehicle, defDecorName) ? DecorGetFloat(vehicle, defDecorName) : GetVehicleHandlingFloat(vehicle, className, fieldName);
                    preset.DefaultFields[fieldName] = defaultValue;
                    preset.Fields[fieldName] = DecorExistOn(vehicle, fieldName) ? DecorGetFloat(vehicle, fieldName) : defaultValue;
                }/*
                else if (fieldType == FieldType.IntType)
                {
                    var defaultValue = DecorExistOn(vehicle, defDecorName) ? DecorGetInt(vehicle, defDecorName) : GetVehicleHandlingInt(vehicle, className, fieldName);
                    preset.DefaultFields[fieldName] = defaultValue;
                    preset.Fields[fieldName] = DecorExistOn(vehicle, fieldName) ? DecorGetInt(vehicle, fieldName) : defaultValue;
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

                    preset.DefaultFields[fieldName] = vec;

                    if (DecorExistOn(vehicle, decorX))
                        vec.X = DecorGetFloat(vehicle, decorX);
                    if (DecorExistOn(vehicle, decorY))
                        vec.Y = DecorGetFloat(vehicle, decorY);
                    if (DecorExistOn(vehicle, decorZ))
                        vec.Z = DecorGetFloat(vehicle, decorZ);

                    preset.Fields[fieldName] = vec;
                }
            }

            return preset;
        }

        /// <summary>
        /// Prints the values of the decorators used on the <paramref name="vehicle"/>
        /// </summary>
        private void PrintDecorators(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
            {
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Can't find vehicle with handle {vehicle}");
                return;
            }

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

        private XmlDocument GetXmlFromPreset(HandlingPreset preset)
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

        private bool SavePresetAsKVP(string name, HandlingPreset preset)
        {
            if (string.IsNullOrEmpty(name) || preset == null)
                return false;

            string kvpName = $"{KvpPrefix}{name}";

            //Key already used
            if(GetResourceKvpString(kvpName) != null)
                return false;

            var xml = GetXmlFromPreset(preset);
            xml["Item"].SetAttribute("presetName", name);
            SetResourceKvp(kvpName, xml.OuterXml);
            return true;
        }

        private bool DeletePresetKVP(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            string key = $"{KvpPrefix}{name}";

            //Nothing to delete
            if (GetResourceKvpString(key) == null)
                return false;

            DeleteResourceKvp(key);
            return true;
        }

        private void GetPresetFromXml(XmlNode node, HandlingPreset preset)
        {
            // Iterate Xml nodes
            foreach (XmlNode item in node.ChildNodes)
            {
                if (item.NodeType != XmlNodeType.Element)
                    continue;

                // Get the field name
                string fieldName = item.Name;

                // Get the field type
                Type fieldType = FieldType.GetFieldType(fieldName);

                // Get the item as element to access attributes
                XmlElement elem = (XmlElement)item;

                // If it's a float field
                if (fieldType == FieldType.FloatType)
                {
                    if (!float.TryParse(elem.GetAttribute("value"), out float result))
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error parsing attribute value in {fieldName} as float.");

                    preset.Fields[fieldName] = result;
                }
                // If it's a int field
                /*
                else if (fieldType == FieldType.IntType)
                {
                    if (!int.TryParse(elem.GetAttribute("value"), out int result))
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error parsing attribute value in {fieldName} from preset.");

                    preset.Fields[fieldName] = result;
                }*/
                // If it's a Vector3 field
                else if (fieldType == FieldType.Vector3Type)
                {
                    if (!float.TryParse(elem.GetAttribute("x"), out float x))
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error parsing attribute x in {fieldName} from preset.");
                    if (!float.TryParse(elem.GetAttribute("y"), out float y))
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error parsing attribute y in {fieldName} from preset."); 
                    if (!float.TryParse(elem.GetAttribute("z"), out float z))
                        CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Error parsing attribute z in {fieldName} from preset.");
                    preset.Fields[fieldName] = new Vector3(x, y, z);
                }/*
                else if (fieldType == FieldType.StringType)
                {
                    preset.Fields[fieldName] = elem.InnerText;
                }*/
                else
                {
                    // Unexpected
                }
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