using Assets.Scripts.CleanArchitecture.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CleanArchitecture.Usecases
{
    internal class ExportMeshUsecase
    {
        private RoomMeshExporter roomMeshExporter;

        public ExportMeshUsecase(RoomMeshExporter roomMeshExporter)
        {
            this.roomMeshExporter = roomMeshExporter;
        }

        public void Execute()
        {
            _ = roomMeshExporter.ExtractAndExportMesh();
        }
    }
}
