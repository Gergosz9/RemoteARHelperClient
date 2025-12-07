using Assets.Scripts.CleanArchitecture.Entities;
using UnityEngine;

namespace Assets.Scripts.CleanArchitecture.Usecases
{
    internal class OffsetUsecases
    {
        private OffsetManager offsetManager;

        public OffsetUsecases(OffsetManager offsetManager)
        {
            this.offsetManager = offsetManager;
        }

        public void UpdatePivot()
        {
            offsetManager.UpdatePivot();
        }

        public void ResetPivot()
        {
            offsetManager.ResetAnchor();
        }

        public void AddObjectToAnchor(GameObject obj)
        {
            offsetManager.AddObjectToAnchor(obj);
        }
        
        public void RemoveObjectFromAnchor(GameObject obj)
        {
            offsetManager.RemoveObjectFromAnchor(obj);
        }
    }
}
