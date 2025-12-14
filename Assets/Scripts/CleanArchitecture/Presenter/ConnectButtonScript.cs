using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class ConnectButtonScript : MonoBehaviour
{
    [SerializeField]
    Button connectButton;

    void Awake()
    {
        connectButton.onClick.AddListener(() =>
        {
            NetworkManager.singleton.networkAddress = "192.168.1.141";
            NetworkManager.singleton.StartClient();
        });
    }
}
