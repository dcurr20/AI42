// ========================================================================
// GameManager.cs
// Merged Version for AI42 (v1.1-mlagents)
// This version combines the original core game logic (team registration,
// player positioning via spawnPoints, win condition check, etc.) with a new
// overall training/game loop. The overall game loop starts with player
// initialization and dealer determination, runs rounds until one team reaches
// the target score, then ends the episode (via EndEpisode on agents), waits
// briefly, and then starts a new overall game (with a re-determined dealer).
// ========================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.MLAgents;  // Required for ML-Agents training.
using System;          // For Array.IndexOf and Array.Find

namespace AI42.Core
{
    public class GameManager : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // --- Public fields (set via the Inspector).
        public GameObject[] redTeamStartingPlayers;
        public GameObject[] blueTeamStartingPlayers;
        // Spawn points for positioning players.
        public List<Transform> spawnPoints;
        // A TextMeshPro element to display overall game status (winner, etc.)
        public TMP_Text statusText;
        // Overall game target (e.g. first team to reach 7 points wins)
        public int GAME_TARGET = 7;

        // --------------------------------------------------------------------
        // --- Private fields for game state.
        private int team0GameScore;    // Overall score for Team 0 (e.g., North, South)
        private int team1GameScore;    // Overall score for Team 1 (e.g., East, West)
        private int dealerIndex;       // Index of the current dealer in the players array

        // Fields for training auto-reset timing.
        [SerializeField] private float autoResetDelay = 2f;  // Delay between overall games
        private bool episodeEnded = false;                  // Flag: ensures EndEpisode() is only called once per game

        // Original multi-agent groups and lists.
        private List<GameObject> redTeamPlayers = new List<GameObject>();
        private List<GameObject> blueTeamPlayers = new List<GameObject>();
        private SimpleMultiAgentGroup redTeamGroup = new SimpleMultiAgentGroup();
        private SimpleMultiAgentGroup blueTeamGroup = new SimpleMultiAgentGroup();

        // --------------------------------------------------------------------
        // Additional fields required for game and training logic.
        private Player winningBidder;
        private int currentTrumpSuit;
        private Player trickLeader;
        private List<Domino> dominoSet;

        // We'll combine red and blue team GameObjects into a Player[] array.
        private Player[] players;

        // --------------------------------------------------------------------
        // Start(): Called on scene initialization.
        // Registers players, sets up positions, determines the initial dealer,
        // and starts the overall game loop.
        void Start()
        {
            // Register red team players with the red team group.
            foreach (var go in redTeamStartingPlayers)
            {
                if (go != null)
                {
                    redTeamGroup.RegisterAgent(go.GetComponent<Agent>());
                }
            }
            // Register blue team players with the blue team group.
            foreach (var go in blueTeamStartingPlayers)
            {
                if (go != null)
                {
                    blueTeamGroup.RegisterAgent(go.GetComponent<Agent>());
                }
            }

            // Initialize players (convert GameObjects to Player components).
            InitializePlayers();

            // Determine the initial dealer.
            DetermineInitialDealer();

            // Hide win/status text.
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("GameManager: statusText is not assigned in the Inspector.");
            }

