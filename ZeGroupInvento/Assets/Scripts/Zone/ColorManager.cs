using UnityEngine;
using System.Collections.Generic;

// Cr�ez une nouvelle classe ColorManager
public class ColorManager : MonoBehaviour
{
    private static ColorManager instance;
    public static ColorManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ColorManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ColorManager");
                    instance = go.AddComponent<ColorManager>();
                }
            }
            return instance;
        }
    }

    private List<string> availableColors = new List<string>
    {
        "#FF6B6B", // Rouge clair
        "#4ECDC4", // Turquoise
        "#45B7D1", // Bleu clair
        "#96CEB4", // Vert menthe
        "#FFEEAD", // Jaune p�le
        "#D4A5A5", // Rose poussi�reux
        "#9B59B6", // Violet
        "#3498DB", // Bleu
        "#E67E22", // Orange
        "#2ECC71", // Vert
        "#F1C40F", // Jaune
        "#E74C3C", // Rouge
        "#1ABC9C", // Turquoise fonc�
        "#D35400", // Orange fonc�
        "#8E44AD", // Violet fonc�
        "#2980B9", // Bleu fonc�
        "#27AE60", // Vert fonc�
        "#F39C12"  // Orange dor�
    };

    private List<string> usedColors = new List<string>();

    public string GetUniqueColor()
    {
        if (availableColors.Count == 0)
        {
            // Si toutes les couleurs sont utilis�es, r�initialiser la liste
            ResetColors();
        }

        // Choisir une couleur al�atoire parmi les disponibles
        int randomIndex = Random.Range(0, availableColors.Count);
        string selectedColor = availableColors[randomIndex];

        // D�placer la couleur vers la liste des utilis�es
        availableColors.RemoveAt(randomIndex);
        usedColors.Add(selectedColor);

        return selectedColor;
    }

    public void ReleaseColor(string color)
    {
        if (usedColors.Contains(color))
        {
            usedColors.Remove(color);
            availableColors.Add(color);
        }
    }

    private void ResetColors()
    {
        // Si on a vraiment besoin de plus de couleurs, on peut g�n�rer des variations
        foreach (string usedColor in usedColors)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(usedColor, out color))
            {
                // Cr�er une variation de la couleur
                float hue, saturation, value;
                Color.RGBToHSV(color, out hue, out saturation, out value);
                hue = (hue + 0.2f) % 1f; // D�caler la teinte
                Color newColor = Color.HSVToRGB(hue, saturation, value);
                string newHexColor = "#" + ColorUtility.ToHtmlStringRGB(newColor);
                availableColors.Add(newHexColor);
            }
        }
    }
}