// PrefabBase.cs - Script de base pour tous les prefabs
using UnityEngine;

public class PrefabBase : MonoBehaviour
{
    protected SpriteRenderer spriteRenderer;
    protected BoxCollider2D boxCollider;
    protected Color baseColor = new Color(0.8f, 0.8f, 0.8f); // Gris clair

    protected virtual void Awake()
    {
        SetupComponents();
    }

    protected virtual void SetupComponents()
    {
        // Ajout du SpriteRenderer s'il n'existe pas
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        // Ajout du BoxCollider2D s'il n'existe pas
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider2D>();

        // Configuration par défaut
        spriteRenderer.color = baseColor;
        spriteRenderer.sortingOrder = 1;
    }
}
