using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq.Expressions;

namespace Icepack
{
    /// <summary> Stores data used to serialize/deserialize a field. </summary>
    internal class FieldMetadata
    {
        /// <summary> Reflection data for the field. </summary>
        public FieldInfo FieldInfo { get; }

        /// <summary> Whether this field is a reference to an object. </summary>
        public bool IsReference { get; }

        /// <summary> Gets the value of this field for a given object. </summary>
        public Func<object, object> Getter { get; }

        /// <summary> Sets the value of this field for a given object. The parameters are (object, value). </summary>
        public Action<object, object> Setter { get; }

        public Func<DeserializationContext, object> Deserialize { get; }

        public Action<object, SerializationContext> Serialize { get; }

        public FieldMetadata(FieldInfo fieldInfo)
        {
            FieldInfo = fieldInfo;
            IsReference = Toolbox.IsClass(fieldInfo.FieldType) && fieldInfo.GetCustomAttribute<ValueOnlyAttribute>() == null;
            Getter = BuildGetter(fieldInfo);
            Setter = BuildSetter(fieldInfo);
            Deserialize = DeserializationOperationFactory.GetOperation(fieldInfo.FieldType, IsReference);
            Serialize = SerializationOperationFactory.GetOperation(fieldInfo.FieldType, IsReference);
        }

        private Func<object, object> BuildGetter(FieldInfo fieldInfo)
        {
            ParameterExpression exInstance = Expression.Parameter(typeof(object));
            UnaryExpression exConvertToDeclaringType = Expression.Convert(exInstance, fieldInfo.DeclaringType);
            MemberExpression exMemberAccess = Expression.MakeMemberAccess(exConvertToDeclaringType, fieldInfo);
            UnaryExpression exConvertToObject = Expression.Convert(exMemberAccess, typeof(object));
            Expression<Func<object, object>> lambda = Expression.Lambda<Func<object, object>>(exConvertToObject, exInstance);
            Func<object, object> action = lambda.Compile();

            return action;
        }

        private Action<object, object> BuildSetter(FieldInfo fieldInfo)
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
