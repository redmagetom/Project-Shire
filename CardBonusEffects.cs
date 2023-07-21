using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardBonusEffects : MonoBehaviour
{
    public UnitCard attachedCard;
    public GameObject managers;
    public float effectValue = 1f;
    public bool preventDeath = false;
    [TextArea]
    public string effectDescription;
    public virtual void DoTargetExtras(GameObject target = null){
       
    }

    public virtual void DoSummonExtras(GameObject target = null){

    }

    public virtual void DoTurnChangeExtras(GameObject target = null){

    }

    public virtual void DoDeathExtras(GameObject target = null){

    }

    public virtual void BoardCheckExtras(){

    }
}
