using Assets.Scripts.CleanArchitecture.Entities;
using Assets.Scripts.CleanArchitecture.Gateways;
using Assets.Scripts.CleanArchitecture.Presenter;
using Assets.Scripts.CleanArchitecture.Usecases;
using UnityEngine;

namespace Assets.Scripts.CleanArchitecture.Domain
{
    internal class DomainManager : MonoBehaviour
    {
        public FileSender fileSender { get; private set; }
        public OffsetManager offsetManager { get; private set; }
        public RoomMeshExporter roomMeshExporter { get; private set; }

        public IRoomMeshGateway roomMeshGateway { get; private set; }
        public ITrackableGateway trackableGateway { get; private set; }

        public ExportMeshUsecase exportMeshUsecase { get; private set; }
        public OffsetUsecases offsetUsecases { get; private set; }
        public SendFileUsecase sendFileUsecase { get; private set; }

        [SerializeField]
        QRCodeVisualizer qrCodeVisualizer;
        [SerializeField]
        bool ExportMeshOnLoad = true;
        [SerializeField]
        bool SendFileOnExport = true;

        private void Awake()
        {
            // Initialize gateways
            roomMeshGateway = new MetaRoomMeshGateway();
            trackableGateway = new MetaTrackableGateway();
            // Initialize entities
            roomMeshExporter = new RoomMeshExporter(roomMeshGateway, ExportMeshOnLoad);
            offsetManager = new OffsetManager(trackableGateway, debugMode: true);
            fileSender = new FileSender(roomMeshExporter, SendFileOnExport);
            // Initialize use cases
            exportMeshUsecase = new ExportMeshUsecase(roomMeshExporter);
            offsetUsecases = new OffsetUsecases(offsetManager);
            sendFileUsecase = new SendFileUsecase(fileSender);
            // Link use cases to presenter
            qrCodeVisualizer.offsetUsecases = offsetUsecases;
        }
    }
}
