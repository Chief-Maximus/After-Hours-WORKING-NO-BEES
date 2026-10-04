#if !APlus
#define APlus
#endif

using UnityEngine;
using System.Collections.Generic;

namespace ArmPlus
{
    [System.Serializable]
    public class ArmChain
    {
        public List<Transform> bones = new List<Transform>();
        public Transform target;
        public Transform pole;
        public bool enableIK = true;
        public int iterations = 10;
        public float amount = 0.01f;
        [Range(0, 1)] public float ikWeight = 1f;
        [Range(0, 1)] public float smoothSpeed = 0.557f;
        [Range(0, 1)] public float snapBackStrength = 1f;
        public bool useTargetRotation = true;

        public bool strechy = false;
        [Range(0f, 2f)] public float strechMultiplier = 1f;
    }

    public class ArmsPlus : MonoBehaviour
    {
        [Header("Arm Setup")]
        public ArmChain armChain = new ArmChain();

        [Header("IK Settings")]
        public bool showGizmos = true;
        public Color chainColor = Color.blue;
        public Color targetColor = Color.red;
        public float gizmoSize = 0.05f;
        public float tL;

        [Header("Newton IK Style")]
        public bool useNewtonStyle = false;

        Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
        Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();
        Vector3[] poses;
        Vector3[] smoothPos;
        Quaternion[] smoothRots;
        float[] lengths;
        Vector3[] startDirections;
        Quaternion[] startRotations;
        Quaternion startTargetRotation;
        Transform root;
        bool initialized = false;

        void Start()
        {
            InitializeIK();
        }

        void Update()
        {
            if (armChain.enableIK && armChain.target && armChain.bones.Count > 1)
            {
                if (useNewtonStyle)
                    SolveNewtonIK();
                else
                    SolveIK();
            }
        }

        void InitializeIK()
        {
            if (armChain.bones.Count < 2) return;

            originalPositions.Clear();
            originalRotations.Clear();

            foreach (Transform bone in armChain.bones)
            {
                if (bone)
                {
                    originalPositions[bone] = bone.localPosition;
                    originalRotations[bone] = bone.rotation;
                }
            }

            int bonersCount = armChain.bones.Count; //hehe
            poses = new Vector3[bonersCount];
            smoothPos = new Vector3[bonersCount];
            smoothRots = new Quaternion[bonersCount];
            lengths = new float[bonersCount - 1];
            startDirections = new Vector3[bonersCount];
            startRotations = new Quaternion[bonersCount];
            tL = 0; //total Lengh tf

            root = transform;
            for (int i = 0; i < bonersCount; i++)
            {
                if (root && root.parent)
                    root = root.parent;
            }

            for (int i = 0; i < bonersCount; i++)
            {
                poses[i] = GetPositionRootSpace(armChain.bones[i]);
                smoothPos[i] = poses[i];
                startRotations[i] = GetRotationRootSpace(armChain.bones[i]);
                smoothRots[i] = armChain.bones[i].rotation;

                if (i == bonersCount - 1)
                {
                    if (armChain.target)
                        startDirections[i] = GetPositionRootSpace(armChain.target) - GetPositionRootSpace(armChain.bones[i]);
                    else
                        startDirections[i] = Vector3.forward;
                }
                else
                {
                    startDirections[i] = GetPositionRootSpace(armChain.bones[i + 1]) - GetPositionRootSpace(armChain.bones[i]);
                    lengths[i] = startDirections[i].magnitude;
                    tL += lengths[i];
                }
            }

            if (armChain.target)
                startTargetRotation = GetRotationRootSpace(armChain.target);

            initialized = true;
        }

        void SolveNewtonIK() // credits Peaceful (https://discord.gg/jX2x6GR3MU) join frfr
        {
            if (!initialized || !armChain.target) return;

            for (int i = 0; i < armChain.bones.Count; i++)
                poses[i] = GetPositionRootSpace(armChain.bones[i]);

            Vector3 targetPos = GetPositionRootSpace(armChain.target);
            Quaternion targetRot = GetRotationRootSpace(armChain.target);

            float dist = (targetPos - poses[0]).magnitude;

            if (dist > tL && armChain.strechy)
            {
                float stretchFactor = dist / tL * armChain.strechMultiplier;
                Vector3 dir = (targetPos - poses[0]).normalized;
                poses[0] = poses[0];
                for (int i = 1; i < poses.Length; i++)
                    poses[i] = poses[i - 1] + dir * lengths[i - 1] * stretchFactor;

                ApplyNewtonResults(targetRot);
                return;
            }
            else if (dist > tL)
            {
                Vector3 dir = (targetPos - poses[0]).normalized;
                for (int i = 1; i < poses.Length; i++)
                    poses[i] = poses[i - 1] + dir * lengths[i - 1];
            }
            else
            {
                for (int i = 0; i < poses.Length - 1; i++)
                    poses[i + 1] = Vector3.Lerp(poses[i + 1], poses[i] + startDirections[i], armChain.snapBackStrength);

                for (int its = 0; its < armChain.iterations; its++)
                {
                    for (int i = poses.Length - 1; i > 0; i--)
                    {
                        if (i == poses.Length - 1)
                            poses[i] = targetPos;
                        else
                            poses[i] = poses[i + 1] + (poses[i] - poses[i + 1]).normalized * lengths[i];
                    }

                    for (int i = 1; i < poses.Length; i++)
                        poses[i] = poses[i - 1] + (poses[i] - poses[i - 1]).normalized * lengths[i - 1];

                    if ((poses[poses.Length - 1] - targetPos).sqrMagnitude < armChain.amount * armChain.amount)
                        break;
                }
            }

            if (armChain.pole)
            {
                Vector3 polePos = GetPositionRootSpace(armChain.pole);
                for (int i = 1; i < poses.Length - 1; i++)
                {
                    Plane plane = new Plane(poses[i + 1] - poses[i - 1], poses[i - 1]);
                    Vector3 projectedPole = plane.ClosestPointOnPlane(polePos);
                    Vector3 projectedBone = plane.ClosestPointOnPlane(poses[i]);
                    float angle = Vector3.SignedAngle(projectedBone - poses[i - 1], projectedPole - poses[i - 1], plane.normal);
                    poses[i] = Quaternion.AngleAxis(angle, plane.normal) * (poses[i] - poses[i - 1]) + poses[i - 1];
                }
            }

            ApplyNewtonResults(targetRot);
        }


