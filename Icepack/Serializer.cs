using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;

namespace Icepack
{
    public class Serializer
    {
        private TypeRegistry typeRegistry;

        public Serializer()
        {
            typeRegistry = new TypeRegistry();
        }

        public string Serialize(object obj)
        {
            SerializationContext context = new SerializationContext();

            StringBuilder objectsStrBuilder = new StringBuilder();
            objectsStrBuilder.Append('[');
            if (Toolbox.IsClass(obj.GetType()))
                context.RegisterInstance(obj);
            objectsStrBuilder.Append(SerializeObject(obj, context));
            while (context.ObjectsToSerialize.Count > 0)
            {
                objectsStrBuilder.Append(',');
                object objToSerialize = context.ObjectsToSerialize.Dequeue();
                objectsStrBuilder.Append(SerializeObject(objToSerialize, context));
            }
            objectsStrBuilder.Append(']');

            StringBuilder typesBuilder = new StringBuilder();
            typesBuilder.Append('[');
            typesBuilder.Append(string.Join(',', context.UsedTypes.Select(type => typeRegistry.GetTypeMetadata(type).SerializedString)));
            typesBuilder.Append(']');

            StringBuilder documentBuilder = new StringBuilder();
            documentBuilder.Append('[');
            documentBuilder.Append(typesBuilder.ToString());
            documentBuilder.Append(',');
            documentBuilder.Append(objectsStrBuilder.ToString());
            documentBuilder.Append(']');

            return documentBuilder.ToString();
        }

        private string SerializeObject(object obj, SerializationContext context)
        {
            Type type = obj.GetType();

            if (type == typeof(bool))
                return ((bool)obj) ? "true" : "false";

            if (type.IsPrimitive || type == typeof(decimal))
                return obj.ToString();

            if (type == typeof(string))
                return $"\"{obj}\"";

            if (type.IsEnum)
                return ((int)obj).ToString();

            if (type.IsValueType)
            {
                TypeMetadata typeMetadata = typeRegistry.GetTypeMetadata(type);
                context.UsedTypes.Add(type);
                StringBuilder builder = new StringBuilder();
                builder.Append('[');
                builder.Append(typeMetadata.Id);
                foreach (Func<object, object> getter in typeMetadata.Getters)
                {
                    builder.Append(',');
                    object value = getter(obj);
                    if (Toolbox.IsClass(value.GetType()))
                    {
                        context.RegisterInstance(value);
                        builder.Append(context.InstanceIds[value]);
                        context.ObjectsToSerialize.Enqueue(value);
                    }
                    else
                        builder.Append(SerializeObject(value, context));
                }
                builder.Append(']');

                return builder.ToString();
            }

            if (type.IsClass)
            {
                if (type.IsArray)
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append('[');
                    builder.Append(context.InstanceIds[obj]);
                    TypeMetadata objTypeMetadata = typeRegistry.GetTypeMetadata(type);
                    builder.Append(',');
                    builder.Append(objTypeMetadata.Id);

                    Type currentType = obj.GetType();
                    TypeMetadata typeMetadata = typeRegistry.GetTypeMetadata(currentType);
                    context.UsedTypes.Add(currentType);

                    foreach (object item in (Array)obj)
                    {
                        builder.Append(',');
                        if (Toolbox.IsClass(item.GetType()))
                        {
                            context.RegisterInstance(item);
                            builder.Append(context.InstanceIds[item]);
                            context.ObjectsToSerialize.Enqueue(item);
                        }
                        else
                            builder.Append(SerializeObject(item, context));
                    }

                    builder.Append(']');

                    return builder.ToString();
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append('[');
                    builder.Append(context.InstanceIds[obj]);
                    TypeMetadata objTypeMetadata = typeRegistry.GetTypeMetadata(obj.GetType());
                    builder.Append(',');
                    builder.Append(objTypeMetadata.Id);

                    Type currentType = obj.GetType();
                    TypeMetadata typeMetadata = typeRegistry.GetTypeMetadata(currentType);
                    context.UsedTypes.Add(currentType);

                    foreach (object item in (IList)obj)
                    {
                        builder.Append(',');
                        if (Toolbox.IsClass(item.GetType()))
                        {
                            context.RegisterInstance(item);
                            builder.Append(context.InstanceIds[item]);
                            context.ObjectsToSerialize.Enqueue(item);
                        }
                        else
                            builder.Append(SerializeObject(item, context));
                    }

                    builder.Append(']');

                    return builder.ToString();
                }
                else
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append('[');
                    builder.Append(context.InstanceIds[obj]);
                    TypeMetadata objTypeMetadata = typeRegistry.GetTypeMetadata(obj.GetType());
                    builder.Append(',');
                    builder.Append(objTypeMetadata.Id);

                    Type currentType = obj.GetType();
                    while (currentType != typeof(object))
                    {
                        TypeMetadata typeMetadata = typeRegistry.GetTypeMetadata(currentType);
                        context.UsedTypes.Add(currentType);

                        builder.Append(',');
                        builder.Append('[');
                        builder.Append(typeMetadata.Id);
                        foreach (Func<object, object> getter in typeMetadata.Getters)
                        {
                            builder.Append(',');
                            object value = getter(obj);
                            if (Toolbox.IsClass(value.GetType()))
                            {
                                context.RegisterInstance(value);
                                builder.Append(context.InstanceIds[value]);
                                context.ObjectsToSerialize.Enqueue(value);
                            }
                            else
                                builder.Append(SerializeObject(value, context));
                        }
                        builder.Append(']');

                        currentType = currentType.BaseType;
                    }
                    builder.Append(']');

                    return builder.ToString();
                }
            }

            throw new ArgumentException($"Unable to serialize object: {obj}");
        }
    }
}
