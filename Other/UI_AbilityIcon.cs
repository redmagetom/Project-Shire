using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_AbilityIcon : MonoBehaviour
{
    public Ability ability;
    public Image abilityIcon;
    public TextMeshProUGUI manaCost;
    public Image lockOverlay;

    public void ChangeIcon(){
        abilityIcon.sprite = ability.abilityImage;
        manaCost.text = ability.manaCost.ToString();
        if(!ability.locked){
            lockOverlay.gameObject.SetActive(false);
        }
    }

  
}
