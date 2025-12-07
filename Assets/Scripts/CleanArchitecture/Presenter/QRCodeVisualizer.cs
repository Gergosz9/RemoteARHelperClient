using Assets.Scripts.CleanArchitecture.Usecases;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.CleanArchitecture.Presenter
{
    internal class QRCodeVisualizer : MonoBehaviour
    {
        [SerializeField]
        private MRUK MRUtilityKit;
        [SerializeField]
        private GameObject QRCodePrefab;

        public OffsetUsecases offsetUsecases;

        public Dictionary<MRUKTrackable, GameObject> trackedQRCodes = new Dictionary<MRUKTrackable, GameObject>();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (MRUtilityKit == null)
            {
                MRUtilityKit = FindFirstObjectByType<MRUK>();
            }

            MRUtilityKit.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);
        }

        private void Update()
        {
            foreach (var trackedQRCode in trackedQRCodes)
            {
                trackedQRCode.Value.transform.position = trackedQRCode.Key.transform.position;
                trackedQRCode.Value.transform.rotation = trackedQRCode.Key.transform.rotation;
            }

            offsetUsecases?.UpdatePivot();
        }

        private void OnTrackableAdded(MRUKTrackable trackable)
        {
            if (trackable.TrackableType == OVRAnchor.TrackableType.QRCode)
            {
                if (!trackedQRCodes.ContainsKey(trackable))
                {
                    GameObject qrCodeObject = Instantiate(QRCodePrefab, trackable.transform.position, trackable.transform.rotation);
                    trackedQRCodes.Add(trackable, qrCodeObject);

                    string qrPayload = trackable.MarkerPayloadString;
                    Transform canvasChild = qrCodeObject.transform.GetChild(0);
                    TextMeshProUGUI tmpComponent = canvasChild.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmpComponent != null)
                    {
                        tmpComponent.text = qrPayload;
                    }
                }
            }
        }
    }
}
