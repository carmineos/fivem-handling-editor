using CitizenFX.Core;
using HandlingEditor.Client.Scripts;
using MenuAPI;
using System;
using System.Collections.Generic;
using static HandlingEditor.Client.UI.MenuUtilities;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core.UI;

namespace HandlingEditor.Client.UI
{
    internal class HandlingEditorMenu : Menu
    {
        private readonly HandlingEditorScript _script;

        private MenuItem ResetItem { get; set; }

        internal event FloatPropertyChanged FloatPropertyChangedEvent;
        internal event Vector3PropertyChanged Vector3PropertyChangedEvent;
        internal event IntPropertyChanged IntPropertyChangedEvent;
        internal event EventHandler<string> ResetPropertiesEvent;

        internal HandlingEditorMenu(HandlingEditorScript script, string name = Globals.ScriptName, string subtitle = "Handling Editor Menu") : base(name, subtitle)
        {
            _script = script;

            _script.HandlingDataChanged += new EventHandler((sender, args) => Update());

            Update();

            AddTextEntry("HANDLING_EDITOR_ENTER_VALUE", "Enter a value (without spaces)");

            OnDynamicListItemCurrentItemChange += DynamicListItemCurrentItemChange;
            OnItemSelect += ItemSelect;
            OnDynamicListItemSelect += DynamicListItemSelect;
        }

        private async void DynamicListItemSelect(Menu menu, MenuDynamicListItem dynamicListItem, string currentItem)
        {
            if (_script.Config.Debug)
                Debug.WriteLine($"{nameof(HandlingEditorMenu)}: {dynamicListItem.Text} MenuDynamicListItem selected");

            // TODO: Move checks logic in script
            var fieldInfo = dynamicListItem.ItemData as HandlingFieldInfo;

            string text = await _script.GetValueFromUser("HANDLING_EDITOR_ENTER_VALUE", dynamicListItem.CurrentItem);

            if (fieldInfo.Type == HandlingFieldTypes.FloatType)
            {
                var min = (fieldInfo as HandlingFieldInfo<float>).Min;
                var max = (fieldInfo as HandlingFieldInfo<float>).Max;

                if (!float.TryParse(text, out float newFloatvalue))
                {
                    Screen.ShowNotification($"~r~ERROR~w~ Invalid value for ~b~{fieldInfo.Name}~w~");
                    return;
                }

                if (newFloatvalue >= min && newFloatvalue <= max)
                {
                    dynamicListItem.CurrentItem = newFloatvalue.ToString("F3");
                    FloatPropertyChangedEvent?.Invoke(fieldInfo.Name, newFloatvalue);
                }
                else
                    Screen.ShowNotification($"~r~ERROR~w~ Value out of allowed limits for ~b~{fieldInfo.Name}~w~ [Min:{min}, Max:{max}]");
            }
            else if (fieldInfo.Type == HandlingFieldTypes.IntType)
            {
                var min = (fieldInfo as HandlingFieldInfo<int>).Min;
                var max = (fieldInfo as HandlingFieldInfo<int>).Max;

                if (!int.TryParse(text, out int newIntvalue))
                {
                    Screen.ShowNotification($"~r~ERROR~w~ Invalid value for ~b~{fieldInfo.Name}~w~");
                    return;
                }

                if (newIntvalue >= min && newIntvalue <= max)
                {
                    dynamicListItem.CurrentItem = newIntvalue.ToString();
                    IntPropertyChangedEvent?.Invoke(fieldInfo.Name, newIntvalue);
                }
                else
                    Screen.ShowNotification($"~r~ERROR~w~ Value out of allowed limits for ~b~{fieldInfo.Name}~w~ [Min:{min}, Max:{max}]");
            }
            else if (fieldInfo.Type == HandlingFieldTypes.Vector3Type)
            {
                var min = (fieldInfo as HandlingFieldInfo<Vector3>).Min;
                var max = (fieldInfo as HandlingFieldInfo<Vector3>).Max;

                if (!float.TryParse(text, out float newfloatValue))
                {
                    Screen.ShowNotification($"~r~ERROR~w~ Invalid value for ~b~{dynamicListItem.Text}~w~");
                    return;
                }

                if (dynamicListItem.Text.EndsWith(".x"))
                {
                    if (newfloatValue >= min.X && newfloatValue <= max.X)
                    {
                        dynamicListItem.CurrentItem = newfloatValue.ToString("F3");
                        Vector3PropertyChangedEvent?.Invoke(fieldInfo.Name, newfloatValue, dynamicListItem.Text);
                    }
                    else
                        Screen.ShowNotification($"~r~ERROR~w~ Value out of allowed limits for ~b~{dynamicListItem.Text}~w~ [Min:{min.X}, Max:{max.X}]");

                }
                else if (dynamicListItem.Text.EndsWith(".y"))
                {
                    if (newfloatValue >= min.Y && newfloatValue <= max.Y)
                    {
                        dynamicListItem.CurrentItem = newfloatValue.ToString("F3");
                        Vector3PropertyChangedEvent?.Invoke(fieldInfo.Name, newfloatValue, dynamicListItem.Text);
                    }
                    else
                        Screen.ShowNotification($"~r~ERROR~w~ Value out of allowed limits for ~b~{dynamicListItem.Text}~w~ [Min:{min.Y}, Max:{max.Y}]");
                }
                else if (dynamicListItem.Text.EndsWith(".z"))
                {
                    if (newfloatValue >= min.Z && newfloatValue <= max.Z)
                    {
                        dynamicListItem.CurrentItem = newfloatValue.ToString("F3");
                        Vector3PropertyChangedEvent?.Invoke(fieldInfo.Name, newfloatValue, dynamicListItem.Text);
                    }
                    else
                        Screen.ShowNotification($"~r~ERROR~w~ Value out of allowed limits for ~b~{dynamicListItem.Text}~w~ [Min:{min.Z}, Max:{max.Z}]");
                }
            }
        }

