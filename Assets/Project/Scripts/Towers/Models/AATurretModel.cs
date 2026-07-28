namespace Towers.Models
{
    public class AATurretModel : BuildingModel
    {
        public int Damage { get; private set; }
        public float CoolDown { get; private set; }
        public float AttackRange { get; private set; }
        public float RotationSpeed { get; private set; }

        /// <summary>Дамаг із конфіга, без перків.</summary>
        public int BaseDamage { get; private set; }

        /// <summary>Дальність із конфіга, без перків.</summary>
        public float BaseAttackRange { get; private set; }

        public AATurretModel(AATurretConfig buildingConfig) : base(buildingConfig)
        {
            Damage = buildingConfig.Damege;
            CoolDown = buildingConfig.CoolDown;
            RotationSpeed = buildingConfig.RotationSpeed;
            AttackRange = buildingConfig.AttackRange;

            BaseDamage = Damage;
            BaseAttackRange = AttackRange;
        }

        /// <summary>
        /// Перераховує параметри від базових значень. Приймає сумарні бонуси, а не
        /// приріст - повторний виклик із тими самими числами нічого не змінює.
        /// </summary>
        public void ApplyPerkBonuses(int bonusDamage, float rangeMultiplier)
        {
            Damage = BaseDamage + bonusDamage;
            AttackRange = BaseAttackRange * rangeMultiplier;
        }
    }
}
