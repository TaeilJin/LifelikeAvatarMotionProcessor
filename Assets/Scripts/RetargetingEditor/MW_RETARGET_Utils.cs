using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using UnityEditor;

public enum RETARGETING_MODE
{
	ALL,
	REALTIME_RETARGETING,
	DATASET_RETARGETING,
	TESTING
}

public class MW_RETARGET_Utils : ScriptableObject
{
	
	public RETARGETING_MODE selectedMode;

	public StringBuilder sb_record = new StringBuilder();
	public WriterClass wr_class = new WriterClass();
	public ImporterClass io_class = new ImporterClass();
	//public static string[][] Joint_Pair_Idx = new string[1][];
	public string jointPairPath;

	public bool b_connect = false;
	public bool b_connect_init_MBS = false;
	public bool b_connect_init_RETARGET = false;
	public bool b_connect_do_retargeting = false;
	public Transform base_offset;
	public class RetargetingGroup{
		public WriterClass _writer_class = new WriterClass();
		public StringBuilder sb_record = new StringBuilder();
		public StreamWriter File_record;

		public Actor actor = null;
		public bool b_is_source = false;
		public Matrix4x4[] Default_local_mat;
		public Quaternion[] Default_world_mat;
		public Matrix4x4[] jointdelta;
		public string MBS_FullName;

	
		// set Actor Class
		public void SetCharacter(Actor _actor)
		{
			//actor = new Actor();

			actor = _actor;
		}
		public bool SetAsTarget()
		{
			b_is_source = false;
			return b_is_source;
		}
		public bool SetAsSource()
		{
			b_is_source = true;
			return b_is_source;
		}

		// calculate local rotation and position using world matrix
		public void CalcLocalFrames(out Matrix4x4[] localmat)
		{
			localmat = new Matrix4x4[actor.Bones.Length]; // local matrix  -> MBS 만들때랑, 조인트 값 업데이트 할때 사용한다.
			//Default_world_mat = new Quaternion[actor.Bones.Length]; // world quaternion -> joint delta 구할 때 사용
			//jointdelta = new Quaternion[actor.Bones.Length]; // joint delta

			for (int j = 0; j < actor.Bones.Length; j++)
			{
				//Default_world_mat[j] = actor.Bones[j].Transform.localToWorldMatrix.GetRotation();
				for (int i = 0; i < actor.Bones[j].Childs.Length; i++)
				{
					int child_index = actor.Bones[j].GetChild(i).Index;
				
					
					Matrix4x4 local = actor.Bones[j].Transform.localToWorldMatrix.inverse * actor.Bones[child_index].Transform.localToWorldMatrix;
					localmat[child_index].SetTRS(local.GetPosition(),local.GetRotation(),Vector3.one);

					
					//Debug.Log("Parent : " + actor.Bones[j].GetName() + " Child: " + actor.Bones[child_index].GetName() 
					//	+ " position " + localmat[child_index].GetPosition() + " rotation " + localmat[child_index].GetRotation());

				}
			}
		}
		// calculate local joint delta from default T-pose frame 
		public void CalcJoinDelta()
        {
			for (int j = 0; j < actor.Bones.Length; j++)
			{ 
				jointdelta[j] = Default_local_mat[j].inverse * jointdelta[j];
				//Debug.Log("Joint Delta : " + actor.Bones[j].GetName()
				//		+ " position " + jointdelta[j].GetPosition() + " rotation " + jointdelta[j].GetRotation());
			}
		}
		// calculate rotation from right-handed oriented to left-handed oriented rotation 
		public Quaternion SET_JOINT_QUATERNION_MW(Quaternion input_Quat)
		{
			Quaternion quat_rH = new Quaternion(input_Quat.x, input_Quat.y * -1.0f, input_Quat.z * -1.0f, input_Quat.w);

			return quat_rH;
		}
		// generate MBS text file using Actor
		public void genMBSTxtFile(string foldername, string i_fileName, Actor i_actor, float scale)
		{

			MBS_FullName = foldername + "/" + i_fileName + ".txt";
			File_record = _writer_class.CreateFile(foldername, i_fileName, false, ".txt");
			sb_record = new StringBuilder();

			File_record.WriteLine("HIERARCHY\n");

			//sb_record = _writer_class.WritePosition(sb_record, i_actor.Bones[0].Transform.position, true);
			Vector3 pos;
			Quaternion quat;

			for (int i = 0; i < i_actor.Bones.Length; i++)
			{
				if (!i_actor.Bones[i].GetName().Contains("Site"))
				{
					if (i != 0)
					{
						pos = Default_local_mat[i].GetPosition() * scale;
						pos.Set(-1 * pos.x, pos.y, pos.z); // to MotionWorks (righthand)

						quat = Default_local_mat[i].GetRotation();
						quat = SET_JOINT_QUATERNION_MW(quat);

					}
					else
					{
						pos = i_actor.Bones[i].Transform.position * scale;
						pos.Set(-1 * pos.x, pos.y, pos.z); // to MotionWorks (righthand)

						quat = i_actor.Bones[i].Transform.rotation;
						quat = SET_JOINT_QUATERNION_MW(quat);
					}


					File_record.WriteLine("LINK");
					File_record.WriteLine("NAME " + i_actor.Bones[i].GetName());

					// Warning: Assume that the root bone index is zero as like getRootPose
					string Joint_Type = "JOINT ACC BALL";
					if (i == 0)
					{
						File_record.WriteLine("REF WORLD");
						Joint_Type = "JOINT ACC FREE";
					}
					else
					{
						//Debug.Log(i + " " + i_actor.Bones[i].GetName());
						File_record.WriteLine("PARENT " + i_actor.Bones[i].GetParent().GetName());
						File_record.WriteLine("REF LOCAL");
					}

					sb_record = _writer_class.WritePosition(sb_record, pos, false);
					File_record.WriteLine("POS " + sb_record.ToString());
					sb_record.Clear();

					sb_record = _writer_class.WriteQuat(sb_record, quat, false);
					File_record.WriteLine("ROT QUAT " + sb_record.ToString());
					sb_record.Clear();

					File_record.WriteLine(Joint_Type);

					File_record.WriteLine("END_LINK\n");

				}
				else
					Debug.LogError("You should erase 'SITE' from the bones !! ");
			}
			File_record.WriteLine("END_HIERARCHY\n");

			File_record.Close();
			sb_record.Clear();


		}

