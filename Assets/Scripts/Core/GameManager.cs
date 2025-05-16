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
    private Player trickLeader = null;

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

        // Correctly start the coroutine for GameLoop.
        StartCoroutine(GameLoop());
    }

    void InitializePlayers()
    {
        players = new List<Player>();
        players.Add(new Player("North", PlayerType.AI));
        players.Add(new Player("East", PlayerType.Human));
        players.Add(new Player("South", PlayerType.AI));
        players.Add(new Player("West", PlayerType.AI));
    }

    void DetermineInitialDealer()
    {
        Debug.Log("Determining initial dealer via one-domino draw...");
        List<Domino> tempSet = new List<Domino>();
        for (int i = 0; i <= 6; i++)
        {
            for (int j = i; j <= 6; j++)
            {
                tempSet.Add(new Domino(i, j));
            }
        }

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
        for (int i = 0; i < dominoSet.Count; i++)
        {
            int randomIndex = Random.Range(i, dominoSet.Count);
            Domino temp = dominoSet[i];
            dominoSet[i] = dominoSet[randomIndex];
            dominoSet[randomIndex] = temp;
        }

        List<Player> dealingOrder = new List<Player>();
        int count = players.Count;
        for (int i = 1; i < count; i++)
        {
            dealingOrder.Add(players[(dealerIndex + i) % count]);
        }
        dealingOrder.Add(players[dealerIndex]);

        string orderLog = "Dealing Order: ";
        foreach (Player p in dealingOrder)
        {
            orderLog += p.Name + " ";
        }
        Debug.Log(orderLog);

        foreach (Player p in players)
        {
            p.Hand = new List<Domino>();
        }

        int handSize = 7;
        for (int i = 0; i < dealingOrder.Count - 1; i++)
        {
            for (int j = 0; j < handSize; j++)
            {
                dealingOrder[i].Hand.Add(dominoSet[i * handSize + j]);
            }
        }

        int dealerStartIndex = (count - 1) * handSize;
        for (int j = 0; j < handSize; j++)
        {
            dealingOrder[count - 1].Hand.Add(dominoSet[dealerStartIndex + j]);
        }

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

    void RunBiddingPhase()
    {
        int currentHighBid = 0;
        winningBidder = null;

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
                winningBidder = player;
            }
        }

        Debug.Log("Highest bid: " + currentHighBid + (winningBidder != null ? " by " + winningBidder.Name : ""));
        if (winningBidder != null && currentHighBid > 0)
        {
            currentTrumpSuit = DetermineTrumpSuit(winningBidder);
            Debug.Log("Trump suit chosen by " + winningBidder.Name + ": " + currentTrumpSuit);
            trickLeader = winningBidder;
        }
    }

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

            Debug.Log("End of Round " + roundCounter + ". Round score: Team0: " + roundResult.team0Tricks + ", Team1: " + roundResult.team1Tricks);
            Debug.Log("Overall score: Team0: " + team0GameScore + ", Team1: " + team1GameScore);

            roundCounter++;

            dealerIndex = (dealerIndex + 1) % players.Count;
            Debug.Log("New dealer for next round is: " + players[dealerIndex].Name);

            yield return new WaitForSeconds(1.0f);
        }

        string winningTeam = (team0GameScore >= GAME_TARGET) ? "Team 0 (North, South)" : "Team 1 (East, West)";
        Debug.Log("Game over. Winner: " + winningTeam);
    }
    
    private RoundResult PlayRound()
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

    private int DetermineTrumpSuit(Player bidder)
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
            trickLeader = trickWinner;  // Ensure the next trick starts with the winner of this one.
        }
        else
        {
            Debug.Log("No trick winner could be determined.");
        }

        return trickWinner;
    }

}
