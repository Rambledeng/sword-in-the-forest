using System.Collections.Generic;
using Godot;
using SwordInTheForest.Run;

namespace SwordInTheForest.Ui;

/// <summary>
/// Small 16×16 pixel icons for the upgrades, drawn in code from character maps (one character per
/// pixel, see <see cref="Palette"/>) in the Ninja Adventure style: dark outline, flat colours,
/// one highlight. Vitality and Deep Gourd reuse the HUD's heart and gourd so they read the same.
/// </summary>
public static class PixelIcons
{
    private const string UiDir = "res://assets/third_party/ninja_adventure/ui";

    private static readonly Dictionary<char, Color> Palette = new()
    {
        ['k'] = new Color("1c1622"), // outline
        ['w'] = new Color("f4f1e8"), // white shine
        ['s'] = new Color("c9d2dc"), // steel
        ['g'] = new Color("7d8796"), // steel shade
        ['y'] = new Color("e8b84a"), // gold
        ['o'] = new Color("b0702c"), // dark gold / wood
        ['b'] = new Color("6b4226"), // brown
        ['r'] = new Color("c83a3a"), // red
        ['p'] = new Color("f0a0b4"), // pink
        ['m'] = new Color("c06088"), // pink shade
        ['G'] = new Color("5aa050"), // green
        ['d'] = new Color("2f6a3a"), // dark green
        ['B'] = new Color("4a78c0"), // blue
        ['c'] = new Color("9ad8f0"), // pale blue
        ['e'] = new Color("e8dcc0"), // cream
        ['n'] = new Color("8a4a2a"), // braised brown
    };

    private static readonly Dictionary<string, string[]> Maps = new()
    {
        // 架势: a round guard shield, wider than you, with a gold boss.
        ["broad_stance"] = new[]
        {
            "................",
            "...kkkkkkkkkk...",
            "..kssssssssssk..",
            ".ksswssssssggsk.",
            ".kswsssyysssgsk.",
            ".kssssyooysssgk.",
            ".kssssyooysssgk.",
            ".kssssyyyysssgk.",
            ".ksssssssssssgk.",
            "..kssssssssssk..",
            "..ksssssssssgk..",
            "...kssssssgsk...",
            "....kssssgsk....",
            ".....kssggk.....",
            "......kkkk......",
            "................",
        },
        // 调息: a breath of wind curling back on itself.
        ["steady_breath"] = new[]
        {
            "................",
            "........kkkk....",
            ".......kcccck...",
            "......kck..kck..",
            "..........kck...",
            ".kkkkkkkkkck....",
            "kccccccccck.....",
            ".kkkkkkkkkk.....",
            "................",
            ".kkkkkkkkkkkk...",
            "kcccccccccccck..",
            ".kkkkkkkkkkkck..",
            ".......kck.kck..",
            "........kcck....",
            ".........kk.....",
            "................",
        },
        // 利刃: a blade with a glint on its edge.
        ["keen_edge"] = new[]
        {
            "...........kw.kk",
            "...........wwwkk",
            "............wksk",
            "...........ksgk.",
            "..........kswk..",
            ".........kswk...",
            "........kswk....",
            ".......kswk.....",
            "......kswk......",
            "..kk.kswk.......",
            "..kykswk........",
            "...kygk.........",
            "...kbyk.........",
            "..kbk.kk........",
            ".kbk............",
            ".kk.............",
        },
        // 铁弹: two blades meeting, sparks at the cross.
        ["iron_deflect"] = new[]
        {
            "kk............kk",
            "ksk..........ksk",
            "kgsk........ksgk",
            ".kgsk..yy..ksgk.",
            "..kgsk.yy.ksgk..",
            "...kgsk..ksgk...",
            "....kgskksgk....",
            "..yy.kgwwgk.yy..",
            "..yy.kgwwgk.yy..",
            "....kgskksgk....",
            "...kgsk..ksgk...",
            "..kok......kok..",
            ".kbok......kobk.",
            "kbk.kk....kk.kbk",
            "kk............kk",
            "................",
        },
        // 慈悲: a lotus, the flower of mercy.
        ["merciful_cut"] = new[]
        {
            "................",
            ".......kk.......",
            "......kppk......",
            "......kpwk......",
            "..kk.kpppmk.kk..",
            "..kpkkpppmkkpk..",
            "..kppkppmkkppk..",
            "...kppkpmkppk...",
            ".kkkppmpmmpmkkk.",
            "kpppkppppppkpppk",
            ".kmmmkpppmkmmmk.",
            "..kkkkkkkkkkkk..",
            "...kGGGGGGGGk...",
            "....kddddddk....",
            ".....kkkkkk.....",
            "................",
        },
        // 瞬步: a blade darting forward, speed lines behind it.
        ["flash_step"] = new[]
        {
            "................",
            "................",
            "kkkkkkk.........",
            "kcccccck........",
            "kkkkkkk.........",
            "........k.......",
            "..kkkk.kokkkkkk.",
            ".kcccckbyksswwwk",
            "..kkkk.kokkkkkk.",
            "........k.......",
            "kkkkkkk.........",
            "kcccccck........",
            "kkkkkkk.........",
            "................",
            "................",
            "................",
        },
        // 余威: a finishing cross-cut, its shock spreading out.
        ["dread"] = new[]
        {
            "................",
            "....yy....yy....",
            "..yy........yy..",
            ".y..kk....kk..y.",
            ".y.krrk..krrk.y.",
            "y...krrkkrrk...y",
            "y....krrrrk....y",
            "y.....krrk.....y",
            "y....krrrrk....y",
            "y...krrkkrrk...y",
            ".y.krrk..krrk.y.",
            ".y..kk....kk..y.",
            "..yy........yy..",
            "....yy....yy....",
            "................",
            "................",
        },
        // 止水: a drop falling into still water.
        ["still_water"] = new[]
        {
            "................",
            ".......kk.......",
            "......kcck......",
            "......kcck......",
            ".....kcwcck.....",
            ".....kcccBk.....",
            ".....kccBBk.....",
            "......kBBk......",
            ".......kk.......",
            "................",
            "...kkkkkkkkkk...",
            ".kkcccccccccckk.",
            "kcckkkkkkkkkkcck",
            ".kk..........kk.",
            "................",
            "................",
        },
        // 米饭: a bowl of rice, blue-and-white porcelain.
        ["rice"] = new[]
        {
            "....kkkk....",
            "..kkwwwwkk..",
            ".kwwwwewwwk.",
            "kwwewwwwwewk",
            "kkkkkkkkkkkk",
            "kBcBBBBBBBBk",
            "kBBcBBBBBBBk",
            ".kBBBBBBBBk.",
            "..kBBBBBBk..",
            "...kkkkkk...",
            "....kBBk....",
            "....kkkk....",
        },
        // 酱牛肉: slices of braised beef on a plate.
        ["braised_beef"] = new[]
        {
            "............",
            "...kkkkkk...",
            "..knnnonnk..",
            ".knnonnnnnk.",
            ".knnnnnnonk.",
            ".kkkkkkkkkk.",
            "knnnonnnnonk",
            "knonnnnnnnnk",
            ".kkkkkkkkkk.",
            "kssswsssssgk",
            ".kgggggggggk",
            "..kkkkkkkkk.",
        },
        // 烧鸡: a roast chicken leg.
        ["roast_chicken"] = new[]
        {
            ".....kkkkk..",
            "....kyyyook.",
            "...kyyoooook",
            "..kyoooooook",
            "..kooooooobk",
            "..koooooobk.",
            "...kboobbk..",
            "..kwkkbkk...",
            ".kwwk.kk....",
            "kwwk........",
            "kewk........",
            ".kk.........",
        },
    };