		// do post-processing for base orientation
		public void DoPostProcessing_BaseModifying(Matrix4x4 control_mat)
        {
			Matrix4x4 bone_hip = actor.Bones[0].Transform.GetWorldMatrix();
			bone_hip = bone_hip.GetRelativeTransformationFrom(control_mat);
			actor.Bones[0].Transform.SetPositionAndRotation(bone_hip.GetPosition(), bone_hip.GetRotation());
		}

	}

	public RetargetingGroup RetargetingSource;
	public RetargetingGroup RetargetingTarget;


	//// 

	[Serializable]
	public class DataPacket
	{
		public string text_indicator;
		public string text_mbs_src_txt;
		public string text_mbs_tar_txt;
		public string text_jointmapping_txt;

		public float[] floatArray;
		public float[] base_offset;
	}

	public string InitMBS()
    {
		// 데이터 구성
		DataPacket dataToSend = new DataPacket
		{
			text_indicator = "initMBS",
			text_mbs_src_txt = RetargetingSource.MBS_FullName,
			text_mbs_tar_txt = RetargetingTarget.MBS_FullName,
			text_jointmapping_txt = jointPairPath,
			floatArray = null,
			base_offset = null
		};
		// 데이터를 JSON 문자열로 직렬화하여 전송
		string jsonData = JsonUtility.ToJson(dataToSend);

		return jsonData;
    }

    public string DoRetargeting(Transform base_offset)
    {

		float[] floatArray = new float[RetargetingSource.actor.Bones.Length * 4 + 3];
		for (int j = 0; j < RetargetingSource.actor.Bones.Length; j++)
		{

			if (j == 0)
			{
				Vector3 position = RetargetingSource.actor.Bones[j].Transform.position; // 0 번 joint 일 때는 world position 을 구한다.

				floatArray[0] = position.x;
				floatArray[1] = position.y;
				floatArray[2] = position.z;

				Quaternion rot = RetargetingSource.actor.Bones[j].Transform.rotation; // 0 번 joint 일 때는 world rotation 을 준다.
				floatArray[3 + 4 * j + 0] = rot.x;
				floatArray[3 + 4 * j + 1] = rot.y;
				floatArray[3 + 4 * j + 2] = rot.z;
				floatArray[3 + 4 * j + 3] = rot.w;

			}
			else
			{
				Quaternion rot = RetargetingSource.jointdelta[j].GetRotation();
				floatArray[3 + 4 * j + 0] = rot.x;
				floatArray[3 + 4 * j + 1] = rot.y;
				floatArray[3 + 4 * j + 2] = rot.z;
				floatArray[3 + 4 * j + 3] = rot.w;
			}
		}

		float[] offset_position = new float[3];
		offset_position[0] = base_offset.transform.position.x;
		offset_position[1] = base_offset.transform.position.y;
		offset_position[2] = base_offset.transform.position.z;
		// 데이터 구성
		DataPacket dataToSend = new DataPacket
		{
			text_indicator = "doRetarget",
			text_mbs_src_txt = null,
			text_mbs_tar_txt = null,
			text_jointmapping_txt = null,
			floatArray = floatArray,
			base_offset = offset_position
		};
		// 데이터를 JSON 문자열로 직렬화하여 전송
		string jsonData = JsonUtility.ToJson(dataToSend);

		return jsonData;
    }

