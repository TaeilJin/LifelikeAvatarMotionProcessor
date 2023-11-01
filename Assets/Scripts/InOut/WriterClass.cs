using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Text;
using System.IO;

public class WriterClass
{
    private StreamWriter File_record;
    private StringBuilder sb_record; // string builder 
    public StreamWriter CreateFile(string foldername, string name, bool newfile, string root_extension)
    {
        string filename = string.Empty;
        string folder = foldername;
        if (!File.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            folder = folder + '/';
        }
        else
            folder = folder + "/";
        if (!File.Exists(folder + name + root_extension))
        {
            filename = folder + name + root_extension;
        }
        else
        {
            if (newfile)
            {
                int i = 1;
                while (File.Exists(folder + name + "_" + i + "_" + root_extension))
                {
                    i += 1;
                }
                filename = folder + name + "_" + i + "_" + root_extension;
            }
            else
                filename = folder + name + root_extension;
        }
        return File.CreateText(filename);
    }
    public StringBuilder WriteFloat(StringBuilder sb_, float x, bool first)
    {
        if (first)
        {
            sb_.Append(x);
        }
        else
        {
            sb_.Append(" ");
            sb_.Append(x);
        }
        return sb_;
    }
    public StringBuilder WriteString(StringBuilder sb_, string x, bool first)
    {
        if (first)
        {
            sb_.Append(x);
        }
        else
        {
            sb_.Append(" ");
            sb_.Append(x);
        }
        return sb_;
    }
    public StringBuilder WritePosition(StringBuilder sb_, Vector3 position, bool first)
    {
        sb_ = WriteFloat(sb_, position.x, first);
        sb_ = WriteFloat(sb_, position.y, false);
        sb_ = WriteFloat(sb_, position.z, false);

        return sb_;
    }
    public StringBuilder WriteQuat(StringBuilder sb_, Quaternion quat, bool first)
    {
        sb_ = WriteFloat(sb_, quat.x, first);
        sb_ = WriteFloat(sb_, quat.y, false);
        sb_ = WriteFloat(sb_, quat.z, false);
        sb_ = WriteFloat(sb_, quat.w, false);

        return sb_;
    }
    public bool WriteMatData(string DirectoryPath, string filename, Matrix4x4 root_mat)
    {
        
        if (Directory.Exists(DirectoryPath))
        {
            Debug.Log("wrtie matrix on " + filename);

            File_record = CreateFile(DirectoryPath, filename,true,".txt");
            sb_record = new StringBuilder();

            sb_record = WritePosition(sb_record, root_mat.GetPosition(), true);
            sb_record = WriteQuat(sb_record, root_mat.GetRotation(), false);

            File_record.WriteLine(sb_record.ToString());

            File_record.Close();
            sb_record.Clear();

            return true;
        }
        else
        {
            return false;
        }
    }
    

}

public class ImporterClass
{
    public bool ImportStringArrayData(string FullName, int rows, out string[][] _outarray)
    {
        string[] Output; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
        Output = FileUtility.ReadAllLines(FullName);

        _outarray = new string[Output.Length][];

        for (int k = 0; k < Output.Length; k++)
        {
            string[] pose_data = FileUtility.ReadStringArray(Output[k]);
            _outarray[k] = new string[rows];
            for (int i = 0; i < rows; i++)
            {
                _outarray[k][i] = pose_data[i];
            }

        }

        return true;

    }
    public bool ImportConnectionData(string message, Actor _actor, Quaternion[] Default_Mat, Matrix4x4 root_mat)
    {
        var splittedStrings = message.Split(' ');
        Debug.Log(" get " + splittedStrings[0] + " " + splittedStrings.Length);
        int total_length = (_actor.Bones.Length * 4 + 3) + 1;
        Debug.Log(" see " + total_length);
        if (splittedStrings[0] == "Hello" && splittedStrings.Length == total_length)
        {
            float x; float y; float z;
            float.TryParse(splittedStrings[1 + 0], out x);
            float.TryParse(splittedStrings[1 + 1], out y);
            float.TryParse(splittedStrings[1 + 2], out z);
            Vector3 pos = new Vector3(x, y, z);

            //_actor.Bones[0].Transform.position = pos / 100.0f;
            _actor.Bones[0].Transform.position = pos;

            for (int j = 0; j < _actor.Bones.Length; j++)
            {
                float w;
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 0], out x);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 1], out y);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 2], out z);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 3], out w);

                Quaternion quat_lH = new Quaternion(x, y, z, w);
                quat_lH = quat_lH.normalized;
                _actor.Bones[j].Transform.localRotation = Default_Mat[j] * quat_lH;

            }

            Matrix4x4 bone_hip = _actor.Bones[0].Transform.GetWorldMatrix();
            bone_hip = bone_hip.GetRelativeTransformationFrom(root_mat);
            _actor.Bones[0].Transform.SetPositionAndRotation(bone_hip.GetPosition(), bone_hip.GetRotation());


            return true;

        }
        else
        {
            Debug.Log("check pose data");
            return false;
        }


    }

}