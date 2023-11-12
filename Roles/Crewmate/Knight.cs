using Hazel;
using System.Collections.Generic;
using TheOtherRoles_Host.Modules;
using UnityEngine;

namespace TheOtherRoles_Host.Roles.Crewmate;

public static class Knight
{
    private static readonly int Id = 135467486;
    private static List<byte> playerIdList = new();
    public static Dictionary<byte, int> KnightLimit = new();
    public static OptionItem SkillLimitOpt;
    public static OptionItem SkillCooldown;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Knight);
        SkillCooldown = FloatOptionItem.Create(Id + 42, "KnightSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Seconds);
       SkillLimitOpt = IntegerOptionItem.Create(Id + 44, "KnightSkillLimit", new(1, 990, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        KnightLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        KnightLimit.TryAdd(playerId, SkillLimitOpt.GetInt());

          if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public static void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        KnightLimit.Remove(playerId);

          if (!AmongUsClient.Instance.AmHost) return;
        if (Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Remove(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (KnightLimit.ContainsKey(PlayerId))
            KnightLimit[PlayerId] = Limit;
        else
            KnightLimit.Add(PlayerId, SkillLimitOpt.GetInt());
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (KnightLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? SkillCooldown.GetFloat() : 300f;
    public static string GetSkillLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Deputy) : Color.gray, KnightLimit.TryGetValue(playerId, out var constableLimit) ? $"({constableLimit})" : "Invalid");
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (KnightLimit[killer.PlayerId] <= 0) return false;
        Main.ForKnight.Remove(target.PlayerId);
        Main.ForKnight.Add(target.PlayerId);
        KnightLimit[killer.PlayerId]--;
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(killer);
        return false;
    }
}
