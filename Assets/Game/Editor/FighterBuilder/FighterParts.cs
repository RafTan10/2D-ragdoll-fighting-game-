using System.Collections.Generic;

/// <summary>One option in the Fighter Builder (a hairstyle, a top, …): pixel layers for the parts it changes.</summary>
public class FighterPart
{
    public string name;
    public string[] head, torso, upperArm, lowerArm, upperLeg, lowerLeg, cape, skirt;
    /// <summary>Tops: the wrist shows (short / rolled sleeves), so a watch can be drawn.</summary>
    public bool showsWrist;
    /// <summary>Tops: tucked in, so a belt can be drawn over the waist.</summary>
    public bool tucked;
    /// <summary>Armour this option gives its region (tops: torso + arms; headwear: head).</summary>
    public ArmorMaterial armor = ArmorMaterial.Cloth;
}

/// <summary>
/// The Fighter Builder's part library. Layers are text rows in the PixelMap format: "<count><letter>" repeats,
/// anything else is literal letters; '.' leaves what's underneath, '_' erases it. Letters are colour <b>roles</b>,
/// recoloured from the recipe (see <see cref="FighterBuilder"/>):
///   a outline | s S l k skin (base, shadow, light, deep) r lip w eye white b brow | h H g hair B beard |
///   c C x X top (base, shadow, light, sheen) n top accent | i I inner shirt | t T y trousers | o O u shoes (sole) |
///   p P Y z hat (base, shadow, light, band) | G F glasses frame L E lens + shine | m M metal | q belt J watch strap
/// Sizes: head 24x24 (neck = bottom), torso 24x36, arms 10x20, legs 12x24, cape / skirt 24 wide. Facing right.
/// </summary>
public static class FighterParts
{
    // ---------------------------------------------------------------- always drawn

    /// <summary>A bald head facing right: dome, ear, eye, brow, nose, lips, neck. Hair and hats go on top.</summary>
    public static readonly string[] BaseHead =
    {
        "24.",
        "8. 7a 9.",
        "6. aa 7l aa 7.",
        "4. aa 3s 6l 2s aa 5.",
        "3. a 5s 6l 5s a 3.",
        "2. a S 5s 5l 6s a 3.",
        "2. a S 6s 4l 6s a 3.",
        "2. a SS 13s lls a 2.",
        "2. a SS 12s llss a 2.",
        "2. a S 3s SS 7s bbb ls a 2.",
        "2. a S ssss a kS sslss w a ssS a 2.",
        "2. a S ssss a kS 9s s s a 1.",
        "2. a S ssss a Sk 8s sS S a 1.",
        "2. a S ssss a SS 6s sss S a 2.",
        "2. a S 4s S 12s a 2.",
        "2. a S 3s a S 4s 5s rr s a 2.",
        "3. a 3s a S 3s 9s a 2.",
        "4. aaa a S 3s 8s a 3.",
        "7. a S 5s ss aaaa 4.",
        "7. a S 6s S a 7.",
        "7. a S 6s S a 7.",
        "7. a S 6s S a 7.",
        "7. a S 6s S a 7.",
        "7. a S 6s S a 7.",
    };

    /// <summary>The fist: bottom 7 rows of every forearm.</summary>
    public static readonly string[] Hand =
    {
        "1. a lsssssS a", "1. a lssssSS a", "1. a lsSsSsS a", "1. a lsSsSsS a", "1. a lssssSS a", "1. a SssssSS a", "2. 7a 1.",
    };

    /// <summary>Trousers: hips on the torso (rows 28–35), thighs, shins (shoes go on rows 18–23 of the shin).</summary>
    public static readonly string[] TrouserHips =
    {
        "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.",
        "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.", "24.",
        "2. a yy 7t T 6t TT a 2.", "2. a yy 7t T 6t TT a 2.", "2. a yy 7t T 6t TT a 2.", "2. a yy 7t T 6t TT a 2.",
        "2. a yy 7t T 6t TT a 2.", "2. a yy 7t T 6t TT a 2.", "2. a yy 7t T 6t TT a 2.", "3. 18a 3.",
    };
    static readonly string Leg = "1. a yttttTTT a 1.", LegF = "1. a ytttTtTT a 1.";
    public static readonly string[] TrouserThigh =
    {
        Leg, Leg, LegF, Leg, Leg, Leg, LegF, Leg, Leg, Leg, LegF, Leg, Leg, Leg, LegF, Leg, Leg, Leg, LegF, Leg, Leg, Leg, LegF, Leg,
    };
    public static readonly string[] TrouserShin =
    {
        Leg, Leg, LegF, Leg, Leg, Leg, LegF, Leg, Leg, Leg, LegF, Leg, Leg, Leg, LegF, Leg, Leg, Leg,
        "", "", "", "", "", "",
    };

