using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods
{
    public static class MapData
    {
        public static Dictionary<int, List<Vector2i>> BlacklistedTilesByMapID = new Dictionary<int, List<Vector2i>>() 
        {
            { 4, new List<Vector2i>(){
                new Vector2i(45, 2),
                new Vector2i(46, 2)
            } }
        };

        public static bool IsBlacklistedTile(int mapID, Vector2i tilePos)
        {
            List<Vector2i> blacklistedTiles = null;
            if (BlacklistedTilesByMapID.TryGetValue(mapID, out blacklistedTiles))
            {
                foreach (Vector2i blacklistedTile in blacklistedTiles)
                {
                    if (blacklistedTile == tilePos)
                        return true;
                }
            }
            return false;
        }
    }
}
