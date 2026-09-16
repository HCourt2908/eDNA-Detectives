using UnityEngine;
using System.Collections.Generic;

public enum SpeciesFrequency
{
    Missing,
    LessFrequent,
    SameFrequent,
    MoreFrequent
}

public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }

    public List<Species> speciesList = new List<Species>();
    public Dictionary<Species, SpeciesFrequency> frequencyMap = new Dictionary<Species, SpeciesFrequency>();
    void Awake() 
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        // Current survey for the shared trawling case. Historical coral is
        // introduced by the archived survey in Detective; it is not detected today.
        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Great Hammerhead Shark"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Great Hammerhead Shark"), SpeciesFrequency.LessFrequent);
        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Atlantic Bluefin Tuna"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Atlantic Bluefin Tuna"), SpeciesFrequency.LessFrequent);
        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Atlantic Herring"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Atlantic Herring"), SpeciesFrequency.MoreFrequent);
        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Northern Krill"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Northern Krill"), SpeciesFrequency.MoreFrequent);
        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Phytoplankton"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Phytoplankton"), SpeciesFrequency.SameFrequent);
        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Tree Bubblegum Coral"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Tree Bubblegum Coral"), SpeciesFrequency.Missing);
    }

    
}
