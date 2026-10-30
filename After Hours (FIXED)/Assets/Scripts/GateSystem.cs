using UnityEngine;
using UnityEngine.InputSystem;

public class GateSystem : MonoBehaviour
{
    [Header("Parts")]
    public GameObject Holderdown;
    public GameObject HolderUp;
    public GameObject GateLowest;
    public GameObject GateMiddle;
    public GameObject GateHighest;

    [Header("Customization")]
    public float RollUpSpeed = 1f;
    public float RollDownSpeed = 1f;
    [Tooltip("EDITOR TEST KEY")]
    public Key ToggleKey = Key.E;

    [Header("Holderdown")]
    public float HolderdownTouchOffset = 0f;

    [Header("Pickup (distance Holderdown to part while carrying)")]
    public float CarryGapLowest = 0f;
    public float CarryGapMiddle = 0f;
    public float CarryGapHighest = 0f;

    [Header("Final Position (distance below HolderUp)")]
    public float OffsetLowest = 0.95f;
    public float OffsetMiddle = 0.87f;
    public float OffsetHighest = 0.86f;

    Transform[] parts;
    float[] closedY;
    float[] openY;
    float[] carryGap;

    float progress;
    bool opening;

    void Start()
    {
        parts = new Transform[]
        {
            Holderdown.transform,
            GateLowest.transform,
            GateMiddle.transform,
            GateHighest.transform
        };

        closedY = new float[parts.Length];
        openY = new float[parts.Length];
        carryGap = new float[parts.Length];

        for (int i = 0; i < parts.Length; i++)
            closedY[i] = parts[i].localPosition.y;

        CalcOpenPositions();
    }

    void CalcOpenPositions()
    {
        Transform space = parts[0].parent;
        float topY = space != null
            ? space.InverseTransformPoint(HolderUp.transform.position).y
            : HolderUp.transform.position.y;

        openY[0] = topY - HolderdownTouchOffset;
        openY[1] = topY - OffsetLowest;
        openY[2] = topY - OffsetMiddle;
        openY[3] = topY - OffsetHighest;

        carryGap[1] = CarryGapLowest;
        carryGap[2] = CarryGapMiddle;
        carryGap[3] = CarryGapHighest;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[ToggleKey].wasPressedThisFrame)
            opening = !opening;

        CalcOpenPositions();

        float distance = Mathf.Max(0.0001f, openY[0] - closedY[0]);
        float speed = opening ? RollUpSpeed : RollDownSpeed;
        float target = opening ? 1f : 0f;

        progress = Mathf.MoveTowards(progress, target, (speed / distance) * Time.deltaTime);

        PositionApl();
    }

    void PositionApl()
    {
        float holderY = Mathf.Lerp(closedY[0], openY[0], progress);
        SetY(parts[0], holderY);

        for (int i = 1; i < parts.Length; i++)
        {
            float carried = holderY + carryGap[i];
            float wanted = Mathf.Max(closedY[i], carried);
            wanted = Mathf.Min(wanted, openY[i]);
            SetY(parts[i], wanted);
        }
    }

    static void SetY(Transform t, float y)
    {
        Vector3 p = t.localPosition;
        p.y = y;
        t.localPosition = p;
    }

    public void Open() { opening = true; }
    public void Close() { opening = false; }
    public void Toggle() { opening = !opening; }
}