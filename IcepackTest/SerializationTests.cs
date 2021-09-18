using NUnit.Framework;
using Icepack;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace IcepackTest
{
    public class SerializationTests
    {
        [SerializableObject]
        private class ClassA
        {
            public int Field1;
            public string Field2;
            public float Field3;
        }

        [Test]
        public void Serialize()
        {
            Serializer serializer = new Serializer();

            ClassA obj1 = new ClassA() { Field1 = 123, Field2 = "asdf", Field3 = 6.78f };
            string str = serializer.Serialize(obj1);
            ClassA obj2 = serializer.Deserialize<ClassA>(str);

            Assert.AreEqual(obj1.Field1, obj2.Field1);
            Assert.AreEqual(obj1.Field2, obj2.Field2);
            Assert.AreEqual(obj1.Field3, obj2.Field3);

            Console.WriteLine(str);
        }
    }
}