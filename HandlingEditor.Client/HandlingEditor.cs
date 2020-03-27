﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CitizenFX.Core;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace HandlingEditor.Client
{
    public class HandlingEditor : BaseScript
    {
        private readonly ILogger logger;
        private readonly INotificationHandler notifier;

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

        #region Private Fields

        private readonly float m_epsilon = 0.001f;
        private HandlingMenu m_handlingMenu;
        private long m_lastTime;
        private int m_playerPed;
        private int m_currentVehicle;
        private IEnumerable<int> m_worldVehicles;

        #endregion

        #region Public Properties

        public bool CurrentPresetIsValid => m_currentVehicle != -1 && CurrentPreset != null;
        public HandlingPreset CurrentPreset { get; private set; }
        public HandlingInfo HandlingInfo { get; private set; }
        public HandlingConfig Config { get; private set; }

        public IPresetManager<string, HandlingPreset> LocalPresetsManager { get; private set; }
        public IPresetManager<string, HandlingPreset> ServerPresetsManager { get; private set; }
        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public HandlingEditor()
        {
            Config = LoadConfig();

            Framework.Build(Config);
            logger = Framework.Logger;
            notifier = Framework.Notifier;

            // If the resource name is not the expected one ...
            if (GetCurrentResourceName() != Globals.ResourceName)
            {
                logger.Log(LogLevel.Error, $"Invalid resource name, be sure the resource name is {Globals.ResourceName}");
                return;
            }

            HandlingInfo = Framework.HandlingInfo;
            LocalPresetsManager = new KvpPresetManager(Globals.KvpPrefix);

            m_lastTime = GetGameTimer();
            m_worldVehicles = Enumerable.Empty<int>();
            m_currentVehicle = -1;
            
            CurrentPreset = null;
            ServerPresetsManager = new MemoryPresetManager();

            ReadFieldInfo();
            ReadServerPresets();
            RegisterDecorators();
            ReadVehiclePermissions();

            #region Register Commands

            RegisterCommand("handling_range", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    logger.Log(LogLevel.Error, "Missing float argument");
                    return;
                }

                if (float.TryParse(args[0], out float value))
                {
                    Config.ScriptRange = value;
                    logger.Log(LogLevel.Information, $"Received new {nameof(Config.ScriptRange)} value {value}");
                }
                else logger.Log(LogLevel.Error, $"Can't parse {args[0]} as float");

            }), false);
            
            /*
            RegisterCommand("handling_loglevel", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    logger.Log(LogLevel.Error, "Missing bool argument");
                    return;
                }

                if (int.TryParse(args[0], out int value))
                {
                    Config.LogLevel = (LogLevel)value;
                    logger.Log(LogLevel.Information, $"Received new {nameof(Config.LogLevel)} value {value}");
                }
                else logger.Log(LogLevel.Error, $"Can't parse {args[0]} as bool");

            }), false);
            */

            RegisterCommand("handling_decorators", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                    PrintDecorators(m_currentVehicle);
                else
                {
                    if (int.TryParse(args[0], out int value))
                        PrintDecorators(value);
                    else logger.Log(LogLevel.Error, $"Can't parse {args[0]} as int");
                }

            }), false);

            RegisterCommand("handling_print", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators(m_worldVehicles);
            }), false);

            RegisterCommand("handling_preset", new Action<int, dynamic>((source, args) =>
            {
                if (CurrentPreset != null)
                    CitizenFX.Core.Debug.WriteLine(CurrentPreset.ToString());
                else
                    logger.Log(LogLevel.Error, "Current preset is not valid");
            }), false);

            RegisterCommand("handling_xml", new Action<int, dynamic>((source, args) =>
            {
                if (CurrentPreset != null)
                    CitizenFX.Core.Debug.WriteLine(CurrentPreset.ToXml());
                else
                    logger.Log(LogLevel.Error, "Current preset is not valid");
            }), false);

            #endregion

            // Create the menu
            m_handlingMenu = new HandlingMenu(this);

            #region GUI Events Handling

            m_handlingMenu.MenuApplyPersonalPresetButtonPressed += GUI_MenuApplyPersonalPresetButtonPressed;
            m_handlingMenu.MenuApplyServerPresetButtonPressed += GUI_MenuApplyServerPresetButtonPressed;
            m_handlingMenu.MenuSavePersonalPresetButtonPressed += GUI_MenuSavePersonalPresetButtonPressed;
            m_handlingMenu.MenuDeletePersonalPresetButtonPressed += GUI_MenuDeletePersonalPresetButtonPressed;
            m_handlingMenu.MenuResetPresetButtonPressed += GUI_MenuResetPresetButtonPressed;
            m_handlingMenu.MenuPresetValueChanged += GUI_MenuPresetValueChanged;

            #endregion

            Tick += GetCurrentVehicle;
            Tick += ScriptTask;
            Tick += HideUITask;

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

            if (!HandlingInfo.Fields.TryGetValue(fieldName, out HandlingFieldInfo fieldInfo))
                return;

            // Get the field type
            var fieldType = fieldInfo.Type;

            // If it's a float field
            if (fieldType == HandlingFieldTypes.FloatType)
            {
                if (float.TryParse(fieldValue, out float result))
                    CurrentPreset.Fields[fieldName] = result;
            }

            // If it's a int field
            else if (fieldType == HandlingFieldTypes.IntType)
            {
                if (int.TryParse(fieldValue, out int result))
                    CurrentPreset.Fields[fieldName] = result;
            }

            // If it's a Vector3 field
            else if (fieldType == HandlingFieldTypes.Vector3Type)
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
            RemoveDecorators(m_currentVehicle);
            RefreshVehicleUsingPreset(m_currentVehicle, CurrentPreset);

            await Delay(200);
            PresetChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void GUI_MenuSavePersonalPresetButtonPressed(object sender, string presetName)
        {
            if (LocalPresetsManager.Save(presetName, CurrentPreset))
            {
                await Delay(200);
                PersonalPresetsListChanged?.Invoke(this, EventArgs.Empty);
                notifier.Notify($"Personal preset ~g~{presetName}~w~ saved");
            }
            else
                notifier.Notify($"~r~ERROR~w~ The name {presetName} is invalid or already used.");
        }

        private async void GUI_MenuDeletePersonalPresetButtonPressed(object sender, string presetName)
        {
            if (LocalPresetsManager.Delete(presetName))
            {
                await Delay(200);
                PersonalPresetsListChanged?.Invoke(this, EventArgs.Empty);
                notifier.Notify($"Personal preset ~r~{presetName}~w~ deleted");
            }
        }

        private async void GUI_MenuApplyServerPresetButtonPressed(object sender, string presetName)
        {
            var loaded = ServerPresetsManager.Load(presetName);
            if (loaded != null)
            {
                CurrentPreset.FromPreset(loaded);

                PresetChanged?.Invoke(this, EventArgs.Empty);
                notifier.Notify($"Server preset ~b~{presetName}~w~ applied");
            }
            else
                notifier.Notify($"~r~ERROR~w~ Server preset ~b~{presetName}~w~ corrupted");

            await Delay(200);
        }

        private async void GUI_MenuApplyPersonalPresetButtonPressed(object sender, string presetName)
        {
            var loaded = LocalPresetsManager.Load(presetName);
            if(loaded != null)
            {
                // TODO:
                CurrentPreset.FromPreset(loaded);
                PresetChanged?.Invoke(this, EventArgs.Empty);
                notifier.Notify($"Personal preset ~b~{presetName}~w~ applied");
            }
            else
                notifier.Notify($"~r~ERROR~w~ Personal preset ~b~{presetName}~w~ corrupted");

            await Delay(200);
        }

        #endregion

        #region Tasks

        /// <summary>
        /// Updates the <see cref="m_currentVehicle"/> and the <see cref="CurrentPreset"/>
        /// </summary>
        /// <returns></returns>
        private async Task GetCurrentVehicle()
        {
            m_playerPed = PlayerPedId();

            if (IsPedInAnyVehicle(m_playerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(m_playerPed, false);

                if (VehiclesPermissions.IsVehicleAllowed(vehicle) && GetPedInVehicleSeat(vehicle, -1) == m_playerPed && !IsEntityDead(vehicle))
                {
                    // Update current vehicle and get its preset
                    if (vehicle != m_currentVehicle)
                    {
                        m_currentVehicle = vehicle;
                        logger.Log(LogLevel.Debug, $"New vehicle handle: {m_currentVehicle}");


                        CurrentPreset = new HandlingPreset();
                        CurrentPreset.FromHandle(m_currentVehicle);
                        PresetChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    // If current vehicle isn't a car or player isn't driving current vehicle or vehicle is dead
                    m_currentVehicle = -1;
                    CurrentPreset = null;
                }
            }
            else
            {
                // If player isn't in any vehicle
                m_currentVehicle = -1;
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
            var CurrentTime = (GetGameTimer() - m_lastTime);

            // Check if decorators needs to be updated
            if (CurrentTime > Config.Timer)
            {
                // Current vehicle could be updated each tick to show the edited fields live
                // Check if current vehicle needs to be refreshed
                if (CurrentPresetIsValid)
                {
                    if (CurrentPreset.IsEdited)
                        RefreshVehicleUsingPreset(m_currentVehicle, CurrentPreset);

                    UpdateVehicleDecorators(m_currentVehicle, CurrentPreset);
                }
                    

                m_worldVehicles = new VehicleEnumerable();

                // Refreshes the iterated vehicles
                RefreshVehicles(m_worldVehicles.Except(new List<int> { m_currentVehicle }));

                m_lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
        }

        private async Task HideUITask()
        {
            if (!CurrentPresetIsValid && m_handlingMenu != null)
                m_handlingMenu.HideUI();

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

                var fieldsInfo = HandlingInfo.Fields;
                if (!fieldsInfo.TryGetValue(fieldName, out HandlingFieldInfo fieldInfo))
                {
                    logger.Log(LogLevel.Debug, $"No fieldInfo definition found for {fieldName}");
                    continue;
                }

                Type fieldType = fieldInfo.Type;
                string className = fieldInfo.ClassName;

                if (fieldType == HandlingFieldTypes.FloatType)
                {
                    var value = GetVehicleHandlingFloat(vehicle, className, fieldName);
                    if (!MathUtil.WithinEpsilon(value, fieldValue, m_epsilon))
                    {
                        SetVehicleHandlingFloat(vehicle, className, fieldName, fieldValue);

                        logger.Log(LogLevel.Debug, $"{fieldName} updated from {value} to {fieldValue}");
                    }
                }

                else if (fieldType == HandlingFieldTypes.IntType)
                {
                    var value = GetVehicleHandlingInt(vehicle, className, fieldName);
                    if (value != fieldValue)
                    {
                        SetVehicleHandlingInt(vehicle, className, fieldName, fieldValue);

                        logger.Log(LogLevel.Debug, $"{fieldName} updated from {value} to {fieldValue}");
                    }
                }

                else if (fieldType == HandlingFieldTypes.Vector3Type)
                {
                    var value = GetVehicleHandlingVector(vehicle, className, fieldName);
                    if (value != fieldValue) // TODO: Check why this is bugged
                    {
                        SetVehicleHandlingVector(vehicle, className, fieldName, fieldValue);

                        logger.Log(LogLevel.Debug, $"{fieldName} updated from {value} to {fieldValue}");
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
            Vector3 currentCoords = GetEntityCoords(m_playerPed, true);

            foreach (int entity in vehiclesList)
            {
                if (DoesEntityExist(entity))
                {
                    Vector3 coords = GetEntityCoords(entity, true);

                    if (Vector3.Distance(currentCoords, coords) <= Config.ScriptRange)
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
            foreach (var item in HandlingInfo.Fields.Where(a => a.Value.Editable))
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;
                string className = item.Value.ClassName;

                if (fieldType == HandlingFieldTypes.FloatType)
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        var decorValue = DecorGetFloat(vehicle, fieldName);
                        var value = GetVehicleHandlingFloat(vehicle, className, fieldName);
                        if (!MathUtil.WithinEpsilon(value, decorValue, m_epsilon))
                        {
                            SetVehicleHandlingFloat(vehicle, className, fieldName, decorValue);

                            logger.Log(LogLevel.Debug, $"{fieldName} updated from {value} to {decorValue} for vehicle {vehicle}");
                        }
                    }
                }
                else if (fieldType == HandlingFieldTypes.IntType)
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        var decorValue = DecorGetInt(vehicle, fieldName);
                        var value = GetVehicleHandlingInt(vehicle, className, fieldName);
                        if (value != decorValue)
                        {
                            SetVehicleHandlingInt(vehicle, className, fieldName, decorValue);

                            logger.Log(LogLevel.Debug, $"{fieldName} updated from {value} to {decorValue} for vehicle {vehicle}");
                        }
                    }
                }
                else if (fieldType == HandlingFieldTypes.Vector3Type)
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

                        logger.Log(LogLevel.Debug, $"{fieldName} updated from {value} to {decorValue} for vehicle {vehicle}");
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
            foreach (var item in HandlingInfo.Fields)
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;

                if (fieldType == HandlingFieldTypes.Vector3Type)
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
            foreach (var item in HandlingInfo.Fields)
            {
                string fieldName = item.Key;
                Type type = item.Value.Type;

                if (type == HandlingFieldTypes.FloatType)
                {
                    DecorRegister(fieldName, 1);
                    DecorRegister($"{fieldName}_def", 1);
                }
                else if (type == HandlingFieldTypes.IntType)
                {
                    DecorRegister(fieldName, 3);
                    DecorRegister($"{fieldName}_def", 3);
                }
                else if (type == HandlingFieldTypes.Vector3Type)
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
            foreach (var item in HandlingInfo.Fields)
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;

                if (fieldType == HandlingFieldTypes.IntType || fieldType == HandlingFieldTypes.FloatType)
                {
                    string defDecorName = $"{fieldName}_def";

                    if (DecorExistOn(vehicle, fieldName))
                        DecorRemove(vehicle, fieldName);
                    if (DecorExistOn(vehicle, defDecorName))
                        DecorRemove(vehicle, defDecorName);
                }
                else if (fieldType == HandlingFieldTypes.Vector3Type)
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

            logger.Log(LogLevel.Debug, $"Removed all decorators on vehicle {vehicle}");
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
                if (!MathUtil.WithinEpsilon(currentValue, decorValue, m_epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    logger.Log(LogLevel.Debug, $"Updated decorator {name} updated from {decorValue} to {currentValue} for vehicle {vehicle}");
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (!MathUtil.WithinEpsilon(currentValue, defaultValue, m_epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    logger.Log(LogLevel.Debug, $"Added decorator {name} with value {currentValue} to vehicle {vehicle}");
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
                    logger.Log(LogLevel.Debug, $"Updated decorator {name} updated from {decorValue} to {currentValue} for vehicle {vehicle}");
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (currentValue != defaultValue)
                {
                    DecorSetInt(vehicle, name, currentValue);
                    logger.Log(LogLevel.Debug, $"Added decorator {name} with value {currentValue} to vehicle {vehicle}");
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
                Type fieldType = HandlingInfo.Fields[fieldName].Type;
                dynamic fieldValue = item.Value;

                string defDecorName = $"{fieldName}_def";
                dynamic defaultValue = preset.DefaultFields[fieldName];

                if (fieldType == HandlingFieldTypes.FloatType)
                {
                    UpdateFloatDecorator(vehicle, fieldName, fieldValue, defaultValue);
                    UpdateFloatDecorator(vehicle, defDecorName, defaultValue, fieldValue);
                }
                else if(fieldType == HandlingFieldTypes.IntType)
                {
                    UpdateIntDecorator(vehicle, fieldName, fieldValue, defaultValue);
                    UpdateIntDecorator(vehicle, defDecorName, defaultValue, fieldValue);
                }
                else if (fieldType == HandlingFieldTypes.Vector3Type)
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
        /// Prints the values of the decorators used on the <paramref name="vehicle"/>
        /// </summary>
        private void PrintDecorators(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
            {
                logger.Log(LogLevel.Error, $"Can't find vehicle with handle {vehicle}");
                return;
            }

            int netID = NetworkGetNetworkIdFromEntity(vehicle);
            StringBuilder s = new StringBuilder();
            s.AppendLine($"Vehicle:{vehicle} netID:{netID}");
            s.AppendLine("Decorators List:");

            foreach (var item in HandlingInfo.Fields)
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;
                string defDecorName = $"{fieldName}_def";

                dynamic value = 0, defaultValue = 0;

                if (fieldType == HandlingFieldTypes.FloatType)
                {
                    if (DecorExistOn(vehicle, item.Key))
                    {
                        value = DecorGetFloat(vehicle, fieldName);
                        defaultValue = DecorGetFloat(vehicle, defDecorName);
                        s.AppendLine($"{fieldName}: {value}({defaultValue})");
                    }
                }
                else if (fieldType == HandlingFieldTypes.IntType)
                {
                    if (DecorExistOn(vehicle, item.Key))
                    {
                        value = DecorGetInt(vehicle, fieldName);
                        defaultValue = DecorGetInt(vehicle, defDecorName);
                        s.AppendLine($"{fieldName}: {value}({defaultValue})");
                    }
                }
                else if (fieldType == HandlingFieldTypes.Vector3Type)
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

            CitizenFX.Core.Debug.WriteLine($"Vehicles with decorators: {entities.Count()}");

            StringBuilder s = new StringBuilder();
            foreach (var vehicle in entities)
            {
                int netID = NetworkGetNetworkIdFromEntity(vehicle);      
                s.AppendLine($"Vehicle:{vehicle} netID:{netID}");
            }
            CitizenFX.Core.Debug.WriteLine(s.ToString());
        }

        private void ReadFieldInfo(string filename = "HandlingInfo.xml")
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile(Globals.ResourceName, filename);
                HandlingInfo.ParseXml(strings);
                var editableFields = HandlingInfo.Fields.Where(a => a.Value.Editable);
                logger.Log(LogLevel.Information, $"Loaded {filename}, found {HandlingInfo.Fields.Count} fields info, {editableFields.Count()} editable.");
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, $"Error loading {filename}");
                logger.Log(LogLevel.Error, e.Message);
            }
        }

        private void ReadVehiclePermissions(string filename = "VehiclesPermissions.xml")
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile(Globals.ResourceName, filename);
                VehiclesPermissions.ParseXml(strings);
                logger.Log(LogLevel.Information, $"Loaded {filename}, found {VehiclesPermissions.Classes.Count} class rules and {VehiclesPermissions.Vehicles.Count} vehicle rules");
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, $"Error loading {filename}");
                logger.Log(LogLevel.Error, e.Message);
            }
        }

        private void ReadServerPresets(string filename = "HandlingPresets.xml")
        {
            try
            {
                string strings = LoadResourceFile(Globals.ResourceName, filename);
                Helpers.RemoveByteOrderMarks(ref strings);
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

                        preset.FromXml(node.OuterXml);
                        ServerPresetsManager.Save(name, preset);
                    }
                }
                logger.Log(LogLevel.Information, $"Loaded {filename}.");
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, $"Error loading {filename}");
                logger.Log(LogLevel.Error, e.Message);
            }
        }

        private HandlingConfig LoadConfig(string filename = "config.json")
        {
            HandlingConfig config;
            try
            {
                string strings = LoadResourceFile(Globals.ResourceName, filename);
                config = JsonConvert.DeserializeObject<HandlingConfig>(strings);

                //logger.Log(LogLevel.Information, $"Loaded config from {filename}");
            }
            catch (Exception e)
            {
                //logger.Log(LogLevel.Error, $"Impossible to load {filename}");
                //logger.Log(LogLevel.Error, e.Message);
                CitizenFX.Core.Debug.WriteLine(e.Message);
                config = new HandlingConfig();
            }

            return config;
        }

        #endregion
    }
}