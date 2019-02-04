using System.Collections.Generic;
using System.Xml;
using static CitizenFX.Core.Native.API;

namespace HandlingEditor.Client
{
    public static class VehiclesPermissions
    {
        public static Dictionary<int, bool> Classes { get; set; } = new Dictionary<int, bool>();
        public static Dictionary<uint, bool> Vehicles { get; set; } = new Dictionary<uint, bool>();

        public static void ParseXml(string xml)
        {
            xml = Helpers.RemoveByteOrderMarks(xml);

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

                var idAttribute = item.Attributes["id"];
                var allowedAttribute = item.Attributes["allowed"];

                if (idAttribute == null || allowedAttribute == null)
                    continue;

                //var className = item.Attributes["name"].Value;
                int classId = int.Parse(idAttribute.Value);
                bool classIsAllowed = bool.Parse(allowedAttribute.Value);

                Classes[classId] = classIsAllowed;
            }

            var modelNodes = rootNode["Models"]?.ChildNodes;
            foreach (XmlNode item in modelNodes)
            {
                if (item.NodeType == XmlNodeType.Comment)
                    continue;

                if (item.Name != "model")
                    continue;

                var nameAttribute = item.Attributes["name"];
                var allowedAttribute = item.Attributes["allowed"];

                if (nameAttribute == null || allowedAttribute == null)
                    continue;

                var modelName = nameAttribute.Value;
                bool modelIsAllowed = bool.Parse(allowedAttribute.Value);

                uint modelHash = unchecked((uint)GetHashKey(modelName));

                // Not checking if the model is valid should allow the scripts to work even if the model is loaded at runtime (eg. starting a resource of an addon vehicle)
                //if (IsModelValid(modelHash))
                    Vehicles[modelHash] = modelIsAllowed;
            }
        }

        public static bool IsVehicleAllowed(int handle)
        {
            uint modelHash = unchecked((uint)GetEntityModel(handle));

            int vehicleClass = GetVehicleClass(handle);

            Classes.TryGetValue(vehicleClass, out bool isAllowed);
            Vehicles.TryGetValue(modelHash, out isAllowed);

            return isAllowed; // vehicle permission overrides class one
        }
    }
}
