using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods
{
    public enum ECompassDirection { North, South, East, West, NorthEast, NorthWest, SouthEast, SouthWest, Center }

    // Author: dogfuntom, Kevin Reid, John Amanatides and Andrew Woo
    // https://gamedev.stackexchange.com/questions/47362/cast-ray-to-select-block-in-voxel-game?rq=1
    // https://gist.github.com/dogfuntom/cc881c8fc86ad43d55d8
    public struct Vector2i : IEquatable<Vector2i>
    {
        public static readonly Vector2i zero = new Vector2i(0, 0);
        public static readonly Vector2i one = new Vector2i(1, 1);

        public static readonly Vector2i up = new Vector2i(0, -1);
        public static readonly Vector2i down = new Vector2i(0, 1);
        public static readonly Vector2i left = new Vector2i(-1, 0);
        public static readonly Vector2i right = new Vector2i(1, 0);
        // diagonals
        public static readonly Vector2i upLeft = new Vector2i(-1, -1);
        public static readonly Vector2i upRight = new Vector2i(1, -1);
        public static readonly Vector2i downLeft = new Vector2i(-1, 1);
        public static readonly Vector2i downRight = new Vector2i(1, 1);
        // ordered by cardinal directiosn on compass (N, S, E, W then NE, NW, SE, SW)
        public static readonly Vector2i[] directions_Eight = new Vector2i[] {
            up, down, left, right,
            upLeft, upRight, downLeft, downRight
        };
        // ordered by cardinal directiosn on compass (N, S, E, W)
        public static readonly Vector2i[] directions_Four = new Vector2i[] {
            up, down, left, right
        };

        public int x, y;

        public Vector2i(int x, int y)
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
            return obj is Vector2i location && x == location.x && y == location.y;
        }

        public bool Equals(Vector2i other)
        {
            return x == other.x &&
                   y == other.y;
        }

        public override int GetHashCode()
        {
            var hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Vector2i left, Vector2i right)
        {
            return left.x == right.x && left.y == right.y;
        }

        public static bool operator !=(Vector2i left, Vector2i right)
        {
            return left.x != right.x || left.y != right.y;
        }

        public static Vector2i operator /(Vector2i v, int factor)
        {
            return new Vector2i(v.x / factor, v.y / factor);
        }

        public static Vector2i operator *(Vector2i v, int factor)
        {
            return new Vector2i(v.x * factor, v.y * factor);
        }

        public static Vector2i operator -(Vector2i a, Vector2i b)
        {
            return new Vector2i(a.x - b.x, a.y - b.y);
        }

        public static Vector2i operator +(Vector2i a, Vector2i b)
        {
            return new Vector2i(a.x + b.x, a.y + b.y);
        }

        public static float Dot(Vector2i left, Vector2i right)
        {
            return ((left.x * right.x) + (left.y * right.y));
        }

        public double DistanceTo_Squared(Vector2i other)
        {
            return ((this.x - other.x) * (this.x - other.x)) + ((this.y - other.y) * (this.y - other.y));
            // equivalent (but slightly more expensive due to the calls)
            //return System.Math.Pow((this.x - other.x), 2) + System.Math.Pow((this.y - other.y), 2);
        }

        public double DistanceTo(Vector2i other)
        {
            return System.Math.Sqrt(((this.x - other.x) * (this.x - other.x)) + ((this.y - other.y) * (this.y - other.y)));
            // equivalent (but slightly more expensive due to the calls)
            //return System.Math.Sqrt(System.Math.Pow((this.x - other.x), 2) + System.Math.Pow((this.y - other.y), 2));
        }

        /*
        public static explicit operator Location(Vector2i v)
        {
            return new Location(v.x, v.y);
        }
        */
    }
}
