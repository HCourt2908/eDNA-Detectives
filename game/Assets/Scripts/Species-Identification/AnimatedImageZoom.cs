
using System.Collections;
using NUnit.Framework.Constraints;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedImageZoom : MonoBehaviour
{
    
    [Header("Zoom UI")]
    public GameObject zoomOverlay;
    public Image enlargedImage;
    public CanvasGroup canvasGroup;

    [Header("Animation")]
    public float animationDuration = 0.3f;

    [Header("MaximumSize")]
    public float maxWidth;
    public float maxHeight;

    private RectTransform enlargedRect;
    private RectTransform originalRect;

    private Vector2 originalPosition;
    private Vector2 originalSize;

    private bool isOpen = false;
    private bool isAnimating = false;

    private void Awake()
    {
        enlargedRect = enlargedImage.GetComponent<RectTransform>();
        maxWidth = enlargedRect.rect.width;
        maxHeight = enlargedRect.rect.height;

        zoomOverlay.SetActive(false);
        
    }

    public void OpenImage()
    {
        if (isAnimating || isOpen)
        {
            return;
        }

        Image clickedImage = GetComponent<Image>();

        if (clickedImage.sprite == null) return;

        originalRect = clickedImage.GetComponent<RectTransform>();

        originalPosition = originalRect.position;
        originalSize = originalRect.rect.size;

        enlargedImage.sprite = clickedImage.sprite;

        zoomOverlay.SetActive(true);

        enlargedRect.anchoredPosition = originalPosition;
        enlargedRect.sizeDelta = originalSize;

        canvasGroup.alpha = 0f;

        StartCoroutine(ZoomIn());
    }

    public void CloseImage()
    {
        if (isAnimating || !isOpen)
        {
            return;
        }

        StartCoroutine(ZoomOut());
    }

    private IEnumerator ZoomIn()
    {
        isAnimating = true;

        float elapsed = 0f;

        Vector2 startPosition = originalPosition;
        Vector2 startSize = originalSize;

        Vector2 targetPosition = Vector2.zero;

        Vector2 targetSize = new Vector2(maxWidth, maxHeight);

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / animationDuration;

            t = Mathf.SmoothStep(0f, 1f, t);

            enlargedRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            enlargedRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        enlargedRect.anchoredPosition = targetPosition;
        enlargedRect.sizeDelta = targetSize;

        canvasGroup.alpha = 1f;

        isOpen = true;
        isAnimating = false;
    }

    private IEnumerator ZoomOut()
    {
        isAnimating = true;

        float elapsed = 0f;

        Vector2 startPosition = enlargedRect.anchoredPosition;
        Vector2 startSize = enlargedRect.sizeDelta;

        Vector2 targetPosition = originalPosition;
        Vector2 targetSize = originalSize;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / animationDuration;

            t = Mathf.SmoothStep(0f, 1f, t);

            enlargedRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            enlargedRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);

            canvasGroup.alpha  = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        enlargedRect.anchoredPosition = targetPosition;
        enlargedRect.sizeDelta = targetSize;

        canvasGroup.alpha = 0f;

        zoomOverlay.SetActive(false);

        isOpen = false;
        isAnimating = false;
    }

}
