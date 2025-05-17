using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AI42Agent : Agent
{
    // Reference to GameManager (set via drag-and-drop in the Inspector)
    public GameManager gameManager;

    // This will store the agent's chosen bid value.
    public int agentBid = 30; // Default initial bid

    public override void Initialize()
    {
        // Initialization code if needed.
    }

    // Collect observations from the game state using GameManager.
    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("AI42Agent is collecting observations.");

        // For now, use stub methods from GameManager.
        // As you expand, add more detailed observations (e.g., trump suit, team score, etc.)
        sensor.AddObservation(gameManager.GetCurrentBid());
        sensor.AddObservation(gameManager.CalculateHandValue());
        // Future observations could include:
        // sensor.AddObservation(gameManager.GetTrumpSuit());
        // sensor.AddObservation(gameManager.GetTeamScore(0)); etc.
    }

    // Map the agent's actions to its decision in the game.
    public override void OnActionReceived(ActionBuffers actions)
    {
        // This example assumes a single discrete action:
        // We'll assign that action directly to our bid.
        if (actions.DiscreteActions.Length > 0)
        {
            agentBid = actions.DiscreteActions[0];
            Debug.Log("AI42Agent received action bid: " + agentBid);
        }
        // Give a dummy reward for testing purposes.
        AddReward(0.1f);
    }

    // Optional: define heuristic actions to test without training.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        // For now, set a default bid value (e.g., 30).
        discreteActionsOut[0] = 30;
    }

    // For testing, we'll trigger decision requests with the Space key.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RequestDecision();
        }
    }
}
