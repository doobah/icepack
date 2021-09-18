using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    internal class SerializationContext
    {
        private Dictionary<object, ulong> instanceIds;
        private ulong largestInstanceId;
        private HashSet<Type> usedTypes;
        private Queue<object> objectsToSerialize;

        public SerializationContext()
        {
            this.instanceIds = new Dictionary<object, ulong>();
            this.largestInstanceId = Toolbox.NULL_ID;
            this.usedTypes = new HashSet<Type>();
            this.objectsToSerialize = new Queue<object>();
        }

        public ulong GetInstanceId(object obj)
        {
            if (instanceIds.ContainsKey(obj))
                return instanceIds[obj];

            return Toolbox.NULL_ID;
        }

        public HashSet<Type> UsedTypes
        {
            get { return usedTypes; }
        }

        public Queue<object> ObjectsToSerialize
        {
            get { return objectsToSerialize; }
        }

        public ulong RegisterObject(object obj)
        {
            ulong newId = ++largestInstanceId;
            instanceIds.Add(obj, newId);

            return newId;
        }

        public bool IsObjectRegistered(object obj)
        {
            return instanceIds.ContainsKey(obj);
        }
    }
}
