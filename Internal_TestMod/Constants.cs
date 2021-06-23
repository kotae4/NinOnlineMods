using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods
{
    public static class Constants
    {
		// === MAP TILES ===
		public const byte TILE_TYPE_WALKABLE = 0;

		public const byte TILE_TYPE_BLOCKED = 1;

		public const byte TILE_TYPE_WARP = 2;

		public const byte TILE_TYPE_ITEM = 3;

		public const byte TILE_TYPE_NPCAVOID = 4;

		public const byte TILE_TYPE_CHECKPOINT = 5;

		public const byte TILE_TYPE_RESOURCE = 7;

		public const byte TILE_TYPE_NPCSPAWN = 9;

		public const byte TILE_TYPE_SHOP = 10;

		public const byte TILE_TYPE_HOUSE = 11;

		public const byte TILE_TYPE_HEAL = 12;

		public const byte TILE_TYPE_TRAP = 13;

		public const byte TILE_TYPE_SLIDE = 14;

		public const byte TILE_TYPE_SOUND = 15;

		public const byte TILE_TYPE_PLAYERSPAWN = 16;

		public const byte TILE_TYPE_WATER = 17;

		public const byte TILE_TYPE_NOJUTSU = 18;

		public const byte TILE_TYPE_NOWARP = 19;

		public const byte TILE_TYPE_FIRE = 20;

		public const byte TILE_TYPE_THROUGH = 21;

		public const byte TILE_TYPE_NOTRAP = 22;

		public const byte TILE_TYPE_SIT = 23;

		// === MOVEMENT === 
		public const byte DIR_UP = 0;

		public const byte DIR_DOWN = 1;

		public const byte DIR_LEFT = 2;

		public const byte DIR_RIGHT = 3;

		public const byte DIR_UPLEFT = 4;

		public const byte DIR_UPRIGHT = 5;

		public const byte DIR_DOWNLEFT = 6;

		public const byte DIR_DOWNRIGHT = 7;

		public const byte MOVING_WALKING = 1;

		public const byte MOVING_RUNNING = 2;

		public const byte MOVING_DIAGONAL = 3;

		public const byte MOVING_KICKBACK = 4;
	}
}
