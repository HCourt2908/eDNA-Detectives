using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class MicroscopeMinigame : MonoBehaviour
{
    [SerializeField] private Image clearSample;
    [SerializeField] private Image blurredSample;
    [SerializeField] private float targetTolerance = 0.05f;
    [SerializeField] private float minMagnification = 1f;
    [SerializeField] private float maxMagnification = 2.5f;
    [SerializeField] private float scrollSpeed = 0.02f;
    [SerializeField] private float focusRange = 0.5f;
    [SerializeField] private float focusTimeRequired = 2f;
    [SerializeField] private Slider focusProgressBar;
    [SerializeField] private CanvasGroup focusProgressGroup;
    [SerializeField] private MicroscopeGameController microscopeGameController;

    private float focusTimer = 0f;
    private bool completed = false;

    private float targetMagnification;
    private bool inFocus;
    private float magnification = 1f;

    private void OnEnable()
    {
        magnification = minMagnification;

        targetMagnification = Random.Range((minMagnification + maxMagnification) / 2, maxMagnification - targetTolerance);
        
        inFocus = false;

        focusProgressGroup.alpha = 0f;
        focusProgressBar.value = 0f;

        UpdateSample();

    }

    private void Update()
    {
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll != 0)
        {
            magnification += scroll * scrollSpeed;
            magnification = Mathf.Clamp(magnification, minMagnification, maxMagnification);

            UpdateSample();
            CheckFocus();
        }

        if (inFocus)
        {
            focusTimer += Time.deltaTime;

            focusProgressBar.value = focusTimer / focusTimeRequired;

            focusProgressGroup.alpha = Mathf.MoveTowards(focusProgressGroup.alpha, 1f, Time.deltaTime * 5f);

            if (focusTimer >= focusTimeRequired)
            {
                CompleteMinigame();
            }
        } else 
        {
            focusTimer = 0f;
            focusProgressGroup.alpha = Mathf.MoveTowards(focusProgressGroup.alpha, 0f, Time.deltaTime * 5f);
        }
    }

    private void UpdateSample()
    {
        clearSample.rectTransform.localScale = Vector3.one * magnification;
        blurredSample.rectTransform.localScale = Vector3.one * magnification;

        float difference = Mathf.Abs(magnification - targetMagnification);

        float clarity = 1f - (difference / focusRange);
        clarity = Mathf.Clamp01(clarity);

        Color color = clearSample.color;
        color.a = clarity;
        clearSample.color = color;
    }

    private void CheckFocus()
    {
        float difference = Mathf.Abs(magnification - targetMagnification);

        if (difference <= targetTolerance)
        {
            inFocus = true;
        } else
        {
            inFocus = false;
        }
    }

    private void CompleteMinigame()
    {
        completed = true;
        microscopeGameController.CloseMicroscope();
    }
}
