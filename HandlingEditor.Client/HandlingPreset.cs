using System;
using System.Collections.Generic;
using System.Text;
using CitizenFX.Core;

namespace HandlingEditor.Client
{
    public class HandlingPreset : IEquatable<HandlingPreset>
    {
        public Dictionary<string, dynamic> DefaultFields { get; private set; }
        public Dictionary<string, dynamic> Fields { get; set; }

        public HandlingPreset()
        {
            DefaultFields = new Dictionary<string, dynamic>();
            Fields = new Dictionary<string, dynamic>();
        }

        public HandlingPreset(Dictionary<string, dynamic> defaultFields, Dictionary<string, dynamic> fields)
        {
            Fields = fields;
            DefaultFields = defaultFields;
        }

        public bool IsEdited
        {
            get
            {
                foreach(var item in Fields)
                {
                    var value = item.Value;
                    var defaultValue = DefaultFields[item.Key];

                    Type fieldType = value.GetType();

                    if (fieldType == typeof(float) || fieldType == typeof(int))
                    {
                        if (defaultValue != value)
                            return true;
                    }
                    else if (fieldType == typeof(Vector3))
                    {
                        value = (Vector3)value;
                        defaultValue = (Vector3)defaultValue;

                        if (value.Equals(defaultValue))
                            return true;
                    }
                }
                return false;
            }
        }

        public void Reset()
        {
            foreach (var item in DefaultFields)
            {
                string name = item.Key;
                Type fieldType = DefaultFields[name].GetType();

                if (fieldType == typeof(float) || fieldType == typeof(int))
                {
                    Fields[name] = DefaultFields[name];
                }
                else if (fieldType == typeof(Vector3))
                {
                    Vector3 vec = (Vector3)DefaultFields[name];
                    Fields[name] = new Vector3(vec.X, vec.Y, vec.Z);
                }
            }
        }

        public bool Equals(HandlingPreset other)
        {
            if (Fields.Count != other.Fields.Count)
                return false;

            foreach (var item in Fields)
            {
                string key = item.Key;

                if (!other.Fields.ContainsKey(key))
                    return false;

                var value = item.Value;
                var otherValue = other.Fields[key];

                Type fieldType = value.GetType();

                if (fieldType == typeof(float) || fieldType == typeof(int))
                {
                    if (value != otherValue)
                        return false;
                }
                else if (fieldType == typeof(Vector3))
                {
                    value = (Vector3)value;
                    otherValue = (Vector3)otherValue;
                    if (!value.Equals(otherValue))
                        return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine("HandlingPreset Fields:");
            foreach (var item in Fields)
            {
                s.AppendLine($"{item.Key}: {item.Value}({DefaultFields[item.Key]})");
            }

            return s.ToString();
        }
    }
}
