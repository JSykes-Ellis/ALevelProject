using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsDB
{
    public static void Init()
    {
        foreach(var kvp in Conditions)
        {
            var conditionId = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionId;
        }
    }

    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.psn,
            new Condition()
            {
                Name = "Poison",
                InflictMessage = "has been poisoned",
                OnTurnEnd = (Monster monster) =>
                {
                    monster.UpdateHP(monster.MaxHp / 8);
                    monster.StatusChanges.Enqueue($"{monster.BaseStats.Name} was hurt by its poison");
                }
            }
        },
        {
            ConditionID.brn,
            new Condition()
            {
                Name = "Burn",
                InflictMessage = "has been burnt",
                OnTurnEnd = (Monster monster) =>
                {
                    monster.UpdateHP(monster.MaxHp / 16);
                    monster.StatusChanges.Enqueue($"{monster.BaseStats.Name} was hurt by its burn");
                }
            }
        },
        {
            ConditionID.par,
            new Condition()
            {
                Name = "Paralysis",
                InflictMessage = "has been paralysed",
                OnBeforeMove = (Monster monster) =>
                {
                    if(Random.Range(1,5) == 1)
                    {
                        monster.StatusChanges.Enqueue($"{monster.BaseStats.Name} is paralysed and can't move");
                        return false;
                    }
                    return true;
                }
            }
        },
        {
            ConditionID.frz,
            new Condition()
            {
                Name = "Freeze",
                InflictMessage = "has been frozen",
                OnBeforeMove = (Monster monster) =>
                {
                    if(Random.Range(1,5) == 1)
                    {
                        monster.CureStatus();
                        monster.StatusChanges.Enqueue($"{monster.BaseStats.Name} thawed out");
                        return true;
                    }
                    return false;
                }
            }
        },
        {
            ConditionID.slp,
            new Condition()
            {
                Name = "Sleep",
                InflictMessage = "has fallen asleep",
                OnStart = (Monster monster) =>
                {
                    // sleep for 1-3 turns
                    monster.StatusTime = Random.Range(1, 4);
                },
                OnBeforeMove = (Monster monster) =>
                {
                    if (monster.StatusTime <= 0)
                    {
                        monster.CureStatus();
                        monster.StatusChanges.Enqueue($"{monster.BaseStats.Name} woke up");
                        return true;
                    }

                    monster.StatusTime--;
                    monster.StatusChanges.Enqueue($"{monster.BaseStats.Name} is asleep");
                    return false;
                }
            }
        },

        //Volatile conditions
        {
            ConditionID.confusion,
            new Condition()
            {
                Name = "Confusion",
                InflictMessage = "is confused",
                OnStart = (Monster monster) =>
                {
                    // Confused for 1-4 turns
                    monster.VolatileStatusTime = Random.Range(1, 5);
                },
                OnBeforeMove = (Monster monster) =>
                {
                    if (monster.VolatileStatusTime <= 0)
                    {
                        monster.CureVolatileStatus();
                        monster.StatusChanges.Enqueue($"{monster.BaseStats.Name} snapped out of it's confusion");
                        return true;
                    }

                    monster.VolatileStatusTime--;

                    //50% chance to move
                    if(Random.Range(1,3) == 1)
                    {
                        return true;
                    }
                    //Hurt by confusion
                    monster.StatusChanges.Enqueue($"{monster.BaseStats.Name} is confused");
                    monster.UpdateHP(monster.MaxHp / 8);
                    monster.StatusChanges.Enqueue($"{monster.BaseStats.Name} hurt itself in it's confusion");
                    return false;
                }
            }
        }
    };

    public static float GetStatusBonus(Condition condition)
    {
        if (condition == null)
        {
            return 1f;
        }
        else if (condition.Id == ConditionID.slp || condition.Id == ConditionID.frz)
        {
            return 2f;
        }
        else if (condition.Id == ConditionID.psn || condition.Id == ConditionID.par || condition.Id == ConditionID.brn)
        {
            return 1.5f;
        }
        else
        {
            return 1f;
        }
    }
}

public enum ConditionID
{
    none,
    psn,
    brn,
    slp,
    par,
    frz,
    confusion
}
