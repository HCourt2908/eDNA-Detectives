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
            },
            silhouetteImagePath="Silhouettes/GreenSeaUrchinShape",
            colouredImagePath="Colours/GreenSeaUrchin"
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
            },
            silhouetteImagePath="Silhouettes/MantaRayShape",
            colouredImagePath="Colours/MantaRay"
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
            },
            silhouetteImagePath="Silhouettes/KitefinSharkShape",
            colouredImagePath="Colours/KitefinShark"
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
            },
            silhouetteImagePath="Silhouettes/HammerheadSharkShape",
            colouredImagePath="Colours/HammerheadShark"
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
            },
            silhouetteImagePath="Silhouettes/OrangeRoughyShape",
            colouredImagePath="Colours/OrangeRoughy"
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
            },
            silhouetteImagePath="Silhouettes/PineconeFishShape",
            colouredImagePath="Colours/PineconeFish"
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
            },
            silhouetteImagePath="Silhouettes/AtlanticBluefinTunaShape",
            colouredImagePath="Colours/AtlanticBluefinTuna"
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
            },
            silhouetteImagePath="Silhouettes/AtlanticHerringShape",
            colouredImagePath="Colours/AtlanticHerring"
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
            },
            silhouetteImagePath="Silhouettes/LanternfishShape",
            colouredImagePath="Colours/Lanternfish"
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
            },
            silhouetteImagePath="Silhouettes/NorthernKrillShape",
            colouredImagePath="Colours/NorthernKrill"
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
            },
            silhouetteImagePath="Silhouettes/KingCrabShape",
            colouredImagePath="Colours/KingCrab"
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
            },
            silhouetteImagePath="Silhouettes/WartySquidShape",
            colouredImagePath="Colours/WartySquid"
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
            },
            silhouetteImagePath="Silhouettes/FlapjackOctopusShape",
            colouredImagePath="Colours/FlapjackOctopus"
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
            },
            silhouetteImagePath="Silhouettes/GiantOctopusShape",
            colouredImagePath="Colours/GiantOctopus"
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
            },
            silhouetteImagePath="Silhouettes/BoneEatingWormShape",
            colouredImagePath="Colours/BoneEatingWorm"
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
            },
            silhouetteImagePath="Silhouettes/TreeBubblegumCoralShape",
            colouredImagePath="Colours/TreeBubblegumCoral"
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
            },
            silhouetteImagePath="Silhouettes/PreciousCoralShape",
            colouredImagePath="Colours/PreciousCoral"
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
            },
            silhouetteImagePath="Silhouettes/ZigzagCoralShape",
            colouredImagePath="Colours/ZigzagCoral"
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
            },
            silhouetteImagePath="Silhouettes/MoonJellyfishShape",
            colouredImagePath="Colours/MoonJellyfish"
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
            },
            silhouetteImagePath="Silhouettes/PhytoplanktonShape",
            colouredImagePath="Colours/Phytoplankton"
        }
    };

    public static Species GetSpeciesByName(string name)
    {
        return AllSpecies.Find(species => species.name == name);
    }
}