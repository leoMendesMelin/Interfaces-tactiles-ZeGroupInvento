using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DisabledInteractionFeedback : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private MonoBehaviour interactionScript; // Reference au script ElementDragHandler ou ZoneControllerPrefab

    private Vector3 originalScale;
    private bool isAnimating = false;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactionScript != null && !interactionScript.enabled && !isAnimating)
        {
            StartCoroutine(ShowFeedback());
        }
    }

    private IEnumerator ShowFeedback()
    {
        isAnimating = true;

        // Réduction
        float duration = 0.1f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float scale = Mathf.Lerp(1f, 0.9f, elapsedTime / duration);
            transform.localScale = originalScale * scale;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Rebond
        duration = 0.2f;
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float scale = Mathf.Lerp(0.9f, 1.05f, elapsedTime / duration);
            transform.localScale = originalScale * scale;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Retour à la normale
        duration = 0.1f;
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float scale = Mathf.Lerp(1.05f, 1f, elapsedTime / duration);
            transform.localScale = originalScale * scale;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        isAnimating = false;
    }
}