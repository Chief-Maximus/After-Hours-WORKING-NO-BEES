using Edgegap;
using TMPro;
using UnityEngine;

public class ManagerUI : MonoBehaviour
{
    public GameObject LobbyUI;
    public GameObject MainMenuUI;
    public TextMeshProUGUI SelectionGame;
    
    public static void QuitGame() 
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif

        Application.Quit();
    }

    public void NextStep()
    {
        MainMenuUI.SetActive(false);
        LobbyUI.SetActive(true);
    }
    
    public void SwitchGamemode(string Gamemode)
    {
        SelectionGame.SetText("Currently Selected: \n" + Gamemode);
        MatchmakingManager.Instance.SelectGamemode(Gamemode);
    }

    public void JoinGamemode()
    {
        MatchmakingManager.Instance.JoinSelectedGamemode();
    }
}