    public void UpdateFromJoinDelta(float[] array)
    {
		//Debug.Log(" joint " + RetargetingTarget.actor.Bones.Length + "dof " + array.Length);
		for (int j = 0; j < RetargetingTarget.actor.Bones.Length; j++)
        {
			if (j == 0)
			{
				RetargetingTarget.actor.Bones[0].Transform.position = new Vector3(array[0], array[1], array[2]);
				RetargetingTarget.actor.Bones[0].Transform.rotation = new Quaternion(array[3], array[4], array[5],array[6]);
			}
			else
			{
				//Debug.Log(" joint name " + RetargetingTarget.actor.Bones[j].GetName());
				float x = array[3 + 4 * j + 0];
				float y = array[3 + 4 * j + 1];
				float z = array[3 + 4 * j + 2];
				float w = array[3 + 4 * j + 3];
				Quaternion quat_lH = new Quaternion(x, y, z, w); // delta
				quat_lH = quat_lH.normalized;

				RetargetingTarget.actor.Bones[j].Transform.localRotation = RetargetingTarget.Default_local_mat[j].GetRotation() * quat_lH; 
			}
        }
    }
	public void inspector(Actor source, Actor target, Transform offset)
    {
		base_offset = offset;

		//Setting source and retarget
		EditorGUILayout.BeginHorizontal();
		if (Utility.GUIButton("Retargeting: generate MBS txt & Joint Pair", Color.white, Color.yellow))
		{
			RetargetingSource = new RetargetingGroup();
			RetargetingSource.SetCharacter(source);
			RetargetingSource.SetAsSource();

			//RetargetingSource.Default_local_mat = new Matrix4x4[RetargetingSource.actor.Bones.Length];
			RetargetingSource.CalcLocalFrames(out RetargetingSource.Default_local_mat);

			RetargetingTarget = new RetargetingGroup();
			RetargetingTarget.SetCharacter(target);
			RetargetingTarget.SetAsTarget();

			//RetargetingTarget.Default_local_mat = new Matrix4x4[RetargetingTarget.actor.Bones.Length];
			RetargetingTarget.CalcLocalFrames(out RetargetingTarget.Default_local_mat);

			EditorApplication.delayCall += () =>
			{
				string dataPath = EditorUtility.OpenFolderPanel("BVH Folder", "", "Assets");
				RetargetingSource.genMBSTxtFile(dataPath, RetargetingSource.actor.name, RetargetingSource.actor, 1.0f);
				RetargetingTarget.genMBSTxtFile(dataPath, RetargetingTarget.actor.name, RetargetingTarget.actor, 1.0f);
				Debug.Log("saved : " + RetargetingSource.MBS_FullName);
				Debug.Log("saved : " + RetargetingTarget.MBS_FullName);

			};

			EditorApplication.delayCall += () =>
			{

				jointPairPath = EditorUtility.OpenFilePanel("Overwrite with txt", "", "txt");
				if (!File.Exists(jointPairPath))
				{
					UnityEngine.Debug.Log("File Path(" + jointPairPath + ") Not Exists.");
				}

				//load pairing data
				string[][] Joint_Pair_Idx = new string[1][];
				io_class.ImportStringArrayData(jointPairPath, 3, out Joint_Pair_Idx);
				for (int j = 0; j < Joint_Pair_Idx.Length; j++)
					UnityEngine.Debug.Log("|Pair| |SRC|: " + Joint_Pair_Idx[j][1] + " |TAR|: " + Joint_Pair_Idx[j][2]);
				int num_TargetJoints = Joint_Pair_Idx.Length;
			};

			

		}
		EditorGUILayout.EndHorizontal();
		
		// Play Motion Data
		if (!Application.isPlaying) return;


		// bool connection
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
		b_connect = EditorGUILayout.Toggle("b_connect", b_connect);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		if (Utility.GUIButton("Retargeting: Init MBS ", Color.white, Color.yellow))
		{
			b_connect_init_MBS = true;
		}

		if (selectedMode == RETARGETING_MODE.REALTIME_RETARGETING)
		{
			if (Utility.GUIButton("Retargeting: Do Retargeting ", Color.white, Color.yellow))
			{
				b_connect_do_retargeting = true;
			}

			// bool connection
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
			b_connect_do_retargeting = EditorGUILayout.Toggle("b_retargeting", b_connect_do_retargeting);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

		}


	}	

}
