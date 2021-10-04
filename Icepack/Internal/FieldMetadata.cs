﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.IO;

namespace Icepack
{
    /// <summary> Stores data used to serialize/deserialize a field. </summary>
    internal class FieldMetadata
    {
        /// <summary> Reflection data for the field. </summary>
        public FieldInfo FieldInfo { get; }

        /// <summary> Delegate that gets the value of this field for a given object. </summary>
        public Func<object, object> Getter { get; }

        /// <summary> Delegate that sets the value of this field for a given object. The parameters are (object, value). </summary>
        public Action<object, object> Setter { get; }

        /// <summary> Delegate that deserializes the field for a given object. </summary>
        public Func<DeserializationContext, BinaryReader, object> Deserialize { get; }

        /// <summary> Delegate that serializes the field for a given object. </summary>
        public Action<object, SerializationContext, BinaryWriter> Serialize { get; }

        /// <summary> The size of the field in bytes. </summary>
        public int Size { get; }

        /// <summary> Called during deserialization. Creates new field metadata. </summary>
        /// <param name="size"> The size of the field in bytes. </param>
        /// <param name="registeredFieldMetadata"> The corresponding registered metadata for the field. </param>
        public FieldMetadata(int size, FieldMetadata registeredFieldMetadata)
        {
            Size = size;

            if (registeredFieldMetadata == null)
            {
                FieldInfo = null;
                Getter = null;
                Setter = null;
                Deserialize = null;
                Serialize = null;
            }
            else
            {
                FieldInfo = registeredFieldMetadata.FieldInfo;
                Getter = registeredFieldMetadata.Getter;
                Setter = registeredFieldMetadata.Setter;
                Deserialize = registeredFieldMetadata.Deserialize;
                Serialize = registeredFieldMetadata.Serialize;
            }
        }

        /// <summary> Called during type registration. Creates new field metadata. </summary>
        /// <param name="fieldInfo"> The <see cref="FieldInfo"/> for the field. </param>
        /// <param name="typeRegistry"> The serializer's type registry. </param>
        public FieldMetadata(FieldInfo fieldInfo, TypeRegistry typeRegistry)
        {
            FieldInfo = fieldInfo;
            Getter = BuildGetter(fieldInfo);
            Setter = BuildSetter(fieldInfo);
            Deserialize = DeserializationDelegateFactory.GetFieldOperation(fieldInfo.FieldType);
            Serialize = SerializationDelegateFactory.GetFieldOperation(fieldInfo.FieldType);
            Size = FieldSizeFactory.GetFieldSize(fieldInfo.FieldType, typeRegistry);
        }

        /// <summary> Builds the delegate used to get the value of the field. </summary>
        /// <param name="fieldInfo"> The <see cref="FieldInfo"/> for the field. </param>
        /// <returns> The delegate. </returns>
        private static Func<object, object> BuildGetter(FieldInfo fieldInfo)
        {
            ParameterExpression exInstance = Expression.Parameter(typeof(object));
            UnaryExpression exConvertToDeclaringType = Expression.Convert(exInstance, fieldInfo.DeclaringType);
            MemberExpression exMemberAccess = Expression.MakeMemberAccess(exConvertToDeclaringType, fieldInfo);
            UnaryExpression exConvertToObject = Expression.Convert(exMemberAccess, typeof(object));
            Expression<Func<object, object>> lambda = Expression.Lambda<Func<object, object>>(exConvertToObject, exInstance);
            Func<object, object> action = lambda.Compile();

            return action;
        }

        /// <summary> Builds the delegate used to set the value of the field. </summary>
        /// <param name="fieldInfo"> The <see cref="FieldInfo"/> for the field. </param>
        /// <returns> The delegate. </returns>
        private static Action<object, object> BuildSetter(FieldInfo fieldInfo)
        {
            ParameterExpression exInstance = Expression.Parameter(typeof(object));
            UnaryExpression exConvertInstanceToDeclaringType;
            if (fieldInfo.DeclaringType.IsValueType)
                exConvertInstanceToDeclaringType = Expression.Unbox(exInstance, fieldInfo.DeclaringType);
            else
                exConvertInstanceToDeclaringType = Expression.Convert(exInstance, fieldInfo.DeclaringType);
            ParameterExpression exValue = Expression.Parameter(typeof(object));
            UnaryExpression exConvertValueToFieldType = Expression.Convert(exValue, fieldInfo.FieldType);
            MemberExpression exMemberAccess = Expression.MakeMemberAccess(exConvertInstanceToDeclaringType, fieldInfo);
            BinaryExpression exAssign = Expression.Assign(exMemberAccess, exConvertValueToFieldType);
            Expression<Action<object, object>> lambda = Expression.Lambda<Action<object, object>>(exAssign, exInstance, exValue);
            Action<object, object> action = lambda.Compile();

            return action;
        }
    }
}
