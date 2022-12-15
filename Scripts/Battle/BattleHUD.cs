using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] Text statusText;
    [SerializeField] HPBar HPBar;

    [SerializeField] Color psnColour;
    [SerializeField] Color brnColour;
    [SerializeField] Color slpColour;
    [SerializeField] Color parColour;
    [SerializeField] Color frzColour;

    Monster _monster;
    Dictionary<ConditionID, Color> statusColours;

    public void SetData(Monster monster)
    {
        _monster = monster;

        nameText.text = monster.BaseStats.Name;
        levelText.text = "Lvl" + monster.Level;
        HPBar.SetHP((float)monster.currentHP / monster.MaxHp);

        statusColours = new Dictionary<ConditionID, Color>()
        {
            {ConditionID.psn, psnColour },
            {ConditionID.brn, brnColour },
            {ConditionID.slp, slpColour },
            {ConditionID.par, parColour },
            {ConditionID.frz, frzColour }
        };

        SetStatusText();
        _monster.OnStatusChanged += SetStatusText;
    }

    public void SetStatusText()
    {
        if (_monster.Status == null)
        {
            statusText.text = "";
        }
        else
        {
            statusText.text = _monster.Status.Id.ToString().ToUpper();
            statusText.color = statusColours[_monster.Status.Id];
        }
    }

    public IEnumerator UpdateHP()
    {
        if (_monster.HpChanged)
        {
            yield return HPBar.SetHPAnimated((float)_monster.currentHP / _monster.MaxHp);
            _monster.HpChanged = false;
        }
    }
}
