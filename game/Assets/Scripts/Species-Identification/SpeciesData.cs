using System.Collections.Generic;
using UnityEngine;

public enum SymbolType
{
    Blue,
    Red,
    Green,
    Yellow,
    Purple,
    Mystery
}

public enum SpeciesPuzzleDifficulty
{
    Easy,
    Medium,
    Hard,
    VeryHard
}

[System.Serializable]
public class Species
{
    public string name;
    public List<SymbolType> sequence;

    public string silhouetteImagePath;
    public string colouredImagePath;
    public int colouredImageIndex = 0;

    public Sprite GetSilhouetteImage()
    {
        return Resources.Load<Sprite>(silhouetteImagePath);
    }

    public Sprite GetColouredImage()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(colouredImagePath);

        return sprites[colouredImageIndex];
    }
}
