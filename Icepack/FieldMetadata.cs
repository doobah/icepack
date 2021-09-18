using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq.Expressions;

namespace Icepack
{
    /// <summary> Contains data necessary for serializing and deserializing a field. </summary>
    internal class FieldMetadata
    {
        private FieldInfo fieldInfo;
        private Func<object, object> getter;
        private Action<object, object> setter;

        public FieldMetadata(FieldInfo fieldInfo)
        {
            this.fieldInfo = fieldInfo;
            getter = BuildGetter(fieldInfo);
            setter = BuildSetter(fieldInfo);
        }

        /// <summary> The type of the field. </summary>
        public FieldInfo FieldInfo
        {
            get { return fieldInfo; }
        }

        /// <summary> A function that gets the value of the field. </summary>
        public Func<object, object> Getter
        {
            get { return getter; }
        }

        /// <summary> A function that sets the value of the field. </summary>
        public Action<object, object> Setter
        {
            get { return setter; }
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
            UnaryExpression exConvertInstanceToDeclaringType = Expression.Convert(exInstance, fieldInfo.DeclaringType);
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