    /// <summary>Belt with a metal buckle over a tucked top (torso rows 27–28).</summary>
    public static readonly string[] Belt = Rows(36, 27, "2. a 7q mMm 8q a 2.", "2. a 7q MmM 8q a 2.");

    /// <summary>Watch on the wrist (forearm rows 11–12), when the sleeve leaves the wrist bare.</summary>
    public static readonly string[] Watch = Rows(20, 11, "1. a JJJJJJ a 1.", "1. a JmmJJJ a 1.");

    // ---------------------------------------------------------------- hair

    public static readonly FighterPart[] Hair =
    {
        new FighterPart { name = "Bald", head = new string[0] },
        new FighterPart { name = "Buzz cut", head = new[]
        {
            "24.", "8. 7a 9.", "6. aa hHhHhHh aa 7.", "4. aa hHhHhHhHhHh aa 5.", "3. a hHhHhHhHhHhHhHhH a 3.",
            "2. a hHhHhHhHhHhHhHhHh a 3.", "2. a HhHhHhHhHhHh 9.", "2. a HhHhHhHh 13.", "2. a hHhHhH 15.", "2. a HhHh 17.",
        } },
        new FighterPart { name = "Short", head = new[]
        {
            "24.", "8. 7a 9.", "6. aa hHgHHhHH aa 6.", "4. aa hHHHgHHHhHH aa 5.", "3. a hHHHHHgHHHHHHHHh a 3.",
            "2. a hHHHHHHHgHHHHHHHH a 3.", "2. a HhHHHHHHHHHHHgHHH a 3.", "2. a HHhHHHHHgHH 10.", "2. a HhHHHHHH 13.",
            "2. a HHhHHH 15.", "2. a HhHH 17.", "2. a HhHH 17.", "2. a HHH 18.",
        } },
        new FighterPart { name = "Wavy (swept)", head = new[]
        {
            "7. 2a . 3a . 2a 8.", "6. a hH a gh a Hh a 8.", "5. a hHH gHH hHH Hg a 6.", "4. a hHHH gHHH hHHH gH a 4.",
            "3. a hHHHH hgHHH HhHHH gH a 2.", "2. a hHHHHH HhgHH HHHgh HHH a 1.", "2. a HhHHHH HHhHH gHHHH HgH a 1.",
            "2. a HHhHHH HHHH 3. HHgH 4.", "2. a HHhHH HHHH 5. Hh 5.", "2. a HHhHH HH 14.", "2. a HhHHH 16.",
            "2. a HHhHH 16.", "2. a HhHHH 16.", "2. a HHhHH 16.", "2. a HhHHH 16.", "2. a HhHH a 16.", "3. a HhH a 16.",
        } },
        new FighterPart { name = "Big wavy", head = new[]
        {
            "5. 2a . 4a . 3a 8.", "4. a hH a hgHH a Hh a 8.", "3. a hHHH gHHHH hHH Hg a 5.", "2. a hHHHH HgHHH HhHH HHg a 3.",
            "1. a hHHHHH HHgHH HHhHH HHHg a 1.", "1. a HhHHHH HHHhg HHHHH gHHH a 1.", "a HhHHHH HHHHH hHHgH HgHHH a 1.",
            "a HHhHHH HHHHH HhHH 2. HHHg a 1.", "a HhHHHHHHHHH 5. HhH 4.", "a HHhHHHHHH 14.", "a HhHHHHHH 15.",
            "a HHhHHHHH 15.", "a HhHHHHHH 15.", "a HHhHHHHH 15.", "a HhHHHHHH 15.", "a HHhHHHH a 15.", "1. a HhHHH a 16.",
            "3. a HH a 17.", "4. aa 18.",
        } },
        new FighterPart { name = "Long", head = new[]
        {
            "24.", "8. 7a 9.", "6. aa hHgHHhHH aa 6.", "4. aa hHHHgHHHhHH aa 5.", "3. a hHHHHHgHHHHHHHHh a 3.",
            "2. a hHHHHHHHgHHHHHHHH a 3.", "2. a HhHHHHHHHHHHHgHHH a 3.", "1. a HHhHHHHHHgHH 10.", "1. a HhHHHHHHH 13.",
            "1. a HHhHHHHHH 13.", "1. a HhHHHHHHH 13.", "1. a HHhHHHHHH 13.", "1. a HhHHHHHHH 13.", "1. a HHhHHHHH 14.",
            "1. a HhHHHHHH 14.", "1. a HHhHHHH 15.", "1. a HhHHHH 16.", "1. a HHhHH 17.", "1. a HhHH a 17.",
        }, cape = LongHairCape() },
    };

