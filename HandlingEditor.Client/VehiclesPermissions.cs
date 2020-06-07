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
            Utilities.RemoveByteOrderMarks(ref xml);

            Classes = new Dictionary<int, bool>();
            Vehicles = new Dictionary<uint, bool>();

            var document = new XmlDocument();
            document.LoadXml(xml);
            var rootNode = document["VehiclesPermissions"];

            var classNodes = rootNode["Classes"]?.ChildNodes;
            foreach (XmlNode item in classNodes)
            {
                if (item.NodeType != XmlNodeType.Element)
                    continue;

                if (item.Name != "class")
                    continue;

                var idAttribute = item.Attributes["id"];
                var allowedAttribute = item.Attributes["allowed"];

                if (idAttribute == null || allowedAttribute == null)
                    continue;

                //var className = item.Attributes["name"].Value;
                int.TryParse(idAttribute.Value, out int classId);
                bool.TryParse(allowedAttribute.Value, out bool classIsAllowed);

                Classes[classId] = classIsAllowed;
            }

            var modelNodes = rootNode["Models"]?.ChildNodes;
            foreach (XmlNode item in modelNodes)
            {
                if (item.NodeType != XmlNodeType.Element)
                    continue;

                if (item.Name != "model")
                    continue;

                var nameAttribute = item.Attributes["name"];
                var allowedAttribute = item.Attributes["allowed"];

                if (nameAttribute == null || allowedAttribute == null)
                    continue;

                var modelName = nameAttribute.Value;
                bool.TryParse(allowedAttribute.Value, out bool modelIsAllowed);

                uint modelHash = unchecked((uint)GetHashKey(modelName));

                // Not checking if the model is valid should allow the scripts to work even if the model is loaded at runtime (eg. starting a resource of an addon vehicle)
                //if (IsModelValid(modelHash))
                    Vehicles[modelHash] = modelIsAllowed;
            }
        }

        /// <summary>
        /// Returns true if the model is allowed
        /// </summary>
        /// <param name="handle">The handle of the vehicle entity</param>
        /// <returns></returns>
        public static bool IsVehicleAllowed(int handle)
        {
            // Get the model hash
            uint modelHash = unchecked((uint)GetEntityModel(handle));

            // Get the vehicle class
            int vehicleClass = GetVehicleClass(handle);

            // If a rule for the model is defined, then return its value
            if (Vehicles.TryGetValue(modelHash, out bool isModelAllowed))
                return isModelAllowed;

            // Otherwise check if a rule its class is defined, and return its value
            if (Classes.TryGetValue(vehicleClass, out bool isClassAllowed))
                return isClassAllowed;

            // No rule exists exists for this model or class
            return false;
        }
    }
    /*
    public class ClassPermission
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Allowed { get; set; }
    }

    public class ModelPermission
    {
        public string Name { get; set; }
        public bool Allowed { get; set; }
    }*/
}
