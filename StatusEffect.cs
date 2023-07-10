using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Hero", menuName = "Status Effect", order = 1)]
public class StatusEffect : ScriptableObject
{
    public enum EffectType{Frozen,Burning,Broken,Bleeding}
    public EffectType effectType;
    public bool isBad;
    public int roundAdded;
    public bool fadesAway;
    public int effectLength;
    public int statusValue;
}
