using Assets.Scripts.CleanArchitecture.Domain;
using UnityEngine;
using UnityEngine.UI;

public class ExportButtonScript : MonoBehaviour
{
    [SerializeField]
    Button exportButton;

    public void Awake()
    {
        exportButton.onClick.AddListener(() =>
        {
            DomainManager domainManager = FindAnyObjectByType<DomainManager>();
            domainManager.exportMeshUsecase.Execute();
        });
    }
}
