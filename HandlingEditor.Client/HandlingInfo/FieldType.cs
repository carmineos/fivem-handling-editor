using CitizenFX.Core;
using System;

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

}
