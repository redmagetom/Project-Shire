using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_DrawnCard : MonoBehaviour
{
    public Sprite neutralCardFace;
    public Image mainCardFace;
    public Ability ability;
    public Image abilityImage;
    public TextMeshProUGUI abilityName;
    public TextMeshProUGUI abilityDescription;
    public TextMeshProUGUI flavorText;
    public TextMeshProUGUI manaCost;
    public Button castButton;
    public GameObject enemyCover;
    public Material cardBack;
    [Header("Rarity Stuff")]
    public Image rarityGem;
    public Sprite uncommonGem;
    public Sprite rareGem;
    public Sprite epicGem;
    public GameObject glow;
    public Color uncommonGlow;
    public Color rareGlow;
    public Color epicGlow;
    
    [Header("Stored Values")]
    public Vector3 centerViewPosition;
    
    public int handPos;
    public void SetUpCard(){
        mainCardFace = gameObject.GetComponent<Image>();

        if(ability.deckOwnership){
            mainCardFace.sprite = ability.deckOwnership.deckCardFace;
        } else {
            mainCardFace.sprite = neutralCardFace;
        }

        if(enemyCover.activeSelf){
            var backMat = cardBack.GetTexture("_BaseMap");
            enemyCover.GetComponent<Image>().sprite = Sprite.Create((Texture2D)backMat, new Rect(0, 0, backMat.width, backMat.height), Vector2.one * 0.5f);
            glow.gameObject.SetActive(false);
        }

        abilityImage.sprite = ability.abilityImage;
        abilityName.text = ability.abilityName;

        if(ability.abilityType == Ability.AbilityType.Summon){
            string _description = ability.abilityDescription;

            if(ability.summonedUnit.cardEffect){
                _description += "\n\n" + ability.summonedUnit.cardEffect.effectDescription;
            }
            abilityDescription.text = _description;
            abilityDescription.text = abilityDescription.text.Replace("[UNIT]", ability.summonedUnit.unitName);
            flavorText.text = ability.summonedUnit.flavor;
        } else {
            abilityDescription.text = ability.abilityDescription;
            flavorText.text = ability.flavorText;
        }

        abilityDescription.text = abilityDescription.text.Replace("[VALUE]", (ability.baseAmount + ability.modifier).ToString());
        
        manaCost.text = ability.manaCost.ToString();
        castButton.gameObject.SetActive(false);

        if(ability.rarity == Ability.Rarity.Uncommon){
            glow.GetComponent<UIOutline>().color = uncommonGlow;
            rarityGem.sprite = uncommonGem;
        } else if (ability.rarity == Ability.Rarity.Rare) {
            glow.GetComponent<UIOutline>().color = rareGlow;
            rarityGem.sprite = rareGem;
        } else if (ability.rarity == Ability.Rarity.Epic){
             glow.GetComponent<UIOutline>().color = epicGlow;
             rarityGem.sprite = epicGem;
        } else {
            glow.gameObject.SetActive(false);
        }
    }
}
