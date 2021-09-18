using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Parses an Icepack object tree. </summary>
    internal static class ObjectTreeParser
    {
        /// <summary> Parses an Icepack object tree. </summary>
        /// <param name="str"> The string to parse. </param>
        /// <returns> The object tree. </returns>
        public static List<object> Parse(string str)
        {
            try
            {
                int idx = 0;
                StringBuilder strBuilder = new StringBuilder();
                return ParseObjectArray(str, strBuilder, ref idx);
            }
            catch (Exception e)
            {
                throw new IcepackException($"Failed to parse string: {str}", e);
            }
        }

        private static List<object> ParseObjectArray(string str, StringBuilder strBuilder, ref int idx)
        {
            List<object> objects = new List<object>();

            while (true)
            {
                char c = str[idx++];
                if (c == '[')
                {
                    objects.Add(ParseObjectArray(str, strBuilder, ref idx));
                    if (idx == str.Length)
                    {
                        // To make sure this only happens once
                        idx++;
                        break;
                    }
                }
                else if (c == ']')
                    break;
                else if (c == '"')
                    objects.Add(ParseString(str, strBuilder, ref idx));
            }

            return objects;
        }

        private static string ParseString(string str, StringBuilder strBuilder, ref int idx)
        {
            strBuilder.Clear();

            while (true)
            {
                char c = str[idx++];
                if (c == '\\')
                {
                    c = str[idx++];
                    strBuilder.Append(c);
                }
                else if (c == '"')
                    break;
                else
                    strBuilder.Append(c);
            }

            return strBuilder.ToString();
        }
    }
}
