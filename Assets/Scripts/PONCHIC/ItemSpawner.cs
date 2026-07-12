using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private float spawnInterval = 3f;
    
    private float _timer;

    void Update()
    {
        // Перевірка стану гри
        if (GameStateManager.Instance.CurrentState != GameState.Playing) return;

        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            SpawnItem();
            _timer = 0;
        }
    }

    void SpawnItem()
    {
        // Рандомна позиція внизу екрану (Viewport 0.1 до 0.9 по X, -0.1 по Y)
        Vector3 spawnPos = Camera.main.ViewportToWorldPoint(new Vector3(Random.Range(0.1f, 0.9f), -0.1f, 10f));
        Instantiate(itemPrefab, spawnPos, Quaternion.identity);
    }
}