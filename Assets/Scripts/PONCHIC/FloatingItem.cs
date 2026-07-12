using UnityEngine;
using Money;

[RequireComponent(typeof(Rigidbody2D))]
public class FloatingItem : MonoBehaviour
{
    [SerializeField] private float minForce = 5f;
    [SerializeField] private float maxForce = 8f;
    [SerializeField] private float sideForceRange = 2f; // Наскільки сильно вбік
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private int moneyReward = 10;

    private Rigidbody2D _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();

        // 1. Надаємо випадковий імпульс (вгору + трішки вбік)
        float forceUp = Random.Range(minForce, maxForce);
        float forceSide = Random.Range(-sideForceRange, sideForceRange);
        
        _rb.AddForce(new Vector2(forceSide, forceUp), ForceMode2D.Impulse);

        // 2. Додаємо випадкове обертання
        _rb.angularVelocity = Random.Range(-rotationSpeed, rotationSpeed);

        // 3. Знищуємо об'єкт через 5 секунд (захист від "зависання" об'єктів)
        Destroy(gameObject, 5f);
    }

    void OnMouseDown()
    {
        MoneyManager.Instance.AddMoney(moneyReward);
        Destroy(gameObject);
    }
}