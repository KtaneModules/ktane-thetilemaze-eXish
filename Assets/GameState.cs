using System;
using System.Linq;
using Rnd = UnityEngine.Random;

namespace TheTileMaze
{
    sealed class GameState : IEquatable<GameState>
    {
        public Tile[] Tiles;    // 7×7
        public Tile ExtraTile;
        public PlayerColor[] CornerColors;
        public PlayerColor PlayerColor;
        public int MoveCount;
        public Shove LastShove;
        public bool StartPlaced;
        public int PlayerPosition;
        public int GoalPosition;
        public int[] NumbersToCollect;
        public int NumbersCollected;
        public string LogMessage;

        private GameState Clone()
        {
            // Note: intentionally not cloning ‘LogMessage’
            return new GameState
            {
                Tiles = Tiles,
                ExtraTile = ExtraTile,
                CornerColors = CornerColors,
                PlayerColor = PlayerColor,
                MoveCount = MoveCount,
                LastShove = LastShove,
                StartPlaced = StartPlaced,
                PlayerPosition = PlayerPosition,
                GoalPosition = GoalPosition,
                NumbersToCollect = NumbersToCollect,
                NumbersCollected = NumbersCollected
            };
        }

        public bool Equals(GameState other)
        {
            return other != null &&
                other.Tiles.SequenceEqual(Tiles) &&
                other.ExtraTile.Equals(ExtraTile) &&
                other.CornerColors.SequenceEqual(CornerColors) &&
                other.PlayerColor == PlayerColor &&
                other.MoveCount == MoveCount &&
                other.LastShove == LastShove &&
                other.StartPlaced == StartPlaced &&
                other.PlayerPosition == PlayerPosition &&
                other.GoalPosition == GoalPosition &&
                other.NumbersToCollect.SequenceEqual(NumbersToCollect) &&
                other.NumbersCollected == NumbersCollected;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var h = 31;
                foreach (var tile in Tiles)
                    h = (h * 47) + tile.GetHashCode();
                h = (h * 47) + ExtraTile.GetHashCode();
                h = (h * 47) + MoveCount;
                h = (h * 47) + PlayerPosition;
                h = (h * 47) + NumbersCollected;
                return h;
            }
        }

        public bool IsSolved { get { return NumbersCollected == NumbersToCollect.Length && PlayerPosition == GoalPosition; } }

        private static readonly TileShape?[] _initialGrid = new TileShape?[]
        {
            TileShape.ES,  null, TileShape.ESW, null, TileShape.ESW, null, TileShape.SW,
            null,          null, null,          null, null,          null, null,
            TileShape.NES, null, TileShape.NES, null, TileShape.ESW, null, TileShape.SWN,
            null,          null, null,          null, null,          null, null,
            TileShape.NES, null, TileShape.NEW, null, TileShape.SWN, null, TileShape.SWN,
            null,          null, null,          null, null,          null, null,
            TileShape.NE,  null, TileShape.NEW, null, TileShape.NEW, null, TileShape.NW
        };

        public static readonly Direction[] AllDirections = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

