using Mirror;
using UnityEngine;
using easyInputs;
using GorillaLocomotion;

public class NetworkGrabLock : NetworkBehaviour
{
    [SerializeField] private float grabDistance = 0.28f;
    [SerializeField] private float followSharpness = 30f;
    [SerializeField] private float releaseVelocityMultiplier = 1.2f;

    private Rigidbody body;
    private NetworkTransformUnreliable networkTransform;
    private Transform heldHand;
    private Vector3 previousHandPosition;
    private Vector3 handVelocity;
    private bool localHolding;
    private bool locallyLocked;
    private bool originalKinematic;
    private bool originalUseGravity;
    private bool awaitingAuthority;
    private bool localRequestPending;
    private float nextAuthorityRequestTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        networkTransform = GetComponent<NetworkTransformUnreliable>();
        if (body != null)
        {
            originalKinematic = body.isKinematic;
            originalUseGravity = body.useGravity;
        }
    }

    public override void OnStartClient()
    {
        ApplyHeldState(false);
    }

    public override void OnStartAuthority()
    {
        awaitingAuthority = false;
        localRequestPending = false;
        locallyLocked = true;
        ApplyHeldState(true);
    }

    public override void OnStopAuthority()
    {
        if (!NetworkServer.active)
        {
            localHolding = false;
            heldHand = null;
            locallyLocked = false;
            ApplyHeldState(false);
        }
    }

    public override void OnStopClient()
    {
        if (body != null)
        {
            body.isKinematic = originalKinematic;
            body.useGravity = originalUseGravity;
        }
    }

    private void Update()
    {
        if (netIdentity == null || !netIdentity.isClient)
            return;

        Player player = Player.Instance;
        if (player == null || player.leftHandTransform == null || player.rightHandTransform == null)
            return;

        if (NetworkServer.active)
        {
            if (localHolding || netIdentity.connectionToClient != null)
                return;

            bool hostLeftHeld = EasyInputs.GetGripButtonDown(EasyHand.LeftHand);
            bool hostRightHeld = EasyInputs.GetGripButtonDown(EasyHand.RightHand);
            Transform hostHand = FindCandidateHand(player, hostLeftHeld, hostRightHeld);
            if (hostHand != null)
                BeginLocalHold(hostHand);
            return;
        }

        if (netIdentity.isOwned)
        {
            if (localHolding)
                return;
            bool leftHeld = EasyInputs.GetGripButtonDown(EasyHand.LeftHand);
            bool rightHeld = EasyInputs.GetGripButtonDown(EasyHand.RightHand);
            Transform ownedHand = FindCandidateHand(player, leftHeld, rightHeld);
            if (ownedHand == null)
            {
                localRequestPending = true;
                CmdReleaseGrab();
                return;
            }
            BeginLocalHold(ownedHand);
            return;
        }

        if (localHolding || awaitingAuthority || localRequestPending || locallyLocked)
            return;

        bool leftPressed = EasyInputs.GetGripButtonDown(EasyHand.LeftHand);
        bool rightPressed = EasyInputs.GetGripButtonDown(EasyHand.RightHand);
        Transform candidate = FindCandidateHand(player, leftPressed, rightPressed);
        if (candidate == null || Time.unscaledTime < nextAuthorityRequestTime)
            return;

        nextAuthorityRequestTime = Time.unscaledTime + 0.15f;
        awaitingAuthority = true;
        CmdRequestGrab();
    }

    private Transform FindCandidateHand(Player player, bool leftPressed, bool rightPressed)
    {
        float leftDistance = leftPressed ? Vector3.Distance(player.leftHandTransform.position, transform.position) : float.MaxValue;
        float rightDistance = rightPressed ? Vector3.Distance(player.rightHandTransform.position, transform.position) : float.MaxValue;
        float closestDistance = Mathf.Min(leftDistance, rightDistance);
        if (closestDistance > grabDistance)
            return null;
        return leftDistance <= rightDistance ? player.leftHandTransform : player.rightHandTransform;
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestGrab(NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null || netIdentity.connectionToClient != null)
            return;

        if (!netIdentity.AssignClientAuthority(sender))
            return;

        if (localHolding)
        {
            localHolding = false;
            heldHand = null;
            locallyLocked = false;
        }

        RpcSetGrabbed(true);
    }

    [ClientRpc]
    private void RpcSetGrabbed(bool grabbed)
    {
        ApplyHeldState(grabbed);
        if (!grabbed)
        {
            localHolding = false;
            heldHand = null;
            awaitingAuthority = false;
            localRequestPending = false;
        }
    }

    private void BeginLocalHold(Transform hand)
    {
        localHolding = true;
        heldHand = hand;
        previousHandPosition = hand.position;
        handVelocity = Vector3.zero;
        locallyLocked = true;
        ApplyHeldState(true);
    }

    private void ApplyHeldState(bool grabbed)
    {
        locallyLocked = grabbed;
        if (body != null)
        {
            body.isKinematic = grabbed || !NetworkServer.active && (netIdentity == null || !netIdentity.isOwned);
            body.useGravity = !grabbed && (NetworkServer.active || netIdentity != null && netIdentity.isOwned) && originalUseGravity;
        }

        if (networkTransform != null)
        {
            networkTransform.enabled = true;
            networkTransform.syncDirection = grabbed && !NetworkServer.active ? SyncDirection.ClientToServer : SyncDirection.ServerToClient;
        }
    }

    private void LateUpdate()
    {
        if (!localHolding || heldHand == null)
            return;

        if (netIdentity == null || !NetworkServer.active && !netIdentity.isOwned)
        {
            localHolding = false;
            heldHand = null;
            return;
        }

        Vector3 targetPosition = heldHand.position;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));
        transform.rotation = heldHand.rotation;
        handVelocity = (heldHand.position - previousHandPosition) / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        previousHandPosition = heldHand.position;
        if (body != null)
        {
            body.position = transform.position;
            body.rotation = transform.rotation;
        }

        bool stillPressed = heldHand == Player.Instance.leftHandTransform
            ? EasyInputs.GetGripButtonDown(EasyHand.LeftHand)
            : EasyInputs.GetGripButtonDown(EasyHand.RightHand);
        if (!stillPressed)
            ReleaseLocalHold();
    }

    private void ReleaseLocalHold()
    {
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = originalUseGravity;
            body.linearVelocity = handVelocity * releaseVelocityMultiplier;
        }

        localHolding = false;
        heldHand = null;
        if (NetworkServer.active)
            ReleaseServerAuthority();
        else
        {
            localRequestPending = true;
            CmdReleaseGrab();
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdReleaseGrab(NetworkConnectionToClient sender = null)
    {
        if (sender == null || netIdentity.connectionToClient != sender)
            return;
        ReleaseServerAuthority();
    }

    private void ReleaseServerAuthority()
    {
        if (netIdentity != null && netIdentity.connectionToClient != null)
            netIdentity.RemoveClientAuthority();
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = originalUseGravity;
        }
        RpcSetGrabbed(false);
    }
}



