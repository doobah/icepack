using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Parses an object tree. </summary>
    internal static class ObjectTreeParser
    {
        /// <summary> Parses a document as an object tree. </summary>
        /// <param name="document"> The document to parse. </param>
        /// <returns> The object tree. </returns>
        public static List<object> Parse(string document)
        {
            try
            {
                int idx = 0;
                StringBuilder strBuilder = new StringBuilder();
                return (List<object>)ParseObjectArray(document, strBuilder, ref idx)[0];
            }
            catch (Exception e)
            {
                throw new IcepackException($"Failed to parse string: {document}", e);
            }
        }

        /// <summary> Recursively parses an object array from a document. </summary>
        /// <param name="document"> The document to parse </param>
        /// <param name="strBuilder"> The string builder used for this parsing operation, reused to avoid unnecessary allocations. </param>
        /// <param name="idx"> The current position in the document. </param>
        /// <returns> The object array. </returns>
        private static List<object> ParseObjectArray(string document, StringBuilder strBuilder, ref int idx)
        {
            List<object> objects = new List<object>();

            while (true)
            {
                char c = document[idx++];
                if (c == '[')
                {
                    objects.Add(ParseObjectArray(document, strBuilder, ref idx));
                    if (idx == document.Length)
                    {
                        // To make sure this only happens once
                        idx++;
                        break;
                    }
                }
                else if (c == ']')
                    break;
                else if (c == '"')
                    objects.Add(ParseString(document, strBuilder, ref idx));
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
