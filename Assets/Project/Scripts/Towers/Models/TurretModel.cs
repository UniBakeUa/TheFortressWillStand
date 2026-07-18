
namespace Towers.Models
{
    public class TurretModel : BuildingModel
    {
        public int Damage { get; private set; }
        public float CoolDown { get; private set; }
        public float AttackRange { get; private set; }
        public float RotationSpeed { get; private set; }

        public TurretModel(TurretConfig buildingConfig) : base(buildingConfig)
        {
            Damage = buildingConfig.Damege;
            CoolDown = buildingConfig.CoolDown;
            RotationSpeed = buildingConfig.RotationSpeed;
            AttackRange = buildingConfig.AttackRange;
        }
    }
}
