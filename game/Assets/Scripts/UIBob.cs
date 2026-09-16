using UnityEngine;

public class UIBob : MonoBehaviour
{
    [Header("Bob Settings")]
    public float bobHeight = 5f;
    public float bobSpeed = 2f;

    [Header("Wave Settings")]
    [Range(1, 8)]
    public int wavePosition = 1;

    private RectTransform rectTransform;
    private Vector2 startPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        float phaseOffset = (wavePosition - 1) * Mathf.PI / 4f;

        float newY = startPosition.y +
            Mathf.Sin(Time.time * bobSpeed + phaseOffset) * bobHeight;

        rectTransform.anchoredPosition = new Vector2(
            startPosition.x,
            newY
        );
    }
}