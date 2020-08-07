public class Type
{
    public enum TypeName
    {
        NONE, NORMAL, GRASS, WATER, FIRE, POISON
    }

    public string TypeToString(TypeName type)
    {
        switch (type)
        {
            case TypeName.NORMAL: return "Normal";
            case TypeName.GRASS: return "Grass";
            case TypeName.WATER: return "Water";
            case TypeName.FIRE: return "Fire";
            case TypeName.POISON: return "Poison";
            default: return "???";
        }
    }
}


