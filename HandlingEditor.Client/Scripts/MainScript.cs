using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

using HandlingEditor.Client.UI;
using Newtonsoft.Json;

namespace HandlingEditor.Client.Scripts
{
    public class MainScript : BaseScript
    {
        private readonly MainMenu Menu;

        private long _lastTime;
        private int _playerVehicleHandle;
        private int _playerPedHandle;
        private Vector3 _playerPedCoords;
        private List<int> _worldVehiclesHandles;
        private float _maxDistanceSquared;

        internal int PlayerVehicleHandle
        {
            get => _playerVehicleHandle;
            private set
            {
                if (Equals(_playerVehicleHandle, value))
                    return;

                _playerVehicleHandle = value;
                PlayerVehicleHandleChanged?.Invoke(this, value);
            }
        }

        internal int PlayerPedHandle
        {
            get => _playerPedHandle;
            private set
            {
                if (Equals(_playerPedHandle, value))
                    return;

                _playerPedHandle = value;
                PlayerPedHandleChanged?.Invoke(this, value);
            }
        }

        internal event EventHandler<int> PlayerVehicleHandleChanged;
        internal event EventHandler<int> PlayerPedHandleChanged;

        internal event EventHandler ToggleMenuVisibility;

        internal Config Config { get; private set; }
        internal HandlingEditorScript HandlingEditorScript { get; private set; }
        internal ClientPresetsScript ClientPresetsScript { get; private set; }
        internal ServerPresetsScript ServerPresetsScript { get; private set; }
        internal ClientSettingsScript ClientSettingsScript { get; private set; }

        public MainScript()
        {
            if (GetCurrentResourceName() != Globals.ResourceName)
            {
                Debug.WriteLine($"{Globals.ScriptName}: Invalid resource name, be sure the resource name is {Globals.ResourceName}");
                return;
            }

            LoadHandlingInfo();

            _lastTime = GetGameTimer();
            _playerVehicleHandle = -1;
            _playerPedHandle = -1;
            _playerPedCoords = Vector3.Zero;
            _worldVehiclesHandles = new List<int>();
            _maxDistanceSquared = 10000;

            Config = LoadConfig();
            _maxDistanceSquared = (float)Math.Pow(Config.ScriptRange, 2.0);

            HandlingEditorScript = new HandlingEditorScript(this);
            RegisterScript(HandlingEditorScript);

            if (Config.EnableClientPresets)
            {
                ClientPresetsScript = new ClientPresetsScript(this);
            }

            if (Config.EnableServerPresets)
            {
                ServerPresetsScript = new ServerPresetsScript(this);
            }

            if (Config.EnableClientSettings)
            {
                ClientSettingsScript = new ClientSettingsScript(this);
            }

            if (!Config.DisableMenu)
                Menu = new MainMenu(this);

            Tick += GetPlayerAndVehicleTask;
            Tick += TimedTask;
            Tick += HideUITask;
        }

        private async Task HideUITask()
        {
            if (Menu != null)
                Menu.HideMenu = _playerVehicleHandle == -1;

            await Task.FromResult(0);
        }

        internal List<int> GetCloseVehicleHandles()
        {
            List<int> closeVehicles = new List<int>();

            foreach (int handle in _worldVehiclesHandles)
            {
                if (!DoesEntityExist(handle))
                    continue;

                Vector3 coords = GetEntityCoords(handle, true);

                if (Vector3.DistanceSquared(_playerPedCoords, coords) <= _maxDistanceSquared)
                    closeVehicles.Add(handle);
            }

            return closeVehicles;
        }

