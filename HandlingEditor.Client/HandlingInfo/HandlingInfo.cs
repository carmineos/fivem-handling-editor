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

        public Dictionary<string, BaseFieldInfo> FieldsInfo;

        public HandlingInfo(ILogger log)
        {
            logger = log;
            FieldsInfo = new Dictionary<string, BaseFieldInfo>();
        }

        public  void ParseXml(string xml)
        {
            // Remove BOM if present
            Helpers.RemoveByteOrderMarks(ref xml);

            // Load the Xml document
            XmlDocument doc = new XmlDocument();  
            doc.LoadXml(xml);
            
            // Iterate all the nodes
            foreach (XmlNode classNode in doc.ChildNodes)
            {
                if (classNode.NodeType == XmlNodeType.Comment)
                    continue;

                // Root nodes are class names
                string className = classNode.Name;

                // Iterate fields of each class
                foreach (XmlNode item in classNode.ChildNodes)
                {
                    if (item.NodeType == XmlNodeType.Comment)
                        continue;

                    // Get the field name
                    string fieldName = item.Name;

                    // Get the field type
                    Type type = FieldType.GetFieldType(fieldName);

                    if(!bool.TryParse(item.Attributes["Editable"].Value, out bool editable))
                        logger.Log(LogLevel.Error, $"Unable to parse Editable attribute in {fieldName}.");

                    string description = item["Description"].InnerText;

                    var minNode = item["Min"];
                    var maxNode = item["Max"];

                    // If it's a float field
                    if (type == FieldType.FloatType)
                    {
                        if (!float.TryParse(minNode.Attributes["value"].Value, out float min))
                            logger.Log(LogLevel.Error, $"Unable to parse Min attribute in {fieldName}.");
                        if (!float.TryParse(maxNode.Attributes["value"].Value, out float max))
                            logger.Log(LogLevel.Error, $"Unable to parse Max attribute in {fieldName}.");

                        FieldInfo<float> fieldInfo = new FieldInfo<float>(fieldName, className, description, editable, min, max);
                        FieldsInfo[fieldName] = fieldInfo;
                    }

                    // If it's a int field
                    else if (type == FieldType.IntType)
                    {
                        if (!int.TryParse(minNode.Attributes["value"].Value, out int min))
                            logger.Log(LogLevel.Error, $"Unable to parse Min attribute in {fieldName}.");
                        if (!int.TryParse(maxNode.Attributes["value"].Value, out int max))
                            logger.Log(LogLevel.Error, $"Unable to parse Max attribute in {fieldName}.");

                        FieldInfo<int> fieldInfo = new FieldInfo<int>(fieldName, className, description, editable, min, max);
                        FieldsInfo[fieldName] = fieldInfo;
                    }

                    // If it's a Vector3 field
                    else if (type == FieldType.Vector3Type)
                    {
                        if(!float.TryParse(minNode.Attributes["x"].Value, out float minX)) logger.Log(LogLevel.Error, $"Unable to parse Min attribute in {fieldName}.");
                        if(!float.TryParse(minNode.Attributes["y"].Value, out float minY)) logger.Log(LogLevel.Error, $"Unable to parse Min attribute in {fieldName}.");
                        if(!float.TryParse(minNode.Attributes["z"].Value, out float minZ)) logger.Log(LogLevel.Error, $"Unable to parse Min attribute in {fieldName}.");
                        Vector3 min = new Vector3(minX, minY, minZ);

                        if (!float.TryParse(maxNode.Attributes["x"].Value, out float maxX)) logger.Log(LogLevel.Error, $"Unable to parse Max attribute in {fieldName}.");
                        if (!float.TryParse(maxNode.Attributes["y"].Value, out float maxY)) logger.Log(LogLevel.Error, $"Unable to parse Max attribute in {fieldName}.");
                        if (!float.TryParse(maxNode.Attributes["z"].Value, out float maxZ)) logger.Log(LogLevel.Error, $"Unable to parse Max attribute in {fieldName}.");
                        Vector3 max = new Vector3(maxX, maxY, maxZ);

                        FieldInfo<Vector3> fieldInfo = new FieldInfo<Vector3>(fieldName, className, description, editable, min, max);
                        FieldsInfo[fieldName] = fieldInfo;
                    }

                    else if (type == FieldType.StringType)
                    {
                        BaseFieldInfo fieldInfo = new BaseFieldInfo(fieldName, className, description, editable);
                        FieldsInfo[fieldName] = fieldInfo;
                    }

                    else
                    {
                        BaseFieldInfo fieldInfo = new BaseFieldInfo(fieldName, className, description, editable);
                        FieldsInfo[fieldName] = fieldInfo;
                    }
                }
            }
        }

        /*
        public static bool IsValueAllowed(string name, dynamic value)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // No field with such name exists
            if (!FieldsInfo.TryGetValue(name, out BaseFieldInfo baseFieldInfo))
                return false;

            if (baseFieldInfo is FieldInfo<int> && value is int)
            {
                FieldInfo<int> fieldInfo = (FieldInfo<int>)baseFieldInfo;
                return (value <= fieldInfo.Max && value >= fieldInfo.Min);
            }
            else if (baseFieldInfo is FieldInfo<float> && value is float)
            {
                FieldInfo<float> fieldInfo = (FieldInfo<float>)baseFieldInfo;
                return (value <= fieldInfo.Max && value >= fieldInfo.Min);
            }
            else if (baseFieldInfo is FieldInfo<Vector3> && value is Vector3)
            {
                FieldInfo<Vector3> fieldInfo = (FieldInfo<Vector3>)baseFieldInfo;

                if (name == $"{fieldInfo.Name}_x") return (value <= fieldInfo.Max.X && value >= fieldInfo.Min.X);
                else if (name == $"{fieldInfo.Name}_y") return (value <= fieldInfo.Max.Y && value >= fieldInfo.Min.Y);
                else if (name == $"{fieldInfo.Name}_z") return (value <= fieldInfo.Max.Z && value >= fieldInfo.Min.Z);
                else return false;
            }
            else
            {
                return false;
            }
        }*/
    }
}
