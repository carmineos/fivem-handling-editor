using HandlingEditor.Client.Scripts;
using MenuAPI;
using System;
using System.Collections.Generic;

namespace HandlingEditor.Client.UI
{
    internal class HandlingEditorMenu : Menu
    {
        private readonly HandlingEditorScript _script;

        private Dictionary<string, MenuDynamicListItem> HandlingFieldDynamicListItems { get; set; }
        private MenuItem ResetItem { get; set; }

        internal HandlingEditorMenu(HandlingEditorScript script, string name = Globals.ScriptName, string subtitle = "Handling Editor Menu") : base(name, subtitle)
        {
            _script = script;

            HandlingFieldDynamicListItems = new Dictionary<string, MenuDynamicListItem>();

            _script.HandlingDataChanged += new EventHandler((sender, args) => Update());

            Update();
        }

        internal void Update()
        {
            ClearMenuItems();

            HandlingFieldDynamicListItems.Clear();

            if (!_script.DataIsValid)
                return;

            foreach (var item in HandlingInfo.Fields)
            {
                var fieldName = item.Key;
                var fieldInfo = item.Value;

                if (fieldInfo.Editable)
                {
                    Type fieldType = fieldInfo.Type;

                    if (fieldType == HandlingFieldTypes.FloatType)
                    {
                        //HandlingFieldDynamicListItems[fieldName] = MenuUtilities.CreateDynamicFloatList(
                        //    fieldName,
                        //    _script.HandlingData.DefaultFields[fieldName],
                        //    _script.HandlingData.Fields[fieldName],
                        //    0f,
                        //    fieldInfo
                        //    );
                    }
                    else if (fieldType == HandlingFieldTypes.IntType)
                    { }
                    else if (fieldType == HandlingFieldTypes.Vector3Type)
                    { }
                }
                else
                {
                    //if (_script.Config.ShowLockedFields)
                    //    AddLockedItem(m_editorMenu, item.Value);
                }
            }

            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = HandlingEditorScript.ResetID };
            AddMenuItem(ResetItem);
        }
    }
}