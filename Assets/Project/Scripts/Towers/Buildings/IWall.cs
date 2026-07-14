namespace Towers.Buildings
{
    public interface IWall
    {
        public void Init(TowerNode a, TowerNode b, FortificationGraph graph, WallStrengthCalculator calc);
        public void Collapse();
    }
}
