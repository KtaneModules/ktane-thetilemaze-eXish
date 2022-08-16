namespace TheTileMaze
{
    abstract class GameActionResult
    {
        public static implicit operator GameActionResult(string message) { return new Strike(message); }
        public static implicit operator GameActionResult(GameState newState) { return new Valid(newState); }
    }

    sealed class Valid : GameActionResult
    {
        public GameState NewState { get; private set; }
        public Valid(GameState newState) { NewState = newState; }
    }

    sealed class Strike : GameActionResult
    {
        public string Message { get; private set; }
        public Strike(string message) { Message = message; }
    }
}
