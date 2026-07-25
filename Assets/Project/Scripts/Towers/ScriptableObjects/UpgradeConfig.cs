using UnityEngine;

namespace Towers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewUpgradeConfig", menuName = "Upgrades/Upgrade Config")]
    public class UpgradeConfig : ProductConfig
    {
        [Tooltip("На скільки відсотків збільшується ціна після покупки поліпшення.")]
        [field: SerializeField, Range(0f, 100f)] public float priceChangePercentage { get; private set; }
        [Tooltip("На скільки сильно поліпшення працює у відсотках.")]
        [field: SerializeField] public float amountOfInfluence { get; private set; }
    }
}
