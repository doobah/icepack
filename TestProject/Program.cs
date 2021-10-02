using System;
using System.Collections;
using System.Collections.Generic;
using Icepack;
using System.IO;

namespace TestProject
{
    class Program
    {
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

        [SerializableObject]
        private class ClassA
        {
            private int intField;

            private string stringField;

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

        static void Main()
        {
            var list = new List<ClassA>();
            for (int i = 0; i < 1000000; i++)
                list.Add(new ClassA());

            DateTime startTime, endTime;

            Console.WriteLine("__Icepack__");

            var serializer = new Serializer();
            serializer.RegisterType(typeof(List<ClassA>));
            serializer.RegisterType(typeof(bool[]));

            var stream = new MemoryStream();

            startTime = DateTime.Now;
            serializer.Serialize(list, stream);
            endTime = DateTime.Now;

            Console.WriteLine($"Serialize time: {endTime - startTime}");
            Console.WriteLine($"Serialize size: {stream.Length}");
            
            startTime = DateTime.Now;
            serializer.Deserialize<List<ClassA>>(stream);
            endTime = DateTime.Now;
            Console.WriteLine($"Deserialize time: {endTime - startTime}");
            
        }
    }
}