    static string[] LongHairCape()
    {
        var rows = new List<string>();
        for (int i = 0; i < 20; i++) rows.Add(i % 4 == 1 ? "1. a HhHHHHgHHHHHHhHHHHHH a 1." : "1. a HHhHHHHHHHgHHHHhHHHH a 1.");
        rows.Add("2. a 18H a 2.");
        rows.Add("3. 18a 3.");
        return rows.ToArray();
    }

    // ---------------------------------------------------------------- face

    public static readonly FighterPart[] FacialHair =
    {
        new FighterPart { name = "None", head = new string[0] },
        new FighterPart { name = "Stubble", head = Rows(24, 14, "15. B 2. 3B 3.", "13. 5B 2. B 3.", "12. 9B 3.", "12. 8B 4.", "14. 2B 8.") },
        new FighterPart { name = "Moustache", head = Rows(24, 14, "18. 3B 3.", "20. B 3.") },
        new FighterPart { name = "Beard", head = Rows(24, 14, "14. B 2. 4B 3.", "12. 6B 2. B 3.", "12. 9B 3.", "12. 9B 3.", "14. a 5B a 3.", "17. 2B a 4.", "17. a 6.") },
    };

    public static readonly FighterPart[] Eyewear =
    {
        new FighterPart { name = "None", head = new string[0] },
        new FighterPart { name = "Glasses", head = Rows(24, 10, "9. 5F . 5G 4.", "15. G 3. G 4.", "15. 5G 4.") },
        new FighterPart { name = "Shades", head = Rows(24, 9, "15. 5G 4.", "9. 6F G LLE G 4.", "15. G LLL G 4.", "16. 3G 5.") },
    };

    public static readonly FighterPart[] Headwear =
    {
        new FighterPart { name = "None", head = new string[0], armor = ArmorMaterial.Skin },
        new FighterPart { name = "Fedora", armor = ArmorMaterial.Cloth, head = new[]
        {
            "8_ 7a 9_", "7_ a YYpppp a 9_", "6_ a Y 7p a 8_", "5_ a Y 8p P a 7_", "4_ a Y 10p P a 6_", "4_ a 12z a 6_",
            "a Y 20p P a", "1. a 20P a 1.",
        } },
        new FighterPart { name = "Cap", armor = ArmorMaterial.Cloth, head = new[]
        {
            "24_", "7_ 8a 9_", "5_ aa Y 6p P aa 7_", "4_ a Y 9p P a 7_", "3_ a Y 11p P a 6_", "2_ a Y 12p P a 6_",
            "2. a 14P 5p a 1.", "16. 6a 2.",
        } },
        new FighterPart { name = "Hijab", armor = ArmorMaterial.Cloth, head = new[]
        {
            "24_", "7_ 8a 9_", "5_ aa pppPpppp aa 7_", "4_ a 13p a 5_", "3_ a 15p a 4_", "2_ a 17p a 3_", "2_ a 16p P a 3_",
            "1_ a 13p P 8.", "1_ a 12p P 9.", "1_ a 11p P 10.", "1_ a 11p P 10.", "1_ a 11p P 10.", "1_ a 11p P 10.",
            "1_ a 11p P 10.", "1_ a 12p P 9.", "1_ a 13p P 8.", "1_ a 14p P 7.", "1_ a 16p P 5.", "1_ a 17p a 4.",
            "1_ a 17p a 4.", "1_ a 17p a 4.", "1_ a 17p a 4.", "1_ a 17p a 4.", "1_ a 17p a 4.",
        }, cape = HijabDrape() },
    };

