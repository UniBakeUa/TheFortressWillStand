using System.Collections.Generic;
using UnityEngine;

namespace Towers.ScriptableObjects
{
    /// <summary>
    /// Усі картки прокачки, що є в грі. PerkManager бере звідси пул для добору.
    /// </summary>
    [CreateAssetMenu(fileName = "PerkLibrary", menuName = "Upgrades/Perk Library")]
    public class PerkLibrary : ScriptableObject
    {
        [Tooltip("Додай сюди всі PerkConfig")]
        [SerializeField] private List<PerkConfig> _perks = new();

        public IReadOnlyList<PerkConfig> Perks => _perks;

        public PerkConfig GetById(int id)
        {
            foreach (var perk in _perks)
            {
                if (perk != null && perk.Id == id) return perk;
            }
            return null;
        }

        private void OnValidate()
        {
            // Копіювання картки в редакторі лишає той самий Id, а це ламає все,
            // що шукає перк за Id (GetById). Сигналимо одразу.
            var seenIds = new Dictionary<int, PerkConfig>();
            var seenAssets = new HashSet<PerkConfig>();

            foreach (var perk in _perks)
            {
                if (perk == null) continue;

                if (!seenAssets.Add(perk))
                {
                    Debug.LogWarning($"[PerkLibrary] Картку '{perk.name}' додано в список двічі.", this);
                    continue;
                }

                if (seenIds.TryGetValue(perk.Id, out PerkConfig other))
                {
                    Debug.LogWarning(
                        $"[PerkLibrary] Дублікат Id={perk.Id}: '{other.name}' і '{perk.name}'. " +
                        "Id мають бути унікальними.", this);
                    continue;
                }

                seenIds.Add(perk.Id, perk);
            }
        }
    }
}
