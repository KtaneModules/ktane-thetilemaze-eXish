using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class TheTileMazeScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;
    public GameObject[] tiles;
    public GameObject extraTile;
    public Material[] tileMats;
    public TextMesh[] cardNumbersText;
    public TextMesh[] tileNumbersText;
    public TextMesh extraTileText;
    public Material[] playerMats;
    public GameObject player;
    public MeshRenderer[] cards;
    public Material cardFlipMat;

    private Vector3[] startPositions = { new Vector3(-0.0795f, 0.023f, 0.0496f), new Vector3(0.0455f, 0.023f, 0.0496f), new Vector3(-0.0795f, 0.023f, -0.0756f), new Vector3(0.0455f, 0.023f, -0.0756f) };
    private string[] colorNames = { "Red", "Blue", "Yellow", "Green" };
    private string[] cornerTilePos = { "Top Left", "Top Right", "Bottom Left", "Bottom Right" };
    private string[] moveDirections = { "left", "right", "up", "down" };
    private string[] coordsFirstList = { "A", "B", "C", "D", "E", "F", "G" };
    private string[] coordsSecondList = { "1", "2", "3", "4", "5", "6", "7" };
    private int[] tileTypes =
    {
        -2,  -1,  2,  -1,  2,  -1, -2,
        -1,  -1, -1,  -1, -1,  -1, -1,
         2,  -1,  2,  -1,  2,  -1,  2,
        -1,  -1, -1,  -1, -1,  -1, -1,
         2,  -1,  2,  -1,  2,  -1,  2,
        -1,  -1, -1,  -1, -1,  -1, -1,
        -2,  -1,  2,  -1,  2,  -1, -2
    };
    private int[] tileRotations =
    {
         3,  -1,  2,  -1,  2,  -1,  0,
        -1,  -1, -1,  -1, -1,  -1, -1,
         1,  -1,  1,  -1,  2,  -1,  3,
        -1,  -1, -1,  -1, -1,  -1, -1,
         1,  -1,  0,  -1,  3,  -1,  3,
        -1,  -1, -1,  -1, -1,  -1, -1,
         2,  -1,  0,  -1,  0,  -1,  1
    };
    private int[] tileNumbers =
    {
        -1,  -1, -1,  -1, -1,  -1, -1,
        -1,  -1, -1,  -1, -1,  -1, -1,
        -1,  -1, -1,  -1, -1,  -1, -1,
        -1,  -1, -1,  -1, -1,  -1, -1,
        -1,  -1, -1,  -1, -1,  -1, -1,
        -1,  -1, -1,  -1, -1,  -1, -1,
        -1,  -1, -1,  -1, -1,  -1, -1
    };
    private int[] startTilePos = { 0, 6, 42, 48 };
    private int[] cardNumbersGenerated = { -1, -1, -1 };
    private int extraTileType = -1;
    private int extraTileRotation = -1;
    private int extraTileNumber = -1;
    private int startingTile = 3;
    private int currentTile = -1;
    private int stage = 0;
    private int moveCount = 0;
    private int lastShove = -1;
    private bool startPlaced = false;
    private bool isStuck = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
    }

    void Start () {
        List<int> startTiles = new List<int> { 3, 4, 5, 6 };
        startTiles = startTiles.Shuffle();
        for (int i = 0; i < 4; i++)
            tileTypes[startTilePos[i]] = startTiles[i];
        int[] tilePos = { 1, 3, 5, 7, 8, 9, 10, 11, 12, 13, 15, 17, 19, 21, 22, 23, 24, 25, 26, 27, 29, 31, 33, 35, 36, 37, 38, 39, 40, 41, 43, 45, 47 };
        List<int> tempTiles = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2 };
        extraTileType = UnityEngine.Random.Range(0, 3);
        extraTileRotation = UnityEngine.Random.Range(0, 4);
        extraTile.GetComponent<MeshRenderer>().material = tileMats[extraTileType];
        extraTile.transform.localEulerAngles = new Vector3(0, 90 * extraTileRotation, 0);
        tempTiles.Remove(extraTileType);
        tempTiles = tempTiles.Shuffle();
        int tempCt = 0;
        for (int i = 0; i < 49; i++)
        {
            if (tilePos.Contains(i))
            {
                tileTypes[tilePos[tempCt]] = tempTiles[tempCt];
                tileRotations[tilePos[tempCt]] = UnityEngine.Random.Range(0, 4);
                tiles[tilePos[tempCt]].GetComponent<MeshRenderer>().material = tileMats[tileTypes[tilePos[tempCt]]];
                tiles[tilePos[tempCt]].transform.localEulerAngles = new Vector3(0, 90 * tileRotations[tilePos[tempCt]], 0);
                tempCt++;
            }
            else
            {
                tiles[i].GetComponent<MeshRenderer>().material = tileMats[tileTypes[i]];
                tiles[i].transform.localEulerAngles = new Vector3(0, 90 * tileRotations[i], 0);
            }
        }
        Debug.LogFormat("[The Tile Maze #{0}] Generated Maze:", moduleId);
        for (int i = 0; i < 7; i++)
            Debug.LogFormat("[The Tile Maze #{0}] {1}{2}{3}{4}{5}{6}{7}", moduleId, GetTileLogChar(i * 7), GetTileLogChar((i * 7) + 1), GetTileLogChar((i * 7) + 2), GetTileLogChar((i * 7) + 3), GetTileLogChar((i * 7) + 4), GetTileLogChar((i * 7) + 5), GetTileLogChar((i * 7) + 6));
        Debug.LogFormat("[The Tile Maze #{0}] Extra Tile: {1}", moduleId, GetTileLogChar(49));
        List<int> numbers = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        numbers = numbers.Shuffle();
        List<int> tileNumIndexes = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            int choice = UnityEngine.Random.Range(0, 50);
            while (tileNumIndexes.Contains(choice) || startTilePos.Contains(choice))
                choice = UnityEngine.Random.Range(0, 50);
            tileNumIndexes.Add(choice);
            if (tileNumIndexes[i] == 49)
            {
                extraTileNumber = numbers[i];
                if (extraTileNumber == 6)
                {
                    extraTileText.text = "!";
                    extraTileText.gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
                }
                else if (extraTileNumber == 9)
                {
                    extraTileText.text = "\"";
                    extraTileText.gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
                }
                else
                    extraTileText.text = extraTileNumber.ToString();
                if (extraTileType == 0)
                    extraTileText.gameObject.transform.localPosition = new Vector3(0, 0.01f, -0.05f);
                else if (extraTileType == 2 && extraTileNumber != 6 && extraTileNumber != 9)
                    extraTileText.gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.1f);
            }
            else
            {
                tileNumbers[tileNumIndexes[i]] = numbers[i];
                if (tileNumbers[tileNumIndexes[i]] == 6)
                {
                    tileNumbersText[tileNumIndexes[i]].text = "!";
                    tileNumbersText[tileNumIndexes[i]].gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
                }
                else if (tileNumbers[tileNumIndexes[i]] == 9)
                {
                    tileNumbersText[tileNumIndexes[i]].text = "\"";
                    tileNumbersText[tileNumIndexes[i]].gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
                }
                else
                    tileNumbersText[tileNumIndexes[i]].text = tileNumbers[tileNumIndexes[i]].ToString();
                if (tileTypes[tileNumIndexes[i]] == 0)
                    tileNumbersText[tileNumIndexes[i]].gameObject.transform.localPosition = new Vector3(0, 0.01f, -0.05f);
                else if (tileTypes[tileNumIndexes[i]] == 2 && tileNumbers[tileNumIndexes[i]] != 6 && tileNumbers[tileNumIndexes[i]] != 9)
                    tileNumbersText[tileNumIndexes[i]].gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.1f);
            }
        }
        Debug.LogFormat("[The Tile Maze #{0}] Tile Numbers:", moduleId);
        for (int i = 0; i < 7; i++)
            Debug.LogFormat("[The Tile Maze #{0}] {1}{2}{3}{4}{5}{6}{7}", moduleId, tileNumbers[i * 7] == -1 ? "-" : tileNumbers[i * 7].ToString(), tileNumbers[(i * 7) + 1] == -1 ? "-" : tileNumbers[(i * 7) + 1].ToString(), tileNumbers[(i * 7) + 2] == -1 ? "-" : tileNumbers[(i * 7) + 2].ToString(), tileNumbers[(i * 7) + 3] == -1 ? "-" : tileNumbers[(i * 7) + 3].ToString(), tileNumbers[(i * 7) + 4] == -1 ? "-" : tileNumbers[(i * 7) + 4].ToString(), tileNumbers[(i * 7) + 5] == -1 ? "-" : tileNumbers[(i * 7) + 5].ToString(), tileNumbers[(i * 7) + 6] == -1 ? "-" : tileNumbers[(i * 7) + 6].ToString());
        Debug.LogFormat("[The Tile Maze #{0}] Extra Tile Number: {1}", moduleId, extraTileNumber == -1 ? "-" : extraTileNumber.ToString());
        string colLog = "";
        for (int i = 0; i < 4; i++)
        {
            if (i == 3)
                colLog += colorNames[tileTypes[startTilePos[i]] - 3];
            else
                colLog += colorNames[tileTypes[startTilePos[i]] - 3] + ", ";
        }
        Debug.LogFormat("[The Tile Maze #{0}] Colors of corner tiles in reading order: {1}", moduleId, colLog);
        List<int> cardNumbers = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            int choice = UnityEngine.Random.Range(0, 10);
            while (cardNumbers.Contains(choice))
                choice = UnityEngine.Random.Range(0, 10);
            cardNumbers.Add(choice);
            cardNumbersGenerated[i] = cardNumbers[i];
        }
        cardNumbersText[0].text = cardNumbersGenerated[0].ToString();
        Debug.LogFormat("[The Tile Maze #{0}] Card numbers from bottom to top: {1}", moduleId, cardNumbersGenerated.Join(", "));
        if (CheckForThreeLTiles())
            startingTile = 0;
        else if (tileTypes[startTilePos[1]] == 3 || tileTypes[startTilePos[1]] == 4)
            startingTile = 2;
        else if (CheckForGreenConnection())
            startingTile = 1;
        else
            startingTile = 3;
        currentTile = startTilePos[startingTile];
        player.transform.localPosition = startPositions[startingTile];
        player.GetComponent<MeshRenderer>().material = playerMats[tileTypes[startTilePos[startingTile]] - 3];
        Debug.LogFormat("[The Tile Maze #{0}] Correct starting tile: {1}", moduleId, cornerTilePos[startingTile]);
        bool[] canMove = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            if (ValidMove(i))
                canMove[i] = true;
        }
        if (!canMove.Contains(true))
            moveCount = 3;
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true)
        {
            int pressPos = Array.IndexOf(buttons, pressed);
            if (pressPos >= 0 && pressPos <= 3 && startPlaced)
            {
                pressed.AddInteractionPunch(0.75f);
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                if (ValidMove(pressPos) && moveCount != 3)
                {
                    switch (pressPos)
                    {
                        case 0:
                            currentTile--;
                            if (startTilePos.Contains(currentTile))
                                player.transform.localPosition = startPositions[Array.IndexOf(startTilePos, currentTile)];
                            else if (startTilePos.Contains(currentTile + 1))
                                player.transform.localPosition = new Vector3(tiles[currentTile].transform.localPosition.x, tiles[currentTile].transform.localPosition.y, tiles[currentTile].transform.localPosition.z);
                            else
                                player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.021f, player.transform.localPosition.y, player.transform.localPosition.z);
                            break;
                        case 1:
                            currentTile++;
                            if (startTilePos.Contains(currentTile))
                                player.transform.localPosition = startPositions[Array.IndexOf(startTilePos, currentTile)];
                            else if (startTilePos.Contains(currentTile - 1))
                                player.transform.localPosition = new Vector3(tiles[currentTile].transform.localPosition.x, tiles[currentTile].transform.localPosition.y, tiles[currentTile].transform.localPosition.z);
                            else
                                player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.021f, player.transform.localPosition.y, player.transform.localPosition.z);
                            break;
                        case 2:
                            currentTile -= 7;
                            if (startTilePos.Contains(currentTile))
                                player.transform.localPosition = startPositions[Array.IndexOf(startTilePos, currentTile)];
                            else if (startTilePos.Contains(currentTile + 7))
                                player.transform.localPosition = new Vector3(tiles[currentTile].transform.localPosition.x, tiles[currentTile].transform.localPosition.y, tiles[currentTile].transform.localPosition.z);
                            else
                                player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.021f);
                            break;
                        default:
                            currentTile += 7;
                            if (startTilePos.Contains(currentTile))
                                player.transform.localPosition = startPositions[Array.IndexOf(startTilePos, currentTile)];
                            else if (startTilePos.Contains(currentTile - 7))
                                player.transform.localPosition = new Vector3(tiles[currentTile].transform.localPosition.x, tiles[currentTile].transform.localPosition.y, tiles[currentTile].transform.localPosition.z);
                            else
                                player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.021f);
                            break;
                    }
                    Debug.LogFormat("[The Tile Maze #{0}] Moved {1} to tile {2}.", moduleId, moveDirections[pressPos], coordsFirstList[currentTile % 7] + coordsSecondList[currentTile / 7]);
                    moveCount++;
                    if (stage <= 2)
                    {
                        if (tileNumbers[currentTile] == cardNumbersGenerated[stage])
                        {
                            Debug.LogFormat("[The Tile Maze #{0}] Successfully navigated to the tile with card {1}'s number.", moduleId, stage + 1);
                            stage++;
                            if (stage <= 2)
                            {
                                cards[stage - 1].material = cardFlipMat;
                                cardNumbersText[stage].text = cardNumbersGenerated[stage].ToString();
                            }
                            else
                                Debug.LogFormat("[The Tile Maze #{0}] All card numbers have been navigated to, return to the starting tile.", moduleId, tileNumbers[currentTile]);
                        }
                    }
                    else if (currentTile == startTilePos[startingTile] && stage == 3)
                    {
                        Debug.LogFormat("[The Tile Maze #{0}] Successfully returned to the starting tile, module solved.", moduleId);
                        moduleSolved = true;
                        GetComponent<KMBombModule>().HandlePass();
                    }
                }
                else if (moveCount == 3)
                {
                    Debug.LogFormat("[The Tile Maze #{0}] A tile must be slid in before you can move again. Strike!", moduleId);
                    GetComponent<KMBombModule>().HandleStrike();
                }
                else
                {
                    Debug.LogFormat("[The Tile Maze #{0}] Attempted to move {1} but no connection was found. Strike!", moduleId, moveDirections[pressPos]);
                    GetComponent<KMBombModule>().HandleStrike();
                }
            }
            else if (pressPos == 4)
            {
                pressed.AddInteractionPunch(0.75f);
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                extraTileRotation--;
                if (extraTileRotation < 0)
                    extraTileRotation = 3;
                extraTile.transform.localEulerAngles = new Vector3(0, 90 * extraTileRotation, 0);
            }
            else if (pressPos == 5)
            {
                pressed.AddInteractionPunch(0.75f);
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                extraTileRotation++;
                if (extraTileRotation > 3)
                    extraTileRotation = 0;
                extraTile.transform.localEulerAngles = new Vector3(0, 90 * extraTileRotation, 0);
            }
            else if (pressPos >= 6 && pressPos <= 9)
            {
                if (startPlaced)
                    return;
                if (startingTile == pressPos - 6)
                {
                    audio.PlaySoundAtTransform("playerPlace", pressed.transform);
                    Debug.LogFormat("[The Tile Maze #{0}] Pressed the {1} tile, which is the starting tile. Navigate to all card numbers.", moduleId, cornerTilePos[pressPos - 6]);
                    player.SetActive(true);
                    startPlaced = true;
                    if (moveCount == 3)
                    {
                        isStuck = true;
                        Debug.LogFormat("[The Tile Maze #{0}] You are stuck, tiles may now be slid in until you are not.", moduleId);
                    }
                }
                else
                {
                    Debug.LogFormat("[The Tile Maze #{0}] Pressed the {1} tile, which is not the starting tile. Strike!", moduleId, cornerTilePos[pressPos - 6]);
                    GetComponent<KMBombModule>().HandleStrike();
                }
            }
            else if (pressPos >= 10 && pressPos <= 12)
            {
                if (!startPlaced)
                    return;
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, pressed.transform);
                if (moveCount != 3)
                {
                    Debug.LogFormat("[The Tile Maze #{0}] There has not been three moves yet, so a tile cannot be slid in. Strike!", moduleId);
                    GetComponent<KMBombModule>().HandleStrike();
                }
                else
                {
                    int[] pos = { 43, 45, 47 };
                    int[] odds = { 1, 3, 5 };
                    if (lastShove == odds[pressPos - 10])
                    {
                        Debug.LogFormat("[The Tile Maze #{0}] Cannot slide in a tile at {1} as the extra tile just slid in there. Strike!", moduleId, coordsFirstList[odds[pressPos - 10] % 7] + coordsSecondList[odds[pressPos - 10] / 7]);
                        GetComponent<KMBombModule>().HandleStrike();
                        return;
                    }
                    lastShove = odds[pressPos - 10];
                    moveCount = 0;
                    int tempExtraT = extraTileType;
                    int tempExtraR = extraTileRotation;
                    int tempExtraN = extraTileNumber;
                    extraTileType = tileTypes[pos[pressPos - 10]];
                    extraTileRotation = tileRotations[pos[pressPos - 10]];
                    extraTileNumber = tileNumbers[pos[pressPos - 10]];
                    UpdateTile(49);
                    for (int i = 5; i >= 0; i--)
                    {
                        tileTypes[(7 * (i + 1)) + odds[pressPos - 10]] = tileTypes[(7 * i) + odds[pressPos - 10]];
                        tileRotations[(7 * (i + 1)) + odds[pressPos - 10]] = tileRotations[(7 * i) + odds[pressPos - 10]];
                        tileNumbers[(7 * (i + 1)) + odds[pressPos - 10]] = tileNumbers[(7 * i) + odds[pressPos - 10]];
                        UpdateTile((7 * (i + 1)) + odds[pressPos - 10]);
                    }
                    tileTypes[odds[pressPos - 10]] = tempExtraT;
                    tileRotations[odds[pressPos - 10]] = tempExtraR;
                    tileNumbers[odds[pressPos - 10]] = tempExtraN;
                    UpdateTile(odds[pressPos - 10]);
                    for (int i = 0; i < 7; i++)
                    {
                        if (((i * 7) + odds[pressPos - 10]) == currentTile)
                        {
                            currentTile += 7;
                            if (currentTile > 48)
                            {
                                currentTile = odds[pressPos - 10];
                                for (int j = 0; j < 6; j++)
                                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.021f);
                            }
                            else
                            {
                                player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.021f);
                            }
                            break;
                        }
                    }
                    Debug.LogFormat("[The Tile Maze #{0}] The extra tile was slid in at {1} with orientation {2}.", moduleId, coordsFirstList[odds[pressPos - 10] % 7] + coordsSecondList[odds[pressPos - 10] / 7], GetTileLogChar(odds[pressPos - 10]));
                    bool[] canMove = new bool[4];
                    for (int i = 0; i < 4; i++)
                    {
                        if (ValidMove(i))
                            canMove[i] = true;
                    }
                    if (!canMove.Contains(true))
                    {
                        moveCount = 3;
                        if (!isStuck)
                        {
                            isStuck = true;
                            Debug.LogFormat("[The Tile Maze #{0}] You are stuck, tiles may now be slid in until you are not.", moduleId);
                        }
                    }
                    else if (isStuck)
                    {
                        isStuck = false;
                        Debug.LogFormat("[The Tile Maze #{0}] You are no longer stuck, regular behaviour must resume.", moduleId);
                    }
                }
            }
            else if (pressPos >= 13 && pressPos <= 15)
            {
                if (!startPlaced)
                    return;
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, pressed.transform);
                if (moveCount != 3)
                {
                    Debug.LogFormat("[The Tile Maze #{0}] There has not been three moves yet, so a tile cannot be slid in. Strike!", moduleId);
                    GetComponent<KMBombModule>().HandleStrike();
                }
                else
                {
                    int[] pos = { 43, 45, 47 };
                    int[] odds = { 1, 3, 5 };
                    if (lastShove == pos[pressPos - 13])
                    {
                        Debug.LogFormat("[The Tile Maze #{0}] Cannot slide in a tile at {1} as the extra tile just slid in there. Strike!", moduleId, coordsFirstList[pos[pressPos - 13] % 7] + coordsSecondList[pos[pressPos - 13] / 7]);
                        GetComponent<KMBombModule>().HandleStrike();
                        return;
                    }
                    lastShove = pos[pressPos - 13];
                    moveCount = 0;
                    int tempExtraT = extraTileType;
                    int tempExtraR = extraTileRotation;
                    int tempExtraN = extraTileNumber;
                    extraTileType = tileTypes[odds[pressPos - 13]];
                    extraTileRotation = tileRotations[odds[pressPos - 13]];
                    extraTileNumber = tileNumbers[odds[pressPos - 13]];
                    UpdateTile(49);
                    for (int i = 1; i < 7; i++)
                    {
                        tileTypes[(7 * (i - 1)) + odds[pressPos - 13]] = tileTypes[(7 * i) + odds[pressPos - 13]];
                        tileRotations[(7 * (i - 1)) + odds[pressPos - 13]] = tileRotations[(7 * i) + odds[pressPos - 13]];
                        tileNumbers[(7 * (i - 1)) + odds[pressPos - 13]] = tileNumbers[(7 * i) + odds[pressPos - 13]];
                        UpdateTile((7 * (i - 1)) + odds[pressPos - 13]);
                    }
                    tileTypes[pos[pressPos - 13]] = tempExtraT;
                    tileRotations[pos[pressPos - 13]] = tempExtraR;
                    tileNumbers[pos[pressPos - 13]] = tempExtraN;
                    UpdateTile(pos[pressPos - 13]);
                    for (int i = 0; i < 7; i++)
                    {
                        if (((i * 7) + odds[pressPos - 13]) == currentTile)
                        {
                            currentTile -= 7;
                            if (currentTile < 0)
                            {
                                currentTile = pos[pressPos - 13];
                                for (int j = 0; j < 6; j++)
                                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.021f);
                            }
                            else
                            {
                                player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.021f);
                            }
                            break;
                        }
                    }
                    Debug.LogFormat("[The Tile Maze #{0}] The extra tile was slid in at {1} with orientation {2}.", moduleId, coordsFirstList[pos[pressPos - 13] % 7] + coordsSecondList[pos[pressPos - 13] / 7], GetTileLogChar(pos[pressPos - 13]));
                    bool[] canMove = new bool[4];
                    for (int i = 0; i < 4; i++)
                    {
                        if (ValidMove(i))
                            canMove[i] = true;
                    }
                    if (!canMove.Contains(true))
                    {
                        moveCount = 3;
                        if (!isStuck)
                        {
                            isStuck = true;
                            Debug.LogFormat("[The Tile Maze #{0}] You are stuck, tiles may now be slid in until you are not.", moduleId);
                        }
                    }
                    else if (isStuck)
                    {
                        isStuck = false;
                        Debug.LogFormat("[The Tile Maze #{0}] You are no longer stuck, regular behaviour must resume.", moduleId);
                    }
                }
            }
            else if (pressPos >= 16 && pressPos <= 18)
            {
                if (!startPlaced)
                    return;
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, pressed.transform);
                if (moveCount != 3)
                {
                    Debug.LogFormat("[The Tile Maze #{0}] There has not been three moves yet, so a tile cannot be slid in. Strike!", moduleId);
                    GetComponent<KMBombModule>().HandleStrike();
                }
                else
                {
                    int[] pos = { 13, 27, 41 };
                    int[] pos2 = { 7, 21, 35 };
                    if (lastShove == pos2[pressPos - 16])
                    {
                        Debug.LogFormat("[The Tile Maze #{0}] Cannot slide in a tile at {1} as the extra tile just slid in there. Strike!", moduleId, coordsFirstList[pos2[pressPos - 16] % 7] + coordsSecondList[pos2[pressPos - 16] / 7]);
                        GetComponent<KMBombModule>().HandleStrike();
                        return;
                    }
                    lastShove = pos2[pressPos - 16];
                    moveCount = 0;
                    int tempExtraT = extraTileType;
                    int tempExtraR = extraTileRotation;
                    int tempExtraN = extraTileNumber;
                    extraTileType = tileTypes[pos[pressPos - 16]];
                    extraTileRotation = tileRotations[pos[pressPos - 16]];
                    extraTileNumber = tileNumbers[pos[pressPos - 16]];
                    UpdateTile(49);
                    for (int i = 5; i >= 0; i--)
                    {
                        tileTypes[pos2[pressPos - 16] + i + 1] = tileTypes[pos2[pressPos - 16] + i];
                        tileRotations[pos2[pressPos - 16] + i + 1] = tileRotations[pos2[pressPos - 16] + i];
                        tileNumbers[pos2[pressPos - 16] + i + 1] = tileNumbers[pos2[pressPos - 16] + i];
                        UpdateTile(pos2[pressPos - 16] + i + 1);
                    }
                    tileTypes[pos2[pressPos - 16]] = tempExtraT;
                    tileRotations[pos2[pressPos - 16]] = tempExtraR;
                    tileNumbers[pos2[pressPos - 16]] = tempExtraN;
                    UpdateTile(pos2[pressPos - 16]);
                    for (int i = 0; i < 7; i++)
                    {
                        if ((pos2[pressPos - 16] + i) == currentTile)
                        {
                            currentTile++;
                            if (currentTile > pos[pressPos - 16])
                            {
                                currentTile = pos2[pressPos - 16];
                                for (int j = 0; j < 6; j++)
                                    player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.021f, player.transform.localPosition.y, player.transform.localPosition.z);
                            }
                            else
                            {
                                player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.021f, player.transform.localPosition.y, player.transform.localPosition.z);
                            }
                            break;
                        }
                    }
                    Debug.LogFormat("[The Tile Maze #{0}] The extra tile was slid in at {1} with orientation {2}.", moduleId, coordsFirstList[pos2[pressPos - 16] % 7] + coordsSecondList[pos2[pressPos - 16] / 7], GetTileLogChar(pos2[pressPos - 16]));
                    bool[] canMove = new bool[4];
                    for (int i = 0; i < 4; i++)
                    {
                        if (ValidMove(i))
                            canMove[i] = true;
                    }
                    if (!canMove.Contains(true))
                    {
                        moveCount = 3;
                        if (!isStuck)
                        {
                            isStuck = true;
                            Debug.LogFormat("[The Tile Maze #{0}] You are stuck, tiles may now be slid in until you are not.", moduleId);
                        }
                    }
                    else if (isStuck)
                    {
                        isStuck = false;
                        Debug.LogFormat("[The Tile Maze #{0}] You are no longer stuck, regular behaviour must resume.", moduleId);
                    }
                }
            }
            else if (pressPos >= 19 && pressPos <= 21)
            {
                if (!startPlaced)
                    return;
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, pressed.transform);
                if (moveCount != 3)
                {
                    Debug.LogFormat("[The Tile Maze #{0}] There has not been three moves yet, so a tile cannot be slid in. Strike!", moduleId);
                    GetComponent<KMBombModule>().HandleStrike();
                }
                else
                {
                    int[] pos = { 7, 21, 35 };
                    int[] pos2 = { 13, 27, 41 };
                    if (lastShove == pos2[pressPos - 19])
                    {
                        Debug.LogFormat("[The Tile Maze #{0}] Cannot slide in a tile at {1} as the extra tile just slid in there. Strike!", moduleId, coordsFirstList[pos2[pressPos - 19] % 7] + coordsSecondList[pos2[pressPos - 19] / 7]);
                        GetComponent<KMBombModule>().HandleStrike();
                        return;
                    }
                    lastShove = pos2[pressPos - 19];
                    moveCount = 0;
                    int tempExtraT = extraTileType;
                    int tempExtraR = extraTileRotation;
                    int tempExtraN = extraTileNumber;
                    extraTileType = tileTypes[pos[pressPos - 19]];
                    extraTileRotation = tileRotations[pos[pressPos - 19]];
                    extraTileNumber = tileNumbers[pos[pressPos - 19]];
                    UpdateTile(49);
                    for (int i = 1; i < 7; i++)
                    {
                        tileTypes[pos[pressPos - 19] + i - 1] = tileTypes[pos[pressPos - 19] + i];
                        tileRotations[pos[pressPos - 19] + i - 1] = tileRotations[pos[pressPos - 19] + i];
                        tileNumbers[pos[pressPos - 19] + i - 1] = tileNumbers[pos[pressPos - 19] + i];
                        UpdateTile(pos[pressPos - 19] + i - 1);
                    }
                    tileTypes[pos2[pressPos - 19]] = tempExtraT;
                    tileRotations[pos2[pressPos - 19]] = tempExtraR;
                    tileNumbers[pos2[pressPos - 19]] = tempExtraN;
                    UpdateTile(pos2[pressPos - 19]);
                    for (int i = 0; i < 7; i++)
                    {
                        if ((pos[pressPos - 19] + i) == currentTile)
                        {
                            currentTile--;
                            if (currentTile < pos[pressPos - 19])
                            {
                                currentTile = pos2[pressPos - 19];
                                for (int j = 0; j < 6; j++)
                                    player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.021f, player.transform.localPosition.y, player.transform.localPosition.z);
                            }
                            else
                            {
                                player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.021f, player.transform.localPosition.y, player.transform.localPosition.z);
                            }
                            break;
                        }
                    }
                    Debug.LogFormat("[The Tile Maze #{0}] The extra tile was slid in at {1} with orientation {2}.", moduleId, coordsFirstList[pos2[pressPos - 19] % 7] + coordsSecondList[pos2[pressPos - 19] / 7], GetTileLogChar(pos2[pressPos - 19]));
                    bool[] canMove = new bool[4];
                    for (int i = 0; i < 4; i++)
                    {
                        if (ValidMove(i))
                            canMove[i] = true;
                    }
                    if (!canMove.Contains(true))
                    {
                        moveCount = 3;
                        if (!isStuck)
                        {
                            isStuck = true;
                            Debug.LogFormat("[The Tile Maze #{0}] You are stuck, tiles may now be slid in until you are not.", moduleId);
                        }
                    }
                    else if (isStuck)
                    {
                        isStuck = false;
                        Debug.LogFormat("[The Tile Maze #{0}] You are no longer stuck, regular behaviour must resume.", moduleId);
                    }
                }
            }
        }
    }
    
    string GetTileLogChar(int tileIndex)
    {
        if (tileIndex != 49)
        {
            switch (tileTypes[tileIndex])
            {
                case 0:
                case 3:
                case 4:
                case 5:
                case 6:
                    string[] ltiles = { "╗", "╝", "╚", "╔" };
                    return ltiles[tileRotations[tileIndex]];
                case 1:
                    string[] itiles = { "║", "═", "║", "═" };
                    return itiles[tileRotations[tileIndex]];
                default:
                    string[] ttiles = { "╩", "╠", "╦", "╣" };
                    return ttiles[tileRotations[tileIndex]];
            }
        }
        else
        {
            switch (extraTileType)
            {
                case 0:
                    string[] ltiles = { "╗", "╝", "╚", "╔" };
                    return ltiles[extraTileRotation];
                case 1:
                    string[] itiles = { "║", "═", "║", "═" };
                    return itiles[extraTileRotation];
                default:
                    string[] ttiles = { "╩", "╠", "╦", "╣" };
                    return ttiles[extraTileRotation];
            }
        }
    }

    bool CheckForThreeLTiles()
    {
        for (int i = 0; i < 7; i++)
        {
            if (i == 1 || i == 3 || i == 5)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (tileTypes[7 * i + j].EqualsAny(0, 3, 4, 5, 6) && tileTypes[7 * i + j + 1].EqualsAny(0, 3, 4, 5, 6) && tileTypes[7 * i + j + 2].EqualsAny(0, 3, 4, 5, 6) && tileTypes[7 * i + j + 3].EqualsAny(0, 3, 4, 5, 6))
                        return true;
                }
            }
        }
        for (int i = 0; i < 7; i++)
        {
            if (i == 1 || i == 3 || i == 5)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (tileTypes[i + (7 * j)].EqualsAny(0, 3, 4, 5, 6) && tileTypes[i + (7 * (j + 1))].EqualsAny(0, 3, 4, 5, 6) && tileTypes[i + (7 * (j + 2))].EqualsAny(0, 3, 4, 5, 6) && tileTypes[i + (7 * (j + 3))].EqualsAny(0, 3, 4, 5, 6))
                        return true;
                }
            }
        }
        return false;
    }

    bool CheckForGreenConnection()
    {
        int tempStore = currentTile;
        for (int i = 0; i < 4; i++)
        {
            if (tileTypes[startTilePos[i]] - 3 == 3)
            {
                currentTile = startTilePos[i];
                break;
            }
        }
        bool[] canMove = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            if (ValidMove(i))
                canMove[i] = true;
        }
        currentTile = tempStore;
        if (canMove.Count(x => x) == 2)
            return true;
        else
            return false;
    }

    bool ValidMove(int move)
    {
        switch (move)
        {
            case 0:
                if (currentTile == 0 || currentTile == 7 || currentTile == 14 || currentTile == 21 || currentTile == 28 || currentTile == 35 || currentTile == 42)
                    return false;
                if (GetTileConnections(currentTile).Contains(0) && GetTileConnections(currentTile - 1).Contains(1))
                    return true;
                else
                    return false;
            case 1:
                if (currentTile == 6 || currentTile == 13 || currentTile == 20 || currentTile == 27 || currentTile == 34 || currentTile == 41 || currentTile == 48)
                    return false;
                if (GetTileConnections(currentTile).Contains(1) && GetTileConnections(currentTile + 1).Contains(0))
                    return true;
                else
                    return false;
            case 2:
                if (currentTile < 7)
                    return false;
                if (GetTileConnections(currentTile).Contains(2) && GetTileConnections(currentTile - 7).Contains(3))
                    return true;
                else
                    return false;
            default:
                if (currentTile > 41)
                    return false;
                if (GetTileConnections(currentTile).Contains(3) && GetTileConnections(currentTile + 7).Contains(2))
                    return true;
                else
                    return false;
        }
    }

    int[] GetTileConnections(int tile)
    {
        switch (tileTypes[tile])
        {
            case 0:
            case 3:
            case 4:
            case 5:
            case 6:
                if (tileRotations[tile] == 0)
                    return new int[] { 0, 3 };
                else if (tileRotations[tile] == 1)
                    return new int[] { 0, 2 };
                else if (tileRotations[tile] == 2)
                    return new int[] { 1, 2 };
                else
                    return new int[] { 1, 3 };
            case 1:
                if (tileRotations[tile] == 0 || tileRotations[tile] == 2)
                    return new int[] { 2, 3 };
                else
                    return new int[] { 0, 1 };
            default:
                if (tileRotations[tile] == 0)
                    return new int[] { 0, 1, 2 };
                else if (tileRotations[tile] == 1)
                    return new int[] { 1, 2, 3 };
                else if (tileRotations[tile] == 2)
                    return new int[] { 0, 1, 3 };
                else
                    return new int[] { 0, 2, 3 };
        }
    }

    void UpdateTile(int tile)
    {
        if (tile == 49)
        {
            extraTile.GetComponent<MeshRenderer>().material = tileMats[extraTileType];
            extraTile.transform.localEulerAngles = new Vector3(0, 90 * extraTileRotation, 0);
            if (extraTileNumber != -1)
            {
                if (extraTileNumber == 6)
                {
                    extraTileText.text = "!";
                    extraTileText.gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
                }
                else if (extraTileNumber == 9)
                {
                    extraTileText.text = "\"";
                    extraTileText.gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
                }
                else
                    extraTileText.text = extraTileNumber.ToString();
                if (extraTileType == 0)
                    extraTileText.gameObject.transform.localPosition = new Vector3(0, 0.01f, -0.05f);
                else if (extraTileType == 2 && extraTileNumber != 6 && extraTileNumber != 9)
                    extraTileText.gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.1f);
            }
            else
                extraTileText.text = "";
        }
        else
        {
            tiles[tile].GetComponent<MeshRenderer>().material = tileMats[tileTypes[tile]];
            tiles[tile].transform.localEulerAngles = new Vector3(0, 90 * tileRotations[tile], 0);
            if (tileNumbers[tile] != -1)
            {
                if (tileNumbers[tile] == 6)
                {
                    tileNumbersText[tile].text = "!";
                    tileNumbersText[tile].gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
                }
                else if (tileNumbers[tile] == 9)
                {
                    tileNumbersText[tile].text = "\"";
                    tileNumbersText[tile].gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.16f);
                }
                else
                    tileNumbersText[tile].text = tileNumbers[tile].ToString();
                if (tileTypes[tile] == 0)
                    tileNumbersText[tile].gameObject.transform.localPosition = new Vector3(0, 0.01f, -0.05f);
                else if (tileTypes[tile] == 2 && tileNumbers[tile] != 6 && tileNumbers[tile] != 9)
                    tileNumbersText[tile].gameObject.transform.localPosition = new Vector3(0, 0.01f, 0.1f);
            }
            else
                tileNumbersText[tile].text = "";
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} start <tl/tr/bl/br> [Presses the tile in the top left, top right, bottom left, or bottom right] | !{0} <ccw/cw> [Presses the counter-clockwise or clockwise button] | !{0} <l/r/u/d> [Presses the left, right, up, or down arrow button] | !{0} <coord> [Slides in a tile at the specified coordinate where A1 is top left and G7 is bottom right] | The previous three commands are chainable with spaces";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*start\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 2)
            {
                string[] positions = { "tl", "tr", "bl", "br" };
                if (!positions.Contains(parameters[1].ToLower()))
                {
                    yield return "sendtochaterror!f The specified position '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (startPlaced)
                {
                    yield return "sendtochaterror Your token has already been placed on the starting tile!";
                    yield break;
                }
                buttons[Array.IndexOf(positions, parameters[1].ToLower()) + 6].OnInteract();
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the position of a tile to press!";
            }
            yield break;
        }
        string[] moves = { "l", "r", "u", "d", "ccw", "cw" };
        string[] coords = { "b1", "d1", "f1", "b7", "d7", "f7", "a2", "a4", "a6", "g2", "g4", "g6" };
        for (int i = 0; i < parameters.Length; i++)
        {
            if (!moves.Contains(parameters[i].ToLower()) && !coords.Contains(parameters[i].ToLower()))
            {
                yield return "sendtochaterror!f The specified command or coord '" + parameters[i] + "' is invalid!";
                yield break;
            }
        }
        yield return null;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (moves.Contains(parameters[i].ToLower()))
                buttons[Array.IndexOf(moves, parameters[i].ToLower())].OnInteract();
            else
                buttons[Array.IndexOf(coords, parameters[i].ToLower()) + 10].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}