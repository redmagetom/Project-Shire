using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopCardVisual : MonoBehaviour
{
    public Ability ability;
    public MeshRenderer cardFace;
    public GameObject cardBack;


    public void UpdateCardVisual(Material _cardBack){
        if(ability){
            cardFace.material.SetTexture("_MainTex", ability.abilityImage.texture);
        } else {
            cardFace.material = _cardBack;
        }
        cardBack.GetComponent<Renderer>().material = _cardBack;
    }
}
