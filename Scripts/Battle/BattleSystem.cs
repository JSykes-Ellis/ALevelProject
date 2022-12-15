using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState
{
    Start,
    ActionSelection,
    MoveSelection,
    PerformingTurn,
    Busy,
    PartyScreen,
    BattleOver,
    AboutToUse
}

public enum BattleAction
{
    Move,
    SwitchMonster,
    UseItem,
    Run
}

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogueBox dialogueBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject cageSprite;

    BattleState state;
    BattleState? previousState;
    int currentAction;
    int currentMove;
    int currentMember;
    bool aboutToUseChoice = true;

    public event Action<bool> OnBattleOver;

    MonsterParty playerParty;
    MonsterParty trainerParty;
    Monster wildMonster;

    bool isTrainerBattle = false;
    PlayerController player;
    TrainerController trainer;

    public void StartRandomEncounterBattle(MonsterParty playerParty, Monster wildMonster)
    {
        this.playerParty = playerParty;
        this.wildMonster = wildMonster;
        player = playerParty.GetComponent<PlayerController>();

        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(MonsterParty playerParty, MonsterParty trainerParty)
    {
        this.playerParty = playerParty;
        this.trainerParty = trainerParty;

        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        playerUnit.Clear();
        enemyUnit.Clear();

        if (!isTrainerBattle)
        {
            //wild battle
            playerUnit.Setup(playerParty.GetUsableMonster());
            enemyUnit.Setup(wildMonster);

            dialogueBox.SetMoveNames(playerUnit.Monster.Moves);
            yield return dialogueBox.AnimateDialogue($"An enraged {enemyUnit.Monster.BaseStats.Name} attacked");
        }
        else
        {
            //trainer battle
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);

            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);
            playerImage.sprite = player.sprite;
            trainerImage.sprite = trainer.sprite;

            yield return dialogueBox.AnimateDialogue($"{trainer.Name} challenged you to a battle");

            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var enemyMonster = trainerParty.GetUsableMonster();
            enemyUnit.Setup(enemyMonster);
            yield return dialogueBox.AnimateDialogue($"{trainer.Name} sent out {enemyMonster.BaseStats.Name}");

            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerMonster = playerParty.GetUsableMonster();
            playerUnit.Setup(playerMonster);
            yield return dialogueBox.AnimateDialogue($"You sent out {playerMonster.BaseStats.Name}");
            dialogueBox.SetMoveNames(playerUnit.Monster.Moves);
        }

        partyScreen.Init();
        ActionSelection();
    }

    void BattleOver(bool won)
    {
        isTrainerBattle = false;
        state = BattleState.BattleOver;
        playerParty.Monsters.ForEach(m => m.OnBattleOver());
        OnBattleOver(won);
    }

    void ActionSelection()
    {
        state = BattleState.ActionSelection;
        dialogueBox.SetDialogue("Choose an action");
        dialogueBox.EnableActionSelector(true);
    }

    void OpenPartyScreen()
    {
        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Monsters);
        partyScreen.gameObject.SetActive(true);
    }

    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogueBox.EnableActionSelector(false);
        dialogueBox.EnableDialogueText(false);
        dialogueBox.EnableMoveSelector(true);
    }

    IEnumerator AboutToUse(Monster newMonster)
    {
        state = BattleState.Busy;
        yield return dialogueBox.AnimateDialogue($"{trainer.Name} is about to send out {newMonster.BaseStats.Name}. Do you want to switch your monster?");
        state = BattleState.AboutToUse;
        dialogueBox.EnableChoiceBox(true);
    }

    IEnumerator PerformTurns(BattleAction playerAction)
    {
        state = BattleState.PerformingTurn;

        if (playerAction == BattleAction.Move)
        {
            playerUnit.Monster.CurrentMove = playerUnit.Monster.Moves[currentMove];
            enemyUnit.Monster.CurrentMove = enemyUnit.Monster.GetRandomMove();

            int playerMovePriorty = playerUnit.Monster.CurrentMove.BaseStats.Priority;
            int enemyMovePriorty = enemyUnit.Monster.CurrentMove.BaseStats.Priority;

            //check who goes first
            bool playersMoveFirst = true;
            if (enemyMovePriorty > playerMovePriorty)
            {
                playersMoveFirst = false;
            }
            else if (enemyMovePriorty == playerMovePriorty)
            {
                playersMoveFirst = playerUnit.Monster.Speed > enemyUnit.Monster.Speed;
            }


            var firstUnit = (playersMoveFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playersMoveFirst) ? enemyUnit : playerUnit;

            var secondMonster = secondUnit.Monster;

            //First turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Monster.CurrentMove);
            yield return RunTurnEnd(firstUnit);
            if (state == BattleState.BattleOver) yield break;

            if (secondMonster.currentHP > 0)
            {
                //Second turn
                yield return RunMove(secondUnit, firstUnit, secondUnit.Monster.CurrentMove);
                yield return RunTurnEnd(secondUnit);
                if (state == BattleState.BattleOver) yield break;
            }
        }
        else
        {
            if(playerAction == BattleAction.SwitchMonster)
            {
                var selectedMonster = playerParty.Monsters[currentMember];
                state = BattleState.Busy;
                yield return SwitchMonster(selectedMonster);
            }
            else if(playerAction == BattleAction.UseItem)
            {
                dialogueBox.EnableActionSelector(false);
                yield return UseCage();
            }

            //Enemy Turn
            var enemyMove = enemyUnit.Monster.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunTurnEnd(enemyUnit);
            if (state == BattleState.BattleOver) yield break;
        }

        if (state != BattleState.BattleOver)
        {
            ActionSelection();
        }
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canMove = sourceUnit.Monster.OnBeforeMove();
        if (!canMove)
        {
            yield return DisplayStatusChanges(sourceUnit.Monster);
            yield return sourceUnit.Hud.UpdateHP();
            yield break;
        }
        yield return DisplayStatusChanges(sourceUnit.Monster);

        move.PP--;
        yield return dialogueBox.AnimateDialogue($"{sourceUnit.Monster.BaseStats.name} used {move.BaseStats.Name}");

        if (CheckForSuccessfulHit(move, sourceUnit.Monster, targetUnit.Monster))
        {
            sourceUnit.AttackAnimation();
            yield return new WaitForSeconds(1f);
            targetUnit.HitAnimation();

            if (move.BaseStats.Category == MoveCategory.Status)
            {
                yield return CarryOutMoveEffects(move.BaseStats.Effects, sourceUnit.Monster, targetUnit.Monster, move.BaseStats.Target);
            }
            else
            {
                var damageDetails = targetUnit.Monster.TakeDamage(move, sourceUnit.Monster);
                yield return targetUnit.Hud.UpdateHP();
                yield return ShowDamageDetails(damageDetails);
            }

            if(move.BaseStats.SecondaryEffects != null && move.BaseStats.SecondaryEffects.Count > 0 && targetUnit.Monster.currentHP > 0)
            {
                foreach(var secondaryEffect in move.BaseStats.SecondaryEffects)
                {
                    var r = UnityEngine.Random.Range(1, 101);
                    if(r <= secondaryEffect.Chance)
                    {
                        yield return CarryOutMoveEffects(secondaryEffect, sourceUnit.Monster, targetUnit.Monster, secondaryEffect.Target);
                    }
                }
            }

            if (targetUnit.Monster.currentHP <= 0)
            {
                yield return dialogueBox.AnimateDialogue($"{targetUnit.Monster.BaseStats.Name} fainted");
                targetUnit.FaintAnimation();
                yield return new WaitForSeconds(2f);

                CheckForBattleOver(targetUnit);
            }
        }
        else
        {
            yield return dialogueBox.AnimateDialogue($"{sourceUnit.Monster.BaseStats.Name}'s attack missed");
        }
    }

    bool CheckForSuccessfulHit(Move move, Monster source, Monster target)
    {
        if (move.BaseStats.NeverMiss)
        {
            return true;
        }

        float moveAccuracy = move.BaseStats.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = source.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if (accuracy > 0)
        {
            moveAccuracy *= boostValues[accuracy];
        }
        else
        {
            moveAccuracy /= boostValues[-accuracy];
        }

        if (evasion > 0)
        {
            moveAccuracy /= boostValues[evasion];
        }
        else
        {
            moveAccuracy *= boostValues[-evasion];
        }

        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    IEnumerator CarryOutMoveEffects(MoveEffects effects, Monster source, Monster target, MoveTarget moveTarget)
    {
        //start boosting
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
            {
                source.ApplyBoosts(effects.Boosts);
            }
            else
            {
                target.ApplyBoosts(effects.Boosts);
            }
        }

        //status condition
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        //volatile status condition
        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return DisplayStatusChanges(source);
        yield return DisplayStatusChanges(target);
    }

    IEnumerator RunTurnEnd(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(() => state == BattleState.PerformingTurn);
        //psn and brn statuses damage the monster after the turn ends
        sourceUnit.Monster.OnTurnEnd();
        yield return DisplayStatusChanges(sourceUnit.Monster);
        yield return sourceUnit.Hud.UpdateHP();
        if (sourceUnit.Monster.currentHP <= 0)
        {
            yield return dialogueBox.AnimateDialogue($"{sourceUnit.Monster.BaseStats.Name} fainted");
            sourceUnit.FaintAnimation();
            yield return new WaitForSeconds(2f);

            CheckForBattleOver(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.PerformingTurn);
        }
    }

    IEnumerator DisplayStatusChanges(Monster monster)
    {
        while(monster.StatusChanges.Count > 0)
        {
            var message = monster.StatusChanges.Dequeue();
            yield return dialogueBox.AnimateDialogue(message);
        }
    }

    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextMonster = playerParty.GetUsableMonster();

            if (nextMonster != null)
            {
                OpenPartyScreen();
            }
            else
            {
                BattleOver(false);
            }
        }
        else
        {
            if (!isTrainerBattle)
            {
                BattleOver(true);
            }
            else
            {
                var nextMonster = trainerParty.GetUsableMonster();
                if(nextMonster != null)
                {
                    StartCoroutine(AboutToUse(nextMonster));
                }
                else
                {
                    BattleOver(true);
                }
            }
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
        {
            yield return dialogueBox.AnimateDialogue("A critical hit!");
        }
        if (damageDetails.TypeEffectiveness > 1f)
        {
            yield return dialogueBox.AnimateDialogue("It's super effective!");
        }
        if (damageDetails.TypeEffectiveness == 0f)
        {
            yield return dialogueBox.AnimateDialogue("The attack had no effect!");
        }
        else if (damageDetails.TypeEffectiveness < 1f)
        {
            yield return dialogueBox.AnimateDialogue("It's not very effective!");
        }
    }

    public void HandleUpdate()
    {
        if (state == BattleState.ActionSelection)
        {
            ManageActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        {
            ManageMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            ManagePartyScreenSelection();
        }
        else if (state == BattleState.AboutToUse)
        {
            ManageAboutToUse();
        }
    }

    void ManageActionSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ++currentAction;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            --currentAction;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentAction += 2;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentAction -= 2;
        }

        currentAction = Mathf.Clamp(currentAction, 0, 3);

        dialogueBox.UpdateActionSelection(currentAction);

        if (Input.GetButtonDown("interact"))
        {
            if (currentAction == 0)
            {
                //Fight
                MoveSelection();

            }
            else if (currentAction == 1)
            {
                //Bag
                StartCoroutine(PerformTurns(BattleAction.UseItem));
            }
            else if (currentAction == 2)
            {
                //Monsters
                previousState = state;
                OpenPartyScreen();
            }
            else if (currentAction == 3)
            {
                //Run
            }
        }
    }

    void ManageMoveSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ++currentMove;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            --currentMove;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentMove += 2;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentMove -= 2;
        }

        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Monster.Moves.Count -1);

        dialogueBox.UpdateMoveSelection(currentMove, playerUnit.Monster.Moves[currentMove]);

        if (Input.GetButtonDown("interact"))
        {
            var move = playerUnit.Monster.Moves[currentMove];
            if (move.PP == 0) return;

            dialogueBox.EnableMoveSelector(false);
            dialogueBox.EnableDialogueText(true);
            StartCoroutine(PerformTurns(BattleAction.Move));
        }

        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogueBox.EnableMoveSelector(false);
            dialogueBox.EnableDialogueText(true);
            ActionSelection();
        }
    }

    void ManagePartyScreenSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ++currentMember;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            --currentMember;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentMember += 2;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentMember -= 2;
        }

        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Monsters.Count - 1);

        partyScreen.UpdateMemberSelection(currentMember);

        if (Input.GetButtonDown("interact"))
        {
            var selectedMember = playerParty.Monsters[currentMember];
            if (selectedMember.currentHP <= 0)
            {
                partyScreen.SetMessageText("This monster has fainted and is unfit to battle");
                return;
            }
            if (selectedMember == playerUnit.Monster)
            {
                partyScreen.SetMessageText("This monster is already in battle");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (previousState == BattleState.ActionSelection)
            {
                previousState = null;
                StartCoroutine(PerformTurns(BattleAction.SwitchMonster));
            }
            else
            {
                state = BattleState.Busy;
                StartCoroutine(SwitchMonster(selectedMember));
            }
        }

        else if (Input.GetKeyDown(KeyCode.X))
        {
            if(playerUnit.Monster.currentHP <= 0)
            {
                partyScreen.SetMessageText("You must choose a monster to continue");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (previousState == BattleState.AboutToUse)
            {
                previousState = null;
                StartCoroutine(SwitchTrainerMonster());
            }
            else
            {
                ActionSelection();
            }
        }
    }

    void ManageAboutToUse()
    {
        if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            aboutToUseChoice = !aboutToUseChoice;
        }

        dialogueBox.UpdateChoiceBoxSelection(aboutToUseChoice);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            dialogueBox.EnableChoiceBox(false);
            if(aboutToUseChoice == true)
            {
                previousState = BattleState.AboutToUse;
                OpenPartyScreen();
            }
            else
            {
                StartCoroutine(SwitchTrainerMonster());
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogueBox.EnableChoiceBox(false);
            StartCoroutine(SwitchTrainerMonster());
        }
    }

    IEnumerator SwitchMonster(Monster newMonster)
    {
        if (playerUnit.Monster.currentHP > 0)
        {
            yield return dialogueBox.AnimateDialogue($"Come back {playerUnit.Monster.BaseStats.Name}");
            playerUnit.FaintAnimation();
            yield return new WaitForSeconds(2f);
        }

        playerUnit.Setup(newMonster);
        dialogueBox.SetMoveNames(newMonster.Moves);
        yield return dialogueBox.AnimateDialogue($"You sent out {newMonster.BaseStats.Name}");

        if (previousState == null)
        {
            state = BattleState.PerformingTurn;
        }
        else if (previousState == BattleState.AboutToUse)
        {
            previousState = null;
            StartCoroutine(SwitchTrainerMonster());
        }
    }

    IEnumerator SwitchTrainerMonster()
    {
        var nextMonster = trainerParty.GetUsableMonster();
        state = BattleState.Busy;

        enemyUnit.Setup(nextMonster);
        yield return dialogueBox.AnimateDialogue($"{trainer.Name} sent out {nextMonster.BaseStats.Name}");

        state = BattleState.PerformingTurn;
    }

    IEnumerator UseCage()
    {
        if (isTrainerBattle)
        {
            yield return dialogueBox.AnimateDialogue($"You can't steal another trainer's monster");
            state = BattleState.PerformingTurn;
            yield break;
        }
        state = BattleState.Busy;
        yield return dialogueBox.AnimateDialogue($"You used a cage");
        var cageObject = Instantiate(cageSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
        var cage = cageObject.GetComponent<SpriteRenderer>();

        yield return cage.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 2f, 1, 1f).WaitForCompletion();
        yield return enemyUnit.CaptureAnimation();
        yield return cage.transform.DOMoveY(enemyUnit.transform.position.y - 1.3f, 0.5f).WaitForCompletion();

        int shakeCount = TryCatchMonster(enemyUnit.Monster);

        for (int i=0; i<Mathf.Min(shakeCount, 3); ++i)
        {
            yield return new WaitForSeconds(0.5f);
            yield return cage.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.8f).WaitForCompletion();
        }

        if(shakeCount == 4)
        {
            yield return dialogueBox.AnimateDialogue($"{enemyUnit.Monster.BaseStats.Name} was caught in the cage");
            yield return cage.DOFade(0, 1.5f).WaitForCompletion();

            playerParty.AddMonster(enemyUnit.Monster);
            yield return dialogueBox.AnimateDialogue($"{enemyUnit.Monster.BaseStats.Name} has been added to the party");

            Destroy(cage);
            BattleOver(true);
        }
        else
        {
            yield return new WaitForSeconds(1f);
            cage.DOFade(0, 0.2f);
            yield return enemyUnit.BreakOutAnimation();

            if(shakeCount < 2)
            {
                yield return dialogueBox.AnimateDialogue($"{enemyUnit.Monster.BaseStats.Name} broke free");
            }
            else
            {
                yield return dialogueBox.AnimateDialogue($"{enemyUnit.Monster.BaseStats.Name} was almost caught");
            }

            Destroy(cage);
            state = BattleState.PerformingTurn;
        }
    }

    int TryCatchMonster(Monster monster)
    {
        float a = (3 * monster.MaxHp - 2 * monster.currentHP) * monster.BaseStats.CatchRate * ConditionsDB.GetStatusBonus(monster.Status) / (3 * monster.MaxHp);
        if (a>= 255)
        {
            return 4;
        }
        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));

        int shakeCount = 0;
        while (shakeCount < 4)
        {
            if (UnityEngine.Random.Range(0, 65535) >= b)
            {
                break;
            }

            ++shakeCount;
        }

        return shakeCount;
    }
}
