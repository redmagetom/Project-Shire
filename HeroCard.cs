using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HeroCard : MonoBehaviour
{
    public enum Ownership{Player,Enemy}
    public Ownership ownership;
    public Hero hero;
    public MeshRenderer cardFace;
    public TextMeshPro heroName;
    public TextMeshPro heroHPDisplay;
    public TextMeshPro heroArmorDisplay;
    public TextMeshPro flavorTextDisplay;
    public GameObject heroHPBackground;
    public GameObject heroLevelBackgroud;
    public TextMeshPro heroLevelDisplay;
    public int level;
    [Header("In Game Stuff")]
    public int hp;
    public int hpMod;
    public int armor;

    void Update(){
        // is this too expensive?
        heroHPDisplay.text = hp.ToString();
        heroArmorDisplay.text = armor.ToString();
        heroLevelDisplay.text = level.ToString();
    }

    public void InitializeHero(){
        
        if(hero.cardImage){
            cardFace.material.SetTexture("_HeroPortrait", hero.cardImage.texture);
        }

        // note: have to clone everything
        List<Ability> abilities = new List<Ability>();
        for(var i = 0; i < hero.abilities.Count; i++){
            var ab = hero.abilities[i];
            var abClone = Instantiate(ab);
            abClone.barPosition = i;
            abClone.locked = true;
            abilities.Add(abClone);
            if(i == 0){
                abClone.locked = false;
            }
        }

        hero.abilities = abilities;
        level = 1;
        hp = hero.baseHP;
        heroName.text = hero.heroName;
        flavorTextDisplay.text = hero.heroFlavorText.Replace("\r", "");
        heroHPDisplay.text = hp.ToString();
        // manaContribution.text = hero.manaContribution.ToString();
    }

 
}