        public IsMove CanMoveTo(int position, Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return CanMoveUp(position);
                case Direction.Right: return CanMoveRight(position);
                case Direction.Down: return CanMoveDown(position);
                default: return CanMoveLeft(position);
            }
        }

        private IsMove CanMoveUp(int position) { return !Tiles[position].IsOpenAt(Direction.Up) ? IsMove.Wall : position / 7 == 0 ? IsMove.Edge : !Tiles[position - 7].IsOpenAt(Direction.Down) ? IsMove.Wall : IsMove.Allowed; }
        private IsMove CanMoveRight(int position) { return !Tiles[position].IsOpenAt(Direction.Right) ? IsMove.Wall : position % 7 == 6 ? IsMove.Edge : !Tiles[position + 1].IsOpenAt(Direction.Left) ? IsMove.Wall : IsMove.Allowed; }
        private IsMove CanMoveDown(int position) { return !Tiles[position].IsOpenAt(Direction.Down) ? IsMove.Wall : position / 7 == 6 ? IsMove.Edge : !Tiles[position + 7].IsOpenAt(Direction.Up) ? IsMove.Wall : IsMove.Allowed; }
        private IsMove CanMoveLeft(int position) { return !Tiles[position].IsOpenAt(Direction.Left) ? IsMove.Wall : position % 7 == 0 ? IsMove.Edge : !Tiles[position - 1].IsOpenAt(Direction.Right) ? IsMove.Wall : IsMove.Allowed; }

        public static GameState GenerateInitial()
        {
            var gs = new GameState
            {
                Tiles = new Tile[7 * 7],
                MoveCount = 0,
                StartPlaced = false,
                CornerColors = new[] { PlayerColor.Red, PlayerColor.Blue, PlayerColor.Yellow, PlayerColor.Green }.Shuffle(),
                NumbersToCollect = Enumerable.Range(0, 10).ToArray().Shuffle().Take(3).ToArray(),
                NumbersCollected = 0
            };

            // All of the shovable tiles (every even row and column, plus the extra tile)
            var tiles = Enumerable.Repeat(TileShape.NE, 16).Concat(Enumerable.Repeat(TileShape.ESW, 6)).Concat(Enumerable.Repeat(TileShape.NS, 12)).ToArray().Shuffle();
            var tIx = 0;

            // All tiles except the corners can potentially have numbers on them, the rest are “null” numbers
            var tileNumbers = Enumerable.Range(0, 10).Select(v => (int?) v).Concat(Enumerable.Repeat((int?) null, 50 /* all tiles */ - 10 /* numbers */ - 4 /* corners */)).ToArray().Shuffle();
            var tnIx = 0;

            for (var i = 0; i < _initialGrid.Length; i++)
                gs.Tiles[i] = new Tile(
                    // Place tiles from _initialGrid; fill the gaps with the shuffled tiles and rotate them randomly
                    _initialGrid[i] != null ? _initialGrid[i].Value : tiles[tIx++].Rotate(Rnd.Range(0, 4)),
                    // Place numbers anywhere except the corners
                    SetStart.AllStarts.Any(s => s.TilePosition == i) ? null : tileNumbers[tnIx++]);

            gs.ExtraTile = new Tile(tiles[tIx].Rotate(Rnd.Range(0, 4)), tileNumbers[tnIx]);
            return gs;
        }

        public GameActionResult Perform(GameAction action)
        {
            GameState newState;

            var setStart = action as SetStart;
            var move = action as Move;
            var rotate = action as Rotate;
            var shove = action as Shove;

            if (setStart != null)
            {
                if (StartPlaced)
                    return this;
                var req = RequiredStartPosition;
                if (setStart != req)
                    return string.Format("You tried to place the player in the {0} instead of the {1}.", setStart.Name, req.Name);
                newState = PlacePlayer(setStart);
            }
            else if (move != null)
            {
                if (!StartPlaced)
                    return "You tried to move, but you have not placed the player in the correct corner yet.";
                if (MoveCount == 3)
                    return "You already made three moves, you have to slide in a tile now.";
                switch (CanMoveTo(PlayerPosition, move.Direction))
                {
                    case IsMove.Wall:
                        return string.Format("You cannot move {0} because there is a wall blocking your way.", move.Direction.ToString().ToLowerInvariant());
                    case IsMove.Edge:
                        return string.Format("You cannot move {0} because you are at the edge of the maze.", move.Direction.ToString().ToLowerInvariant());
                }
                newState = MovePlayer(move.Direction);
            }
            else if (rotate != null)
                newState = RotateExtraTile(rotate.IsClockwise);
            else if (shove != null)
            {
                if (!StartPlaced)
                    return "You tried to slide in a tile, but you have not placed the player in the correct corner yet.";
                if (!Stuck())
                {
                    if (MoveCount == 0)
                        return "You tried to slide in a tile, but you need to make three moves first (and you are not stuck).";
                    if (MoveCount < 3)
                        return string.Format("You tried to slide in a tile, but you need to make three moves first (you’ve only made {0}, and you are not stuck).", MoveCount);
                }
                if (shove == LastShove)
                    return "You tried to slide in a tile at the same place as last time. This is not allowed.";
                newState = ShoveTile(shove);
            }
            else
                throw new InvalidOperationException("No valid game action was specified.");

            // Potentially collect the next number
            if (newState.StartPlaced
                    && newState.NumbersCollected < newState.NumbersToCollect.Length
                    && newState.Tiles[newState.PlayerPosition].Number == newState.NumbersToCollect[newState.NumbersCollected])
            {
                newState.NumbersCollected++;
                newState.LogMessage = (newState.LogMessage == null ? "" : newState.LogMessage + " ") +
                    string.Format(
                        "You collected the {0}.{1}",
                        newState.NumbersToCollect[newState.NumbersCollected - 1],
                        newState.NumbersCollected == newState.NumbersToCollect.Length ? string.Format(" Now on to the goal position ({0}{1}).", (char) ('A' + GoalPosition % 7), 1 + GoalPosition / 7) : "");
            }

            if ((setStart != null || shove != null) && newState.Stuck())
                newState.LogMessage = (newState.LogMessage == null ? "" : newState.LogMessage + " ") + "You are stuck. You may slide in a tile.";

            return newState;
        }

        private bool Stuck()
        {
            return Move.AllMoves.All(m => CanMoveTo(PlayerPosition, m.Direction) != IsMove.Allowed);
        }

        private GameState RotateExtraTile(bool clockwise)
        {
            var newState = Clone();
            newState.ExtraTile = ExtraTile.Rotate(clockwise ? 1 : 3);
            return newState;
        }

        public SetStart RequiredStartPosition
        {
            get
            {
                // There are at least four consecutive L-tiles horizontally or vertically
                for (var t = 0; t < 49; t++)
                {
                    if (t % 7 < 4 && Enumerable.Range(0, 4).All(x => Tiles[t + x].IsL))
                        return SetStart.TopLeft;
                    if (t / 7 < 4 && Enumerable.Range(0, 4).All(x => Tiles[t + 7 * x].IsL))
                        return SetStart.TopLeft;
                }

                // The top right tile has a red or blue circle
                if (CornerColors[1] == PlayerColor.Red || CornerColors[1] == PlayerColor.Blue)
                    return SetStart.BottomLeft;

                // The tile with a green circle connects to two other tiles
                var greenWhere = SetStart.AllStarts[Array.IndexOf(CornerColors, PlayerColor.Green)].TilePosition;
                if (AllDirections.Count(dir => CanMoveTo(greenWhere, dir) == IsMove.Allowed) >= 2)
                    return SetStart.TopRight;

                // No other conditions were true
                return SetStart.BottomRight;
            }
        }

        private GameState PlacePlayer(SetStart setStart)
        {
            var newState = Clone();
            newState.PlayerPosition = setStart.TilePosition;
            newState.GoalPosition = setStart.TilePosition;
            newState.MoveCount = 0;
            newState.PlayerColor = CornerColors[Array.IndexOf(SetStart.AllStarts, setStart)];
            newState.StartPlaced = true;
            newState.LogMessage = "Player placed correctly.";
            return newState;
        }

        private GameState MovePlayer(Direction direction)
        {
            var dx = direction == Direction.Left ? -1 : direction == Direction.Right ? 1 : 0;
            var dy = direction == Direction.Up ? -1 : direction == Direction.Down ? 1 : 0;
            var newState = Clone();
            newState.PlayerPosition += dx + 7 * dy;
            newState.MoveCount++;
            newState.LogMessage = string.Format("Moved {2} to {0}{1}.", (char) ('A' + newState.PlayerPosition % 7), 1 + newState.PlayerPosition / 7, direction.ToString().ToLowerInvariant());
            return newState;
        }

        private GameState ShoveTile(Shove shove)
        {
            var newState = Clone();
            newState.Tiles = Tiles.ToArray();    // take a copy of the array so we don’t modify the current state
            newState.LastShove = shove;
            newState.MoveCount = 0;
            var oldPlayerPosition = PlayerPosition;

            if (shove.Direction == Direction.Down)
            {
                var x = shove.RowCol;
                newState.Tiles[x + 7 * 0] = ExtraTile;
                for (var i = 1; i < 7; i++)
                    newState.Tiles[x + 7 * i] = Tiles[x + 7 * (i - 1)];
                newState.ExtraTile = Tiles[x + 7 * 6];
                if (PlayerPosition % 7 == x)
                    newState.PlayerPosition = x + 7 * ((PlayerPosition / 7 + 1) % 7);
            }
            else if (shove.Direction == Direction.Left)
            {
                var y = shove.RowCol;
                for (var i = 0; i < 6; i++)
                    newState.Tiles[i + 7 * y] = Tiles[i + 1 + 7 * y];
                newState.Tiles[6 + 7 * y] = ExtraTile;
                newState.ExtraTile = Tiles[0 + 7 * y];
                if (PlayerPosition / 7 == y)
                    newState.PlayerPosition = ((PlayerPosition % 7 + 6) % 7) + 7 * y;
            }
            else if (shove.Direction == Direction.Up)
            {
                var x = shove.RowCol;
                for (var i = 0; i < 6; i++)
                    newState.Tiles[x + 7 * i] = Tiles[x + 7 * (i + 1)];
                newState.Tiles[x + 7 * 6] = ExtraTile;
                newState.ExtraTile = Tiles[x + 7 * 0];
                if (PlayerPosition % 7 == x)
                    newState.PlayerPosition = x + 7 * ((PlayerPosition / 7 + 6) % 7);
            }
            else // direction == Direction.Right
            {
                var y = shove.RowCol;
                newState.Tiles[0 + 7 * y] = ExtraTile;
                for (var i = 1; i < 7; i++)
                    newState.Tiles[i + 7 * y] = Tiles[i - 1 + 7 * y];
                newState.ExtraTile = Tiles[6 + 7 * y];
                if (PlayerPosition / 7 == y)
                    newState.PlayerPosition = ((PlayerPosition % 7 + 1) % 7) + 7 * y;
            }

            newState.LogMessage = string.Format("Shoved {0} {1}. Extra tile is now {3}.{2}",
                shove.Direction == Direction.Up || shove.Direction == Direction.Down ? "column " + (char) ('A' + shove.RowCol) : "row " + (1 + shove.RowCol),
                shove.Direction.ToString().ToLowerInvariant(),
                newState.PlayerPosition != oldPlayerPosition ? string.Format(" Player is now at {0}{1}", (char) ('A' + newState.PlayerPosition % 7), 1 + newState.PlayerPosition / 7) : "",
                newState.ExtraTile.Char);

            return newState;
        }

        public int DistanceToTarget
        {
            get
            {
                var pos = NumbersCollected >= NumbersToCollect.Length ? GoalPosition : Tiles.IndexOf(tile => tile.Number == NumbersToCollect[NumbersCollected]);
                return (PlayerPosition % 7 - pos % 7) * (PlayerPosition % 7 - pos % 7)
                    + (PlayerPosition / 7 - pos / 7) * (PlayerPosition / 7 - pos / 7);
            }
        }
    }
}
