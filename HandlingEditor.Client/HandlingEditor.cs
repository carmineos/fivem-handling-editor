using System;
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
    // TODO: Rework events subscriptions, split UI, editor and preset events
    public class HandlingEditor : BaseScript
    {
        private readonly ILogger logger;
        private readonly INotificationHandler notifier;

        private const float Epsilon = 0.001f;

        private readonly HandlingMenu _handlingMenu;

        private long _lastTime;

        private int _playerPedHandle;

        private int _playerVehicleHandle;

        private IEnumerable<int> _worldVehiclesHandles;

        /// <summary>
        /// An event triggered when <see cref="CurrentPreset"/> changes
        /// </summary>
        public event EventHandler CurrentPresetChanged;

        public bool CurrentPresetIsValid => _playerVehicleHandle != -1 && CurrentPreset != null;
        public HandlingPreset CurrentPreset { get; private set; }
        public HandlingInfo HandlingInfo { get; private set; }
        public HandlingConfig Config { get; private set; }

        public IPresetManager<string, HandlingPreset> LocalPresetsManager { get; private set; }
        public IPresetManager<string, HandlingPreset> ServerPresetsManager { get; private set; }


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

            _lastTime = GetGameTimer();
            _worldVehiclesHandles = Enumerable.Empty<int>();
            _playerVehicleHandle = -1;
            
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
                    PrintDecorators(_playerVehicleHandle);
                else
                {
                    if (int.TryParse(args[0], out int value))
                        PrintDecorators(value);
                    else logger.Log(LogLevel.Error, $"Can't parse {args[0]} as int");
                }

            }), false);

            RegisterCommand("handling_print", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators(_worldVehiclesHandles);
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
            _handlingMenu = new HandlingMenu(this);

            _handlingMenu.MenuApplyPersonalPresetButtonPressed += (sender, key) => LoadPersonalPreset(key);
            _handlingMenu.MenuApplyServerPresetButtonPressed += (sender, key) => LoadServerPreset(key);
            _handlingMenu.MenuSavePersonalPresetButtonPressed += (sender, key) => SavePersonalPreset(key, CurrentPreset);
            _handlingMenu.MenuDeletePersonalPresetButtonPressed += (sender, key) => DeletePersonalPreset(key);
            _handlingMenu.MenuResetPresetButtonPressed += (senders, args) => Reset();
            _handlingMenu.MenuPresetValueChanged += SetPresetValue;

            Tick += GetPlayerVehicleTask;
            Tick += UpdateWorldVehiclesTask;
            Tick += HideUITask;

        }


        /// <summary>
        /// Updates a field of the current preset
        /// </summary>
        /// <param name="fieldName">The name of the field which needs to be updated</param>
        /// <param name="fieldValue">The new value of the field</param>
        /// <param name="fieldId">The ID of the field (used for vector3 components)</param>
        private void SetPresetValue(string fieldName, string fieldValue, string fieldId)
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
                var value = (Vector3)CurrentPreset.Fields[fieldName];

                //Debug.WriteLine($"Current Value is:{value} received (value: {fieldValue}, id: {fieldId}");
                
                // Update the correct Vector3 component
                if (fieldId.EndsWith(".x"))
                {
                    if (float.TryParse(fieldValue, out float result))
                    {
                        value.X = result;
                        CurrentPreset.Fields[fieldName] = value;
                    }
                }
                else if (fieldId.EndsWith(".y"))
                {
                    if (float.TryParse(fieldValue, out float result))
                    {
                        value.Y = result;
                        CurrentPreset.Fields[fieldName] = value;
                    }
                }
                else if (fieldId.EndsWith(".z"))
                {
                    if (float.TryParse(fieldValue, out float result))
                    {
                        value.Z = result;
                        CurrentPreset.Fields[fieldName] = value;
                    }
                }
            }

            // TODO: This will be called as callback once HandlingPreset has been refactored to invoke HandlingFieldEdited
            OnPresetFieldEdited(this, fieldName);
        }

        private void OnPresetFieldEdited(object sender, string fieldName)
        {
            if (!CurrentPresetIsValid)
                return;

            UpdateVehicleHandlingFieldUsingPreset(_playerVehicleHandle, CurrentPreset, fieldName);

            UpdateVehicleDecoratorUsingPreset(_playerVehicleHandle, CurrentPreset, fieldName);
        }

        private async void Reset()
        {
            // Reset the preset
            CurrentPreset.Reset();
            RemoveDecorators(_playerVehicleHandle);
            UpdateVehicleHandlingUsingPreset(_playerVehicleHandle, CurrentPreset);

            await Delay(200);
            CurrentPresetChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void SavePersonalPreset(string presetName, HandlingPreset preset)
        {
            await Task.FromResult(0);

            if (LocalPresetsManager.Save(presetName, preset))
                notifier.Notify($"Personal preset ~g~{presetName}~w~ saved");
            else
                notifier.Notify($"~r~ERROR~w~ The name {presetName} is invalid or already used.");
        }

        private async void DeletePersonalPreset(string presetName)
        {
            await Task.FromResult(0);

            if (LocalPresetsManager.Delete(presetName))
                notifier.Notify($"Personal preset ~r~{presetName}~w~ deleted");
            else
                notifier.Notify($"~r~ERROR~w~ The name {presetName} is invalid or not found.");
        }

        private async void LoadServerPreset(string presetName)
        {
            if (!ServerPresetsManager.Load(presetName, out HandlingPreset preset))
                notifier.Notify($"~r~ERROR~w~ Server preset ~b~{presetName}~w~ corrupted");
            else
            {
                CurrentPreset.CopyFields(preset, Config.CopyOnlySharedFields);
                CurrentPresetChanged?.Invoke(this, EventArgs.Empty);

                notifier.Notify($"Server preset ~b~{presetName}~w~ applied");
            }

            await Delay(200);
        }

        private async void LoadPersonalPreset(string presetName)
        {
            if(!LocalPresetsManager.Load(presetName, out HandlingPreset preset))
                notifier.Notify($"~r~ERROR~w~ Personal preset ~b~{presetName}~w~ corrupted");
            else
            {
                CurrentPreset.CopyFields(preset, Config.CopyOnlySharedFields);
                CurrentPresetChanged?.Invoke(this, EventArgs.Empty);

                notifier.Notify($"Personal preset ~b~{presetName}~w~ applied");
            }

            await Delay(200);
        }


        /// <summary>
        /// Updates the <see cref="_playerVehicleHandle"/> and the <see cref="CurrentPreset"/>
        /// </summary>
        /// <returns></returns>
        private async Task GetPlayerVehicleTask()
        {
            await Task.FromResult(0);
            
            _playerPedHandle = PlayerPedId();

            // If player isn't in any vehicle
            if (!IsPedInAnyVehicle(_playerPedHandle, false))
            {
                _playerVehicleHandle = -1;
                //CurrentPreset.HandlingFieldEdited -= OnPresetFieldEdited;
                CurrentPreset = null;
                return;
            }

            // Actually this will return 0 if ped isn't in any vehicle
            // So maybe the check above can be removed
            int vehicle = GetVehiclePedIsIn(_playerPedHandle, false);

            // If vehicle is not allowed, or vehicle is dead, or player isn't the driver
            if (!VehiclesPermissions.IsVehicleAllowed(vehicle) || IsEntityDead(vehicle) || GetPedInVehicleSeat(vehicle, -1) != _playerPedHandle)
            {
                _playerVehicleHandle = -1;
                //CurrentPreset.HandlingFieldEdited -= OnPresetFieldEdited;
                CurrentPreset = null;
                return;
            }

            // If the vehicle is new
            if (vehicle != _playerVehicleHandle)
            {
                // Update current vehicle and get its preset
                _playerVehicleHandle = vehicle;
                logger.Log(LogLevel.Debug, $"New vehicle handle: {_playerVehicleHandle}");

                CurrentPreset = new HandlingPreset();
                CurrentPreset.FromHandle(_playerVehicleHandle);
                //CurrentPreset.HandlingFieldEdited += OnPresetFieldEdited;
                CurrentPresetChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// The main task of the script
        /// </summary>
        /// <returns></returns>
        private async Task UpdateWorldVehiclesTask()
        {
            var CurrentTime = (GetGameTimer() - _lastTime);

            // Check if decorators needs to be updated
            if (CurrentTime > Config.Timer)
            {
                _worldVehiclesHandles = new VehicleEnumerable();

                // Refreshes the iterated vehicles
                UpdateWorldVehiclesUsingDecorators(_worldVehiclesHandles.Except(new List<int> { _playerVehicleHandle }));

                _lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
        }

        private async Task HideUITask()
        {
            if (_handlingMenu != null)
                _handlingMenu.HideUI = !CurrentPresetIsValid;

            await Task.FromResult(0);
        }


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
                    if (DecorExistOn(vehicle, $"{fieldName}.x") || DecorExistOn(vehicle, $"{fieldName}.y") || DecorExistOn(vehicle, $"{fieldName}.z"))
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
                    string decorX = $"{fieldName}.x";
                    string decorY = $"{fieldName}.y";
                    string decorZ = $"{fieldName}.z";

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
                    string decorX = $"{fieldName}.x";
                    string decorY = $"{fieldName}.y";
                    string decorZ = $"{fieldName}.z";
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
                    string decorX = $"{fieldName}.x";
                    if (DecorExistOn(vehicle, decorX))
                    {
                        string defDecorNameX = $"{decorX}_def";
                        var x = DecorGetFloat(vehicle, decorX);
                        var defX = DecorGetFloat(vehicle, defDecorNameX);
                        s.AppendLine($"{decorX}: {x}({defX})");
                    }

                    string decorY = $"{fieldName}.y";
                    if (DecorExistOn(vehicle, decorY))
                    {
                        string defDecorNameY = $"{decorY}_def";
                        var y = DecorGetFloat(vehicle, decorY);
                        var defY = DecorGetFloat(vehicle, defDecorNameY);
                        s.AppendLine($"{decorY}: {y}({defY})");
                    }

                    string decorZ = $"{fieldName}.z";
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
                logger.Log(LogLevel.Information, $"Loaded {filename}, found {HandlingInfo.Fields.Count} fields info");
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




        private void UpdateVehicleHandlingDecorator(int vehicle, string fieldName, Vector3 fieldValue, Vector3 defaultValue)
        {
            string decorX = $"{fieldName}.x";
            string decorY = $"{fieldName}.y";
            string decorZ = $"{fieldName}.z";
            string defDecorNameX = $"{decorX}_def";
            string defDecorNameY = $"{decorY}_def";
            string defDecorNameZ = $"{decorZ}_def";

            UpdateDecorator(vehicle, decorX, fieldValue.X, defaultValue.X);
            UpdateDecorator(vehicle, defDecorNameX, defaultValue.X, fieldValue.X);

            UpdateDecorator(vehicle, decorY, fieldValue.Y, defaultValue.Y);
            UpdateDecorator(vehicle, defDecorNameY, defaultValue.Y, fieldValue.Y);

            UpdateDecorator(vehicle, decorZ, fieldValue.Z, defaultValue.Z);
            UpdateDecorator(vehicle, defDecorNameZ, defaultValue.Z, fieldValue.Z);
        }

        private void UpdateVehicleHandlingDecorator(int vehicle, string fieldName, int fieldValue, int defaultValue)
        {
            string defDecorName = $"{fieldName}_def";
            UpdateDecorator(vehicle, fieldName, fieldValue, defaultValue);
            UpdateDecorator(vehicle, defDecorName, defaultValue, fieldValue);
        }

        private void UpdateVehicleHandlingDecorator(int vehicle, string fieldName, float fieldValue, float defaultValue)
        {
            string defDecorName = $"{fieldName}_def";
            UpdateDecorator(vehicle, fieldName, fieldValue, defaultValue);
            UpdateDecorator(vehicle, defDecorName, defaultValue, fieldValue);
        }

        private void UpdateVehicleHandlingField(int vehicle, string className, string fieldName, int fieldValue)
        {
            var value = GetVehicleHandlingInt(vehicle, className, fieldName);
            if (value != fieldValue)
            {
                SetVehicleHandlingInt(vehicle, className, fieldName, fieldValue);
                logger.Log(LogLevel.Debug, $"Entity ({vehicle}) handling field {fieldName} updated from {value} to {fieldValue}");
            }
        }

        private void UpdateVehicleHandlingField(int vehicle, string className, string fieldName, float fieldValue)
        {
            var value = GetVehicleHandlingFloat(vehicle, className, fieldName);
            if (!MathUtil.WithinEpsilon(value, fieldValue, Epsilon))
            {
                SetVehicleHandlingFloat(vehicle, className, fieldName, fieldValue);
                logger.Log(LogLevel.Debug, $"Entity ({vehicle}) handling field {fieldName} updated from {value} to {fieldValue}");
            }
        }

        private void UpdateVehicleHandlingField(int vehicle, string className, string fieldName, Vector3 fieldValue)
        {
            var value = GetVehicleHandlingVector(vehicle, className, fieldName);
            if (!value.Equals(fieldValue))
            {
                SetVehicleHandlingVector(vehicle, className, fieldName, fieldValue);

                logger.Log(LogLevel.Debug, $"Entity ({vehicle}) handling field {fieldName} updated from {value} to {fieldValue}");
            }
        }

        private void UpdateVehicleHandlingFieldUsingPreset(int vehicle, HandlingPreset preset, string fieldName)
        {
            // Be sure the handling contains such field 
            if (!preset.Fields.TryGetValue(fieldName, out dynamic fieldValue))
            {
                logger.Log(LogLevel.Error, $"Preset doesn't contain the field {fieldName}");
                return;
            }

            // Be sure the handling info contains such field
            if (!HandlingInfo.Fields.TryGetValue(fieldName, out HandlingFieldInfo handlingFieldInfo))
            {
                logger.Log(LogLevel.Error, $"HandlingInfo doesn't contain the field {fieldName}");
                return;
            }

            // Get field type
            Type fieldType = handlingFieldInfo.Type;
            string className = handlingFieldInfo.ClassName;

            if (fieldType == HandlingFieldTypes.IntType)
            {
                UpdateVehicleHandlingField(vehicle, className, fieldName, (int)fieldValue);
            }
            else if (fieldType == HandlingFieldTypes.FloatType)
            {
                UpdateVehicleHandlingField(vehicle, className, fieldName, (float)fieldValue);
            }
            else if (fieldType == HandlingFieldTypes.Vector3Type)
            {
                UpdateVehicleHandlingField(vehicle, className, fieldName, (Vector3)fieldValue);
            }
        }

        /// <summary>
        /// Refreshes the handling for the <paramref name="vehicle"/> using the <paramref name="preset"/>.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="preset"></param>
        private void UpdateVehicleHandlingUsingPreset(int vehicle, HandlingPreset preset)
        {
            if (!DoesEntityExist(vehicle))
                return;

            foreach (var fieldName in preset.Fields.Keys)
            {
                UpdateVehicleHandlingFieldUsingPreset(vehicle, preset, fieldName);
            }
        }

        /// <summary>
        /// Refreshes the handling for the <paramref name="vehicle"/> using the decorators attached to it.
        /// </summary>
        /// <param name="vehicle"></param>
        private void UpdateVehicleHandlingUsingDecorators(int vehicle)
        {
            foreach (var item in HandlingInfo.Fields)
            {
                string fieldName = item.Key;
                Type fieldType = item.Value.Type;
                string className = item.Value.ClassName;

                if (fieldType == HandlingFieldTypes.FloatType)
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        var decorValue = DecorGetFloat(vehicle, fieldName);
                        UpdateVehicleHandlingField(vehicle, className, fieldName, decorValue);
                    }
                }
                else if (fieldType == HandlingFieldTypes.IntType)
                {
                    if (DecorExistOn(vehicle, fieldName))
                    {
                        var decorValue = DecorGetInt(vehicle, fieldName);
                        UpdateVehicleHandlingField(vehicle, className, fieldName, decorValue);
                    }
                }
                else if (fieldType == HandlingFieldTypes.Vector3Type)
                {
                    string decorX = $"{fieldName}.x";
                    string decorY = $"{fieldName}.y";
                    string decorZ = $"{fieldName}.z";

                    Vector3 value = GetVehicleHandlingVector(vehicle, className, fieldName);
                    Vector3 decorValue = new Vector3(value.X, value.Y, value.Z);

                    if (DecorExistOn(vehicle, decorX))
                        decorValue.X = DecorGetFloat(vehicle, decorX);

                    if (DecorExistOn(vehicle, decorY))
                        decorValue.Y = DecorGetFloat(vehicle, decorY);

                    if (DecorExistOn(vehicle, decorZ))
                        decorValue.Z = DecorGetFloat(vehicle, decorZ);

                    UpdateVehicleHandlingField(vehicle, className, fieldName, decorValue);
                }
            }
        }

        /// <summary>
        /// Refreshes the handling for the vehicles in <paramref name="vehiclesList"/> if they are close enough.
        /// </summary>
        /// <param name="vehiclesList"></param>
        private void UpdateWorldVehiclesUsingDecorators(IEnumerable<int> vehiclesList)
        {
            Vector3 currentCoords = GetEntityCoords(_playerPedHandle, true);

            foreach (int entity in vehiclesList)
            {
                if (!DoesEntityExist(entity))
                    continue;

                Vector3 coords = GetEntityCoords(entity, true);

                if (Vector3.Distance(currentCoords, coords) <= Config.ScriptRange)
                    UpdateVehicleHandlingUsingDecorators(entity);
            }
        }

        /// <summary>
        /// Updates the decorators on the <paramref name="vehicle"/> with updated values from the <paramref name="preset"/>
        /// </summary>
        /// <param name="vehicle"></param>
        private void UpdateVehicleDecoratorsUsingPreset(int vehicle, HandlingPreset preset)
        {
            if (!DoesEntityExist(vehicle))
                return;

            foreach (var item in preset.Fields)
            {
                string fieldName = item.Key;

                // Be sure the handling info contains such field
                if (!HandlingInfo.Fields.TryGetValue(fieldName, out HandlingFieldInfo handlingFieldInfo))
                {
                    logger.Log(LogLevel.Error, $"HandlingInfo doesn't contain the field {fieldName}");
                    return;
                }

                Type fieldType = handlingFieldInfo.Type;
                dynamic fieldValue = item.Value;
                dynamic defaultValue = preset.DefaultFields[fieldName];

                if (fieldType == HandlingFieldTypes.FloatType)
                {
                    UpdateVehicleHandlingDecorator(vehicle, fieldName, (float)fieldValue, (float)defaultValue);
                }
                else if (fieldType == HandlingFieldTypes.IntType)
                {
                    UpdateVehicleHandlingDecorator(vehicle, fieldName, (int)fieldValue, (int)defaultValue);
                }
                else if (fieldType == HandlingFieldTypes.Vector3Type)
                {
                    UpdateVehicleHandlingDecorator(vehicle, fieldName, (Vector3)fieldValue, (Vector3)defaultValue);
                }
            }
        }

        private void UpdateVehicleDecoratorUsingPreset(int vehicle, HandlingPreset preset, string fieldName)
        {
            if (!DoesEntityExist(vehicle))
                return;

            // Be sure the handling info contains such field
            if (!HandlingInfo.Fields.TryGetValue(fieldName, out HandlingFieldInfo handlingFieldInfo))
            {
                logger.Log(LogLevel.Error, $"HandlingInfo doesn't contain the field {fieldName}");
                return;
            }

            Type fieldType = handlingFieldInfo.Type;
            dynamic fieldValue = preset.Fields[fieldName];
            dynamic defaultValue = preset.DefaultFields[fieldName];

            if (fieldType == HandlingFieldTypes.FloatType)
            {
                UpdateVehicleHandlingDecorator(vehicle, fieldName, (float)fieldValue, (float)defaultValue);
            }
            else if (fieldType == HandlingFieldTypes.IntType)
            {
                UpdateVehicleHandlingDecorator(vehicle, fieldName, (int)fieldValue, (int)defaultValue);
            }
            else if (fieldType == HandlingFieldTypes.Vector3Type)
            {
                UpdateVehicleHandlingDecorator(vehicle, fieldName, (Vector3)fieldValue, (Vector3)defaultValue);
            }
        }

        /// <summary>
        /// It checks if the <paramref name="vehicle"/> has a decorator named <paramref name="name"/> and updates its value with <paramref name="currentValue"/>, otherwise if <paramref name="currentValue"/> isn't equal to <paramref name="defaultValue"/> it adds the decorator <paramref name="name"/>
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="name"></param>
        /// <param name="currentValue"></param>
        /// <param name="defaultValue"></param>
        private void UpdateDecorator(int vehicle, string name, float currentValue, float defaultValue)
        {
            // Decorator exists
            if (DecorExistOn(vehicle, name))
            {
                float decorValue = DecorGetFloat(vehicle, name);
                // Check if needs to be updated
                if (!MathUtil.WithinEpsilon(currentValue, decorValue, Epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    logger.Log(LogLevel.Debug, $"Decorator {name} updated from {decorValue} to {currentValue} for entity {vehicle}");
                }
            }
            else // Decorator doesn't exist
            {
                // Create it if required
                if (!MathUtil.WithinEpsilon(currentValue, defaultValue, Epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    logger.Log(LogLevel.Debug, $"Decorator {name} added with value {currentValue} for entity {vehicle}");
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
        private void UpdateDecorator(int vehicle, string name, int currentValue, int defaultValue)
        {
            // Decorator exists
            if (DecorExistOn(vehicle, name))
            {
                int decorValue = DecorGetInt(vehicle, name);
                // Check if needs to be updated
                if (currentValue != decorValue)
                {
                    DecorSetInt(vehicle, name, currentValue);
                    logger.Log(LogLevel.Debug, $"Decorator {name} updated from {decorValue} to {currentValue} for entity {vehicle}");
                }
            }
            else // Decorator doesn't exist
            {
                // Create it if required
                if (currentValue != defaultValue)
                {
                    DecorSetInt(vehicle, name, currentValue);
                    logger.Log(LogLevel.Debug, $"Decorator {name} added with value {currentValue} for entity {vehicle}");
                }
            }
        }
    }
}