    static string[] HijabDrape()
    {
        var rows = new List<string>();
        for (int i = 0; i < 16; i++) rows.Add(i % 5 == 2 ? "1. a pPpppppppppppppPpppp a 1." : "1. a ppppppPppppppppppPpp a 1.");
        rows.Add("2. a 18P a 2.");
        rows.Add("3. 18a 3.");
        return rows.ToArray();
    }

    // ---------------------------------------------------------------- tops

    static readonly string Shirt = "2. a xx cccccc C c cccccc CC a 2.", ShirtButton = "2. a xx cccccc C n cccccc CC a 2.",
                           ShirtFold = "2. a xx ccCccc C c cccCcc CC a 2.";
    static readonly string Vest = "2. a x 7c C c 7c C a 2.", VestButton = "2. a x 7c C n 7c C a 2.",
                           VestPocket = "2. a x c 5C c C c c 5C c C a 2.";
    static readonly string Tee = "2. a xx 14c CC a 2.", TeeFold = "2. a xx 5c C 8c CC a 2.";
    static readonly string Sleeve = "1. a xccccC a 1.", SleeveFold = "1. a xcCccC a 1.";
    static readonly string Arm = "1. a lsssSS a 1.";

    public static readonly FighterPart[] Tops =
    {
        new FighterPart
        {
            name = "T-shirt", showsWrist = true, tucked = false,
            torso = Fill(36, new[]
            {
                "8. a S 4s S a 8.", "4. aaaa c C 4s C c aaaa 4.", "3. a xcccc 6C ccccC a 3.",
                Tee, Tee, TeeFold, Tee, Tee, Tee, Tee, TeeFold, Tee, Tee, Tee, Tee, Tee, TeeFold, Tee, Tee, Tee, Tee, Tee,
                TeeFold, Tee, Tee, Tee, Tee, "2. a xx 14C CC a 2.",
            }),
            upperArm = new[] { "2. 6a 2.", Sleeve, Sleeve, SleeveFold, Sleeve, Sleeve, Sleeve, Sleeve, Sleeve, "1. a CCCCCC a 1.",
                               Arm, Arm, Arm, Arm, Arm, Arm, Arm, Arm, Arm, Arm },
            lowerArm = new[] { Arm, Arm, Arm, Arm, Arm, Arm, Arm, Arm, Arm, Arm, Arm, Arm, Arm },
        },
        new FighterPart
        {
            name = "Shirt (rolled sleeves)", showsWrist = true, tucked = true,
            torso = Fill(36, new[]
            {
                "8. a S sss s S a 8.", "4. aaaa C S ssss S C aaaa 4.", "3. a xxcc c C ssss C c ccCC a 3.",
                "2. a xxccc cc C ss C cc cccCC a 2.", "2. a xxccc ccc CC ccc ccCCC a 2.",
                Shirt, ShirtButton, Shirt, ShirtFold, Shirt, ShirtButton, Shirt, Shirt, ShirtFold, ShirtButton, Shirt, Shirt,
                ShirtFold, ShirtButton, Shirt, Shirt, ShirtFold, ShirtButton, Shirt, Shirt, Shirt, Shirt, Shirt,
            }),
            upperArm = new[] { "2. 6a 2.", Sleeve, Sleeve, SleeveFold, Sleeve, Sleeve, Sleeve, "1. a xccCcC a 1.", Sleeve, Sleeve,
                               SleeveFold, Sleeve, Sleeve, "1. a xccCcC a 1.", Sleeve, Sleeve, SleeveFold, Sleeve, Sleeve, Sleeve },
            lowerArm = new[] { "1. a xxxxxC a 1.", "1. a xnxxxC a 1.", "1. a CCCCCC a 1.", "1. a xxxxxC a 1.",
                               "1. a lsssSS a 1.", "1. a lsssSS a 1.", "1. a lsssSS a 1.", "1. a lssSSS a 1.", "1. a lsssSS a 1.",
                               "1. a lsssSS a 1.", "1. a lsssSS a 1.", "1. a lsssSS a 1.", "1. a lsssSS a 1." },
        },
        new FighterPart
        {
            name = "Waistcoat over shirt", showsWrist = true, tucked = false,
            torso = Fill(36, new[]
            {
                "8. a S ssss S a 8.", "4. aaaa i Sssss S i aaaa 4.", "3. a xcc ii i ssss ii cccC a 3.",
                "2. a xcccc ii S ss S ii ccccC a 2.", "2. a xcccc ii m ss m ii ccccC a 2.", "2. a xccccc ii mm ii cccccC a 2.",
                "2. a xcccccc I ss I ccccccC a 2.", "2. a xcccccc i ii i ccccccC a 2.", "2. a xccccccc ii cccccccC a 2.",
                VestButton, Vest, Vest, Vest, VestButton, Vest, Vest, Vest, VestButton, VestPocket, Vest, Vest, VestButton,
                Vest, Vest, Vest, Vest, Vest, "2. a x 7c CC 7c C a 2.", "2. a 18C a 2.",
            }),
            upperArm = Repeat20("2. aaaaaa 2.", "1. a iiiiiI a 1.", "1. a iiIiiI a 1."),
            lowerArm = new[] { "1. a iiiiiI a 1.", "1. a IIIIII a 1.", "1. a iiiiiI a 1.", "1. a IiIiII a 1.",
                               "1. a lsssSS a 1.", "1. a lsssSS a 1.", "1. a lsssSS a 1.", "1. a lsssSS a 1.", "1. a lsssSS a 1.",
                               "1. a lsssSS a 1.", "1. a lsssSS a 1.", "1. a lsssSS a 1.", "1. a lsssSS a 1." },
        },
        new FighterPart
        {
            name = "Leather jacket", showsWrist = false, tucked = false, armor = ArmorMaterial.Leather,
            torso = Fill(36, new[]
            {
                "8. a S ssss S a 8.", "3. a xccc a S ssss S a cccC a 3.", "2. a xcccc a iiiiii a ccccC a 2.",
                "2. a x ccc a xx iiii xx a ccc C a 2.", "2. a x cc a xxx iiii xxx a cc C a 2.",
                "2. a x cc a xxm iiii mxx a cc C a 2.", "2. a x cccc a x iiii a ccccc C a 2.",
                "2. a x cccccc a ii a m ccccc C a 2.",
                "..axccccccccccmCccccCa..", "..axccccccccccmCccccCa..", "..axccccccccccmCcxccCa..", "..axcXccccccccmCccccCa..",
                "..axcXcccccccmCcccccCa..", "..axcccccccccmCcccccCa..", "..axcccccccccmCcccccCa..", "..axcccccccccmCcccccCa..",
                "..axcXccccccmCccccccCa..", "..axcXccccccmCcccxccCa..", "..axccccccccmCccccccCa..", "..axccccccccmCccccccCa..",
                "..axcccccccmCcccccccCa..", "..axcXcccccmCcccccccCa..", "..axcXcccccmCcccccccCa..", "..axcccccccmCcccccccCa..",
                "..axccccccmCcccccxccCa..", "..axccccccmCccccccccCa..", "..axcXccccmCccccccccCa..", "..axcXccccmCccccccccCa..",
                "2. a x CC mm 12C C a 2.", "2. a x CC mC 12C C a 2.", "2. a x 16c C a 2.", "2. a 18C a 2.",
            }),
            upperArm = Repeat20("2. aaaaaa 2.", "1. a xccccC a 1.", "1. a xXcccC a 1."),
            lowerArm = new[] { Sleeve, "1. a xXcccC a 1.", "1. a xXcccC a 1.", Sleeve, Sleeve, Sleeve, "1. a xXcccC a 1.", Sleeve,
                               Sleeve, Sleeve, "1. a CmCCCC a 1.", "1. a CmCCCC a 1.", Sleeve },
        },
        new FighterPart
        {
            name = "Hoodie", showsWrist = false, tucked = false,
            torso = Fill(36, new[]
            {
                "3. aaaa . a S 4s S a 8.", "3. a xccc C S ssss S C cccC a 3.", "2. a xxcccc C n ss n C cccccC a 2.",
                "2. a xxcccc C n CC n C cccccC a 2.", "2. a xxccccc c n n c ccccccC a 2.",
                "2. a xx 6c n c n 6c C a 2.", "2. a xx 6c n c n 6c C a 2.", "2. a xx 6c m c m 6c C a 2.",
                Tee, Tee, TeeFold, Tee, Tee, Tee, Tee, TeeFold, Tee, Tee,
                "2. a xx cc 10C cc CC a 2.", "2. a xx cc C 8c C cc CC a 2.", "2. a xx cc C 8c C cc CC a 2.",
                "2. a xx cc C 8c C cc CC a 2.", "2. a xx cc C 8c C cc CC a 2.",
                Tee, Tee, Tee, Tee, Tee, "2. a 18C a 2.", Tee, "2. a 18C a 2.",
            }),
            upperArm = Repeat20("2. aaaaaa 2.", Sleeve, SleeveFold),
            lowerArm = new[] { Sleeve, Sleeve, SleeveFold, Sleeve, Sleeve, Sleeve, Sleeve, SleeveFold, Sleeve, Sleeve, Sleeve,
                               "1. a CCCCCC a 1.", "1. a CCCCCC a 1." },
        },
        new FighterPart
        {
            name = "Panjabi / kameez", showsWrist = false, tucked = false,
            torso = Fill(36, new[]
            {
                "8. a S 4s S a 8.", "4. aaaa c C 4s C c aaaa 4.", "3. a xcccccc C n ccccccC a 3.",
                "2. a xx 6c C c 7c C a 2.", "2. a xx 6c C n 7c C a 2.", "2. a xx 6c C c 7c C a 2.", "2. a xx 6c C c 7c C a 2.",
                "2. a xx 6c C n 7c C a 2.", "2. a xx 6c C c 7c C a 2.", "2. a xx 6c C c 7c C a 2.", "2. a xx 6c C n 7c C a 2.",
                "2. a xx 6c C c 7c C a 2.", Tee, Tee, TeeFold, Tee, Tee, Tee, Tee, TeeFold, Tee, Tee, Tee, Tee, Tee, TeeFold,
                Tee, Tee, Tee, Tee, Tee, Tee, TeeFold, Tee, Tee, Tee,
            }),
            upperArm = Repeat20("2. aaaaaa 2.", Sleeve, SleeveFold),
            lowerArm = new[] { Sleeve, Sleeve, SleeveFold, Sleeve, Sleeve, Sleeve, Sleeve, SleeveFold, Sleeve, Sleeve, Sleeve,
                               "1. a CCCCCC a 1.", "1. a xxxxxC a 1." },
            skirt = new[] { Tee, Tee, TeeFold, Tee, Tee, Tee, Tee, TeeFold, Tee, Tee, Tee, Tee, TeeFold, Tee, "2. a 18C a 2.", "3. 18a 3." },
        },
    };

