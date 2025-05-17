using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AI42Agent : Agent
{
    // Reference to your GameManager, so you can query state.
    public GameManager gameManager;

    public override void Initialize()
    {
        // Initialize the agent (e.g., store references, initialize state variables).
    }

    // Define the observations your agent makes.
    public override void CollectObservations(VectorSensor sensor)
    {
        // For example, observe the current hand, current bid, trump status, etc.
        // sensor.AddObservation(gameManager.GetSomeGameStateInfo());
        // In a later version, build a comprehensive observation vector.
        Debug.Log("AI42Agent is collecting observations.");
        // Example: add current bid (if you create a public method in GameManager that returns the bid)
        sensor.AddObservation(gameManager.GetCurrentBid());
        // Example: add a total hand value (make sure you have a method that calculates that)
        sensor.AddObservation(gameManager.CalculateHandValue());
        //sensor.AddObservation(gameManager.GetTrumpSuit());
        //sensor.AddObservation(gameManager.GetTeamScore());
        // Later, add additional observations as needed:
        // sensor.AddObservation();
    }

    // Map the agent’s actions to game decisions.
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Example: use discrete actions to determine bidding or to play a domino.
        // This is where you’ll define how actions affect the game state.
        // Log the action values (0 might mean pass, 1 could mean bid a certain increment, etc.)
        Debug.Log("AI42Agent received actions: " + string.Join(",", actions.DiscreteActions));
        // Later, use these action values to alter the bidding or play decisions.

        // For now, add a dummy reward to see the reward system in action:
        AddReward(0.1f); // Positive reward for testing

        // Later, you'll adjust reward signals based on game outcomes (e.g., bid success, winning a trick, etc.)
    }

    // Optional: provide heuristic controls (useful for testing).
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Define manual controls for testing (e.g., using keyboard input).
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RequestDecision();
        }
    }

}
