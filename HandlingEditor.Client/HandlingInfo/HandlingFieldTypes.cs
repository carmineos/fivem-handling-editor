using CitizenFX.Core;
using System;

namespace HandlingEditor.Client
{
    public static class HandlingFieldTypes
    {
        public static Type FloatType = typeof(float);
        public static Type IntType = typeof(int);
        public static Type Vector3Type = typeof(Vector3);
        public static Type StringType = typeof(string);

        public static Type GetHandlingFieldTypeByName(string name)
        {
            if (name.StartsWith("f")) return FloatType;
            else if (name.StartsWith("n")) return IntType;
            else if (name.StartsWith("str")) return StringType;
            else if (name.StartsWith("vec")) return Vector3Type;
            else return StringType;
        }
    }

}
