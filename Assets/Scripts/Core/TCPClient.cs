using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class TCPClient : ScriptableObject
{
    private TcpClient client;
    private NetworkStream stream;
    //public string serverAddress;// = "���� IP �ּ�"; // Python ������ IP �ּ�
    //public int serverPort;// = ��Ʈ ��ȣ; // Python ������ ��Ʈ ��ȣ
    [Serializable]
    public class DataPacket
    {
        public string text_indicator;
        public string text_mbs_src_txt;
        public string text_mbs_tar_txt;
        public string text_jointmapping_txt;
        public float[] floatArray;
    }

    public float[] receivedFloatArray;
    public void Setup(string serverAddress, int serverPort)
    {
        try
        {
            client = new TcpClient(serverAddress, serverPort);
            stream = client.GetStream();
            Debug.Log("���� ����");

            receivedFloatArray = new float[22 * 4 + 3];
            // Ŭ���̾�Ʈ �ʱ�ȭ �� ������ �޽��� ����
            //SendToServer("apple")

        }
        catch (Exception e)
        {
            Debug.LogError("���� ����: " + e.Message);
        }
    }

    public void SendData(string data)
    {
        byte[] dataBytes = Encoding.ASCII.GetBytes(data);
        stream.Write(dataBytes, 0, dataBytes.Length);
    }

    public void ReceiveData(int dof)
    {
        // ������ ���� �� ó��
        // �����κ��� ������ ����
        byte[] receivedData = new byte[(dof) * sizeof(float)];
        int bytesRead = stream.Read(receivedData, 0, receivedData.Length);
        if (bytesRead > 0)
        {
            //Debug.Log(bytesRead);
            receivedFloatArray = new float[bytesRead / sizeof(float)];
            Buffer.BlockCopy(receivedData, 0, receivedFloatArray, 0, bytesRead);
            //Debug.Log("Received float array from Python:" + receivedFloatArray.Length);
            //foreach (float value in receivedFloatArray)
            //{
            //    Debug.Log(value);
            //}
        }
    }

    private void SendToServer(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);

        // �������� ó���� ������ ����
        byte[] responseBuffer = new byte[1024];
        int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
        string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);
        Debug.Log("���������� ����: " + response);
    }

    public void OnDestroy()
    {
        if (client != null)
        {
            stream.Close();
            client.Close();
        }
    }
}
