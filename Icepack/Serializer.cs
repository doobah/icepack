using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Linq.Expressions;

namespace Icepack
{
    /// <summary> Contains methods for serializing/deserializing objects using the Icepack format. </summary>
    public class Serializer
    {
        private TypeRegistry typeRegistry;

        public Serializer()
        {
            typeRegistry = new TypeRegistry();
        }

        /// <summary> Registers a type as serializable. </summary>
        /// <param name="type"> The type to register. </param>
        public void RegisterType(Type type)
        {
            typeRegistry.RegisterType(type);
        }

        /// <summary> Serializes an object graph as a string. </summary>
        /// <param name="obj"> The root object to be serialized. </param>
        /// <returns> The serialized object graph. </returns>
        public string Serialize(object obj)
        {
            SerializationContext context = new SerializationContext();

            StringBuilder documentBuilder = new StringBuilder();
            documentBuilder.Append('[');

            // Serialize objects
            documentBuilder.Append('[');
            if (Toolbox.IsClass(obj.GetType()))
                context.RegisterObject(obj);
            documentBuilder.Append(SerializeObject(obj, context));
            while (context.ObjectsToSerialize.Count > 0)
            {
                documentBuilder.Append(',');
                object objToSerialize = context.ObjectsToSerialize.Dequeue();
                documentBuilder.Append(SerializeObject(objToSerialize, context));
            }
            documentBuilder.Append(']');

            documentBuilder.Append(',');

            // Serialize type data
            documentBuilder.Append('[');
            documentBuilder.AppendJoin(',', context.UsedTypes.Select(type => typeRegistry.GetTypeMetadata(type).SerializedString));
            documentBuilder.Append(']');
            
            documentBuilder.Append(']');

            return documentBuilder.ToString();
        }

        private string SerializeObject(object obj, SerializationContext context)
        {
            if (obj == null)
                return "\"0\"";

            Type type = obj.GetType();

            if (type == typeof(bool))
                return ((bool)obj) ? "\"1\"" : "\"0\"";

            if (type.IsPrimitive || type == typeof(decimal))
                return $"\"{obj}\"";

            if (type == typeof(string))
                return $"\"{EscapeString((string)obj)}\"";

            if (type.IsEnum)
                return $"\"{((int)obj)}\"";

            if (obj is ISerializationHooks)
                ((ISerializationHooks)obj).OnBeforeSerialize();

            if (type.IsValueType)
            {
                TypeMetadata typeMetadata = typeRegistry.GetTypeMetadata(type);
                context.UsedTypes.Add(type);

                StringBuilder builder = new StringBuilder();

                builder.Append('[');
                builder.Append($"\"{typeMetadata.Id}\"");

                foreach (FieldMetadata field in typeMetadata.Fields.Values)
                {
                    object value = field.Getter(obj);
                    builder.Append(',');
                    builder.Append(SerializeField(value, context));
                }

                builder.Append(']');

                return builder.ToString();
            }
            
            if (type.IsClass)
            {
                TypeMetadata typeMetadata = typeRegistry.GetTypeMetadata(type);
                context.UsedTypes.Add(type);

                StringBuilder builder = new StringBuilder();

                builder.Append('[');
                builder.Append($"\"{context.GetInstanceId(obj)}\"");
                builder.Append(',');
                builder.Append($"\"{typeMetadata.Id}\"");

                if (type.IsArray)
                {
                    foreach (object item in (Array)obj)
                    {
                        builder.Append(',');
                        builder.Append(SerializeField(item, context));
                    }
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    foreach (object item in (IList)obj)
                    {
                        builder.Append(',');
                        builder.Append(SerializeField(item, context));
                    }
                }
                else
                {
                    Type currentType = obj.GetType();
                    while (currentType != typeof(object))
                    {
                        TypeMetadata currentTypeMetadata = typeRegistry.GetTypeMetadata(currentType);
                        context.UsedTypes.Add(currentType);

                        builder.Append(',');
                        builder.Append('[');
                        builder.Append($"\"{currentTypeMetadata.Id}\"");

                        foreach (FieldMetadata field in currentTypeMetadata.Fields.Values)
                        {
                            object value = field.Getter(obj);
                            builder.Append(',');
                            builder.Append(SerializeField(value, context));
                        }

                        builder.Append(']');

                        currentType = currentType.BaseType;
                    }
                }

                builder.Append(']');

                return builder.ToString();
            }

            throw new ArgumentException($"Unable to serialize object: {obj}");
        }

