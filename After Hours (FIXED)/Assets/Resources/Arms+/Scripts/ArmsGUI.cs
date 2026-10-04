#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ArmPlus.Editor
{
    [CustomEditor(typeof(ArmsPlus))]
    public class ArmsGUIPlus : UnityEditor.Editor
    {
        private ArmsPlus script;
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Setup", "IK Control", "Newton IK", "Debug" };
        private Texture2D logo;
        private Vector2 debugScrollPos;

        private GUIStyle headerStyle;
        private GUIStyle tabStyle;
        private GUIStyle activeTabStyle;
        private GUIStyle boxStyle;
        private GUIStyle boldLabelStyle;
        private GUIStyle creditsStyle;

        void OnEnable()
        {
            script = (ArmsPlus)target;
            if (!logo)
                logo = Resources.Load<Texture2D>("Arms+/Assets/Arms+");
        }

        void InitStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            creditsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };

            tabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30,
                normal = { background = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f, 0.8f)) },
                hover = { background = MakeTex(1, 1, new Color(0.4f, 0.4f, 0.4f, 0.8f)) }
            };

            activeTabStyle = new GUIStyle(tabStyle)
            {
                normal = { background = MakeTex(1, 1, new Color(0.2f, 0.7f, 0.9f, 0.9f)) },
                hover = { background = MakeTex(1, 1, new Color(0.2f, 0.7f, 0.9f, 0.9f)) }
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            boldLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            DrawHeader();
            DrawTabs();
            DrawTabContent();
            DrawCredits();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            if (logo)
            {
                var rect = GUILayoutUtility.GetRect(0, 200, GUILayout.ExpandWidth(true));
                float aspect = (float)logo.width / logo.height;
                float logoWidth = Mathf.Min(rect.width, logo.height * aspect);
                float logoHeight = logoWidth / aspect;
                var logoRect = new Rect(
                    rect.x + (rect.width - logoWidth) / 2,
                    rect.y + (rect.height - logoHeight) / 2,
                    logoWidth,
                    logoHeight
                );

                GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit);
            }
            else
            {
                GUILayout.Label("Arms+", headerStyle, GUILayout.Height(40));
            }

            DrawStatusInfo();
            EditorGUILayout.EndVertical();
        }

        void DrawStatusInfo()
        {
            if (!Application.isPlaying) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            bool hasTarget = script.armChain.target != null;
            bool reachable = script.IsTargetReachable();

            var statusColor = hasTarget ? (reachable ? Color.green : Color.yellow) : Color.red;
            var prevColor = GUI.color;
            GUI.color = statusColor;

            string status = hasTarget ? (reachable ? "● Target Reachable" : "● Target Too Far") : "● No Target";
            GUILayout.Label(status, boldLabelStyle);
            GUI.color = prevColor;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < tabNames.Length; i++)
            {
                var style = selectedTab == i ? activeTabStyle : tabStyle;
                if (GUILayout.Button(tabNames[i], style))
                    selectedTab = i;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        void DrawTabContent()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            switch (selectedTab)
            {
                case 0: DrawSetupTab(); break;
                case 1: DrawIKControlTab(); break;
                case 2: DrawNewtonIKTab(); break;
                case 3: DrawDebugTab(); break;
            }

            EditorGUILayout.EndVertical();
        }

        void DrawSetupTab()
        {
            GUILayout.Label("Arm Chain Setup", boldLabelStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Arm Bones (Index 0 = Upper Arm)", boldLabelStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("armChain.bones"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Target Setup", boldLabelStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("armChain.target"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("armChain.pole"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);
            if (GUILayout.Button("Auto Set Up Arm", GUILayout.Height(35)))
            {
                script.AutoSetUpArm();
                EditorUtility.SetDirty(script);
            }

            EditorGUILayout.Space(10);
            GUILayout.Label("Stretchy Arms", boldLabelStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("armChain.strechy"), new GUIContent("Enable Stretchy Arms"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("armChain.strechMultiplier"), new GUIContent("Stretch Multiplier"));

            EditorGUILayout.Space(10);
            GUILayout.Label("Visualization", boldLabelStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showGizmos"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chainColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSize"));
        }

        void DrawIKControlTab()
        {
            GUILayout.Label("Inverse Kinematics Control", boldLabelStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("armChain.enableIK"));
            EditorGUILayout.Space(5);

            GUILayout.Label("IK Weight", boldLabelStyle);
            script.armChain.ikWeight = EditorGUILayout.Slider(script.armChain.ikWeight, 0f, 1f);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("armChain.iterations"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("armChain.amount"));
            EditorGUILayout.EndVertical();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(15);
                DrawIKButtons();
                EditorGUILayout.Space(10);
                DrawReachInfo();
            }
        }

        void DrawNewtonIKTab()
        {
            GUILayout.Label("Newton IK Settings", boldLabelStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useNewtonStyle"), new GUIContent("Enable Newton IK"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("armChain.smoothSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("armChain.snapBackStrength"));
            EditorGUILayout.EndVertical();
        }

        void DrawDebugTab()
        {
            GUILayout.Label("Debug Information", boldLabelStyle);
            EditorGUILayout.Space(5);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Debug info only available during play mode", MessageType.Info);
                return;
            }

            debugScrollPos = EditorGUILayout.BeginScrollView(debugScrollPos, GUILayout.Height(200));
            EditorGUILayout.LabelField($"Target Reachable: {script.IsTargetReachable()}");
            EditorGUILayout.LabelField($"Reach Percentage: {script.GetReachPercentage():P}");
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Refresh Inspector", GUILayout.Height(30)))
                Repaint();
        }

        void DrawIKButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable IK", GUILayout.Height(30)))
            {
                script.armChain.enableIK = true;
                script.armChain.ikWeight = 1f;
            }
            if (GUILayout.Button("Disable IK", GUILayout.Height(30)))
            {
                script.armChain.enableIK = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Reset Arm Position", GUILayout.Height(30)))
            {
                script.ResetArmPosition();
            }
        }

        void DrawReachInfo()
        {
            if (!script.armChain.target) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Reach Analysis", boldLabelStyle);

            bool reachable = script.IsTargetReachable();
            float reachPercent = script.GetReachPercentage();

            var prevColor = GUI.color;
            GUI.color = reachable ? Color.green : Color.red;
            EditorGUILayout.LabelField($"Status: {(reachable ? "REACHABLE" : "TOO FAR")}");
            GUI.color = prevColor;

            EditorGUILayout.LabelField($"Reach: {reachPercent:P}");
            Rect rect = GUILayoutUtility.GetRect(0, 20);
            EditorGUI.ProgressBar(rect, reachPercent, $"{reachPercent:P}");
            EditorGUILayout.EndVertical();
        }

        void DrawCredits()
        {
            GUILayout.Label("Credits: Keo.CS (scripts), Peacefull (Newton IK Base + Idea)", creditsStyle);
        }

        Texture2D MakeTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        void OnInspectorUpdate()
        {
            if (Application.isPlaying)
                Repaint();
        }
    }
}
#endif
