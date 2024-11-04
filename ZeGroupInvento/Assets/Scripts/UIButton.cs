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

        // Initialiser la position de d�part si c'est le premier prefab
        if (itemCount == 0)
        {
            nextPosition = new Vector2(startX, startY);
        }
    }

    private void OnClick()
    {
        // Appeler SpawnPrefab du MenuManager
        menuManager.SpawnPrefab(prefabName);

        // R�cup�rer l'objet qui vient d'�tre spawn�
        GameObject spawnedObject = menuManager.GetLastSpawnedObject();

        if (spawnedObject != null)
        {
            // Positionner l'objet � la position calcul�e
            RectTransform rectTransform = spawnedObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = nextPosition;
            }

            // Incr�menter le compteur et mettre � jour la prochaine position
            itemCount++;
            UpdateNextPosition();
        }
    }

    private void UpdateNextPosition()
    {
        // Passer � la ligne suivante si on atteint le maximum d'items par ligne
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

    // M�thode statique pour r�initialiser les positions si n�cessaire
    public static void ResetPositions(float startX, float startY)
    {
        nextPosition = new Vector2(startX, startY);
        itemCount = 0;
    }

    // Pour nettoyer quand la sc�ne est d�charg�e
    private void OnDestroy()
    {
        if (gameObject.scene.isLoaded)
        {
            ResetPositions(startX, startY);
        }
    }
}