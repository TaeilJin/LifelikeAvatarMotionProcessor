using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EnvMotionData : ScriptableObject
{
	public Matrix4x4[][] Motion_only = new Matrix4x4[0][];
	public Sequence Sequences;
	public Matrix4x4[] RootTrajectory = new Matrix4x4[0];
	public Matrix4x4[] RootTrajectory_seenByChild = new Matrix4x4[0];
	public int LeftShoulder, RightShoulder, LeftHip, RightHip;
	public Matrix4x4 StartRoot, EndRoot;
	public GameObject ChairMat, DeskMat, StartingRoot;
	
	public Matrix4x4 GetRoot(int index, float y_offset)
	{
		//vector_x
		Vector3 vec_shoulder = Motion_only[index][LeftShoulder].GetPosition() - Motion_only[index][RightShoulder].GetPosition();
		vec_shoulder = vec_shoulder.normalized;
		Vector3 vec_upleg = Motion_only[index][LeftHip].GetPosition() - Motion_only[index][RightHip].GetPosition();
		vec_upleg = vec_upleg.normalized;
		Vector3 vec_across = vec_shoulder + vec_upleg;
		vec_across = vec_across.normalized;
		//vector_forward
		Vector3 vec_forward = Vector3.Cross(-1.0f * vec_across, Vector3.up);
		//vector_x_new
		Vector3 vec_right = Vector3.Cross(-1.0f * vec_forward, Vector3.up);
		//root matrix 
		Matrix4x4 root_interaction = Matrix4x4.identity;
		Vector4 vec_x = new Vector4(vec_right.x, vec_right.y, vec_right.z, 0.0f);
		Vector4 vec_z = new Vector4(vec_forward.x, vec_forward.y, vec_forward.z, 0.0f);
		Vector4 vec_y = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
		Vector3 pos__ = Motion_only[index][0].GetPosition();
		Vector4 pos_h = new Vector4(pos__.x, y_offset, pos__.z, 1.0f);
		root_interaction.SetColumn(0, vec_x); root_interaction.SetColumn(1, vec_y); root_interaction.SetColumn(2, vec_z);
		root_interaction.SetColumn(3, pos_h);
		//
		return root_interaction;
	}

	// generate root trajectory from start root 
	public void GenerateRootTrajectory(Matrix4x4 startMat)
	{
		RootTrajectory[0] = startMat;
		//Debug.Log("RootTR " + RootTr.Length + " Root Vel " + RootVelTr.Length);
		for (int n = 0; n < RootTrajectory_seenByChild.Length; n++)
		{
			RootTrajectory[n + 1] = RootTrajectory_seenByChild[n].GetRelativeTransformationFrom(RootTrajectory[n]);

		}

	}
	// genreate root trajectory of motion
	public void GenerateRootTrajectory(int start, int end)
	{
		RootTrajectory = new Matrix4x4[end - start + 1];
		for (int n = start, k = 0; n <= end; n++, k++)
		{
			RootTrajectory[k] = GetRoot(n, 0.0f);
		}
	}
	// generate root trajectory seen by child root 
	public void GenerateRootRelative()
	{
		RootTrajectory_seenByChild = new Matrix4x4[RootTrajectory.Length - 1];
		for (int n = 0; n < RootTrajectory_seenByChild.Length; n++)
		{
			RootTrajectory_seenByChild[n] = RootTrajectory[n + 1].GetRelativeTransformationTo(RootTrajectory[n]);
		}
	}
	public void inspector()
    {
		EditorGUILayout.BeginHorizontal();
		ChairMat = (GameObject)EditorGUILayout.ObjectField("Chair Matrix", ChairMat, typeof(GameObject), true);
		DeskMat = (GameObject)EditorGUILayout.ObjectField("Desk Matrix", DeskMat, typeof(GameObject), true);
		StartingRoot = (GameObject)EditorGUILayout.ObjectField("StartRoot", StartingRoot, typeof(GameObject), true);
		EditorGUILayout.EndHorizontal();

		
		

		// inspector
		Sequences.Inspector();

		// calculate Root Trajectory
		if (Utility.GUIButton("Human-Object Data : Calc Root Trajectory", Color.white, Color.red))
		{
			GenerateRootTrajectory(Sequences.Start, Sequences.End);
			StartRoot = RootTrajectory.First<Matrix4x4>();
			EndRoot = RootTrajectory.Last<Matrix4x4>();
			Debug.Log("Interesting Motion " + RootTrajectory.Length + " : " + Motion_only.Length);
		}
	}


}
