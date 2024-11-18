using UnityEngine;
using System.Collections.Generic;

public class CardHandLayout : MonoBehaviour
{
    public float baseOverlap = -30f;   // Base overlap between cards
    public float maxOverlap = -25f;    // Maximum overlap between cards when there are many
    public float maxCurve = 30f;       // Maximum curve (rotation) angle for outermost cards
    public float curveHeight = 4f;    // How much to raise the outer cards for a curved effect

    private List<Transform> cardTransforms = new List<Transform>();

    void Start()
    {
        GetChildTransforms();
        LayoutCards();
    }

    public void UpdateLayout()
    {
        GetChildTransforms();
        LayoutCards();
    }

    void GetChildTransforms()
    {
        cardTransforms.Clear();
        foreach (Transform child in transform)
        {
            cardTransforms.Add(child);
        }
    }

    void LayoutCards()
    {
        int totalCards = cardTransforms.Count;
        if (totalCards == 0) return;

        // Calculate dynamic overlap and curve based on the number of cards
        float overlapAmount = Mathf.Lerp(baseOverlap, maxOverlap, totalCards / 10f);  // More overlap for more cards
        float curveAmount = maxCurve / totalCards;  // Curve increases with more cards

        float middleIndex = (totalCards - 1) / 2f;

        for (int i = 0; i < totalCards; i++)
        {
            // Calculate position and rotation for each card
            float xPos = (i - middleIndex) * overlapAmount;
            float curveOffset = Mathf.Abs(i - middleIndex) * curveHeight;
            float rotationAngle = (i - middleIndex) * curveAmount;

            // Set the card's position and rotation using RectTransform
            RectTransform cardRectTransform = cardTransforms[i].GetComponent<RectTransform>();
            if (cardRectTransform != null)
            {
                cardRectTransform.anchoredPosition = new Vector2(xPos, -curveOffset);  // Lower outer cards slightly
                cardRectTransform.localRotation = Quaternion.Euler(0, 0, rotationAngle);

                // Ensure correct drawing order
                cardRectTransform.SetSiblingIndex(i);
            }
        }
    }
}
