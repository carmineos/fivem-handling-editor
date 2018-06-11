using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Xml;

namespace HandlingEditor
{
    /*
    public class FieldInfo<T>
    {
        public string Name;
        public Type Type;
        public bool Editable;
        public string Description;
        public T Min;
        public T Max;

        public FieldInfo(string name, string description, bool editable)
        {
            Name = name;
            Type = typeof(T);
            Description = description;
            Editable = editable;
            Min = default(T);
            Max = default(T);
        }

        public FieldInfo(string name, string description, bool editable, T min, T max)
        {
            Name = name;
            Type = typeof(T);
            Description = description;
            Editable = editable;
            Min = min;
            Max = max;
        }
    }
    

    public class CHandlingDataInfo
    {
        public Dictionary<string, FieldInfo<Type>> FieldsInfo;

        public CHandlingDataInfo()
        {
            FieldsInfo = new Dictionary<string, FieldInfo<Type>>();
        }

        public static Type GetFieldType(string name)
        {
            if (name.StartsWith("f")) return typeof(float);
            else if (name.StartsWith("n")) return typeof(int);
            else if (name.StartsWith("str")) return typeof(string);
            else if (name.StartsWith("vec")) return typeof(Vector3);
            else return typeof(string);
        }

        public void ParseXML(string xml)
        {
            XDocument doc = XDocument.Parse(xml);

            foreach (var item in doc.Element("CHandlingData").Elements())
            {
                string name = item.Name.ToString();
                Type type = GetFieldType(name);

                bool editable = bool.Parse(item.Attribute("Editable").Value);
                string description = item.Element("Description").Value;

                var minNode = item.Element("Min");
                var maxNode = item.Element("Max");

                if (type == typeof(float))
                {
                    float min = float.Parse(minNode.Attribute("value").Value);
                    float max = float.Parse(maxNode.Attribute("value").Value);

                    FieldInfo<float> fieldInfo = new FieldInfo<float>(name, description, editable, min, max);
                    FieldsInfo[name] = fieldInfo;
                }

                else if (type == typeof(int))
                {
                    int min = int.Parse(minNode.Attribute("value").Value);
                    int max = int.Parse(maxNode.Attribute("value").Value);

                    FieldInfo<int> fieldInfo = new FieldInfo<int>(name, description, editable, min, max);
                    FieldsInfo[name] = fieldInfo;
                }

                else if (type == typeof(Vector3))
                {
                    Vector3 min = new Vector3(
                        float.Parse(minNode.Attribute("x").Value),
                        float.Parse(minNode.Attribute("y").Value),
                        float.Parse(minNode.Attribute("z").Value));

                    Vector3 max = new Vector3(
                        float.Parse(maxNode.Attribute("x").Value),
                        float.Parse(maxNode.Attribute("y").Value),
                        float.Parse(maxNode.Attribute("z").Value));

                    FieldInfo<Vector3> fieldInfo = new FieldInfo<Vector3>(name, description, editable, min, max);
                    FieldsInfo[name] = fieldInfo;
                }

                else if (type == typeof(string))
                {
                    FieldInfo<string> fieldInfo = new FieldInfo<string>(name, description, editable);
                    FieldsInfo[name] = fieldInfo;
                }
            }
        }
    }
    */

    public static class FieldType
    {
        // TODO: Replace FieldType everywhere
        public static Type FloatType = typeof(float);
        public static Type IntType = typeof(int);
        public static Type Vector3Type = typeof(Vector3);
        public static Type StringType = typeof(string);
    }

    public abstract class FieldInfo
    {
        public string Name;
        public string ClassName;
        public Type Type;
        public bool Editable;
        public string Description;

        public FieldInfo(string name, string className, string description, bool editable)
        {
            Name = name;
            ClassName = className;
            Type = GetFieldType(name);
            Description = description;
            Editable = editable;
        }

        public static Type GetFieldType(string name)
        {
            if (name.StartsWith("f")) return typeof(float);
            else if (name.StartsWith("n")) return typeof(int);
            else if (name.StartsWith("str")) return typeof(string);
            else if (name.StartsWith("vec")) return typeof(Vector3);
            else return typeof(string);
        }
    }

    public class FloatFieldInfo : FieldInfo
    {
        public float Min;
        public float Max;

        public FloatFieldInfo(string name, string className, string description, bool editable, float min, float max) : base(name, className, description, editable)
        {
            Min = min;
            Max = max;
        }
    }

    public class IntFieldInfo : FieldInfo
    {
        public int Min;
        public int Max;

        public IntFieldInfo(string name, string className, string description, bool editable, int min, int max) : base(name, className, description, editable)
        {
            Min = min;
            Max = max;
        }
    }

    public class VectorFieldInfo : FieldInfo
    {
        public Vector3 Min;
        public Vector3 Max;

