using System.Collections.Generic;

namespace TheTileMaze
{
    static class TileExtensions
    {
        public static TileShape Clockwise(this TileShape tile)
        {
            var tileInt = (int) tile;
            return (TileShape) ((tileInt & ~3) | ((tileInt % 4 + 1) % 4));
        }

        public static TileShape Rotate(this TileShape tile, int amount)
        {
            var t = tile;
            for (var i = 0; i < amount; i++)
                t = t.Clockwise();
            return t;
        }

        public static float Rotation(this TileShape tile) { return 90f * ((int) tile & 3); }

        private static readonly Dictionary<TileShape, bool[]> _tileOpen = new Dictionary<TileShape, bool[]>
        {
            { TileShape.NE, new[] { true, true, false, false } },
            { TileShape.ES, new[] { false, true, true, false } },
            { TileShape.SW, new[] { false, false, true, true } },
            { TileShape.NW, new[] { true, false, false, true } },
            { TileShape.ESW, new[] { false, true, true, true } },
            { TileShape.SWN, new[] { true, false, true, true } },
            { TileShape.NEW, new[] { true, true, false, true } },
            { TileShape.NES, new[] { true, true, true, false } },
            { TileShape.NS, new[] { true, false, true, false } },
            { TileShape.EW, new[] { false, true, false, true } },
            { TileShape.SN, new[] { true, false, true, false } },
            { TileShape.WE, new[] { false, true, false, true } }
        };

        public static bool IsOpenAt(this TileShape tile, Direction direction) { return _tileOpen[tile][(int) direction]; }

        public static bool IsL(this TileShape tile) { return ((int) tile >> 2) == 0; }
        public static bool IsT(this TileShape tile) { return ((int) tile >> 2) == 1; }
        public static bool IsStraight(this TileShape tile) { return ((int) tile >> 2) == 2; }
    }
}
