using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public List<Player> players;
    private List<Domino> dominoSet;

    private int team0GameScore = 0;
    private int team1GameScore = 0;
    private const int GAME_TARGET = 10;

    private int dealerIndex = -1;
    private int currentTrumpSuit = -1;
    private Player winningBidder = null;
    private Player trickLeader = null; // will hold the leader for the current trick

    // Structure to record a round's result
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
        DetermineInitialDealer();
        StartCoroutine(GameLoop());
    }

    // Create four players in fixed seating order.
    void InitializePlayers()
    {
        players = new List<Player>();
        // For testing the ML Agent integration, let’s assume "North" is controlled by ML.
        players.Add(new Player("North", PlayerType.AI));
        players.Add(new Player("East", PlayerType.Human));
        players.Add(new Player("South", PlayerType.AI));
        players.Add(new Player("West", PlayerType.AI));
    }

    // Determine the initial dealer using one-domino draw.
    void DetermineInitialDealer()
    {
        Debug.Log("Determining initial dealer via one-domino draw...");
        List<Domino> tempSet = new List<Domino>();
        for (int i = 0; i <= 6; i++)
        {
            for (int j = i; j <= 6; j++)
                tempSet.Add(new Domino(i, j));
        }
        // Shuffle the temp set using Fisher-Yates.
        for (int i = 0; i < tempSet.Count; i++)
        {
            int randIndex = Random.Range(i, tempSet.Count);
            Domino temp = tempSet[i];
            tempSet[i] = tempSet[randIndex];
            tempSet[randIndex] = temp;
        }
        int highestPips = -1;
        int chosenDealerIndex = -1;
        for (int i = 0; i < players.Count; i++)
        {
            Domino drawn = tempSet[i];
            int pipTotal = drawn.SideA + drawn.SideB;
            Debug.Log("Player " + players[i].Name + " draws " + drawn.ToString() +
                      " (Total pips: " + pipTotal + ")");
            if (pipTotal > highestPips)
            {
                highestPips = pipTotal;
                chosenDealerIndex = i;
            }
        }
        dealerIndex = chosenDealerIndex;
        Debug.Log("Initial Dealer is " + players[dealerIndex].Name);
    }

    // Prepare the main domino set.
    void InitializeDominoSet()
    {
        dominoSet = new List<Domino>();
        for (int i = 0; i <= 6; i++)
        {
            for (int j = i; j <= 6; j++)
                dominoSet.Add(new Domino(i, j));
        }
    }

    // Shuffle and deal dominoes.
    void DealDominoes()
    {
        // Shuffle dominoSet in place.
        for (int i = 0; i < dominoSet.Count; i++)
        {
            int randomIndex = Random.Range(i, dominoSet.Count);
            Domino temp = dominoSet[i];
            dominoSet[i] = dominoSet[randomIndex];
            dominoSet[randomIndex] = temp;
        }
        // Build the dealing order: starting with player immediately to dealer's left.
        List<Player> dealingOrder = new List<Player>();
        int count = players.Count;
        for (int i = 1; i < count; i++)
        {
            dealingOrder.Add(players[(dealerIndex + i) % count]);
        }
        dealingOrder.Add(players[dealerIndex]); // Dealer dealt last

        string orderLog = "Dealing Order: ";
        foreach (Player p in dealingOrder)
        {
            orderLog += p.Name + " ";
        }
        Debug.Log(orderLog);

        // Clear hands.
        foreach (Player p in players)
            p.Hand = new List<Domino>();

        int handSize = 7;
        // Deal to non-dealers.
        for (int i = 0; i < dealingOrder.Count - 1; i++)
        {
            for (int j = 0; j < handSize; j++)
                dealingOrder[i].Hand.Add(dominoSet[i * handSize + j]);
        }
        // Deal to dealer.
        int dealerStartIndex = (count - 1) * handSize;
        for (int j = 0; j < handSize; j++)
            dealingOrder[count - 1].Hand.Add(dominoSet[dealerStartIndex + j]);

        Debug.Log("Player Hands in Drawing Order:");
        foreach (Player player in dealingOrder)
        {
            string handInfo = "Player " + player.Name + " hand: ";
            foreach (Domino d in player.Hand)
                handInfo += d.ToString() + " ";
            Debug.Log(handInfo);
        }
    }

    // Bidding phase: players bid in turn (starting with player to dealer's left, dealer bids last).
    // For testing, we override the bid for "North" using the ML Agent's decision.
    void RunBiddingPhase()
    {
        int currentHighBid = 0;
        winningBidder = null;

        List<Player> biddingOrder = new List<Player>();
        int count = players.Count;
        for (int i = 1; i < count; i++)
            biddingOrder.Add(players[(dealerIndex + i) % count]);
        biddingOrder.Add(players[dealerIndex]);

        // Look for the ML Agent attached to AgentController.
        AI42Agent mlAgent = null;
        GameObject agentControllerGO = GameObject.Find("AgentController");
        if (agentControllerGO != null)
        {
            mlAgent = agentControllerGO.GetComponent<AI42Agent>();
        }

        foreach (Player player in biddingOrder)
        {
            int bid = currentHighBid; // default bid value

            if (player.Name == "North" && mlAgent != null)
            {
                // Use the bid provided by the ML agent.
                bid = mlAgent.agentBid;
                Debug.Log("ML Agent for " + player.Name + " bids: " + bid);
            }
            else
            {
                bid = player.Bid(currentHighBid);
                if (bid == currentHighBid)
                    Debug.Log("Player " + player.Name + " bids: Pass");
                else
                    Debug.Log("Player " + player.Name + " bids: " + bid);
            }
            if (bid > currentHighBid)
            {
                currentHighBid = bid;
                winningBidder = player;
            }
        }

        Debug.Log("Highest bid: " + currentHighBid + (winningBidder != null ? " by " + winningBidder.Name : ""));
        if (winningBidder != null && currentHighBid > 0)
        {
            currentTrumpSuit = DetermineTrumpSuit(winningBidder);
            Debug.Log("Trump suit chosen by " + winningBidder.Name + ": " + currentTrumpSuit);
            // Winning bidder leads the first trick.
            trickLeader = winningBidder;
        }
        else
        {
            trickLeader = players[(dealerIndex + 1) % count];  // fallback
        }
    }

    // Determine trump suit by counting frequency of pips in the bidder's hand.
    int DetermineTrumpSuit(Player bidder)
    {
        int[] frequency = new int[7];
        foreach (Domino d in bidder.Hand)
        {
            frequency[d.SideA]++;
            frequency[d.SideB]++;
        }
        int maxFreq = -1, trump = 0;
        for (int i = 0; i < 7; i++)
        {
            if (frequency[i] > maxFreq || (frequency[i] == maxFreq && i > trump))
            {
                maxFreq = frequency[i];
                trump = i;
            }
        }
        return trump;
    }

    // Play a single trick: starting with the current trickLeader.
    Player PlayTrick()
    {
        Debug.Log("Starting trick round. Current trick leader: " + trickLeader.Name);
        Dictionary<Player, Domino> trickPlays = new Dictionary<Player, Domino>();

        int trickStartIndex = players.IndexOf(trickLeader);
        for (int i = 0; i < players.Count; i++)
        {
            Player currentPlayer = players[(trickStartIndex + i) % players.Count];
            Domino played = currentPlayer.PlayDomino();
            if (played != null)
            {
                trickPlays[currentPlayer] = played;
                Debug.Log("Player " + currentPlayer.Name + " plays: " + played.ToString());
            }
            else
            {
                Debug.Log("Player " + currentPlayer.Name + " has no domino left to play.");
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
            Debug.Log("Trick winner is " + trickWinner.Name + " with total pips " + highestPipTotal);
            trickLeader = trickWinner; // Update leader for next trick.
        }
        else
        {
            Debug.Log("No trick winner could be determined.");
        }
        return trickWinner;
    }

    // Play a full round (each player plays one domino per trick).
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

    // The overall game loop.
    IEnumerator GameLoop()
    {
        team0GameScore = 0;
        team1GameScore = 0;
        int roundCounter = 1;
        while (team0GameScore < GAME_TARGET && team1GameScore < GAME_TARGET)
        {
            Debug.Log("----- Starting Round " + roundCounter + " -----");

            InitializeDominoSet();
            DealDominoes();
            RunBiddingPhase();
            RoundResult roundResult = PlayRound();

            team0GameScore += roundResult.team0Tricks;
            team1GameScore += roundResult.team1Tricks;

            Debug.Log("End of Round " + roundCounter + ". Round score: Team0: " + roundResult.team0Tricks +
                      ", Team1: " + roundResult.team1Tricks);
            Debug.Log("Overall score: Team0: " + team0GameScore + ", Team1: " + team1GameScore);

            roundCounter++;

            // Rotate dealer for next round.
            dealerIndex = (dealerIndex + 1) % players.Count;
            Debug.Log("New dealer for next round is: " + players[dealerIndex].Name);

            yield return new WaitForSeconds(1.0f);
        }
        string winningTeam = (team0GameScore >= GAME_TARGET) ? "Team 0 (North, South)" : "Team 1 (East, West)";
        Debug.Log("Game over. Winner: " + winningTeam);
    }

    // Stub methods to support ML Agent observations.
    public int GetCurrentBid()
    {
        // Placeholder: return a dummy current bid.
        return 30;
    }

    public int CalculateHandValue()
    {
        // Placeholder: return a dummy hand value.
        return 50;
    }
}
