using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class EnemyManager : MonoBehaviour
{
    public List<UnitCard> activeUnits;
    public List<HeroCard> activeHeroes;
    public List<Ability> activeCards;


    // ----- private vars ----------
    private GameManager gm;
    private BoardManager bm;
    private UIManager ui;


    private List<Ability> ignoredAbilitiesThisTurn = new List<Ability>();
    void Start(){
        gm = gameObject.GetComponent<GameManager>();
        bm = gameObject.GetComponent<BoardManager>();
        ui = gameObject.GetComponent<UIManager>();
    }


    private bool turnOngoing = false;
    private bool abilityUnlockPossible = true;
    // private bool noAbilityTargets = false;
    public IEnumerator EnemyTakesTurn(){
        ResetTurnThings();

        while(turnOngoing){
            RefreshAvailable();
            GetOwnUnitValues();

            if(!CanTakeAction()){
                break;
            }
            yield return new WaitForSeconds(1f);
            ui.UpdateManaDisplay();
       
            if(ShouldLevelUpHero() && abilityUnlockPossible){
                continue;
            }
            
            int mostDirectDamage = MostDirectDamage();
            
            if(ShouldDirectDamageUnit()){
                continue;
            }

            // check for debuff removal
            if(!CheckForDebuffRemoval()){
                // if there's nothing to dispel, choose the best action by value
                ChooseActionWithHighestValue(true);
            }

        }
      
        yield return new WaitForSeconds(1);
        gm.ProcessEndTurn();
        Debug.Log("Turn Ending");
        yield return new WaitForSeconds(1);
       
    }

    private void ResetTurnThings(){
        turnOngoing = true;
        // noAbilityTargets = false;
        abilityUnlockPossible = true;
        ignoredAbilitiesThisTurn.Clear();
        foreach(HeroCard heroCard in gm.enemyHeroCards){
            foreach(Ability ab in heroCard.hero.abilities){
                ab.usedThisRound = false;
            }
        }
    }

    private bool ShouldLevelUpHero(){
        if(!abilityUnlockPossible){
            return false;
        }

        float highestActionValue = ChooseActionWithHighestValue();
        
        float highestUnlockValue = -999;
        Ability chosenUnlock = null;
        HeroCard chosenHero = null;
        
        
        int unlockChecks = 0;
        foreach(HeroCard heroCard in gm.enemyHeroCards){
            if(!heroCard || heroCard.hp <= 0){continue;}
            Ability nextUnlock = heroCard.hero.abilities.OrderBy(x => x.barPosition).FirstOrDefault(x => x.locked);
            if(!nextUnlock){
                continue;
            }
            int levelUpCost = gm.enemyLevelUpBase + nextUnlock.barPosition;

            // unlock costs too much
            //todo: caught in loop, doesn't see it can level up due to negative value score
            if(levelUpCost > gm.enemyMana){
                Debug.Log($"Costs too much to unlock {nextUnlock}");
                unlockChecks += 1;
                continue;
            }

            // Note: adjust the multiplier. 
            float abValue = DetermineAbilityValue(nextUnlock) * (nextUnlock.barPosition * 1.2f);
            abValue -= (levelUpCost);
            Debug.Log($"Will cost {levelUpCost} to unlock {nextUnlock}. Value of ({abValue})");
            if(abValue >= highestActionValue && abValue >= highestUnlockValue && gm.enemyMana >= levelUpCost){
                highestUnlockValue = abValue;
                chosenUnlock = nextUnlock;
                chosenHero = heroCard;
            }
        }


        // if its checked for all unlocks, and can't, gtfo
        if(unlockChecks == gm.enemyHeroCards.FindAll(x => x.hp > 0).Count){
            Debug.Log("--- Not able to unlock anything with given mana ---");
            abilityUnlockPossible = false;
            return false;
        }

        Debug.Log("------");
        Debug.Log(abilityUnlockPossible);
        Debug.Log(gm.enemyHeroCards.FindAll(x => x.hp > 0).Count);
        Debug.Log(unlockChecks);
        Debug.Log("------");


        if(chosenUnlock){
            Debug.Log($"Chose to unlock {chosenUnlock}");
            var unlock = chosenHero.hero.abilities.Find(x => x == chosenUnlock);
            chosenHero.level += 1;
            chosenUnlock.locked = false;
            gm.enemyMana -= gm.enemyLevelUpBase + chosenUnlock.barPosition;
            // note: keep level up cost consistent for enemy and player. Possibly make function in GM.
            gm.enemyLevelUpBase += 2;
            return true;
        }
        return false;
    }

    private bool ShouldDirectDamageUnit(){

        float maxOtherActionValue = ChooseActionWithHighestValue();
        List<UnitCard> sortedUnits = gm.playerUnits.OrderByDescending(x => DetermineUnitValue(x)).ToList();
        foreach(UnitCard unitCard in sortedUnits){
            if(!unitCard){continue;}
            if(unitCard.potentialValue >= maxOtherActionValue && MostDirectDamage() >= unitCard.hp){
                MostDirectDamage(true, unitCard.gameObject);
                return true;
            }
        }

        return false;
    }

    private int MostDirectDamage(bool autoCast = false, GameObject target = null){
        int mostDamage = 0;
        int _manaLeft = gm.enemyMana;

        List<Ability> allDamageAbilities = new List<Ability>();
        List<Ability> allHeroAbilities = new List<Ability>();
        List<Ability> allCardAbilities = new List<Ability>();

        foreach(HeroCard heroCard in gm.enemyHeroCards){
            if(!heroCard){continue;}
            foreach(Ability ab in heroCard.hero.abilities.OrderByDescending(x => x.baseAmount)){
                if(!ab.locked && ab.abilityType == Ability.AbilityType.Damage && ab.manaCost <= gm.enemyMana){
                    allDamageAbilities.Add(ab);
                    if(ab.manaCost <= gm.enemyMana || !ab.usedThisRound){
                        allHeroAbilities.Add(ab);
                    }       
                }
            }
        }

        foreach(Ability ab in gm.enemyDrawnCards.OrderByDescending(x => x.baseAmount)){  
            if(ab.manaCost <= gm.enemyMana && ab.abilityType == Ability.AbilityType.Damage){
                allDamageAbilities.Add(ab);
                if(ab.manaCost <= gm.enemyMana){
                    allCardAbilities.Add(ab);
                }   
            }  
        }  

        allDamageAbilities = allDamageAbilities.OrderByDescending(x => x.baseAmount).ToList();

        int damageNeeded = 0;
        if(target){
            UnitCard _uc = target.GetComponent<UnitCard>();
            if(_uc){
                damageNeeded = _uc.armor + (_uc.hp + _uc.hpMod);
            } else {
                damageNeeded = target.GetComponent<HeroCard>().hp;
            }
            allHeroAbilities = allHeroAbilities.OrderByDescending(x => x.baseAmount).ToList();
            allCardAbilities = allCardAbilities.OrderByDescending(x => x.baseAmount).ToList();
        }

        if(autoCast){
            bool isCard = false;
            if(allCardAbilities.Count > 0){
                if(allHeroAbilities.Count > 0){
                    if(allHeroAbilities[0].baseAmount < allCardAbilities[0].baseAmount){
                        isCard = true;
                    }
                } else {
                    isCard = true;
                }
            } 

            if(isCard){
                int _cardPos = gm.enemyDrawnCards.FindIndex(x => x == allCardAbilities[0]);
                EnemyPlaysCard(_cardPos, target);
                return mostDamage;
            } else {
                foreach(HeroCard heroCard in gm.enemyHeroCards){
                    if(!heroCard){continue;}
                    foreach(Ability ab in heroCard.hero.abilities){
                        if(ab == allHeroAbilities[0]){
                            EnemyHeroUsesAbility(heroCard, ab, target);
                            return mostDamage;
                        }
                    }
                }
            }
        }


        foreach(Ability ab in allDamageAbilities){
            if(ab.manaCost <= _manaLeft){
                mostDamage += ab.baseAmount;
                _manaLeft -= ab.manaCost;
            }
        }  

        // Debug.Log($"Enemy can do max of {mostDamage} directly");

        return mostDamage;  
    }

    private bool CanTakeAction(){

        // can play an action from a hero
        foreach(HeroCard heroCard in gm.enemyHeroCards){
            if(!heroCard){continue;}
            foreach(Ability ab in heroCard.hero.abilities){
                if(!ab.locked && !ignoredAbilitiesThisTurn.Contains(ab)){
                    if(ab.manaCost <= gm.enemyMana){
                        return true;
                    }
                }
            }
        }

        // can play a card
        foreach(Ability ab in gm.enemyDrawnCards){  
            if(ab.manaCost <= gm.enemyMana && !ignoredAbilitiesThisTurn.Contains(ab)){
                return true;
            }  
        }

        // can level something up
        if(ShouldLevelUpHero() && abilityUnlockPossible){
            return true;
        }

        return false;
    }

    private float ChooseActionWithHighestValue(bool autoCast = false){

        float highestActionValue = 0;
        Ability chosenAbility = null;
        HeroCard source = null;
        int cardPos = 0;

        for(var i = 0; i < gm.enemyDrawnCards.Count; i++){
            Ability ab = gm.enemyDrawnCards[i];
            if(!ab || ignoredAbilitiesThisTurn.Contains(ab) || ab.manaCost > gm.enemyMana){continue;}

            float cardValue = DetermineAbilityValue(ab);
            Debug.Log($"{ab.abilityName} has a value of {cardValue}");
            if(cardValue <= 0){
                ignoredAbilitiesThisTurn.Add(ab);
                continue;
            }
            if(cardValue > highestActionValue){
                highestActionValue = cardValue;
                chosenAbility = ab;
                cardPos = i;
            }
        }

        //todo: need to check if has mana, but has no target for something it has mana for 
        if(!chosenAbility){return 0;}
        foreach(HeroCard heroCard in gm.enemyHeroCards){
            if(!heroCard){continue;}
            foreach(Ability ab in heroCard.hero.abilities){
                if(ab.locked || ab.manaCost > gm.enemyMana || ignoredAbilitiesThisTurn.Contains(ab) || ab.usedThisRound){continue;}

                float cardValue = DetermineAbilityValue(ab);
                Debug.Log($"{ab.abilityName} has a value of {cardValue}");
                if(cardValue > highestActionValue){
                    highestActionValue = cardValue;
                    chosenAbility = ab;
                    source = heroCard;
                }
            }
        }

        GameObject target = FindTarget(chosenAbility, highestActionValue);
        if(!target && chosenAbility.abilityType != Ability.AbilityType.Summon){
            // cant find a target for highest chosen
            ignoredAbilitiesThisTurn.Add(chosenAbility);
            return highestActionValue;
        }

        // need to generate a target for the chosen ability with autocast
        if(autoCast){
            // decided to cast a summon
            if(chosenAbility.abilityType == Ability.AbilityType.Summon){
                if(!source){
                    EnemyPlaysCard(cardPos);    
                } else {
                    EnemyHeroUsesAbility(source, chosenAbility);
                }
                return highestActionValue;
            }
            
    
            if(!source){
                EnemyPlaysCard(cardPos, target);    
            } else {
                EnemyHeroUsesAbility(source, chosenAbility, target);
            }
        }
 


        return highestActionValue;


        // float targetableThreat = 0;
        // foreach(HeroCard heroCard in gm.playerHeroCards){
        //     if(!heroCard){continue;}

        //     foreach(Ability ab in heroCard.hero.abilities){
        //         if(ab.locked || ab.manaCost > gm.playerMana){continue;}
        //         if(ab.abilityType == Ability.AbilityType.Damage){
        //             targetableThreat += DetermineAbilityValue(ab);
        //         }
        //     }
        // }

        // Debug.Log($"Player has {targetableThreat} direct damage");

    }

    private GameObject FindTarget(Ability ab, float comparedValue){
        if(!ab){
            turnOngoing = false;
            return null;
        }
        Debug.Log($"Finding target for {ab}");
        // if(ab.abilityType == Ability.AbilityType.Summon && ab.summonType == Ability.SummonType.Ally){
        //     StartCoroutine(AbilityGoesOff(ab));
        //     return null;
        // }

        float highestValue = 0;
        GameObject chosenTarget = null;


        

        // looking for a bonus effect
        if(ab.bonusEffect){   
            System.Type specificBonusEffect = ab.bonusEffect.GetType();
            Debug.Log($"Looking for bonus effect type of {specificBonusEffect}");

            // shatter status effect card being used
            if(specificBonusEffect == typeof(Shatter_SE)){
                foreach(UnitCard unitCard in gm.playerUnits){
                    if(!unitCard){continue;}
                    float value = DetermineUnitValue(unitCard);
                    value += unitCard.armor;
                    if(value + 2 > comparedValue && value > highestValue){
                        highestValue = value;
                        chosenTarget = unitCard.gameObject;
                    }
                }
            }
        }

        // looking for status effect
        if(ab.statusEffect){
            var specificStatus = ab.statusEffect.effectType;
            Debug.Log($"Looking for status effect type of {specificStatus}");
            if(specificStatus == ab.statusEffect.effectType){
                foreach(UnitCard unitCard in gm.playerUnits){
                    if(!unitCard){continue;}
                    if(unitCard.statusEffects.Find(x => x.effectType == specificStatus)){continue;}
                    float value = DetermineUnitValue(unitCard);
                    if(value + 2 > comparedValue && value > highestValue){
                        highestValue = value;
                        chosenTarget = unitCard.gameObject;
                    }
                }
            }
        }


        // looking for heal target
        if(ab.abilityType == Ability.AbilityType.Heal){
            float maxHealValue = 0;
            if(ab.abilityType == Ability.AbilityType.Heal){
                foreach(UnitCard unitCard in gm.enemyUnits){
                    if(!unitCard){continue;}
                    if(unitCard.hp < unitCard.unit.baseHP + unitCard.hpMod){
                        float unitHealValue = (unitCard.unit.baseHP - unitCard.hp);
                        if(unitHealValue > maxHealValue){
                            chosenTarget = unitCard.gameObject;
                        }
                    }
                    
                }
            }
        }

        // looking for direct damage target
        if(ab.abilityType == Ability.AbilityType.Damage){
            float maxDamageValue = 0;
            foreach(UnitCard unitCard in gm.playerUnits){
                if(!unitCard){continue;}
                float value = DetermineUnitValue(unitCard);
                if(ab.baseAmount >= (unitCard.hp + unitCard.armor)){
                    value = value * 3;
                }

                if(value > maxDamageValue){
                    maxDamageValue = value;
                    chosenTarget = unitCard.gameObject;
                }
            }

            foreach(HeroCard heroCard in gm.playerHeroCards){
                if(!heroCard || heroCard.hp <= 0){continue;}
                float value = ab.baseAmount * ((heroCard.hero.baseHP / heroCard.hp) * 2);
                if(heroCard.hp - ab.baseAmount <= 0){
                    // if the direct attack can kill a hero, Do it.
                    value = 999;
                }
                if(value >= maxDamageValue){
                    maxDamageValue = value;
                    chosenTarget = heroCard.gameObject;
                }
            }
        }    



        if(ab.abilityType == Ability.AbilityType.Alteration && ab.targetType == Ability.TargetType.Ally && !ab.usedThisRound){
            Debug.Log("BUFFING FROM HERE");
            chosenTarget = TryToBuff(ab);
        }


        return chosenTarget;
    }


    private void FindHighestPotentialSummon(){

        List<Ability> allBuffAbilities = new List<Ability>();
        List<Ability> allPotentialSummons = new List<Ability>();

        foreach(Ability ab in gm.enemyDrawnCards){
            if(ab.abilityType == Ability.AbilityType.Alteration){
                allBuffAbilities.Add(ab);
            }
            if(ab.abilityType == Ability.AbilityType.Summon){
                allPotentialSummons.Add(ab);
            }
        }

        foreach(HeroCard heroCard in gm.enemyHeroCards){
            foreach(Ability ab in heroCard.hero.abilities){
                if(ab.locked){continue;}
                if(ab.abilityType == Ability.AbilityType.Alteration){
                    allBuffAbilities.Add(ab);
                }
                if(ab.abilityType == Ability.AbilityType.Summon){
                    allPotentialSummons.Add(ab);
                }
            }
        }

        allBuffAbilities = allBuffAbilities.OrderByDescending(x => x.baseAmount).ToList();
        allPotentialSummons = allPotentialSummons.OrderByDescending(x => x.manaCost).ToList();


        foreach(Ability ab in allPotentialSummons){
            // Debug.Log(ab);
            float _potential = 0;
            int _usableMana = gm.enemyMana - ab.manaCost;

            foreach(Ability _ab in allBuffAbilities){
                if(_ab.manaCost <= _usableMana){
                    _potential += BuffWorthiness(_ab, null, ab.summonedUnit);
                    _usableMana -= _ab.manaCost;
                }
            }
            // Debug.Log($"Potential Unit ({ab.summonedUnit.unitName}) has a potential of {_potential}");
        }
    }

    private GameObject TryToBuff(Ability chosenAbility){
        // bool isCard = false;
        // int cardPos = 0;

        float maxWorthiness = 0;
        UnitCard chosenBuffee = null;
        // HeroCard chosenHero = null;
        // Ability chosenBuff = null;


        foreach(UnitCard unit in gm.enemyUnits){
            if(!unit){continue;}
            // Debug.Log(unit);
            float _worthiness = BuffWorthiness(chosenAbility, unit);
            if(_worthiness > maxWorthiness){
                maxWorthiness = _worthiness;
                chosenBuffee = unit;
                // chosenBuff = ab;
                // chosenHero = heroCard;
            }   
        }

        if(!chosenBuffee){
            return null;
        }
        return chosenBuffee.gameObject;

    }

    private float BuffWorthiness(Ability ab, UnitCard unitCard, Unit unit = null){
        float _armorWeight = 1.5f;
        float _hpWeight = 0.75f;
        float _threatWeight = 0.3f;


        float worthiness = 0;

        if(unitCard){
            float unitPotential = DetermineUnitValue(unitCard) + (ab.baseAmount * 5);
            worthiness = (unitPotential - (ImmediateThreatToUnit(unitCard) * _threatWeight)) / ab.manaCost;

            if(ab.alterationType == Ability.AlterationType.Armor){
                worthiness += (ab.baseAmount * (5 - unitCard.boardPos)) * _armorWeight;
            }

            if(ab.alterationType == Ability.AlterationType.HP){
                worthiness += (ab.baseAmount * (5 - unitCard.boardPos)) * _hpWeight;
            }

            // Debug.Log($"Unit ({unitCard.unit.unitName}) has a potential worth of {worthiness}");
        } else {
            UnitCard tempCard = new UnitCard();
            tempCard.unit = unit;
            tempCard.SetUpUnit(true);
    
            for(var i = 0; i < gm.enemyUnits.Count; i++){
                if(!gm.enemyUnits[i]){
                    tempCard.boardPos = i;
                    break;
                }
            }
            
            float unitPotential = DetermineUnitValue(tempCard) + (ab.baseAmount * 5);   
            worthiness = (ab.manaCost / (unitPotential - (ImmediateThreatToUnit(tempCard) * _threatWeight))) * 10;

            if(ab.alterationType == Ability.AlterationType.Armor){
                worthiness += (ab.baseAmount * (5 - unitCard.boardPos)) * _armorWeight;
            }

            if(ab.alterationType == Ability.AlterationType.HP){
                worthiness += (ab.baseAmount * (5 - unitCard.boardPos)) * _hpWeight;
            }

            // Debug.Log($"Temp Unit ({tempCard.unit.unitName}) has a potential worth of {worthiness}");
            Destroy(tempCard);
           
        }

         return worthiness;
    }

    private bool CheckForDebuffRemoval(){
        List<UnitCard> _unitsWithDebuffs = new List<UnitCard>();
        foreach(UnitCard unitCard in gm.enemyUnits){
            if(!unitCard){continue;}
            foreach(var _status in unitCard.statusEffects){
                if(_status.isBad){
                    _unitsWithDebuffs.Add(unitCard);
                }
            }
        }

        if(_unitsWithDebuffs.Count == 0){
            return false;
        }

        float maxUnitVal = 0;
        UnitCard chosenUnit = null;
        foreach(UnitCard u in _unitsWithDebuffs){
            float unitValue = DetermineUnitValue(u);
            if(unitValue > maxUnitVal){
                maxUnitVal = unitValue;
                chosenUnit = u;
            }
        }

        return FindDispelSource(chosenUnit);

        

 
    }

    private bool FindDispelSource(UnitCard u){
        Dictionary<HeroCard, Ability> _heroEffects = new Dictionary<HeroCard, Ability>();
        Dictionary<int, Ability> _cardEffects = new Dictionary<int, Ability>();
        int lowestCostDispel = 99;

        foreach(HeroCard heroCard in gm.enemyHeroCards){
            if(!heroCard){continue;}
            foreach(Ability ab in heroCard.hero.abilities){
                if(ab.locked || ab.manaCost > gm.enemyMana){continue;}
                if(ab.abilityType == Ability.AbilityType.Dispel){
                    Debug.Log(ab);
                    _heroEffects[heroCard] = ab;
                    if(ab.manaCost < lowestCostDispel){
                        lowestCostDispel = ab.manaCost;
                    }
                }
            }
        }

        for(var i = 0; i < gm.enemyDrawnCards.Count; i++){
            if(!gm.enemyDrawnCards[i] || gm.enemyDrawnCards[i].manaCost > gm.enemyMana){continue;}
            if(gm.enemyDrawnCards[i].abilityType == Ability.AbilityType.Dispel){
                Debug.Log(_cardEffects[i]);
                _cardEffects[i] = gm.enemyDrawnCards[i];
                if(_cardEffects[i].manaCost <= lowestCostDispel){
                    lowestCostDispel = _cardEffects[i].manaCost;
                }
            }
        }

        // is it worht it to dispel?
        var unitVal = DetermineUnitValue(u);
        if(unitVal < (2 * lowestCostDispel)){
            // Debug.Log($"{u} dispel too expensive. Value: {unitVal} ---- Percieved Cost: {lowestCostDispel * 2}");
        }


        if(_heroEffects.Count == 0 && _cardEffects.Count == 0){
            return false;
        } else {
            if(_cardEffects.Count == 0){
                var _sortedHero = _heroEffects.OrderBy(x => x.Value.manaCost).ToDictionary(x => x.Key, x => x.Value);
                EnemyHeroUsesAbility(_sortedHero.ElementAt(0).Key, _sortedHero.ElementAt(0).Value, u.gameObject);
            } else if(_heroEffects.Count == 0){
                var _sortedCard = _cardEffects.OrderBy(x => x.Value.manaCost).ToDictionary(x => x.Key, x => x.Value);
                EnemyPlaysCard(_sortedCard.ElementAt(0).Key, u.gameObject);
            } else {
                var _sortedHero = _heroEffects.OrderBy(x => x.Value.manaCost).ToDictionary(x => x.Key, x => x.Value);
                var _sortedCard = _cardEffects.OrderBy(x => x.Value.manaCost).ToDictionary(x => x.Key, x => x.Value);

                if(_sortedHero.ElementAt(0).Value.manaCost <= _sortedCard.ElementAt(0).Value.manaCost){
                    EnemyHeroUsesAbility(_sortedHero.ElementAt(0).Key, _sortedHero.ElementAt(0).Value, u.gameObject);
                } else {
                    EnemyPlaysCard(_sortedCard.ElementAt(0).Key, u.gameObject);
                }
            }

            return true;
        }


    }

 
    public void SummonUnit(Ability ab){
        var _unitCard = Instantiate(bm.unitCardPrefab as UnitCard);
        
        _unitCard.unit = ab.summonedUnit;

        _unitCard.unit.managers = gameObject;
        _unitCard.ownership = HeroCard.Ownership.Enemy;
        _unitCard.SetUpUnit();

        ChooseSummonPosition(_unitCard);

        // gm.enemyMana -= ab.manaCost;

       
        if(_unitCard.unit.cardEffect){
            _unitCard.unit.cardEffect.managers = gameObject;
            _unitCard.unit.cardEffect.attachedCard = _unitCard;
            _unitCard.unit.cardEffect.DoExtras();
        }
        _unitCard.transform.SetParent(bm.activeUnitCardHolder.transform);

        DetermineUnitValue(_unitCard);
        Debug.Log($"Enemy summons a {ab.summonedUnit}");
    }


    private void ChooseSummonPosition(UnitCard unitCard){
        int chosenPos = 99;

        List<int> viableSlots = new List<int>();
        for(var i = 0; i < bm.enemyUnitCards.Count; i++){
            if(gm.enemyUnits[i] == null){
                viableSlots.Add(i);
            }
        }

        Dictionary<int, float> posAndDef = new Dictionary<int, float>();
        for(var i = viableSlots[0]; i < bm.enemyUnitCards.Count; i++){
            float _posDef = 0;

            for(var k = 0; k < i; k++){
                if(gm.enemyUnits[k] != null){
                    _posDef += DetermineUnitValue(gm.enemyUnits[k]);
                } else {
                    _posDef += 0.25f * (k + 1);
                }
            }
            posAndDef[i] = _posDef;
            // Debug.Log($"Pos {i} has defense of {_posDef}");
        }

        // defenders go in front
        if(unitCard.unit.unitRole == Unit.UnitRole.Defender){
            chosenPos = posAndDef.OrderBy(x => x.Value).ElementAt(0).Key;
        }

        // glass hang back
        if(unitCard.unit.unitRole == Unit.UnitRole.Glass){
            chosenPos = posAndDef.OrderByDescending(x => x.Value).ElementAt(0).Key;
        }

        // neutral takes first avail
        if(unitCard.unit.unitRole == Unit.UnitRole.Neutral){
            chosenPos = viableSlots[0];
        }

        unitCard.boardPos = chosenPos;

        var selectedPosition = bm.enemyUnitCards[chosenPos];

        var overPos = selectedPosition.transform.position;
        var downPos = selectedPosition.transform.position;
        overPos.y += 1;
        downPos.y = 0.01f;
        unitCard.transform.position = overPos;
         
        LeanTween.move(unitCard.gameObject, downPos, 0.25f);

        var posIndex = selectedPosition.transform.GetSiblingIndex();
        gm.enemyUnits[posIndex] = unitCard;
        unitCard.name = $"Enemy's {unitCard.unit.unitName} (Position: {posIndex})";

    }


    private float ImmediateThreatToUnit(UnitCard unitThreatened){
        float _threat = 0;

        foreach(UnitCard u in gm.playerUnits){
            if(!u){continue;}
            // Debug.Log($"Adding {DetermineUnitValue(u)} from {u}");
            _threat += DetermineUnitValue(u);
        }

        List<Ability> _availableAbilities = new List<Ability>();
        foreach(HeroCard heroCard in gm.playerHeroCards){
            foreach(Ability ab in heroCard.hero.abilities){
                if(ab.locked){continue;}
                if(ab.abilityType != Ability.AbilityType.Damage){continue;}
                _availableAbilities.Add(ab);
            }
        }
      
        int _playerMana = gm.playerMana;
        while(true){
            if(_availableAbilities.OrderBy(x => x.manaCost).ElementAt(0).manaCost > _playerMana){
                break;
            }

            foreach(Ability ab in _availableAbilities.OrderByDescending(x => x.baseAmount)){
                _threat += ab.baseAmount;
                _playerMana -= ab.manaCost;
            }
        }
  
        for(var i = 0; i < gm.enemyUnits.Count; i++){
            if(!gm.enemyUnits[i]){continue;}
            if(i < unitThreatened.boardPos){
                _threat -= DetermineUnitValue(gm.enemyUnits[i]);
            } 
        }

        // Debug.Log($"{unitThreatened} has an active threat of {_threat}");
        return _threat;


    }

    private int MaxUnitDamageToPlayer(){
        int _usableMana = gm.enemyMana;

        int _possibleUnitDamage = 0;

        // get total damage of all active units
        foreach(UnitCard unit in gm.enemyUnits){
            if(!unit){continue;}
            _possibleUnitDamage += unit.damage;
        }

        // add any buffs if available
        foreach(Ability ab in gm.enemyDrawnCards){
            if(ab.abilityType == Ability.AbilityType.Alteration && ab.alterationType == Ability.AlterationType.Damage){
                if(_usableMana >= ab.manaCost){
                    // Debug.Log($"Can buff with {ab.abilityName}");
                    _usableMana -= ab.manaCost;
                    _possibleUnitDamage += ab.baseAmount;
                };       
            }
        }

        foreach(HeroCard hero in gm.enemyHeroCards){
            if(!hero){continue;}
            foreach(Ability ab in hero.hero.abilities){
                if(ab.locked){continue;}
                if(ab.abilityType == Ability.AbilityType.Alteration && ab.alterationType == Ability.AlterationType.Damage){
                    if(_usableMana >= ab.manaCost){
                        _usableMana -= ab.manaCost;
                        _possibleUnitDamage += ab.baseAmount;
                        // Debug.Log($"Can buff with {ab.abilityName}");
                    };

                }
            }
        }

        // subtract armor and hp of player units
        foreach(UnitCard unit in gm.playerUnits){
            if(!unit){continue;}
            _possibleUnitDamage -= unit.armor;
            _possibleUnitDamage -= unit.hp;
        }

        return _possibleUnitDamage;

    }

   
    private bool FindEffect(System.Type effect, GameObject target){
         // ---------- used to find a source of a specific card bonus effect -----------
        Dictionary<HeroCard, Ability> _heroEffects = new Dictionary<HeroCard, Ability>();
        Dictionary<int, Ability> _cardEffects = new Dictionary<int, Ability>();
        _heroEffects = HeroCanDoEffect(effect);
        _cardEffects = CardCanDoEffect(effect);

        // if effect not found, return false
        if(_heroEffects.Count == 0 && _cardEffects.Count == 0){
            return false;
        }

        // effect exists in both hero and card form
        if(_cardEffects.Count >= 1 && _heroEffects.Count >= 1){
            // use the hero ability first if its less than the card
            if(_heroEffects.ElementAt(0).Value.manaCost <= _cardEffects.ElementAt(0).Value.manaCost){
                EnemyHeroUsesAbility(_heroEffects.ElementAt(0).Key, _heroEffects.ElementAt(0).Value, target);
            } else {
                // otherwise use the card
                EnemyPlaysCard(_cardEffects.ElementAt(0).Key, target);
            }

            return true;

        // if effect exists in just one, do that one
        } else {
            if(_cardEffects.Count >= 1){
                EnemyPlaysCard(_cardEffects.ElementAt(0).Key, target);
            }

            if(_heroEffects.Count >= 1){
                EnemyHeroUsesAbility(_heroEffects.ElementAt(0).Key, _heroEffects.ElementAt(0).Value, target);
            }
            return true;
        }
    }


    private Dictionary<HeroCard, Ability> HeroCanDoEffect(System.Type effect){

        Dictionary<HeroCard, Ability> possibleHeroCasts = new Dictionary<HeroCard, Ability>();
       
        // search through all heroes for source of effect
        foreach(HeroCard hero in gm.enemyHeroCards){
            if(!hero){continue;}
            foreach(Ability ab in hero.hero.abilities){
                if(ab.locked){continue;}
                if(ab.manaCost > gm.enemyMana){continue;}
                if(ab.bonusEffect){
                    if(ab.bonusEffect.GetType() == effect){
                        possibleHeroCasts[hero] = ab;
                    }
                }
            }         
        }
        return possibleHeroCasts.OrderBy(x => x.Value.manaCost).ToDictionary(x => x.Key, x => x.Value);
    }


    private Dictionary<int, Ability> CardCanDoEffect(System.Type effect){

        Dictionary<int, Ability> possibleCardCasts = new Dictionary<int, Ability>();

        // search through all cards for source of effect
        for(var i = 0; i < gm.enemyDrawnCards.Count; i++){
            if(gm.enemyDrawnCards[i].manaCost > gm.enemyMana){continue;}
            if(gm.enemyDrawnCards[i].bonusEffect){
                if(gm.enemyDrawnCards[i].bonusEffect.GetType() == effect){
                    possibleCardCasts[i] = gm.enemyDrawnCards[i];
                }
            }
        }
        return possibleCardCasts.OrderBy(x => x.Value.manaCost).ToDictionary(x => x.Key, x => x.Value);
    }

    private void EnemyHeroUsesAbility(HeroCard card, Ability ab, GameObject target = null){
        gm.enemyMana -= ab.manaCost;
        ab.usedThisRound = true;
        StartCoroutine(AbilityGoesOff(ab, target));
        
        // Debug.Log($"{card.hero.heroName} uses {ab.abilityName} on {target}");
    }

    private void EnemyPlaysCard(int cardPos, GameObject target = null){
        Ability ab = gm.enemyDrawnCards[cardPos];
        StartCoroutine(AbilityGoesOff(ab, target));
        gm.enemyMana -= ab.manaCost;
        gm.enemyDrawnCards.RemoveAt(cardPos);
        // Debug.Log($"Enemy uses the {ab.abilityName} card on {target}");
    }

 
    private IEnumerator AbilityGoesOff(Ability ab, GameObject target = null){
        yield return null;

        Debug.Log($"Enemy is using {ab} on {target}");

        // just a summon
        if(ab.abilityType == Ability.AbilityType.Summon){
            SummonUnit(ab);
            yield break;
        }

        // if(ab.abilityType == Ability.AbilityType.Alteration && ab.targetType == Ability.TargetType.Ally){
        //     target = TryToBuff(ab);
        // }

   

        if(target){
            HeroCard heroCardTarget = target.GetComponent<HeroCard>();
            UnitCard unitCardTarget = target.GetComponent<UnitCard>();

            // damage
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
                gm.CheckForDeath(target);
            }

            // buffs
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

            // heals
             if(ab.abilityType == Ability.AbilityType.Heal){
                int _amount = ab.baseAmount + ab.modifier;
                if(heroCardTarget){
                    heroCardTarget.hp = Mathf.Min(heroCardTarget.hp + _amount, heroCardTarget.hero.baseHP + heroCardTarget.hpMod);
                } else {
                    unitCardTarget.hp = Mathf.Min(unitCardTarget.hp + _amount, unitCardTarget.unit.baseHP + unitCardTarget.hpMod);
                }
            }


            // after effects for targeted ability
            if(ab.statusEffect){         
                if(unitCardTarget){
                    var _status = ScriptableObject.CreateInstance<StatusEffect>();
                    _status.effectType = ab.statusEffect.effectType;
                    _status.isBad = ab.statusEffect.isBad;
                    _status.fadesAway = ab.statusEffect.fadesAway;
                    _status.roundAdded = gm.round;
                    unitCardTarget.statusEffects.Add(_status);
                }       
            }
        }
        


        // have all bonus effects go off    
        if(ab.bonusEffect){
            ab.bonusEffect.managers = gameObject;
            ab.bonusEffect.DoExtras(target);
        }

        Debug.Log($"Enemy uses {ab.abilityName} on {target}");

    }


    public void GetOwnUnitValues(){
        foreach(var unit in activeUnits){
            if(!unit){return;}
            var unitVal = DetermineUnitValue(unit);
        }
    }

    public void GetPlayerUnitValues(){
        foreach(var unit in gm.playerUnits){
            if(!unit){return;}
            var unitVal = DetermineUnitValue(unit);
        }
    }

    private float DetermineUnitValue(UnitCard unitCard, Unit unit = null){
        float pVal = 0;
        if(!unitCard && !unit){return 0;}

        // its a generic unit concept, not in play yet
        if(unit){
            pVal += (unit.baseHP * 0.5f);
            pVal += (unit.baseDamage * 1.25f);
            pVal += (unit.baseArmor * 1.55f);

            if(unit.cardEffect){
                pVal += (unit.cardEffect.effectValue * 1.5f);
            }

            if(unit.ranged){pVal += 1;}
            return pVal;
        }

        // its a card
        pVal += (unitCard.hp * 0.5f);
        pVal += (unitCard.damage * 1.25f);
        pVal += (unitCard.armor * 1.55f);

        if(unitCard.unit.cardEffect){
            pVal += (unitCard.unit.cardEffect.effectValue * 1.5f);
        }

        unitCard.potentialValue = pVal;
        if(unitCard.unit.ranged){pVal += 1;}
        return pVal; 
    }

    private float DetermineAbilityValue(Ability ab){
        if(ab.usedThisRound){return 0;}
        // base values
        float pVal = 0;
        pVal += (ab.baseAmount * 1.25f);
        
        if(ab.abilityType == Ability.AbilityType.Summon){
            if(!gm.enemyUnits.Contains(null)){return 0;}
            pVal += (DetermineUnitValue(null, ab.summonedUnit) * 2);
        }

        if(ab.statusEffect){
            bool viableTarget = false;
            foreach(UnitCard unitCard in gm.playerUnits){
                if(!unitCard){continue;}
                if(!unitCard.statusEffects.Contains(ab.statusEffect)){
                    viableTarget = true;
                    break;
                }
            }
            if(viableTarget){
                pVal += (ab.statusEffect.effectLength * 2);
            } else {
                // no viable target for the status effect
                return 0;
            }
            
        }

        if(ab.bonusEffect){
            pVal += (ab.bonusEffect.effectValue * 1.5f);
        }

        pVal -= (ab.manaCost/2);
        // Debug.Log($"{pVal} ----");

        // healing
        float maxHealValue = 0;
        if(ab.abilityType == Ability.AbilityType.Heal){
            foreach(UnitCard unitCard in gm.enemyUnits){
                if(!unitCard){continue;}

                if(unitCard.hp < unitCard.unit.baseHP){
                    maxHealValue += (unitCard.unit.baseHP - unitCard.hp);
                }
            }
            pVal += (maxHealValue * 2f);

            if(maxHealValue == 0){
                pVal = 0;
            }
        }

        // buffs / debuffs
        float maxDebuffVal = 0;
        if(ab.abilityType == Ability.AbilityType.Alteration){
            if(ab.targetType == Ability.TargetType.Enemy){
                foreach(UnitCard unitCard in gm.playerUnits){
                    if(!unitCard){continue;}
                    if(ab.alterationType == Ability.AlterationType.Armor){
                        if(unitCard.armor == 0){continue;}
                    }
                    maxDebuffVal = DetermineUnitValue(unitCard) - (ab.baseAmount * 2);
                }

                pVal += maxDebuffVal;
                if(maxDebuffVal == 0){
                    pVal = 0;
                }
            } else {
                pVal += ab.baseAmount;
            }
        }

        // todo: add dispel value as well as status effect


        if(ab.statusEffect){
            pVal += (ab.statusEffect.statusValue * 2);
        }


        return pVal;
    }


    public void RefreshAvailable(){
        activeUnits = gm.enemyUnits;
        activeHeroes = gm.enemyHeroCards;
        activeCards = gm.enemyDrawnCards;
    }   
}
