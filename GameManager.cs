using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public enum Phase{PlayerTurn,EnemyTurn,UnitTurn,EndPhase}
    public Phase gamePhase;
    public int round;
    private Phase firstPhase;
    [Header("Player")]

    public int playerLevelUpBase = 2;
    public Material playerCardBack;
    public List<Hero> selectedPlayerHeroes;
    public List<HeroCard> playerHeroCards;
    public List<Ability> playerDeck;
    public List<Ability> playerDrawnCards;
    public List<UnitCard> playerUnits = new List<UnitCard>(5);
    private int playerBaseMana;
    public int playerMana;
    [Header("Enemy")]

    public int enemyLevelUpBase = 2;
    public Material enemyCardBack;
    public List<Hero> selectedEnemyHeroes;
    public List<HeroCard> enemyHeroCards;
    public List<Ability> enemyDeck;
    public List<Ability> enemyDrawnCards;
    public List<UnitCard> enemyUnits = new List<UnitCard>(5);
    private int enemyBaseMana;
    public int enemyMana;
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

        ChooseStarter();
        SetUpHeroes();
        SetUpDecks();
        // first player to go gets 1 less card
        StartCoroutine(DrawPlayerCards(4));
        StartCoroutine(DrawEnemyCards(5));
    }

    public Ability testUnit;
    // private void GiveTestUnits(){
    //     for(var i = 0; i < 2; i++){
    //         em.SummonUnit(testUnit);
    //     }
    // }


    private void ChooseStarter(){
        // todo: make random later
        gamePhase = Phase.PlayerTurn;
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

    // public bool PlayerUseAbility(Ability ab, GameObject target){
    //         var _hero = target.GetComponent<HeroCard>();
    //         var _unit = target.GetComponent<UnitCard>();

    //         HeroCard.Ownership _ownership;

    //         if(_hero){
    //             _ownership = _hero.ownership;
    //         } else {
    //             _ownership = _unit.ownership;
    //         }

    //         // -------------- targeting enemy --------------
    //         if(ab.targetType == Ability.TargetType.Enemy){
    //             if(_ownership == HeroCard.Ownership.Enemy){

    //                 // targeting enemy with damage
    //                 if(ab.abilityType == Ability.AbilityType.Damage){
    //                     int totalDamage = 0;
    //                     if(_hero){
    //                         totalDamage = Mathf.Max((ab.baseAmount + ab.modifier) - _hero.armor, 0); 
    //                         if(totalDamage == 0){
    //                             _hero.armor = Mathf.Max(_hero.armor - (ab.baseAmount + ab.modifier));
    //                         } else {
    //                             _hero.hp -= totalDamage;
    //                         }
                            
    //                     } else {
    //                         totalDamage = Mathf.Max((ab.baseAmount + ab.modifier) - _unit.hp, 0); 
    //                         if(totalDamage == 0){
    //                             int armorBeforeHit = _unit.armor;
    //                             _unit.armor = Mathf.Max(_unit.armor - (ab.baseAmount + ab.modifier), 0);
    //                             if(_unit.armor == 0){
    //                                 _unit.hp -= ((ab.baseAmount + ab.modifier) - armorBeforeHit);
    //                             }
    //                         } else {
    //                             _unit.hp -= totalDamage;
    //                         }
    //                     }
    //                     CheckForDeath(target);
    //                 }

    //             } else {
    //                 Debug.Log("Target isn't an enemy");
    //                 return false;
    //             }
    //         }

    //         // --------- target is ally -------------
    //         if(ab.targetType == Ability.TargetType.Ally){
    //             if(_ownership == HeroCard.Ownership.Player){

    //                 // buffs
    //                 if(ab.abilityType == Ability.AbilityType.Alteration){
    //                     // add armor
    //                     if(ab.alterationType == Ability.AlterationType.Armor){
    //                         int _amount = ab.baseAmount + ab.modifier;
    //                         if(_hero){
    //                             _hero.armor += _amount;
    //                         } else {
    //                             _unit.armor += _amount;
    //                         }
    //                     }

    //                     if(ab.alterationType == Ability.AlterationType.HP){
    //                         int _amount = ab.baseAmount + ab.modifier;
    //                         if(_hero){
    //                             _hero.hpMod += _amount;
    //                             _hero.hp += _amount;
    //                         } else {
    //                             _unit.hpMod += _amount;
    //                             _unit.hp += _amount;
    //                         }
    //                     }

    //                     if(ab.alterationType == Ability.AlterationType.Damage){
    //                         int _amount = ab.baseAmount + ab.modifier;
    //                         if(_hero){
    //                             Debug.Log("Target is not a unit");
    //                             return false;
    //                         } else {
    //                             _unit.damage += _amount + ab.modifier;
    //                         }
    //                     }

    //                 }

    //                 // heal
    //                 if(ab.abilityType == Ability.AbilityType.Heal){
    //                     int _amount = ab.baseAmount + ab.modifier;
    //                         if(_hero){
    //                             if(_hero.hp >= _hero.hero.baseHP + _hero.hpMod){
    //                                 Debug.Log("Hero at max HP");
    //                                 return false;
    //                             }
    //                             _hero.hp = Mathf.Min(_hero.hp + _amount, _hero.hero.baseHP + _hero.hpMod);
    //                         } else {
    //                             if(_unit.hp >= _unit.unit.baseHP + _hero.hpMod){
    //                                 Debug.Log("Unit at max HP");
    //                                 return false;
    //                             }
    //                             _unit.hp = Mathf.Min(_unit.hp + _amount, _unit.unit.baseHP + _unit.hpMod);
    //                         }
    //                 }

    //             } else {
    //                 Debug.Log("Target isn't owned by you");
    //                 return false;
    //             }       
    //         }


    //         // add status effects if any
    //         if(ab.statusEffect){
    //             if(_unit){
    //                 var _status = ScriptableObject.CreateInstance<StatusEffect>();
    //                 _status.effectType = ab.statusEffect.effectType;
    //                 _status.isBad = ab.statusEffect.isBad;
    //                 _status.fadesAway = ab.statusEffect.fadesAway;
    //                 _status.roundAdded = round;
    //                 _unit.statusEffects.Add(_status);
    //             }
                
    //         }
            
    //         if(ab.bonusEffect){
    //             ab.bonusEffect.managers = gameObject;
    //             ab.bonusEffect.DoTargetExtras(target);
    //         }

    //         playerMana -= ab.manaCost;
    //         ui.CancelCast();
    //         ui.UpdateManaDisplay();
    //         return true;
    //     }


    public IEnumerator AbilityGoesOff(Ability ab, bool playerCast, GameObject target = null, GameObject selectedPosition = null){

        // todo: add in multi target stuff for all other ability types, not just damage
       
        // unit summoned
        if(ab.abilityType == Ability.AbilityType.Summon){
            UnitSummoned(ab, playerCast, selectedPosition);
        }

        // damage dealt
        if(ab.abilityType == Ability.AbilityType.Damage){

            // if its a board target
            if(ab.targetType == Ability.TargetType.AllEnemies){

                // cast from player
                if(playerCast){
                    foreach(UnitCard unit in enemyUnits.FindAll(x => x != null)){
                        DamageDealt(ab, unit.gameObject);
                    }
                // cast from enemy
                } else {
                    foreach(UnitCard unit in playerUnits.FindAll(x => x !=null)){
                        DamageDealt(ab, unit.gameObject);
                    }
                }

            // ability is single target damage type
            } else if(ab.targetType == Ability.TargetType.Enemy) {
                DamageDealt(ab, target);
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
            playerMana -= ab.manaCost;
        } else{
            enemyMana -= ab.manaCost;
        }

        if(playerCast){
            Debug.Log($"---- Player used {ab} --------");
        } else {
            Debug.Log($"---- Enemy used {ab} --------");
        }

        ui.UpdateManaDisplay();
        yield return null;
    }


    private void UnitSummoned(Ability ab, bool playerCast, GameObject selectedPosition = null){
           
        var _unitCard = Instantiate(bm.unitCardPrefab as UnitCard);
        
        _unitCard.unit = ab.summonedUnit;
        _unitCard.unit.managers = gameObject;


        if(playerCast){
            _unitCard.ownership = HeroCard.Ownership.Player;
            _unitCard.SetUpUnit();
            var overPos = selectedPosition.transform.position;
            var downPos = selectedPosition.transform.position;
            overPos.y += 1;
            downPos.y = 0.01f;
            _unitCard.transform.position = overPos;    
            LeanTween.move(_unitCard.gameObject, downPos, 0.25f);
            var posIndex = selectedPosition.transform.GetSiblingIndex();
            playerUnits[posIndex] = _unitCard;
            _unitCard.name = $"Player's {_unitCard.unit.unitName} (Position: {posIndex})";

        } else {
            _unitCard.ownership = HeroCard.Ownership.Enemy;
            _unitCard.SetUpUnit();
            em.ChooseSummonPosition(_unitCard);       
        }

       
        ui.UpdateManaDisplay();

        if(_unitCard.unit.cardEffect){
            _unitCard.unit.cardEffect.managers = gameObject;
            _unitCard.unit.cardEffect.attachedCard = _unitCard;
            _unitCard.unit.cardEffect.DoSummonExtras();
        }
        _unitCard.transform.SetParent(bm.activeUnitCardHolder.transform);
   
    }
    private void DamageDealt(Ability ab, GameObject target){
        HeroCard heroCardTarget = target.GetComponent<HeroCard>();
        UnitCard unitCardTarget = target.GetComponent<UnitCard>();
        if(ab.abilityType == Ability.AbilityType.Damage){
            int totalDamage = 0;
            if(heroCardTarget){
                totalDamage = Mathf.Max((ab.baseAmount + ab.modifier) - heroCardTarget.armor, 0); 
                if(totalDamage == 0){
                    heroCardTarget.armor = Mathf.Max(heroCardTarget.armor - (ab.baseAmount + ab.modifier));
                } else {
                    heroCardTarget.hp -= totalDamage;
                }
                
            } else {
                totalDamage = Mathf.Max((ab.baseAmount + ab.modifier) - unitCardTarget.hp, 0); 
                if(totalDamage == 0){
                    int armorBeforeHit = unitCardTarget.armor;
                    unitCardTarget.armor = Mathf.Max(unitCardTarget.armor - (ab.baseAmount + ab.modifier), 0);
                    if(unitCardTarget.armor == 0){
                        unitCardTarget.hp -= ((ab.baseAmount + ab.modifier) - armorBeforeHit);
                    }
                } else {
                    unitCardTarget.hp -= totalDamage;
                }
            }
            CheckForDeath(target);
        }
    }

    private void BuffUsed(Ability ab, GameObject target){
        HeroCard heroCardTarget = target.GetComponent<HeroCard>();
        UnitCard unitCardTarget = target.GetComponent<UnitCard>();
            if(ab.abilityType == Ability.AbilityType.Alteration){
                if(ab.alterationType == Ability.AlterationType.Armor){
                    if(unitCardTarget){
                        unitCardTarget.armor += ab.baseAmount;
                    } else {
                        heroCardTarget.armor += ab.baseAmount;
                    }
                } else if(ab.alterationType == Ability.AlterationType.HP){
                    if(unitCardTarget){
                        unitCardTarget.hp += ab.baseAmount;
                    } else {
                        heroCardTarget.hp += ab.baseAmount;
                    }                  
                } else if(ab.alterationType == Ability.AlterationType.Damage){
                    if(unitCardTarget){
                        unitCardTarget.damage += ab.baseAmount;
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
                heroCardTarget.hp = Mathf.Min(heroCardTarget.hp + _amount, heroCardTarget.hero.baseHP + heroCardTarget.hpMod);
            } else {
                unitCardTarget.hp = Mathf.Min(unitCardTarget.hp + _amount, unitCardTarget.unit.baseHP + unitCardTarget.hpMod);
            }
        }
    }

    private void StatusAndBonusEffects(Ability ab, GameObject target){
        if(target){
            HeroCard heroCardTarget = target.GetComponent<HeroCard>();
            UnitCard unitCardTarget = target.GetComponent<UnitCard>();
            if(ab.statusEffect){         
                if(unitCardTarget){
                    var _status = ScriptableObject.CreateInstance<StatusEffect>();
                    _status.effectType = ab.statusEffect.effectType;
                    _status.isBad = ab.statusEffect.isBad;
                    _status.fadesAway = ab.statusEffect.fadesAway;
                    _status.roundAdded = round;
                    unitCardTarget.statusEffects.Add(_status);
                }       
            }
        }
        
        // have all bonus effects go off    
        if(ab.bonusEffect){
            ab.bonusEffect.managers = gameObject;
            ab.bonusEffect.DoTargetExtras(target);
        }
    }
    
    public void CheckForDeath(GameObject target){
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
            if(target.GetComponent<UnitCard>()){
                Destroy(target.gameObject);
            // otherwise do animation thing
            } else {

            }
            
        }
    }


    private IEnumerator UnitsTakeTurns(){
    
        foreach(UnitCard pu in playerUnits){
            if(pu && !pu.attackedThisTurn){
                // check for status effects 
                if(pu.statusEffects.Find(x => x.effectType == StatusEffect.EffectType.Frozen)){
                    continue;
                }
                foreach(UnitCard eu in enemyUnits){
                    if(eu){
                        // there's a unit to attack down the line, left to right
                        pu.attackedThisTurn = true;
                        yield return StartCoroutine(UnitAttacksUnit(pu, eu));
                        yield return new WaitForEndOfFrame();
                        break;
                    }                  
                }

                // attack hero instead
                foreach(HeroCard eh in enemyHeroCards){
                    if(pu && eh && eh.hp > 0 && !pu.attackedThisTurn){
                        pu.attackedThisTurn = true;
                        yield return StartCoroutine(UnitAttacksHero(pu, eh));
                    }
                }
            }
        }

        yield return new WaitForEndOfFrame();

        foreach(UnitCard eu in enemyUnits){
            if(eu && !eu.attackedThisTurn){
                if(eu.statusEffects.Find(x => x.effectType == StatusEffect.EffectType.Frozen)){
                    continue;
                }
                foreach(UnitCard pu in playerUnits){
                    // there's a unit to attack down the line, left to right
                    if(pu){
                        eu.attackedThisTurn = true;
                        yield return StartCoroutine(UnitAttacksUnit(eu, pu));
                        yield return new WaitForEndOfFrame();
                        break;
                    }
                }

                // attack hero instaed
                foreach(HeroCard ph in playerHeroCards){
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
            CheckForDeath(defender.gameObject);
        } else {
            defender.armor -= attacker.damage;
        }


        // the defender does damage to the attacker if attacker not ranged or effected by a status that prohibits it
        if(
            !attacker.unit.ranged 
            || attacker.statusEffects.Find(x => x.effectType == StatusEffect.EffectType.Frozen))
        
        {
            if(defender.damage > attacker.armor){
                defenderDamageUsed += attacker.armor;
                attacker.armor = 0;
                attacker.hp -= (defender.damage - defenderDamageUsed);
                CheckForDeath(attacker.gameObject);
            } else {
                attacker.armor -= defender.damage;
            }
        }


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
                gamePhase = Phase.UnitTurn;
                StartCoroutine(UnitsTakeTurns());
            } else {
                gamePhase = Phase.EnemyTurn;
                
                StartCoroutine(em.EnemyTakesTurn());
            }
            return;
        }

        if(gamePhase == Phase.EnemyTurn){
            enemyWent = true;
            if(playerWent){
                gamePhase = Phase.UnitTurn;
                StartCoroutine(UnitsTakeTurns());
            } else {
                gamePhase = Phase.PlayerTurn;
                
            }

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
            StartCoroutine(DrawPlayerCards(1));
        }
        if(gamePhase == Phase.EnemyTurn){
            StartCoroutine(DrawEnemyCards(1));
        }

        if(gamePhase == Phase.EndPhase){
            StartCoroutine(ProcessEndPhase());
        }
        ui.phaseText.text = gamePhase.ToString();
        ui.UpdateManaDisplay();
    }

    public IEnumerator ProcessEndPhase(){
        yield return StartCoroutine(HandleStatusEffects());
    }

    private IEnumerator HandleStatusEffects(){
        List<UnitCard> allActiveUnits = new List<UnitCard>();
        allActiveUnits = allActiveUnits.Concat(enemyUnits).Concat(playerUnits).ToList();

        foreach(UnitCard unitCard in allActiveUnits){
            if(!unitCard){continue;}
            // Debug.Log($"Checking status effects for {unitCard}");

            // remove the status if expired
            foreach(StatusEffect status in unitCard.statusEffects){
                if(round - status.roundAdded > status.effectLength && status.fadesAway){
                    unitCard.statusEffects.Remove(status);
                    continue;
                }

                // do all the status effect things
                if(status.effectType == StatusEffect.EffectType.Burning){
                    unitCard.hp = Mathf.Max(unitCard.hp - 1, 0);
                    unitCard.armor = Mathf.Max(unitCard.armor - 1, 0);
                }


                CheckForDeath(unitCard.gameObject);

            }
        }

        yield return null;
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

