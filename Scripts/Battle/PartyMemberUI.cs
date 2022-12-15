using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar HPBar;

    [SerializeField] Color selectionColour;

    Monster _monster;

    public void SetData(Monster monster)
    {
        _monster = monster;

        nameText.text = monster.BaseStats.Name;
        levelText.text = "Lvl" + monster.Level;
        HPBar.SetHP((float)monster.currentHP / monster.MaxHp);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            nameText.color = selectionColour;
        }
        else
        {
            nameText.color = Color.black;
        }
    }
}
