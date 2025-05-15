using System;

using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public List<Player> players;

    private List<Domino> dominoSet;

    void Start()
    {
        InitializePlayers();
        InitializeDominoSet();
        DealDominoes();
        StartGameLoop();
    }

    void InitializePlayers()
    {
        // Example: Create 4 players (adjust based on human vs. AI setup)  
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
        // Shuffle dominoSet and deal evenly to players  
        // The exact mechanism might consider game variations  
    }

    void StartGameLoop()
    {
        // Handle bidding, trick rounds and scoring based on your rules  
    }

    // Additional methods for bidding, playing rounds, and updating scores…  
}
