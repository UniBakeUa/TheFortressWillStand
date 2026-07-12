namespace Towers
{
    /// <summary>
    /// Контракт для будь-якого об'єкта, що періодично генерує дохід (Drill і майбутні
    /// подібні структури - наприклад "ферма" чи "склад"). Дозволяє UI/аналітиці
    /// перелічити всі джерела доходу на сцені через FindObjectsOfType&lt;MonoBehaviour&gt;()
    /// + фільтр по інтерфейсу, не знаючи наперед про конкретні класи.
    /// </summary>
    public interface IIncomeProducer
    {
        float IncomePerTick { get; }
        float TickInterval { get; }
    }
}   