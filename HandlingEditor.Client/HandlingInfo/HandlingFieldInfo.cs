using System;

namespace HandlingEditor.Client
{
    public class HandlingFieldInfo
    {
        public string Name;
        public string ClassName;
        public Type Type;
        public bool Editable;
        public string Description;

        public HandlingFieldInfo(string name, string className, string description, bool editable)
        {
            Name = name;
            ClassName = className;
            Type = HandlingFieldTypes.GetHandlingFieldTypeByName(name);
            Description = description;
            Editable = editable;
        }
    }

    public class HandlingFieldInfo<T> : HandlingFieldInfo
    {
        public T Min;
        public T Max;

        public HandlingFieldInfo(string name, string className, string description, bool editable, T min, T max) : base(name, className, description, editable)
        {
            Min = min;
            Max = max;
        }
    }

}
