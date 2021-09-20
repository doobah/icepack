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
            if (document == null || document.Length == 0 || document[0] != '[')
                throw new IcepackException($"Malformed Icepack document: {document}");

            try
            {
                int idx = 1;
                StringBuilder strBuilder = new StringBuilder();
                List<object> objTree = ParseObjectArray(document, strBuilder, 0, ref idx);
                if (idx != document.Length)
                    throw new IcepackException($"Reached end of document unexpectedly! Position: {idx}, Length: {document.Length}");
                return objTree;
            }
            catch (Exception e)
            {
                throw new IcepackException($"Failed to parse document: {document}", e);
            }
        }

        /// <summary> Recursively parses an object array from a document. </summary>
        /// <param name="document"> The document to parse </param>
        /// <param name="strBuilder"> The string builder used for this parsing operation, reused to avoid unnecessary allocations. </param>
        /// <param name="depth"> The depth of the current object array in the tree. </param>
        /// <param name="idx"> The current position in the document. </param>
        /// <returns> The object array. </returns>
        private static List<object> ParseObjectArray(string document, StringBuilder strBuilder, int depth, ref int idx)
        {
            List<object> objects = new List<object>();

            while (true)
            {
                char c = document[idx];
                if (c == '[')
                {
                    idx++;
                    objects.Add(ParseObjectArray(document, strBuilder, depth + 1, ref idx));
                }
                else if (c == ']')
                {
                    idx++;
                    break;
                }
                else if (c == ',')
                    idx++;
                else
                    objects.Add(ParseString(document, strBuilder, ref idx));
            }

            return objects;
        }

        private static string ParseString(string str, StringBuilder strBuilder, ref int idx)
        {
            strBuilder.Clear();

            while (true)
            {
                char c = str[idx];
                if (c == '\\')
                {
                    idx++;
                    c = str[idx];
                    idx++;
                    strBuilder.Append(c);
                }
                else if (c == ',' || c == ']')
                    break;
                else
                {
                    idx++;
                    strBuilder.Append(c);
                }
            }

            return strBuilder.ToString();
        }
    }
}
