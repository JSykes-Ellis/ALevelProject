using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterParty : MonoBehaviour
{
    [SerializeField] List<Monster> monsters;

    public List<Monster> Monsters
    {
        get
        {
            return monsters;
        }
    }

    private void Start()
    {
        foreach (var monster in monsters)
        {
            monster.Init();
        }
    }

    public Monster GetUsableMonster()
    {
        return monsters.Where(x => x.currentHP > 0).FirstOrDefault();
    }

    public void AddMonster(Monster newMonster)
    {
        if(monsters.Count < 6)
        {
            monsters.Add(newMonster);
        }
        else
        {
            //transfer to storage
        }
    }
}
