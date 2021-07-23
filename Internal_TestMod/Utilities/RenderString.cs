using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods.Utilities
{
    public struct RenderString
    {
        public string Message;
        public int Width;
        public int Height;
        private byte _FontSize;

        public byte FontSize { 
            get { return _FontSize; } 
            set { if (_FontSize != value) { _FontSize = value; RecalculateMessageSize(); } }
        }

        public RenderString(string msg, byte fontSize = 13)
        {
            Message = msg;
            _FontSize = fontSize;

            client.modText.CachedFontTextObjects[client.modText.Font[1]].DisplayedString = Message;
            client.modText.CachedFontTextObjects[client.modText.Font[1]].CharacterSize = (uint)_FontSize;
            Height = (int)client.modText.CachedFontTextObjects[client.modText.Font[1]].GetLocalBounds().Height;
            Width = (int)client.modText.CachedFontTextObjects[client.modText.Font[1]].GetLocalBounds().Width;
        }

        private void RecalculateMessageSize()
        {
            client.modText.CachedFontTextObjects[client.modText.Font[1]].DisplayedString = Message;
            client.modText.CachedFontTextObjects[client.modText.Font[1]].CharacterSize = (uint)_FontSize;
            Height = (int)client.modText.CachedFontTextObjects[client.modText.Font[1]].GetLocalBounds().Height;
            Width = (int)client.modText.CachedFontTextObjects[client.modText.Font[1]].GetLocalBounds().Width;
        }
    }
}
