using System;
using System.Reflection;
using Mirror;
using UnityEngine;

public class NetworkGrabLock : NetworkBehaviour
{
    [SyncVar]
    private bool isGrabbed;

    private Component grabbable;
    private int localGrabCount;
    private bool isLocallyLocked;
    private object localGrabber;
    private EventInfo grabbedEvent;
    private EventInfo releasedEvent;
    private Delegate grabbedHandler;
    private Delegate releasedHandler;

    private void Awake()
    {
        grabbable = FindGrabbable();
    }

    public override void OnStartClient()
    {
        if (grabbable == null)
        {
            grabbable = FindGrabbable();
        }

        if (grabbable != null)
        {
            SubscribeToGrabEvents();
        }
    }

    public override void OnStopClient()
    {
        if (grabbable != null)
        {
            UnsubscribeFromGrabEvents();
        }
    }

    public override void OnStartServer()
    {
        if (grabbable == null)
        {
            grabbable = FindGrabbable();
        }

        SetGrabbableEnabled(!isGrabbed);
    }

    private Component FindGrabbable()
    {
        foreach (Component component in GetComponents<Component>())
        {
            if (component != null && component.GetType().Name == "Grabbable")
            {
                return component;
            }
        }

        return null;
    }

    private void SubscribeToGrabEvents()
    {
        if (grabbable == null || grabbedHandler != null)
        {
            return;
        }

        Type grabbableType = grabbable.GetType();
        grabbedEvent = grabbableType.GetEvent("OnGrabbed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        releasedEvent = grabbableType.GetEvent("OnReleased", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (grabbedEvent == null || releasedEvent == null)
        {
            return;
        }

        MethodInfo grabbedMethod = GetType().GetMethod(nameof(OnLocalGrabbed), BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo releasedMethod = GetType().GetMethod(nameof(OnLocalReleased), BindingFlags.Instance | BindingFlags.NonPublic);
        grabbedHandler = Delegate.CreateDelegate(grabbedEvent.EventHandlerType, this, grabbedMethod, false);
        releasedHandler = Delegate.CreateDelegate(releasedEvent.EventHandlerType, this, releasedMethod, false);

        if (grabbedHandler != null && releasedHandler != null)
        {
            grabbedEvent.AddEventHandler(grabbable, grabbedHandler);
            releasedEvent.AddEventHandler(grabbable, releasedHandler);
        }
    }

    private void UnsubscribeFromGrabEvents()
    {
        if (grabbable != null && grabbedHandler != null && grabbedEvent != null)
        {
            grabbedEvent.RemoveEventHandler(grabbable, grabbedHandler);
        }

        if (grabbable != null && releasedHandler != null && releasedEvent != null)
        {
            releasedEvent.RemoveEventHandler(grabbable, releasedHandler);
        }

        grabbedHandler = null;
        releasedHandler = null;
    }

    private void OnLocalGrabbed(object item, object player)
    {
        localGrabCount++;
        localGrabber = player;

        NetworkIdentity playerIdentity = GetPlayerIdentity(player);
        if (playerIdentity != null && playerIdentity.isLocalPlayer)
        {
            isLocallyLocked = true;
            SetGrabbableEnabled(false);

            CmdSetGrabbed(true, netIdentity);
        }
    }

    private void OnLocalReleased(object item, object player)
    {
        localGrabCount = Mathf.Max(0, localGrabCount - 1);

        NetworkIdentity playerIdentity = GetPlayerIdentity(localGrabber);
        if (localGrabCount == 0 && playerIdentity != null && playerIdentity.isLocalPlayer)
        {
            isLocallyLocked = false;
            SetGrabbableEnabled(true);

            CmdSetGrabbed(false, netIdentity);
            localGrabber = null;
        }
    }

    private static NetworkIdentity GetPlayerIdentity(object player)
    {
        if (player is Component playerComponent)
        {
            return playerComponent.GetComponent<NetworkIdentity>();
        }

        if (player is GameObject playerObject)
        {
            return playerObject.GetComponent<NetworkIdentity>();
        }

        return null;
    }

    private void SetGrabbableEnabled(bool enabled)
    {
        if (grabbable is Behaviour behaviour)
        {
            behaviour.enabled = enabled;
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
        SetGrabbableEnabled(enabled && !isLocallyLocked);
    }
}
