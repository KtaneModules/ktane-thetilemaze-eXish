using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using TheTileMaze;
using UnityEngine;

public class TheTileMazeScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public GameObject[] Tiles;
    public TextMesh[] TileNumbersText;
    public GameObject ExtraTile;
    public TextMesh ExtraTileText;
    public MeshRenderer[] Cards;
    public TextMesh[] CardNumbersText;
    public GameObject Player;

    public Material[] TileMaterials;
    public Material[] ColorTileMaterials;
    public Material[] PlayerMaterials;
    public Material CardFaceDownMaterial;
    public Material CardFaceUpMaterial;

    public KMSelectable[] ArrowButtons;     // Same order as Move.AllMoves
    public KMSelectable[] CornerButtons;    // Same order as SetStart.AllStarts
    public KMSelectable[] ShoveButtons;     // Same order as Shove.AllShoves
    public KMSelectable RotateClockwiseButton;
    public KMSelectable RotateCounterclockwiseButton;

    private GameState _state;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    void Awake()
    {
        _moduleId = _moduleIdCounter++;
        _moduleSolved = false;

        for (var i = 0; i < ArrowButtons.Length; i++)
            SetButtonHandler(ArrowButtons[i], Move.AllMoves[i]);
        for (var i = 0; i < CornerButtons.Length; i++)
            SetButtonHandler(CornerButtons[i], SetStart.AllStarts[i]);
        for (var i = 0; i < ShoveButtons.Length; i++)
            SetButtonHandler(ShoveButtons[i], Shove.AllShoves[i]);
        SetButtonHandler(RotateClockwiseButton, Rotate.Clockwise);
        SetButtonHandler(RotateCounterclockwiseButton, Rotate.CounterClockwise);
    }

    void Start()
    {
        _state = GameState.GenerateInitial();
        UpdateView();

        Debug.LogFormat("[The Tile Maze #{0}] Generated Maze:", _moduleId);
        for (int i = 0; i < 7; i++)
            Debug.LogFormat("[The Tile Maze #{0}] {1}", _moduleId, Enumerable.Range(0, 7).Select(x => _state.Tiles[x + 7 * i].Char).Join(""));
        Debug.LogFormat("[The Tile Maze #{0}] Extra tile: {1}", _moduleId, _state.ExtraTile.Char);
        Debug.LogFormat("[The Tile Maze #{0}] Player must be placed in: {1}", _moduleId, _state.RequiredStartPosition.Name);
        Debug.LogFormat("[The Tile Maze #{0}] Numbers to collect: {1}", _moduleId, _state.NumbersToCollect.Join(", "));
    }

    private void SetButtonHandler(KMSelectable button, GameAction action)
    {
        button.OnInteract += delegate
        {
            button.AddInteractionPunch(0.75f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
            if (_moduleSolved)
                return false;

            // When waiting for the player to slide in a tile, do not strike on other actions
            if (!_state.StartPlaced && !(action is SetStart))
                return false;

            var result = _state.Perform(action);
            var valid = result as Valid;
            var strike = result as Strike;

            if (valid != null)
            {
                _state = valid.NewState;
                if (valid.NewState.LogMessage != null)
                    Debug.LogFormat("[The Tile Maze #{0}] {1}", _moduleId, valid.NewState.LogMessage);
                if (_state.IsSolved)
                {
                    _moduleSolved = true;
                    Debug.LogFormat("[The Tile Maze #{0}] Module solved!", _moduleId);
                    Module.HandlePass();
                }
                UpdateView();
            }
            else if (strike != null)
            {
                Debug.LogFormat("[The Tile Maze #{0}] Strike! {1}", _moduleId, strike.Message);
                Module.HandleStrike();
            }
            return false;
        };
    }

    private void UpdateView()
    {
        for (var i = 0; i < 7 * 7; i++)
        {
            var ix = SetStart.AllStarts.IndexOf(s => s.TilePosition == i);
            SetTile(Tiles[i], TileNumbersText[i], _state.Tiles[i], ix == -1 ? (PlayerColor?) null : _state.CornerColors[ix]);
        }
        SetTile(ExtraTile, ExtraTileText, _state.ExtraTile, null);

        Player.SetActive(_state.StartPlaced);
        if (_state.StartPlaced)
        {
            Player.transform.localPosition = new Vector3(-.0795f + (_state.PlayerPosition % 7) * .125f / 6, .023f, .0496f - (_state.PlayerPosition / 7) * .1252f / 6);
            Player.GetComponent<MeshRenderer>().sharedMaterial = PlayerMaterials[(int) _state.PlayerColor];
        }

        for (var card = 0; card < 3; card++)
        {
            Cards[card].sharedMaterial = _state.NumbersCollected >= card ? CardFaceUpMaterial : CardFaceDownMaterial;
            CardNumbersText[card].text = _state.NumbersCollected >= card ? _state.NumbersToCollect[card].ToString() : "";
        }
    }

    private void SetTile(GameObject tileObj, TextMesh tileText, Tile tile, PlayerColor? color)
    {
        tileObj.GetComponent<MeshRenderer>().sharedMaterial = color == null ? TileMaterials[(int) tile.Shape >> 2] : ColorTileMaterials[(int) color.Value];
        tileObj.transform.localEulerAngles = new Vector3(0, tile.Rotation + 180, 0);    // The UV mapping on the buttons is upside-down

        if (tileText != null)
        {
            if (tile.Number == 6)
            {
                tileText.text = "!";
                tileText.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
            }
            else if (tile.Number == 9)
            {
                tileText.text = "\"";
                tileText.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
            }
            else
                tileText.text = tile.Number.ToString();
            if (tile.IsL)
                tileText.transform.localPosition = new Vector3(0, 0.01f, -0.05f);
            else if (tile.IsT && tile.Number != 6 && tile.Number != 9)
                tileText.transform.localPosition = new Vector3(0, 0.01f, 0.1f);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} start <tl/tr/bl/br> [Presses the tile in the top left, top right, bottom left, or bottom right] | !{0} <ccw/cw> [Presses the counter-clockwise or clockwise button] | !{0} <l/r/u/d> [Presses the left, right, up, or down arrow button] | !{0} <coord> [Slides in a tile at the specified coordinate where A1 is top left and G7 is bottom right] | The previous three commands are chainable with spaces";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        if ((m = Regex.Match(command, @"^\s*start\s+(tl|tr|bl|br)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            string[] positions = { "tl", "tr", "bl", "br" };
            var ix = positions.IndexOf(str => str.Equals(m.Groups[1].Value, StringComparison.InvariantCultureIgnoreCase));
            if (ix == -1)
                yield break;
            yield return null;
            yield return new[] { CornerButtons[ix] };
            yield break;
        }

        string[] parameters = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var btns = new List<KMSelectable>();
        foreach (var p in parameters)
        {
            if (!(m = Regex.Match(p, @"^\s*(l|r|u|d|ccw|cw|b1|d1|f1|b7|d7|f7|a2|a4|a6|g2|g4|g6)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
            {
                yield return string.Format("sendtochaterror The specified command or coord “{0}” is invalid!", p);
                yield break;
            }
            if (p.Equals("u", StringComparison.InvariantCultureIgnoreCase))
                btns.Add(ArrowButtons[0]);
            else if (p.Equals("r", StringComparison.InvariantCultureIgnoreCase))
                btns.Add(ArrowButtons[1]);
            else if (p.Equals("d", StringComparison.InvariantCultureIgnoreCase))
                btns.Add(ArrowButtons[2]);
            else if (p.Equals("l", StringComparison.InvariantCultureIgnoreCase))
                btns.Add(ArrowButtons[3]);
            else if (p.Equals("cw", StringComparison.InvariantCultureIgnoreCase))
                btns.Add(RotateClockwiseButton);
            else if (p.Equals("ccw", StringComparison.InvariantCultureIgnoreCase))
                btns.Add(RotateCounterclockwiseButton);
            else
                btns.Add(ShoveButtons["b1,d1,f1,g2,g4,g6,f7,d7,b7,a6,a4,a2".Split(',').IndexOf(str => str.Equals(p, StringComparison.InvariantCultureIgnoreCase))]);
        }
        yield return null;
        yield return btns;
    }

    struct Stuff
    {
        public GameState State { get; private set; }
        public GameState Parent { get; private set; }
        public GameAction Action { get; private set; }
        public int Rotations { get; private set; }
        public Stuff(GameState state, GameState parent, GameAction action, int rotations)
        {
            State = state;
            Parent = parent;
            Action = action;
            Rotations = rotations;
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        if (!_state.StartPlaced)
        {
            CornerButtons[_state.RequiredStartPosition.Index].OnInteract();
            yield return new WaitForSeconds(.2f);
        }

        // Here’s how this autosolver works:
        // Fundamentally, we run Dijkstra’s algorithm on the graph of possible game states.
        // However, in many situations the total number of possible states to explore before the algorithm finds
        // a solution is too large. Therefore, the following three things happen:
        // 1) When we find the next digit, we go there and restart the graph search.
        // 2) When the number of visited game states goes above 1000, AND we already found a game state that
        //     gets us physically closer to the intended target (goal or next digit), we go there and restart the search.
        // 3) When the number of visited game states goes above 1000 and we have not found a way to get closer,
        //     we perform some random valid moves and then restart the search.
        // Also, we run Dijkstra’s algorithm in a separate thread so that it doesn’t lock up the game.

        while (!_state.IsSolved)
        {
            // Setup for Dijkstra’s algorithm
            var visited = new Dictionary<GameState, Stuff>();
            var q = new Queue<Stuff>();
            q.Enqueue(new Stuff(_state, null, null, 0));
            GameState finalState = null;
            var doRandomStuff = false;

            // Run Dijkstra’s algorithm inside of a thread
            var thread = new Thread(() =>
            {
                while (q.Count > 0)
                {
                    var item = q.Dequeue();
                    if (visited.ContainsKey(item.State))
                        continue;
                    visited[item.State] = item;

                    // If ‘visited’ gets too large, try something
                    if (visited.Count > 1000)
                    {
                        // Find a state where the player is closest to the intended position
                        GameState bestState = null;
                        int closestDistance = 0;
                        foreach (var state in visited.Keys)
                        {
                            var dist = state.DistanceToTarget;
                            if (bestState == null || dist < closestDistance)
                            {
                                bestState = state;
                                closestDistance = dist;
                            }
                        }

                        // If this gets us closer, then go there; otherwise, perform some random moves
                        if (bestState.DistanceToTarget < _state.DistanceToTarget)
                            finalState = bestState;
                        else
                            doRandomStuff = true;
                        return;
                    }

                    // Have we found the solution?
                    if (item.State.NumbersCollected > _state.NumbersCollected || _state.IsSolved)
                    {
                        finalState = item.State;
                        return;
                    }

                    // Try moving the pawn
                    Valid v;
                    foreach (var move in Move.AllMoves)
                        if ((v = item.State.Perform(move) as Valid) != null)
                            q.Enqueue(new Stuff(v.NewState, item.State, move, 0));

                    // Try shoving in the tile
                    foreach (var shove in Shove.AllShoves)
                    {
                        GameState ns = null;
                        for (var rotations = 0; rotations < 4; rotations++)
                        {
                            ns = rotations == 0 ? item.State : (ns.Perform(Rotate.Clockwise) as Valid).NewState;
                            if ((v = ns.Perform(shove) as Valid) != null)
                                q.Enqueue(new Stuff(v.NewState, item.State, shove, rotations));
                        }
                    }
                }
            });
            thread.Start();
            while (finalState == null && !doRandomStuff)
                yield return true;

            // The thread told us to make random movements (this happens sometimes when we can’t make progress otherwise)
            if (doRandomStuff)
            {
                for (var i = 0; i < 12; i++)
                {
                    var validAction = GameAction.All
                        .Where(a => !(a is Rotate) && !(a is SetStart))
                        .Select(a => new { Action = a, Result = _state.Perform(a) })
                        .Where(tup => tup.Result is Valid)
                        .PickRandom();
                    TpPerform(validAction.Action);
                    yield return new WaitForSeconds(.1f);
                }
            }
            // Otherwise, perform the actions generated by the algorithm in the thread
            else
            {
                var actions = new List<GameAction>();
                var st = finalState;
                while (true)
                {
                    var item = visited[st];
                    if (item.Parent == null)
                        break;
                    actions.Add(item.Action);
                    for (var i = 0; i < item.Rotations; i++)
                        actions.Add(Rotate.Clockwise);
                    st = item.Parent;
                }
                for (int i = actions.Count - 1; i >= 0; i--)
                {
                    TpPerform(actions[i]);
                    yield return new WaitForSeconds(.1f);
                }
            }
        }
    }

    private void TpPerform(GameAction action)
    {
        var move = action as Move;
        var shove = action as Shove;
        var rotation = action as Rotate;
        var setStart = action as SetStart;

        if (move != null)
            ArrowButtons[(int) move.Direction].OnInteract();
        else if (shove != null)
            ShoveButtons[shove.Index].OnInteract();
        else if (rotation != null)
            (rotation.IsClockwise ? RotateClockwiseButton : RotateCounterclockwiseButton).OnInteract();
        else if (setStart != null)
            CornerButtons[setStart.Index].OnInteract();
    }
}
