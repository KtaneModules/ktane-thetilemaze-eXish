using System;

namespace TheTileMaze
{
    struct Tile : IEquatable<Tile>
    {
        public TileShape Shape { get; private set; }
        public int? Number { get; private set; }
        public Tile(TileShape shape, int? number) { Shape = shape; Number = number; }

        public float Rotation { get { return Shape.Rotation(); } }
        public bool IsOpenAt(Direction direction) { return Shape.IsOpenAt(direction); }
        public bool IsL { get { return Shape.IsL(); } }
        public bool IsT { get { return Shape.IsT(); } }
        public bool IsStraight { get { return Shape.IsStraight(); } }

        public Tile Rotate(int amount) { return new Tile(Shape.Rotate(amount), Number); }

        public char Char { get { return "╚╔╗╝╦╣╩╠║═"[(int) Shape]; } }

        public bool Equals(Tile other) { return other.Shape == Shape && other.Number == Number; }
        public override int GetHashCode() { return (int) Shape * 31 + (Number ?? 10); }
    }
}
