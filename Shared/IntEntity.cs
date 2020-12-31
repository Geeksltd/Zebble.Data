namespace Zebble.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.ComponentModel;
    using System.Linq;
    using Zebble.Data;

    public class IntEntity : Entity<int>
    {
        bool IsIdLoaded = false;
        int id;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Func<Type, int> NewIdGenerator = DefaultNewIdGenerator;

        /// <summary>
        /// Gets a unique Identifier for this instance. In the database, this will be the primary key of this object.
        /// </summary>
        public override int ID
        {
            get
            {
                if (IsIdLoaded) return id;
                else
                {
                    id = NewIdGenerator(GetType());
                    IsIdLoaded = true;
                    return id;
                }
            }
            set
            {
                id = value;
                IsIdLoaded = true;
            }
        }

        static object InitializeIdLock = new object();

        static ConcurrentDictionary<Type, int> LastUsedIds = new ConcurrentDictionary<Type, int>();

        static int DefaultNewIdGenerator(Type type)
        {
            if (type.BaseType != typeof(IntEntity))
                return DefaultNewIdGenerator(type.BaseType);

            int initialize(Type t)
            {
                if (TransientEntityAttribute.IsTransient(t)) return 1;

                return Database.GetList(t, new[] { QueryOption.Take(1), QueryOption.OrderByDescending("ID") })
                    .FirstOrDefault()?.Get(x => (int)x.GetId() + 1) ?? 1;
            }

            return LastUsedIds.AddOrUpdate(type, initialize, (t, old) => old + 1);
        }

        public static bool operator !=(IntEntity entity, int? id) => entity?.ID != id;

        public static bool operator ==(IntEntity entity, int? id) => entity?.ID == id;

        public static bool operator !=(IntEntity entity, int id) => entity?.ID != id;

        public static bool operator ==(IntEntity entity, int id) => entity?.ID == id;

        public static bool operator !=(int? id, IntEntity entity) => entity?.ID != id;

        public static bool operator ==(int? id, IntEntity entity) => entity?.ID == id;

        public static bool operator !=(int id, IntEntity entity) => entity?.ID != id;

        public static bool operator ==(int id, IntEntity entity) => entity?.ID == id;

        public override bool Equals(Entity other) => GetType() == other?.GetType() && ID == (other as IntEntity)?.ID;

        public override bool Equals(object other) => Equals(other as Entity);

        public override int GetHashCode() => ID.GetHashCode();
    }
}