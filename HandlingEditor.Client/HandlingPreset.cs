using System;
using System.Collections.Generic;
using System.Text;
using CitizenFX.Core;

namespace HandlingEditor.Client
{
    public class HandlingPreset : IEquatable<HandlingPreset>
    {
        public static float FloatPrecision { get; private set; } = 0.001f;

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

                    if (fieldType == FieldType.FloatType || fieldType == FieldType.IntType)
                    {
                        if (defaultValue != value)
                            return true;
                    }
                    else if (fieldType == FieldType.Vector3Type)
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
                var value = item.Value;
                Type fieldType = value.GetType();

                if (fieldType == FieldType.FloatType || fieldType == FieldType.IntType)
                    Fields[name] = value;

                else if (fieldType == FieldType.Vector3Type)
                {
                    Vector3 vec = (Vector3)value;
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

                if (!other.Fields.TryGetValue(key, out dynamic otherValue))
                    return false;

                var value = item.Value;

                Type fieldType = value.GetType();

                if (fieldType == FieldType.IntType)
                {
                    if (value != otherValue)
                        return false;
                }
                else if(fieldType == FieldType.FloatType)
                {
                    if (Math.Abs(value - otherValue) > FloatPrecision)
                        return false;
                }
                else if (fieldType == FieldType.Vector3Type)
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
