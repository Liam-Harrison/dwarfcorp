﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp.Gui
{
    public enum JsonTileSheetType
    {
        VariableWidthFont,
        TileSheet,
        Generated,
        JsonFont
    }

    public class JsonTileSheet
    {
        public String Name;
        public String Texture;
        public int TileWidth;
        public int TileHeight;
        public JsonTileSheetType Type = JsonTileSheetType.TileSheet;
        public bool RepeatWhenUsedAsBorder = false;
    }
}
