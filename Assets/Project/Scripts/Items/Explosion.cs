using DG.Tweening;
using System;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Explosion : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sr;
    private void OnEnable()
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one * 0.4f, 0.2f).OnComplete(()=>
        {
            _sr.DOFade(0, 2f).OnComplete(() => OnAnimationEnd());
        });
    }

    private void OnAnimationEnd()
    {
        Destroy(gameObject);
    }

}
