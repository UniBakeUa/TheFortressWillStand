namespace Towers
{
    /// <summary>
    /// Контракт для будь-якого об'єкта фортифікації, що має HP і може бути зруйнований.
    /// Дозволяє іншим системам (UI здоров'я, ефекти, аналітика) працювати з Tower/Wall/Drill
    /// та БУДЬ-ЯКИМ майбутнім типом однаково, не знаючи про конкретний клас.
    /// </summary>
    public interface IDamageable
    {
        float CurrentHP { get; }
        float MaxHP { get; }
        float ExposureFraction { get; }

        void TakeDamage(float amount);
        void Repair(float amount);
        void Collapse();
    }
}