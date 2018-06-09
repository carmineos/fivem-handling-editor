using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                foreach(string name in DefaultFields.Keys)
                {
                    if ((Fields[name] != DefaultFields[name]))
                        return true;
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

            foreach (string name in Fields.Keys)
            {
                if ((Fields[name] != other.Fields[name]))
                    return false;
            }
            return true;
        }
    }
}
