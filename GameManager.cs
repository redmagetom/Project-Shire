using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public bool actionInProgress;
    public enum Phase{PlayerTurn,EnemyTurn,Strategy,UnitTurn,EndPhase}
    public Phase gamePhase;
    public int round;
    private Phase firstPhase;

    public bool playerStrategyDone;
    public bool enemyStrategyDone;
    [Header("Player")]

    public int playerLevelUpBase = 2;
    public int playerDrawNumber;
    public Material playerCardBack;
    public List<Hero> selectedPlayerHeroes;
    public List<HeroCard> playerHeroCards;
    public List<Ability> playerDeck;
    public List<Ability> playerDrawnCards;
    public List<UnitCard> playerUnits = new List<UnitCard>(5);
    private int playerBaseMana;
    public int playerMana;
    public int playerCardsPlayed;
    public int playerHeroAbilitiesCast;
    public int playerTotalCastCount;
    [Header("Enemy")]

    public int enemyLevelUpBase = 2;
    public int enemyDrawNumber;
    public Material enemyCardBack;
    public List<Hero> selectedEnemyHeroes;
    public List<HeroCard> enemyHeroCards;
    public List<Ability> enemyDeck;
    public List<Ability> enemyDrawnCards;
    public List<UnitCard> enemyUnits = new List<UnitCard>(5);
    private int enemyBaseMana;
    public int enemyMana;
    public int enemyCardsPlayed;
    public int enemyHeroAbilitiesCast;
    public int enemyTotalCastCount;
    // ----- Private vars ------
    private BoardManager bm;
    private PlayerManager pm;
    private UIManager ui;
    private EnemyManager em;

    void Start(){
        pm = gameObject.GetComponent<PlayerManager>();
        bm = gameObject.GetComponent<BoardManager>();
        ui = gameObject.GetComponent<UIManager>();
        em = gameObject.GetComponent<EnemyManager>();
        StartCoroutine(SetUpGame());
    }

    private IEnumerator SetUpGame(){
        while(!ui.gm){
            yield return new WaitForEndOfFrame();
        }
        round = 1;
        playerDrawNumber = 1;
        enemyDrawNumber = 1;

        ChooseStarter();
        SetUpHeroes();
        SetUpDecks();
        //note: todo:  first player to go gets 1 less card
        StartCoroutine(DrawPlayerCards(4));
        StartCoroutine(DrawEnemyCards(4));
    }


    private void ChooseStarter(){
        // todo: make random later
        gamePhase = Phase.PlayerTurn;
        // gamePhase = Phase.Strategy;
        firstPhase = gamePhase;
        ui.phaseText.text = gamePhase.ToString();
    }


    public void SetUpHeroes(){
        var playerHeroes = selectedPlayerHeroes;
        for(var i = 0; i < playerHeroes.Count; i++){
            var _heroCard = Instantiate(bm.heroCardPrefab) as HeroCard;
            _heroCard.ownership = HeroCard.Ownership.Player;
            Hero clone = new Hero();
            clone = Instantiate(playerHeroes[i]); 
            _heroCard.hero = clone;
            
            _heroCard.InitializeHero();
            _heroCard.transform.SetParent(bm.playerHeroCards[i].transform);
            _heroCard.transform.localPosition = Vector3.zero;  
            _heroCard.transform.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));
            playerBaseMana += _heroCard.hero.manaContribution;
            playerHeroCards.Add(_heroCard);
            _heroCard.transform.SetParent(bm.activeHeroCardHolder.transform);
            _heroCard.name = $"Player's {_heroCard.hero.heroName}";
        }

        var enemyHeroes = selectedEnemyHeroes;
        for(var i = 0; i < enemyHeroes.Count; i++){
            var _heroCard = Instantiate(bm.heroCardPrefab) as HeroCard;
            _heroCard.ownership = HeroCard.Ownership.Enemy;

            // todo: change to enemy heroes
            Hero clone = new Hero();
            clone = Instantiate(playerHeroes[i]); 
            _heroCard.hero = clone;

            _heroCard.InitializeHero();
            _heroCard.transform.SetParent(bm.enemyHeroCards[i].transform);
            _heroCard.transform.localPosition = Vector3.zero;
            _heroCard.transform.localRotation = Quaternion.Euler(Vector3.zero);
            
            // ----------- todo: figure out way to better display hero stats like hp and stuff ------------

            var _heroUIPos = _heroCard.heroHPBackground.transform.localPosition;
            _heroUIPos.z = _heroUIPos.z * -1;
            _heroCard.heroHPBackground.transform.localPosition = _heroUIPos;

            var _heroUIScale = _heroCard.heroHPBackground.transform.localScale;
            Vector3 newScale = new Vector3(_heroUIScale.x * 1.5f, _heroUIScale.y * 1.5f, _heroUIScale.z * 1.5f);
            _heroCard.heroHPBackground.transform.localScale = newScale;

            var _heroLevelIconPos = _heroCard.heroLevelBackgroud.transform.localPosition;
            _heroLevelIconPos.z = _heroLevelIconPos.z * -1;
            _heroCard.heroLevelBackgroud.transform.localPosition = _heroLevelIconPos;
            _heroCard.heroLevelBackgroud.transform.localScale = newScale;

            enemyBaseMana += _heroCard.hero.manaContribution;
            enemyHeroCards.Add(_heroCard);
            _heroCard.transform.SetParent(bm.activeHeroCardHolder.transform);
            _heroCard.name = $"Enemy's {_heroCard.hero.heroName}";
        }


        playerMana = playerBaseMana;
        enemyMana = enemyBaseMana;

        ui.UpdateManaDisplay();
    }

    public IEnumerator DrawPlayerCards(int amount){        
        for(var i = 0; i < amount; i++){
            playerDrawnCards.Add(playerDeck[0]);
            var cardDrawVisual = Instantiate(bm.topCardVisual as TopCardVisual);
            cardDrawVisual.ability = playerDeck[0];
            cardDrawVisual.UpdateCardVisual(playerCardBack);
            cardDrawVisual.transform.SetParent(bm.playerDeckVisual.transform.parent);
            cardDrawVisual.transform.position = bm.playerDeckVisual.transform.position;
            cardDrawVisual.transform.localRotation = Quaternion.Euler(new Vector3(-90, 0, 0));

            LeanTween.moveLocal(cardDrawVisual.gameObject, bm.playerDrawToPos, 0.25f);
            LeanTween.rotateLocal(cardDrawVisual.gameObject, new Vector3(60, 0, 0), 0.25f);
            yield return new WaitForSeconds(0.25f);
            Destroy(cardDrawVisual.gameObject);
            ui.ShowPlayerCardDraw(playerDeck[0]);
            playerDeck.RemoveAt(0);
            yield return new WaitForSeconds(0.1f);
        }
    }


    public IEnumerator DrawEnemyCards(int amount){        
        for(var i = 0; i < amount; i++){
            enemyDrawnCards.Add(enemyDeck[0]);
            var cardDrawVisual = Instantiate(bm.topCardVisual as TopCardVisual);
            cardDrawVisual.ability = enemyDeck[0];
            cardDrawVisual.UpdateCardVisual(enemyCardBack);

            cardDrawVisual.transform.SetParent(bm.enemyDeckVisual.transform.parent, worldPositionStays: false);
            cardDrawVisual.transform.position = bm.enemyDeckVisual.transform.position;
            cardDrawVisual.transform.localRotation = Quaternion.Euler(new Vector3(-90, 0, 0));

            LeanTween.moveLocal(cardDrawVisual.gameObject, bm.enemyDrawToPos, 0.25f);
            LeanTween.rotateLocal(cardDrawVisual.gameObject, new Vector3(-60, 0, 0), 0.25f);
            yield return new WaitForSeconds(0.25f);
            Destroy(cardDrawVisual.gameObject);
            ui.ShowEnemyCardDraw(enemyDeck[0]);
            enemyDeck.RemoveAt(0);
        }
        yield return null;
    }

    public void SetUpDecks(){
        playerDeck.Clear();
        enemyDeck.Clear();

        foreach(Hero hero in selectedPlayerHeroes){
            foreach(Ability _card in hero.deck){
                Ability cardClone = new Ability();
                cardClone = _card;
                cardClone.deckOwnership = hero;
                playerDeck.Add(cardClone);
            }
        }

        foreach(Hero hero in selectedEnemyHeroes){
            foreach(Ability _card in hero.deck){
                Ability cardClone = new Ability();
                cardClone = _card;
                enemyDeck.Add(cardClone);
            }
        }

        playerDeck.Shuffle();
        enemyDeck.Shuffle();
    }


    public IEnumerator ShowCardPlayed(UI_DrawnCard card){
        actionInProgress = true;
        card.enemyCover.SetActive(false);
        card.transform.SetParent(ui.canvas.transform);
        LeanTween.moveLocal(card.gameObject, new Vector3(-800, 0, 0), 0.5f);
        LeanTween.rotateLocal(card.gameObject, Vector3.zero, 0.5f);
        LeanTween.scale(card.gameObject, new Vector3(2,2,2), 0.5f);
        yield return new WaitForSeconds(2);
        Destroy(card.gameObject);
        // yield return new WaitForSeconds(0.25f);
        actionInProgress = false;
    }

    public IEnumerator AbilityGoesOff(Ability ab, bool playerCast, GameObject target = null, UI_DrawnCard card = null){

        if(playerCast){
            playerMana -= ab.manaCost;
            playerTotalCastCount += 1;
        } else{
            enemyMana -= ab.manaCost;
            enemyTotalCastCount += 1;
        }

        if(card){
            yield return StartCoroutine(ShowCardPlayed(card));
        }
        // todo: add in multi target stuff for all other ability types, not just damage
       
        // unit summoned
        if(ab.abilityType == Ability.AbilityType.Summon){
            yield return StartCoroutine(UnitSummoned(ab, playerCast));
        }

        // damage dealt
        if(ab.abilityType == Ability.AbilityType.Damage){

            // if its a board target
            if(ab.targetType == Ability.TargetType.AllEnemies){

                // cast from player
                if(playerCast){
                    foreach(UnitCard unit in enemyUnits.FindAll(x => x != null)){
                        DamageDealt(ab.baseAmount + ab.modifier, unit.gameObject);
                    }
                // cast from enemy
                } else {
                    foreach(UnitCard unit in playerUnits.FindAll(x => x !=null)){
                        DamageDealt(ab.baseAmount + ab.modifier, unit.gameObject);
                    }
                }

            // ability is single target damage type
            } else if(ab.targetType == Ability.TargetType.Enemy) {
                DamageDealt(ab.baseAmount + ab.modifier, target);
            }
            
        }

        // buffing
        if(ab.abilityType == Ability.AbilityType.Alteration){
            BuffUsed(ab, target);
        }

        // healing
        if(ab.abilityType == Ability.AbilityType.Heal){
            HealUsed(ab, target);
        }

        StatusAndBonusEffects(ab, target);


        if(playerCast){
            Debug.Log($"---- Player used {ab} --------");
        } else {
            Debug.Log($"---- Enemy used {ab} --------");
        }

        ui.UpdateManaDisplay();
        yield return null;
    }

 
    private IEnumerator UnitSummoned(Ability ab, bool playerCast){
           
        var _unitCard = Instantiate(bm.unitCardPrefab as UnitCard);
        var _unitCopy = Instantiate(ab.summonedUnit);
        _unitCard.unit = _unitCopy;
        _unitCard.unit.managers = gameObject;

      _unitCard.transform.SetParent(bm.activeUnitCardHolder.transform);

        if(playerCast){
            _unitCard.ownership = HeroCard.Ownership.Player;
            _unitCard.SetUpUnit();

            var overPos = bm.playerUnitsCenter.transform.position;
            var downPos = overPos;

            // var overPos = selectedPosition.transform.position;

            // note: base x spacing is 1f
            overPos.x += (playerUnits.Count * 1f);

            
            overPos.y += 1;
            downPos.y = 0.01f;
            _unitCard.transform.position = overPos;    
            LeanTween.move(_unitCard.gameObject, downPos, 0.25f);
            yield return new WaitForSeconds(0.25f);
           
            // var posIndex = selectedPosition.transform.GetSiblingIndex();
            // playerUnits[posIndex] = _unitCard;
            playerUnits.Add(_unitCard);
            _unitCard.boardPos = playerUnits.Count-1;
            _unitCard.name = $"Player's {_unitCard.unit.unitName} (Position: {_unitCard.boardPos})";
           
            
            yield return new WaitForEndOfFrame();
            // if(playerUnits.Count > 1){
                yield return StartCoroutine(SpaceOutUnits(playerUnits));
            // }
 

        } else {
            var overPos = bm.enemyUnitsCenter.transform.position;
            var downPos = overPos;

            // var overPos = selectedPosition.transform.position;

            // note: base x spacing is 1f
            overPos.x += (enemyUnits.Count * 1f);

            
            overPos.y += 1;
            downPos.y = 0.01f;
            _unitCard.transform.position = overPos;   
            _unitCard.ownership = HeroCard.Ownership.Enemy;
            _unitCard.SetUpUnit();
            enemyUnits.Add(_unitCard);
            _unitCard.boardPos = enemyUnits.Count-1;
            _unitCard.name = $"Enemy's {_unitCard.unit.unitName} (Position: {_unitCard.boardPos})";
            LeanTween.move(_unitCard.gameObject, downPos, 0.25f);
            yield return new WaitForSeconds(0.25f);

            
            
            
            yield return StartCoroutine(SpaceOutUnits(enemyUnits));
            
            //todo: redo summon position so that it rearranges on the tactics phase
            // em.ChooseSummonPosition(_unitCard);       
        }

       
        ui.UpdateManaDisplay();

        _unitCard.roundPlayed = round;

        if(_unitCard.unit.cardEffect){
            var copy = CopyComponent(_unitCard.unit.cardEffect, _unitCard.gameObject);
            copy.GetComponent<CardBonusEffects>().managers = gameObject;
            copy.GetComponent<CardBonusEffects>().attachedCard = _unitCard;
            copy.GetComponent<CardBonusEffects>().DoSummonExtras();
        }

       
        
        yield return null;

   
    }

    public IEnumerator SpaceOutUnits(List<UnitCard> unitList){
        float posModifier = 1f;
        float rightBuffer = 0;
        float leftBuffer = 0;
        float _posY = bm.playerUnitsCenter.transform.localPosition.y;
        float _posZ = 0;

        if(unitList[0].ownership == HeroCard.Ownership.Enemy){
            posModifier = 1.1f;
            _posZ = bm.enemyUnitsCenter.transform.localPosition.z;
        } else {
            _posZ = bm.playerUnitsCenter.transform.localPosition.z;
        }

        if(unitList.Count % 2 == 0){
            leftBuffer = (unitList.Count / 2) -1;
            rightBuffer = (unitList.Count/2);
            foreach(var _card in unitList){

                var movePos = _card.transform.localPosition;
                movePos.y = _posY;
                movePos.z = _posZ;

                if(_card.boardPos == leftBuffer){
                    movePos.x = -(posModifier/2);
                }

                if(_card.boardPos == rightBuffer){
                    movePos.x = (posModifier/2);
                }

                
                if(_card.boardPos < leftBuffer){
                    movePos.x = -((leftBuffer -_card.boardPos) + (posModifier/2));
                    
                } else if(_card.boardPos > rightBuffer){
                    movePos.x = ((_card.boardPos - rightBuffer) + (posModifier/2));
                }
            
                LeanTween.moveLocal(_card.gameObject, movePos, 0.2f);
            }

        } else {
            leftBuffer = (unitList.Count/2);
            foreach(var _card in unitList){

                var movePos = _card.transform.localPosition;
                movePos.y = _posY;
                movePos.z = _posZ;

                if(_card.boardPos == leftBuffer){
                    movePos.x = 0;
                } else {
                    movePos.x = (_card.boardPos - leftBuffer) * posModifier;
                }
                LeanTween.moveLocal(_card.gameObject, movePos, 0.2f);
            }
        }
        yield return null;
    }



    private void DamageDealt(int damage, GameObject target){
        HeroCard heroCardTarget = target.GetComponent<HeroCard>();
        UnitCard unitCardTarget = target.GetComponent<UnitCard>();
        

            if(heroCardTarget){
                if(heroCardTarget.armor > damage){
                    heroCardTarget.armor -= damage;
                } else {
                    int damageLeft = (damage - heroCardTarget.armor);
                    heroCardTarget.armor = 0;
                    heroCardTarget.hp -= damageLeft;
                }
            } else {
                if(unitCardTarget.armor > damage){
                    unitCardTarget.armor -= damage;
                } else {
                    int damageLeft = (damage - unitCardTarget.armor);
                    unitCardTarget.armor = 0;
                    unitCardTarget.hp -= damageLeft;
                }
            }

            StartCoroutine(CheckForDeath(target));

        
    }

    private void BuffUsed(Ability ab, GameObject target){
        HeroCard heroCardTarget = target.GetComponent<HeroCard>();
        UnitCard unitCardTarget = target.GetComponent<UnitCard>();
            if(ab.abilityType == Ability.AbilityType.Alteration){
                if(ab.alterationType == Ability.AlterationType.Armor){
                    if(unitCardTarget){
                        unitCardTarget.maxArmor += ab.baseAmount;
                        unitCardTarget.armor += ab.baseAmount;
                    } else {
                        heroCardTarget.armor += ab.baseAmount;
                    }
                } else if(ab.alterationType == Ability.AlterationType.HP){
                    if(unitCardTarget){
                        unitCardTarget.maxHp += ab.baseAmount;
                        unitCardTarget.hp += ab.baseAmount;
                    } else {
                        heroCardTarget.hp += ab.baseAmount;
                    }                  
                } else if(ab.alterationType == Ability.AlterationType.Damage){
                    if(unitCardTarget){
                        unitCardTarget.damage += ab.baseAmount;
                        unitCardTarget.maxDamage += ab.baseAmount;
                        //note: do I add a damage modifier for abilities for heroes?
                    }
                }
            }
    }

    private void HealUsed(Ability ab, GameObject target){
        HeroCard heroCardTarget = target.GetComponent<HeroCard>();
        UnitCard unitCardTarget = target.GetComponent<UnitCard>();
        if(ab.abilityType == Ability.AbilityType.Heal){
            int _amount = ab.baseAmount + ab.modifier;
            if(heroCardTarget){
                heroCardTarget.hp = Mathf.Min(heroCardTarget.hp + _amount, heroCardTarget.hero.baseHP);
            } else {
                unitCardTarget.hp = Mathf.Min(unitCardTarget.hp + _amount, unitCardTarget.maxHp);
            }
        }
    }

    private void StatusAndBonusEffects(Ability ab, GameObject target){
        if(target){
            HeroCard heroCardTarget = target.GetComponent<HeroCard>();
            UnitCard unitCardTarget = target.GetComponent<UnitCard>();
            if(ab.statusEffect){         
                if(unitCardTarget){
                    ApplyStatusEffect(ab.statusEffect, ab.statusEffect.effectLength, unitCardTarget);
                }       
            }
        }
        
        // have all bonus effects go off    
        if(ab.bonusEffect){
            ab.bonusEffect.managers = gameObject;
            ab.bonusEffect.DoTargetExtras(target);
        }


        // have all units check for the board for their board effects
        foreach(Transform unitCard in bm.activeUnitCardHolder.transform){
            if(unitCard.GetComponent<CardBonusEffects>()){
                unitCard.GetComponent<CardBonusEffects>().BoardCheckExtras();
            }
        }


    }
    
    private void ApplyStatusEffect(StatusEffect effect, int length, UnitCard unitCardTarget){
        var _status = ScriptableObject.CreateInstance<StatusEffect>();
        _status.name = effect.ToString();
        _status.effectType = effect.effectType;
        _status.isBad = effect.isBad;
        _status.fadesAway = effect.fadesAway;
        _status.roundAdded = round;
        unitCardTarget.statusEffects.Add(_status);
    }

    // todo: fix rearranging on death
    public IEnumerator CheckForDeath(GameObject target){
        var _hero = target.GetComponent<HeroCard>();
        var _unit = target.GetComponent<UnitCard>();
        int _hpValue = 99;

        if(_hero){
            _hpValue = _hero.hp;
        } else {
            _hpValue = _unit.hp;
        }

        if(_hpValue <= 0){
            Debug.Log($"{target.name} died");
            // remove if unit

            bool skipDeath = false;
            if(target.GetComponent<CardBonusEffects>()){
                skipDeath = target.GetComponent<CardBonusEffects>().preventDeath;
            }

            if(_unit){
                if(target.GetComponent<CardBonusEffects>()){
                    target.GetComponent<CardBonusEffects>().DoDeathExtras();
                }

                if(!skipDeath){
                    if(_unit.ownership == HeroCard.Ownership.Player){
                        playerUnits.RemoveAt(_unit.boardPos);
                        foreach(var u in playerUnits){
                            if(u.boardPos > _unit.boardPos){
                                u.boardPos -= 1;
                            }
                        }
                        yield return new WaitForSeconds(0.01f);
                        if(playerUnits.Count > 0){
                            yield return StartCoroutine(SpaceOutUnits(playerUnits));
                        }
                        
                    } else {
                        enemyUnits.RemoveAt(_unit.boardPos);
                        foreach(var u in enemyUnits){
                            if(u.boardPos > _unit.boardPos){
                                u.boardPos -= 1;
                            }
                        }
                        yield return new WaitForSeconds(0.01f);
                        if(enemyUnits.Count > 0){
                            yield return StartCoroutine(SpaceOutUnits(enemyUnits));
                        }
                        
                    }
                    Destroy(target.gameObject);
                    yield return new WaitForSeconds(0.1f);
                }
                
            // otherwise do animation thing
            } else {

            }
            
        }

        foreach(Transform unitCard in bm.activeUnitCardHolder.transform){
            if(unitCard.GetComponent<CardBonusEffects>()){
                unitCard.GetComponent<CardBonusEffects>().BoardCheckExtras();
            }
        }

        yield return null;
    }


    private IEnumerator UnitsTakeTurns(){
    
        foreach(UnitCard pu in playerUnits.ToList()){
            if(pu && !pu.attackedThisTurn){
                // check for status effects 
                if(pu.statusEffects.Find(x => x.effectType == StatusEffect.EffectType.Frozen)){
                    continue;
                }


                // does aoe damage to all
                if(pu.unit.targetType == Unit.TargetType.All){
                    pu.attackedThisTurn = true;
                    yield return StartCoroutine(UnitAttacksAll(pu));
                    yield return new WaitForEndOfFrame();
                    continue;
                }


                foreach(UnitCard eu in enemyUnits.ToList()){
                    if(eu){
                        // there's a unit to attack down the line, left to right
                        pu.attackedThisTurn = true;
                        yield return StartCoroutine(UnitAttacksUnit(pu, eu));
                        yield return new WaitForEndOfFrame();
                        break;
                    }                  
                }

                // attack hero instead
                foreach(HeroCard eh in enemyHeroCards.ToList()){
                    if(pu && eh && eh.hp > 0 && !pu.attackedThisTurn){
                        pu.attackedThisTurn = true;
                        yield return StartCoroutine(UnitAttacksHero(pu, eh));
                    }
                }
            }
        }

        yield return new WaitForEndOfFrame();

        foreach(UnitCard eu in enemyUnits.ToList()){
            if(eu && !eu.attackedThisTurn){
                if(eu.statusEffects.Find(x => x.effectType == StatusEffect.EffectType.Frozen)){
                    continue;
                }

                // does aoe damage to all
                if(eu.unit.targetType == Unit.TargetType.All){
                    eu.attackedThisTurn = true;
                    yield return StartCoroutine(UnitAttacksAll(eu));
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                foreach(UnitCard pu in playerUnits.ToList()){
                    // there's a unit to attack down the line, left to right
                    if(pu){
                        eu.attackedThisTurn = true;
                        yield return StartCoroutine(UnitAttacksUnit(eu, pu));
                        yield return new WaitForEndOfFrame();
                        break;
                    }
                }

                // attack hero instaed
                foreach(HeroCard ph in playerHeroCards.ToList()){
                    if(eu && ph && ph.hp > 0 && !eu.attackedThisTurn){
                        eu.attackedThisTurn = true;
                        yield return StartCoroutine(UnitAttacksHero(eu, ph));
                        break;
                    }
                }
            }
        } 

        yield return new WaitForSeconds(1);
        ProcessEndTurn();
        //todo: check for win or draw condition
    }

    //todo: redo all damage, armor, hp to include mods
    private IEnumerator UnitAttacksAll(UnitCard attacker){
        List<UnitCard> targets = new List<UnitCard>();
        if(attacker.ownership == HeroCard.Ownership.Player){
            targets = enemyUnits;
        } else {
            targets = playerUnits;
        }

        Vector3 attackerPos = attacker.transform.position;
        Vector3 aRisePos = attackerPos;
        aRisePos.y += 0.75f;
        LeanTween.move(attacker.gameObject, aRisePos, 0.5f).setEaseInExpo();
        LeanTween.scale(attacker.gameObject, new Vector3(1.4f, 1.4f, 1.4f), 0.5f).setEaseInBack();
        // do animation here
        yield return new WaitForSeconds(0.75f);
        LeanTween.move(attacker.gameObject, attackerPos, 0.25f);
        LeanTween.scale(attacker.gameObject, new Vector3(1,1,1), 0.25f);
        yield return new WaitForSeconds(0.5f);

        foreach(UnitCard uc in targets){
            if(!uc){continue;}
            DamageDealt(attacker.damage, uc.gameObject);
        }
    }


  
    private IEnumerator UnitAttacksHero(UnitCard attacker, HeroCard defender){
        Vector3 attackerPos = attacker.transform.position;
        Vector3 defenderPos = defender.transform.position;
        defenderPos.y += 0.1f;
        Vector3 aRisePos = attackerPos;
        aRisePos.y += 0.75f;

        LeanTween.move(attacker.gameObject, aRisePos, 0.5f).setEaseInExpo();
        yield return new WaitForSeconds(0.75f);
        LeanTween.move(attacker.gameObject, defenderPos, 0.5f).setEaseInBack();
        yield return new WaitForSeconds(0.75f);
        LeanTween.move(attacker.gameObject, attackerPos, 0.25f);
        yield return new WaitForSeconds(0.5f);


        int attackerDamageUsed = 0;
        if(attacker.damage > defender.armor){
            attackerDamageUsed += defender.armor;
            defender.armor = 0;
            defender.hp -= (attacker.damage - attackerDamageUsed);
        } else {
            defender.armor -= attacker.damage;
        }
    }


    private IEnumerator UnitAttacksUnit(UnitCard attacker, UnitCard defender){
        //todo: make sure this works

        Vector3 attackerPos = attacker.transform.localPosition;
        Vector3 defenderPos = defender.transform.localPosition;
        defenderPos.y += 0.1f;
        Vector3 aRisePos = attackerPos;
        aRisePos.y += 0.75f;

        LeanTween.moveLocal(attacker.gameObject, aRisePos, 0.5f).setEaseInExpo();
        yield return new WaitForSeconds(0.75f);
        LeanTween.moveLocal(attacker.gameObject, defenderPos, 0.5f).setEaseInBack();
        yield return new WaitForSeconds(0.75f);
        LeanTween.moveLocal(attacker.gameObject, attackerPos, 0.25f);
        yield return new WaitForSeconds(0.5f);


        int attackerDamageUsed = 0;
        int defenderDamageUsed = 0;

        // attacker does damage to the defender
        if(attacker.damage > defender.armor){
            attackerDamageUsed += defender.armor;
            defender.armor = 0;
            defender.hp -= (attacker.damage - attackerDamageUsed);
            // yield return StartCoroutine(CheckForDeath(defender.gameObject));
        } else {
            defender.armor -= attacker.damage;
        }

        // if the defender is still alive, 
        if(defender.gameObject){
            // do effects if exsit
            if(attacker.unit.cardEffect){
                attacker.unit.cardEffect.DoTargetExtras(defender.gameObject);
            }
            
            // apply default on hit effects
            var _onHitEffects = attacker.unit.onHitStatuses;

            for(var i = 0; i < _onHitEffects.status.Count; i++){
                if(defender.statusEffects.FirstOrDefault(x => x.effectType == _onHitEffects.status[i].effectType)){
                    defender.statusEffects.FirstOrDefault(x => x.effectType == _onHitEffects.status[i].effectType).effectLength += _onHitEffects.length[i];
                } else {
                    ApplyStatusEffect(_onHitEffects.status[i], _onHitEffects.length[i], defender.GetComponent<UnitCard>());
                }
            }
    
            

        }

        // apply default on hit status effects
            

        // the defender does damage to the attacker if attacker not ranged or effected by a status that prohibits it, etc
        if(
            !attacker.unit.ranged 
            || attacker.statusEffects.Find(x => x.effectType == StatusEffect.EffectType.Frozen)
            || defender.unit.targetType != Unit.TargetType.Single
        ) {
            if(defender.damage > attacker.armor){
                defenderDamageUsed += attacker.armor;
                attacker.armor = 0;
                attacker.hp -= (defender.damage - defenderDamageUsed);
                // yield return StartCoroutine(CheckForDeath(attacker.gameObject));
            } else {
                attacker.armor -= defender.damage;
            }



            // if the attacer still alive, do effects
            if(attacker.gameObject){
            // do effects if exsit
                if(defender.unit.cardEffect){
                    defender.unit.cardEffect.DoTargetExtras(attacker.gameObject);
                }
            
            // apply default on hit effects
                var _onHitEffects = defender.unit.onHitStatuses;

                for(var i = 0; i < _onHitEffects.status.Count; i++){
                    if(attacker.statusEffects.FirstOrDefault(x => x.effectType == _onHitEffects.status[i].effectType)){
                        attacker.statusEffects.FirstOrDefault(x => x.effectType == _onHitEffects.status[i].effectType).effectLength += _onHitEffects.length[i];
                    } else {
                        ApplyStatusEffect(_onHitEffects.status[i], _onHitEffects.length[i], attacker.GetComponent<UnitCard>());
                    }
                }
         

            }
        }

        yield return CheckForDeath(attacker.gameObject);
        yield return CheckForDeath(defender.gameObject);
        // has to be longer than a rearraning units
        yield return new WaitForSeconds(0.3f);

    }




    // keep track of if both sides went
    public bool playerWent;
    public bool enemyWent;
    private void ChangeTurn(){
        if(gamePhase == Phase.EndPhase){
            gamePhase = firstPhase;
            playerWent = false;
            enemyWent = false;

            foreach(var eu in enemyUnits){
                if(eu){
                    eu.attackedThisTurn = false;
                }
            }
            foreach(var pu in playerUnits){
                if(pu){
                    pu.attackedThisTurn = false;
                }
            }
            round += 1;

            enemyMana += enemyBaseMana;
            playerMana += playerBaseMana;
            return;
        }

        if(gamePhase == Phase.PlayerTurn){
            playerWent = true;
            if(enemyWent){
                gamePhase = Phase.Strategy;
                // gamePhase = Phase.UnitTurn;
                // StartCoroutine(UnitsTakeTurns());
            } else {
                gamePhase = Phase.EnemyTurn;
                StartCoroutine(em.EnemyTakesTurn());
            }
            return;
        }

        if(gamePhase == Phase.EnemyTurn){
            enemyWent = true;
            if(playerWent){
                gamePhase = Phase.Strategy;
            } else {
                gamePhase = Phase.PlayerTurn;
                
            }

            return;
        }

        if(gamePhase == Phase.Strategy){
            gamePhase = Phase.UnitTurn;
            StartCoroutine(UnitsTakeTurns());
            return;
        }

        if(gamePhase == Phase.UnitTurn){
            gamePhase = Phase.EndPhase;
            return;
        }

        // increase mana for each person by x at end phase?
        gamePhase = Phase.EndPhase;

    }

    public void ProcessEndTurn(){
        ChangeTurn();
        if(gamePhase == Phase.PlayerTurn){
            StartCoroutine(DrawPlayerCards(playerDrawNumber));
        }
        if(gamePhase == Phase.EnemyTurn){
            StartCoroutine(DrawEnemyCards(enemyDrawNumber));
        }

        if(gamePhase == Phase.EndPhase){
            StartCoroutine(ProcessEndPhase());
        }
        ui.phaseText.text = gamePhase.ToString();
        ui.UpdateManaDisplay();
    }

    public IEnumerator ProcessEndPhase(){
        yield return StartCoroutine(HandleStatusEffects());

        playerCardsPlayed = 0;
        playerHeroAbilitiesCast = 0;

        enemyCardsPlayed = 0;
        enemyHeroAbilitiesCast = 0;
        
    }

    private IEnumerator HandleStatusEffects(){
        List<UnitCard> allActiveUnits = new List<UnitCard>();
        allActiveUnits = allActiveUnits.Concat(enemyUnits).Concat(playerUnits).ToList();


        foreach(UnitCard unitCard in allActiveUnits){
            
            if(!unitCard){continue;}
            // Debug.Log(unitCard);
            // Debug.Log($"Checking status effects for {unitCard}");

            // remove the status if expired
            foreach(StatusEffect status in unitCard.statusEffects){
             

                // do all the status effect things todo: finish programmign what status effects do
                if(status.effectType == StatusEffect.EffectType.Burning){
                    unitCard.hp = Mathf.Max(unitCard.hp - 1, 0);
                    unitCard.armor = Mathf.Max(unitCard.armor - 1, 0);
                }

                if(round - status.roundAdded > status.effectLength && status.fadesAway){
                    unitCard.statusEffects.Remove(status);
                }

                StartCoroutine(CheckForDeath(unitCard.gameObject));
            }

            if(unitCard.GetComponent<CardBonusEffects>()){
                unitCard.GetComponent<CardBonusEffects>().DoTurnChangeExtras();
            }
            
        }

        yield return null;
    }



    Component CopyComponent(Component original, GameObject destination){
        System.Type type = original.GetType();
        Component copy = destination.AddComponent(type);
        // Copied fields can be restricted with BindingFlags
        System.Reflection.FieldInfo[] fields = type.GetFields(); 
        foreach (System.Reflection.FieldInfo field in fields) {
            field.SetValue(copy, field.GetValue(original));
            }
        return copy;
    }
    
}




public static class ListExtensions  {
    public static void Shuffle<T>(this IList<T> list) {
        System.Random rnd = new System.Random();
        for (var i = 0; i < list.Count; i++)
            list.Swap(i, rnd.Next(i, list.Count));
    }
 
    public static void Swap<T>(this IList<T> list, int i, int j) {
        var temp = list[i];
        list[i] = list[j];
        list[j] = temp;
    }
}

