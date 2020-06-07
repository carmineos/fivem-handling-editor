using CitizenFX.Core;
using HandlingEditor.Client.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace HandlingEditor.Client.Scripts
{
    internal class HandlingEditorScript : BaseScript
    {
        private readonly MainScript _mainScript;

        private int _playerVehicleHandle;
        private long _lastTime;

        private HandlingData _handlingData;

        internal HandlingData HandlingData
        {
            get => _handlingData;
            set
            {
                if (Equals(_handlingData, value))
                    return;

                _handlingData = value;
                HandlingDataChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal bool DataIsValid => _playerVehicleHandle != -1 && HandlingData != null;

        internal Config Config => _mainScript.Config;
        internal HandlingEditorMenu Menu { get; private set; }

        internal event EventHandler HandlingDataChanged;

        internal const string ResetID = "handling_editor_reset";

        internal HandlingEditorScript(MainScript mainScript)
        {
            _mainScript = mainScript;

            _playerVehicleHandle = -1;

            RegisterDecorators();

            if (!_mainScript.Config.DisableMenu)
            {
                Menu = new HandlingEditorMenu(this);
                Menu.ResetPropertiesEvent += (sender, id) => OnMenuCommandInvoked(id);
                Menu.FloatPropertyChangedEvent += OnMenuFloatPropertyChanged;
                Menu.IntPropertyChangedEvent += OnMenuIntPropertyChanged;
                Menu.Vector3PropertyChangedEvent += OnMenuVector3PropertyChanged;
            }

            Tick += UpdateWorldVehiclesTask;

            mainScript.PlayerVehicleHandleChanged += (sender, handle) => PlayerVehicleChanged(handle);
            PlayerVehicleChanged(_mainScript.PlayerVehicleHandle);

            HandlingDataChanged += (sender, args) => OnHandlingDataChanged();
        }

        private void PlayerVehicleChanged(int vehicle)
        {
            if (vehicle == _playerVehicleHandle)
                return;

            _playerVehicleHandle = vehicle;

            if (HandlingData != null)
                HandlingData.PropertyChanged -= OnHandlingDataPropertyChanged;

            if (_playerVehicleHandle == -1)
            {
                HandlingData = null;
                return;
            }

            HandlingData = GetHandlingDataFromEntity(vehicle);
        }

        private HandlingData GetHandlingDataFromEntity(int vehicle)
        {
            Dictionary<string, object> defaultValues = new Dictionary<string, object>();
            Dictionary<string, object> currentValues = new Dictionary<string, object>();

            foreach (var item in HandlingInfo.Fields)
            {
                string fieldName = item.Key;
                string className = item.Value.ClassName;
                Type fieldType = item.Value.Type;
                string defDecorName = $"{fieldName}_def";

                if (fieldType == HandlingFieldTypes.FloatType)
                {
                    var defaultValue = DecorExistOn(vehicle, defDecorName) ? DecorGetFloat(vehicle, defDecorName) : GetVehicleHandlingFloat(vehicle, className, fieldName);
                    defaultValues[fieldName] = defaultValue;
                    currentValues[fieldName] = DecorExistOn(vehicle, fieldName) ? DecorGetFloat(vehicle, fieldName) : defaultValue;
                }
                else if (fieldType == HandlingFieldTypes.IntType)
                {
                    var defaultValue = DecorExistOn(vehicle, defDecorName) ? DecorGetInt(vehicle, defDecorName) : GetVehicleHandlingInt(vehicle, className, fieldName);
                    defaultValues[fieldName] = defaultValue;
                    currentValues[fieldName] = DecorExistOn(vehicle, fieldName) ? DecorGetFloat(vehicle, fieldName) : defaultValue;
                }
                else if (fieldType == HandlingFieldTypes.Vector3Type)
                {
                    Vector3 vec = GetVehicleHandlingVector(vehicle, className, fieldName);

                    string decorX = $"{fieldName}.x";
                    string decorY = $"{fieldName}.y";
                    string decorZ = $"{fieldName}.z";

                    string defDecorNameX = $"{decorX}_def";
                    string defDecorNameY = $"{decorY}_def";
                    string defDecorNameZ = $"{decorZ}_def";

                    if (DecorExistOn(vehicle, defDecorNameX))
                        vec.X = DecorGetFloat(vehicle, defDecorNameX);
                    if (DecorExistOn(vehicle, defDecorNameY))
                        vec.Y = DecorGetFloat(vehicle, defDecorNameY);
                    if (DecorExistOn(vehicle, defDecorNameZ))
                        vec.Z = DecorGetFloat(vehicle, defDecorNameZ);

                    defaultValues[fieldName] = vec;

                    if (DecorExistOn(vehicle, decorX))
                        vec.X = DecorGetFloat(vehicle, decorX);
                    if (DecorExistOn(vehicle, decorY))
                        vec.Y = DecorGetFloat(vehicle, decorY);
                    if (DecorExistOn(vehicle, decorZ))
                        vec.Z = DecorGetFloat(vehicle, decorZ);

                    currentValues[fieldName] = vec;
                }
            }

            return new HandlingData(currentValues, defaultValues);
        }

        private async void OnHandlingDataPropertyChanged(string fieldName, object value)
        {
            if (!DataIsValid)
                return;

            switch (fieldName)
            {
                case nameof(HandlingData.Reset):
                    RemoveDecoratorsFromVehicle(_playerVehicleHandle);
                    UpdateVehicleUsingHandlingData(_playerVehicleHandle, HandlingData);
                    await Delay(50);
                    HandlingDataChanged?.Invoke(this, EventArgs.Empty);
                    break;

                default:
                    UpdateVehicleUsingHandlingDataField(_playerVehicleHandle, HandlingData, fieldName);
                    UpdateVehicleDecoratorUsingHandlingData(_playerVehicleHandle, HandlingData, fieldName);
                    break;
            }
        }

        private void OnHandlingDataChanged()
        {
            if (HandlingData != null)
                HandlingData.PropertyChanged += OnHandlingDataPropertyChanged;
        }

        private void UpdateVehicleUsingHandlingData(int vehicle, HandlingData handlingData)
        {
            if (!DoesEntityExist(vehicle))
                return;

            foreach (var fieldName in handlingData.Fields)
            {
                UpdateVehicleUsingHandlingDataField(vehicle, handlingData, fieldName.Key);
            }
        }

        private void UpdateVehicleDecoratorUsingHandlingData(int vehicle, HandlingData handlingData, string fieldName)
        {
            if (!DoesEntityExist(vehicle))
                return;

            if (!HandlingInfo.Fields.TryGetValue(fieldName, out HandlingFieldInfo handlingFieldInfo))
            {
                Debug.WriteLine($"{nameof(HandlingEditorScript)}: HandlingInfo doesn't contain the field {fieldName}");
                return;
            }

            Type fieldType = handlingFieldInfo.Type;
            object fieldValue = handlingData.GetFieldValue(fieldName);
            object defaultValue = handlingData.GetDefaultFieldValue(fieldName);

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

        private void UpdateVehicleHandlingDecorator(int vehicle, string fieldName, Vector3 fieldValue, Vector3 defaultValue)
        {
            string decorX = $"{fieldName}.x";
            string decorY = $"{fieldName}.y";
            string decorZ = $"{fieldName}.z";
            string defDecorNameX = $"{decorX}_def";
            string defDecorNameY = $"{decorY}_def";
            string defDecorNameZ = $"{decorZ}_def";

            Utilities.UpdateDecorator(vehicle, decorX, fieldValue.X, defaultValue.X);
            Utilities.UpdateDecorator(vehicle, defDecorNameX, defaultValue.X, fieldValue.X);

            Utilities.UpdateDecorator(vehicle, decorY, fieldValue.Y, defaultValue.Y);
            Utilities.UpdateDecorator(vehicle, defDecorNameY, defaultValue.Y, fieldValue.Y);

            Utilities.UpdateDecorator(vehicle, decorZ, fieldValue.Z, defaultValue.Z);
            Utilities.UpdateDecorator(vehicle, defDecorNameZ, defaultValue.Z, fieldValue.Z);
        }

        private void UpdateVehicleHandlingDecorator(int vehicle, string fieldName, int fieldValue, int defaultValue)
        {
            string defDecorName = $"{fieldName}_def";
            Utilities.UpdateDecorator(vehicle, fieldName, fieldValue, defaultValue);
            Utilities.UpdateDecorator(vehicle, defDecorName, defaultValue, fieldValue);
        }

        private void UpdateVehicleHandlingDecorator(int vehicle, string fieldName, float fieldValue, float defaultValue)
        {
            string defDecorName = $"{fieldName}_def";
            Utilities.UpdateDecorator(vehicle, fieldName, fieldValue, defaultValue);
            Utilities.UpdateDecorator(vehicle, defDecorName, defaultValue, fieldValue);
        }

        private void UpdateVehicleUsingHandlingDataField(int vehicle, HandlingData handlingData, string fieldName)
        {
            if (!handlingData.Fields.TryGetValue(fieldName, out dynamic fieldValue))
            {
                Debug.WriteLine($"{nameof(HandlingEditorScript)}: HandlingData doesn't contain the field {fieldName}");
                return;
            }

            if (!HandlingInfo.Fields.TryGetValue(fieldName, out HandlingFieldInfo handlingFieldInfo))
            {
                Debug.WriteLine($"{nameof(HandlingEditorScript)}: HandlingInfo doesn't contain the field {fieldName}");
                return;
            }

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

        private async Task UpdateWorldVehiclesTask()
        {
            long currentTime = (GetGameTimer() - _lastTime);

            if (currentTime > Config.Timer)
            {
                foreach (int entity in _mainScript.GetCloseVehicleHandles())
                {
                    if (entity == _playerVehicleHandle)
                        continue;

                    UpdateVehicleUsingDecorators(entity);
                }

                _lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
        }

        private void UpdateVehicleUsingDecorators(int vehicle)
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

        private void UpdateVehicleHandlingField(int vehicle, string className, string fieldName, int fieldValue)
        {
            var value = GetVehicleHandlingInt(vehicle, className, fieldName);
            if (value != fieldValue)
            {
                SetVehicleHandlingInt(vehicle, className, fieldName, fieldValue);
                
                if(Config.Debug)
                    Debug.WriteLine($"{nameof(HandlingEditorScript)}: Handling field {fieldName} updated from {value} to {fieldValue} for entity {vehicle}");
            }
        }

        private void UpdateVehicleHandlingField(int vehicle, string className, string fieldName, float fieldValue)
        {
            var value = GetVehicleHandlingFloat(vehicle, className, fieldName);
            if (!MathUtil.WithinEpsilon(value, fieldValue, Utilities.Epsilon))
            {
                SetVehicleHandlingFloat(vehicle, className, fieldName, fieldValue);
                
                if (Config.Debug)
                    Debug.WriteLine($"{nameof(HandlingEditorScript)}: Handling field {fieldName} updated from {value} to {fieldValue} for entity {vehicle}");
            }
        }

        private void UpdateVehicleHandlingField(int vehicle, string className, string fieldName, Vector3 fieldValue)
        {
            var value = GetVehicleHandlingVector(vehicle, className, fieldName);
            if (!value.Equals(fieldValue))
            {
                SetVehicleHandlingVector(vehicle, className, fieldName, fieldValue);

                if (Config.Debug)
                    Debug.WriteLine($"{nameof(HandlingEditorScript)}: Handling field {fieldName} updated from {value} to {fieldValue} for entity {vehicle}");
            }
        }

        private void RemoveDecoratorsFromVehicle(int vehicle)
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

            if(Config.Debug)
                Console.WriteLine($"{nameof(HandlingEditorScript)}: Removed all decorators on vehicle {vehicle}");
        }

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

        private void OnMenuCommandInvoked(string commandID)
        {
            switch (commandID)
            {
                case ResetID:
                    if (!DataIsValid)
                        return;

                    HandlingData.Reset();
                    break;
            }
        }

        private void OnMenuFloatPropertyChanged(string fieldName, float value)
        {
            if (!HandlingInfo.Fields.TryGetValue(fieldName, out HandlingFieldInfo fieldInfo))
                return;

            HandlingData.SetFieldValue(fieldName, value);
        }

        private void OnMenuIntPropertyChanged(string fieldName, int value)
        {
            if (!HandlingInfo.Fields.TryGetValue(fieldName, out HandlingFieldInfo fieldInfo))
                return;

            HandlingData.SetFieldValue(fieldName, value);
        }

        private void OnMenuVector3PropertyChanged(string fieldName, float value, string componentName)
        {
            if (!HandlingInfo.Fields.TryGetValue(fieldName, out HandlingFieldInfo fieldInfo))
                return;

            Vector3 fieldValue = (Vector3)HandlingData.GetFieldValue(fieldName);

            if (componentName.EndsWith(".x"))
                fieldValue.X = value;
            else if (componentName.EndsWith(".y"))
                fieldValue.Y = value;
            else if (componentName.EndsWith(".z"))
                fieldValue.Z = value;

            HandlingData.SetFieldValue(fieldName, fieldValue);
        }
    }
}