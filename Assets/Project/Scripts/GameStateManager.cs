using Managers;
using System;
using UnityEngine;

public enum GameState { Playing, Building, Paused, PerkSelection }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnStateChange;

    [SerializeField] private WaveManager _waveManager;
    void Awake()
    {
        Instance = this;
        CurrentState = GameState.Paused;
    }


    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnStateChange?.Invoke(newState);

        CheckForAreaChanges(newState);
    }

    private void CheckForAreaChanges(GameState newState)
    {
        if (newState != GameState.Building) return;

        int targetConfigID = BuildManager.Instance.GetAreaConfigIdForWave(_waveManager.CurrentLevel);
        BuildManager.Instance.ChangeAreaConfig(targetConfigID);
    }
}