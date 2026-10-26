using System;
using System.Collections.Generic;
using System.Text;
using EpicTransport;
using Mirror;
using Mirror.VR;
using TMPro;
using UnityEngine;
using Application = UnityEngine.Application;
#if META_XR_SDK
using Oculus.Platform;
#endif

public class NetworkDebugOverlay : MonoBehaviour
{
    [Header("Debug Overlay")]
    public TMP_Text targetText;
    public float refreshInterval = 0.5f;
    public int maxLines = 14;
    public bool showInfoLogs = false;
    public bool showStackTraces = false;

    private float nextRefresh;
    private static readonly object logLock = new object();
    private static readonly List<LogEntry> logs = new List<LogEntry>();
    private static bool installed;

    private struct LogEntry
    {
        public string message;
        public string detail;
        public LogType type;
        public int count;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (installed) return;
        installed = true;
        Application.logMessageReceivedThreaded += OnLog;
        MirrorVRManager.AuthenticationFailed += OnAuthFailed;
        EOSManager.LoginFailed += OnEosFailed;
        Application.quitting += Uninstall;
    }

    private static void Uninstall()
    {
        Application.logMessageReceivedThreaded -= OnLog;
        MirrorVRManager.AuthenticationFailed -= OnAuthFailed;
        EOSManager.LoginFailed -= OnEosFailed;
        Application.quitting -= Uninstall;
        installed = false;
    }

    private static void OnAuthFailed(string message)
    {
        Add(message, LogType.Error, null);
    }

    private static void OnEosFailed(string message)
    {
        Add(message, LogType.Error, null);
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        Add(condition, type, stackTrace);
    }

    private static void Add(string message, LogType type, string stackTrace)
    {
        if (string.IsNullOrEmpty(message)) return;

        message = message.Replace("\r", " ").Replace("\n", " ").Trim();
        if (message.Length > 240) message = message.Substring(0, 240) + "...";

        string detail = null;
        if (!string.IsNullOrEmpty(stackTrace))
        {
            string[] lines = stackTrace.Split('\n');
            if (lines.Length > 0)
            {
                detail = lines[0].Trim();
                if (detail.Length > 200) detail = detail.Substring(0, 200) + "...";
            }
        }

        lock (logLock)
        {
            if (logs.Count > 0)
            {
                LogEntry last = logs[logs.Count - 1];
                if (last.message == message && last.type == type)
                {
                    last.count++;
                    logs[logs.Count - 1] = last;
                    return;
                }
            }

            logs.Add(new LogEntry { message = message, detail = detail, type = type, count = 1 });
            if (logs.Count > 100) logs.RemoveAt(0);
        }
    }

    private void Awake()
    {
        if (targetText == null) targetText = GetComponent<TMP_Text>();
        if (targetText != null) targetText.richText = true;
    }

    private void Update()
    {
        if (targetText == null) return;
        if (Time.unscaledTime < nextRefresh) return;

        nextRefresh = Time.unscaledTime + Mathf.Max(0.1f, refreshInterval);
        targetText.text = BuildText();
    }

    private string BuildText()
    {
        bool eosExists = EOSManager.instance != null;
        bool eosReady = eosExists && EOSManager.Initialized;
        bool eosConnecting = eosExists && EOSManager.IsConnecting;
        string eosError = eosExists ? EOSManager.LastLoginError : null;
        string authError = MirrorVRManager.LastAuthenticationError;

        bool lobby = EOSTransport.instance != null && EOSTransport.ConnectedToLobby;
        string lobbyCode = "-";
        if (lobby && EOSTransport.ConnectedLobbyInfo != null) lobbyCode = EOSTransport.ConnectedLobbyInfo.LobbyId;

#if META_XR_SDK
        bool metaReady = Core.IsInitialized();
#else
        bool metaReady = false;
#endif

        StringBuilder sb = new StringBuilder(2048);
        sb.AppendLine("<b>AFTER HOURS // NETWORK DEBUG</b>");
        sb.AppendLine($"<size=70%><noparse>{Application.identifier}</noparse>  v{Application.version}  {Application.platform}</size>");
        sb.AppendLine($"Meta:  {Status(metaReady, "INIT OK", "NOT INIT")}");
        sb.AppendLine($"EOS:   {Status(eosReady, "INIT OK", eosConnecting ? "CONNECTING" : "FAILED")}");

        if (!string.IsNullOrEmpty(eosError))
            sb.AppendLine($"<color=#ff7777>EOS error: <noparse>{Short(eosError, 180)}</noparse></color>");

        if (!string.IsNullOrEmpty(authError))
            sb.AppendLine($"<color=#ff7777>Auth error: <noparse>{Short(authError, 180)}</noparse></color>");

        sb.AppendLine($"Net:   {(NetworkServer.active ? "HOST" : NetworkClient.active ? "CLIENT" : "OFFLINE")}");
        sb.AppendLine($"Lobby: {(lobby ? $"<color=#77ff77><noparse>{lobbyCode}</noparse></color>" : "NOT CONNECTED")}");
        sb.AppendLine($"Player: <noparse>{(MirrorVRPlayer.Player != null ? MirrorVRPlayer.Player.name : "-")}</noparse>");
        sb.AppendLine("<line-height=90%>--- LOG ---</line-height>");

        List<string> visible = new List<string>();

        lock (logLock)
        {
            for (int i = logs.Count - 1; i >= 0 && visible.Count < maxLines; i--)
            {
                LogEntry entry = logs[i];

                if (!showInfoLogs && entry.type == LogType.Log) continue;

                string color = entry.type == LogType.Error || entry.type == LogType.Exception
                    ? "#ff7777"
                    : entry.type == LogType.Warning ? "#ffcc55" : "#bbbbbb";

                string prefix = entry.type == LogType.Error || entry.type == LogType.Exception
                    ? "E"
                    : entry.type == LogType.Warning ? "W" : "I";

                visible.Add($"<color={color}>[{prefix}] <noparse>{entry.message}</noparse>{(entry.count > 1 ? $" x{entry.count}" : "")}</color>");

                if (showStackTraces && !string.IsNullOrEmpty(entry.detail))
                    visible.Add($"<color=#aa5555>    <noparse>{entry.detail}</noparse></color>");
            }
        }

        visible.Reverse();
        sb.Append(string.Join("\n", visible));

        return sb.ToString();
    }

    private static string Status(bool ok, string yes, string no)
    {
        return ok ? $"<color=#77ff77>{yes}</color>" : $"<color=#ffaa33>{no}</color>";
    }

    private static string Short(string value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length > max ? value.Substring(0, max) + "..." : value;
    }
}