        private void ItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
        {
            if (_script.Config.Debug)
                Debug.WriteLine($"{nameof(HandlingEditorMenu)}: {menuItem.Text} MenuItem selected");

            if (menuItem == ResetItem)
            {
                ResetPropertiesEvent?.Invoke(this, menuItem.ItemData as string);
                return;
            }
        }

        private void DynamicListItemCurrentItemChange(Menu menu, MenuDynamicListItem dynamicListItem, string oldValue, string newValue)
        {
            // TODO: Does it need to check if newvalue != oldvalue?
            if (oldValue == newValue)
                return;

            if(_script.Config.Debug)
                Debug.WriteLine($"{nameof(HandlingEditorMenu)}: {dynamicListItem.Text} changed from {oldValue} to {newValue}");

            // TODO: Maybe just use string as argument and move this logic in the script
            var fieldInfo = dynamicListItem.ItemData as HandlingFieldInfo;

            if(fieldInfo.Type == HandlingFieldTypes.FloatType)
            {
                if (float.TryParse(newValue, out float newfloatValue))
                    FloatPropertyChangedEvent?.Invoke(fieldInfo.Name, newfloatValue);
            }
            else if (fieldInfo.Type == HandlingFieldTypes.IntType)
            {
                if (int.TryParse(newValue, out int newfloatValue))
                    IntPropertyChangedEvent?.Invoke(fieldInfo.Name, newfloatValue);
            }
            else if (fieldInfo.Type == HandlingFieldTypes.Vector3Type)
            {
                if (float.TryParse(newValue, out float newfloatValue))
                    Vector3PropertyChangedEvent?.Invoke(fieldInfo.Name, newfloatValue, dynamicListItem.Text);
            }
        }



        internal void Update()
        {
            ClearMenuItems();

            if (!_script.DataIsValid)
                return;

            foreach (var item in HandlingInfo.Fields)
            {
                var fieldName = item.Key;
                var fieldInfo = item.Value;

                if (fieldInfo.Editable)
                {
                    Type fieldType = fieldInfo.Type;

                    MenuDynamicListItem menuItem;

                    if (fieldType == HandlingFieldTypes.FloatType)
                    {
                        var FloatFieldInfo = (HandlingFieldInfo<float>)fieldInfo;

                        menuItem = CreateDynamicFloatList(
                            fieldName,
                            (float)_script.HandlingData.GetFieldValue(fieldName),
                            FloatFieldInfo
                            );

                        AddMenuItem(menuItem);
                    }
                    else if (fieldType == HandlingFieldTypes.IntType)
                    {
                        var IntFieldInfo = (HandlingFieldInfo<int>)fieldInfo;

                        menuItem = CreateDynamicIntList(
                            fieldName,
                            (int)_script.HandlingData.GetFieldValue(fieldName),
                            IntFieldInfo
                            );

                        AddMenuItem(menuItem);
                    }
                    else if (fieldType == HandlingFieldTypes.Vector3Type)
                    {
                        var Vector3FieldInfo = (HandlingFieldInfo<Vector3>)fieldInfo;

                        var menuItems = CreateDynamicVector3List(
                            fieldName,
                            (Vector3)_script.HandlingData.GetFieldValue(fieldName),
                            Vector3FieldInfo
                            );

                        AddMenuItem(menuItems[0]);
                        AddMenuItem(menuItems[1]);
                        AddMenuItem(menuItems[2]);
                    }
                }
                else
                {
                    // TODO: Add ClientSettings
                    //if (_script.Config.ShowLockedFields)
                    //    AddLockedItem(m_editorMenu, item.Value);
                }
            }

            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = HandlingEditorScript.ResetID };
            AddMenuItem(ResetItem);
        }
    }
}