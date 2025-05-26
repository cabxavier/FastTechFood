using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FastTechFood.Domain.ValueObjects
{
    public abstract class EntityBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; protected set; }

        protected EntityBase()
        {
            this.Id = Guid.NewGuid();
        }

        public override bool Equals(object? obj)
        {
            if (obj is not EntityBase other) return false;

            if (ReferenceEquals(this, other)) return true;

            if (GetType() != other.GetType()) return false;

            return this.Id == other.Id;
        }

        public static bool operator ==(EntityBase? a, EntityBase? b)
        {
            if (a is null && b is null) return true;

            if (a is null || b is null) return false;

            return a.Equals(b);
        }

        public static bool operator !=(EntityBase? a, EntityBase? b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return (GetType().ToString() + this.Id).GetHashCode();
        }

    }
}
