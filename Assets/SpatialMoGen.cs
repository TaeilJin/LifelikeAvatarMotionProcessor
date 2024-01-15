using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public class SpatialMoGen : RealTimeAnimation
{
    public CylinderMap Environment;
    private float size = 2f;
    private float resolution = 5;
    private float layers = 10;
    public Actor actor_source;
    public TCPClient _tcpClient;
    public DataPacket dataToSend;

    
    public class DataPacket
    {
        public string text_indicator;
        public float[] floatArray;
        public int nFrame;
    }
    
    public bool b_vis;
    public bool b_space_enable;
    public bool b_init_space;
    protected override void Setup()
    {
        Environment = new CylinderMap(size, (int)resolution, (int)layers, false);
        Debug.Log("Space " + Environment.Points.Length);
        _tcpClient = new TCPClient();
        _tcpClient.Setup("143.248.6.198", 80);

        // 데이터 구성
        dataToSend = new DataPacket
        {
            text_indicator = "initSpace",
            floatArray = null,
            nFrame = 0,
        };

        b_init_space = false;
    }
    protected override void Close()
    {
        
    }

    protected override void Feed()
    {
        if (b_space_enable)
        {
            dataToSend.text_indicator = "Space";
            dataToSend.nFrame = Frame;
            
            // 데이터를 JSON 문자열로 직렬화하여 전송
            string jsonData = JsonUtility.ToJson(dataToSend);

            _tcpClient.SendData(jsonData);
        }
        if(b_init_space)
        {
            dataToSend.text_indicator = "initSpace";
            dataToSend.nFrame = 0;
            // 데이터를 JSON 문자열로 직렬화하여 전송
            _tcpClient.SendData(JsonUtility.ToJson(dataToSend));

            b_init_space = false;
        }
    }

    protected override void Read()
    {
        if (b_space_enable && Frame < 126)
        {
            _tcpClient.ReceiveData(630);

            Environment.Sense(Matrix4x4.identity, LayerMask.GetMask("None"));
            for(int i =0; i < 630; i++)
                Environment.Occupancies[i] = _tcpClient.receivedFloatArray[i];
            
            Frame++;
        }   
    }

    protected override void Postprocess()
    {
        
    }
    protected override void OnGUIDerived()
    {
        
    }

    protected override void OnRenderObjectDerived()
    {
        Environment.Draw(Color.green, true, false);
    }
    
    [CustomEditor(typeof(SpatialMoGen), true)]
    public class SpatialMoGen_Editor : Editor
    {
        public SpatialMoGen Target;
        SerializedProperty selectedOption;

        public void Awake()
        {
            Target = (SpatialMoGen)target;
            selectedOption = serializedObject.FindProperty("selectedOption");
        }
        public override void OnInspectorGUI()
        {
            ///Undo.RecordObject(Target, Target.name);
            Inspector();
            //if (GUI.changed)
            //{
            //    EditorUtility.SetDirty(Target);
            //}
        }
        private void Inspector()
        {
            Utility.ResetGUIColor();
            Utility.SetGUIColor(UltiDraw.LightGrey);

            // Assigning Target Avatar
            EditorGUILayout.BeginVertical();
            Target.actor_source = (Actor)EditorGUILayout.ObjectField("Source Actor", Target.actor_source, typeof(Actor), true);
            EditorGUILayout.EndVertical();

            if (Utility.GUIButton("reset & play animation", Color.white, Color.red))
            {
                Target.b_init_space = true;
                //Target.Frame = 0;
            }
            Target.b_space_enable = EditorGUILayout.Toggle("Space Enable", Target.b_space_enable);
        }
    }
}
