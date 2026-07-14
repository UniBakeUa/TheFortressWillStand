using Items.Data;

namespace Items.Spawners
{
    public interface ISpawner
    {
        public void SpawnTimer(ItemSpawnData spawnData);
    }
}
