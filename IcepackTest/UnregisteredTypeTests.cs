using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcepackTest
{
    public class UnregisteredTypeTests
    {
        [Test]
        public void SerializeUnregisteredType()
        {
            var serializer = new Serializer();

            var stream = new MemoryStream();

            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(typeof(UnregisteredClass), stream);
            });

            stream.Close();
        }

        [Test]
        public void SerializeUnregisteredTypeInTypeField()
        {
            var serializer = new Serializer();

            var obj = new ClassWithTypeField() { Field1 = 123, Field2 = typeof(UnregisteredClass), Field3 = 789 };

            var stream = new MemoryStream();

            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });

            stream.Close();
        }

        [Test]
        public void SerializeUnregisteredTypeInObjectField()
        {
            var serializer = new Serializer();

            var obj = new ClassWithObjectField() { Field1 = 123, Field2 = typeof(UnregisteredClass), Field3 = 789 };

            var stream = new MemoryStream();

            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });

            stream.Close();
        }

        [Test]
        public void SerializeUnregisteredClass()
        {
            var serializer = new Serializer();
            var obj = new UnregisteredClass();

            var stream = new MemoryStream();
            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });
            stream.Close();
        }
    }
}
