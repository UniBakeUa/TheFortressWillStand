using System;
using UnityEngine;

    public enum GameState { Playing, Building }

    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }
        public GameState CurrentState { get; private set; }
        public event Action<GameState> OnStateChange;
        void Awake()
        {
            Instance = this;
            CurrentState = GameState.Building;
        }


        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"State changed to: {newState}");
            OnStateChange?.Invoke(newState);
        }
}