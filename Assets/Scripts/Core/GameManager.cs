using System;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public List<Player> players;
    private List<Domino> dominoSet;

    void Start()
    {
        Debug.Log("GameManager Start() called.");

        InitializePlayers();
        Debug.Log("Players initialized.");

        InitializeDominoSet();
        Debug.Log("Domino set created.");

        // DealDominoes();
        // Debug.Log("DealDominoes() placeholder called.");

        // StartGameLoop();
        // Debug.Log("StartGameLoop() placeholder called.");
    }

    void InitializePlayers()
    {
        players = new List<Player>();
        players.Add(new Player("North", PlayerType.AI));
        players.Add(new Player("East", PlayerType.Human));
        players.Add(new Player("South", PlayerType.AI));
        players.Add(new Player("West", PlayerType.AI));
    }

    void InitializeDominoSet()
    {
        dominoSet = new List<Domino>();
        for (int i = 0; i <= 6; i++)
        {
            for (int j = i; j <= 6; j++)
            {
                dominoSet.Add(new Domino(i, j));
            }
        }
    }

    void DealDominoes()
    {
        // For now, we are not implementing full deal logic.
        Debug.Log("DealDominoes() placeholder called.");
    }

    void StartGameLoop()
    {
        // For now, we are not implementing the full game loop.
        Debug.Log("StartGameLoop() placeholder called.");
    }
}