        void ApplyNewtonResults(Quaternion targetRot)
        {
            //sSD (smooth Speed Delta)
            float sSD = Mathf.Clamp01(armChain.smoothSpeed * Time.deltaTime * 60f);

            for (int i = 0; i < poses.Length; i++)
            {
                smoothPos[i] = Vector3.Lerp(smoothPos[i], poses[i], sSD);

                Vector3 finalPos = Vector3.Lerp(GetPositionRootSpace(armChain.bones[i]), smoothPos[i], armChain.ikWeight);

                Quaternion ikRot;
                if (i == poses.Length - 1 && armChain.useTargetRotation)
                {
                    ikRot = Quaternion.Inverse(targetRot) * startTargetRotation * Quaternion.Inverse(startRotations[i]);
                }
                else if (i < poses.Length - 1)
                {
                    ikRot = Quaternion.FromToRotation(startDirections[i], smoothPos[i + 1] - smoothPos[i]) * Quaternion.Inverse(startRotations[i]);
                }
                else
                {
                    ikRot = Quaternion.identity;
                }

                Quaternion finalRot = Quaternion.Lerp(originalRotations[armChain.bones[i]], ikRot, armChain.ikWeight);
                smoothRots[i] = Quaternion.Slerp(smoothRots[i], finalRot, sSD);

                SetPositionRootSpace(armChain.bones[i], finalPos);
                SetRotationRootSpace(armChain.bones[i], smoothRots[i]);
            }
        }

        void SolveIK()
        {
            if (armChain.bones.Count < 2 || !armChain.target) return;

            Vector3 targetPos = armChain.target.position;
            Quaternion targetRot = armChain.target.rotation;
            Transform root = armChain.bones[0];

            float dist = Vector3.Distance(root.position, targetPos);
            if (dist > tL)
            {
                StretchToTarget(targetPos, targetRot);
                return;
            }

            if (dist > tL && armChain.strechy)
            {
                float stretchFactor = dist / tL * armChain.strechMultiplier;
                Vector3 dir = (targetPos - root.position).normalized;
                for (int i = 0; i < poses.Length; i++)
                {
                    if (i == 0) poses[i] = root.position;
                    else poses[i] = poses[i - 1] + dir * lengths[i - 1] * stretchFactor;
                }
                ApplyIKResults(targetRot);
                return;
            }

            for (int its = 0; its < armChain.iterations; its++) //its comming, the itterations are close
            {
                poses[poses.Length - 1] = Vector3.Lerp(poses[poses.Length - 1], targetPos, armChain.ikWeight);

                for (int i = poses.Length - 2; i >= 0; i--)
                {
                    Vector3 dir = (poses[i] - poses[i + 1]).normalized;
                    poses[i] = poses[i + 1] + dir * lengths[i];
                }

                poses[0] = root.position;

                for (int i = 1; i < poses.Length; i++)
                {
                    Vector3 dir = (poses[i] - poses[i - 1]).normalized;
                    poses[i] = poses[i - 1] + dir * lengths[i - 1];
                }

                if (Vector3.Distance(poses[poses.Length - 1], targetPos) < armChain.amount)
                    break;
            }

            ApplyIKResults(targetRot);
        }

        void StretchToTarget(Vector3 targetPos, Quaternion targetRot)
        {
            Vector3 dir = (targetPos - armChain.bones[0].position).normalized;
            float cDist = 0;

            poses[0] = armChain.bones[0].position;

            for (int i = 1; i < poses.Length; i++)
            {
                cDist += lengths[i - 1];
                poses[i] = Vector3.Lerp(poses[i], armChain.bones[0].position + dir * cDist, armChain.ikWeight);
            }

            ApplyIKResults(targetRot);
        }

