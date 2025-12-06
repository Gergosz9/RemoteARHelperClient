using Assets.Scripts.QRTracking;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QRCodeTracker : MonoBehaviour
{
    [SerializeField]
    private MRUK MRUtilityKit;
    [SerializeField]
    private GameObject QRCodePrefab;
    [SerializeField]
    private PivotManager pivotManager;
    [SerializeField]
    private bool autoAnchorQRObjects = true;

    public Dictionary<MRUKTrackable, GameObject> trackedQRCodes = new Dictionary<MRUKTrackable, GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (MRUtilityKit == null)
        {
            MRUtilityKit = FindFirstObjectByType<MRUK>();
        }

        if (pivotManager == null)
        {
            pivotManager = FindFirstObjectByType<Assets.Scripts.QRTracking.PivotManager>();
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

                // Add QR code objects to pivot system if enabled and pivot manager exists
                if (autoAnchorQRObjects && pivotManager != null)
                {
                    pivotManager.AddObjectToAnchor(qrCodeObject);
                }
            }
        }
    }
}
