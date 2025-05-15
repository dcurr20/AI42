using UnityEngine;

public class Domino
{
    public int SideA { get; private set; }
    public int SideB { get; private set; }

    public Domino(int sideA, int sideB)
    {
        SideA = sideA;
        SideB = sideB;
    }

    public override string ToString()
    {
        return SideA + "-" + SideB;
    }
}
