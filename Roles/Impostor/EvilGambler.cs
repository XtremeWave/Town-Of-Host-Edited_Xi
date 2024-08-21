using System;
using System.Collections.Generic;
using static TOHEXI.Options;

namespace TOHEXI;

public static class EvilGambler
{
    public static List<byte> playerIdList = new();
    public static OptionItem EvilGamblerKillCooldown;
    public static OptionItem EvilGamblerBetToWin;
    public static OptionItem EvilGamblerBetToWinKillCooldown;
    public static OptionItem EvilGamblerBetAndLoseKillCooldown;
    private static Dictionary<byte, float> NowCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(1254423, TabGroup.ImpostorRoles, CustomRoles.EvilGambler);
        EvilGamblerKillCooldown = FloatOptionItem.Create(1231241, "EvilGamblerKillCooldown", new(20f, 100f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilGambler])
            .SetValueFormat(OptionFormat.Seconds);
        EvilGamblerBetToWin = IntegerOptionItem.Create(2145475, "EvilGamblerBetToWin", new(0, 100, 5), 50, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilGambler])
       .SetValueFormat(OptionFormat.Percent);
        EvilGamblerBetToWinKillCooldown = FloatOptionItem.Create(53454534, "EvilGamblerBetToWinKillCooldown", new(0f, 10f, 0.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilGambler])
            .SetValueFormat(OptionFormat.Seconds);
        EvilGamblerBetAndLoseKillCooldown = FloatOptionItem.Create(12315344, "EvilGamblerBetAndLoseKillCooldown", new(30f, 250f, 1f), 40f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilGambler])
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
        NowCooldown.TryAdd(playerId, EvilGamblerKillCooldown.GetFloat());
    }
    public static bool IsEnable() => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public static void OnCheckMurder(PlayerControl killer)
    {
        if (killer.Is(CustomRoles.OldThousand))
        {
            NowCooldown[killer.PlayerId] = EvilGamblerBetToWinKillCooldown.GetFloat();
            killer.ResetKillCooldown();
            killer.SyncSettings();
        }
        var Eg = IRandom.Instance;
        if (Eg.Next(0, 100) < EvilGamblerBetToWin.GetInt())
        {
            NowCooldown[killer.PlayerId] = EvilGamblerBetToWinKillCooldown.GetFloat();
            killer.SetKillCooldownV2();
            killer.MarkDirtySettings();
        }
        else
        {
            NowCooldown[killer.PlayerId] = EvilGamblerBetAndLoseKillCooldown.GetFloat();
            killer.SetKillCooldownV2();
            killer.MarkDirtySettings();
        }
    }
}