        void ApplyIKResults(Quaternion targetRot)
        {
            for (int i = 0; i < armChain.bones.Count - 1; i++)
            {
                if (!armChain.bones[i] || !armChain.bones[i + 1]) continue;

                Vector3 targetDir = (poses[i + 1] - poses[i]).normalized;
                Vector3 currentDir = (armChain.bones[i + 1].position - armChain.bones[i].position).normalized;

                if (targetDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.FromToRotation(currentDir, targetDir) * armChain.bones[i].rotation;
                    armChain.bones[i].rotation = Quaternion.Lerp(armChain.bones[i].rotation, targetRotation, armChain.ikWeight);
                }
            }

            if (armChain.useTargetRotation && armChain.bones.Count > 0)
            {
                Transform endBone = armChain.bones[armChain.bones.Count - 1];
                endBone.rotation = Quaternion.Lerp(endBone.rotation, targetRot, armChain.ikWeight);
            }
        }

        public void AutoSetUpArm(int boneCount = 3)
        {
            if (armChain.bones.Count == 0) return;

            Transform current = armChain.bones[0];
            armChain.bones.Clear();

            int a = 0; //https://www.youtube.com/watch?v=nT0RnbJ8n14
            while (current && a < boneCount)
            {
                armChain.bones.Add(current);
                current = current.childCount > 0 ? current.GetChild(0) : null;
                a++;
            }

            InitializeIK();
        }

        [ContextMenu("Reset Arm Position")]
        public void ResetArmPosition()
        {
            foreach (var kvp in originalPositions)
            {
                if (kvp.Key)
                    kvp.Key.localPosition = kvp.Value;
            }

            foreach (var kvp in originalRotations)
            {
                if (kvp.Key)
                    kvp.Key.rotation = kvp.Value;
            }
        }

        public bool IsTargetReachable()
        {
            if (!armChain.target || armChain.bones.Count < 2) return false;
            float dist = Vector3.Distance(armChain.bones[0].position, armChain.target.position);
            return dist <= tL;
        }

        public float GetReachPercentage()
        {
            if (!armChain.target || armChain.bones.Count < 2) return 0f;
            float dist = Vector3.Distance(armChain.bones[0].position, armChain.target.position);
            return Mathf.Clamp01(dist / tL);
        }

        Vector3 GetPositionRootSpace(Transform current)
        {
            if (!root) return current.position;
            return Quaternion.Inverse(root.rotation) * (current.position - root.position);
        }

        void SetPositionRootSpace(Transform current, Vector3 pos)
        {
            if (!root) current.position = pos;
            else current.position = root.rotation * pos + root.position;
        }

        Quaternion GetRotationRootSpace(Transform current)
        {
            if (!root) return current.rotation;
            return Quaternion.Inverse(current.rotation) * root.rotation;
        }

        void SetRotationRootSpace(Transform current, Quaternion rot)
        {
            if (!root) current.rotation = rot;
            else current.rotation = root.rotation * rot;
        }

        void OnDrawGizmos()
        {
            if (!showGizmos) return;
            DrawArmChain();
            DrawTarget();
            DrawPole();
        }

        void DrawArmChain()
        {
            if (armChain.bones == null || armChain.bones.Count < 2) return;

            Gizmos.color = chainColor;

            for (int i = 0; i < armChain.bones.Count; i++)
            {
                if (!armChain.bones[i]) continue;

                Gizmos.DrawSphere(armChain.bones[i].position, gizmoSize);

                if (i < armChain.bones.Count - 1 && armChain.bones[i + 1])
                    Gizmos.DrawLine(armChain.bones[i].position, armChain.bones[i + 1].position);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(armChain.bones[i].position + Vector3.up * 0.1f, $"Bone {i}");
#endif
            }

            if (armChain.bones.Count > 0 && armChain.bones[0])
            {
                Gizmos.color = chainColor * 0.3f;
                Gizmos.DrawWireSphere(armChain.bones[0].position, tL);
            }
        }

        void DrawTarget()
        {
            if (!armChain.target) return;

            bool reachable = IsTargetReachable();
            Gizmos.color = reachable ? targetColor : Color.red * 0.5f;

            Gizmos.DrawSphere(armChain.target.position, gizmoSize * 1.5f);
            Gizmos.DrawWireSphere(armChain.target.position, gizmoSize * 2f);

            if (armChain.bones.Count > 0 && armChain.bones[armChain.bones.Count - 1])
            {
                Gizmos.color = reachable ? Color.green : Color.red;
                Gizmos.DrawLine(armChain.bones[armChain.bones.Count - 1].position, armChain.target.position);
            }
        }

        void DrawPole() // north pole
        {
            if (!armChain.pole) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(armChain.pole.position, gizmoSize * 0.8f);

            if (armChain.bones.Count > 1)
                Gizmos.DrawLine(armChain.bones[1].position, armChain.pole.position);
        }

        void OnValidate()
        {
            if (Application.isPlaying)
                InitializeIK();
        }
    }
}