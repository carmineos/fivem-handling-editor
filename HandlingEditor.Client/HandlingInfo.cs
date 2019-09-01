using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace HandlingEditor.Client
{
    public static class FieldType
    {
        public static Type FloatType = typeof(float);
        public static Type IntType = typeof(int);
        public static Type Vector3Type = typeof(Vector3);
        public static Type StringType = typeof(string);

        public static Type GetFieldType(string name)
        {
            if (name.StartsWith("f")) return FieldType.FloatType;
            else if (name.StartsWith("n")) return FieldType.IntType;
            else if (name.StartsWith("str")) return FieldType.StringType;
            else if (name.StartsWith("vec")) return FieldType.Vector3Type;
            else return FieldType.StringType;
        }
    }

    public class BaseFieldInfo
    {
        public string Name;
        public string ClassName;
        public Type Type;
        public bool Editable;
        public string Description;

        public BaseFieldInfo(string name, string className, string description, bool editable)
        {
            Name = name;
            ClassName = className;
            Type = FieldType.GetFieldType(name);
            Description = description;
            Editable = editable;
        } 
    }

    public class FieldInfo<T> : BaseFieldInfo
    {
        public T Min;
        public T Max;

        public FieldInfo(string name, string className, string description, bool editable, T min, T max) : base(name, className, description, editable)
        {
            Min = min;
            Max = max;
        }
    }

    public static class HandlingInfo
    {
        public static Dictionary<string, BaseFieldInfo> FieldsInfo = new Dictionary<string, BaseFieldInfo>();

        public static void ParseXml(string xml)
        {
            // Remove BOM if present
            xml = Helpers.RemoveByteOrderMarks(xml);

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
                        CitizenFX.Core.Debug.WriteLine($"Error parsing Editable attribute in {fieldName}.");

                    string description = item["Description"].InnerText;

                    var minNode = item["Min"];
                    var maxNode = item["Max"];

                    // If it's a float field
                    if (type == FieldType.FloatType)
                    {
                        if (!float.TryParse(minNode.Attributes["value"].Value, out float min))
                            CitizenFX.Core.Debug.WriteLine($"Error parsing Min attribute in {fieldName}.");
                        if (!float.TryParse(maxNode.Attributes["value"].Value, out float max))
                            CitizenFX.Core.Debug.WriteLine($"Error parsing Max attribute in {fieldName}.");

                        FieldInfo<float> fieldInfo = new FieldInfo<float>(fieldName, className, description, editable, min, max);
                        FieldsInfo[fieldName] = fieldInfo;
                    }

                    // If it's a int field
                    else if (type == FieldType.IntType)
                    {
                        if (!int.TryParse(minNode.Attributes["value"].Value, out int min))
                            CitizenFX.Core.Debug.WriteLine($"Error parsing Min attribute in {fieldName}.");
                        if (!int.TryParse(maxNode.Attributes["value"].Value, out int max))
                            CitizenFX.Core.Debug.WriteLine($"Error parsing Max attribute in {fieldName}.");

                        FieldInfo<int> fieldInfo = new FieldInfo<int>(fieldName, className, description, editable, min, max);
                        FieldsInfo[fieldName] = fieldInfo;
                    }

                    // If it's a Vector3 field
                    else if (type == FieldType.Vector3Type)
                    {
                        if(!float.TryParse(minNode.Attributes["x"].Value, out float minX)) CitizenFX.Core.Debug.WriteLine($"Error parsing Min attribute in {fieldName}.");
                        if(!float.TryParse(minNode.Attributes["y"].Value, out float minY)) CitizenFX.Core.Debug.WriteLine($"Error parsing Min attribute in {fieldName}.");
                        if(!float.TryParse(minNode.Attributes["z"].Value, out float minZ)) CitizenFX.Core.Debug.WriteLine($"Error parsing Min attribute in {fieldName}.");
                        Vector3 min = new Vector3(minX, minY, minZ);

                        if (!float.TryParse(maxNode.Attributes["x"].Value, out float maxX)) CitizenFX.Core.Debug.WriteLine($"Error parsing Max attribute in {fieldName}.");
                        if (!float.TryParse(maxNode.Attributes["y"].Value, out float maxY)) CitizenFX.Core.Debug.WriteLine($"Error parsing Max attribute in {fieldName}.");
                        if (!float.TryParse(maxNode.Attributes["z"].Value, out float maxZ)) CitizenFX.Core.Debug.WriteLine($"Error parsing Max attribute in {fieldName}.");
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
