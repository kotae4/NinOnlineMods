using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Bot
{
    public static class BotUtils
    {
        public static ECompassDirection GetCompassDirectionFromTo(Vector2i from, Vector2i to)
        {
            Vector2i dir = to - from;
            float maxDot = float.NegativeInfinity;
            int retVal = (int)ECompassDirection.Center;
            for (int index = 0; index < Vector2i.directions_Eight.Length; index++)
            {
                float dot = Vector2i.Dot(dir, Vector2i.directions_Eight[index]);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    retVal = index;
                }
            }
            return (ECompassDirection)retVal;
        }

        public static bool GetNearestMonster(Vector2i from, out client.modTypes.MapNpcRec nearestMonster, out int nearestMonsterIndex)
        {
            Vector2i npcLocation = new Vector2i(0, 0);
            double distance = double.MaxValue;
            double closestDistance = double.MaxValue;
            // would probably be better to return the index instead.
            nearestMonster = null;
            nearestMonsterIndex = 0;
            // NOTE:
            // the game starts the index at 1 for some reason, and also NPC_HighIndex is literally the highest index rather than the count, hence the '<=' comparison
            for (int npcIndex = 1; npcIndex <= client.modGlobals.NPC_HighIndex; npcIndex++)
            {
                npcLocation.x = client.modTypes.MapNpc[npcIndex].X;
                npcLocation.y = client.modTypes.MapNpc[npcIndex].Y;
                if ((npcLocation.x < 0) || (npcLocation.x > client.modTypes.Map.MaxX) ||
                    (npcLocation.y < 0) || (npcLocation.y > client.modTypes.Map.MaxY) ||
					(client.modTypes.MapNpc[npcIndex].num <= 0) || (client.modTypes.MapNpc[npcIndex].num > 255) 
					|| (client.modTypes.MapNpc[npcIndex].Vital[(int)client.modEnumerations.Vitals.HP] <= 0)
					|| (client.modTypes.Npc[client.modTypes.MapNpc[npcIndex].num].Village == client.modTypes.Player[client.modGlobals.MyIndex].Village))
                    continue;

                distance = from.DistanceTo_Squared(npcLocation);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestMonster = client.modTypes.MapNpc[npcIndex];
                    nearestMonsterIndex = npcIndex;
                }
            }
            return nearestMonster != null;
        }

        public static Stack<Vector2i> GetPathToMonster(client.modTypes.MapNpcRec monster, Vector2i fromPos)
        {
            Stack<Vector2i> path = null;
            ECompassDirection idealDir = GetCompassDirectionFromTo(fromPos, new Vector2i(monster.X, monster.Y));
            Vector2i attackingTile = new Vector2i(monster.X + Vector2i.directions_Eight[(int)idealDir].x, monster.Y + Vector2i.directions_Eight[(int)idealDir].y);
            path = Pathfinder.GetPathTo(attackingTile.x, attackingTile.y);
            if (path != null)
            {
                Logger.Log.Write($"Returning early, found ideal attacking tile {attackingTile} from target tile ({monster.X}, {monster.Y})");
                return path;
            }

            foreach (Vector2i dir in Vector2i.directions_Four)
            {
                attackingTile = new Vector2i(monster.X + dir.x, monster.Y + dir.y);
                path = Pathfinder.GetPathTo(attackingTile.x, attackingTile.y);
                if (path != null)
                {
                    Logger.Log.Write($"Returning from loop, found attacking tile {attackingTile} from target tile ({monster.X}, {monster.Y})");
                    return path;
                }
            }
            return path;
        }

		public static void SetTarget(int targetIndex, int targetType = Constants.TARGET_TYPE_NPC)
        {
			client.clsBuffer clsBuffer2 = new client.clsBuffer();
			clsBuffer2.WriteLong(118);
			clsBuffer2.WriteByte((byte)targetType);
			clsBuffer2.WriteLong(targetIndex);
			client.modClientTCP.SendData(clsBuffer2.ToArray());
		}

        public static bool MoveDir(Vector2i tileDirection)
        {
            Vector2i botLocation = GetSelfLocation();
            Vector2i nextTile = new Vector2i(botLocation.x + tileDirection.x, botLocation.y + tileDirection.y);
            byte gameDir = 255;
            for (int index = 0; index < Vector2i.directions_Eight.Length; index++)
                if (tileDirection == Vector2i.directions_Eight[index])
                    gameDir = (byte)index;

            if (gameDir == 255)
            {
                Logger.Log.WriteError($"Could not get direction out of {tileDirection} (self: {botLocation}; nextTile: {nextTile})");
                return false;
            }
            Logger.Log.Write($"Moving bot from {botLocation} to {nextTile} in direction {gameDir} (tileDir: {tileDirection})", Logger.ELogType.Info, null, true);
            // perform next movement
            // set state before sending packet
            client.modTypes.Player[client.modGlobals.MyIndex].Dir = gameDir;
            // NOTE:
            // the game has a value (3) for MOVING_DIAGONAL but doesn't seem to implement it anywhere. in fact, using it will cause movement to break.
            client.modTypes.Player[client.modGlobals.MyIndex].Moving = Constants.MOVING_RUNNING;
            client.modTypes.Player[client.modGlobals.MyIndex].Running = true;
            // send state to server
            client.modClientTCP.SendPlayerMove();
            // client-side prediction (notice how the game's code is literally hundreds of lines to accomplish the exact same thing?)
            // the game uses 143 lines to do JUST this.
            client.modTypes.Player[client.modGlobals.MyIndex].xOffset = System.Math.Abs(tileDirection.x * 32f);
            client.modTypes.Player[client.modGlobals.MyIndex].yOffset = System.Math.Abs(tileDirection.y * 32f);
            client.modTypes.Player[client.modGlobals.MyIndex].X = (byte)(botLocation.x + tileDirection.x);
            client.modTypes.Player[client.modGlobals.MyIndex].Y = (byte)(botLocation.y + tileDirection.y);
            Logger.Log.Write($"Predicted: ({client.modTypes.Player[client.modGlobals.MyIndex].X}, " +
                $"{client.modTypes.Player[client.modGlobals.MyIndex].Y}) (offset: " +
                $"{client.modTypes.Player[client.modGlobals.MyIndex].xOffset}, " +
                $"{client.modTypes.Player[client.modGlobals.MyIndex].yOffset})");
            return true;
        }

        public static bool FaceDir(Vector2i tileDirection)
        {
            byte gameDir = 255;
            for (int index = 0; index < Vector2i.directions_Eight.Length; index++)
                if (tileDirection == Vector2i.directions_Eight[index])
                    gameDir = (byte)index;

            if (gameDir == 255)
            {
                // NOTE:
                // TO-DO:
                // logging inconsistency. normally log message would be here, instead it's left to the caller.
                return false;
            }
            if (gameDir == client.modTypes.Player[client.modGlobals.MyIndex].Dir)
            {
                Logger.Log.Write("Bot is already facing target, no need to send dir packet");
            }
            else
            {
                Logger.Log.Write($"Setting bot to face target (was {client.modTypes.Player[client.modGlobals.MyIndex].Dir} now {gameDir})");
                client.modTypes.Player[client.modGlobals.MyIndex].Dir = gameDir;
                client.clsBuffer clsBuffer2 = new client.clsBuffer();
                clsBuffer2.WriteLong(18);
                clsBuffer2.WriteLong(gameDir);
                client.modClientTCP.SendData(clsBuffer2.ToArray());
            }
            return true;
        }

        public static void BasicAttack()
        {
			// TO-DO:
			// send animation too (to fix the long-standing speedhack... no one's noticed yet, though)
            client.clsBuffer clsBuffer2 = new client.clsBuffer();
            clsBuffer2.WriteLong(20);
            client.modClientTCP.SendData(clsBuffer2.ToArray());
            client.modGlobals.TimeSinceAttack = (int)client.modGlobals.Tick;
        }

        public static void CollectItem()
        {
            client.clsBuffer clsBuffer2 = new client.clsBuffer();
            clsBuffer2.WriteLong(31);
            client.modClientTCP.SendData(clsBuffer2.ToArray());
        }

		// NOTE:
		// this only needs to be called once
		public static void ChargeChakra()
        {
			// NOTE:
			// the game does this even if it's not currently casting anything
			client.modClientTCP.BreakSpell();

			client.modTypes.PlayerRec bot = GetSelf();
			if (bot.ChargeTimer == 0)
			{
				bot.ChargeTimer = (int)client.modGlobals.Tick;
			}
			bot.ChargeChakra = true;
			client.clsBuffer clsBuffer2 = new client.clsBuffer();
			clsBuffer2.WriteLong(104);
			client.modClientTCP.SendData(clsBuffer2.ToArray());
		}

		// NOTE:
		// this takes the index into the modGlobals.PlayerSpells array, *NOT* the spellID (aka the index into the modTypes.Spell array)
		public static void CastSpell(int spellIndex)
        {
			int spellID = client.modGlobals.PlayerSpells[spellIndex];
			client.modTypes.SpellRec spell = client.modTypes.Spell[spellID];
			if (spell.CastTime == 0 && spell.EndCastDuration > 0)
			{
				client.modGlobals.StunDuration = spell.EndCastDuration;
				client.modGlobals.StunTimer = (int)(client.modGlobals.Tick + 100);
			}
			client.modClientTCP.SendCast((byte)spellIndex);
		}

		// NOTE:
		// modGameLogic.CanInteract() - this is used by CheckAttack() and CheckCharge()
		// because we have to set modGlobals.StunDuration to get chakra charging working, we need to modify this to ignore StunDuration.
		// basically, we'll use this in our own CanChargeChakra() but we should use the game's modGameLogic.CanInteract() in all other cases.
		public static bool CanInteract(bool Ignore = false)
        {
			if ((client.modGlobals.InEvent) || (client.modGlobals.InNpcChat) || (client.modTypes.Player[client.modGlobals.MyIndex].DeathTimer > 0) ||
				(client.modGlobals.InBank) || (client.modGlobals.InShop > 0) || (client.modGlobals.InTrade > 0) || (client.modGlobals.InSpecialShop) ||
				(!Ignore && client.modGlobals.SpellBuffer > 0))
			{
				return false;
			}
			return true;
		}

        public static bool CanMove()
        {
            if (client.modGlobals.tmr25 >= client.modGlobals.Tick)
            {
                Logger.Log.Write($"Skipping frame because tmr25 isn't ready yet ({client.modGlobals.tmr25} > {client.modGlobals.Tick})");
                return false;
            }
            // NOTE:
            // taken from client.modGameLogic.CheckMovement()
            if ((client.modTypes.Player[client.modGlobals.MyIndex].Moving > 0) || (client.modTypes.Player[client.modGlobals.MyIndex].DeathTimer > 0))
            {
                Logger.Log.Write($"Skipping frame because player is in invalid state ({client.modTypes.Player[client.modGlobals.MyIndex].Moving}, {client.modTypes.Player[client.modGlobals.MyIndex].DeathTimer})");
                return false;
            }
            return true;
        }

        public static bool CanAttack()
        {
            if (client.modGlobals.tmr25 >= client.modGlobals.Tick)
                return false;

            int playerAttackSpeed = client.modDatabase.GetPlayerAttackSpeed(client.modGlobals.MyIndex);
            int nextAttackTime = client.modGlobals.TimeSinceAttack + playerAttackSpeed + 30;
            // NOTE:
            // we ignore some things because we can be reasonably sure the bot won't be in that state (like whether certain menus are open, etc)
            // taken from client.modGameLogic.CheckAttack()
            if ((nextAttackTime > client.modGlobals.Tick) || (client.modGlobals.SpellBuffer > 0) || (client.modGameLogic.CanPlayerInteract() == false))
                return false;
            if (client.modTypes.Player[client.modGlobals.MyIndex].EventTimer > client.modGlobals.Tick)
                return false;

            return true;
        }

		public static bool CanChargeChakra()
        {
			client.modTypes.PlayerRec bot = GetSelf();
			if (bot.ChargeChakra == true)
            {
				Logger.Log.Write("Cannot charge chakra because we're already charging chakra", Logger.ELogType.Error);
				return false;
            }
			if ((bot.Village != 3) && (bot.Village != 13) && (client.modTypes.Map.Tile[bot.X, bot.Y].Type == Constants.TILE_TYPE_WATER))
			{
				Logger.Log.Write($"Cannot charge chakra in water (botLoc ({bot.X}, {bot.Y}), botVillage {bot.Village}, tileType {client.modTypes.Map.Tile[bot.X, bot.Y].Type})", Logger.ELogType.Error);
				return false;
			}
			if (((double)(bot.ChargeTimer + 500) - (double)bot.Stat[5] * 0.2 * 5.0) > (double)client.modGlobals.Tick)
            {
				Logger.Log.Write($"Cannot charge chakra while on cooldown (chargeTimer {bot.ChargeTimer}, effectiveTimer {((double)(bot.ChargeTimer + 500) - (double)bot.Stat[5] * 0.2 * 5.0)}, tick {(double)client.modGlobals.Tick})", Logger.ELogType.Error);
				return false;
			}
			if (client.modGameLogic.CanPlayerInteract(Ignore:true) == false)
            {
				Logger.Log.Write("Cannot charge chakra while in a menu or CC'd", Logger.ELogType.Error);
				return false;
			}
			if (bot.Vital[(int)client.modEnumerations.Vitals.MP] == bot.MaxVital[(int)client.modEnumerations.Vitals.MP])
            {
				Logger.Log.Write($"Cannot charge chakra because it's already full ({bot.Vital[(int)client.modEnumerations.Vitals.MP]} / {bot.MaxVital[(int)client.modEnumerations.Vitals.MP]})", Logger.ELogType.Error);
				return false;
			}
			return true;
		}

		// NOTE:
		// this takes the index into the modGlobals.PlayerSpells array, *NOT* the spellID (aka the index into the modTypes.Spell array)
		public static bool CanCastSpell(int spellIndex)
		{
			// oh boy this is a long one... if the dev could change modGameLogic.CastSpell(...) to return a boolean things would be so much easier.
			bool clearedTarget = false;
			client.modTypes.PlayerRec bot = GetSelf();
			Vector2i botPos = GetSelfLocation();
			if (client.modGlobals.GettingMap || spellIndex < 1 || spellIndex > 40 || client.modGlobals.SpellBuffer > 0 || client.modGlobals.StunDuration > 0 || client.modGlobals.PlayerSpells[spellIndex] <= 0 || client.modTypes.Player[client.modGlobals.MyIndex].DeathTimer > 0 || client.modGlobals.SilenceTimer > 0)
			{
				return false;
			}
			int spellID = client.modGlobals.PlayerSpells[spellIndex];
			client.modTypes.SpellRec spell = client.modTypes.Spell[spellID];
			if (client.modTypes.Map.Tile[bot.X, bot.Y].Type == 18)
			{
				return false;
			}
			if (client.modGlobals.SpellCD[spellIndex] > 0)
			{
				return false;
			}
			if (spell.MPCost > bot.Vital[(int)client.modEnumerations.Vitals.MP])
			{
				return false;
			}
			// TO-DO:
			// requires myTargetType and myTarget to be set prior to calling this
			// checks if trying to cast on a player from a certain village (some spells can only be cast on members of certain villages?)
			if (spell.TargetVillageReq > 0 && client.modGlobals.myTargetType == 1 && client.modTypes.Player[client.modGlobals.myTarget].Village != spell.TargetVillageReq)
			{
				return false;
			}
			if (bot.Access < 7)
			{
				if (spell.LevelReq > bot.Level)
				{
					return false;
				}
				if (spell.ClassReq > 0 && spell.ClassReq != bot.Class)
				{
					return false;
				}
				if (spell.VillageReq > 0 && bot.Village != spell.VillageReq)
				{
					return false;
				}
				if (spell.AccessReq > bot.Access)
				{
					return false;
				}
				// NOTE:
				// 'element' refers to elemental type masteries, i think (fire, water, wind, earth, lightning)
				for (int elementIndex = 1; elementIndex <= 5; elementIndex++)
				{
					if (spell.Element[elementIndex] > 0 && bot.Element[elementIndex] != spell.Element[elementIndex])
					{
						return false;
					}
				}
				// NOTE:
				// 'nonelement' refers to the non-elemental type masters (medical, weapon mastery, taijutsu)
				for (int nonElementalIndex = 1; nonElementalIndex <= 3; nonElementalIndex++)
				{
					if (spell.NonElement[nonElementalIndex] > 0 && bot.NonElement[nonElementalIndex] != spell.NonElement[nonElementalIndex])
					{
						return false;
					}
				}
			}
			// now we start checking actual spell logic. range, type of spell (buff, aoe, etc), and what direction we're facing when we cast it
			byte maybeCastType = 0;
			// NOTE:
			// this is the worst ternary operator i've ever seen. never start a ternary operator with a false conditional, wtf? literally inverts the entire expression chain for no reason.
			// b = (byte)((!modTypes.Spell[modGlobals.PlayerSpells[SpellSlot]].IsDirectional) ? ((modTypes.Spell[modGlobals.PlayerSpells[SpellSlot]].Range > 0) ? (modTypes.Spell[modGlobals.PlayerSpells[SpellSlot]].IsAoE ? 3 : 2) : (modTypes.Spell[modGlobals.PlayerSpells[SpellSlot]].IsAoE ? 1 : 0)) : (modTypes.Spell[modGlobals.PlayerSpells[SpellSlot]].IsAoE ? 5 : 4));
			// alright, the above ternary is just so convoluted i'm going to expand it for clarity and my sanity
			if (spell.IsDirectional)
            {
				if (spell.IsAoE)
					maybeCastType = 5;
				else 
					maybeCastType = 4;
			}
			else
            {
				if (spell.Range > 0)
                {
					if (spell.IsAoE)
						maybeCastType = 3;
					else
						maybeCastType = 2;
                }
				else
                {
					if (spell.IsAoE)
						maybeCastType = 1;
					else
						maybeCastType = 0;
                }
			}
			// ^^^^ infinitely easier to see exactly what's happening, isn't it?
			if (spell.Type == 4)
			{
				maybeCastType = 0;
			}
			switch (maybeCastType)
			{
				case 2:
				case 3:
					// uhhh... they are checking the BuffTexture for uhhh... i dunno, but i'll do it too!
					// NOTE:
					// pretty sure these shouldn't be &&'s but it's how the game does it, so i'm keeping it
					if ((client.modGlobals.myTarget == 0) && (client.modGlobals.SelfCastKeyDown == false) && (spell.Buff.BuffTexture == 0))
					{
						return false;
					}
					break;
				case 4:
					{
						// optimized away 18 calls and 38 lines of code...
						bool hasDirectionalTarget = false;
						Vector2i spellHitPos = botPos + Vector2i.directions_Eight[bot.Dir];
						// (game only supports 300 players online at once?)
						// NOTE:
						// checks all possible players to see if they're on the tile our spell is targeting... and then sets our myTarget to 1 instead of their playerIndex because of a bug..
						for (int i = 1; i <= 300; i++)
						{
							if (client.modClientTCP.IsPlaying(i) && client.modGlobals.MyIndex != i && client.modDatabase.GetPlayerMap(i) == bot.Map && client.modDatabase.GetPlayerX(i) == spellHitPos.x && client.modDatabase.GetPlayerY(i) == spellHitPos.y)
							{
								client.modGlobals.myTarget = i;
								// NOTE:
								// game code has this as 'myTarget = 1', an obvious bug. changing it here proactively because i expect it to be fixed in the client eventually.
								client.modGlobals.myTargetType = Constants.TARGET_TYPE_PLAYER;
								hasDirectionalTarget = true;
								break;
							}
						}
						// NOTE:
						// if we haven't found a player target, then start looking for NPC targets too using the same logic
						if (!hasDirectionalTarget)
						{
							for (int i = 1; i <= client.modGlobals.NPC_HighIndex; i++)
							{
								if (client.modTypes.MapNpc[i].num > 0 && client.modTypes.MapNpc[i].num <= 255 && client.modTypes.MapNpc[i].X == spellHitPos.x && client.modTypes.MapNpc[i].Y == spellHitPos.y)
								{
									client.modGlobals.myTarget = i;
									client.modGlobals.myTargetType = Constants.TARGET_TYPE_NPC;
									hasDirectionalTarget = true;
									break;
								}
							}
						}
						if (!hasDirectionalTarget)
						{
							return false;
						}
						break;
					}
			}
			if (client.modGlobals.myTargetType == Constants.TARGET_TYPE_CLONE)
			{
				// NOTE:
				// can't buff the hp or mp of clones
				if (spell.Range > 0 && ((spell.Buff.BuffType == 1) || (spell.Buff.BuffType == 2)))
				{
					return false;
				}
			}
			else if (client.modGlobals.myTargetType == Constants.TARGET_TYPE_PLAYER)
			{
				if (spell.Type != Constants.SPELL_TYPE_REVIVE && (client.modTypes.Player[client.modGlobals.myTarget].DeathTimer > 0 || client.modDatabase.GetPlayerVital(client.modGlobals.myTarget, client.modEnumerations.Vitals.HP) == 0))
				{
					// NOTE:
					// the game clears the target here but doesn't return. i'm going to make note of the cleared target w/out actually clearing it
					clearedTarget = true;
				}
				// spell.Type == 5 seems to be revival type spells
				if (spell.Range > 0 && spell.Type == Constants.SPELL_TYPE_REVIVE)
				{
					if (client.modGlobals.myTarget == client.modGlobals.MyIndex)
					{
						return false;
					}
					// checks if the target is still alive
					if ((client.modTypes.Player[client.modGlobals.myTarget].DeathTimer < 1) && (client.modTypes.Player[client.modGlobals.myTarget].Vital[(int)client.modEnumerations.Vitals.HP] != 0))
					{
						return false;
					}
				}
			}
			if (((bot.CastingSpell <= 0) || (client.modTypes.Spell[bot.CastingSpell].CastTime <= 0)) && 
				((spell.Type != Constants.SPELL_TYPE_WARP) || ((client.modGlobals.myTarget != 0) && (client.modGlobals.myTargetType == Constants.TARGET_TYPE_PLAYER) && (clearedTarget == false))))
			{
				if (spell.WalkCast)
				{
					return true;
					
				}
				else if (bot.Moving == 0)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			return false;
		}

        public static client.modTypes.PlayerRec GetSelf()
        {
            // TO-DO:
            // add sanity checks (checking if we're in-game and our player has fully loaded)
            return client.modTypes.Player[client.modGlobals.MyIndex];
        }

        public static Vector2i GetSelfLocation()
        {
            // TO-DO:
            // add sanity checks (checking if we're in-game and our player has fully loaded)
            return new Vector2i(client.modTypes.Player[client.modGlobals.MyIndex].X, client.modTypes.Player[client.modGlobals.MyIndex].Y);
        }

		public static List<Vector2i> GetAllTilesMatchingPredicate(Func<Vector2i, bool> predicate)
		{
			List<Vector2i> matchingTiles = new List<Vector2i>();
			client.modTypes.MapRec currentMap = client.modTypes.Map;
			int tileLengthX = currentMap.Tile.GetLength(0);
			int tileLengthY = currentMap.Tile.GetLength(1);
			Vector2i tilePos = new Vector2i();
			for (int tileX = 0; tileX < tileLengthX; tileX++)
			{
				for (int tileY = 0; tileY < tileLengthY; tileY++)
				{
					tilePos.x = tileX;
					tilePos.y = tileY;
					if (predicate(tilePos))
                    {
						matchingTiles.Add(new Vector2i(tileX, tileY));
                    }
				}
			}
			return matchingTiles;
		}
    }
}
