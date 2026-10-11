using easyInputs;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class GrababbleObject : MonoBehaviour
{
    public _GrabPoint _grabPoint;
    [Header("If custom then you need this")]
    public Transform _customGrabPoint;
    public UnityEvent _onGrabObject;
    public UnityEvent _onReleaseObject;
    public EasyHand _hand;

    public enum _GrabPoint
    {
        Hand,
        Piviot,
        Custom
    }
}
