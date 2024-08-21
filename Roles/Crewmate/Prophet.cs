using Hazel;
using System.Collections.Generic;
using TOHEXI.Modules;
using UnityEngine;
using static TOHEXI.Translator;

namespace TOHEXI.Roles.Crewmate;

public static class Prophet
{
    private static readonly int Id = 14789213;
    private static List<byte> playerIdList = new();
    public static Dictionary<byte, int> ProphetLimit = new();
  private static OptionItem SkillLimitOpt;
    private static OptionItem SkillCooldown;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Prophet);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "ProphetSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prophet])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimitOpt = IntegerOptionItem.Create(Id + 12, "ProphetSkillLimit", new(1, 990, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Prophet])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        ProphetLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ProphetLimit.TryAdd(playerId, SkillLimitOpt.GetInt());

       if (!AmongUsClient.Instance.AmHost) return;
       if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
   }
   public static bool IsEnable => playerIdList.Count > 0;
   private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetProphetSellLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(ProphetLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (ProphetLimit.ContainsKey(PlayerId))
            ProphetLimit[PlayerId] = Limit;
        else
            ProphetLimit.Add(PlayerId, SkillLimitOpt.GetInt());
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (ProphetLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? SkillCooldown.GetFloat() : 300f;
    public static string GetSkillLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Prophet) : Color.gray, ProphetLimit.TryGetValue(playerId, out var prophetLimit) ? $"({prophetLimit})" : "Invalid");
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (ProphetLimit[killer.PlayerId] < 1)
        if (Main.ForSourcePlague.Contains(killer.PlayerId))
        {
            if (target.GetCustomRole().IsNeutral())
            {
                    Main.ForSourcePlague.Add(target.PlayerId);
               ProphetLimit[killer.PlayerId]--;
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#FF0000");
                NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Prophet), GetString("ProphetBad")));
                    return false;
                }
            if (target.GetCustomRole().IsCrewmate())
            {
                    Main.ForSourcePlague.Add(target.PlayerId);
                    ProphetLimit[killer.PlayerId]--;
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                killer.RpcGuardAndKill(killer);
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#66ff00");
                NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Prophet), GetString("ProphetNice")));
                    return false;
                }
            if (target.GetCustomRole().IsImpostor())
            {
                    Main.ForSourcePlague.Add(target.PlayerId);
                    ProphetLimit[killer.PlayerId]--;
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#FF0000");
                NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Prophet), GetString("ProphetBad")));
                    return false;
                }
        }
        if (target.GetCustomRole().IsNeutral())
        {
            ProphetLimit[killer.PlayerId]--;
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            NameColorManager.Add(killer.PlayerId, target.PlayerId, "#FF0000");
            NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Prophet), GetString("ProphetBad")));
            return false;
        }
        if (target.GetCustomRole().IsCrewmate())
        {
            ProphetLimit[killer.PlayerId]--;
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            killer.RpcGuardAndKill(killer);
            NameColorManager.Add(killer.PlayerId, target.PlayerId, "#66ff00");
            NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Prophet), GetString("ProphetNice")));
            return false;
        }
        if (target.GetCustomRole().IsImpostor())
        {
           ProphetLimit[killer.PlayerId]--;
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            NameColorManager.Add(killer.PlayerId, target.PlayerId, "#FF0000");
            NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Prophet), GetString("ProphetBad")));
            return false;
        }
        return false;
    }
}