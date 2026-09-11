using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuzzleGenerator : MonoBehaviour
{
    public SpeciesPuzzleDifficulty difficulty { get; set; }

    public List<SymbolType> CurrentSequence { get; private set; }
    public Species CorrectSpecies { get; private set; }

    private int MissingCount
    {
        get
        {
            switch (difficulty)
            {
                case SpeciesPuzzleDifficulty.Easy:
                    return 0;
                case SpeciesPuzzleDifficulty.Medium:
                    return 1;
                case SpeciesPuzzleDifficulty.Hard:
                    return 2;
                case SpeciesPuzzleDifficulty.VeryHard:
                    return 3;
                default:
                    return 0;
            }
        }
    }

    // public void GenerateRandomPuzzle()
    // {
    //     List<Species> species = SpeciesDatabase.AllSpecies;

    //     List<Species> possibleTargets = new List<Species>(species);

    //     possibleTargets = possibleTargets.OrderBy(x => Random.value).ToList();

    //     foreach (Species target in possibleTargets)
    //     {
    //         List<List<int>> combinations = new List<List<int>>();

    //         GeneratePositionCombinations(0, MissingCount, new List<int>(), combinations);

    //         combinations = combinations.OrderBy(x => Random.value).ToList();

    //         foreach (List<int> missingPositions in combinations)
    //         {
    //             List<SymbolType> partialSequence = CreatePartialSequence(target, missingPositions);

    //             List<Species> matches = FindMatchingSpecies(partialSequence);

    //             if (matches.Count == 1)
    //             {
    //                 CorrectSpecies = target;
    //                 CurrentSequence = partialSequence;

    //                 return;
    //             }
    //         }
    //     }

    //     Debug.LogError("Couldn't generate a " + difficulty + " puzzle with the current database");

    // }

    public void GenerateRandomPuzzle()
{
    List<Species> species = SpeciesDatabase.AllSpecies;

    List<(Species species, List<SymbolType> sequence)> validPuzzles =
        new List<(Species, List<SymbolType>)>();

    foreach (Species target in species)
    {
        List<List<int>> combinations = new List<List<int>>();

        GeneratePositionCombinations(
            0,
            MissingCount,
            new List<int>(),
            combinations
        );

        foreach (List<int> missingPositions in combinations)
        {
            List<SymbolType> partialSequence =
                CreatePartialSequence(target, missingPositions);

            List<Species> matches =
                FindMatchingSpecies(partialSequence);

            if (matches.Count == 1)
            {
                validPuzzles.Add((target, partialSequence));
            }
        }
    }

    if (validPuzzles.Count == 0)
    {
        Debug.LogError(
            "Couldn't generate a " + difficulty +
            " puzzle with the current database"
        );
        return;
    }

    var chosenPuzzle =
        validPuzzles[Random.Range(0, validPuzzles.Count)];

    CorrectSpecies = chosenPuzzle.species;
    CurrentSequence = chosenPuzzle.sequence;
}

    public void GenerateSpecificPuzzle(string targetName)
    {
        List<Species> species = SpeciesDatabase.AllSpecies;

        Species target = species.FirstOrDefault(s => s.name == targetName);

        if (target == null) Debug.Log("Species with name " + targetName + " not found in database");

        List<List<int>> combinations = new List<List<int>>();

        GeneratePositionCombinations(0, MissingCount, new List<int>(), combinations);

        combinations = combinations.OrderBy(x => Random.value).ToList();

        foreach (List<int> missingPositions in combinations)
        {
            List<SymbolType> partialSequence = CreatePartialSequence(target, missingPositions);

            List<Species> matches = FindMatchingSpecies(partialSequence);

            if (matches.Count == 1)
            {
                CorrectSpecies = target;
                CurrentSequence = partialSequence;

                return;
            }
        }

        Debug.LogError("Couldn't generate a " + difficulty + " puzzle with the current database");
    }

    private List<SymbolType> CreatePartialSequence(Species target, List<int> missingPositions)
    {
        List<SymbolType> sequence = new List<SymbolType>(target.sequence);

        foreach (int position in missingPositions)
        {
            sequence[position] = SymbolType.Mystery;
        }

        return sequence;
    }

    private List<Species> FindMatchingSpecies(List<SymbolType> partialSequence)
    {
        List<Species> matches = new List<Species>();

        foreach (Species species in SpeciesDatabase.AllSpecies)
        {
            bool matchesSequence = true;

            for (int i = 0; i < partialSequence.Count; i++)
            {
                if (partialSequence[i] == SymbolType.Mystery) continue;

                if (species.sequence[i] != partialSequence[i])
                {
                    matchesSequence = false;
                    break;
                }
            }

            if (matchesSequence) matches.Add(species);
        }

        return matches;
    }

    private void GeneratePositionCombinations(int start, int amount, List<int> current, List<List<int>> result)
    {
        if (current.Count == amount)
        {
            result.Add(new List<int>(current));
            return;
        }

        for(int i = start; i < 5; i++)
        {
            current.Add(i);

            GeneratePositionCombinations(i + 1, amount, current, result);

            current.RemoveAt(current.Count - 1);
        }
    }
}
