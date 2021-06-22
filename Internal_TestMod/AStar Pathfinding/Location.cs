using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Pathfinding
{
    public struct Location
    {
        // Implementation notes: I am using the default Equals but it can
        // be slow. You'll probably want to override both Equals and
        // GetHashCode in a real project.
        public readonly int x, y;
        public Location(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return string.Format("({0} {1})", x, y);
        }

        public override bool Equals(object obj)
        {
            // NOTE:
            // "obj is Location location"
            // this is a neat little thing in C#7, it effectively replaces the 'as' operator.
            // it's even cooler written like this because the && operators mean that if obj is NOT a Location type then the eval expression immediately exits and returns false
            // without hitting the location.x and causing an exception. programming is fun.
            return obj is Location location && x == location.x && y == location.y;
        }

        public override int GetHashCode()
        {
            var hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Location left, Location right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Location left, Location right)
        {
            return !(left == right);
        }

        public static explicit operator Location(Vector2i v)
        {
            return new Location(v.x, v.y);
        }
    }
}
