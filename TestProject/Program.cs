using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Icepack;

namespace TestProject
{

    class Program
    {
        [JsonObject]
        [SerializableObject]
        private class ClassB
        {
            private bool boolField;

            public ClassB()
            {
                boolField = false;
            }

            public bool BoolField
            {
                get { return boolField; }
                set { boolField = value; }
            }
        }

        [JsonObject]
        [SerializableObject]
        private class ClassA
        {
            private int intField;

            private string stringField;

            [JsonProperty(IsReference = true)]
            private ClassB classBField;

            public ClassA()
            {
                intField = 123;
                stringField = "asdf";
                classBField = new ClassB();
            }

            public int IntField
            {
                get { return intField; }
                set { intField = value; }
            }

            public string StringField
            {
                get { return stringField; }
                set { stringField = value; }
            }

            public ClassB ClassBField
            {
                get { return classBField; }
                set { classBField = value; }
            }
        }

        static void Main(string[] args)
        {
            List<ClassA> list = new List<ClassA>();
            for (int i = 0; i < 1000000; i++)
                list.Add(new ClassA());

            DateTime startTime, endTime;

            {
                Console.WriteLine("__Icepack__");

                Serializer serializer = new Serializer();
                serializer.RegisterType(typeof(List<ClassA>));
                serializer.RegisterType(typeof(bool[]));

                startTime = DateTime.Now;
                string str = serializer.Serialize(list);
                endTime = DateTime.Now;

                Console.WriteLine($"Serialize time: {endTime - startTime}");
                Console.WriteLine($"Serialize size: {str.Length}");

                startTime = DateTime.Now;
                serializer.Deserialize<List<ClassA>>(str);
                endTime = DateTime.Now;

                Console.WriteLine($"Deserialize time: {endTime - startTime}");
            }

            //{
            //    Console.WriteLine("__JSON.NET__");

            //    JsonSerializerSettings settings = new JsonSerializerSettings();
            //    settings.TypeNameHandling = TypeNameHandling.Objects | TypeNameHandling.Arrays;

            //    startTime = DateTime.Now;
            //    string str = JsonConvert.SerializeObject(list, settings);
            //    endTime = DateTime.Now;

            //    Console.WriteLine($"Serialize time: {endTime - startTime}");
            //    Console.WriteLine($"Serialize size: {str.Length}");

            //    startTime = DateTime.Now;
            //    JsonConvert.DeserializeObject<List<ClassA>>(str);
            //    endTime = DateTime.Now;

            //    Console.WriteLine($"Deserialize time: {endTime - startTime}");
            //}
        }
    }
}
