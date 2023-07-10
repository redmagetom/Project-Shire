using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UnitCard : MonoBehaviour
{
    [Header("Stats")]
    public int boardPos;
    public bool attackedThisTurn;
    public float potentialValue;
    [Header("Unit Base Information")]
    public HeroCard.Ownership ownership;
    public Unit unit;
    public MeshRenderer cardBase;
    public TextMeshPro hpReadout;
    public TextMeshPro armorReadout;
    public TextMeshPro damageReadout;

    [Header("In Game")]
    public int hp;
    public int hpMod;
    public int armor;
    public int damage;
    public List<StatusEffect> statusEffects;

    void Update(){
        hpReadout.text = hp.ToString();
        armorReadout.text = armor.ToString();
        damageReadout.text = damage.ToString();
    }

    public void SetUpUnit(bool calculationOnly = false){
        hp = unit.baseHP;
        armor = unit.baseArmor;
        damage = unit.baseDamage;

        if(calculationOnly){return;}

        // visual stuff for real cards 
        hpReadout.text = hp.ToString();
        armorReadout.text = armor.ToString();
        damageReadout.text = damage.ToString();
        cardBase.material.SetTexture("_UnitPortrait", unit.cardImage.texture);

        if(ownership == HeroCard.Ownership.Enemy){
            gameObject.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        }
    }

    public virtual void ProcessExtras(){

    }
}
