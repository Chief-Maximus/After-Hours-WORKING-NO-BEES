using UnityEngine;
using easyInputs;
using Photon.Pun;
using System.Collections.Generic;

public class GrabbHand : MonoBehaviourPun
{
    [Header("Hand Settings")]
    public EasyHand _hand;
    public float _grabRange = 0.056f;

    [Header("Grabbing Settings")]
    public LayerMask _grabLayer = -1;

    [Header("Test Grab")]
    public bool _testGrab = false;

    [Header("* Optional")]
    public AudioClip _grabSound;
    public AudioClip _releaseSound;

    [Header("Photon Sync")]
    public _NetworkType _networkType;
    public enum _NetworkType
    {
        Dont_Network,
        Steal,
        Dont_Steal
    }

    GrababbleObject _cGrabbed;
    GrababbleObject _nearestGrabbable;
    AudioSource _audioPlayer;
    Rigidbody _rb;
    //idk why it gets displayed that they arent used but they are pluh
    Vector3 _posOff;
    Quaternion _rotOff;
    bool _wasGrabbing = false;

    private void Start()
    {
        _audioPlayer = GetComponent<AudioSource>();
        if (!_audioPlayer)
        {
            _audioPlayer = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        bool isGrabbing = EasyInputs.GetGripButtonDown(_hand) || _testGrab;

        if (isGrabbing && !_wasGrabbing)
        {
            TryGrabObject();
        }
        else if (!isGrabbing && _wasGrabbing)
        {
            ReleaseObject();
        }

        if (!_cGrabbed)
        {
            UpdateNearestGrabbable();
        }

        _wasGrabbing = isGrabbing;
    }

    private void UpdateNearestGrabbable()
    {
        GrababbleObject _close = null;
        float _closestGrabShitThingyIForgo = float.MaxValue;

        Collider[] colliders = Physics.OverlapSphere(transform.position, _grabRange, _grabLayer);

        foreach (Collider col in colliders)
        {
            GrababbleObject grabbable = col.GetComponent<GrababbleObject>();
            if (grabbable)
            {
                float _dist = Vector3.Distance(transform.position, col.transform.position);
                if (_dist < _closestGrabShitThingyIForgo)
                {
                    _close = grabbable;
                    _closestGrabShitThingyIForgo = _dist;
                }
            }
        }

        if (_nearestGrabbable != _close)
        {
            _nearestGrabbable = _close;
        }
    }

    private void TryGrabObject()
    {
        if (_nearestGrabbable &!_cGrabbed)
        {
            PhotonView targetPV = _nearestGrabbable.GetComponent<PhotonView>();
            if (targetPV && !targetPV.IsMine)
            {
                if (_networkType == _NetworkType.Dont_Network)
                {
                    return;
                }

                if (_networkType == _NetworkType.Dont_Steal && targetPV.Owner != PhotonNetwork.LocalPlayer)
                {
                    return;
                }

                if (_networkType == _NetworkType.Steal)
                {
                    targetPV.RequestOwnership();
                }
            }

            _cGrabbed = _nearestGrabbable;
            _rb = _cGrabbed.GetComponent<Rigidbody>();

            if (_rb)
            {
                SetupTransformGrab();

                _cGrabbed._onGrabObject.Invoke();
                _cGrabbed._hand = _hand;

                if (_grabSound && _audioPlayer)
                {
                    _audioPlayer.PlayOneShot(_grabSound);
                }
            }
        }
    }

    private void SetupTransformGrab()
    {
        _rb.isKinematic = true;
        _rb.transform.SetParent(transform);

        if (_cGrabbed._grabPoint == GrababbleObject._GrabPoint.Hand)
        {
            _posOff = transform.InverseTransformPoint(_rb.transform.position);
            _rotOff = Quaternion.Inverse(transform.rotation) * _rb.transform.rotation;
        }
        else if (_cGrabbed._grabPoint == GrababbleObject._GrabPoint.Piviot)
        {
            _rb.transform.localPosition = Vector3.zero;
            _rb.transform.localRotation = Quaternion.identity;
        }
        else if (_cGrabbed._grabPoint == GrababbleObject._GrabPoint.Custom && _cGrabbed._customGrabPoint)
        {
            Vector3 worldGrabPoint = _cGrabbed._customGrabPoint.position;
            Quaternion worldGrabRot = _cGrabbed._customGrabPoint.rotation;

            _rb.transform.position = worldGrabPoint;
            _rb.transform.rotation = worldGrabRot;
        }
    }

    private void ReleaseObject()
    {
        if (_cGrabbed)
        {
            if (_rb)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
                _rb.transform.SetParent(null); // maybe shoul have doned original parent but womp womp

                Vector3 _handVel = EasyInputs.GetDeviceVelocity(_hand);
                _rb.linearVelocity = _handVel;
            }

            _cGrabbed._onReleaseObject.Invoke();

            if (_releaseSound && _audioPlayer)
            {
                _audioPlayer.PlayOneShot(_releaseSound);
            }

            _cGrabbed = null;
            _rb = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _grabRange);

        if (_nearestGrabbable)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _nearestGrabbable.transform.position);
        }
    }

    public void ForceRelease()
    {
        ReleaseObject();
    }
}
