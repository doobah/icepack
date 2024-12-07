using Icepack;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IcepackTest;

public class CollectionTests
{
    [Test]
    public void SerializeArray()
    {
        Serializer serializer = new();

        int[] array = [ 1, 2, 3 ];

        MemoryStream stream = new();
        serializer.Serialize(array, stream);
        stream.Position = 0;
        int[]? deserializedArray = serializer.Deserialize<int[]>(stream);
        stream.Close();

        Assert.That(deserializedArray, Is.Not.Null);
        Assert.That(deserializedArray!.Length, Is.EqualTo(3));
        Assert.That(deserializedArray[0], Is.EqualTo(1));
        Assert.That(deserializedArray[1], Is.EqualTo(2));
        Assert.That(deserializedArray[2], Is.EqualTo(3));
    }

    [Test]
    public void SerializeList()
    {
        Serializer serializer = new();

        List<string> list = [ "qwer", "asdf", "zxcv" ];

        MemoryStream stream = new();
        serializer.Serialize(list, stream);
        stream.Position = 0;
        List<string>? deserializedList = serializer.Deserialize<List<string>>(stream);
        stream.Close();

        Assert.That(deserializedList, Is.Not.Null);
        Assert.That(deserializedList!.Count, Is.EqualTo(3));
        Assert.That(deserializedList[0], Is.EqualTo("qwer"));
        Assert.That(deserializedList[1], Is.EqualTo("asdf"));
        Assert.That(deserializedList[2], Is.EqualTo("zxcv"));
    }

    [Test]
    public void SerializeHashSet()
    {
        Serializer serializer = new();

        HashSet<string> set = [ "qwer", "asdf", "zxcv" ];

        MemoryStream stream = new();
        serializer.Serialize(set, stream);
        stream.Position = 0;
        HashSet<string>? deserializedSet = serializer.Deserialize<HashSet<string>>(stream);
        stream.Close();

        Assert.That(deserializedSet, Is.Not.Null);
        Assert.That(deserializedSet!.Count, Is.EqualTo(3));
        Assert.That(deserializedSet.Contains("qwer"));
        Assert.That(deserializedSet.Contains("asdf"));
        Assert.That(deserializedSet.Contains("zxcv"));
    }

    [Test]
    public void SerializeDictionary()
    {
        Serializer serializer = new();

        Dictionary<int, string> dictionary = new() { { 1, "asdf" }, { 2, "zxcv" } };

        MemoryStream stream = new();
        serializer.Serialize(dictionary, stream);
        stream.Position = 0;
        Dictionary<int, string>? deserializedDictionary = serializer.Deserialize<Dictionary<int, string>>(stream);
        stream.Close();

        Assert.That(deserializedDictionary, Is.Not.Null);
        Assert.That(deserializedDictionary!.Count, Is.EqualTo(2));
        Assert.That(deserializedDictionary.ContainsKey(1));
        Assert.That(deserializedDictionary.ContainsKey(2));
        Assert.That(deserializedDictionary[1], Is.EqualTo("asdf"));
        Assert.That(deserializedDictionary[2], Is.EqualTo("zxcv"));
    }
}
