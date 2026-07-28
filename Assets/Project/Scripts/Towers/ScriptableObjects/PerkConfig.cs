using UnityEngine;

namespace Towers.ScriptableObjects
{
    /// <summary>
    /// Одна картка прокачки: як виглядає, що пише гравцю і що робить.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPerkConfig", menuName = "Upgrades/Perk Config")]
    public class PerkConfig : ScriptableObject
    {
        [Header("Ідентифікація")]
        [Tooltip("Унікальний ID у межах PerkLibrary")]
        [field: SerializeField] public int Id { get; private set; }

        [Header("Вигляд картки")]
        [field: SerializeField] public string DisplayName { get; private set; }
        [Tooltip("Спрайт-іконка на картці")]
        [field: SerializeField] public Sprite Icon { get; private set; }
        [Tooltip("Опис для гравця. {0} підставляє Amount, {1} - SecondaryAmount")]
        [field: SerializeField, TextArea(2, 4)] public string Description { get; private set; }
        [Tooltip("Колір рамки/підсвітки картки")]
        [field: SerializeField] public Color Tint { get; private set; } = Color.white;

        [Header("Ефект")]
        [field: SerializeField] public PerkEffectType EffectType { get; private set; }
        [Tooltip("Головне число: дамаг, частка дальності (0.5 = +50%), HP ремонту, кількість пончиків")]
        [field: SerializeField] public float Amount { get; private set; } = 1f;
        [Tooltip("Друге число: інтервал у секундах для періодичних ефектів (ремонт, німцеріз)")]
        [field: SerializeField] public float SecondaryAmount { get; private set; } = 5f;

        [Header("Правила випадання")]
        [Tooltip("Скільки копій цієї картки кладеться в колоду на старті гри. " +
                 "1 = унікальна, трапиться щонайбільше раз за гру; 3 = можна взяти тричі")]
        [field: SerializeField, Min(1)] public int CopiesInDeck { get; private set; } = 1;
        [Tooltip("З якої хвилі картка може випасти")]
        [field: SerializeField] public int RequiredWave { get; private set; }

        /// <summary>Опис із підставленими числами - те, що бачить гравець на картці.</summary>
        public string GetFormattedDescription()
        {
            if (string.IsNullOrEmpty(Description)) return string.Empty;

            // Опис може не містити плейсхолдерів взагалі - тоді Format поверне
            // його як є. Некоректний формат не має ламати відкриття панелі.
            try
            {
                return string.Format(Description, Amount, SecondaryAmount);
            }
            catch (System.FormatException)
            {
                Debug.LogWarning($"[PerkConfig] Некоректний формат опису в '{name}'. Дозволені плейсхолдери: {{0}}, {{1}}.");
                return Description;
            }
        }
    }
}
