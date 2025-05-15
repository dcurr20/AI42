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

    // AI bidding logic: compute the hand's total pips and derive a potential bid.
    // The bid will be between 30 and 41. If the hand isn’t strong enough (total pips below 40)
    // or if the potential bid isn’t more than the current bid, the AI “passes” (returns currentHighBid).
    public int Bid(int currentHighBid)
    {
        if (Type == PlayerType.Human)
        {
            // For now, human input is not implemented; simulate as pass.
            return currentHighBid;
        }
        int handPips = 0;
        foreach (Domino d in Hand)
        {
            handPips += d.SideA + d.SideB;
        }
        // Simple heuristic: divide handPips by 2 and clamp to [30, 41].
        int potentialBid = Mathf.Clamp(handPips / 2, 30, 41);
        if (potentialBid > currentHighBid && handPips >= 40)
        {
            return potentialBid;
        }
        return currentHighBid;
    }

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
