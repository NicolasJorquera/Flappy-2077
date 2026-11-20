using UnityEngine;

/// <summary>Un color del auto: sprites recoloreados (Tools/art/skins.py), nivel para desbloquear y estela.</summary>
public class CarSkin
{
    public int Index;
    public string NameKey;
    public int UnlockLevel;
    public Color TrailA, TrailB;
    /// <summary>Ciclo de color en tiempo real (holograma).</summary>
    public bool Holo;

    public bool Unlocked => GameSettings.Level >= UnlockLevel;

    public Sprite Up => Index == 0 ? null : Resources.Load<Sprite>($"Skins/skin{Index}_up");
    public Sprite Down => Index == 0 ? null : Resources.Load<Sprite>($"Skins/skin{Index}_down");
}

/// <summary>Colores del auto que se desbloquean subiendo de nivel de piloto.</summary>
public static class CarSkins
{
    private static readonly Color Pink = new Color(1f, 0.2f, 0.63f), Cyan = new Color(0.25f, 0.96f, 0.82f),
        Lilac = new Color(0.92f, 0.51f, 0.9f), Amber = new Color(1f, 0.83f, 0.42f), Lime = new Color(0.6f, 1f, 0.3f),
        Red = new Color(1f, 0.25f, 0.3f), Ice = new Color(0.7f, 0.95f, 1f), Violet = new Color(0.55f, 0.3f, 1f);

    public static readonly CarSkin[] All =
    {
        new CarSkin { Index = 0, NameKey = "skin_original", UnlockLevel = 1, TrailA = Pink, TrailB = Lilac },
        new CarSkin { Index = 1, NameKey = "skin_pink", UnlockLevel = 2, TrailA = Pink, TrailB = Cyan },
        new CarSkin { Index = 2, NameKey = "skin_ice", UnlockLevel = 3, TrailA = Cyan, TrailB = Ice },
        new CarSkin { Index = 3, NameKey = "skin_toxic", UnlockLevel = 4, TrailA = Lime, TrailB = Lilac },
        new CarSkin { Index = 4, NameKey = "skin_gold", UnlockLevel = 6, TrailA = Amber, TrailB = Pink },
        new CarSkin { Index = 5, NameKey = "skin_midnight", UnlockLevel = 8, TrailA = Violet, TrailB = Cyan },
        new CarSkin { Index = 6, NameKey = "skin_crimson", UnlockLevel = 10, TrailA = Red, TrailB = Amber },
        new CarSkin { Index = 7, NameKey = "skin_holo", UnlockLevel = 12, TrailA = Cyan, TrailB = Pink, Holo = true },
    };

    public static CarSkin Selected
    {
        get
        {
            int i = Mathf.Clamp(GameSettings.SelectedSkin, 0, All.Length - 1);
            return All[i].Unlocked ? All[i] : All[0];
        }
    }

    /// <summary>Colores que se desbloquean al llegar exactamente a este nivel.</summary>
    public static CarSkin UnlockedAt(int level)
    {
        foreach (var s in All) if (s.UnlockLevel == level) return s;
        return null;
    }
}
