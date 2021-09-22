/*
using System;
using Icepack;

namespace Example
{
    [SerializableObject]
    abstract class ClassZ
    {
        private string field1;

        public ClassZ(string field1)
        {
            this.field1 = field1;
        }

        public override string ToString()
        {
            return $"ClassZ.field1={field1}";
        }

        public ClassZ() { }
    }

    [SerializableObject]
    class ClassA : ClassZ
    {
        private int field1;
        private ClassA field2;

        public ClassA(int field1, ClassA field2, string baseField1) : base(baseField1)
        {
            this.field1 = field1;
            this.field2 = field2;
        }

        public ClassA() : base() { }

        public override string ToString()
        {
            return $"[ClassA.field1={field1}, ClassA.field2={field2}, {base.ToString()}]";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Serializer serializer = new Serializer();

            ClassA nestedObj = new ClassA(123, null, "asdf");
            ClassA rootObj = new ClassA(456, nestedObj, "qwer");

            string str = serializer.Serialize(rootObj);
            Console.WriteLine("___Serialized Output___");
            Console.WriteLine(str);
            Console.WriteLine("");

            ClassA deserializedObj = serializer.Deserialize<ClassA>(str);
            Console.WriteLine("___Deserialized Object___");
            Console.WriteLine(deserializedObj);
        }
    }
}
*/


using System;
using System.Collections;
using System.Collections.Generic;
using Icepack;

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

        static void Main(string[] args)
        {
            List<ClassA> list = new List<ClassA>();
            for (int i = 0; i < 1000000; i++)
                list.Add(new ClassA());

            DateTime startTime, endTime;

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
    }
}
