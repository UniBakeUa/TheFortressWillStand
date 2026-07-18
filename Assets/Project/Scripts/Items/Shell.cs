using UI;
using UI.Factories;
using UnityEngine;

namespace Items
{
    public class Shell : ClickableItem
    {
        [Header("Visual Settings")]
        [SerializeField] private FloatingTextView _floatingTextPrefab;

        private FloatingTextFactory _floatingTextFactory;
        private void Awake()
        {
            _floatingTextFactory = new FloatingTextFactory(_floatingTextPrefab, transform.parent);
        }
        private void Start()
        {
            transform.rotation = new Quaternion(transform.rotation.x,
                                                transform.rotation.y,
                                                Random.Range(0f, 1f),
                                                transform.rotation.w);
        }

        protected override void OnMouseDown()
        {
            base.OnMouseDown();
            if (GameStateManager.Instance.CurrentState != GameState.Playing)
                return;
            _floatingTextFactory.SpawnText((int)moneyValue, transform.position + Vector3.up * 1.5f);
        }
    }
}