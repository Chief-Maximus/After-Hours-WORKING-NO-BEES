using System;
using UnityEngine;
using Mirror;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.Platform;
using Oculus.Platform;
using Oculus.Platform.Models;

public class Login : MonoBehaviour
{
    const string GameKey = "GAME";
    const string GameValue = "GAME";
    const uint MaxPlayers = 4;

    public ProductUserId LocalUserId { get; private set; }
    public bool IsLoggedIn => LocalUserId != null && LocalUserId.IsValid();

    PlatformInterface platform;
    ConnectInterface connect => platform.GetConnectInterface();
    LobbyInterface lobby => platform.GetLobbyInterface();

    string currentLobbyId;

    void Start()
    {
        platform = GetPlatform();
        StartMetaLogin();
    }

    PlatformInterface GetPlatform()
    {
        throw new NotImplementedException("Return the PlatformInterface from your EOSManager.");
    }

    void StartMetaLogin()
    {
        try
        {
            Core.AsyncInitialize().OnComplete(OnMetaInit);
        }
        catch (Exception e)
        {
            Debug.LogError("Meta init exception: " + e.Message);
        }
    }

    void OnMetaInit(Message<PlatformInitialize> msg)
    {
        if (msg.IsError) { Debug.LogError("Meta init error: " + msg.GetError().Message); return; }
        Entitlements.IsUserEntitledToApplication().OnComplete(OnEntitlement);
    }

    void OnEntitlement(Message msg)
    {
        if (msg.IsError) { Debug.LogError("Entitlement check failed: " + msg.GetError().Message); return; }
        Users.GetLoggedInUser().OnComplete(OnLoggedInUser);
    }

    void OnLoggedInUser(Message<User> msg)
    {
        if (msg.IsError) { Debug.LogError("GetLoggedInUser error: " + msg.GetError().Message); return; }
        ulong userId = msg.Data.ID;
        if (userId == 0) { Debug.LogError("Meta user ID is 0. Check app ID and entitlement."); return; }

        Users.GetUserProof().OnComplete(proof =>
        {
            if (proof.IsError) { Debug.LogError("GetUserProof error: " + proof.GetError().Message); return; }
            string token = userId + "|" + proof.Data.Value;
            EosConnectLogin(token);
        });
    }

    void EosConnectLogin(string token)
    {
        var options = new Epic.OnlineServices.Connect.LoginOptions
        {
            Credentials = new Credentials
            {
                Type = ExternalCredentialType.OculusUseridNonce,
                Token = token
            }
        };

        connect.Login(ref options, null, (ref LoginCallbackInfo data) =>
        {
            if (data.ResultCode == Result.Success)
            {
                LocalUserId = data.LocalUserId;
                Debug.Log("EOS login OK: " + LocalUserId);
            }
            else if (data.ResultCode == Result.InvalidUser)
            {
                var create = new CreateUserOptions { ContinuanceToken = data.ContinuanceToken };
                connect.CreateUser(ref create, null, (ref CreateUserCallbackInfo c) =>
                {
                    if (c.ResultCode == Result.Success)
                    {
                        LocalUserId = c.LocalUserId;
                        Debug.Log("EOS user created: " + LocalUserId);
                    }
                    else Debug.LogError("CreateUser error: " + c.ResultCode);
                });
            }
            else
            {
                Debug.LogError("EOS login error: " + data.ResultCode);
            }
        });
    }

    public void HostLobby()
    {
        if (!IsLoggedIn) { Debug.LogWarning("Not logged in yet."); return; }

        var opts = new CreateLobbyOptions
        {
            LocalUserId = LocalUserId,
            MaxLobbyMembers = MaxPlayers,
            PermissionLevel = LobbyPermissionLevel.Publicadvertised,
            PresenceEnabled = false,
            AllowInvites = true,
            BucketId = GameValue
        };

        lobby.CreateLobby(ref opts, null, (ref CreateLobbyCallbackInfo d) =>
        {
            if (d.ResultCode != Result.Success) { Debug.LogError("CreateLobby error: " + d.ResultCode); return; }

            currentLobbyId = d.LobbyId;
            SetLobbyAttribute(currentLobbyId, GameKey, GameValue, () =>
            {
                Debug.Log("Lobby created: " + currentLobbyId);
                NetworkManager.singleton.StartHost();
            });
        });
    }

