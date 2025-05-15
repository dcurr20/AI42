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

    // Simple bidding logic: if AI, bid currentHighBid + 1; if Human, simulate a pass.
    public int Bid(int currentHighBid)
    {
        if (Type == PlayerType.AI)
        {
            return currentHighBid + 1;
        }
        else
        {
            return currentHighBid;
        }
    }

    // For simulation, play the first domino in hand.
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
