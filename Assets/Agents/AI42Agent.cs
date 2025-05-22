using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace AI42.Core
{
    public class AI42Agent : Agent
    {
        // Reference to GameManager—assign via Inspector.
        public GameManager gameManager;

        // Stores the ML Agent's chosen bid value.
        public int agentBid = 30; // Default initial bid.

        public override void Initialize()
        {
            // Optionally, force an early decision:
            // RequestDecision();
        }

        // Collect observations for the ML model.
        public override void CollectObservations(VectorSensor sensor)
        {
            Debug.Log("AI42Agent is collecting observations.");
            // Expand observation vector to 5 values:
            sensor.AddObservation(gameManager.GetCurrentBid());       // 1. Current bid.
            sensor.AddObservation(gameManager.CalculateHandValue());    // 2. Hand value.
            sensor.AddObservation(gameManager.GetTrumpSuit());          // 3. Dummy trump suit.
            sensor.AddObservation(gameManager.GetTeamScore(0));         // 4. Team 0 score.
            sensor.AddObservation(gameManager.EvaluateHandQuality());   // 5. Hand quality for North.
        }

        // Map actions (a single discrete action) to a bidding decision.
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (actions.DiscreteActions.Length > 0)
            {
                agentBid = actions.DiscreteActions[0];
                Debug.Log("AI42Agent received action bid: " + agentBid);
            }
            // No dummy reward added here; rewards are assigned based on game outcomes.
        }

        // Optional heuristic for testing without training.
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;
            // For testing, you can directly set the bid value.
            discreteActionsOut[0] = 30;
        }

        // Trigger decision requests for testing using Space.
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                RequestDecision();
            }
        }
        // --- NEW: Auto-reset per training episode ---
        public override void OnEpisodeBegin()
        {
            Debug.Log("OnEpisodeBegin: Resetting agent with bid " + agentBid);
            // Optionally, add any additional reset logic here.
        }
    }
}
