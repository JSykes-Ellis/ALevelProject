using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BattleUnit : MonoBehaviour
{
    [SerializeField] bool isPlayerUnit;
    [SerializeField] BattleHUD hud;

    public bool IsPlayerUnit
    {
        get { return isPlayerUnit; }
    }

    public BattleHUD Hud
    {
        get { return hud; }
    }

    public Monster Monster { get; set; }

    Image image;
    Vector3 originalPosition;
    Color originalColour;

    private void Awake()
    {
        image = GetComponent<Image>();
        originalPosition = image.transform.localPosition;
        originalColour = image.color;
    }

    public void Setup(Monster monster)
    {
        Monster =  monster;
        if (isPlayerUnit)
        {
            image.sprite = Monster.BaseStats.Back;
        }
        else
        {
            image.sprite = Monster.BaseStats.Front;
        }

        hud.gameObject.SetActive(true);
        hud.SetData(monster);

        transform.localScale = new Vector3(1, 1, 1);
        image.color = originalColour;
        EnterAnimation();
    }

    public void Clear()
    {
        hud.gameObject.SetActive(false);
    }

    public void EnterAnimation()
    {
        if (isPlayerUnit)
        {
            image.transform.localPosition = new Vector3(-500f, originalPosition.y);
        }
        else
        {
            image.transform.localPosition = new Vector3(500f, originalPosition.y);
        }

        image.transform.DOLocalMoveX(originalPosition.x, 1f);
    }

    public void AttackAnimation()
    {
        var sequence = DOTween.Sequence();
        if (isPlayerUnit)
        {
            sequence.Append(image.transform.DOLocalMoveX(originalPosition.x + 50f, 0.25f));
        }
        else
        {
            sequence.Append(image.transform.DOLocalMoveX(originalPosition.x - 50f, 0.25f));
        }

        sequence.Append(image.transform.DOLocalMoveX(originalPosition.x, 0.25f));
    }

    public void HitAnimation()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(image.DOColor(Color.gray, 0.1f));
        sequence.Append(image.DOColor(originalColour, 0.1f));
    }

    public void FaintAnimation()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(image.transform.DOLocalMoveY(originalPosition.y - 150f, 0.5f));
        sequence.Join(image.DOFade(0f, 0.5f));
    }

    public IEnumerator CaptureAnimation()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(image.DOFade(0, 0.5f));
        sequence.Join(transform.DOLocalMoveY(originalPosition.y + 50f, 0.5f));
        sequence.Join(transform.DOScale(new Vector3(0.3f, 0.3f, 1f), 0.5f));
        yield return sequence.WaitForCompletion();
    }
    public IEnumerator BreakOutAnimation()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(image.DOFade(1, 0.5f));
        sequence.Join(transform.DOLocalMoveY(originalPosition.y, 0.5f));
        sequence.Join(transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f));
        yield return sequence.WaitForCompletion();
    }
}