        private string EscapeString(string str)
        {
            StringBuilder builder = new StringBuilder(str.Length * 2);
            foreach (char c in str)
            {
                if ("\"\\".Contains(c))
                    builder.Append('\\');

                builder.Append(c);
            }

            return builder.ToString();
        }

        private string SerializeField(object value, SerializationContext context)
        {
            if (Toolbox.IsClass(value.GetType()))
            {
                if (context.IsObjectRegistered(value))
                    return context.GetInstanceId(value).ToString();

                ulong id = context.RegisterObject(value);
                context.ObjectsToSerialize.Enqueue(value);
                return $"\"{id}\"";
            }
            else
                return SerializeObject(value, context);
        }

        public T Deserialize<T>(string str)
        {
            List<object> documentNodes = (List<object>)ObjectTreeParser.Parse(str)[0];
            List<object> typeNodes = (List<object>)documentNodes[1];
            Dictionary<ulong, TypeMetadata> types = new Dictionary<ulong, TypeMetadata>();
            foreach (object t in typeNodes)
            {
                List<object> typeNode = (List<object>)t;
                string name = (string)typeNode[1];
                TypeMetadata registeredTypeMetadata = typeRegistry.GetTypeMetadata(name);
                if (registeredTypeMetadata == null)
                    continue;

                TypeMetadata typeMetadata = new TypeMetadata(registeredTypeMetadata, typeNode);
                types.Add(typeMetadata.Id, typeMetadata);
            }

            List<object> objectNodes = (List<object>)documentNodes[0];
            Dictionary<ulong, object> objects = new Dictionary<ulong, object>();
            int startIdx = Toolbox.IsClass(typeof(T)) ? 0 : 1;
            for (int i = startIdx; i < objectNodes.Count; i++)
            {
                List<object> objectNode = (List<object>)objectNodes[i];
                ulong objectId = ulong.Parse((string)objectNode[0]);
                TypeMetadata objectTypeMetadata = types[ulong.Parse((string)objectNode[1])];
                Type objectType = objectTypeMetadata.Type;
                object obj;
                if (objectType.IsArray)
                {
                    Type elementType = objectType.GetElementType();
                    obj = Array.CreateInstance(elementType, objectNode.Count - 2);
                }
                else
                    obj = Activator.CreateInstance(objectType);
                objects.Add(objectId, obj);
            }

            List<object> rootObjectNode = (List<object>)objectNodes[0];
            T rootObject = (T)DeserializeObject(rootObjectNode, typeof(T), types, objects);
            for (int i = 1; i < objectNodes.Count; i++)
            {
                List<object> objectNode = (List<object>)objectNodes[i];
                ulong typeId = ulong.Parse((string)objectNode[1]);
                Type type = types[typeId].Type;
                DeserializeObject(objectNode, type, types, objects);
            }

            return rootObject;
        }

