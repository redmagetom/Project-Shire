using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    public Camera mainCam;
    private GameManager gm;
    private BoardManager bm;
    private EnemyManager em;
    private UIManager ui;
    public List<Hero> activeHeroes;

    public Ability readiedAbility;
    public UI_DrawnCard selectedCard;

    void Update(){
        HandlePlayerInteractions();
    }


    void Start(){
        gm = gameObject.GetComponent<GameManager>();
        bm = gameObject.GetComponent<BoardManager>();
        ui = gameObject.GetComponent<UIManager>();
        em = gameObject.GetComponent<EnemyManager>();

    }


    private void HandlePlayerInteractions(){
        if(Input.GetMouseButtonUp(0)){
            RaycastHit clicked;
            if(Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out clicked, 10000f, ~LayerMask.GetMask("Invisible"))){
                
                // Debug.Log(clicked.transform.name);
                if(ui.blackOverlay.gameObject.activeSelf){
                    CheckForOtherInteraction();
                    return;
                }

                var _heroCard = clicked.transform.gameObject.GetComponent<HeroCard>();
                var _unitCard = clicked.transform.gameObject.GetComponent<UnitCard>();

                // view the clicked card
                if(_heroCard && !readiedAbility){
                    if(OverUI()){return;}
                    ui.MoveCardsFromCenterToHand();
                    
                    if(_heroCard.ownership == HeroCard.Ownership.Player){
                        ui.abilityBar.SetActive(true);
                        ui.SetUpAbilityBar(_heroCard.hero);
                    } else {
                        ui.abilityBar.SetActive(false);
                        ui.abilityDescriptionWindow.SetActive(false);
                    }

                    //todo: redo card viewer to be 3d object
                    ui.heroCardView.gameObject.SetActive(true);
                    ui.heroCardViewPortrait.sprite = _heroCard.hero.cardImage;
                    ui.heroCardViewName.text = _heroCard.heroName.text;
                    ui.heroCardViewFlavor.text = _heroCard.flavorTextDisplay.text;
                    ui.heroCardViewHP.text = _heroCard.hp.ToString();
                    ui.heroCardViewLevel.text = _heroCard.level.ToString();
                    // ui.cardViewer.material = ui.heroCardBase;
                    // ui.cardViewer.sprite = _heroCard.hero.cardImage;
                    return;

                // if there's an ability ready 
                } else if(readiedAbility){      
                    // if the ability is a summon
                    if(readiedAbility.abilityType == Ability.AbilityType.Summon){

                        // target is a unit slot
                        if(bm.playerUnitCards.Contains(clicked.transform.gameObject) || bm.enemyUnitCards.Contains(clicked.transform.gameObject)){
                            SummonUnit(readiedAbility, clicked.transform.gameObject);
                            ui.CancelCast();
                            if(selectedCard){
                                gm.playerDrawnCards[selectedCard.transform.GetSiblingIndex()] = null;
                                Destroy(selectedCard.gameObject);
                                selectedCard = null;
                            }
                        }

                    } else {
                        // ability is not a summon
                        if(!_heroCard && !_unitCard){return;} 
                        if(!gm.PlayerUseAbility(readiedAbility, clicked.transform.gameObject)){
                            return;
                        }

                        // if the source was from a card, get rid of that card
                        if(selectedCard){
                            gm.playerDrawnCards[selectedCard.transform.GetSiblingIndex()] = null;
                            Destroy(selectedCard.gameObject);
                            selectedCard = null;
                        }
                    }
           
                }   
            }
            CheckForOtherInteraction();
   
       
        }
    }



    // --------- DEPRECATED ----------
    // public void EnemySummonsUnit(Ability ab, GameObject selectedPosition){
    //     var _unitCard = Instantiate(bm.unitCardPrefab as UnitCard);
        
    //     _unitCard.unit = ab.summonedUnit;

    //     _unitCard.unit.managers = gameObject;
    //     _unitCard.ownership = HeroCard.Ownership.Enemy;
    //     _unitCard.SetUpUnit();

    //     var overPos = selectedPosition.transform.position;
    //     var downPos = selectedPosition.transform.position;
    //     overPos.y += 1;
    //     downPos.y = 0.01f;
    //     _unitCard.transform.position = overPos;
         
    //     LeanTween.move(_unitCard.gameObject, downPos, 0.25f);

    //     var posIndex = selectedPosition.transform.GetSiblingIndex();
    //     gm.enemyUnits[posIndex] = _unitCard;
    //     gm.enemyMana -= ab.manaCost;
    //     _unitCard.name = $"Enemy's {_unitCard.unit.unitName} (Position: {posIndex})";
    //     ui.UpdateManaDisplay();
    //     foreach(CardBonusEffects effect in _unitCard.unit.cardEffects){
    //         effect.managers = gameObject;
    //         effect.attachedCard = _unitCard;
    //         effect.DoExtras();
    //     }
    //     _unitCard.transform.SetParent(bm.activeUnitCardHolder.transform);
    // }


    public void SummonUnit(Ability ab, GameObject selectedPosition){
        var _unitCard = Instantiate(bm.unitCardPrefab as UnitCard);
        
        _unitCard.unit = ab.summonedUnit;

        _unitCard.unit.managers = gameObject;
        _unitCard.ownership = HeroCard.Ownership.Player;
        _unitCard.SetUpUnit();

        var overPos = selectedPosition.transform.position;
        var downPos = selectedPosition.transform.position;
        overPos.y += 1;
        downPos.y = 0.01f;
        _unitCard.transform.position = overPos;
         
        LeanTween.move(_unitCard.gameObject, downPos, 0.25f);

        var posIndex = selectedPosition.transform.GetSiblingIndex();
        gm.playerUnits[posIndex] = _unitCard;
        gm.playerMana -= ab.manaCost;

        _unitCard.name = $"Player's {_unitCard.unit.unitName} (Position: {posIndex})";
       
        ui.UpdateManaDisplay();
        if(_unitCard.unit.cardEffect){
            _unitCard.unit.cardEffect.managers = gameObject;
            _unitCard.unit.cardEffect.attachedCard = _unitCard;
            _unitCard.unit.cardEffect.DoExtras();
        }
        _unitCard.transform.SetParent(bm.activeUnitCardHolder.transform);
    }

   



    private bool OverUI(){
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        // nothing clicked so just dismiss things
        if(results.Count == 0){return false;
        } else {
            return true;
        }
    }

    private void CheckForOtherInteraction(){
        // check for ui click
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        // nothing clicked so just dismiss things
        if(results.Count == 0){
            CloseAbilityAndCardStuff();
            ui.MoveCardsFromCenterToHand();    
        } else {
    
            // some other ui element was clicked
            foreach(var thing in results){
                UI_DrawnCard _drawnCard = thing.gameObject.GetComponent<UI_DrawnCard>();
                if(_drawnCard){
                    // if clicked in corner vs in view mode

                    // card is in center view 
                    if(_drawnCard.transform.parent == ui.centerCardView.transform){

                        // if clicked cast button, allow it to do its thing
                        if(_drawnCard.castButton.gameObject.activeSelf){
                            StartCoroutine(ui.ReturnFromCastView(_drawnCard));
                            return;
                        }

                        // if card not scaled, show cast view, otherwise, return it
                        if(_drawnCard.transform.localScale == new Vector3(1,1,1)){
                            StartCoroutine(ui.ShowCardCastView(_drawnCard));
                        } else {
                            StartCoroutine(ui.ReturnFromCastView(_drawnCard));
                        }
                        
                        return;
                    }

                    // card is in hand
                    ui.ShowAbilityCards();
                    return;
                }
            }
        }
        
    }

    private void CloseAbilityAndCardStuff(){
        ui.heroCardView.gameObject.SetActive(false);
        ui.abilityBar.SetActive(false);
        ui.abilityDescriptionWindow.SetActive(false);
        ui.blackOverlay.gameObject.SetActive(false);
    }
}


