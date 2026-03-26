using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pile : MonoBehaviour
{
    public List<Card> cards = new();
    public PileType pileType;
    public GameObject displayCard;
    public Collider col;
}
