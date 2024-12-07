using System;
using System.Collections.Generic;
using System.IO;
using Icepack;

namespace DemoApp;

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

    static void Main()
    {
        MemoryStream stream = new();

        List<DataType> data = PrepareData();
        DateTime startTime;
        Serializer serializer = new(new SerializerSettings(preserveReferences: false));

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
        List<DataType> data = [];
        for (int i = 0; i < 10000000; i++)
        {
            DataType item = new();
            InnerDataType innerItem1 = new();
            innerItem1.Field1 = 123;
            InnerDataType innerItem2 = new();
            innerItem2.Field1 = 123;
            InnerDataType innerItem3 = new();
            innerItem3.Field1 = 123;
            InnerDataType innerItem4 = new();
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
