using System;
using System.Linq;

namespace TheTileMaze
{
    abstract class GameAction
    {
        private static GameAction[] _all;
        public static GameAction[] All { get { return _all ?? (_all = SetStart.AllStarts.Concat<GameAction>(Move.AllMoves).Concat(Rotate.AllRotations).Concat(Shove.AllShoves).ToArray()); } }
    }

    sealed class SetStart : GameAction
    {
        public int TilePosition { get; private set; }
        public string Name { get; private set; }
        private SetStart(int tilePosition, string name) { TilePosition = tilePosition; Name = name; }
        public static SetStart TopLeft = new SetStart(0, "top-left");
        public static SetStart TopRight = new SetStart(6, "top-right");
        public static SetStart BottomLeft = new SetStart(6 * 7, "bottom-left");
        public static SetStart BottomRight = new SetStart(6 * 7 + 6, "bottom-right");
        public static SetStart[] AllStarts = new[] { TopLeft, TopRight, BottomLeft, BottomRight };
        public int Index { get { return AllStarts.IndexOf(s => s.TilePosition == TilePosition); } }
        public override string ToString() { return string.Format("Place player in {0}", Name); }
    }

    sealed class Move : GameAction
    {
        public Direction Direction { get; private set; }
        private Move(Direction direction) { Direction = direction; }
        public static Move Up = new Move(Direction.Up);
        public static Move Right = new Move(Direction.Right);
        public static Move Down = new Move(Direction.Down);
        public static Move Left = new Move(Direction.Left);
        public static Move[] AllMoves = new[] { Up, Right, Down, Left };
        public override string ToString() { return string.Format("Move {0}", Direction.ToString().ToLowerInvariant()); }
    }

    sealed class Rotate : GameAction
    {
        public bool IsClockwise { get; private set; }
        private Rotate(bool clockwise) { IsClockwise = clockwise; }
        public static Rotate Clockwise = new Rotate(true);
        public static Rotate CounterClockwise = new Rotate(false);
        public static Rotate[] AllRotations = new[] { Clockwise, CounterClockwise };
        public override string ToString() { return IsClockwise ? "Rotate clockwise" : "Rotate counter-clockwise"; }
    }

    sealed class Shove : GameAction
    {
        public int RowCol { get; private set; }
        public Direction Direction { get; private set; }
        public int Index { get { return Array.IndexOf(AllShoves, this); } }
        private Shove(int rowCol, Direction direction) { RowCol = rowCol; Direction = direction; }
        public static Shove[] AllShoves = new Shove[]
        {
            new Shove(1, Direction.Down),
            new Shove(3, Direction.Down),
            new Shove(5, Direction.Down),
            new Shove(1, Direction.Left),
            new Shove(3, Direction.Left),
            new Shove(5, Direction.Left),
            new Shove(5, Direction.Up),
            new Shove(3, Direction.Up),
            new Shove(1, Direction.Up),
            new Shove(5, Direction.Right),
            new Shove(3, Direction.Right),
            new Shove(1, Direction.Right)
        };
        public override string ToString() { return string.Format("Slide {0} {1}", Direction == Direction.Up || Direction == Direction.Down ? "column " + (char) ('A' + RowCol) : "row " + (1 + RowCol), Direction.ToString().ToLowerInvariant()); }
    }
}
