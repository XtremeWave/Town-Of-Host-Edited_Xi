using System.Collections.Generic;
using static TOHEXI.Options;
using UnityEngine;
using static TOHEXI.Translator;
using static UnityEngine.GraphicsBuffer;
using TOHEXI.Modules;
using TOHEXI.Roles.Neutral;
using MS.Internal.Xml.XPath;
using Rewired.Utils.Platforms.Windows;
using System.Runtime.Intrinsics.Arm;
using AmongUs.GameOptions;

namespace TOHEXI.Roles.Impostor;

public static class Blackmailer
{
    private static readonly int Id = 1658974;
    private static List<byte> playerIdList = new();

    public static OptionItem SkillCooldown;
    public static Dictionary<byte, int> BlackmailerMaxUp;
    public static List<byte> ForBlackmailer = new ();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Blackmailer);
        SkillCooldown = FloatOptionItem.Create(Id + 42, "BlackmailerSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blackmailer])
           .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        BlackmailerMaxUp = new();
        ForBlackmailer = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = SkillCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }


}

