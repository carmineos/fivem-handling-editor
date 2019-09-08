using System;

namespace HandlingEditor.Client
{
    public static class Helpers
    {
        public static void RemoveByteOrderMarks(ref string xml)
        {
            /*
            string bom = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (xml.StartsWith(bom))
                xml = xml.Remove(0, bom.Length);
            */

            // Workaround 
            if (!xml.StartsWith("<", StringComparison.Ordinal))
                xml = xml.Substring(xml.IndexOf("<"));
        }
    }
}
