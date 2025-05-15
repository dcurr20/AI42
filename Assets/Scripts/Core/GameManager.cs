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

    // To track dealer rotations. dealerIndex represents the current dealer's index in the players list.
    private int dealerIndex = -1;

    // A simple struct to store the result of a round.
    private struct RoundResult
    {
        public int team0Tricks;
        public int team1Tricks;
    }

    void Start()
    {
        Debug.Log("GameManager Start() called.");

        // Initialize players (seating order remains fixed).
        InitializePlayers();
        Debug.Log("Players initialized.");

        // Determine the initial dealer by doing a one-domino draw.
        DetermineInitialDealer();

        // Start the overall game loop as a coroutine.
        StartCoroutine(GameLoop());
    }

    // Create 4 players with seating order assumed fixed.
    // For scoring we assume:
    // Team 0: North and South; Team 1: East and West.
    void InitializePlayers()
    {
        players = new List<Player>();
        players.Add(new Player("North", PlayerType.AI));   // Team 0
        players.Add(new Player("East", PlayerType.Human));   // Team 1
        players.Add(new Player("South", PlayerType.AI));     // Team 0
        players.Add(new Player("West", PlayerType.AI));      // Team 1
    }

    // Determines the initial dealer by having each player draw one domino.
    // The player with the highest total pips on the drawn domino becomes the dealer.
    void DetermineInitialDealer()
    {
        Debug.Log("Determining initial dealer via one-domino draw...");
        // Create a temporary domino set.
        List<Domino> tempSet = new List<Domino>();
        for (int i = 0; i <= 6; i++)
        {
            for (int j = i; j <= 6; j++)
            {
                tempSet.Add(new Domino(i, j));
            }
        }
        // Shuffle the temporary set using Fisher-Yates.
        for (int i = 0; i < tempSet.Count; i++)
        {
            int randIndex = Random.Range(i, tempSet.Count);
            Domino temp = tempSet[i];
            tempSet[i] = tempSet[randIndex];
            tempSet[randIndex] = temp;
        }
        int highestPips = -1;
        int chosenDealerIndex = -1;
        // Each player draws one domino.
        for (int i = 0; i < players.Count; i++)
        {
            Domino drawn = tempSet[i]; // For simplicity, player i draws tempSet[i]
            int pipTotal = drawn.SideA + drawn.SideB;
            Debug.Log("Player " + players[i].Name + " draws " + drawn.ToString() + " (Total pips: " + pipTotal + ")");
            if (pipTotal > highestPips)
            {
                highestPips = pipTotal;
                chosenDealerIndex = i;
            }
        }
        dealerIndex = chosenDealerIndex;
        Debug.Log("Initial Dealer is " + players[dealerIndex].Name);
    }

    // Reinitialize the main domino set (standard double-six: 28 dominoes).
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

    // Deal dominoes so that non-dealer players (3 players) draw their 7 dominoes first according to the correct drawing order,
    // and then the dealer gets his 7.
    void DealDominoes()
    {
        // Shuffle the dominoSet.
        for (int i = 0; i < dominoSet.Count; i++)
        {
            int randomIndex = Random.Range(i, dominoSet.Count);
            Domino temp = dominoSet[i];
            dominoSet[i] = dominoSet[randomIndex];
            dominoSet[randomIndex] = temp;
        }

        // Build the dealing order.
        // The drawing order starts with the player immediately to the left of the dealer and wraps around,
        // with the dealer receiving the dominoes last.
        // For our seating order [North, East, South, West]:
        // If the dealer is East (index 1), the expected drawing order is: South, West, North, then dealer East.
        List<Player> dealingOrder = new List<Player>();
        int count = players.Count;
        for (int i = 1; i < count; i++)
        {
            dealingOrder.Add(players[(dealerIndex + i) % count]);
        }
        dealingOrder.Add(players[dealerIndex]);

        // Debug: Print the computed dealing order.
        string orderLog = "Dealing Order: ";
        foreach (Player p in dealingOrder)
        {
            orderLog += p.Name + " ";
        }
        Debug.Log(orderLog);

        // Clear hands for all players.
        foreach (Player p in players)
        {
            p.Hand = new List<Domino>();
        }

        int handSize = 7;
        // Deal to non-dealers (first count-1 players in the dealingOrder).
        for (int i = 0; i < dealingOrder.Count - 1; i++)
        {
            for (int j = 0; j < handSize; j++)
            {
                dealingOrder[i].Hand.Add(dominoSet[i * handSize + j]);
            }
        }
        // Deal to the dealer (last in dealingOrder).
        int dealerStartIndex = (count - 1) * handSize;
        for (int j = 0; j < handSize; j++)
        {
            dealingOrder[count - 1].Hand.Add(dominoSet[dealerStartIndex + j]);
        }

        // Print each player's hand in the order they drew.
        Debug.Log("Player Hands in Drawing Order:");
        foreach (Player player in dealingOrder)
        {
            string handInfo = "Player " + player.Name + " hand: ";
            foreach (Domino d in player.Hand)
            {
                handInfo += d.ToString() + " ";
            }
            Debug.Log(handInfo);
        }
    }

    // Bidding phase: The bidding order is determined such that the dealer bids last.
    // We build a biddingOrder list starting with the player immediately to the left of the dealer and ending with the dealer.
    void RunBiddingPhase()
    {
        int currentHighBid = 0;
        string highBidder = "";

        List<Player> biddingOrder = new List<Player>();
        int count = players.Count;
        for (int i = 1; i < count; i++)
        {
            biddingOrder.Add(players[(dealerIndex + i) % count]);
        }
        biddingOrder.Add(players[dealerIndex]);

        foreach (Player player in biddingOrder)
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

    // Simulate one trick: Each player plays one domino; the trick winner is determined by the highest pip total.
    // Returns the winning player.
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

    // Play a full round (all tricks in the players’ current hands).
    private RoundResult PlayRound()
    {
        Debug.Log("Starting a full round...");
        int tricksPerRound = players[0].Hand.Count; // All players have 7 dominoes.
        RoundResult result = new RoundResult();
        result.team0Tricks = 0;
        result.team1Tricks = 0;

        for (int i = 0; i < tricksPerRound; i++)
        {
            Player winner = PlayTrick();
            if (winner != null)
            {
                // For scoring, assume Team 0 is North and South; Team 1 is East and West.
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

    // Overall game loop: Plays rounds until one team reaches the target score.
    // After each round, the dealer rotates to the left (to the next player in seating order).
    IEnumerator GameLoop()
    {
        team0GameScore = 0;
        team1GameScore = 0;
        int roundCounter = 1;

        while (team0GameScore < GAME_TARGET && team1GameScore < GAME_TARGET)
        {
            Debug.Log("----- Starting Round " + roundCounter + " -----");

            // Reinitialize the domino set & deal new hands.
            InitializeDominoSet();
            DealDominoes();
            RunBiddingPhase();
            RoundResult roundResult = PlayRound();
            team0GameScore += roundResult.team0Tricks;
            team1GameScore += roundResult.team1Tricks;
            Debug.Log("End of Round " + roundCounter + ". Round score: Team0: " + roundResult.team0Tricks + ", Team1: " + roundResult.team1Tricks);
            Debug.Log("Overall score: Team0: " + team0GameScore + ", Team1: " + team1GameScore);
            roundCounter++;

            // Rotate the dealer: move to the next player in the fixed seating order.
            dealerIndex = (dealerIndex + 1) % players.Count;
            Debug.Log("New dealer for next round is: " + players[dealerIndex].Name);

            yield return new WaitForSeconds(1.0f);  // Pause between rounds for clarity.
        }

        string winningTeam = (team0GameScore >= GAME_TARGET) ? "Team 0 (North, South)" : "Team 1 (East, West)";
        Debug.Log("Game over. Winner: " + winningTeam);
    }
}