        public VectorFieldInfo(string name, string className, string description, bool editable, Vector3 min, Vector3 max) : base(name, className, description, editable)
        {
            Min = min;
            Max = max;
        }
    }

    public class StringFieldInfo : FieldInfo
    {
        public StringFieldInfo(string name, string className, string description, bool editable) : base(name, className, description, editable)
        {
        }
    }

    public class HandlingInfo
    {
        public Dictionary<string, FieldInfo> FieldsInfo;

        public HandlingInfo()
        {
            FieldsInfo = new Dictionary<string, FieldInfo>();
        }

        public void ParseXML(string xml)
        {
            /*string bom = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (xml.StartsWith(bom))
            {
                xml = xml.Remove(0, bom.Lenght);
            }*/

            xml = xml.Remove(0, 1);
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
                    Type type = FieldInfo.GetFieldType(name);

                    bool editable = bool.Parse(item.Attributes["Editable"].Value);
                    string description = item["Description"].InnerText;

                    var minNode = item["Min"];
                    var maxNode = item["Max"];

                    if (type == typeof(float))
                    {
                        float min = float.Parse(minNode.Attributes["value"].Value);
                        float max = float.Parse(maxNode.Attributes["value"].Value);

                        FloatFieldInfo fieldInfo = new FloatFieldInfo(name, className, description, editable, min, max);
                        FieldsInfo[name] = fieldInfo;
                    }

                    else if (type == typeof(int))
                    {
                        int min = int.Parse(minNode.Attributes["value"].Value);
                        int max = int.Parse(maxNode.Attributes["value"].Value);

                        IntFieldInfo fieldInfo = new IntFieldInfo(name, className, description, editable, min, max);
                        FieldsInfo[name] = fieldInfo;
                    }

                    else if (type == typeof(Vector3))
                    {
                        Vector3 min = new Vector3(
                            float.Parse(minNode.Attributes["x"].Value),
                            float.Parse(minNode.Attributes["y"].Value),
                            float.Parse(minNode.Attributes["z"].Value));

                        Vector3 max = new Vector3(
                            float.Parse(maxNode.Attributes["x"].Value),
                            float.Parse(maxNode.Attributes["y"].Value),
                            float.Parse(maxNode.Attributes["z"].Value));

                        VectorFieldInfo fieldInfo = new VectorFieldInfo(name, className, description, editable, min, max);
                        FieldsInfo[name] = fieldInfo;
                    }

                    else if (type == typeof(string))
                    {
                        StringFieldInfo fieldInfo = new StringFieldInfo(name, className, description, editable);
                        FieldsInfo[name] = fieldInfo;
                    }

                    else
                    {
                        StringFieldInfo fieldInfo = new StringFieldInfo(name, className, description, editable);
                        FieldsInfo[name] = fieldInfo;
                    }
                }

            }
        }

        /** public void ParseXMLLinq(string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            
            foreach (var item in doc.Element("CHandlingData").Elements())
            {
                string name = item.Name.ToString();
                Type type = FieldInfo.GetFieldType(name);

                bool editable = bool.Parse(item.Attribute("Editable").Value);
                string description = item.Element("Description").Value;

                var minNode = item.Element("Min");
                var maxNode = item.Element("Max");

                if (type == typeof(float))
                {
                    float min = float.Parse(minNode.Attribute("value").Value);
                    float max = float.Parse(maxNode.Attribute("value").Value);

                    FloatFieldInfo fieldInfo = new FloatFieldInfo(name, description, editable, min, max);
                    FieldsInfo[name] = fieldInfo;
                }

                else if (type == typeof(int))
                {
                    int min = int.Parse(minNode.Attribute("value").Value);
                    int max = int.Parse(maxNode.Attribute("value").Value);

                    IntFieldInfo fieldInfo = new IntFieldInfo(name, description, editable, min, max);
                    FieldsInfo[name] = fieldInfo;
                }

                else if (type == typeof(Vector3))
                {
                    Vector3 min = new Vector3(
                        float.Parse(minNode.Attribute("x").Value),
                        float.Parse(minNode.Attribute("y").Value),
                        float.Parse(minNode.Attribute("z").Value));

                    Vector3 max = new Vector3(
                        float.Parse(maxNode.Attribute("x").Value),
                        float.Parse(maxNode.Attribute("y").Value),
                        float.Parse(maxNode.Attribute("z").Value));

                    VectorFieldInfo fieldInfo = new VectorFieldInfo(name, description, editable, min, max);
                    FieldsInfo[name] = fieldInfo;
                }

                else if (type == typeof(string))
                {
                    StringFieldInfo fieldInfo = new StringFieldInfo(name, description, editable);
                    FieldsInfo[name] = fieldInfo;
                }

                else
                {
                    StringFieldInfo fieldInfo = new StringFieldInfo(name, description, editable);
                    FieldsInfo[name] = fieldInfo;
                }
            }
        }
        */
    }
}
