using System.Collections.Generic;
using Items;
using Towers.Buildings;
using Towers.ScriptableObjects;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Тримає всі взяті прокачки і застосовує їхні ефекти.
    ///
    /// Ефекти двох сортів:
    /// - постійні (дамаг ПВО, дальність турелей) - зберігаються як сумарні бонуси
    ///   і накладаються на модель кожної турелі: і на вже збудовані, і на нові
    ///   через ApplyToNewTurret();
    /// - періодичні (авторемонт, німцеріз) - тікають в Update під час Playing.
    ///
    /// Вибором карток керує PerkSelectionController; сюди приходить уже готове
    /// рішення "гравець узяв цей перк".
    /// </summary>
    public class PerkManager : MonoBehaviour
    {
        public static PerkManager Instance { get; private set; }

        [Header("База карток")]
        [SerializeField] private PerkLibrary _perkLibrary;

        [Header("Посилання на сцену")]
        [SerializeField] private SpawnerManager _spawnerManager;
        [Tooltip("Префаб вибуху для німцеріза (Prefabs/Items/Explosion). " +
                 "Можна лишити порожнім - тоді візьметься той самий, що в MouseBomber")]
        [SerializeField] private global::Explosion _fingerStrikeExplosionPrefab;
        [Tooltip("Звідки взяти префаб вибуху, якщо поле вище порожнє")]
        [SerializeField] private MouseBomber _mouseBomber;

        [Header("Діагностика")]
        [Tooltip("Логувати кожен тік авторемонту - видно, чи перк узагалі працює")]
        [SerializeField] private bool _logRepairs;

        [Header("Німцеріз")]
        [Tooltip("Радіус удару 'пальцем' по випадковому ворогу")]
        [SerializeField] private float _fingerStrikeRadius = 1f;

        // Скільки разів узято кожну картку. Скільки їх узагалі можна взяти,
        // обмежує колода (PerkConfig.CopiesInDeck), а не цей лічильник.
        //
        // Ключ - сам конфіг, а НЕ його Id: різні асети легко отримують однаковий
        // Id (копіювання картки в редакторі), і тоді лічильники різних перків
        // зливались би в один.
        private readonly Dictionary<PerkConfig, int> _takenStacks = new();

        // Унікальні взяті перки в порядку взяття - для в'юшки активних прокачок.
        // Повторне взяття не додає новий запис, а збільшує лічильник у _takenStacks.
        private readonly List<PerkConfig> _takenPerks = new();

        // Сумарні постійні бонуси.
        private int _antiAirBonusDamage;
        private float _groundRangeMultiplier = 1f;

        // Періодичні ефекти. Кожен запис - "лікуємо/б'ємо кожні Interval секунд".
        private readonly List<PeriodicEffect> _periodicEffects = new();

        public PerkLibrary Library => _perkLibrary;

        /// <summary>Узяті перки, кожен по одному разу, у порядку взяття.</summary>
        public IReadOnlyList<PerkConfig> TakenPerks => _takenPerks;

        /// <summary>
        /// Спрацьовує після взяття будь-якого перка. Аргумент - щойно взятий,
        /// щоб в'юшка могла підсвітити саме його.
        /// </summary>
        public event System.Action<PerkConfig> OnPerkTaken;

        private class PeriodicEffect
        {
            public PerkEffectType Type;
            public float Amount;
            public float Interval;
            public float Timer;
        }

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>Скільки разів узято картку (0, якщо жодного).</summary>
        public int GetStacks(PerkConfig perk)
        {
            if (perk == null) return 0;
            return _takenStacks.TryGetValue(perk, out int count) ? count : 0;
        }

        /// <summary>
        /// Застосовує картку. Викликає PerkSelectionController після того, як
        /// оплата вже пройшла.
        /// </summary>
        public void ApplyPerk(PerkConfig perk)
        {
            if (perk == null) return;

            _takenStacks[perk] = GetStacks(perk) + 1;

            if (!_takenPerks.Contains(perk))
                _takenPerks.Add(perk);


            switch (perk.EffectType)
            {
                case PerkEffectType.AntiAirDamage:
                    _antiAirBonusDamage += Mathf.RoundToInt(perk.Amount);
                    RefreshAllTurrets();
                    break;

                case PerkEffectType.GroundTurretRange:
                    // Amount - частка приросту (0.5 = +50%). Стакається додаванням
                    // до множника, а не множенням: два перки по +50% дають +100%,
                    // а не +125%.
                    _groundRangeMultiplier += perk.Amount;
                    RefreshAllTurrets();
                    break;

                case PerkEffectType.AutoRepairBuildings:
                case PerkEffectType.AutoRepairFortress:
                case PerkEffectType.AutoFingerStrike:
                    AddPeriodicEffect(perk);
                    break;

                case PerkEffectType.InstantPonchics:
                    if (PonchicManager.Instance != null)
                        PonchicManager.Instance.AddPonchics(Mathf.RoundToInt(perk.Amount));
                    break;
            }

            if (_logRepairs)
            {
                Debug.Log($"[PerkManager] Узято перк '{perk.DisplayName}' " +
                          $"(тип={perk.EffectType}, Amount={perk.Amount}, кожні {perk.SecondaryAmount}с). " +
                          $"Періодичних ефектів зараз: {_periodicEffects.Count}");
            }

            OnPerkTaken?.Invoke(perk);
        }

        private void AddPeriodicEffect(PerkConfig perk)
        {
            // Другий такий самий перк не додає окремий таймер, а підсилює наявний:
            // інакше два "+1 HP раз на 5с" тікали б у різній фазі й виглядали як хаос.
            foreach (var effect in _periodicEffects)
            {
                if (effect.Type != perk.EffectType) continue;

                effect.Amount += perk.Amount;
                // Інтервал беремо найкоротший із узятих.
                effect.Interval = Mathf.Min(effect.Interval, Mathf.Max(0.1f, perk.SecondaryAmount));
                return;
            }

            _periodicEffects.Add(new PeriodicEffect
            {
                Type = perk.EffectType,
                Amount = perk.Amount,
                Interval = Mathf.Max(0.1f, perk.SecondaryAmount),
                Timer = 0f,
            });
        }

        /// <summary>
        /// Накладає поточні бонуси на щойно збудовану турель. Кличе TurretBase
        /// після Initialize - інакше нова турель була б слабшою за старі.
        /// </summary>
        public void ApplyToNewTurret(TurretBase turret)
        {
            ApplyBonusesTo(turret);
        }

        private void RefreshAllTurrets()
        {
            foreach (var building in BaseBuilding.AllBuildings)
            {
                if (building is TurretBase turret)
                    ApplyBonusesTo(turret);
            }
        }

        private void ApplyBonusesTo(TurretBase turret)
        {
            if (turret == null) return;

            switch (turret)
            {
                case AATurret aa when aa.TurretModel != null:
                    aa.TurretModel.ApplyPerkBonuses(_antiAirBonusDamage, 1f);
                    break;

                case GroundTurret ground when ground.TurretModel != null:
                    ground.TurretModel.ApplyPerkBonuses(0, _groundRangeMultiplier);
                    break;
            }
        }

        private void Update()
        {
            if (GameStateManager.Instance == null) return;

            GameState state = GameStateManager.Instance.CurrentState;

            // Гра на паузі або на екрані вибору карток - не тікає нічого.
            if (state != GameState.Playing && state != GameState.Building) return;

            for (int i = 0; i < _periodicEffects.Count; i++)
            {
                PeriodicEffect effect = _periodicEffects[i];

                // Німцеріз б'є по ворогах, тож поза боєм не має сенсу. Авторемонт
                // працює і в Building - гравець має бачити, як будівлі відновлюються
                // між хвилями.
                if (!IsEffectActiveIn(effect.Type, state)) continue;

                effect.Timer += Time.deltaTime;

                if (effect.Timer < effect.Interval) continue;

                effect.Timer -= effect.Interval;
                FirePeriodicEffect(effect);
            }
        }

        private static bool IsEffectActiveIn(PerkEffectType type, GameState state)
        {
            if (state == GameState.Playing) return true;

            // У Building лишається тільки ремонт.
            return type == PerkEffectType.AutoRepairBuildings
                || type == PerkEffectType.AutoRepairFortress;
        }

        private void FirePeriodicEffect(PeriodicEffect effect)
        {
            switch (effect.Type)
            {
                case PerkEffectType.AutoRepairBuildings:
                    RepairBuildings(effect.Amount, repairFortress: false);
                    break;

                case PerkEffectType.AutoRepairFortress:
                    RepairBuildings(effect.Amount, repairFortress: true);
                    break;

                case PerkEffectType.AutoFingerStrike:
                    FingerStrikeRandomEnemy();
                    break;
            }
        }

        private void RepairBuildings(float amount, bool repairFortress)
        {
            int repaired = 0;

            // Копії списку не робимо: Repair не руйнує будівлі, тож колекція
            // під час обходу не змінюється.
            foreach (var building in BaseBuilding.AllBuildings)
            {
                if (building == null) continue;
                if (building.IsFortress != repairFortress) continue;
                if (!building.NeedsRepair) continue;

                building.Repair(amount);
                repaired++;
            }

            if (_logRepairs)
            {
                Debug.Log($"[PerkManager] Ремонт (фортеця={repairFortress}, +{amount} HP): " +
                          $"полагоджено {repaired} з {BaseBuilding.AllBuildings.Count} будівель у реєстрі.");
            }
        }

        /// <summary>
        /// "Німцеріз": б'є по випадковому живому ворогу так само, як клік мишкою -
        /// вибух і миттєва смерть у радіусі.
        /// </summary>
        private void FingerStrikeRandomEnemy()
        {
            if (_spawnerManager == null) return;

            List<Enemy> enemies = _spawnerManager.ActiveEnemies;
            if (enemies == null || enemies.Count == 0) return;

            Enemy target = PickRandomAliveEnemy(enemies);
            if (target == null) return;

            Vector2 position = target.transform.position;

            var hits = Physics2D.CircleCastAll(position, _fingerStrikeRadius, Vector2.zero, 0f, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                if (hit.transform.TryGetComponent(out Enemy enemy))
                {
                    // Як і MouseBomber - б'ємо згори, напрямку розльоту крові немає.
                    enemy.WasStricken();
                }
            }

            global::Explosion explosionPrefab = ResolveExplosionPrefab();
            if (explosionPrefab != null)
            {
                // Той самий порядок, що в MouseBomber: масштаб треба виставити до
                // того, як увімкнеться OnEnable, бо саме він стартує анімацію.
                var explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
                explosion.enabled = false;
                explosion.ChangeScaleModifier(_fingerStrikeRadius);
                explosion.enabled = true;
            }

            if (SoundManager.Instance != null)
                SoundManager.Instance.Play(Audio.SoundId.MouseBomberShot, position);
        }

        /// <summary>
        /// Префаб вибуху: спершу власне поле, потім - той, що вже налаштований
        /// у MouseBomber. Так перк не залежить від зайвого посилання в інспекторі.
        /// </summary>
        private global::Explosion ResolveExplosionPrefab()
        {
            if (_fingerStrikeExplosionPrefab != null) return _fingerStrikeExplosionPrefab;

            if (_mouseBomber == null)
                _mouseBomber = FindFirstObjectByType<MouseBomber>();

            return _mouseBomber != null ? _mouseBomber.ExplosionPrefab : null;
        }

        private Enemy PickRandomAliveEnemy(List<Enemy> enemies)
        {
            // Резервуарний вибір: список може містити вимкнених ворогів, тож
            // просто Random.Range по індексу міг би раз за разом влучати в них.
            Enemy chosen = null;
            int seen = 0;

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled) continue;

                seen++;
                if (Random.Range(0, seen) == 0)
                    chosen = enemy;
            }

            return chosen;
        }
    }
}
