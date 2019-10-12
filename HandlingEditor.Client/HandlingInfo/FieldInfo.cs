using System;

namespace HandlingEditor.Client
{
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

}
