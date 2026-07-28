using System.Collections.Generic;
using Managers;
using Towers.ScriptableObjects;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Показує іконки всіх зібраних прокачок.
    ///
    /// Розкладкою тут займається Grid Layout Group на контейнері - на відміну від
    /// карток вибору, іконки нікуди не літають по своїх координатах, тож груп-
    /// компонент нічому не заважає.
    /// </summary>
    public class ActivePerksView : MonoBehaviour
    {
        [Header("Грід")]
        [SerializeField] private ActivePerkIcon _iconPrefab;
        [Tooltip("Контейнер з Grid Layout Group, куди спавняться іконки")]
        [SerializeField] private Transform _gridContainer;

        [Header("Порожній стан")]
        [Tooltip("Необов'язково: ховається, щойно з'явилась перша прокачка")]
        [SerializeField] private GameObject _emptyHint;

        // Іконка на кожен унікальний перк.
        private readonly Dictionary<PerkConfig, ActivePerkIcon> _icons = new();

        private void Start()
        {
            if (PerkManager.Instance == null) return;

            PerkManager.Instance.OnPerkTaken += HandlePerkTaken;

            // Перки могли бути взяті ще до того, як в'юшка увімкнулась.
            RebuildAll();
        }

        private void HandlePerkTaken(PerkConfig perk)
        {
            if (perk == null) return;

            // Список щоразу збирається заново з даних PerkManager - так у гріді
            // не може лишитись жодної застарілої чи дубльованої іконки.
            RebuildAll();

            // Підсвічуємо саме той перк, який щойно взяли.
            if (_icons.TryGetValue(perk, out ActivePerkIcon icon) && icon != null)
            {
                if (PerkManager.Instance.GetStacks(perk) > 1)
                    icon.PlayStackPulse();
                else
                    icon.PlayAppear();
            }
        }

        /// <summary>Збирає список заново з поточного стану PerkManager.</summary>
        public void RebuildAll()
        {
            ClearIcons();

            if (PerkManager.Instance == null) return;

            foreach (var perk in PerkManager.Instance.TakenPerks)
            {
                if (perk == null) continue;

                SpawnIcon(perk, PerkManager.Instance.GetStacks(perk));
            }

            UpdateEmptyHint();
        }

        private ActivePerkIcon SpawnIcon(PerkConfig perk, int stacks)
        {
            if (_iconPrefab == null || _gridContainer == null) return null;

            ActivePerkIcon icon = Instantiate(_iconPrefab, _gridContainer);
            icon.Setup(perk, stacks);
            _icons[perk] = icon;

            return icon;
        }

        private void ClearIcons()
        {
            // Знищуємо всіх дітей контейнера, а не лише те, що є в словнику:
            // після перезаходу в сцену чи ручних правок у гріді могли лишитись
            // чужі іконки, і вони б дублювались із новими.
            if (_gridContainer != null)
            {
                for (int i = _gridContainer.childCount - 1; i >= 0; i--)
                {
                    GameObject child = _gridContainer.GetChild(i).gameObject;

                    // Destroy у Unity відкладений до кінця кадру, а Grid Layout
                    // Group рахує дітей одразу - без SetActive(false) старі іконки
                    // ще кадр займали б клітинки поруч із новими.
                    child.SetActive(false);
                    child.transform.SetParent(null, false);
                    Destroy(child);
                }
            }

            _icons.Clear();
        }

        private void UpdateEmptyHint()
        {
            if (_emptyHint != null) _emptyHint.SetActive(_icons.Count == 0);
        }

        private void OnDestroy()
        {
            if (PerkManager.Instance != null)
                PerkManager.Instance.OnPerkTaken -= HandlePerkTaken;
        }
    }
}
