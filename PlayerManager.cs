using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

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


    public bool movingUnit;
    public GameObject unitBeingMoved;
    public int potentialMovePos;
    void Update(){
        StartCoroutine(HandlePlayerInteractions());
    }


    void Start(){
        gm = gameObject.GetComponent<GameManager>();
        bm = gameObject.GetComponent<BoardManager>();
        ui = gameObject.GetComponent<UIManager>();
        em = gameObject.GetComponent<EnemyManager>();

    }

    private IEnumerator HandlePlayerInteractions(){
        #region Moving Units
        if(gm.gamePhase == GameManager.Phase.Strategy){
            
            
            // release of moving unit
            if(unitBeingMoved && Input.GetMouseButtonUp(0)){
                unitBeingMoved.layer = LayerMask.NameToLayer("Default");
                unitBeingMoved.GetComponent<UnitCard>().boardPos = potentialMovePos;

                foreach(var u in gm.playerUnits){
                    if(u.boardPos >= potentialMovePos){
                        u.boardPos += 1;
                    }
                }

                gm.playerUnits.Add(unitBeingMoved.GetComponent<UnitCard>());
                gm.playerUnits = gm.playerUnits.OrderBy(x => x.boardPos).ToList();

                foreach(var u in gm.playerUnits){
                    u.leftIndicator.SetActive(false);
                    u.rightIndicator.SetActive(false);
                }

                yield return StartCoroutine(gm.SpaceOutUnits(gm.playerUnits));
                potentialMovePos = 0;
                unitBeingMoved = null;

                yield break;
            }

            if(Input.GetMouseButton(0)){
                LayerMask mask = ~(1 << LayerMask.NameToLayer("Invisible") | 1 <<LayerMask.NameToLayer("Ignore Raycast"));
                RaycastHit clicked;
                if(Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out clicked, 10000f, mask)){

                    // if moving unit around
                    if(unitBeingMoved){
                        var prevPos = potentialMovePos;
                        var _pointerPos = clicked.point;
                        _pointerPos.y += 0.5f;
                        unitBeingMoved.transform.position = _pointerPos;
                        var _uXPos = unitBeingMoved.transform.position.x;

                        var _cardToLeft = gm.playerUnits.FindAll(x => x.transform.position.x < _uXPos).OrderByDescending(x => x.boardPos).ToList();
                        if(_cardToLeft.Count > 0){
                            potentialMovePos = _cardToLeft[0].boardPos + 1;
                        } else {
                            potentialMovePos = 0;
                        }

                        if(!unitBeingMoved){yield break;}

                        foreach(var _uc in gm.playerUnits.ToList()){
                            

                            if(_uc.boardPos == potentialMovePos - 1){
                                _uc.leftIndicator.SetActive(false);
                                _uc.rightIndicator.SetActive(true);
                            } else if(_uc.boardPos == potentialMovePos){
                                _uc.leftIndicator.SetActive(true);
                                _uc.rightIndicator.SetActive(false);
                            } else {
                                _uc.leftIndicator.SetActive(false);
                                _uc.rightIndicator.SetActive(false);
                            }
                            
                            //todo: maybe make cards spread a bit but fien for now
   
                            yield return new WaitForSeconds(0.1f);
                        }

                        yield break;
                    }

                    // set up moving unit
                    var _unitCard = clicked.transform.gameObject.GetComponent<UnitCard>();
                    if(_unitCard && _unitCard.ownership == HeroCard.Ownership.Player){

                        // move the rest of the units out of the way and remove it from the unit list
                        unitBeingMoved = _unitCard.gameObject;
                        potentialMovePos = _unitCard.boardPos;
                        gm.playerUnits.Remove(_unitCard);
                        foreach(var u in gm.playerUnits){

                            if(u.boardPos > 0 && u.boardPos >= potentialMovePos){
                                u.boardPos -= 1;
                            }

                        }
                        unitBeingMoved.layer = LayerMask.NameToLayer("Ignore Raycast");
                        yield return new WaitForSeconds(0.1f);
                        yield return StartCoroutine(gm.SpaceOutUnits(gm.playerUnits));
                        
                        
                        
                    }
                }
            }
        }

        #endregion


        if(Input.GetMouseButtonUp(0)){
            RaycastHit clicked;
            if(Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out clicked, 10000f, ~LayerMask.GetMask("Invisible"))){
                
                // Debug.Log(clicked.transform.name);
                if(ui.blackOverlay.gameObject.activeSelf){
                    CheckForOtherInteraction();
                    yield break;
                }

                var _heroCard = clicked.transform.gameObject.GetComponent<HeroCard>();
                var _unitCard = clicked.transform.gameObject.GetComponent<UnitCard>();

                // view the clicked card
                if(_heroCard && !readiedAbility){
                    if(OverUI()){yield break;}
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
                    yield break;

                // if there's an ability ready 
                } else if(readiedAbility){      
                    // if the ability is a summon
                    if(readiedAbility.abilityType == Ability.AbilityType.Summon){
                        
                        // target is a unit slot
                        // if(bm.playerUnitCards.Contains(clicked.transform.gameObject) || bm.enemyUnitCards.Contains(clicked.transform.gameObject)){
                            StartCoroutine(gm.AbilityGoesOff(readiedAbility, true));
                            ui.CancelCast();
                            if(selectedCard){
                                Debug.Log("Card - Summon");
                                int cardIdx = selectedCard.transform.GetSiblingIndex();
                                gm.playerDrawnCards[cardIdx] = null;
                                StartCoroutine(gm.ShowCardPlayed(ui.playerDrawnCardsHolder.transform.GetChild(cardIdx).GetComponent<UI_DrawnCard>()));
                                selectedCard = null;
                                gm.playerCardsPlayed += 1;
                            } else {
                                gm.playerHeroAbilitiesCast += 1;
                            }
                        // }

                    } else {
                        // ability is not a summon
                        if(!_heroCard && !_unitCard){yield break;} 

                        StartCoroutine(gm.AbilityGoesOff(readiedAbility, true, clicked.transform.gameObject));
                        ui.CancelCast();
                        

                        // if the source was from a card, get rid of that card
                        if(selectedCard){
                            Debug.Log("Card - Not summon");
                            int cardIdx = selectedCard.transform.GetSiblingIndex();
                            gm.playerDrawnCards[cardIdx] = null;
                            StartCoroutine(gm.ShowCardPlayed(ui.playerDrawnCardsHolder.transform.GetChild(cardIdx).GetComponent<UI_DrawnCard>()));
                            selectedCard = null;
                            gm.playerCardsPlayed += 1;
                        } else {
                            gm.playerHeroAbilitiesCast += 1;
                        }
                    }
           
                }   
            }
            CheckForOtherInteraction();
   
       
        }
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
                    if(_drawnCard.enemyCover.activeSelf){return;}
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


