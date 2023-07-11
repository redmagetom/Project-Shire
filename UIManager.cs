using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class UIManager : MonoBehaviour
{
    public Canvas canvas;
    private PlayerManager pm;
    public GameManager gm;
    private BoardManager bm;
    [Header("In Game UI")]
    public Transform centerCardView;
    public GameObject inGameBottomBar;
    [Header("Hero Card Viewer")]
    public Image heroCardView;
    public Image heroCardViewPortrait;
    public TextMeshProUGUI heroCardViewName;
    public TextMeshProUGUI heroCardViewFlavor;
    public TextMeshProUGUI heroCardViewHP;
    public TextMeshProUGUI heroCardViewLevel;
    public GameObject abilityBar;
    public GameObject castingBar;
    public Image castingBarImage;
    public TextMeshProUGUI castingBarText;
    public RectTransform blackOverlay;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI playerManaValue;
    public TextMeshProUGUI enemyManaValue;

    [Header("Ability Description Window")]
    public GameObject abilityDescriptionWindow;
    public TextMeshProUGUI adwName;
    public TextMeshProUGUI adwDescription;
    public Image adwImage;
    public Button adwCastButton;

    [Header("Ability Cards")]
    public GameObject playerDrawnCardsHolder;
    public GameObject enemyDrawnCardHolder;

    #region Prefabs
    [Header("Prefabs")]
    public UI_AbilityIcon abilityIconPrefab;
    public UI_DrawnCard drawnCardPrefab;
    public GameObject enemyDrawnCardPrefab;

    #endregion

    void Start(){
        pm = gameObject.GetComponent<PlayerManager>();
        gm = gameObject.GetComponent<GameManager>();
        bm = gameObject.GetComponent<BoardManager>();
        ResizeUIElements();
    }


    public void SetUpAbilityBar(Hero hero){
        foreach(Transform child in abilityBar.transform){
            Destroy(child.gameObject);
        }

        abilityDescriptionWindow.SetActive(false);

        foreach(Ability _ab in hero.abilities){
            var _icon = Instantiate(abilityIconPrefab);
            _icon.ability = _ab;
            _icon.ChangeIcon();
            _icon.transform.SetParent(abilityBar.transform, worldPositionStays: false);
            _icon.GetComponent<Button>().onClick.AddListener(delegate{
                ShowAbilityDescription(_ab);
            });
        }
    }

    public void ShowAbilityDescription(Ability _ab){
        adwCastButton.onClick.RemoveAllListeners();
        adwName.text = _ab.abilityName;
        adwImage.sprite = _ab.abilityImage;
        adwDescription.text = _ab.abilityDescription.Replace("[VALUE]", _ab.baseAmount.ToString());
        if(_ab.abilityType == Ability.AbilityType.Summon){
            adwDescription.text = adwDescription.text.Replace("[UNIT]", _ab.summonedUnit.unitName);
        }
        



        abilityDescriptionWindow.SetActive(true);
        if(gm.gamePhase != GameManager.Phase.PlayerTurn){
            adwCastButton.gameObject.SetActive(false);
        } else {
            adwCastButton.gameObject.SetActive(true);
        }

        if(!_ab.locked){
            adwCastButton.onClick.AddListener(delegate{
                ReadyAbilityForCasting(_ab);
            });
            if(gm.playerMana < _ab.manaCost){
                adwCastButton.interactable = false;
                adwCastButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Not Enough Mana";
            } else {
                adwCastButton.interactable = true;
                adwCastButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Cast";
            }
        } else {
            adwCastButton.interactable = false;
            adwCastButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Level Hero to {_ab.barPosition + 1} to unlock";
        }

    }

    private void ReadyAbilityForCasting(Ability _ab){

        if(gm.gamePhase != GameManager.Phase.PlayerTurn){return;}
        pm.readiedAbility = _ab;
        if(_ab.category == Ability.Category.HeroAbility){
            pm.selectedCard = null;
        }

        CloseHeroSelection();

        if(_ab.targetType == Ability.TargetType.AllEnemies || _ab.targetType == Ability.TargetType.AllAllies){
            StartCoroutine(gm.AbilityGoesOff(_ab, true));
            MoveCardsFromCenterToHand();
            return;
        }

        castingBar.SetActive(true);
        castingBarImage.sprite = _ab.abilityImage;
        castingBarText.text = $"Casting {_ab.abilityName}";
        MoveCardsFromCenterToHand();

        if(_ab.abilityType == Ability.AbilityType.Summon){
            ShowPlayerUnitPlacements();            
        }

    }


    private void ShowPlayerUnitPlacements(){
        foreach(var p in bm.playerUnitCards){
            if(gm.playerUnits[p.transform.GetSiblingIndex()]){continue;}
            p.layer = LayerMask.NameToLayer("Default");
        }
    }

    public void HidePlayerUnitPlacements(){
        foreach(var p in bm.playerUnitCards){
            p.layer = LayerMask.NameToLayer("Invisible");
        }
    }

    public void CloseHeroSelection(){
        abilityDescriptionWindow.SetActive(false);
        heroCardView.gameObject.SetActive(false);
        abilityBar.SetActive(false);
    }

    public void CancelCast(){
        pm.readiedAbility = null;
        castingBar.SetActive(false);
        HidePlayerUnitPlacements();
    }

    public void UpdateManaDisplay(){
        playerManaValue.text = gm.playerMana.ToString();
        enemyManaValue.text = gm.enemyMana.ToString();
    }

    public void ShowPlayerCardDraw(Ability card){
        var _newCard = Instantiate(drawnCardPrefab) as UI_DrawnCard;
        _newCard.ability = card;
        _newCard.SetUpCard();
        _newCard.castButton.onClick.AddListener(delegate{
            ReadyAbilityForCasting(card);
        });
        MovePlayerCardToHand(_newCard);
    }
    private void MovePlayerCardToHand(UI_DrawnCard _newCard){
        _newCard.transform.SetParent(playerDrawnCardsHolder.transform, worldPositionStays: false);
        _newCard.transform.localPosition = new Vector3(_newCard.transform.GetSiblingIndex() * 50, 0, 0);
        _newCard.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -3 * _newCard.transform.GetSiblingIndex()));
        _newCard.transform.localScale = new Vector3(1,1,1);
        _newCard.handPos = _newCard.transform.GetSiblingIndex();
        _newCard.centerViewPosition = Vector3.zero;
        _newCard.castButton.gameObject.SetActive(false);
    }

    public void ShowEnemyCardDraw(Ability card){
        var _newCard = Instantiate(drawnCardPrefab as UI_DrawnCard);
        _newCard.ability = card;
        _newCard.enemyCover.SetActive(true);
        _newCard.SetUpCard();
        StartCoroutine(MoveEnemyCardToHand(_newCard));
    }


    public IEnumerator MoveEnemyCardToHand(UI_DrawnCard _newCard){
        yield return new WaitForSeconds(0.01f);
        if(!_newCard){yield break;}
        _newCard.transform.SetParent(enemyDrawnCardHolder.transform, worldPositionStays: false);
        _newCard.transform.localPosition = new Vector3(_newCard.transform.GetSiblingIndex() * 50, 0, 0);
        _newCard.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 3 * _newCard.transform.GetSiblingIndex()));
        _newCard.transform.localScale = new Vector3(0.4f ,0.4f , 0.4f);
        _newCard.handPos = _newCard.transform.GetSiblingIndex();
        _newCard.centerViewPosition = Vector3.zero;
        _newCard.castButton.gameObject.SetActive(false);
    }

    private IEnumerator MovePlayerCardsToCenter(){
        if(pm.readiedAbility){yield break;}
        CloseHeroSelection();
        centerCardView.GetComponent<GridLayoutGroup>().enabled = true;

        List<Transform> _cardsToMove = new List<Transform>();
        foreach(Transform card in playerDrawnCardsHolder.transform){
            _cardsToMove.Add(card);
        }

        List<Vector3> cardPositions = new List<Vector3>();
        foreach(Transform card in _cardsToMove){
            // hide the card then get the position, then move it
            card.gameObject.SetActive(false);
            card.transform.SetParent(centerCardView, worldPositionStays: false);    
            yield return new WaitForSeconds(0.01f); 
            cardPositions.Add(card.transform.position);
            MovePlayerCardToHand(card.GetComponent<UI_DrawnCard>());
        }

        for(var i = 0; i < cardPositions.Count; i++){
            _cardsToMove[i].localScale = new Vector3(1,1,1);
            _cardsToMove[i].gameObject.SetActive(true);
            LeanTween.move(_cardsToMove[i].gameObject, cardPositions[i], 0.05f);
            LeanTween.rotate(_cardsToMove[i].gameObject, new Vector3(0, 0, 0), 0.05f);
            yield return new WaitForSeconds(0.05f);
            _cardsToMove[i].transform.SetParent(centerCardView, worldPositionStays: false); 
        }

        yield return new WaitForSeconds(0.05f);

        // have to disable glg then change anchors to get proper localposition
        centerCardView.GetComponent<GridLayoutGroup>().enabled = false;
        yield return new WaitForSeconds(0.05f);
        for(var i = 0; i < cardPositions.Count; i++){
            _cardsToMove[i].GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            _cardsToMove[i].GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            _cardsToMove[i].gameObject.GetComponent<UI_DrawnCard>().centerViewPosition = _cardsToMove[i].localPosition;
        }
    }


    public void MoveCardsFromCenterToHand(){
        if(playerDrawnCardsHolder.transform.childCount != 0){return;}
        CloseHeroSelection();
        List<Transform> _cardsToMove = new List<Transform>();
        foreach(Transform card in centerCardView.transform){
            _cardsToMove.Add(card);
        }
        foreach(Transform card in _cardsToMove){
            MovePlayerCardToHand(card.GetComponent<UI_DrawnCard>());
        }

    }

    public void ShowAbilityCards(){
        StartCoroutine(MovePlayerCardsToCenter());
    }

    public IEnumerator ShowCardCastView(UI_DrawnCard card){
        // return any other card being viewed to its other state in center
        foreach(Transform _card in centerCardView.transform){
            if(_card.localScale != new Vector3(1,1,1)){
                _card.GetComponent<UI_DrawnCard>().castButton.gameObject.SetActive(false);
                LeanTween.scale(_card.gameObject, new Vector3(1,1,1), 0.25f);
                LeanTween.moveLocal(_card.gameObject, _card.GetComponent<UI_DrawnCard>().centerViewPosition, 0.25f);
            }
        }
        
        card.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        card.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        yield return new WaitForSeconds(0.01f);
        card.transform.SetAsLastSibling();
        blackOverlay.gameObject.SetActive(true);
        LeanTween.scale(card.gameObject, new Vector3(2.5f, 2.5f, 2.5f), 0.25f);
        LeanTween.moveLocal(card.gameObject, Vector3.zero, 0.25f);
        pm.selectedCard = card;

        if(gm.gamePhase != GameManager.Phase.PlayerTurn){yield break;}

        card.castButton.gameObject.SetActive(true);
        if(gm.playerMana < card.ability.manaCost){
            card.castButton.interactable = false;
            card.castButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Not Enough Mana";
        } else {
            card.castButton.interactable = true;
            card.castButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Cast";
        }
    }

    public IEnumerator ReturnFromCastView(UI_DrawnCard card){
        card.transform.SetSiblingIndex(card.handPos);
        card.castButton.gameObject.SetActive(false);
        LeanTween.moveLocal(card.gameObject, card.centerViewPosition, 0.25f);
        LeanTween.scale(card.gameObject, new Vector3(1,1,1), 0.25f);
        yield return new WaitForSeconds(0.25f);
        
    }

    private void ResizeUIElements(){
        var _centerCardView = centerCardView.GetComponent<GridLayoutGroup>();

        // todo: update later for dynamic resolution
        _centerCardView.cellSize = new Vector2(225, 375);
    }

}
