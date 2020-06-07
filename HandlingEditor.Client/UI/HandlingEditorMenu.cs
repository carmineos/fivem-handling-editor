using CitizenFX.Core;
using HandlingEditor.Client.Scripts;
using MenuAPI;
using System;
using System.Collections.Generic;
using static HandlingEditor.Client.UI.MenuUtilities;

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

            OnDynamicListItemCurrentItemChange += DynamicListItemCurrentItemChange;
            OnItemSelect += ItemSelect;
        }

        private void ItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
        {
            if (menuItem == ResetItem)
            {
                ResetPropertiesEvent?.Invoke(this, menuItem.ItemData as string);
                return;
            }

            // TODO: Get on screen value
            var fieldInfo = menuItem.ItemData as HandlingFieldInfo;
            if (fieldInfo.Type == HandlingFieldTypes.FloatType)
            {
                //
            }
            else if (fieldInfo.Type == HandlingFieldTypes.IntType)
            {
                //
            }
            else if (fieldInfo.Type == HandlingFieldTypes.Vector3Type)
            {
                //
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
                            (float)_script.HandlingData.Fields[fieldName],
                            FloatFieldInfo
                            );

                        AddMenuItem(menuItem);
                    }
                    else if (fieldType == HandlingFieldTypes.IntType)
                    {
                        var IntFieldInfo = (HandlingFieldInfo<int>)fieldInfo;

                        menuItem = CreateDynamicIntList(
                            fieldName,
                            (int)_script.HandlingData.Fields[fieldName],
                            IntFieldInfo
                            );

                        AddMenuItem(menuItem);
                    }
                    else if (fieldType == HandlingFieldTypes.Vector3Type)
                    {
                        var Vector3FieldInfo = (HandlingFieldInfo<Vector3>)fieldInfo;

                        var menuItems = CreateDynamicVector3List(
                            fieldName,
                            (Vector3)_script.HandlingData.Fields[fieldName],
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