using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.GameTypeWrappers
{
    public class MapItemWrapper : IEquatable<MapItemWrapper>
    {
        public client.modTypes.MapItemRec mapItem;
        public int mapItemIndex;

        public MapItemWrapper(client.modTypes.MapItemRec _mapItem, int _mapItemIndex)
        {
            mapItem = _mapItem;
            mapItemIndex = _mapItemIndex;
        }
        // NOTE:
        // mvalue is stack count
        // num is its index into modTypes.Item[] (which is of type ItemRec, containing the non-changing data associated with an item, like its name, type, level requirement, etc)
        // num of 1 is ryo, hardcoded
        public bool Equals(MapItemWrapper other)
        {
            if (Object.ReferenceEquals(this, other))
                return true;

            return ((this.mapItem.X == other.mapItem.X) && (this.mapItem.Y == other.mapItem.Y)
                && (this.mapItem.num == other.mapItem.num)
                && (this.mapItem.mvalue == other.mapItem.mvalue));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as MapItemWrapper);
        }

        public override int GetHashCode()
        {
            var hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + mapItem.X.GetHashCode();
            hashCode = hashCode * -1521134295 + mapItem.Y.GetHashCode();
            hashCode = hashCode * -1521134295 + mapItem.num.GetHashCode();
            hashCode = hashCode * -1521134295 + mapItem.mvalue.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(MapItemWrapper lhs, MapItemWrapper rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(MapItemWrapper lhs, MapItemWrapper rhs)
        {
            return !(lhs == rhs);
        }
    }
}
