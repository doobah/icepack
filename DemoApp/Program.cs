using System;
using System.Collections.Generic;
using System.IO;
using Icepack;

namespace DemoApp
{
    internal class Program
    {
        [SerializableObject]
        private class DataClass
        {
            public int Field1 { get; set; }
        }

        static void Main(string[] args)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Serializer serializer = new Serializer();
                serializer.Serialize(GetData(), stream);
                stream.Position = 0;
                serializer.Deserialize<List<DataClass>>(stream);
            }
        }

        private static object GetData()
        {
            List<DataClass> data = new List<DataClass>();
            for (int i = 0; i < 10000000; i++)
            {
                DataClass item = new DataClass();
                item.Field1 = 123;

                data.Add(item);
            }

            return data;
        }
    }
}