            // Start the overall game loop.
            StartCoroutine(RunOverallGameLoop());
        }

        // --------------------------------------------------------------------
        // RunOverallGameLoop(): Overall training/game loop.
        // Wraps a single game (a series of rounds until one team wins) and,
        // after signaling EndEpisode() to agents, waits before starting a new game.
        IEnumerator RunOverallGameLoop()
        {
            while (true) // Loop indefinitely for continuous training episodes.
            {
                // Reset overall game scores.
                team0GameScore = 0;
                team1GameScore = 0;
                Debug.Log("Starting a new overall game.");

                // Start the internal game loop (round-by-round play until one team wins).
                yield return StartCoroutine(GameLoop());

                // Game over—determine the overall winner.
                string overallWinner = (team0GameScore >= GAME_TARGET) ? "Team 0 (North, South)" : "Team 1 (East, West)";
                Debug.Log("Overall Game Over. Winner: " + overallWinner);
                if (statusText != null)
                {
                    statusText.text = "Game Over. Winner: " + overallWinner;
                    statusText.gameObject.SetActive(true);
                }

                // --- Signal to ML-Agents that the training episode is finished.
                if (!episodeEnded)
                {
                    // Use the new API to find all agents (no sorting needed).
                    AI42Agent[] agents = UnityEngine.Object.FindObjectsByType<AI42Agent>(UnityEngine.FindObjectsSortMode.None);
                    foreach (var agent in agents)
                    {
                        agent.EndEpisode();
                    }
                    episodeEnded = true;
                }

                // Wait a bit before starting a new overall game.
                yield return new WaitForSeconds(autoResetDelay);

                // Hide win/status text for the new game.
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(false);
                }

                // Reinitialize additional overall game state (positions and dealer) for the new training episode.
                ResetOverallGameState();

                // Reset flag for the next training episode.
                episodeEnded = false;

                Debug.Log("New training episode starting.");
            }
        }

        // --------------------------------------------------------------------
        // GameLoop(): Internal loop for playing rounds until one team's score meets GAME_TARGET.
        IEnumerator GameLoop()
        {
            int roundCounter = 1;
            while (team0GameScore < GAME_TARGET && team1GameScore < GAME_TARGET)
            {
                Debug.Log("----- Starting Round " + roundCounter + " -----");

                // Initialize the domino set for this round.
                InitializeDominoSet();

                // Deal dominoes to players.
                DealDominoes();

                // Run the bidding phase.
                RunBiddingPhase();

                // Play the round and get its result.
                RoundResult roundResult = PlayRound();

                // Update overall scores.
                team0GameScore += roundResult.team0Tricks;
                team1GameScore += roundResult.team1Tricks;

                Debug.Log("End of Round " + roundCounter + ". Round score: Team0: " + roundResult.team0Tricks +
                          ", Team1: " + roundResult.team1Tricks);
                Debug.Log("Overall score: Team0: " + team0GameScore + ", Team1: " + team1GameScore);

                roundCounter++;

                // Rotate dealer for next round (if players exist).
                if (players != null && players.Length > 0)
                {
                    dealerIndex = (dealerIndex + 1) % players.Length;
                    Debug.Log("New dealer for next round is: " + players[dealerIndex].Name);
                }

                // Wait briefly between rounds.
                yield return new WaitForSeconds(1.0f);
            }
            yield break; // End the game loop when GAME_TARGET is reached.
        }

        // --------------------------------------------------------------------
        // InitializePlayers(): Combines red and blue team GameObjects into a Player[] array
        // and positions them using ResetPlayers().
        void InitializePlayers()
        {
            List<Player> allPlayers = new List<Player>();

            // Convert red team GameObjects.
            foreach (var go in redTeamStartingPlayers)
            {
                if (go != null)
                {
                    Player p = go.GetComponent<Player>();
                    if (p != null)
                        allPlayers.Add(p);
                    else
                        Debug.LogWarning("Red team GameObject " + go.name + " lacks a Player component.");
                }
            }
            // Convert blue team GameObjects.
            foreach (var go in blueTeamStartingPlayers)
            {
                if (go != null)
                {
                    Player p = go.GetComponent<Player>();
                    if (p != null)
                        allPlayers.Add(p);
                    else
                        Debug.LogWarning("Blue team GameObject " + go.name + " lacks a Player component.");
                }
            }
            players = allPlayers.ToArray();

            // If exactly 4 players, assign default names if they're not already set.
            if (players.Length == 4)
            {
                string[] defaultNames = new string[] { "South", "West", "North", "East" };
                for (int i = 0; i < players.Length; i++)
                {
                    if (string.IsNullOrEmpty(players[i].Name))
                    {
                        players[i].Name = defaultNames[i];
                    }
                }
            }

            // Position the players.
            ResetPlayers();

            Debug.Log("InitializePlayers() called.");
        }

        // --------------------------------------------------------------------
        // DetermineInitialDealer(): Chooses the initial dealer via a one-domino draw.
        void DetermineInitialDealer()
        {
            Debug.Log("Determining initial dealer via one-domino draw...");
            if (players == null || players.Length == 0)
            {
                Debug.LogError("No players found! Cannot determine dealer.");
                return;
            }
            // Build a temporary full domino set.
            List<Domino> tempSet = new List<Domino>();
            for (int i = 0; i <= 6; i++)
            {
                for (int j = i; j <= 6; j++)
                    tempSet.Add(new Domino(i, j));
            }
            // Shuffle using Fisher-Yates.
            for (int i = 0; i < tempSet.Count; i++)
            {
                int randIndex = UnityEngine.Random.Range(i, tempSet.Count);
                Domino temp = tempSet[i];
                tempSet[i] = tempSet[randIndex];
                tempSet[randIndex] = temp;
            }
            int highestPips = -1;
            int chosenDealerIndex = -1;
            // Use the first few dominoes from the shuffled set.
            for (int i = 0; i < players.Length; i++)
            {
                if (i >= tempSet.Count)
                {
                    Debug.LogWarning("Not enough dominoes for player " + i + ". Using default value.");
                    break;
                }
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
            if (chosenDealerIndex == -1)
            {
                chosenDealerIndex = 0;
                Debug.LogWarning("No valid dealer found. Defaulting to index 0.");
            }
            dealerIndex = chosenDealerIndex;
            Debug.Log("Initial Dealer is " + players[dealerIndex].Name);
        }

        // --------------------------------------------------------------------
        // InitializeDominoSet(): Creates a full set of dominoes (0-6).
        void InitializeDominoSet()
        {
            Debug.Log("InitializeDominoSet() called.");
            dominoSet = new List<Domino>();
            for (int i = 0; i <= 6; i++)
            {
                for (int j = i; j <= 6; j++)
                    dominoSet.Add(new Domino(i, j));
            }
        }

        // --------------------------------------------------------------------
        // DealDominoes(): Shuffles and deals dominoes to players.
        void DealDominoes()
        {
            Debug.Log("DealDominoes() called.");
            // Shuffle the domino set.
            for (int i = 0; i < dominoSet.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, dominoSet.Count);
                Domino temp = dominoSet[i];
                dominoSet[i] = dominoSet[randomIndex];
                dominoSet[randomIndex] = temp;
            }
            // Build the dealing order: players starting from the dealer's left.
            List<Player> dealingOrder = new List<Player>();
            int count = players.Length;
            for (int i = 1; i < count; i++)
            {
                dealingOrder.Add(players[(dealerIndex + i) % count]);
            }
            // Dealer is added last.
            dealingOrder.Add(players[dealerIndex]);

            string orderLog = "Dealing Order: ";
            foreach (Player p in dealingOrder)
                orderLog += p.Name + " ";
            Debug.Log(orderLog);

            // Clear each player's hand.
            foreach (Player p in players)
                p.Hand = new List<Domino>();

            int handSize = 7;

            // Deal dominoes to non-dealers.
            for (int i = 0; i < dealingOrder.Count - 1; i++)
            {
                for (int j = 0; j < handSize; j++)
                    dealingOrder[i].Hand.Add(dominoSet[i * handSize + j]);
            }
            // Deal dominoes to the dealer.
            int dealerStartIndex = (count - 1) * handSize;
            for (int j = 0; j < handSize; j++)
                dealingOrder[count - 1].Hand.Add(dominoSet[dealerStartIndex + j]);

            Debug.Log("Player hands after dealing:");
            foreach (Player p in dealingOrder)
            {
                string handInfo = "Player " + p.Name + " hand: ";
                foreach (Domino d in p.Hand)
                    handInfo += d.ToString() + " ";
                Debug.Log(handInfo);
            }
        }

        // --------------------------------------------------------------------
        // RunBiddingPhase(): Handles the bidding phase.
        // For player "North", the bid is taken from the ML Agent.
        void RunBiddingPhase()
        {
            Debug.Log("RunBiddingPhase() called.");
            int currentHighBid = 0;
            winningBidder = null;

            List<Player> biddingOrder = new List<Player>();
            int count = players.Length;
            for (int i = 1; i < count; i++)
                biddingOrder.Add(players[(dealerIndex + i) % count]);
            biddingOrder.Add(players[dealerIndex]); // Dealer is last.

            // Locate ML Agent from "AgentController".
            AI42Agent mlAgent = null;
            GameObject agentControllerGO = GameObject.Find("AgentController");
            if (agentControllerGO != null)
                mlAgent = agentControllerGO.GetComponent<AI42Agent>();

            foreach (Player player in biddingOrder)
            {
                int bid = currentHighBid; // Default bid.
                if (player.Name == "North" && mlAgent != null)
                {
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

            Debug.Log("Highest bid: " + currentHighBid +
                      (winningBidder != null ? " by " + winningBidder.Name : ""));
            if (winningBidder != null && currentHighBid > 0)
            {
                currentTrumpSuit = DetermineTrumpSuit(winningBidder);
                Debug.Log("Trump suit chosen by " + winningBidder.Name + ": " + currentTrumpSuit);
                trickLeader = winningBidder; // Winning bidder leads the first trick.
            }
            else
            {
                trickLeader = players[(dealerIndex + 1) % count]; // Fallback.
            }

            // Adjust rewards for bidding.
            if (mlAgent != null)
            {
                if (winningBidder != null && winningBidder.Name == "North")
                {
                    mlAgent.AddReward(1.0f);
                    Debug.Log("ML Agent rewarded +1.0 for winning the bid.");
                }
                else
                {
                    mlAgent.AddReward(-0.5f);
                    Debug.Log("ML Agent penalized -0.5 for not winning the bid.");
                }
            }
        }

        // --------------------------------------------------------------------
        // DetermineTrumpSuit(): Determines trump based on the bidder's hand.
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

        // --------------------------------------------------------------------
        // PlayRound(): Plays one full round (hand) and aggregates trick wins.
        RoundResult PlayRound()
        {
            Debug.Log("PlayRound() called.");
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

        // --------------------------------------------------------------------
        // PlayTrick(): Plays a single trick—each player plays one domino—and returns the trick winner.
        Player PlayTrick()
        {
            Debug.Log("Starting trick round. Current trick leader: " + trickLeader.Name);
            Dictionary<Player, Domino> trickPlays = new Dictionary<Player, Domino>();

            int trickStartIndex = Array.IndexOf(players, trickLeader);
            int count = players.Length;
            for (int i = 0; i < count; i++)
            {
                Player currentPlayer = players[(trickStartIndex + i) % count];
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
                trickLeader = trickWinner; // Update trick leader.

                // Reward the ML Agent for trick play.
                AI42Agent mlAgent = null;
                GameObject agentControllerGO = GameObject.Find("AgentController");
                if (agentControllerGO != null)
                    mlAgent = agentControllerGO.GetComponent<AI42Agent>();

                if (mlAgent != null)
                {
                    if (trickWinner.Name == "North")
                    {
                        mlAgent.AddReward(0.5f);
                        Debug.Log("ML Agent rewarded +0.5 for winning the trick.");
                    }
                    else
                    {
                        mlAgent.AddReward(-0.2f);
                        Debug.Log("ML Agent penalized -0.2 for losing the trick.");
                    }
                }
                else
                {
                    Debug.Log("ML Agent not found; no reward adjustment.");
                }

                return trickWinner;
            }
            else
            {
                Debug.Log("No trick winner could be determined.");
                return null;
            }
        }

        // --------------------------------------------------------------------
        // ResetPlayers(): Resets player positions using the assigned spawnPoints.
        public void ResetPlayers()
        {
            Debug.Log("ResetPlayers() called.");
            // Reset red team positions.
            for (int i = 0; i < redTeamStartingPlayers.Length && i < spawnPoints.Count; i++)
            {
                if (redTeamStartingPlayers[i] != null)
                {
                    redTeamStartingPlayers[i].transform.position = spawnPoints[i].position;
                    redTeamStartingPlayers[i].transform.rotation = Quaternion.identity;
                }
            }
            // Reset blue team positions.
            for (int i = 0; i < blueTeamStartingPlayers.Length && (i + redTeamStartingPlayers.Length) < spawnPoints.Count; i++)
            {
                if (blueTeamStartingPlayers[i] != null)
                {
                    blueTeamStartingPlayers[i].transform.position = spawnPoints[i + redTeamStartingPlayers.Length].position;
                    blueTeamStartingPlayers[i].transform.rotation = Quaternion.identity;
                }
            }
        }

        // --------------------------------------------------------------------
        // Stub Methods to satisfy external references.
        // Replace these with your actual game logic as needed.

        // Returns dummy current bid.
        public int GetCurrentBid()
        {
            return 30;
        }

        // Returns dummy hand value.
        public int CalculateHandValue()
        {
            return 50;
        }

        // Returns dummy trump suit.
        public int GetTrumpSuit()
        {
            return 2;
        }

        // Returns team score based on team index.
        public int GetTeamScore(int teamIndex)
        {
            return teamIndex == 0 ? team0GameScore : team1GameScore;
        }

        // Evaluates hand quality for the ML Agent.
        public int EvaluateHandQuality()
        {
            // Example: Sum of pip totals for North's hand.
            Player north = Array.Find(players, p => p.Name == "North");
            if (north != null)
            {
                int quality = 0;
                foreach (Domino d in north.Hand)
                    quality += (d.SideA + d.SideB);
                return quality;
            }
            return 0;
        }

        // --------------------------------------------------------------------
        // RoundResult: Used to store the trick counts for each team in a round.
        public class RoundResult
        {
            public int team0Tricks;
            public int team1Tricks;
            public RoundResult() { team0Tricks = 0; team1Tricks = 0; }
        }

        // --------------------------------------------------------------------
        // ResetOverallGameState()
        // Reinitializes additional game state (player positions and dealer) for a new training episode.
        private void ResetOverallGameState()
        {
            // Reposition the players.
            ResetPlayers();
            // Re-determine the initial dealer.
            DetermineInitialDealer();
            Debug.Log("Overall game state has been reset for a new episode.");
        }
    } // End of GameManager class

      // --------------------------------------------------------------------
      // Minimal stub for Domino (represents a domino with two sides).
    public class Domino
    {
        public int SideA;
        public int SideB;
        public Domino(int a, int b)
        {
            SideA = a;
            SideB = b;
        }
        public override string ToString()
        {
            return SideA + "-" + SideB;
        }
    }
} // End of namespace AI42.Core