    // ---------------------------------------------------------------- shoes (shin rows 18–23)

    public static readonly FighterPart[] Shoes =
    {
        new FighterPart { name = "Shoes", lowerLeg = Rows(24, 18,
            "1. a oooooooo a 1.", "1. a oOoooooo oa", "1. a oOooooooo a", "1. a OOOOOOOOO a", "1. a ooooooooo a", "2. 10a") },
        new FighterPart { name = "Sneakers", lowerLeg = Rows(24, 18,
            "1. a oooooooo a 1.", "1. a oooooooo oa", "1. a ooooOoooo a", "1. a uuuuuuuuu a", "1. a ooooooooo a", "2. 10a") },
        new FighterPart { name = "Boots", lowerLeg = Rows(24, 15,
            "1. a oOoooooo a 1.", "1. a oOoooooo a 1.", "1. a oOoooooo a 1.",
            "1. a oooooooo a 1.", "1. a oOoooooo oa", "1. a oOooooooo a", "1. a uuuuuuuuu a", "1. a ooooooooo a", "2. 10a") },
    };

    // ---------------------------------------------------------------- helpers

    /// <summary>A layer that is empty except for <paramref name="rows"/> starting at row <paramref name="from"/>.</summary>
    static string[] Rows(int height, int from, params string[] rows)
    {
        var all = new string[height];
        for (int i = 0; i < height; i++) all[i] = "";
        for (int i = 0; i < rows.Length; i++) all[from + i] = rows[i];
        return all;
    }

    /// <summary>Pads a layer with empty rows up to <paramref name="height"/>.</summary>
    static string[] Fill(int height, string[] rows)
    {
        var all = new string[height];
        for (int i = 0; i < height; i++) all[i] = i < rows.Length ? rows[i] : "";
        return all;
    }

    /// <summary>A 20-row sleeve: a top row, then <paramref name="a"/> with <paramref name="b"/> every few rows.</summary>
    static string[] Repeat20(string top, string a, string b)
    {
        var rows = new string[20];
        rows[0] = top;
        for (int i = 1; i < 20; i++) rows[i] = i % 5 == 3 ? b : a;
        return rows;
    }
}
