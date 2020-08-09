public class Type
{
    public enum Label
    {
        None, Normal, Grass, Water, Fire, Poison
    }

    public static string TypeToString(Label type)
    {
        switch (type)
        {
            case Label.Normal: return "Normal";
            case Label.Grass: return "Grass";
            case Label.Water: return "Water";
            case Label.Fire: return "Fire";
            case Label.Poison: return "Poison";
            default: return "???";
        }
    }
}


