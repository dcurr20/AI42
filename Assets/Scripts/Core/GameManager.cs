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

        // Play a full round (all tricks in the hand)
        PlayRound();
    }

    void InitializePlayers()
    {
        // Create 4 players.
        // For scoring, we assume:
        // Team 0: North and South; Team 1: East and West.
        players = new List<Player>();
        players.Add(new Player("North", PlayerType.AI));   // Team 0
        players.Add(new Player("East", PlayerType.Human));   // Team 1
        players.Add(new Player("South", PlayerType.AI));     // Team 0
        players.Add(new Player("West", PlayerType.AI));      // Team 1
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

        // For 28 dominoes and 4 players, each gets 7.
        int handSize = dominoSet.Count / players.Count;

        for (int i = 0; i < players.Count; i++)
        {
            players[i].Hand = new List<Domino>();
            for (int j = 0; j < handSize; j++)
            {
                players[i].Hand.Add(dominoSet[i * handSize + j]);
            }
        }

        // Output each player's hand for verification.
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

    // Modified PlayTrick now returns the Player who wins the trick.
    Player PlayTrick()
    {
        Debug.Log("Starting trick round...");
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
        return trickWinner;
    }

    void PlayRound()
    {
        Debug.Log("Starting a full round...");
        // Assume every player has the same number of dominoes.
        int tricksPerRound = players[0].Hand.Count;

        // Initialize trick counts for teams.
        int team0Tricks = 0, team1Tricks = 0;

        for (int i = 0; i < tricksPerRound; i++)
        {
            // Play one trick and get the winner.
            Player winner = PlayTrick();
            if (winner != null)
            {
                // Assign the trick to a team based on the player name.
                if (winner.Name == "North" || winner.Name == "South")
                    team0Tricks++;
                else if (winner.Name == "East" || winner.Name == "West")
                    team1Tricks++;
            }
        }

        // Log the team trick counts.
        Debug.Log("Round completed.");
        Debug.Log("Team 0 (North, South) won " + team0Tricks + " tricks.");
        Debug.Log("Team 1 (East, West) won " + team1Tricks + " tricks.");
        // Future scoring rules can be applied here.
    }
}
