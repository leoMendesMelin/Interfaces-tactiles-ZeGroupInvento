using UnityEngine;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    [SerializeField] private string prefabName;
    [SerializeField] private float spacing = 200f;
    [SerializeField] private float startX = 100f;
    [SerializeField] private float startY = 100f;
    [SerializeField] private int maxItemsPerRow = 4;

    private MenuManager menuManager;
    private static Vector2 nextPosition;
    private static int itemCount = 0;

    private void Start()
    {
        menuManager = FindObjectOfType<MenuManager>();
        GetComponent<Button>().onClick.AddListener(OnClick);

        // Initialiser la position de départ si c'est le premier prefab
        if (itemCount == 0)
        {
            nextPosition = new Vector2(startX, startY);
        }
    }

    private void OnClick()
    {
        // Appeler SpawnPrefab du MenuManager
        menuManager.SpawnPrefab(prefabName);

        // Récupérer l'objet qui vient d'être spawné
        GameObject spawnedObject = menuManager.GetLastSpawnedObject();

        if (spawnedObject != null)
        {
            // Positionner l'objet à la position calculée
            RectTransform rectTransform = spawnedObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = nextPosition;
            }

            // Incrémenter le compteur et mettre à jour la prochaine position
            itemCount++;
            UpdateNextPosition();
        }
    }

    private void UpdateNextPosition()
    {
        // Passer à la ligne suivante si on atteint le maximum d'items par ligne
        if (itemCount % maxItemsPerRow == 0)
        {
            nextPosition.x = startX;
            nextPosition.y -= spacing;
        }
        else
        {
            nextPosition.x += spacing;
        }
    }

    // Méthode statique pour réinitialiser les positions si nécessaire
    public static void ResetPositions(float startX, float startY)
    {
        nextPosition = new Vector2(startX, startY);
        itemCount = 0;
    }

    // Pour nettoyer quand la scène est déchargée
    private void OnDestroy()
    {
        if (gameObject.scene.isLoaded)
        {
            ResetPositions(startX, startY);
        }
    }
}