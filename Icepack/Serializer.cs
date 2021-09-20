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
        public void RegisterType(Type type, bool isItemsNoReference = false)
        {
            typeRegistry.RegisterType(type, isItemsNoReference);
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
                return "0";

            Type type = obj.GetType();

            if (type == typeof(bool))
                return ((bool)obj) ? "1" : "0";
            else if (type.IsPrimitive || type == typeof(decimal))
                return obj.ToString();
            else if (type == typeof(string))
                return Toolbox.EscapeString((string)obj);
            else if (type.IsEnum)
                return ((int)obj).ToString();
            else if (type.IsValueType)
                return SerializeStruct(obj, context);
            else if (type.IsClass)
                return SerializeClass(obj, context);

            throw new ArgumentException($"Unable to serialize object: {obj}");
        }

        private string SerializeStruct(object obj, SerializationContext context)
        {
            if (obj is IIcepackHooks)
                ((IIcepackHooks)obj).OnBeforeSerialize();

            Type type = obj.GetType();

            TypeMetadata typeMetadata = typeRegistry.GetTypeMetadata(type);
            context.UsedTypes.Add(type);

            StringBuilder builder = new StringBuilder();

            builder.Append('[');
            builder.Append(typeMetadata.Id);

            foreach (FieldMetadata field in typeMetadata.Fields.Values)
            {
                object value = field.Getter(obj);
                builder.Append(',');
                builder.Append(SerializeField(value, field.IsReference, context));
            }

            builder.Append(']');

            return builder.ToString();
        }

        private string SerializeClass(object obj, SerializationContext context)
        {
            if (obj is IIcepackHooks)
                ((IIcepackHooks)obj).OnBeforeSerialize();

            Type type = obj.GetType();

            TypeMetadata typeMetadata = typeRegistry.GetTypeMetadata(type);
            context.UsedTypes.Add(type);

            StringBuilder builder = new StringBuilder();

            builder.Append('[');
            builder.Append(context.GetInstanceId(obj));
            builder.Append(',');
            builder.Append(typeMetadata.Id);

            if (type.IsArray)
            {
                foreach (object item in (Array)obj)
                {
                    builder.Append(',');
                    builder.Append(SerializeField(item, !typeMetadata.IsItemsNoReference, context));
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                foreach (object item in (IEnumerable)obj)
                {
                    builder.Append(',');
                    builder.Append(SerializeField(item, !typeMetadata.IsItemsNoReference, context));
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                foreach (object item in (IEnumerable)obj)
                {
                    builder.Append(',');
                    builder.Append(SerializeField(item, !typeMetadata.IsItemsNoReference, context));
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                IDictionary dict = (IDictionary)obj;

                foreach (object key in dict.Keys)
                {
                    builder.Append(',');
                    builder.Append(SerializeField(key, !typeMetadata.IsItemsNoReference, context));
                }

                foreach (object value in dict.Values)
                {
                    builder.Append(',');
                    builder.Append(SerializeField(value, !typeMetadata.IsItemsNoReference, context));
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
                    builder.Append(currentTypeMetadata.Id);

                    foreach (FieldMetadata field in currentTypeMetadata.Fields.Values)
                    {
                        object value = field.Getter(obj);
                        builder.Append(',');
                        builder.Append(SerializeField(value, field.IsReference, context));
                    }

                    builder.Append(']');

                    currentType = currentType.BaseType;
                }
            }

            builder.Append(']');

            return builder.ToString();
        }

        private string SerializeField(object value, bool isReference, SerializationContext context)
        {
            if (value != null && Toolbox.IsClass(value.GetType()) && isReference)
            {
                if (context.IsObjectRegistered(value))
                    return context.GetInstanceId(value).ToString();

                ulong id = context.RegisterObject(value);
                context.ObjectsToSerialize.Enqueue(value);
                return id.ToString();
            }
            else
                return SerializeObject(value, context);
        }

        public T Deserialize<T>(string str)
        {
            DeserializationContext context = new DeserializationContext();

            // Parse document as intermediate object tree
            List<object> documentNodes = ObjectTreeParser.Parse(str);

            // Extract types
            List<object> typeNodes = (List<object>)documentNodes[1];
            foreach (object t in typeNodes)
            {
                List<object> typeNode = (List<object>)t;
                string name = (string)typeNode[1];
                TypeMetadata registeredTypeMetadata = typeRegistry.GetTypeMetadata(name);
                if (registeredTypeMetadata == null)
                    continue;

                TypeMetadata typeMetadata = new TypeMetadata(registeredTypeMetadata, typeNode);
                context.Types.Add(typeMetadata.Id, typeMetadata);
            }

            // Create empty objects
            List<object> objectNodes = (List<object>)documentNodes[0];
            int startIdx = Toolbox.IsClass(typeof(T)) ? 0 : 1;
            for (int i = startIdx; i < objectNodes.Count; i++)
            {
                List<object> objectNode = (List<object>)objectNodes[i];
                ulong objectId = ulong.Parse((string)objectNode[0]);
                TypeMetadata objectTypeMetadata = context.Types[ulong.Parse((string)objectNode[1])];
                Type objectType = objectTypeMetadata.Type;
                object obj;
                if (objectType.IsArray)
                {
                    Type elementType = objectType.GetElementType();
                    obj = Array.CreateInstance(elementType, objectNode.Count - 2);
                }
                else
                    obj = Activator.CreateInstance(objectType);
                context.Objects.Add(objectId, obj);
            }

            // Deserialize objects
            T rootObject = (T)DeserializeObject(objectNodes[0], typeof(T), context);
            for (int i = 1; i < objectNodes.Count; i++)
            {
                List<object> objectNode = (List<object>)objectNodes[i];
                ulong typeId = ulong.Parse((string)objectNode[1]);
                Type type = context.Types[typeId].Type;
                DeserializeObject(objectNode, type, context);
            }

            return rootObject;
        }

        private object DeserializeObject(object objNode, Type type, DeserializationContext context)
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
            else if (type.IsValueType)
                return DeserializeStruct(objNode, context);
            else if (type.IsClass)
                return DeserializeClass(objNode, context);

            throw new ArgumentException($"Unable to deserialize object: {objNode}");
        }

        private object DeserializeStruct(object objNode, DeserializationContext context)
        {
            List<object> structNode = (List<object>)objNode;
            ulong typeId = ulong.Parse((string)structNode[0]);
            TypeMetadata typeMetadata = context.Types[typeId];
            object structObj = Activator.CreateInstance(typeMetadata.Type);

            for (int i = 0; i < typeMetadata.Fields.Count; i++)
            {
                FieldMetadata field = typeMetadata.Fields.Values[i];
                Type fieldType = field.FieldInfo.FieldType;
                object fieldNode = structNode[i + 1];
                object value = DeserializeField(fieldType, field.IsReference, fieldNode, context);
                field.Setter(structObj, value);
            }

            if (structObj is IIcepackHooks)
                ((IIcepackHooks)structObj).OnAfterDeserialize();

            return structObj;
        }

        private object DeserializeClass(object objNode, DeserializationContext context)
        {
            List<object> classNode = (List<object>)objNode;
            ulong objId = ulong.Parse((string)classNode[0]);
            ulong typeId = ulong.Parse((string)classNode[1]);
            TypeMetadata classTypeMetadata = context.Types[typeId];
            Type classType = classTypeMetadata.Type;

            object classObj;
            if (objId == Toolbox.NULL_ID)
            {
                if (classType.IsArray)
                {
                    Type elementType = classType.GetElementType();
                    classObj = Array.CreateInstance(elementType, classNode.Count - 2);
                }
                else
                    classObj = Activator.CreateInstance(classType);
            }
            else
                classObj = context.Objects[objId];

            if (classType.IsArray)
            {
                Array arrayObj = (Array)classObj;
                Type elementType = classType.GetElementType();
                bool isReference = Toolbox.IsClass(elementType) && !classTypeMetadata.IsItemsNoReference;

                for (int i = 2; i < classNode.Count; i++)
                {
                    object elementNode = classNode[i];
                    object value = DeserializeField(elementType, isReference, elementNode, context);
                    arrayObj.SetValue(value, i - 2);
                }
            }
            else if (classType.IsGenericType && classType.GetGenericTypeDefinition() == typeof(List<>))
            {
                IList listObj = (IList)classObj;
                Type elementType = classType.GenericTypeArguments[0];
                bool isReference = Toolbox.IsClass(elementType) && !classTypeMetadata.IsItemsNoReference;

                for (int i = 2; i < classNode.Count; i++)
                {
                    object elementNode = classNode[i];
                    object value = DeserializeField(elementType, isReference, elementNode, context);
                    listObj.Add(value);
                }
            }
            else if (classType.IsGenericType && classType.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                Type elementType = classType.GenericTypeArguments[0];
                bool isReference = Toolbox.IsClass(elementType) && !classTypeMetadata.IsItemsNoReference;

                for (int i = 2; i < classNode.Count; i++)
                {
                    object elementNode = classNode[i];
                    object value = DeserializeField(elementType, isReference, elementNode, context);
                    classTypeMetadata.HashSetAdder(classObj, value);
                }
            }
            else if (classType.IsGenericType && classType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                IDictionary dictObj = (IDictionary)classObj;
                Type keyType = classType.GenericTypeArguments[0];
                bool isKeyReference = Toolbox.IsClass(keyType) && !classTypeMetadata.IsItemsNoReference;
                Type valueType = classType.GenericTypeArguments[1];
                bool isValueReference = Toolbox.IsClass(valueType) && !classTypeMetadata.IsItemsNoReference;
                int length = (classNode.Count - 2) / 2;

                for (int i = 0; i < length; i++)
                {
                    int keyIdx = i + 2;
                    int valueIdx = keyIdx + length;

                    object key = DeserializeField(keyType, isKeyReference, classNode[keyIdx], context);
                    object value = DeserializeField(valueType, isValueReference, classNode[valueIdx], context);
                    dictObj.Add(key, value);
                }
            }
            else
            {
                for (int i = 2; i < classNode.Count; i++)
                {
                    List<object> partialClassNode = (List<object>)classNode[i];
                    ulong partialClassTypeId = ulong.Parse((string)partialClassNode[0]);
                    TypeMetadata partialClassTypeMetadata = context.Types[partialClassTypeId];
                    for (int j = 1; j < partialClassNode.Count; j++)
                    {
                        FieldMetadata field = partialClassTypeMetadata.Fields.Values[j - 1];
                        Type fieldType = field.FieldInfo.FieldType;
                        object fieldNode = partialClassNode[j];
                        object value = DeserializeField(fieldType, field.IsReference, fieldNode, context);
                        field.Setter(classObj, value);
                    }
                }

                if (classObj is IIcepackHooks)
                    ((IIcepackHooks)classObj).OnAfterDeserialize();
            }

            return classObj;
        }

        private object DeserializeField(Type fieldType, bool isReference, object fieldNode, DeserializationContext context)
        {
            object value;
            if (isReference)
            {
                ulong objId = ulong.Parse((string)fieldNode);
                value = (objId == Toolbox.NULL_ID) ? null : context.Objects[objId];
            }
            else
                value = DeserializeObject(fieldNode, fieldType, context);

            return value;
        }
    }
}