    private static readonly Dictionary<string, Texture2D> Cache = new();

    /// <summary>The icon for an upgrade (16×16 or the HUD sprite's size).</summary>
    public static Texture2D For(Upgrade u) => u switch
    {
        Upgrade.Vitality => Heart(),
        Upgrade.DeepGourd => Load($"{UiDir}/gourd.png"),
        _ => Drawn(UpgradeScreen.Key(u)),
    };

    /// <summary>A code-drawn icon by name (a key of the maps above).</summary>
    public static Texture2D Drawn(string name)
    {
        if (Cache.TryGetValue(name, out var cached)) return cached;
        var map = Maps[name];
        var img = Image.CreateEmpty(map[0].Length, map.Length, false, Image.Format.Rgba8);
        for (var y = 0; y < map.Length; y++)
            for (var x = 0; x < map[y].Length; x++)
                if (Palette.TryGetValue(map[y][x], out var c)) img.SetPixel(x, y, c);
        var tex = ImageTexture.CreateFromImage(img);
        Cache[name] = tex;
        return tex;
    }

    /// <summary>The icon for a food drop (12×12).</summary>
    public static Texture2D For(Food f) => Drawn(f switch
    {
        Food.Rice => "rice",
        Food.BraisedBeef => "braised_beef",
        _ => "roast_chicken",
    });

    /// <summary>All code-drawn icon names (for tests and the credits check).</summary>
    public static IEnumerable<string> DrawnNames => Maps.Keys;

    private static Texture2D Heart()
    {
        if (Cache.TryGetValue("heart", out var cached)) return cached;
        // The full heart is the sheet's last 16×16 frame (empty, ¼, ½, ¾, full).
        var tex = new AtlasTexture { Atlas = GD.Load<Texture2D>($"{UiDir}/heart.png"), Region = new Rect2(64, 0, 16, 16) };
        Cache["heart"] = tex;
        return tex;
    }

    private static Texture2D Load(string path)
    {
        if (Cache.TryGetValue(path, out var cached)) return cached;
        var tex = GD.Load<Texture2D>(path);
        Cache[path] = tex;
        return tex;
    }
}
