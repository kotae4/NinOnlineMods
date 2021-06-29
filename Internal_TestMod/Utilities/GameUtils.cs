using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Utilities
{
    public static class GameUtils
    {
        public static Vector2i GetTilePosFromCursor()
        {
            // this displays the tile pos of the cursor
            // modText.RenderText(ref modText.Font[1], Strings.Trim("Mouse X: " + Convert.ToString(modGlobals.CurX) + " Y: " + Convert.ToString(modGlobals.CurY)), 9, num + modText.CharHeight(ref modText.Font[1]), 14, shadow: true, 14, GameWindowForm.Window);
            // modGlobals.CurX and CurY are set like:
            // modGlobals.CurX = modInterfaceEvents.GetMouseXGame() / 32;
            // modGlobals.CurY = modInterfaceEvents.GetMouseYGame() / 32;
            // GetMouseXGame() (both of these calls below are SFML lib calls):
            // Vector2i position = Mouse.GetPosition(modGraphics.GameWindowForm.Window);
            // return (int)modGraphics.GameWindow.MapPixelToCoords(position).X;
            // this displays the absolute screen pos of the cursor (not relevant - just putting it here in case i want to do something with it later)
            // modText.RenderText(ref modText.Font[1], Strings.Trim("X: " + Convert.ToString(modGlobals.GlobalX) + " Y: " + Convert.ToString(modGlobals.GlobalY)), 9, num + modText.CharHeight(ref modText.Font[1]) * 4, 14, shadow: true, 14, GameWindowForm.Window);
            return new Vector2i(client.modGlobals.CurX, client.modGlobals.CurY);
        }

        public enum EPlayerAccessType
        {
            ACCESS_FREE = 0,
            ACCESS_DONOR = 1,
            ACCESS_MEDIA = 3,
            ACCESS_TRAILGM = 4,
            ACCESS_MODERATOR = 2,
            ACCESS_GAMEMASTER = 5,
            ACCESS_GAMEARTIST = 6,
            ACCESS_DEVELOPER = 7,
            ACCESS_ADMIN = 8
        }

    }
}
