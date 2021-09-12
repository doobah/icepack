using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

namespace Icepack
{
    internal class TypeMetadata
    {
        private ulong id;
        private List<Func<object, object>> getters;
        private string serializedStr;

        public TypeMetadata(ulong id, Type type)
        {
            this.id = id;

            List<FieldInfo> fields = new List<FieldInfo>();
            foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                IgnoreAttribute ignoreAttr = fieldInfo.GetCustomAttribute<IgnoreAttribute>();
                if (ignoreAttr == null)
                    fields.Add(fieldInfo);
            }

            this.getters = new List<Func<object, object>>();
            foreach (FieldInfo fieldInfo in fields)
                getters.Add(BuildGetter(fieldInfo));

            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append('[');
            strBuilder.Append(id);
            strBuilder.Append(',');
            strBuilder.Append($"\"{type.FullName}\"");
            foreach (FieldInfo fieldInfo in fields)
            {
                strBuilder.Append(',');
                strBuilder.Append($"\"{fieldInfo.Name}\"");
            }
            strBuilder.Append(']');
            this.serializedStr = strBuilder.ToString();
        }

        public ulong Id
        {
            get { return id; }
        }

        public IEnumerable<Func<object, object>> Getters
        {
            get
            {
                foreach (Func<object, object> getter in getters)
                    yield return getter;
            }
        }

        public string SerializedString
        {
            get { return serializedStr; }
        }

        private Func<object, object> BuildGetter(MemberInfo memberInfo)
        {
            ParameterExpression exInstance = Expression.Parameter(typeof(object));
            UnaryExpression exConvertToDeclaringType = Expression.Convert(exInstance, memberInfo.DeclaringType);
            MemberExpression exMemberAccess = Expression.MakeMemberAccess(exConvertToDeclaringType, memberInfo);
            UnaryExpression exConvertToObject = Expression.Convert(exMemberAccess, typeof(object));
            Expression<Func<object, object>> lambda = Expression.Lambda<Func<object, object>>(exConvertToObject, exInstance);
            Func<object, object> action = lambda.Compile();

            return action;
        }
    }
}
