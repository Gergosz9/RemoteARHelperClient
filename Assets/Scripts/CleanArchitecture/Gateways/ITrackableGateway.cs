using UnityEngine.Events;


namespace Assets.Scripts.CleanArchitecture.Gateways
{
    internal interface ITrackableGateway
    {
        public UnityEvent<object> TrackableAdded();
        public UnityEvent<object> TrackableRemoved();
    }
}
