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
    public Actor actor_target;
    protected override void Setup()
    {
        _tcpClient = new TCPClient();
        _tcpClient.Setup("143.248.6.198", 80);
        _MotionData = ScriptableObject.CreateInstance<MotionDataFile>();
        _MotionData.b_play = false;
        _MotionData.b_data = false;
        _IK = ScriptableObject.CreateInstance<IK_Utils>();
        _IK.IK_Target = ScriptableObject.CreateInstance<MBS>();

    }

    protected override void Close()
    {
        //throw new System.NotImplementedException();
    }

    protected override void Feed()
    {
        if (_MotionData.b_data && _MotionData.FBXFiles[_MotionData.selectedData].Motion != null)
        {
            if (_MotionData.CurFrame > TotalFrames)
            {
                _MotionData.CurFrame = 0;
                _MotionData.b_play = false;
            }
            // update pose
            if (selectedOption == MotionData_Type.ALL || selectedOption == MotionData_Type.FBX_MOTIONTEXT || selectedOption == MotionData_Type.FBX)
            {
                //Debug.Log("frame " + frameIdx + " / " + _MotionData.FBXFiles[_MotionData.selectedData].Motion.Length + " select " + _MotionData.FBXFiles[_MotionData.selectedData].Character.Bones.Length);
                update_pose(_MotionData.CurFrame, _MotionData.FBXFiles[_MotionData.selectedData].Motion, actor_source);
            }
            // do play
            if(_MotionData.b_play)
                _MotionData.CurFrame++;

        }
        
        if (_IK.b_connect_init_MBS)
        {
            string jsonData = _IK.InitMBS();


            _tcpClient.SendData(jsonData);


            _IK.b_connect_init_MBS = false;
        }

        if (_IK.b_connect_set_Pose)
        {
            // desired
            int cnt = 0;
            for (int i = 0; i < actor_source.Bones.Length; i++)
            {
                _IK.PointDes[i] = 1.0f;
                _IK.PointWeights[i] = 1.0f;
                _IK.DesPoints[i] = actor_source.Bones[i].Transform.position;
                
                if (i != 0)
                {
                    _IK.DirDes[cnt] = 1.0f;
                    _IK.DirWeights[cnt] = 1.0f;
                    _IK.DesDirs[cnt++] = actor_source.Bones[i].Transform.position - actor_source.Bones[i].GetParent().Transform.position;
                }
                
             }

            string jsonData = _IK.SetDesiredPositionDirectionArr();

            _tcpClient.SendData(jsonData);

            //_IK.b_connect_set_Points = false;
        }

        // do Retargeting
        if (_IK.b_connect_IK)
        {
            //if (play_data && selectedOption == MotionData_Type.FBX_MOTIONTEXT)
            //   _retargetingUtil.RetargetingSource.actor = _MotionData.FBXFiles[_MotionData.selectedData].Character;
            //_IK.IK_Target.CalcLocalFrames(out _IK.IK_Target.jointdelta);
            //_IK.IK_Target.CalcJoinDelta();

            string jsonData = _IK.doIK();

            _tcpClient.SendData(jsonData);

            
        }

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
        if (_IK.b_connect == true && _IK.b_connect_IK == true)
        {
            _tcpClient.ReceiveData(_IK.IK_Target.actor.Bones.Length * 4 + 3);

            _IK.IK_Target.UpdateFromJoinDelta(_tcpClient.receivedFloatArray);
            
            //_IK.b_connect_IK = false;
        }
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
            Target.actor_target = (Actor)EditorGUILayout.ObjectField("Target Actor", Target.actor_target, typeof(Actor), true);
            EditorGUILayout.EndVertical();

            
            

            // Play Motion Data
            if (!Application.isPlaying) return;

            if (Target.actor_source != null)
                Target._IK.inspector(Target.actor_target);

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
            if (Target._MotionData != null)
            {
                Target._MotionData.Inspector_PlayMode(out Target.Frame);
            }
        }
    }

}