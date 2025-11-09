using Meta.XR.MRUtilityKit;
using UnityEngine;

public class QRCodeTracker : MonoBehaviour
{
    [SerializeField]
    private MRUK MRUtilityKit;
    [SerializeField]
    private GameObject QRCodePrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (MRUtilityKit == null)
        {
            MRUtilityKit = FindFirstObjectByType<MRUK>();
        }

        MRUtilityKit.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTrackableAdded(MRUKTrackable trackable)
    {
        if (trackable.TrackableType == OVRAnchor.TrackableType.QRCode)
        {
            Instantiate(QRCodePrefab, trackable.transform.position, trackable.transform.rotation);
        }
    }
}
