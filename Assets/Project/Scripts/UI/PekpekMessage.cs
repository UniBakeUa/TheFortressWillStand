using UnityEngine;
using DG.Tweening;

public class PekpekMessage : MonoBehaviour
{
    [SerializeField] private float _duration;
    private void Start()
    {
        transform.localScale = Vector2.zero;
    }
    public void FoldIn()
    {
        transform.DOScale(0, _duration / 2).SetEase(Ease.InSine);
        transform.DORotate(new Vector3(0, 0, 90), _duration / 2).SetEase(Ease.InSine);
    }
    public void Unfold()
    {
        transform.DOScale(1, _duration).SetEase(Ease.OutBack);
        transform.DORotate(Vector3.zero, _duration).SetEase(Ease.OutBack);
    }
}
