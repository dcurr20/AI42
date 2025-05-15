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

        DealDominoes();
        RunBiddingPhase();

        // Future implementation for full game loop:
        // StartGameLoop();
    }

    void InitializePlayers()
    {
        // Create 4 players; adjust as needed.
        players = new List<Player>();
        players.Add(new Player("North", PlayerType.AI));
        players.Add(new Player("East", PlayerType.Human));
        players.Add(new Player("South", PlayerType.AI));
        players.Add(new Player("West", PlayerType.AI));
    }

    void InitializeDominoSet()
    {
        // Create a standard double-six set (28 dominoes).
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
        // Shuffle the dominoSet using the Fisher-Yates algorithm.
        for (int i = 0; i < dominoSet.Count; i++)
        {
            int randomIndex = Random.Range(i, dominoSet.Count);
            Domino temp = dominoSet[i];
            dominoSet[i] = dominoSet[randomIndex];
            dominoSet[randomIndex] = temp;
        }

        // Determine how many dominoes each player gets.
        int handSize = dominoSet.Count / players.Count;

        // Distribute dominoes to each player's hand.
        for (int i = 0; i < players.Count; i++)
        {
            players[i].Hand = new List<Domino>();
            for (int j = 0; j < handSize; j++)
            {
                players[i].Hand.Add(dominoSet[i * handSize + j]);
            }
        }

        // Output each player's hand to the Console for verification.
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

    void RunBiddingPhase()
    {
        int currentHighBid = 0;
        string highBidder = "";

        // Simulate a simple bidding phase.
        foreach (Player player in players)
        {
            int bid = player.Bid(currentHighBid);
            Debug.Log("Player " + player.Name + " bids: " + bid);

            if (bid > currentHighBid)
            {
                currentHighBid = bid;
                highBidder = player.Name;
            }
        }
        Debug.Log("Highest bid: " + currentHighBid + " by " + highBidder);
    }

    // Future placeholder for a full game loop:
    // void StartGameLoop()
    // {
    //     Debug.Log("StartGameLoop() placeholder called.");
    // }
}
