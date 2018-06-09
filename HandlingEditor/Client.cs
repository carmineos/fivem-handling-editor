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

namespace handling_editor
{
    public class Client : BaseScript
    {
        #region CONFIG_FIEDS
        private static float editingFactor;
        private static float maxSyncDistance;
        private static long timer;
        private static bool debug;
        private static int toggleMenu;
        private static float screenPosX;
        private static float screenPosY;
        public static string title;
        public static string description;
        public static uint bannerColor;
        private static bool EnableBannerColor;
        #endregion


        #region FIELDS
        private static CHandlingDataInfo handlingInfo;
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
        #endregion
        /*
        private List<dynamic> BuildFloatList(float min, float max)
        {
            var values = new List<dynamic>();

            for (float i = min; i <= max; i += editingFactor)
                values.Add((float)Math.Round(i, 3));

            return values;
        }*/
        /*
        private List<dynamic> BuildIntList(int min, int max)
        {
            var values = new List<dynamic>();

            for (int i = min; i <= max; i++)
                values.Add(i);

            return values;
        }*/

        /*
        private UIMenuListItem AddList(UIMenu menu, FloatFieldInfo fieldInfo)
        {
            List<dynamic> values = BuildFloatList(fieldInfo.Min, fieldInfo.Max);
            //var currentIndex = values.IndexOf((float)Math.Round(currentValue, 3));
            var newitem = new UIMenuListItem(fieldInfo.Name, values, 0, fieldInfo.Description);
            menu.AddItem(newitem);
            return newitem;
        }*/
        /*
        private UIMenuListItem AddList(UIMenu menu, IntFieldInfo fieldInfo)
        {
            List<dynamic> values = BuildIntList(fieldInfo.Min, fieldInfo.Max);
            //var currentIndex = values.IndexOf((float)Math.Round(currentValue, 3));
            var newitem = new UIMenuListItem(fieldInfo.Name, values, 0, fieldInfo.Description);
            menu.AddItem(newitem);
            return newitem;
        }*/
        private UIMenuDynamicListItem AddDynamicFloatList(UIMenu menu, FloatFieldInfo fieldInfo)
        {
            if (!currentPreset.Fields.ContainsKey(fieldInfo.Name))
                return null;

            float value = currentPreset.Fields[fieldInfo.Name];
            var newitem = new UIMenuDynamicListItem(fieldInfo.Name, fieldInfo.Description, value.ToString("F2"), (sender, direction) =>
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
                return value.ToString("F2");
            });

            menu.AddItem(newitem);
            return newitem;
        }

        private UIMenuDynamicListItem AddDynamicIntList(UIMenu menu, IntFieldInfo fieldInfo)
        {
            if (!currentPreset.Fields.ContainsKey(fieldInfo.Name))
                return null;

            int value = currentPreset.Fields[fieldInfo.Name]; //TODO: Get value from current preset
            var newitem = new UIMenuDynamicListItem(fieldInfo.Name, fieldInfo.Description, value.ToString("F2"), (sender, direction) =>
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
                return value.ToString("F2");
            });

