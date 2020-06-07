using CitizenFX.Core;
using CitizenFX.Core.UI;
using MenuAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandlingEditor.Client.UI
{
    internal static class MenuUtilities
    {
        internal delegate void FloatPropertyChanged(string id, float value);
        internal delegate void Vector3PropertyChanged(string id, float value, string componentName);
        internal delegate void IntPropertyChanged(string id, int value);
        internal delegate void BoolPropertyChanged(string id, bool value);

        internal static MenuDynamicListItem CreateDynamicFloatList(string name, float value, HandlingFieldInfo<float> fieldInfo, float step = 0.01f)
        {
            var callback = FloatChangeCallback(name, value, fieldInfo.Min, fieldInfo.Max, step);

            return new MenuDynamicListItem(name, value.ToString("F3"), callback) { ItemData = fieldInfo };
        }

        internal static MenuDynamicListItem CreateDynamicIntList(string name, int value, HandlingFieldInfo<int> fieldInfo, int step = 1)
        {
            var callback = IntChangeCallback(name, value, fieldInfo.Min, fieldInfo.Max, step);

            return new MenuDynamicListItem(name, value.ToString(), callback) { ItemData = fieldInfo };
        }

        internal static MenuDynamicListItem[] CreateDynamicVector3List(string name, Vector3 value, HandlingFieldInfo<Vector3> fieldInfo, float step = 0.01f)
        {
            var nameX = $"{name}.x";
            var callbackX = FloatChangeCallback(nameX, value.X, fieldInfo.Min.X, fieldInfo.Max.X, step);
            var itemX = new MenuDynamicListItem(nameX, value.X.ToString("F3"), callbackX) { ItemData = fieldInfo };

            var nameY = $"{name}.y";
            var callbackY = FloatChangeCallback(nameY, value.Y, fieldInfo.Min.Y, fieldInfo.Max.Y, step);
            var itemY = new MenuDynamicListItem(nameY, value.Y.ToString("F3"), callbackY) { ItemData = fieldInfo };

            var nameZ = $"{name}.z";
            var callbackZ = FloatChangeCallback(nameZ, value.Z, fieldInfo.Min.Z, fieldInfo.Max.Z, step);
            var itemZ = new MenuDynamicListItem(nameZ, value.Z.ToString("F3"), callbackZ) { ItemData = fieldInfo };

            return new MenuDynamicListItem[3] { itemX, itemY, itemZ };
        }

        internal static MenuDynamicListItem.ChangeItemCallback FloatChangeCallback(string name, float value, float minimum, float maximum, float step)
        {
            string callback(MenuDynamicListItem sender, bool left)
            {
                var min = minimum;
                var max = maximum;

                var newvalue = value;

                if (left)
                    newvalue -= step;
                else if (!left)
                    newvalue += step;
                else return value.ToString("F3");

                // Hotfix to trim the value to 3 digits
                newvalue = float.Parse((newvalue).ToString("F3"));

                if (newvalue < min)
                    Screen.ShowNotification($"~o~Warning~w~: Min ~b~{name}~w~ value allowed is {min}");
                else if (newvalue > max)
                    Screen.ShowNotification($"~o~Warning~w~: Max ~b~{name}~w~ value allowed is {max}");
                else
                {
                    value = newvalue;
                }
                return value.ToString("F3");
            };
            return callback;
        }

        internal static MenuDynamicListItem.ChangeItemCallback IntChangeCallback(string name, int value, int minimum, int maximum, int step)
        {
            string callback(MenuDynamicListItem sender, bool left)
            {
                var min = minimum;
                var max = maximum;

                var newvalue = value;

                if (left)
                    newvalue -= step;
                else if (!left)
                    newvalue += step;
                else return value.ToString();

                if (newvalue < min)
                    Screen.ShowNotification($"~o~Warning~w~: Min ~b~{name}~w~ value allowed is {min}");
                else if (newvalue > max)
                    Screen.ShowNotification($"~o~Warning~w~: Max ~b~{name}~w~ value allowed is {max}");
                else
                {
                    value = newvalue;
                }
                return value.ToString();
            };
            return callback;
        }
    }
}
