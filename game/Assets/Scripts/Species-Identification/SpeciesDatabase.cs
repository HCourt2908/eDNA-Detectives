using System.Collections.Generic;

public static class SpeciesDatabase
{
    public static readonly List<Species> AllSpecies = new()
    {
        new Species
        {
            name = "Green Sea Urchin",
            sequence = new()
            {
                SymbolType.Blue,
                SymbolType.Blue,
                SymbolType.Purple,
                SymbolType.Green,
                SymbolType.Green
            }
        },

        new Species
        {
            name = "Reef Manta Ray",
            sequence = new()
            {
                SymbolType.Blue,
                SymbolType.Red,
                SymbolType.Green,
                SymbolType.Yellow,
                SymbolType.Blue
            }
        },

        new Species
        {
            name = "Kitefin Shark",
            sequence = new()
            {
                SymbolType.Blue,
                SymbolType.Red,
                SymbolType.Green,
                SymbolType.Purple,
                SymbolType.Yellow
            }
        },

        new Species
        {
            name = "Great Hammerhead Shark",
            sequence = new()
            {
                SymbolType.Blue,
                SymbolType.Purple,
                SymbolType.Blue,
                SymbolType.Red,
                SymbolType.Red
            }
        },

        new Species
        {
            name = "Orange Roughy",
            sequence = new()
            {
                SymbolType.Blue,
                SymbolType.Purple,
                SymbolType.Blue,
                SymbolType.Green,
                SymbolType.Purple
            }
        },

        new Species
        {
            name = "Pinecone Fish",
            sequence = new()
            {
                SymbolType.Red,
                SymbolType.Yellow,
                SymbolType.Yellow,
                SymbolType.Red,
                SymbolType.Green
            }
        },

        new Species
        {
            name = "Atlantic Bluefin Tuna",
            sequence = new()
            {
                SymbolType.Red,
                SymbolType.Yellow,
                SymbolType.Yellow,
                SymbolType.Green,
                SymbolType.Purple
            }
        },

        new Species
        {
            name = "Atlantic Herring",
            sequence = new()
            {
                SymbolType.Red,
                SymbolType.Purple,
                SymbolType.Purple,
                SymbolType.Purple,
                SymbolType.Blue
            }
        },

        new Species
        {
            name = "Spotted Lanternfish",
            sequence = new()
            {
                SymbolType.Green,
                SymbolType.Blue,
                SymbolType.Red,
                SymbolType.Purple,
                SymbolType.Red
            }
        },

        new Species
        {
            name = "Northern Krill",
            sequence = new()
            {
                SymbolType.Green,
                SymbolType.Green,
                SymbolType.Purple,
                SymbolType.Red,
                SymbolType.Yellow
            }
        },

        new Species
        {
            name = "King Crab",
            sequence = new()
            {
                SymbolType.Green,
                SymbolType.Yellow,
                SymbolType.Blue,
                SymbolType.Blue,
                SymbolType.Blue
            }
        },

        new Species
        {
            name = "Warty Squid",
            sequence = new()
            {
                SymbolType.Yellow,
                SymbolType.Red,
                SymbolType.Blue,
                SymbolType.Red,
                SymbolType.Purple
            }
        },

        new Species
        {
            name = "Flapjack Octopus",
            sequence = new()
            {
                SymbolType.Yellow,
                SymbolType.Red,
                SymbolType.Blue,
                SymbolType.Yellow,
                SymbolType.Green
            }
        },

        new Species
        {
            name = "Giant Pacific Octopus",
            sequence = new()
            {
                SymbolType.Yellow,
                SymbolType.Red,
                SymbolType.Purple,
                SymbolType.Blue,
                SymbolType.Red
            }
        },

        new Species
        {
            name = "Bone Eating Worm",
            sequence = new()
            {
                SymbolType.Yellow,
                SymbolType.Green,
                SymbolType.Red,
                SymbolType.Green,
                SymbolType.Blue
            }
        },

        new Species
        {
            name = "Tree Bubblegum Coral",
            sequence = new()
            {
                SymbolType.Yellow,
                SymbolType.Purple,
                SymbolType.Yellow,
                SymbolType.Purple,
                SymbolType.Purple
            }
        },

        new Species
        {
            name = "Precious Coral",
            sequence = new()
            {
                SymbolType.Purple,
                SymbolType.Blue,
                SymbolType.Green,
                SymbolType.Blue,
                SymbolType.Purple
            }
        },

        new Species
        {
            name = "Zigzag Coral",
            sequence = new()
            {
                SymbolType.Purple,
                SymbolType.Blue,
                SymbolType.Green,
                SymbolType.Yellow,
                SymbolType.Red
            }
        },

        new Species
        {
            name = "Moon Jellyfish",
            sequence = new()
            {
                SymbolType.Purple,
                SymbolType.Red,
                SymbolType.Yellow,
                SymbolType.Green,
                SymbolType.Yellow
            }
        },

        new Species
        {
            name = "Phytoplankton",
            sequence = new()
            {
                SymbolType.Purple,
                SymbolType.Green,
                SymbolType.Blue,
                SymbolType.Purple,
                SymbolType.Green
            }
        }
    };

    public static Species GetSpeciesByName(string name)
    {
        return AllSpecies.Find(species => species.name == name);
    }
}