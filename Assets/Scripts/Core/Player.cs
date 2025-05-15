using System;
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

    // For AI players, this method could later incorporate ML Agent decisions  
    public int Bid(int currentHighBid)
    {
        if (Type == PlayerType.AI)
        {
            // AI bidding logic – start simple and later integrate ML agents  
            return currentHighBid + 1;
        }
        else
        {
            // For human players, trigger UI input  
            // Use an input field or a selection panel to record bid  
            return 0; // Placeholder  
        }
    }

    // Method to select and play a domino from allowed moves  
    public Domino PlayDomino(List<Domino> validDominoes)
    {
        if (Type == PlayerType.AI)
        {
            // AI decision making  
            return validDominoes[0];
        }
        else
        {
            // For human players, wait for UI selection  
            return null; // Placeholder for UI integration  
        }
    }
}
