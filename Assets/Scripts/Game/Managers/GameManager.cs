using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public Action<GameState> OnGameStateChanged;

    public static GameManager Instance;

    [SerializeField]
    private GameState gameState;
    public GameState GameState
    {
        get { return gameState; }
        set 
        { 
            if (gameState == value)
                return;

            gameState = value;
            OnGameStateChanged?.Invoke(gameState);
        }
    }

    public PlayerController playerController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        GetPlayerController();

    }

    void Start()
    {
        GameState = GameState.Playing;
    }

    private void GetPlayerController()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<PlayerController>();
        }
    }
}
