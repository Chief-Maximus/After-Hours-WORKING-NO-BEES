using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror.VR;
using AttributeData = Epic.OnlineServices.Lobby.AttributeData;

public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager Instance { get; private set; }

    [SerializeField] private string LobbySceneName = "Main";
    [SerializeField] private int MaxPlayers = 10;

    public string SelectedGamemode { get; private set; }

    private bool joining;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectGamemode(string gamemode)
    {
        SelectedGamemode = gamemode;
    }

    public void JoinSelectedGamemode()
    {
        if (string.IsNullOrWhiteSpace(SelectedGamemode) || joining)
            return;

        joining = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(LobbySceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != LobbySceneName)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(ConnectToLobby());
    }

    private IEnumerator ConnectToLobby()
    {
        float timeout = Time.realtimeSinceStartup + 45f;

        while ((MirrorVRManager.instance == null || !MirrorVRManager.Initialized) && Time.realtimeSinceStartup < timeout)
            yield return null;

        if (MirrorVRManager.instance == null || !MirrorVRManager.Initialized)
        {
            Debug.LogError("MirrorVR authentication killed it self.");
            joining = false;
            yield break;
        }

        AttributeData[] attributes =
        {
            new AttributeData
            {
                Key = "GameMode",
                Value = SelectedGamemode
            }
        };

        MirrorVRManager.JoinRandomLobby(MaxPlayers, attributes, true);

        joining = false;
    }
}