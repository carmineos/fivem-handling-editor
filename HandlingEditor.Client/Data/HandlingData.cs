using System;
using System.Collections.Generic;
using System.Text;
using CitizenFX.Core;

namespace HandlingEditor.Client
{
    public class HandlingData : IEquatable<HandlingData>
    {
        public const float Epsilon = 0.001f;

        public event EventHandler<string> HandlingFieldEdited;

        public Dictionary<string, dynamic> DefaultFields { get; private set; }
        public Dictionary<string, dynamic> Fields { get; set; }

        public HandlingData()
        {
            DefaultFields = new Dictionary<string, dynamic>();
            Fields = new Dictionary<string, dynamic>();
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

                    if (fieldType == HandlingFieldTypes.IntType)
                    {
                        if (defaultValue != value)
                            return true;
                    }
                    else if(fieldType == HandlingFieldTypes.FloatType)
                    {
                        if (!MathUtil.WithinEpsilon(value, defaultValue, Epsilon))
                            return true;
                    }
                    else if (fieldType == HandlingFieldTypes.Vector3Type)
                    {
                        if (!((Vector3)value).Equals((Vector3)defaultValue))
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

                if (fieldType == HandlingFieldTypes.FloatType || fieldType == HandlingFieldTypes.IntType)
                    Fields[name] = value;

                else if (fieldType == HandlingFieldTypes.Vector3Type)
                {
                    Vector3 vec = (Vector3)value;
                    Fields[name] = new Vector3(vec.X, vec.Y, vec.Z);
                }
            }
        }

        public bool Equals(HandlingData other)
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

                if (fieldType == HandlingFieldTypes.IntType)
                {
                    if (value != otherValue)
                        return false;
                }
                else if(fieldType == HandlingFieldTypes.FloatType)
                {
                    if (!MathUtil.WithinEpsilon(value, otherValue, Epsilon))
                        return false;
                }
                else if (fieldType == HandlingFieldTypes.Vector3Type)
                {
                    if (!((Vector3)value).Equals((Vector3)otherValue))
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

        public void CopyFields(HandlingData other, bool onlySharedFields = true)
        {
            foreach (var item in other.Fields)
            {
                if (onlySharedFields)
                {
                    if (Fields.ContainsKey(item.Key))
                        Fields[item.Key] = item.Value;
                }
                else
                {
                    Fields[item.Key] = item.Value;
                }
            }
        }

    }
}
