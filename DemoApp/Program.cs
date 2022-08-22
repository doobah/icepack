using System;
using System.Collections.Generic;
using System.IO;
using Icepack;

namespace DemoApp
{
    internal class Program
    {
        [SerializableType]
        private struct InnerDataType
        {
            public int Field1 { get; set; }
        }

        [SerializableType]
        private class DataType
        {
            public InnerDataType Field1 { get; set; }
            public InnerDataType Field2 { get; set; }
            public InnerDataType Field3 { get; set; }
            public InnerDataType Field4 { get; set; }
        }

        static void Main(string[] args)
        {
            MemoryStream stream = new MemoryStream();

            List<DataType> data = PrepareData();
            DateTime startTime;
            Serializer serializer = new Serializer(new SerializerSettings(preserveReferences: false));

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
                InnerDataType innerItem1 = new InnerDataType();
                innerItem1.Field1 = 123;
                InnerDataType innerItem2 = new InnerDataType();
                innerItem2.Field1 = 123;
                InnerDataType innerItem3 = new InnerDataType();
                innerItem3.Field1 = 123;
                InnerDataType innerItem4 = new InnerDataType();
                innerItem4.Field1 = 123;
                item.Field1 = innerItem1;
                item.Field2 = innerItem2;
                item.Field3 = innerItem3;
                item.Field4 = innerItem4;

                data.Add(item);
            }

            return data;
        }
    }
}
