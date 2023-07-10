using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopCardVisual : MonoBehaviour
{
    public Ability ability;
    public MeshRenderer cardFace;
    public GameObject cardBack;


    public void UpdateCardVisual(Material _cardBack){
        cardFace.material.SetTexture("_MainTex", ability.abilityImage.texture);
        cardBack.GetComponent<Renderer>().material = _cardBack;
    }
}
