using Assets.Scripts.CleanArchitecture.Gateways;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.CleanArchitecture.Entities
{
    internal class OffsetManager
    {
        private bool debugMode = true;

        private GameObject pivotObject;
        private bool anchorSet = false;
        private Vector3 firstQRPosition;
        private Quaternion firstQRRotation;
        private MRUKTrackable anchorQRCode;

        private List<GameObject> objectsToAnchor = new List<GameObject>();

        private ITrackableGateway trackableGateway;

        public OffsetManager(ITrackableGateway trackableGateway, bool debugMode = false)
        {
            this.trackableGateway = trackableGateway;
            this.debugMode = debugMode;

            this.trackableGateway.TrackableAdded().AddListener(OnTrackableAdded);
        }


        private void OnTrackableAdded(object trackable)
        {
            // Only process QR codes and only if we haven't set an anchor yet
            if (!anchorSet && trackable is MRUKTrackable qrTrackable && qrTrackable.TrackableType == OVRAnchor.TrackableType.QRCode)
            {
                SetFirstQRAsAnchor(qrTrackable);
            }
        }

        private void SetFirstQRAsAnchor(MRUKTrackable qrTrackable)
        {
            // Store the first QR code's position and rotation
            firstQRPosition = qrTrackable.transform.position;
            firstQRRotation = qrTrackable.transform.rotation;
            anchorQRCode = qrTrackable;

            // Create the pivot object at world origin
            CreatePivotObject();

            // Move existing objects to be children of the pivot
            AnchorExistingObjects();

            anchorSet = true;

            if (debugMode)
            {
                Debug.Log($"PivotManager: First QR code detected at position {firstQRPosition}, creating anchor pivot at origin.");
                string qrPayload = qrTrackable.MarkerPayloadString;
                Debug.Log($"PivotManager: Anchor QR code payload: {qrPayload}");
            }
        }

        private void CreatePivotObject()
        {
            // Create pivot object at world origin (0,0,0)
            pivotObject = new GameObject("QR_Anchor_Pivot");
            pivotObject.transform.position = Vector3.zero;
            pivotObject.transform.rotation = Quaternion.identity;

            // Calculate offset from QR to world origin
            Vector3 offsetFromQRToOrigin = Vector3.zero - firstQRPosition;

            // Apply this offset to the pivot so that when QR moves, world objects stay in place
            pivotObject.transform.position = offsetFromQRToOrigin;

            if (debugMode)
            {
                // Add a visual indicator for debugging
                GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = "Pivot_Debug_Indicator";
                debugSphere.transform.parent = pivotObject.transform;
                debugSphere.transform.localPosition = Vector3.zero;
                debugSphere.transform.localScale = Vector3.one * 0.1f;

                // Make it visible and colored
                Renderer renderer = debugSphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.red;
                }

                // Remove collider as it's just for debugging
                Collider collider = debugSphere.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.Destroy(collider);
                }
            }
        }

        private void AnchorExistingObjects()
        {
            // Move specified objects to be children of the pivot
            foreach (GameObject obj in objectsToAnchor)
            {
                if (obj != null)
                {
                    obj.transform.SetParent(pivotObject.transform, true);
                    if (debugMode)
                    {
                        Debug.Log($"PivotManager: Anchored object '{obj.name}' to pivot.");
                    }
                }
            }
        }

        public void UpdatePivot()
        {
            // Adjust pivot position based on QR movement
            if (anchorSet && anchorQRCode != null && pivotObject != null)
            {
                // Calculate how much the QR has moved from its original position
                Vector3 qrMovement = anchorQRCode.transform.position - firstQRPosition;

                // Apply inverse movement to pivot to keep world objects stable
                Vector3 offsetFromQRToOrigin = Vector3.zero - firstQRPosition;
                pivotObject.transform.position = offsetFromQRToOrigin - qrMovement;

                // Optionally handle rotation drift as well
                Quaternion qrRotationDrift = anchorQRCode.transform.rotation * Quaternion.Inverse(firstQRRotation);
                pivotObject.transform.rotation = Quaternion.Inverse(qrRotationDrift);
            }
        }

        /// <summary>
        /// Add an object to be anchored to the pivot system
        /// </summary>
        /// <param name="obj">GameObject to anchor</param>
        public void AddObjectToAnchor(GameObject obj)
        {
            if (obj != null && !objectsToAnchor.Contains(obj))
            {
                objectsToAnchor.Add(obj);

                // If pivot is already created, immediately anchor this object
                if (anchorSet && pivotObject != null)
                {
                    obj.transform.SetParent(pivotObject.transform, true);
                    if (debugMode)
                    {
                        Debug.Log($"PivotManager: Added and anchored object '{obj.name}' to pivot.");
                    }
                }
            }
        }

        /// <summary>
        /// Remove an object from the pivot system
        /// </summary>
        /// <param name="obj">GameObject to remove from anchor</param>
        public void RemoveObjectFromAnchor(GameObject obj)
        {
            if (obj != null && objectsToAnchor.Contains(obj))
            {
                objectsToAnchor.Remove(obj);
                obj.transform.SetParent(null, true);

                if (debugMode)
                {
                    Debug.Log($"PivotManager: Removed object '{obj.name}' from pivot anchor.");
                }
            }
        }

        /// <summary>
        /// Reset the anchor system (useful for testing)
        /// </summary>
        public void ResetAnchor()
        {
            if (pivotObject != null)
            {
                // Unparent all children first
                while (pivotObject.transform.childCount > 0)
                {
                    pivotObject.transform.GetChild(0).SetParent(null, true);
                }

                Object.Destroy(pivotObject);
            }

            anchorSet = false;
            anchorQRCode = null;
            pivotObject = null;

            if (debugMode)
            {
                Debug.Log("PivotManager: Anchor system reset.");
            }
        }

        /// <summary>
        /// Get the current pivot object (null if not set)
        /// </summary>
        public GameObject GetPivotObject()
        {
            return pivotObject;
        }

        /// <summary>
        /// Check if the anchor has been set
        /// </summary>
        public bool IsAnchorSet()
        {
            return anchorSet;
        }
    }
}
