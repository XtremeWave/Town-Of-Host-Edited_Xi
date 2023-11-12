using Hazel;
using MS.Internal.Xml.XPath;
using System;
using System.Collections.Generic;
using static TheOtherRoles_Host.Translator;
using static TheOtherRoles_Host.Options;

namespace TheOtherRoles_Host;

public static class SoulSucker
{
    private static readonly int Id = 2189985;
    public static List<byte> playerIdList = new();

    private static OptionItem DefaultKillCooldown;
    private static OptionItem SoulCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;

    private static Dictionary<byte, float> NowCooldown;
    public static void SetupCustomOption()
    {

        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.SoulSucker);
        SoulCooldown = FloatOptionItem.Create(Id + 10, "SoulCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulSucker])
            .SetValueFormat(OptionFormat.Seconds);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 14, "SansDefaultKillCooldown", new(0f, 180f, 2.5f), 65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulSucker])
    .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "SansReduceKillCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulSucker])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, "SansMinKillCooldown", new(0f, 180f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SoulSucker])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        NowCooldown = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
    }
    public static bool IsEnable() => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = SoulCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public static void OnShapeshift(PlayerControl pc, PlayerControl target)
    {
        if (!pc.Is(CustomRoles.SoulSucker) || target == null || pc == target || !target.IsAlive() || target.GetCustomRole().IsImpostorTeam()) return;
        NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledBySoulSucker")));
        Main.PlayerStates[target.PlayerId].SetDead();
        target.SetRealKiller(pc);
        NowCooldown[pc.PlayerId] = Math.Clamp(NowCooldown[pc.PlayerId] - ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        pc.SyncSettings();
        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Soul;
    }
}