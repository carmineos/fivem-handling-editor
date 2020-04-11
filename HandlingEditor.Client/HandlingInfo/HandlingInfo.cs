using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace HandlingEditor.Client
{
    public class HandlingInfo
    {
        private readonly ILogger logger;

        public Dictionary<string, HandlingFieldInfo> Fields;

        public HandlingInfo(ILogger log)
        {
            logger = log;
            Fields = new Dictionary<string, HandlingFieldInfo>();
        }

        public void ParseXml(string xml)
        {
            // Remove BOM if present
            Helpers.RemoveByteOrderMarks(ref xml);

            // Load the Xml document
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            // Iterate all the nodes
            foreach (XmlNode classNode in doc.ChildNodes)
            {
                if (classNode.NodeType != XmlNodeType.Element)
                    continue;

                // Root nodes are class names
                string className = classNode.Name;

                // Iterate fields of each class
                foreach (XmlNode item in classNode.ChildNodes)
                {
                    if (item.NodeType != XmlNodeType.Element)
                        continue;

                    // Get the field name
                    string fieldName = item.Name;

                    // Get the field type
                    Type type = HandlingFieldTypes.GetHandlingFieldTypeByName(fieldName);

                    if (!bool.TryParse(item.Attributes["Editable"].Value, out bool editable))
                        logger.Log(LogLevel.Error, $"Unable to parse Editable attribute in {fieldName}.");

                    string description = item["Description"].InnerText;

                    var minNode = item["Min"];
                    var maxNode = item["Max"];

                    // If it's a float field
                    if (type == HandlingFieldTypes.FloatType)
                    {
                        if (!float.TryParse(minNode.Attributes["value"].Value, out float min))
                            logger.Log(LogLevel.Error, $"Unable to parse Min attribute in {fieldName}.");
                        if (!float.TryParse(maxNode.Attributes["value"].Value, out float max))
                            logger.Log(LogLevel.Error, $"Unable to parse Max attribute in {fieldName}.");

                        HandlingFieldInfo<float> fieldInfo = new HandlingFieldInfo<float>(fieldName, className, description, editable, min, max);
                        Fields[fieldName] = fieldInfo;
                    }

                    // If it's a int field
                    else if (type == HandlingFieldTypes.IntType)
                    {
                        if (!int.TryParse(minNode.Attributes["value"].Value, out int min))
                            logger.Log(LogLevel.Error, $"Unable to parse Min attribute in {fieldName}.");
                        if (!int.TryParse(maxNode.Attributes["value"].Value, out int max))
                            logger.Log(LogLevel.Error, $"Unable to parse Max attribute in {fieldName}.");

                        HandlingFieldInfo<int> fieldInfo = new HandlingFieldInfo<int>(fieldName, className, description, editable, min, max);
                        Fields[fieldName] = fieldInfo;
                    }

                    // If it's a Vector3 field
                    else if (type == HandlingFieldTypes.Vector3Type)
                    {
                        if (!float.TryParse(minNode.Attributes["x"].Value, out float minX)) logger.Log(LogLevel.Error, $"Unable to parse Min attribute in {fieldName}.");
                        if (!float.TryParse(minNode.Attributes["y"].Value, out float minY)) logger.Log(LogLevel.Error, $"Unable to parse Min attribute in {fieldName}.");
                        if (!float.TryParse(minNode.Attributes["z"].Value, out float minZ)) logger.Log(LogLevel.Error, $"Unable to parse Min attribute in {fieldName}.");
                        Vector3 min = new Vector3(minX, minY, minZ);

                        if (!float.TryParse(maxNode.Attributes["x"].Value, out float maxX)) logger.Log(LogLevel.Error, $"Unable to parse Max attribute in {fieldName}.");
                        if (!float.TryParse(maxNode.Attributes["y"].Value, out float maxY)) logger.Log(LogLevel.Error, $"Unable to parse Max attribute in {fieldName}.");
                        if (!float.TryParse(maxNode.Attributes["z"].Value, out float maxZ)) logger.Log(LogLevel.Error, $"Unable to parse Max attribute in {fieldName}.");
                        Vector3 max = new Vector3(maxX, maxY, maxZ);

                        HandlingFieldInfo<Vector3> fieldInfo = new HandlingFieldInfo<Vector3>(fieldName, className, description, editable, min, max);
                        Fields[fieldName] = fieldInfo;
                    }

                    else if (type == HandlingFieldTypes.StringType)
                    {
                        HandlingFieldInfo fieldInfo = new HandlingFieldInfo(fieldName, className, description, editable);
                        Fields[fieldName] = fieldInfo;
                    }

                    else
                    {
                        HandlingFieldInfo fieldInfo = new HandlingFieldInfo(fieldName, className, description, editable);
                        Fields[fieldName] = fieldInfo;
                    }
                }
            }
        }


        public bool IsValueAllowed(string name, int value)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // No field with such name exists
            if (!Fields.TryGetValue(name, out HandlingFieldInfo baseFieldInfo))
                return false;

            if (baseFieldInfo is HandlingFieldInfo<int> fieldInfo)
                return value <= fieldInfo.Max && value >= fieldInfo.Min;

            return false;
        }

        public bool IsValueAllowed(string name, float value)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // No field with such name exists
            if (!Fields.TryGetValue(name, out HandlingFieldInfo baseFieldInfo))
                return false;

            if (baseFieldInfo is HandlingFieldInfo<float> fieldInfo)
                return value <= fieldInfo.Max && value >= fieldInfo.Min;

            return false;
        }

        public bool IsValueAllowed(string name, Vector3 value)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // No field with such name exists
            if (!Fields.TryGetValue(name, out HandlingFieldInfo baseFieldInfo))
                return false;

            if (baseFieldInfo is HandlingFieldInfo<Vector3> fieldInfo)
            {
                return 
                    value.X <= fieldInfo.Max.X && value.X >= fieldInfo.Min.X &&
                    value.Y <= fieldInfo.Max.Y && value.Y >= fieldInfo.Min.Y &&
                    value.Z <= fieldInfo.Max.Z && value.Z >= fieldInfo.Min.Z;
            }
            return false;
        }

        public bool IsComponentValueAllowed(string componentName, float value)
        {
            if (string.IsNullOrEmpty(componentName))
                return false;

            string name = GetFieldNameFromComponentFieldName(componentName);

            // No field with such name exists
            if (!Fields.TryGetValue(name, out HandlingFieldInfo baseFieldInfo))
                return false;

            if (!(baseFieldInfo is HandlingFieldInfo<Vector3> fieldInfo))
                return false;

            if (componentName.EndsWith(".x"))
                return value <= fieldInfo.Max.X && value >= fieldInfo.Min.X;
            else if (componentName.EndsWith(".y"))
                return value <= fieldInfo.Max.Y && value >= fieldInfo.Min.Y;
            else if (componentName.EndsWith(".z"))
                return value <= fieldInfo.Max.Z && value >= fieldInfo.Min.Z;

            return false;
        }

        public static string GetFieldNameFromComponentFieldName(string componentFieldName)
        {
            if (componentFieldName.EndsWith(".x") ||
                componentFieldName.EndsWith(".y") ||
                componentFieldName.EndsWith(".z"))
                return componentFieldName.Remove(componentFieldName.Length - 2, 2);
            else
                return componentFieldName;
        }
    }
}
