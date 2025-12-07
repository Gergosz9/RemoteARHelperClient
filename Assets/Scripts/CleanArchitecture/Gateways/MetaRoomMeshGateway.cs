using Meta.XR.BuildingBlocks;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.CleanArchitecture.Gateways
{
    internal class MetaRoomMeshGateway : IRoomMeshGateway
    {
        RoomMeshEvent roomMeshEvent;
        private UnityEvent<MeshFilter> onRoomMeshCreated = new UnityEvent<MeshFilter>();

        public MetaRoomMeshGateway()
        {
            roomMeshEvent = Object.FindFirstObjectByType<RoomMeshEvent>();

            if (roomMeshEvent != null)
            {
                roomMeshEvent.OnRoomMeshLoadCompleted.AddListener(OnRoomMeshCreated);
            }
            else
            {
                Debug.LogError("MetaRoomMeshGateway: RoomMeshEvent component not found in the scene.");
            }
        }

        UnityEvent<MeshFilter> IRoomMeshGateway.RoomMeshCreated()
        {
            return onRoomMeshCreated;
        }

        private void OnRoomMeshCreated(MeshFilter meshFilter)
        {
            onRoomMeshCreated.Invoke(meshFilter);
        }

        public void Dispose()
        {
            if (roomMeshEvent != null)
            {
                roomMeshEvent.OnRoomMeshLoadCompleted.RemoveListener(OnRoomMeshCreated);
            }
        }
    }
}
