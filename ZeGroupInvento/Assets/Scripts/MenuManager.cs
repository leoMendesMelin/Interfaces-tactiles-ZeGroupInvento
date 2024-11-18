using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform backgroundPanel;
    private GameObject lastSpawnedObject;

    private void Start()
    {
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        if (backgroundPanel == null)
        {
            backgroundPanel = canvas.GetComponentInChildren<RectTransform>();
        }

        SetupButtons();
    }

    private void Update()
    {
        // Vérifie si la touche N est pressée
        if (Input.GetKeyDown(KeyCode.N))
        {
            ReloadCurrentScene();
        }
    }

    private void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }


    private void SetupButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            string buttonName = button.gameObject.name;
            button.onClick.AddListener(() => SpawnPrefab(buttonName));
        }
    }

    public void SpawnPrefab(string prefabName)
    {
        // Chercher d'abord avec le nom exact
        GameObject prefabToSpawn = System.Array.Find(prefabs, p => p.name.Equals(prefabName));

        // Si non trouvé, chercher avec Contains
        if (prefabToSpawn == null)
        {
            prefabToSpawn = System.Array.Find(prefabs, p => p.name.Contains(prefabName));
        }

        // Si toujours non trouvé, essayer sans le "(Clone)"
        if (prefabToSpawn == null)
        {
            string cleanName = prefabName.Replace("(Clone)", "").Trim();
            prefabToSpawn = System.Array.Find(prefabs, p => p.name.Contains(cleanName));
        }

        if (prefabToSpawn != null)
        {
            lastSpawnedObject = Instantiate(prefabToSpawn, canvas.transform);
            RectTransform rectTransform = lastSpawnedObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;

            UIDragAndResize dragAndResize = lastSpawnedObject.AddComponent<UIDragAndResize>();
            bool isTable = prefabName.Contains("CircleTable") || prefabName.Contains("RectangleTable");
            dragAndResize.SetResizable(!isTable);

            lastSpawnedObject.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogWarning($"Prefab with name {prefabName} not found! Available prefabs:");
            foreach (GameObject prefab in prefabs)
            {
                Debug.LogWarning($"- {prefab.name}");
            }
        }
    }

    public void ListAllPrefabs()
    {
        Debug.Log("Available prefabs:");
        foreach (GameObject prefab in prefabs)
        {
            Debug.Log($"- {prefab.name}");
        }
    }

    public GameObject GetLastSpawnedObject()
    {
        return lastSpawnedObject;
    }

    private GameObject FindPrefabByName(string name)
    {
        foreach (GameObject prefab in prefabs)
        {
            if (prefab.name.ToLower().Contains(name.ToLower()))
            {
                return prefab;
            }
        }
        return null;
    }
}
