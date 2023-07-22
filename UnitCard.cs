using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NaughtyAttributes;

public class UnitCard : MonoBehaviour
{
    [Header("Stuff")]
    public GameObject leftIndicator;
    public GameObject rightIndicator;
    [Header("Stats")]
    public int boardPos;
    public bool attackedThisTurn;
    public float potentialValue;
    public int roundPlayed;
    [Header("Unit Base Information")]
    public HeroCard.Ownership ownership;
    public Unit unit;
    public MeshRenderer cardBase;
    public TextMeshPro hpReadout;
    public TextMeshPro armorReadout;
    public TextMeshPro damageReadout;
    [Header("In Game")]

    public int hp;
    public int maxHp;
    public int armor;
    public int maxArmor;
    public int damage;
    public int maxDamage;
    public List<StatusEffect> statusEffects;

    // tracked by unit name or ability name
    public List<string> tempBuffs;

    void Update(){
        hpReadout.text = hp.ToString();
        armorReadout.text = armor.ToString();
        damageReadout.text = damage.ToString();
    }

    public void SetUpUnit(bool calculationOnly = false){
        hp = unit.baseHP;
        maxHp = hp;
        armor = unit.baseArmor;
        maxArmor = armor;
        damage = unit.baseDamage;
        maxDamage = damage;

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
