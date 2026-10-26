using Mirror;
using UnityEngine;

public class NetworkGrabLock : NetworkBehaviour
{
    [SyncVar]
    private bool isGrabbed;

    private Grabbable grabbable;
    private int localGrabCount;
    private Player localGrabber;
    private bool isLocallyLocked;

    private void Awake()
    {
        grabbable = GetComponent<Grabbable>();
    }

    public override void OnStartClient()
    {
        if (grabbable == null)
        {
            grabbable = GetComponent<Grabbable>();
        }

        if (grabbable != null)
        {
            grabbable.OnGrabbed += OnLocalGrabbed;
            grabbable.OnReleased += OnLocalReleased;
        }
    }

    public override void OnStopClient()
    {
        if (grabbable != null)
        {
            grabbable.OnGrabbed -= OnLocalGrabbed;
            grabbable.OnReleased -= OnLocalReleased;
        }
    }

    public override void OnStartServer()
    {
        if (grabbable == null)
        {
            grabbable = GetComponent<Grabbable>();
        }

        if (grabbable != null)
        {
            grabbable.enabled = !isGrabbed;
        }
    }

    private void OnLocalGrabbed(Grabbable item, Player player)
    {
        localGrabCount++;
        localGrabber = player;

        NetworkIdentity playerIdentity = player != null ? player.GetComponent<NetworkIdentity>() : null;
        if (playerIdentity != null && playerIdentity.isLocalPlayer)
        {
            isLocallyLocked = true;
            if (grabbable != null)
            {
                grabbable.enabled = false;
            }

            CmdSetGrabbed(true, netIdentity);
        }
    }

    private void OnLocalReleased(Grabbable item, Player player)
    {
        localGrabCount = Mathf.Max(0, localGrabCount - 1);

        NetworkIdentity playerIdentity = localGrabber != null ? localGrabber.GetComponent<NetworkIdentity>() : null;
        if (localGrabCount == 0 && playerIdentity != null && playerIdentity.isLocalPlayer)
        {
            isLocallyLocked = false;
            if (grabbable != null)
            {
                grabbable.enabled = true;
            }

            CmdSetGrabbed(false, netIdentity);
            localGrabber = null;
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdSetGrabbed(bool grabbed, NetworkIdentity itemIdentity, NetworkConnectionToClient sender = null)
    {
        if (itemIdentity != netIdentity || sender == null || sender.identity == null)
        {
            return;
        }

        if (grabbed && isGrabbed)
        {
            RpcSetGrabEnabled(false);
            return;
        }

        if (!grabbed && !isGrabbed)
        {
            return;
        }

        isGrabbed = grabbed;
        RpcSetGrabEnabled(!grabbed);
    }

    [ClientRpc]
    private void RpcSetGrabEnabled(bool enabled)
    {
        if (grabbable != null)
        {
            grabbable.enabled = enabled && !isLocallyLocked;
        }
    }
}
