using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public Action<GameState> OnGameStateChanged;

    public static GameManager Instance;

    public PlayerAuthentication Authentication { get; } = new PlayerAuthentication();

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
            return;
        }
        GetPlayerController();

    }

    private async void Start()
    {
        if (Instance != this)
            return;

        GameState = GameState.Playing;

        try
        {
            await Authentication.SignInAsync();
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogError("Unity Authentication failed. Check the Services project link and network connection. " +
                    "Call Authentication.SignInAsync() to retry.", this);
                Debug.LogException(exception, this);
            }
        }
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
