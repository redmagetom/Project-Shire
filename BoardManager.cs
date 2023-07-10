using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BoardManager : MonoBehaviour
{
    private GameManager gm;
    private PlayerManager pm;
    private UIManager ui;
    public HeroCard heroCardPrefab;
    public UnitCard unitCardPrefab;
    public TopCardVisual topCardVisual;
    [Header("Holders")]
    public GameObject activeUnitCardHolder;
    public GameObject activeHeroCardHolder;
    [Header("Player Board Objects")]
    public GameObject playerDeckVisual;
    public List<GameObject> playerHeroCards;
    public List<GameObject> playerUnitCards;
    [Header("Enemy Board Objects")]
    public GameObject enemyDeckVisual;
    public List<GameObject> enemyHeroCards;
    public List<GameObject> enemyUnitCards;


    public Vector3 playerDrawToPos;
    void Start(){
        gm = gameObject.GetComponent<GameManager>();
        pm = gameObject.GetComponent<PlayerManager>();
        ui = gameObject.GetComponent<UIManager>();

        playerDrawToPos = new Vector3(3.75f, 1.7f, -3.5f);
    }

}