            menu.AddItem(newitem);
            return newitem;
        }
        
        private void AddMenuReset(UIMenu menu)
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
        }

        private void InitialiseMenu()
        {
            _menuPool = new MenuPool();
            EditorMenu = new UIMenu(title, description, new PointF(screenPosX * Screen.Width, screenPosY * Screen.Height));

            if (EnableBannerColor)
            {
                var banner = new UIResRectangle();
                banner.Color = Color.FromArgb((int)bannerColor);
                EditorMenu.SetBannerType(banner);
            }

            foreach (var item in handlingInfo.FieldsInfo.Where(a => a.Value.Editable == true))
            {
                if(item.Value.Type == typeof(float))
                    AddDynamicFloatList(EditorMenu, (FloatFieldInfo)item.Value);
                else if(item.Value.Type == typeof(int))
                    AddDynamicIntList(EditorMenu, (IntFieldInfo)item.Value);
                /*else if (item.Value.Type == typeof(VectorFieldInfo))
                    AddDynamicVectorList(EditorMenu, (VectorFieldInfo)item.Value);*/
            }

            AddMenuReset(EditorMenu);

            EditorMenu.MouseEdgeEnabled = false;
            EditorMenu.ControlDisablingEnabled = false;
            EditorMenu.MouseControlsEnabled = false;

            _menuPool.Add(EditorMenu);
            _menuPool.RefreshIndex();
            
            /*
            EditorMenu.OnListChange += (sender, item, index) =>
            {
                var value = item.IndexToItem(index);

                if (debug)
                    Debug.WriteLine($"Edited {item.Text} => [value:{value} index:{index}]");
            };*/
        }

        public Client()
        {
            Debug.WriteLine("HANDLING EDITOR: Script by Neos7");
            handlingInfo = new CHandlingDataInfo();
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
                if (IsControlJustPressed(1, toggleMenu) || IsDisabledControlJustPressed(1, toggleMenu)) // TOGGLE MENU VISIBLE
                    EditorMenu.Visible = !EditorMenu.Visible;
            }
            else
            {
                // Close menu if opened
                if (EditorMenu.Visible)
                    EditorMenu.Visible = false;
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

            // Check if current vehicle needs to be refreshed
            if (currentVehicle != -1 && currentPreset != null)
            {
                if (currentPreset.IsEdited)
                    //Debug.WriteLine("Current preset is edited");
                    RefreshVehicleUsingPreset(currentVehicle, currentPreset);
            }

            // Check if decorators needs to be updated
            if (currentTime > timer)
            {
                if (currentVehicle != -1 && currentPreset != null)
                    UpdateVehicleDecorators(currentVehicle, currentPreset);

                vehicles = new VehicleList();

                lastTime = GetGameTimer();
            }

            // Refreshes the iterated vehicles
            RefreshVehicles(vehicles.Except(new List<int> { currentVehicle }));

            await Delay(0);
        }

        private async void RefreshVehicleUsingPreset(int vehicle, HandlingPreset preset)
        {
            if (DoesEntityExist(vehicle))
            {
                foreach (var item in preset.Fields)
                {
                    //Debug.Write($"Refreshing {item.Key}, default:{preset.DefaultFields[item.Key]}, current:{item.Value}");

                    Type type = handlingInfo.FieldsInfo[item.Key].Type;

                    if (type == typeof(float))
                        SetVehicleHandlingFloat(vehicle, "CHandlingData", item.Key, item.Value);
                    /*
                    if (type == typeof(int))
                        SetVehicleHandlingInt(vehicle, "CHandlingData", item.Key, item.Value);

                    if (type == typeof(Vector3))
                        SetVehicleHandlingVector(vehicle, "CHandlingData", item.Key, item.Value);*/
                }
            }
            await Delay(0);
        }

        private async void UpdateVehicleDecorators(int vehicle, HandlingPreset preset)
        {
            foreach (var item in preset.Fields)
            {
                string defDecorName = $"{item.Key}_def";
                Type fieldType = handlingInfo.FieldsInfo[item.Key].Type;

                dynamic defaultValue = preset.DefaultFields[item.Key];

                if (fieldType == typeof(float))
                {
                    if (DecorExistOn(vehicle, item.Key))
                    {
                        float value = DecorGetFloat(vehicle, item.Key);
                        if (value != item.Value)
                            DecorSetFloat(vehicle, item.Key, item.Value);
                    }
                    else
                    {
                        if (defaultValue != item.Value)
                            DecorSetFloat(vehicle, item.Key, item.Value);
                    }

                    if (DecorExistOn(vehicle, defDecorName))
                    {
                        float value = DecorGetFloat(vehicle, defDecorName);
                        if (value != defaultValue)
                            DecorSetFloat(vehicle, defDecorName, defaultValue);
                    }
                    else
                    {
                        if (defaultValue != item.Value)
                            DecorSetFloat(vehicle, defDecorName, defaultValue);
                    }
                }
                if(fieldType == typeof(int))
                { }


            }
            await Delay(0);
        }

        private async void RemoveDecorators(int vehicle)
        {
            foreach (var item in handlingInfo.FieldsInfo)
            {
                string defDecorName = $"{item.Key}_def";

                if (DecorExistOn(vehicle, item.Key))
                    DecorRemove(vehicle, item.Key);
                if (DecorExistOn(vehicle, defDecorName))
                    DecorRemove(vehicle, defDecorName);
            }

            await Delay(0);
        }

        private void RegisterDecorators()
        {
            foreach (var item in handlingInfo.FieldsInfo)
            {
                string defDecorName = $"{item.Key}_def";
                Type type = item.Value.Type;

                if (type == typeof(float))
                {
                    DecorRegister(item.Key, 1);
                    DecorRegister(defDecorName, 1);
                }/*
                else if (type == typeof(int))
                {
                    DecorRegister(item.Key, 3);
                    DecorRegister(defDecorName, 3);
                }*/
            }
        }

        private async void RefreshVehicles(IEnumerable<int> vehiclesList)
        {
            Vector3 currentCoords = GetEntityCoords(playerPed, true);

            foreach (int entity in vehiclesList)
            {
                //if (entity != currentVehicle)
                if (DoesEntityExist(entity))
                {
                    Vector3 coords = GetEntityCoords(entity, true);

                    if (Vector3.Distance(currentCoords, coords) <= maxSyncDistance)
                        RefreshVehicleUsingDecorators(entity);
                }
            }
            await Delay(0);
        }

        private async void RefreshVehicleUsingDecorators(int vehicle)
        {
            foreach (var item in handlingInfo.FieldsInfo)
            {
                if (DecorExistOn(vehicle, item.Key))
                {
                    Type type = item.Value.Type;

                    if (type == typeof(float))
                    {
                        var value = DecorGetFloat(vehicle, item.Key);
                        SetVehicleHandlingFloat(vehicle, "CHandlingData", item.Key, value);
                    }/*
                    else if (type == typeof(int))
                    {
                        var value = DecorGetInt(vehicle, item.Key);
                        SetVehicleHandlingInt(vehicle, "CHandlingData", item.Key, value);
                    }
                    else if (type == typeof(Vector3))
                    { }*/

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

                string defDecorName = $"{item.Key}_def";

                if (item.Value.Type == typeof(float))
                {
                    if (DecorExistOn(vehicle, defDecorName))
                        defaultFields[item.Key] = DecorGetFloat(vehicle, defDecorName);
                    else defaultFields[item.Key] = GetVehicleHandlingFloat(vehicle, "CHandlingData", item.Key);

                    if (DecorExistOn(vehicle, item.Key))
                        fields[item.Key] = DecorGetFloat(vehicle, item.Key);
                    else fields[item.Key] = defaultFields[item.Key];
                }
                /*
                else if (item.Value.Type == typeof(int))
                {
                    if (DecorExistOn(vehicle, defDecorName))
                        defaultFields[item.Key] = DecorGetInt(vehicle, defDecorName);
                    else defaultFields[item.Key] = GetVehicleHandlingInt(vehicle, "CHandlingData", item.Key);

                    if (DecorExistOn(vehicle, item.Key))
                        fields[item.Key] = DecorGetInt(vehicle, item.Key);
                    else fields[item.Key] = defaultFields[item.Key];
                }*/
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
                        string defDecorName = $"{item.Key}_def";
                        Type type = item.Value.Type;

                        dynamic value = 0, defaultValue = 0;

                        if (type == typeof(float))
                        {
                            value = DecorGetFloat(vehicle, item.Key);
                            defaultValue = DecorGetFloat(vehicle, defDecorName);
                        }
                        if (type == typeof(int))
                        {
                            value = DecorGetInt(vehicle, item.Key);
                            defaultValue = DecorGetInt(vehicle, defDecorName);
                        }
                        s.Append($"{item.Key}:{value}({defaultValue}) ");
                    }
                        
                }
                Debug.WriteLine(s.ToString());
            }
            else Debug.WriteLine("HANDLING_EDITOR: Current vehicle doesn't exist");

            await Delay(0);
        }

        private bool HasDecorators(int entity)
        {
            foreach (var item in handlingInfo.FieldsInfo)
            {
                if (DecorExistOn(entity, item.Key))
                    return true;
            }
            return false;
        }

        private async void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
        {
            IEnumerable<int> entities = vehiclesList.Where(entity => HasDecorators(entity));

            Debug.WriteLine($"HANDLING EDITOR: Vehicles with decorators: {entities.Count()}");

            foreach (var item in entities)
                PrintDecoratorsInfo(item);

            await Delay(0);
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
                title = config.title;
                description = config.description;
                bannerColor = config.bannerColor;
                EnableBannerColor = config.EnableBannerColor;
            }
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
        public string title { get; set; }
        public string description { get; set; }
        public uint bannerColor { get; set; }
        public bool EnableBannerColor { get; set; }

        public Config()
        {
            editingFactor = 0.01f;
            maxSyncDistance = 150.0f;
            toggleMenu = 167;
            timer = 1000;
            debug = false;
            screenPosX = 1.0f;
            screenPosY = 0.0f;
            title = "Wheels Editor";
            description = "~b~Track Width & Camber";
            bannerColor = 0xFFF04040;
            EnableBannerColor = false;
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

            if (Entries.ContainsKey("title"))
                title = Entries["title"].Trim();

            if (Entries.ContainsKey("description"))
                description = Entries["description"].Trim();

            if (Entries.ContainsKey("bannerColor"))
                bannerColor = Convert.ToUInt32(Entries["bannerColor"], 16);

            if (Entries.ContainsKey("EnableBannerColor"))
                EnableBannerColor = bool.Parse(Entries["EnableBannerColor"]);
        }
    }
}
