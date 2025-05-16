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

    // AI bidding logic: determines bid strength and ensures the bid is higher than the current bid.
    public int Bid(int currentHighBid)
    {
        if (Type == PlayerType.Human)
        {
            // Human input not implemented yet; simulate pass.
            return currentHighBid;
        }

        int handPips = 0;
        foreach (Domino d in Hand)
        {
            handPips += d.SideA + d.SideB;
        }

        // Simple heuristic: divide handPips by 2 and clamp to [30, 41].
        int potentialBid = Mathf.Clamp(handPips / 2, 30, 41);

        // If the bid is NOT higher than the current bid, the AI must pass.
        if (potentialBid <= currentHighBid || handPips < 40)
        {
            return currentHighBid; // This ensures no duplicate bids.
        }

        return potentialBid;
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
