using System.Collections.Generic;
using UnityEngine;

public enum PlayerType { Human, AI }

public class Player
{
    public string Name { get; private set; }
    public List<Domino> Hand { get; set; }
    public PlayerType Type { get; private set; }

    public Player(string name, PlayerType type)
    {
        Name = name;
        Type = type;
        Hand = new List<Domino>();
    }

    // AI bidding logic.
    // Compute base bid = clamped(handPips/2, 30, 41).  
    // If base bid isn't higher than currentHighBid but the hand is strong (handPips ≥ 40), 
    // there's a 50% chance to raise by 1; otherwise, pass.
    public int Bid(int currentHighBid)
    {
        if (Type == PlayerType.Human)
        {
            // Simulate human passing for now.
            return currentHighBid;
        }

        int handPips = 0;
        foreach (Domino d in Hand)
            handPips += d.SideA + d.SideB;

        int baseBid = Mathf.Clamp(handPips / 2, 30, 41);
        if (baseBid <= currentHighBid)
        {
            if (handPips >= 40 && Random.value > 0.5f)
            {
                // Raise by 1 over currentHighBid.
                return currentHighBid + 1;
            }
            return currentHighBid; // Pass.
        }
        return baseBid;
    }

    // Play a domino by simply removing and returning the first domino in hand.
    public Domino PlayDomino()
    {
        if (Hand.Count > 0)
        {
            Domino played = Hand[0];
            Hand.RemoveAt(0);
            return played;
        }
        return null;
    }
}
