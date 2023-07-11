using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_DrawnCard : MonoBehaviour
{
    public Ability ability;
    public Image abilityImage;
    public TextMeshProUGUI abilityName;
    public TextMeshProUGUI abilityDescription;
    public TextMeshProUGUI flavorText;
    public TextMeshProUGUI manaCost;
    public Button castButton;
    public GameObject enemyCover;

    [Header("Stored Values")]
    public Vector3 centerViewPosition;
  
    public int handPos;
    public void SetUpCard(){
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
    }
}
