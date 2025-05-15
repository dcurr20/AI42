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

        // Re-enable the call to DealDominoes() so it runs:
        DealDominoes();

        // Optionally, you can leave out StartGameLoop() for now if you aren't testing it:
        // StartGameLoop();
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
        // Shuffle the dominoSet using the Fisher-Yates algorithm
        for (int i = 0; i < dominoSet.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, dominoSet.Count);
            Domino temp = dominoSet[i];
            dominoSet[i] = dominoSet[randomIndex];
            dominoSet[randomIndex] = temp;
        }

        // Calculate how many dominoes each player should receive.
        // For a standard double-six set, there are 28 dominoes.
        // With 4 players, each gets 7 dominoes.
        int handSize = dominoSet.Count / players.Count;

        // Assign dominoes to each player's hand
        for (int i = 0; i < players.Count; i++)
        {
            // Create a new hand list for the player
            players[i].Hand = new List<Domino>();
            // Deal 'handSize' dominoes to the player
            for (int j = 0; j < handSize; j++)
            {
                players[i].Hand.Add(dominoSet[i * handSize + j]);
            }
        }

        // Output each player's hand to the Console for verification
        foreach (Player player in players)
        {
            string handInfo = "Player " + player.Name + " hand: ";
            foreach (Domino d in player.Hand)
            {
                handInfo += d.ToString() + " ";
            }
            Debug.Log(handInfo);
        }
    }

    void StartGameLoop()
    {
        // For now, we are not implementing the full game loop.
        Debug.Log("StartGameLoop() placeholder called.");
    }
}
