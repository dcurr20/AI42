using System;

public class Domino
{
    public int SideA { get; private set; }
    public int SideB { get; private set; }

    public Domino(int sideA, int sideB)
    {
        SideA = sideA;
        SideB = sideB;
    }

    // Useful for UI display  
    public override string ToString()
    {
        return $"{SideA}-{SideB}";
    }

    public bool Matches(Domino other)
    {
        return SideA == other.SideA || SideA == other.SideB ||
               SideB == other.SideA || SideB == other.SideB;
    }
}
