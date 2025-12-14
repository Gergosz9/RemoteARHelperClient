using Assets.Scripts.CleanArchitecture.Domain;
using UnityEngine;
using UnityEngine.UI;

public class SendButtonScript : MonoBehaviour
{
    [SerializeField]
    Button sendButton;

    void Awake()
    {
        sendButton.onClick.AddListener(() =>
        {
            DomainManager domainManager = FindAnyObjectByType<DomainManager>();
            domainManager.sendFileUsecase.SendMesh();
        });
    }   
}
