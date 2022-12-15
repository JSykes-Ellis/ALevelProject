using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPBar : MonoBehaviour
{
    [SerializeField] GameObject health;

    public void SetHP(float hp)
    {
        health.transform.localScale = new Vector3(hp, 1f);
    }

    public IEnumerator SetHPAnimated(float newHp)
    {
        float currentHP = health.transform.localScale.x;
        float changeAmount = currentHP - newHp;

        while (currentHP - newHp > Mathf.Epsilon)
        {
            currentHP -= changeAmount * Time.deltaTime;
            health.transform.localScale = new Vector3(currentHP, 1f);
            yield return null;
        }
        health.transform.localScale = new Vector3(newHp, 1f);
    }
}
