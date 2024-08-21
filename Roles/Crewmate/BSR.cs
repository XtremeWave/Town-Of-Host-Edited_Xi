using System;
using System.Collections.Generic;
using static TOHEXI.Options;

namespace TOHEXI;

public static class BSR
{
    private static readonly int Id = 11537055;
    public static List<byte> playerIdList = new();

    private static OptionItem DefaultKillCooldown;

    private static Dictionary<byte, float> NowCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.BSR);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "SansDefaultKillCooldown", new(0f, 180f, 2.5f), 65f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BSR])
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
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (target.GetCustomRole().IsCrewmate())
        {
            NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] * 2f, 300f, DefaultKillCooldown.GetFloat());
            killer.ResetKillCooldown();
            killer.SyncSettings();
        }
        if (target.GetCustomRole().IsNeutral())
        {
            NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] - 5f, 0f, DefaultKillCooldown.GetFloat());
            killer.ResetKillCooldown();
            killer.SyncSettings();
        }
        if (target.GetCustomRole().IsImpostor())
        {
            killer.RpcMurderPlayerV3(killer);
        }
        return false;
    }
}