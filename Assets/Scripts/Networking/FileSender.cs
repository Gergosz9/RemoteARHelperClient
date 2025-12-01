using Mirror;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public struct FileChunkMessage : NetworkMessage
{
    public int fileId;
    public int totalChunks;
    public int chunkIndex;
    public byte[] data;
}


public class FileSender : MonoBehaviour
{
    private const int _chunkSize = 16000;

    private const string _receivedFileName = "received_file.obj";

    public void SendFileToAll(string filePath)
    {
        SendFileToServer(File.ReadAllBytes(filePath));
    }

    public void SendFileToTarget(NetworkConnectionToClient target)
    {
        byte[][] chunks = SplitFile(File.ReadAllBytes(_receivedFileName));
        int id = UnityEngine.Random.Range(0, int.MaxValue);

        for (int i = 0; i < chunks.Length; i++)
        {
            FileChunkMessage msg = new FileChunkMessage
            {
                fileId = id,
                totalChunks = chunks.Length,
                chunkIndex = i,
                data = chunks[i]
            };

            target.Send(msg);
        }
    }

    private void SendFileToServer(byte[] file)
    {
        byte[][] chunks = SplitFile(file);
        int id = UnityEngine.Random.Range(0, int.MaxValue);

        for (int i = 0; i < chunks.Length; i++)
        {
            FileChunkMessage msg = new FileChunkMessage
            {
                fileId = id,
                totalChunks = chunks.Length,
                chunkIndex = i,
                data = chunks[i]
            };

            NetworkClient.Send(msg);
        }
    }
    
    private byte[][] SplitFile(byte[] data)
    {
        int numOfChunks = Mathf.CeilToInt((float)data.Length / _chunkSize);
        byte[][] result = new byte[numOfChunks][];

        for (int i = 0; i < numOfChunks; i++)
        {
            int size = Mathf.Min(_chunkSize, data.Length - (i * _chunkSize));
            result[i] = new byte[size];
            Buffer.BlockCopy(data, i * _chunkSize, result[i], 0, size);
        }

        return result;
    }

}