        private object DeserializeObject(object objNode, Type type, Dictionary<ulong, TypeMetadata> types, Dictionary<ulong, object> objects)
        {
            if (Toolbox.IsClass(type))
            {
                List<object> classNode = (List<object>)objNode;
                ulong objId = ulong.Parse((string)classNode[0]);
                ulong typeId = ulong.Parse((string)classNode[1]);
                TypeMetadata classType = types[typeId];
                object classObj = objects[objId];

                if (classType.Type.IsArray)
                {
                    Array arrayObj = (Array)classObj;
                    Type itemType = classType.Type.GetElementType();
                    bool isElementClass = Toolbox.IsClass(itemType);

                    for (int i = 2; i < classNode.Count; i++)
                    {
                        object value;
                        if (isElementClass)
                            value = objects[ulong.Parse((string)classNode[i])];
                        else
                            value = DeserializeObject(classNode[i], itemType, types, objects);
                        arrayObj.SetValue(value, i - 2);
                    }
                }
                else if (classType.Type.IsGenericType && classType.Type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    IList listObj = (IList)classObj;
                    Type itemType = type.GenericTypeArguments[0];
                    bool isElementClass = Toolbox.IsClass(itemType);

                    for (int i = 2; i < classNode.Count; i++)
                    {
                        object value;
                        if (isElementClass)
                            value = objects[ulong.Parse((string)classNode[i])];
                        else
                            value = DeserializeObject(classNode[i], itemType, types, objects);
                        listObj.Add(value);
                    }
                }
                else
                {
                    for (int i = 2; i < classNode.Count; i++)
                    {
                        List<object> partialClassNode = (List<object>)classNode[i];
                        ulong partialClassTypeId = ulong.Parse((string)partialClassNode[0]);
                        TypeMetadata partialClassTypeMetadata = types[partialClassTypeId];
                        for (int j = 1; j < partialClassNode.Count; j++)
                        {
                            FieldMetadata field = partialClassTypeMetadata.Fields.Values[j - 1];
                            Type fieldType = field.FieldInfo.FieldType;
                            if (Toolbox.IsClass(fieldType))
                            {
                                ulong fieldObjId = ulong.Parse((string)partialClassNode[j]);
                                field.Setter(classObj, objects[fieldObjId]);
                            }
                            else
                            {
                                object value = DeserializeObject(partialClassNode[j], fieldType, types, objects);
                                field.Setter(classObj, value);
                            }
                        }
                    }
                }

                return classObj;
            }
            else
            {
                if (type == typeof(bool))
                    return (string)objNode == "1" ? true : false;
                else if (type == typeof(int))
                    return int.Parse((string)objNode);
                else if (type == typeof(uint))
                    return uint.Parse((string)objNode);
                else if (type == typeof(short))
                    return short.Parse((string)objNode);
                else if (type == typeof(ushort))
                    return ushort.Parse((string)objNode);
                else if (type == typeof(long))
                    return long.Parse((string)objNode);
                else if (type == typeof(ulong))
                    return ulong.Parse((string)objNode);
                else if (type == typeof(byte))
                    return byte.Parse((string)objNode);
                else if (type == typeof(decimal))
                    return decimal.Parse((string)objNode);
                else if (type == typeof(float))
                    return float.Parse((string)objNode);
                else if (type == typeof(double))
                    return double.Parse((string)objNode);
                else if (type == typeof(string))
                    return (string)objNode;
                else if (type.IsEnum)
                    return Enum.ToObject(type, int.Parse((string)objNode));

                if (type.IsValueType)
                {
                    List<object> structNode = (List<object>)objNode;
                    ulong typeId = ulong.Parse((string)structNode[0]);
                    TypeMetadata typeMetadata = types[typeId];
                    object structObj = Activator.CreateInstance(typeMetadata.Type);

                    for (int i = 0; i < typeMetadata.Fields.Count; i++)
                    {
                        FieldMetadata field = typeMetadata.Fields.Values[i];
                        Type fieldType = field.FieldInfo.FieldType;
                        if (Toolbox.IsClass(fieldType))
                        {
                            ulong objId = ulong.Parse((string)structNode[i + 1]);
                            field.Setter(structObj, objects[objId]);
                        }
                        else
                        {
                            object value = DeserializeObject(structNode[i + 1], fieldType, types, objects);
                            field.Setter(structObj, value);
                        }
                    }

                    return structObj;
                }
            }

            throw new ArgumentException($"Unable to deserialize object: {objNode}");
        }
    }
}
