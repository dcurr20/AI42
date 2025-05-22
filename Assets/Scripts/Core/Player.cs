using System.Collections.Generic;
using UnityEngine;

namespace AI42.Core
{
    public enum PlayerType { Human, AI }

    public class Player : MonoBehaviour
    {
        public string Name;
        public PlayerType Type;
        public List<Domino> Hand;

        void Awake()
        {
            Hand = new List<Domino>();
        }

        public int Bid(int currentHighBid)
        {
            // Bidding logic…
            if (Type == PlayerType.Human)
            {
                return currentHighBid;
            }
            int handPips = 0;
            foreach (Domino d in Hand)
                handPips += d.SideA + d.SideB;
            int baseBid = Mathf.Clamp(handPips / 2, 30, 41);
            if (baseBid <= currentHighBid)
            {
                if (handPips >= 40 && Random.value > 0.5f)
                    return currentHighBid + 1;
                return currentHighBid;
            }
            return baseBid;
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
}
