using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Behaviour for the map objects authored in the CTD scene.
/// All visual layout lives in the scene hierarchy, so designers can move or resize
/// the panels, seamount, markers, and button directly with Unity's Rect Tool.
/// </summary>
public class RosetteMapHub : MonoBehaviour
{
    [Serializable]
    public class LocationProfile
    {
        public string locationId = "Waypoint-01";
        public string conductivity = "84.250 mS/cm";
        public string temperature = "25.60 °C";
        public string depth = "221 m";
        [TextArea] public string habitats = "Deep coral colonies\nEndemic fauna hotspots\nGeothermal vents";
    }

    [Header("Scene objects")]
    [Tooltip("The MapPanel GameObject in the Canvas. Adjust all visual positions below this object in the scene.")]
    public GameObject rootPanel;
    public Button[] waypointButtons;
    public GameObject idleInformation;
    public GameObject locationInformation;
    public Button deployButton;

    [Header("Location information")]
    public TMP_Text locationIdText;
    public TMP_Text conductivityText;
    public TMP_Text temperatureText;
    public TMP_Text depthText;
    public TMP_Text habitatsText;
    public LocationProfile[] locations;

    public event Action<int> ReadyToDeploy;

    private int selectedLocation = -1;

    private void Awake()
    {
        for (int index = 0; index < waypointButtons.Length; index++)
        {
            int capturedIndex = index;
            waypointButtons[index].onClick.AddListener(() => SelectLocation(capturedIndex));
        }

        if (deployButton != null)
        {
            deployButton.onClick.AddListener(DeploySelectedLocation);
        }

        ShowIdleInformation();
    }

    public void Show()
    {
        rootPanel.SetActive(true);
        ShowIdleInformation();
    }

    public void Hide()
    {
        rootPanel.SetActive(false);
    }

    private void SelectLocation(int index)
    {
        if (index < 0 || index >= locations.Length)
        {
            return;
        }

        selectedLocation = index;
        LocationProfile profile = locations[index];
        locationIdText.text = profile.locationId;
        conductivityText.text = profile.conductivity;
        temperatureText.text = profile.temperature;
        depthText.text = profile.depth;
        habitatsText.text = profile.habitats;

        idleInformation.SetActive(false);
        locationInformation.SetActive(true);
        deployButton.interactable = true;

        for (int buttonIndex = 0; buttonIndex < waypointButtons.Length; buttonIndex++)
        {
            Image markerImage = waypointButtons[buttonIndex].GetComponent<Image>();
            if (markerImage != null)
            {
                markerImage.color = buttonIndex == selectedLocation
                    ? new Color(0.25f, 1f, 0.72f, 1f)
                    : Color.white;
            }
        }
    }

    private void ShowIdleInformation()
    {
        selectedLocation = -1;
        idleInformation.SetActive(true);
        locationInformation.SetActive(false);
        deployButton.interactable = false;

        foreach (Button button in waypointButtons)
        {
            Image markerImage = button.GetComponent<Image>();
            if (markerImage != null)
            {
                markerImage.color = Color.white;
            }
        }
    }

    private void DeploySelectedLocation()
    {
        if (selectedLocation >= 0)
        {
            ReadyToDeploy?.Invoke(selectedLocation);
        }
    }
}