    void SetLobbyAttribute(string lobbyId, string key, string value, Action done)
    {
        var modOpts = new UpdateLobbyModificationOptions { LobbyId = lobbyId, LocalUserId = LocalUserId };
        if (lobby.UpdateLobbyModification(ref modOpts, out LobbyModification mod) != Result.Success)
        {
            Debug.LogError("UpdateLobbyModification failed");
            return;
        }

        var addOpts = new LobbyModificationAddAttributeOptions
        {
            Attribute = new AttributeData { Key = key, Value = value },
            Visibility = LobbyAttributeVisibility.Public
        };
        mod.AddAttribute(ref addOpts);

        var upd = new UpdateLobbyOptions { LobbyModificationHandle = mod };
        lobby.UpdateLobby(ref upd, null, (ref UpdateLobbyCallbackInfo d) =>
        {
            if (d.ResultCode == Result.Success) done?.Invoke();
            else Debug.LogError("UpdateLobby error: " + d.ResultCode);
        });
        mod.Release();
    }

    public void JoinFirstLobby()
    {
        if (!IsLoggedIn) { Debug.LogWarning("Not logged in yet."); return; }

        var so = new CreateLobbySearchOptions { MaxResults = 10 };
        if (lobby.CreateLobbySearch(ref so, out LobbySearch search) != Result.Success)
        {
            Debug.LogError("CreateLobbySearch failed");
            return;
        }

        var param = new LobbySearchSetParameterOptions
        {
            Parameter = new AttributeData { Key = GameKey, Value = GameValue },
            ComparisonOp = ComparisonOp.Equal
        };
        search.SetParameter(ref param);

        var find = new LobbySearchFindOptions { LocalUserId = LocalUserId };
        search.Find(ref find, null, (ref LobbySearchFindCallbackInfo d) =>
        {
            if (d.ResultCode != Result.Success) { Debug.LogError("Lobby search error: " + d.ResultCode); search.Release(); return; }

            var countOpts = new LobbySearchGetSearchResultCountOptions();
            uint count = search.GetSearchResultCount(ref countOpts);
            if (count == 0) { Debug.Log("No lobby found."); search.Release(); return; }

            var copyOpts = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = 0 };
            if (search.CopySearchResultByIndex(ref copyOpts, out LobbyDetails details) != Result.Success)
            {
                Debug.LogError("CopySearchResultByIndex failed");
                search.Release();
                return;
            }

            var ownerOpts = new LobbyDetailsGetLobbyOwnerOptions();
            ProductUserId hostId = details.GetLobbyOwner(ref ownerOpts);

            var join = new JoinLobbyOptions
            {
                LobbyDetailsHandle = details,
                LocalUserId = LocalUserId,
                PresenceEnabled = false
            };

            lobby.JoinLobby(ref join, null, (ref JoinLobbyCallbackInfo j) =>
            {
                if (j.ResultCode == Result.Success)
                {
                    currentLobbyId = j.LobbyId;
                    hostId.ToString(out Utf8String hostUtf8);
                    string hostString = hostUtf8;
                    Debug.Log("Joined lobby, host: " + hostString);

                    NetworkManager.singleton.networkAddress = hostString;
                    NetworkManager.singleton.StartClient();
                }
                else Debug.LogError("JoinLobby error: " + j.ResultCode);

                details.Release();
                search.Release();
            });
        });
    }

    public void LeaveLobby()
    {
        if (string.IsNullOrEmpty(currentLobbyId)) return;

        var opts = new LeaveLobbyOptions { LobbyId = currentLobbyId, LocalUserId = LocalUserId };
        lobby.LeaveLobby(ref opts, null, (ref LeaveLobbyCallbackInfo d) =>
        {
            Debug.Log("Left lobby: " + d.ResultCode);
            currentLobbyId = null;
        });

        if (NetworkServer.active && NetworkClient.isConnected) NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected) NetworkManager.singleton.StopClient();
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        if (kb.hKey.wasPressedThisFrame) HostLobby();
        if (kb.jKey.wasPressedThisFrame) JoinFirstLobby();
        if (kb.lKey.wasPressedThisFrame) LeaveLobby();
    }
}