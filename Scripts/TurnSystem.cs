using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
/// <summary>
/// The main logic for turns in combat. 
/// </summary>


public class TurnSystem : MonoBehaviour
{
    [Header("References")]
    GameManager gm;
    [SerializeField]
    CanvasCombat canvasCombat;
    [SerializeField]
    

    [Header("Lists")]
    public List<UnitStats> unitsLeftToAct = new();
    public List<UnitStats> unitsInBattle = new();
    public List<Action> actionsToAdd = new();
    public Queue<Action> actionList = new();
    

    private void Start()
    {
        gm = GameManager.gm;
        foreach (Player playerUnit in gm.party)
        {
            UnitStats currentUnitStats = playerUnit.GetComponent<UnitStats>();
            unitsInBattle.Add(currentUnitStats);
        }

        foreach (Enemy enemyUnit in canvasCombat.enemyDisplay)
        {
            UnitStats currentUnitStats = enemyUnit.GetComponent<UnitStats>();
            unitsInBattle.Add(currentUnitStats);
        }
    }

    // At the start of each round of combat, our list of combatants is cleared and repopulated then units are sorted by speed
    public void BeginRound()
    {
        unitsLeftToAct.Clear();
        foreach (UnitStats unit in unitsInBattle)
        {
            if (unit.isGuarded) unit.isGuarded = false;
            unit.CalculateNextActTurn(0);
            unitsLeftToAct.Add(unit);
        }
        

        //Enemy clickability are disabled until we're sure a player character has started its turn (see: SelectUnit.cs)
        EnemyButtonsInteractable(false);
        NextTurn();
    }

    // Every turn starts by checking if one side has been wiped out and, if so, runs the appropriate logic for a win or loss
    // Then we check if there are combatants that need to take a turn
    // If not, the script executes all queued up Action instances in actionList and starts a new round
    public void NextTurn()
    {
        List<UnitStats> livingEnemyUnits = new();
        List<UnitStats> livingPlayerUnits = new();
        foreach (UnitStats unit in unitsInBattle)
        {
            if (unit.character == eChar.enemy && !unit.IsDead())
            {
                livingEnemyUnits.Add(unit);
            }
            else livingPlayerUnits.Add(unit);
        }
        if (livingEnemyUnits.Count == 0)
        {   
            StartCoroutine(CollectReward(true));
            return;
        }
        if (livingPlayerUnits.Count == 0)
        {
            StartCoroutine(CollectReward(false));
            return;
        }
        if (unitsLeftToAct.Count == 0)
        {
            actionsToAdd.Sort();
            foreach (Action action in actionsToAdd)
            {
                actionList.Enqueue(action);
            }
            actionsToAdd.Clear();
            StartCoroutine(ExecuteActions());
            return;
        }
        UnitStats currentUnitStats = unitsLeftToAct[0]; // We point to the next Unit to go in the turn order list (unitStats)
        unitsLeftToAct.Remove(currentUnitStats); // Current unit is removed so that the game knows when everyone has taken a turn

        

        if (!currentUnitStats.IsDead()) // Dead units are persistent in case they get revived
        {   
            GameObject currentUnit = currentUnitStats.gameObject;
            if (currentUnitStats.character != eChar.enemy)
            {
                battleState = eBattleState.playerTurn;
                canvasCombat.ShowMessageInfo(currentUnitStats.unitStatus.charName + "'s Turn", .5f); // Update the Message box with turn taker's name
                canvasCombat.panelPlayerInfo.GetComponent<SelectUnit>().SelectCurrentUnit(currentUnit);//Turn taker is highlighted and given access to Actions
            }
            else // No functionality for NPCs in combat yet so this condition just corresponds to it being the Enemy's turn
            {
                battleState = eBattleState.enemyTurn;
                Debug.Log(currentUnit.name + " going");
                currentUnit.GetComponent<EnemyUnitAction>().Attack();
            }
        }
        else NextTurn();
    }

    // Prevents enemies from being clicked/spammed
    public void EnemyButtonsInteractable(bool _enabled)
    {
        foreach (Enemy enemy in canvasCombat.enemyDisplay)
        {
            if (!enemy.GetComponent<UnitStats>().IsDead()) enemy.attackTarget.interactable = _enabled;
        }
    }

    //NOTE: Currently, all party members earn experience points regardless of whether they lived or died. 
    public IEnumerator CollectReward(bool _win)
    {
        if (_win)
        {   
            yield return StartCoroutine(canvasCombat.UpdateText("V I C T O R Y !", 3f));
            yield return null;
            List<UnitStats> aliveUnits = new();
            foreach (Player player in gm.party)
            {
                player.GetComponent<PlayerUnitAction>().BattleStart(false);
                if (!player.stats.IsDead()) aliveUnits.Add(player.stats);
            }
            float xpPerUnit = canvasCombat.currentEncounter.experience / (float) aliveUnits.Count;
            yield return StartCoroutine(canvasCombat.UpdateText("The party gained " + (int)xpPerUnit + " experience each!", 2f));
            foreach (UnitStats stats in aliveUnits)
            {
                stats.ReceiveExperience(xpPerUnit);
            }
            yield return null;
            gm.sceneLoader.LoadScene(eScene.travel);
        }
        else 
        {
            yield return StartCoroutine(canvasCombat.UpdateText("The party was defeated!", 4f));
            gm.sceneLoader.LoadScene(eScene.frontEnd);
            Destroy(gm.gameObject);
        }
    }

    // The Turn Queue. Pulls the first Action added to it out, enables the GameObject it lives on,
    // then runs Start() since we skipped Start() by having it disabled on initalization.
    // IsFinished defaults to true so it destroys itself and continues through the While loop
    public IEnumerator ExecuteActions()
    {
        Action currentAction = null;
        actionsMenu.SetActive(false);
        
        while (actionList.Count > 0)
        {
            currentAction = actionList.Dequeue(); // Queues up the first action to be resovled
            if ((!currentAction.ownerStats.IsDead())) 
            {
                //currentAction.gameObject.SetActive(true);
                currentAction.Init();
                yield return new WaitUntil(currentAction.isFinished);
            }
            Destroy(currentAction.gameObject);
            yield return null;
        }
        BeginRound(); // Once all actions have resolved, we start a new round
        
    }

   
}
