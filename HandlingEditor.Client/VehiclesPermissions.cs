using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace HandlingEditor.Client
{
    public static class VehiclesPermissions
    {
        public static Dictionary<int, bool> Classes = new Dictionary<int, bool>();
        public static Dictionary<uint, bool> Vehicles = new Dictionary<uint, bool>();

        public static void ParseXml(string xml)
        {
            Classes = new Dictionary<int, bool>();
            Vehicles = new Dictionary<uint, bool>();

            var document = new XmlDocument();
            document.LoadXml(xml);
            var rootNode = document["VehiclesPermissions"];

            var classNodes = rootNode["Classes"]?.ChildNodes;
            foreach (XmlNode item in classNodes)
            {
                if (item.NodeType == XmlNodeType.Comment)
                    continue;

                if (item.Name != "class")
                    continue;

                //var className = item.Attributes["name"].Value;
                int classId = int.Parse(item.Attributes["id"].Value);
                bool classIsAllowed = bool.Parse(item.Attributes["allowed"].Value);

                Classes[classId] = classIsAllowed;
            }

            var vehicleNodes = rootNode["Vehicles"]?.ChildNodes;
            foreach (XmlNode item in vehicleNodes)
            {
                if (item.NodeType == XmlNodeType.Comment)
                    continue;

                if (item.Name != "model")
                    continue;

                var modelName = item.Attributes["name"].Value;
                bool modelIsAllowed = bool.Parse(item.Attributes["allowed"].Value);

                uint modelHash = unchecked((uint)GetHashKey(modelName));

                // Not checking if the model is valid should allow the scripts to work even if the any model is loaded at runtime (eg. starting a resource of an addon vehicle)
                //if (IsModelValid(modelHash))
                    Vehicles[modelHash] = modelIsAllowed;
            }
        }

        public static bool IsVehicleAllowed(int handle)
        {
            uint modelHash = unchecked((uint)GetEntityModel(handle));

            int vehicleClass = GetVehicleClass(handle);

            bool isClassAllowed = Classes.ContainsKey(vehicleClass) ? Classes[vehicleClass] : false;
            bool isVehicleAllowed = Vehicles.ContainsKey(modelHash) ? Vehicles[modelHash] : isClassAllowed;

            return isVehicleAllowed; // vehicle permission overrides class one
        }
    }
}
