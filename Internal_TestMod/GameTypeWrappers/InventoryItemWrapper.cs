using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.GameTypeWrappers
{
    public class InventoryItemWrapper : IEquatable<InventoryItemWrapper>
    {
        public client.modTypes.PlayerInvRec invItem;
        public int invItemIndex;

        public InventoryItemWrapper(client.modTypes.PlayerInvRec _invItem, int _invItemIndex)
        {
            invItem = _invItem;
            invItemIndex = _invItemIndex;
        }

        // NOTE:
        // mvalue is stack count
        // num is its index into modTypes.Item[] (which is of type ItemRec, containing the non-changing data associated with an item, like its name, type, level requirement, etc)
        // num of 1 is ryo, hardcoded
        public bool Equals(InventoryItemWrapper other)
        {
            if (Object.ReferenceEquals(this, other))
                return true;

            return ((this.invItem.num == other.invItem.num)
                && (this.invItem.mvalue == other.invItem.mvalue));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as InventoryItemWrapper);
        }

        public override int GetHashCode()
        {
            var hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + invItem.num.GetHashCode();
            hashCode = hashCode * -1521134295 + invItem.mvalue.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(InventoryItemWrapper lhs, InventoryItemWrapper rhs)
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

        public static bool operator !=(InventoryItemWrapper lhs, InventoryItemWrapper rhs)
        {
            return !(lhs == rhs);
        }
    }
}
