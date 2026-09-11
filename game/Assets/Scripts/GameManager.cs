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

        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Great Hammerhead Shark"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Great Hammerhead Shark"), SpeciesFrequency.Missing);
        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Atlantic Bluefin Tuna"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Atlantic Bluefin Tuna"), SpeciesFrequency.MoreFrequent);
        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Atlantic Herring"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Atlantic Herring"), SpeciesFrequency.LessFrequent);
        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Northern Krill"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Northern Krill"), SpeciesFrequency.MoreFrequent);
        speciesList.Add(SpeciesDatabase.GetSpeciesByName("Phytoplankton"));
        frequencyMap.Add(SpeciesDatabase.GetSpeciesByName("Phytoplankton"), SpeciesFrequency.LessFrequent);
    }

    
}
