using System;
using System.Collections.Generic;
using System.IO;
using Icepack;

namespace DemoApp
{
    internal class Program
    {
        [SerializableObject]
        private struct DataType
        {
            public int Field1 { get; set; }
        }

        static void Main(string[] args)
        {
            MemoryStream stream = new MemoryStream();

            List<DataType> data = PrepareData();
            DateTime startTime;
            Serializer serializer = new Serializer();

            startTime = DateTime.Now;
            serializer.Serialize(data, stream);
            TimeSpan serializeTime = DateTime.Now - startTime;

            stream.Position = 0;

            startTime = DateTime.Now;
            serializer.Deserialize<List<DataType>>(stream);
            TimeSpan deserializeTime = DateTime.Now - startTime;

            Console.WriteLine("Serialize: {0:F}, Deserialize: {1:F}", serializeTime.TotalSeconds, deserializeTime.TotalSeconds);

            stream.Close();
        }

        private static List<DataType> PrepareData()
        {
            List<DataType> data = new List<DataType>();
            for (int i = 0; i < 10000000; i++)
            {
                DataType item = new DataType();
                item.Field1 = 123;

                data.Add(item);
            }

            return data;
        }
    }
}
