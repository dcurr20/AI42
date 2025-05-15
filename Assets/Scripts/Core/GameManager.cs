using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public List<Player> players;
    private List<Domino> dominoSet;

    // Overall game scores for each team.
    private int team0GameScore = 0;
    private int team1GameScore = 0;
    private const int GAME_TARGET = 10;  // First team to reach (or exceed) 10 wins.

    // A simple struct to store the result of a round.
    private struct RoundResult
    {
        public int team0Tricks;
        public int team1Tricks;
    }

    void Start()
    {
        Debug.Log("GameManager Start() called.");
        InitializePlayers();
        Debug.Log("Players initialized.");
        // Start the overall game loop as a coroutine.
        StartCoroutine(GameLoop());
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

        // For 28 dominoes and 4 players, each gets 7 dominoes.
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

    // Simulate one trick round; each player plays one domino.
    // The trick winner is determined by the highest pip total.
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

    // Play one full round (all tricks in the players' hands).
    RoundResult PlayRound()
    {
        Debug.Log("Starting a full round...");
        int tricksPerRound = players[0].Hand.Count;
        RoundResult result = new RoundResult();
        result.team0Tricks = 0;
        result.team1Tricks = 0;

        for (int i = 0; i < tricksPerRound; i++)
        {
            Player winner = PlayTrick();
            if (winner != null)
            {
                if (winner.Name == "North" || winner.Name == "South")
                    result.team0Tricks++;
                else if (winner.Name == "East" || winner.Name == "West")
                    result.team1Tricks++;
            }
        }
        Debug.Log("Round completed.");
        Debug.Log("Team 0 (North, South) won " + result.team0Tricks + " tricks.");
        Debug.Log("Team 1 (East, West) won " + result.team1Tricks + " tricks.");
        return result;
    }

    // The overall game loop that plays rounds until a team reaches the target score.
    IEnumerator GameLoop()
    {
        team0GameScore = 0;
        team1GameScore = 0;
        int roundCounter = 1;

        while (team0GameScore < GAME_TARGET && team1GameScore < GAME_TARGET)
        {
            Debug.Log("----- Starting Round " + roundCounter + " -----");

            // Reinitialize the domino set and deal new hands every round.
            InitializeDominoSet();
            DealDominoes();
            RunBiddingPhase();
            RoundResult roundResult = PlayRound();
            team0GameScore += roundResult.team0Tricks;
            team1GameScore += roundResult.team1Tricks;
            Debug.Log("End of Round " + roundCounter + ". Round score: Team0: " + roundResult.team0Tricks + ", Team1: " + roundResult.team1Tricks);
            Debug.Log("Overall score: Team0: " + team0GameScore + ", Team1: " + team1GameScore);
            roundCounter++;

            yield return new WaitForSeconds(1.0f);  // Pause between rounds for readability.
        }

        string winningTeam = (team0GameScore >= GAME_TARGET) ? "Team 0 (North, South)" : "Team 1 (East, West)";
        Debug.Log("Game over. Winner: " + winningTeam);
    }
}
