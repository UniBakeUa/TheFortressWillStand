
namespace Towers.Models
{
    public class TurretModel : BuildingModel
    {
        public int Damage { get; private set; }
        public float CoolDown { get; private set; }
        public float AttackRange { get; private set; }
        public float RotationSpeed { get; private set; }
        public float SplashRadius { get; private set; }

        /// <summary>Дальність із конфіга, без перків. Множники рахуються від неї.</summary>
        public float BaseAttackRange { get; private set; }

        /// <summary>Дамаг із конфіга, без перків.</summary>
        public int BaseDamage { get; private set; }

        public TurretModel(TurretConfig buildingConfig) : base(buildingConfig)
        {
            Damage = buildingConfig.Damege;
            CoolDown = buildingConfig.CoolDown;
            RotationSpeed = buildingConfig.RotationSpeed;
            AttackRange = buildingConfig.AttackRange;
            SplashRadius = buildingConfig.SplashRadius;

            BaseDamage = Damage;
            BaseAttackRange = AttackRange;
        }

        /// <summary>
        /// Перераховує параметри від базових значень. Приймає сумарні бонуси, а не
        /// приріст - тож повторний виклик із тими самими числами нічого не змінює
        /// (важливо: викликається і для нових турелей, і при взятті нового перка).
        /// </summary>
        public void ApplyPerkBonuses(int bonusDamage, float rangeMultiplier)
        {
            Damage = BaseDamage + bonusDamage;
            AttackRange = BaseAttackRange * rangeMultiplier;
        }
    }
}
