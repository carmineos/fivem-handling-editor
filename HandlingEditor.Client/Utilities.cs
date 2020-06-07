using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static CitizenFX.Core.Native.API;

namespace HandlingEditor.Client
{
    public static class Utilities
    {
        public const float Epsilon = 0.001f;

        public static List<string> GetKeyValuePairs(string prefix)
        {
            List<string> pairs = new List<string>();

            int handle = StartFindKvp(prefix);

            if (handle != -1)
            {
                string kvp;
                do
                {
                    kvp = FindKvp(handle);

                    if (kvp != null)
                        pairs.Add(kvp);
                }
                while (kvp != null);
                EndFindKvp(handle);
            }

            return pairs;
        }

        public static List<int> GetWorldVehicles()
        {
            List<int> handles = new List<int>();

            int entity = -1;
            int handle = FindFirstVehicle(ref entity);

            if (handle != -1)
            {
                do handles.Add(entity);
                while (FindNextVehicle(handle, ref entity));

                EndFindVehicle(handle);
            }

            return handles;
        }

        public static void UpdateDecorator(int vehicle, string name, float currentValue, float defaultValue)
        {
            // Decorator exists but needs to be updated
            if (DecorExistOn(vehicle, name))
            {
                float decorValue = DecorGetFloat(vehicle, name);
                if (!MathUtil.WithinEpsilon(currentValue, decorValue, Epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
#if DEBUG
                    Debug.WriteLine($"{Globals.ScriptName}: Updated decorator {name} from {decorValue} to {currentValue} on vehicle {vehicle}");
#endif
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (!MathUtil.WithinEpsilon(currentValue, defaultValue, Epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
#if DEBUG
                    Debug.WriteLine($"{Globals.ScriptName}: Added decorator {name} with value {currentValue} to vehicle {vehicle}");
#endif
                }
            }
        }

        public static void UpdateDecorator(int vehicle, string name, int currentValue, int defaultValue)
        {
            // Decorator exists but needs to be updated
            if (DecorExistOn(vehicle, name))
            {
                int decorValue = DecorGetInt(vehicle, name);
                if (!MathUtil.WithinEpsilon(currentValue, decorValue, Epsilon))
                {
                    DecorSetInt(vehicle, name, currentValue);
#if DEBUG
                    Debug.WriteLine($"{Globals.ScriptName}: Updated decorator {name} from {decorValue} to {currentValue} on vehicle {vehicle}");
#endif
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (!MathUtil.WithinEpsilon(currentValue, defaultValue, Epsilon))
                {
                    DecorSetInt(vehicle, name, currentValue);
#if DEBUG
                    Debug.WriteLine($"{Globals.ScriptName}: Added decorator {name} with value {currentValue} to vehicle {vehicle}");
#endif
                }
            }
        }

        public static void RemoveByteOrderMarks(ref string xml)
        {
            /*
            string bom = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (xml.StartsWith(bom))
                xml = xml.Remove(0, bom.Length);
            */

            // Workaround 
            if (!xml.StartsWith("<", StringComparison.Ordinal))
                xml = xml.Substring(xml.IndexOf("<"));
        }
    }
}
