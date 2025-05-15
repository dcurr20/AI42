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
        PlayTrick();  // New: simulate one trick round.
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

        // Determine hand size: for 28 dominoes and 4 players, each gets 7.
        int handSize = dominoSet.Count / players.Count;
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

    // New method: simulate one trick round.
    void PlayTrick()
    {
        Debug.Log("Starting trick round...");
        // Each player plays one domino.
        Dictionary<Player, Domino> trickPlays = new Dictionary<Player, Domino>();

        foreach (Player player in players)
        {
            Domino played = player.PlayDomino();
            if (played != null)
            {
                trickPlays[player] = played;
                Debug.Log("Player " + player.Name + " plays: " + played.ToString());
            }
            else
            {
                Debug.Log("Player " + player.Name + " has no domino left to play.");
            }
        }

        // Determine the winning domino based on highest pip total (sideA + sideB).
        Player trickWinner = null;
        int highestPipTotal = -1;
        foreach (KeyValuePair<Player, Domino> kvp in trickPlays)
        {
            int pipTotal = kvp.Value.SideA + kvp.Value.SideB;
            if (pipTotal > highestPipTotal)
            {
                highestPipTotal = pipTotal;
                trickWinner = kvp.Key;
            }
        }

        if (trickWinner != null)
        {
            Debug.Log("Trick winner is " + trickWinner.Name + " with a total of " + highestPipTotal);
        }
        else
        {
            Debug.Log("No trick winner could be determined.");
        }
    }
}
