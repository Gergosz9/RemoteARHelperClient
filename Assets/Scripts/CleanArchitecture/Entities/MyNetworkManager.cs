using Assets.Scripts.CleanArchitecture.Entities;
using Mirror;
using System.Collections.Generic;
using UnityEngine;


public class MyNetworkManager : NetworkManager
{
    private List<Color> _availableColors = new List<Color>()
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    private Dictionary<NetworkConnectionToClient, Color> _assignedColors = new Dictionary<NetworkConnectionToClient, Color>();

    private bool _fileSent = false;

    /*
        public override void OnStartServer()
        {
            base.OnStartServer();

            NetworkServer.RegisterHandler<FileChunkMessage>(FindFirstObjectByType<FileSender>().OnReceiveFileChunkFromClient);
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);

            _assignedColors.Add(conn, _availableColors[0]);
            _availableColors.RemoveAt(0);
        }
    */

    public override void OnStartClient()
    {
        base.OnStartClient();

        // NetworkClient.RegisterHandler<FileChunkMessage>(FindFirstObjectByType<FileSender>().OnReceiveChunk);
    }
    /*
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        if (_fileSent)
        {
            FindFirstObjectByType<FileSender>().SendFileToTarget(conn);
        }
    }
    
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        foreach (var identity in NetworkServer.spawned.Values)
        {
            if (identity == null || (identity == conn.identity && identity.connectionToClient == conn))
            {
                continue;
            }

            if (identity.connectionToClient == conn)
            {
                identity.RemoveClientAuthority();

                var handle = identity.gameObject.GetComponent<ObjectHandle>();
                if (handle != null)
                {
                    handle.ChangeOwnerColor(Color.white);
                }
            }
        }

        base.OnServerDisconnect(conn);

        _availableColors.Add(_assignedColors[conn]);
        _assignedColors.Remove(conn);
    }
    */
    public Color GetColor(NetworkConnectionToClient conn)
    {
        return _assignedColors[conn];
    }

    public void FileSent()
    {
        _fileSent = true;
    }
}