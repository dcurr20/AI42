using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AI42Agent : Agent
{
    // Reference to GameManager—assign via Inspector.
    public GameManager gameManager;

    // Stores the ML Agent's chosen bid value.
    public int agentBid = 30; // Default initial bid

    public override void Initialize()
    {
        // Initialize agent parameters if needed. 
    }

    // Collect observations for the ML model.
    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("AI42Agent is collecting observations.");
        sensor.AddObservation(gameManager.GetCurrentBid());
        sensor.AddObservation(gameManager.CalculateHandValue());
        sensor.AddObservation(gameManager.GetTrumpSuit());   // new stub method
        sensor.AddObservation(gameManager.GetTeamScore(0));   // new stub method for team 0
    }

    // Map the agent's actions (discrete output) to a bidding decision.
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Expecting a single discrete action.
        if (actions.DiscreteActions.Length > 0)
        {
            agentBid = actions.DiscreteActions[0];
            Debug.Log("AI42Agent received action bid: " + agentBid);
        }
        // (No dummy reward is added here now; reward shaping is handled in the bidding phase.)
    }

    // Optional heuristic for testing without training.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 32; // Default bid value for testing.
    }

    // For testing, trigger decision requests using Space.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RequestDecision();
        }
    }
}
