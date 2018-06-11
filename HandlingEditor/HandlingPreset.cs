using System;
using System.Collections.Generic;
using System.Text;
using CitizenFX.Core;

namespace handling_editor
{
    public class HandlingPreset : IEquatable<HandlingPreset>
    {
        public Dictionary<string, dynamic> DefaultFields { get; private set; }
        public Dictionary<string, dynamic> Fields;

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
                foreach(var item in Fields.Keys)
                {
                    var value = Fields[item];
                    var defaultValue = DefaultFields[item];

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
            foreach (string name in DefaultFields.Keys)
                Fields[name] = DefaultFields[name];
        }

        public bool Equals(HandlingPreset other)
        {
            if (Fields.Count != other.Fields.Count)
                return false;

            foreach (var item in Fields.Keys)
            {
                if (!other.Fields.ContainsKey(item))
                    return false;

                var value = Fields[item];
                var otherValue = other.Fields[item];

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
            s.AppendLine("PRESET FIELDS:");
            foreach (var item in Fields)
            {
                s.AppendLine($"{item.Key}: {item.Value}({DefaultFields[item.Key]})");
            }

            return s.ToString();
        }
    }
}
