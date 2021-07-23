using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.GameTypeWrappers
{
    public class MapNpcWrapper : IEquatable<MapNpcWrapper>
    {
        public client.modTypes.MapNpcRec mapNpc;
        public int mapNpcIndex;

        public MapNpcWrapper(client.modTypes.MapNpcRec _mapNpc, int _mapNpcIndex)
        {
            mapNpc = _mapNpc;
            mapNpcIndex = _mapNpcIndex;
        }

        // NOTE:
        // num is its index into modTypes.Npc[] (which is of type NpcRec, containing the non-changing data associated with an NPC, like its name, level, sprite, etc)
        public bool Equals(MapNpcWrapper other)
        {
            if (Object.ReferenceEquals(this, other))
                return true;

            return ((this.mapNpc.X == other.mapNpc.X) && (this.mapNpc.Y == other.mapNpc.Y)
                && (this.mapNpc.num == other.mapNpc.num)
                && (this.mapNpc.Dir == other.mapNpc.Dir)
                && (this.mapNpc.isClone == other.mapNpc.isClone)
                && (this.mapNpc.LastMoving == other.mapNpc.LastMoving));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as MapNpcWrapper);
        }

        public override int GetHashCode()
        {
            var hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + mapNpc.X.GetHashCode();
            hashCode = hashCode * -1521134295 + mapNpc.Y.GetHashCode();
            hashCode = hashCode * -1521134295 + mapNpc.num.GetHashCode();
            hashCode = hashCode * -1521134295 + mapNpc.Dir.GetHashCode();
            hashCode = hashCode * -1521134295 + mapNpc.isClone.GetHashCode();
            hashCode = hashCode * -1521134295 + mapNpc.LastMoving.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(MapNpcWrapper lhs, MapNpcWrapper rhs)
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

        public static bool operator !=(MapNpcWrapper lhs, MapNpcWrapper rhs)
        {
            return !(lhs == rhs);
        }
    }
}
