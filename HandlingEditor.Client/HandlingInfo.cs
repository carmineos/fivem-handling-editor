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

    public class HandlingInfo
    {
        public Dictionary<string, BaseFieldInfo> FieldsInfo;

        public HandlingInfo()
        {
            FieldsInfo = new Dictionary<string, BaseFieldInfo>();
        }

        public void ParseXml(string xml)
        {
            xml = Helpers.RemoveByteOrderMarks(xml);
            XmlDocument doc = new XmlDocument();
            
            doc.LoadXml(xml);
            
            foreach (XmlNode classNode in doc.ChildNodes)
            {
                if (classNode.NodeType == XmlNodeType.Comment)
                    continue;

                string className = classNode.Name;

                foreach (XmlNode item in classNode.ChildNodes)
                {

                    if (item.NodeType == XmlNodeType.Comment)
                        continue;

                    string name = item.Name;
                    Type type = FieldType.GetFieldType(name);

                    bool editable = bool.Parse(item.Attributes["Editable"].Value);
                    string description = item["Description"].InnerText;

                    var minNode = item["Min"];
                    var maxNode = item["Max"];

                    if (type == FieldType.FloatType)
                    {
                        float min = float.Parse(minNode.Attributes["value"].Value);
                        float max = float.Parse(maxNode.Attributes["value"].Value);

                        FieldInfo<float> fieldInfo = new FieldInfo<float>(name, className, description, editable, min, max);
                        FieldsInfo[name] = fieldInfo;
                    }

                    else if (type == FieldType.IntType)
                    {
                        int min = int.Parse(minNode.Attributes["value"].Value);
                        int max = int.Parse(maxNode.Attributes["value"].Value);

                        FieldInfo<int> fieldInfo = new FieldInfo<int>(name, className, description, editable, min, max);
                        FieldsInfo[name] = fieldInfo;
                    }

                    else if (type == FieldType.Vector3Type)
                    {
                        Vector3 min = new Vector3(
                            float.Parse(minNode.Attributes["x"].Value),
                            float.Parse(minNode.Attributes["y"].Value),
                            float.Parse(minNode.Attributes["z"].Value));

                        Vector3 max = new Vector3(
                            float.Parse(maxNode.Attributes["x"].Value),
                            float.Parse(maxNode.Attributes["y"].Value),
                            float.Parse(maxNode.Attributes["z"].Value));

                        FieldInfo<Vector3> fieldInfo = new FieldInfo<Vector3>(name, className, description, editable, min, max);
                        FieldsInfo[name] = fieldInfo;
                    }

                    else if (type == FieldType.StringType)
                    {
                        BaseFieldInfo fieldInfo = new BaseFieldInfo(name, className, description, editable);
                        FieldsInfo[name] = fieldInfo;
                    }

                    else
                    {
                        BaseFieldInfo fieldInfo = new BaseFieldInfo(name, className, description, editable);
                        FieldsInfo[name] = fieldInfo;
                    }
                }
            }
        }
    }
}
