namespace Towers.Models
{
    public class AATurretModel : BuildingModel
    {
        public int Damage { get; private set; }
        public float CoolDown { get; private set; }
        public float AttackRange { get; private set; }
        public float RotationSpeed { get; private set; }

        public AATurretModel(AATurretConfig buildingConfig) : base(buildingConfig)
        {
            Damage = buildingConfig.Damege;
            CoolDown = buildingConfig.CoolDown;
            RotationSpeed = buildingConfig.RotationSpeed;
            AttackRange = buildingConfig.AttackRange;
        }
    }
}
