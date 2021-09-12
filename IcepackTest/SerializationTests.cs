using NUnit.Framework;
using Icepack;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace IcepackTest
{
    public class SerializationTests
    {
        [JsonObject]
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
        private class ClassA
        {
            private int intField;

            private string stringField;

            private ClassB classBField;

            private bool[] boolArrayField;

            public ClassA()
            {
                intField = 123;
                stringField = "asdf";
                classBField = new ClassB();
                boolArrayField = new bool[] { true, true, false, true };
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

            public bool[] BoolArrayField
            {
                get { return boolArrayField; }
                set { boolArrayField = value; }
            }
        }

        [SetUp]
        public void Setup()
        {
        }

        /*
        [Test]
        public void SerializeFlatObject()
        {
            ClassA obj = new ClassA();

            Serializer serializer = new Serializer();
            string str = serializer.Serialize(obj);

            Console.WriteLine(str);
        }
        */

        [Test]
        public void Serialize10000Objects()
        {
            List<ClassA> list = new List<ClassA>();
            for (int i = 0; i < 1000000; i++)
                list.Add(new ClassA());

            {
                Serializer serializer = new Serializer();
                DateTime startTime = DateTime.Now;
                string str = serializer.Serialize(list);
                DateTime endTime = DateTime.Now;

                Console.WriteLine(endTime - startTime);
                Console.WriteLine(str.Length);
            }

            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.TypeNameHandling = TypeNameHandling.Objects | TypeNameHandling.Arrays;

                DateTime startTime = DateTime.Now;
                string str = JsonConvert.SerializeObject(list, settings);
                DateTime endTime = DateTime.Now;

                Console.WriteLine(endTime - startTime);
                Console.WriteLine(str.Length);
            }
        }
    }
}