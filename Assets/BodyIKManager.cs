using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BodyIKManager : RealTimeAnimation
{
    //Using Python Connection
    public TCPClient _tcpClient;
    public MotionData_Type selectedOption;
    //Using Motion Data Input
    public MotionDataFile _MotionData;
    //Using IK 
    public IK_Utils _IK;
    //Actor
    public Actor actor_source;
    protected override void Setup()
    {
        _tcpClient = new TCPClient();
        _tcpClient.Setup("143.248.6.198", 80);
        _MotionData = ScriptableObject.CreateInstance<MotionDataFile>();
        _IK = ScriptableObject.CreateInstance<IK_Utils>();
        
    }

    protected override void Close()
    {
        //throw new System.NotImplementedException();
    }

    protected override void Feed()
    {
        //throw new System.NotImplementedException();
    }

    protected override void OnGUIDerived()
    {
        //throw new System.NotImplementedException();
    }

    protected override void OnRenderObjectDerived()
    {
        //throw new System.NotImplementedException();
    }

    protected override void Postprocess()
    {
        //throw new System.NotImplementedException();
    }

    protected override void Read()
    {
        //throw new System.NotImplementedException();
    }


    [CustomEditor(typeof(BodyIKManager), true)]
    public class BodyIKManager_Editor : Editor
    {
        public BodyIKManager Target;
        SerializedProperty selectedOption;
        SerializedProperty selectedMode;
        public void Awake()
        {
            Target = (BodyIKManager)target;
            selectedOption = serializedObject.FindProperty("selectedOption");
            selectedMode = serializedObject.FindProperty("selectedMode");
        }

        public override void OnInspectorGUI()
        {
            inspector();
        }

        void inspector()
        {
            Utility.ResetGUIColor();
            Utility.SetGUIColor(UltiDraw.LightGrey);

            // Assigning Target Avatar
            EditorGUILayout.BeginVertical();
            Target.actor_source = (Actor)EditorGUILayout.ObjectField("Source Actor", Target.actor_source, typeof(Actor), true);
            EditorGUILayout.EndVertical();

            // Motion Data Type Selection
            EditorGUILayout.PropertyField(selectedOption, true);
            serializedObject.ApplyModifiedProperties();

            if (Target._MotionData != null)
            {
                if (Target.selectedOption == MotionData_Type.ALL ||
                    Target.selectedOption == MotionData_Type.FBX_MOTIONTEXT
                    || Target.selectedOption == MotionData_Type.FBX)
                {
                    // Motion Data inspector
                    Target._MotionData.FBX_inspector(Target.actor_source);
                    if (Target._MotionData.FBXFiles != null && Target._MotionData.FBXFiles.Length > 0)
                        Target.TotalFrames = Target._MotionData.FBXFiles[Target._MotionData.selectedData].GetTotalFrames() - 1;
                }

                if (Target.selectedOption == MotionData_Type.ALL ||
                    Target.selectedOption == MotionData_Type.BVH_MOTIONTEXT ||
                    Target.selectedOption == MotionData_Type.BVH)
                {
                    // BVH Data inspector
                    Target._MotionData.BVH_inspector();
                    if (Target._MotionData.BVHFiles != null && Target._MotionData.BVHFiles.Length > 0)
                        Target.TotalFrames = Target._MotionData.BVHFiles[Target._MotionData.selectedData].GetTotalFrames() - 1;
                }


                if (Target.selectedOption == MotionData_Type.ALL ||
                    Target.selectedOption == MotionData_Type.BVH_MOTIONTEXT ||
                    Target.selectedOption == MotionData_Type.FBX_MOTIONTEXT ||
                    Target.selectedOption == MotionData_Type.MOTIONTEXT)
                {
                    // Motion Text Data inspector
                    Target._MotionData.MotionTextFile_inspector(Target.actor_source);
                    if (Target._MotionData.MotionTextFiles != null && Target._MotionData.MotionTextFiles.Length > 0)
                        Target.TotalFrames = Target._MotionData.MotionTextFiles[Target._MotionData.selectedData].GetTotalFrames() - 1;
                }
            }

            // Play Motion Data
            if (!Application.isPlaying) return;

            if (Utility.GUIButton("reset & play animation", Color.white, Color.red))
            {
                Target.Frame = 0;
                Target.play_data = true;
            }
            if (Target.play_data == false)
            {
                if (Utility.GUIButton("re-play animation", Color.white, Color.red))
                {
                    Target.play_data = true;
                }
            }
            if (Target.play_data == true)
            {
                if (Utility.GUIButton("pause animation", Color.white, Color.red))
                {
                    Target.b_data = true;
                    Target.play_data = false;
                }
            }
            if (Target.play_data != true && Target._MotionData.BVHFiles != null)
                Target.Frame = EditorGUILayout.IntSlider(Target.Frame, 1, Target._MotionData.Motion.Length - 1);
        }
    }

}