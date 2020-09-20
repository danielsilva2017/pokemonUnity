public enum Type
{
    Normal, Grass, Water, Fire, Electric, Ground, Flying, Ice, Rock, Poison, Bug, Steel, None
}

public class Types
{
    public static string TypeToString(Type type)
    {
        switch (type)
        {
            case Type.Normal: return "Normal";
            case Type.Grass: return "Grass";
            case Type.Water: return "Water";
            case Type.Fire: return "Fire";
            case Type.Electric: return "Electric";
            case Type.Ground: return "Ground";
            case Type.Flying: return "Flying";
            case Type.Ice: return "Ice";
            case Type.Rock: return "Rock";
            case Type.Poison: return "Poison";
            case Type.Bug: return "Bug";
            case Type.Steel: return "Steel";
            case Type.None: return "--";
            default: return "???";
        }
    }

    /// <summary> 
    /// Damage multiplier, which can be 0, 0.5, 1 or 2.
    /// Row is the attacker, column is the defender.
    /// </summary>
    private static readonly float[][] matrix = new float[][]
    {
    /*                      Norm    Grass   Water   Fire    Elec    Grnd    Fly     Ice     Rock    Psn     Bug     Steel */
    /* Norm */  new float[]{1f,     1f,     1f,     1f,     1f,     1f,     1f,     1f,     0.5f,   1f,     1f,     0.5f   },
    /* Grass */ new float[]{1f,     0.5f,   2f,     0.5f,   1f,     2f,     0.5f,   1f,     2f,     0.5f,   0.5f,   0.5f   },
    /* Water */ new float[]{1f,     0.5f,   0.5f,   2f,     1f,     2f,     1f,     1f,     2f,     1f,     1f,     1f     },
    /* Fire */  new float[]{1f,     2f,     0.5f,   0.5f,   1f,     1f,     1f,     2f,     0.5f,   1f,     2f,     2f     },
    /* Elec */  new float[]{1f,     0.5f,   2f,     1f,     0.5f,   0f,     2f,     1f,     1f,     1f,     1f,     1f     },
    /* Grnd */  new float[]{1f,     0.5f,   1f,     2f,     2f,     1f,     0f,     1f,     2f,     2f,     0.5f,   2f     },
    /* Fly */   new float[]{1f,     2f,     1f,     1f,     0.5f,   1f,     1f,     1f,     0.5f,   1f,     2f,     0.5f   },
    /* Ice */   new float[]{1f,     2f,     0.5f,   1f,     1f,     2f,     2f,     0.5f,   1f,     1f,     1f,     0.5f   },
    /* Rock */  new float[]{1f,     1f,     1f,     2f,     1f,     0.5f,   2f,     2f,     1f,     1f,     2f,     0.5f   },
    /* Psn */   new float[]{1f,     2f,     1f,     1f,     1f,     0.5f,   1f,     1f,     0.5f,   0.5f,   1f,     0f     },
    /* Bug */   new float[]{1f,     2f,     1f,     0.5f,   1f,     1f,     0.5f,   1f,     1f,     0.5f,   1f,     0.5f   },
    /* Steel */ new float[]{1f,     1f,     0.5f,   0.5f,   0.5f,   1f,     1f,     2f,     2f,     1f,     1f,     0.5f   }
    };

    /// <summary>
    /// Damage multiplier when attacking a certain type. Returns 1 if either type is None.
    /// </summary>
    public static float Affinity(Type attacker, Type defender)
    {
        if (attacker == Type.None || defender == Type.None) return 1f;
        return matrix[(int) attacker][(int) defender];
    }

    /// <summary>
    /// Damage multiplier when attacking a certain Pokemon. Returns 1 if either type is None.
    /// </summary>
    public static float Affinity(Move attacker, Pokemon defender)
    {
        return Affinity(attacker.Type, defender.PrimaryType) * Affinity(attacker.Type, defender.SecondaryType);
    }
}