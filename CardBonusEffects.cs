using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardBonusEffects : MonoBehaviour
{
    public UnitCard attachedCard;
    public GameObject managers;
    public float effectValue = 1f;
    [TextArea]
    public string effectDescription;
    public virtual void DoExtras(GameObject target = null){
       
    }
}
