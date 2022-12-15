using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Monster
{
    [SerializeField] MonsterBase _base;
    [SerializeField] int level;

    public Monster(MonsterBase mBase, int mLevel)
    {
        _base = mBase;
        level = mLevel;

        Init();
    }

    public MonsterBase BaseStats
    {
        get
        {
            return _base;
        }
    }
    public int Level
    {
        get
        {
            return level;
        }
    }

    public int currentHP { get; set; }

    public List<Move> Moves { get; set; }
    public Move CurrentMove { get; set; }
    public Dictionary<Stat, int> Stats { get; private set; }
    public Dictionary<Stat, int> StatBoosts { get; private set; }
    public Condition Status { get; private set; }
    public int StatusTime { get; set; }
    public Condition VolatileStatus { get; private set; }
    public int VolatileStatusTime { get; set; }

    public Queue<string> StatusChanges { get; private set; }
    public bool HpChanged { get; set; }
    public event System.Action OnStatusChanged;

    public void Init()
    {
        //generate moves
        Moves = new List<Move>();
        foreach (var move in BaseStats.LearnableMoves)
        {
            if (move.Level <= Level)
            {
                Moves.Add(new Move(move.BaseStats));
            }
            if (Moves.Count >= 4)
            {
                break;
            }
        }

        StatCalculations();
        currentHP = MaxHp;

        StatusChanges = new Queue<string>();
        ResetStatBoost();
        Status = null;
        VolatileStatus = null;
    }

    void StatCalculations()
    {
        Stats = new Dictionary<Stat, int>();
        Stats.Add(Stat.Attack, Mathf.FloorToInt((BaseStats.Attack * Level) / 100f) + 5);
        Stats.Add(Stat.SpAttack, Mathf.FloorToInt((BaseStats.SpAttack * Level) / 100f) + 5);
        Stats.Add(Stat.Defense, Mathf.FloorToInt((BaseStats.Defense * Level) / 100f) + 5);
        Stats.Add(Stat.SpDefense, Mathf.FloorToInt((BaseStats.SpDefense * Level) / 100f) + 5);
        Stats.Add(Stat.Speed, Mathf.FloorToInt((BaseStats.Speed * Level) / 100f) + 5);

        MaxHp = Mathf.FloorToInt((BaseStats.MaxHp * Level) / 100f) + 10 + Level;
    }

    void ResetStatBoost()
    {
        StatBoosts = new Dictionary<Stat, int>()
        {
            {Stat.Attack, 0 },
            {Stat.SpAttack, 0 },
            {Stat.Defense, 0 },
            {Stat.SpDefense, 0 },
            {Stat.Speed, 0 },
            {Stat.Accuracy, 0 },
            {Stat.Evasion, 0 }
        };
    }

    int GetStat(Stat stat)
    {
        int statVal = Stats[stat];

        //APPLY STAT BOOST
        int boost = StatBoosts[stat];
        var boostValues = new float[] { 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f };

        if (boost >= 0)
        {
            statVal = Mathf.FloorToInt(statVal * boostValues[boost]);
        }
        else
        {
            statVal = Mathf.FloorToInt(statVal / boostValues[-boost]);
        }

        return statVal;
    }

    public void ApplyBoosts(List<StatBoost> statBoosts)
    {
        foreach (var statBoost in statBoosts)
        {
            var stat = statBoost.stat;
            var boost = statBoost.boost;

            StatBoosts[stat] = Mathf.Clamp(StatBoosts[stat] + boost, -6, 6);

            if(boost > 0)
            {
                StatusChanges.Enqueue($"{BaseStats.Name}'s {stat} rose!");
            }
            else
            {
                StatusChanges.Enqueue($"{BaseStats.Name}'s {stat} fell!");
            }
        }
    }

    public int Attack
    {
        get { return GetStat(Stat.Attack); }
    }

    public int SpAttack
    {
        get { return GetStat(Stat.SpAttack); }
    }

    public int Defense
    {
        get { return GetStat(Stat.Defense); }
    }

    public int SpDefense
    {
        get { return GetStat(Stat.SpDefense); }
    }

    public int Speed
    {
        get { return GetStat(Stat.Speed); }
    }

    public int MaxHp { get; private set; }


    public DamageDetails TakeDamage(Move move, Monster attacker)
    {
        float critical = 1f;
        if (Random.value * 100f <= 6.25f)
        {
            critical = 2f;
        }

        float type = TypeChart.GetEffectiveness(move.BaseStats.Type, this.BaseStats.Type1) * TypeChart.GetEffectiveness(move.BaseStats.Type, this.BaseStats.Type2);

        var damageDetails = new DamageDetails()
        {
            TypeEffectiveness = type,
            Critical = critical,
            Fainted = false
        };

        float attack = (move.BaseStats.Category == MoveCategory.Special) ? attacker.SpAttack : attacker.Attack;
        float defense = (move.BaseStats.Category == MoveCategory.Special) ? SpDefense : Defense;

        float modifiers = Random.Range(0.85f, 1f) * type * critical;
        float a = (2 * attacker.Level + 10) / 250f;
        float d = a * move.BaseStats.Power * ((float)attack / defense) + 2;
        int damage = Mathf.FloorToInt(d * modifiers);

        UpdateHP(damage);

        return damageDetails;
    }

    public void UpdateHP(int damage)
    {
        currentHP = Mathf.Clamp(currentHP - damage, 0, MaxHp);
        HpChanged = true;
    }

    public void SetStatus(ConditionID conditionId)
    {
        if (Status != null) return;

        Status = ConditionsDB.Conditions[conditionId];
        Status?.OnStart?.Invoke(this);
        StatusChanges.Enqueue($"{BaseStats.Name} {Status.InflictMessage}");
        OnStatusChanged?.Invoke();
    }

    public void CureStatus()
    {
        Status = null;
        OnStatusChanged?.Invoke();
    }

    public void SetVolatileStatus(ConditionID conditionId)
    {
        if (VolatileStatus != null) return;

        VolatileStatus = ConditionsDB.Conditions[conditionId];
        VolatileStatus?.OnStart?.Invoke(this);
        StatusChanges.Enqueue($"{BaseStats.Name} {VolatileStatus.InflictMessage}");
    }

    public void CureVolatileStatus()
    {
        VolatileStatus = null;
    }

    public Move GetRandomMove()
    {
        var movesWithPP = Moves.Where(x => x.PP > 0).ToList();

        int r = Random.Range(0, movesWithPP.Count);
        return movesWithPP[r];
    }

    public bool OnBeforeMove()
    {
        bool canMove = true;
        if(Status?.OnBeforeMove != null)
        {
            if (!Status.OnBeforeMove(this))
            {
                canMove = false;
            }
        }

        if (VolatileStatus?.OnBeforeMove != null)
        {
            if (!VolatileStatus.OnBeforeMove(this))
            {
                canMove = false;
            }
        }

        return canMove;
    }

    public void OnTurnEnd()
    {
        Status?.OnTurnEnd?.Invoke(this);
        VolatileStatus?.OnTurnEnd?.Invoke(this);
    }

    public void OnBattleOver()
    {
        VolatileStatus = null;
        ResetStatBoost();
    }
}

public class DamageDetails
{
    public bool Fainted { get; set; }
    public float Critical { get; set; }
    public float TypeEffectiveness { get; set; }
}