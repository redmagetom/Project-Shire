using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Hero", menuName = "Status Effect", order = 1)]
public class StatusEffect : ScriptableObject
{
    public enum EffectType{Frozen,Chilled,Burning,Bleeding,Poisoned}
    public EffectType effectType;
    public bool isBad;
    public int roundAdded;
    public bool fadesAway;
    public int effectLength;
    public int statusValue;



    //notes: frozen --stops all action, cant attack or defend, goes away
    // chilled -- doubles damage done to armor, goes away
    // burning -- does damage to armor and hp, goes away
    // bleeding -- does damage to hp directly = rounds exsiting ... 5 does 5 damage, 4 does 4, goes away
    // poisoned -- does damage to hp directly, does -not- go away without removal
}