        private async Task TimedTask()
        {
            long currentTime = GetGameTimer() - _lastTime;

            if (currentTime > Config.Timer)
            {
                _playerPedCoords = GetEntityCoords(_playerPedHandle, true);

                _worldVehiclesHandles = Utilities.GetWorldVehicles();

                _lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
        }

        private async Task GetPlayerAndVehicleTask()
        {
            await Task.FromResult(0);

            _playerPedHandle = PlayerPedId();

            if (!IsPedInAnyVehicle(_playerPedHandle, false))
            {
                PlayerVehicleHandle = -1;
                return;
            }

            int vehicle = GetVehiclePedIsIn(_playerPedHandle, false);

            // If this model isn't a car, or player isn't the driver, or vehicle is not driveable
            if (!IsThisModelACar((uint)GetEntityModel(vehicle)) || GetPedInVehicleSeat(vehicle, -1) != _playerPedHandle || !IsVehicleDriveable(vehicle, false))
            {
                PlayerVehicleHandle = -1;
                return;
            }

            PlayerVehicleHandle = vehicle;
        }

        private Config LoadConfig(string filename = "config.json")
        {
            Config config;

            try
            {
                string strings = LoadResourceFile(Globals.ResourceName, filename);
                config = JsonConvert.DeserializeObject<Config>(strings);

                Debug.WriteLine($"{Globals.ScriptName}: Loaded config from {filename}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{Globals.ScriptName}: Impossible to load {filename}", e.Message);
                Debug.WriteLine(e.StackTrace);

                config = new Config();
            }

            return config;
        }

        private void LoadHandlingInfo(string filename = "HandlingInfo.xml")
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile(Globals.ResourceName, filename);
                HandlingInfo.ParseXml(strings);
                Debug.WriteLine($"{nameof(MainScript)}: Loaded {filename}, found {HandlingInfo.Fields.Count} fields info");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{nameof(MainScript)}: Error loading {filename}");
                Debug.WriteLine($"{nameof(MainScript)}: {e.Message}");
            }
        }

        public async Task<string> GetOnScreenString(string title, string defaultText)
        {
            DisplayOnscreenKeyboard(1, title, "", defaultText, "", "", "", 128);
            while (UpdateOnscreenKeyboard() != 1 && UpdateOnscreenKeyboard() != 2) await Delay(100);

            return GetOnscreenKeyboardResult();
        }

        private void RegisterCommands()
        {
            
            RegisterCommand($"{Globals.CommandPrefix}range", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"{nameof(MainScript)}: Missing float argument");
                    return;
                }

                if (float.TryParse(args[0], out float value))
                {
                    Config.ScriptRange = value;
                    _maxDistanceSquared = (float)Math.Pow(Config.ScriptRange, 2.0);
                    Debug.WriteLine($"{nameof(MainScript)}: {nameof(Config.ScriptRange)} updated to {value}");
                }
                else Debug.WriteLine($"{nameof(MainScript)}: Error parsing {args[0]} as float");

            }), false);

            /**
            RegisterCommand($"{Globals.CommandPrefix}decorators", new Action<int, dynamic>((source, args) =>
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

            RegisterCommand($"{Globals.CommandPrefix}print", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators(_worldVehiclesHandles);
            }), false);

            RegisterCommand($"{Globals.CommandPrefix}preset", new Action<int, dynamic>((source, args) =>
            {
                if (CurrentPreset != null)
                    CitizenFX.Core.Debug.WriteLine(CurrentPreset.ToString());
                else
                    logger.Log(LogLevel.Error, "Current preset is not valid");
            }), false);

            RegisterCommand($"{Globals.CommandPrefix}xml", new Action<int, dynamic>((source, args) =>
            {
                if (CurrentPreset != null)
                    CitizenFX.Core.Debug.WriteLine(CurrentPreset.ToXml());
                else
                    logger.Log(LogLevel.Error, "Current preset is not valid");
            }), false);
            */

            if (!Config.DisableMenu)
            {
                if (Config.ExposeCommand)
                    RegisterCommand("handling_editor", new Action<int, dynamic>((source, args) => { ToggleMenuVisibility?.Invoke(this, EventArgs.Empty); }), false);

                if (Config.ExposeEvent)
                    EventHandlers.Add("handling_editor:toggleMenu", new Action(() => { ToggleMenuVisibility?.Invoke(this, EventArgs.Empty); }));
            }
        }
    }
}
