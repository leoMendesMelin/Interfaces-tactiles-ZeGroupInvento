using UnityEngine;
using UnityEngine.UI;


public class UIButton : MonoBehaviour
{
    [SerializeField] private string prefabName;
    private MenuManager menuManager;

    private void Start()
    {
        menuManager = FindObjectOfType<MenuManager>();
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        menuManager.SpawnPrefab(prefabName);
    }
}