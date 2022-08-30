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
    public class CollectionTests
    {
        [Test]
        public void SerializeArray()
        {
            var serializer = new Serializer();

            int[] array = new int[] { 1, 2, 3 };

            var stream = new MemoryStream();
            serializer.Serialize(array, stream);
            stream.Position = 0;
            int[] deserializedArray = serializer.Deserialize<int[]>(stream);
            stream.Close();

            Assert.NotNull(deserializedArray);
            Assert.AreEqual(3, deserializedArray.Length);
            Assert.AreEqual(1, deserializedArray[0]);
            Assert.AreEqual(2, deserializedArray[1]);
            Assert.AreEqual(3, deserializedArray[2]);
        }

        [Test]
        public void SerializeList()
        {
            var serializer = new Serializer();

            var list = new List<string>() { "qwer", "asdf", "zxcv" };

            var stream = new MemoryStream();
            serializer.Serialize(list, stream);
            stream.Position = 0;
            List<string> deserializedList = serializer.Deserialize<List<string>>(stream);
            stream.Close();

            Assert.NotNull(deserializedList);
            Assert.AreEqual(3, deserializedList.Count);
            Assert.AreEqual("qwer", deserializedList[0]);
            Assert.AreEqual("asdf", deserializedList[1]);
            Assert.AreEqual("zxcv", deserializedList[2]);
        }

        [Test]
        public void SerializeHashSet()
        {
            var serializer = new Serializer();

            var set = new HashSet<string>() { "qwer", "asdf", "zxcv" };

            var stream = new MemoryStream();
            serializer.Serialize(set, stream);
            stream.Position = 0;
            HashSet<string> deserializedSet = serializer.Deserialize<HashSet<string>>(stream);
            stream.Close();

            Assert.NotNull(deserializedSet);
            Assert.AreEqual(3, deserializedSet.Count);
            Assert.True(deserializedSet.Contains("qwer"));
            Assert.True(deserializedSet.Contains("asdf"));
            Assert.True(deserializedSet.Contains("zxcv"));
        }

        [Test]
        public void SerializeDictionary()
        {
            var serializer = new Serializer();

            var dictionary = new Dictionary<int, string>() { { 1, "asdf" }, { 2, "zxcv" } };

            var stream = new MemoryStream();
            serializer.Serialize(dictionary, stream);
            stream.Position = 0;
            Dictionary<int, string> deserializedDictionary = serializer.Deserialize<Dictionary<int, string>>(stream);
            stream.Close();

            Assert.NotNull(deserializedDictionary);
            Assert.AreEqual(2, deserializedDictionary.Count);
            Assert.True(deserializedDictionary.ContainsKey(1));
            Assert.True(deserializedDictionary.ContainsKey(2));
            Assert.AreEqual("asdf", deserializedDictionary[1]);
            Assert.AreEqual("zxcv", deserializedDictionary[2]);
        }
    }
}
