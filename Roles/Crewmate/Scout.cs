using Hazel;
using System.Collections.Generic;
using TheOtherRoles_Host.Modules;
using TheOtherRoles_Host.Roles.Neutral;
using UnityEngine;
using static TheOtherRoles_Host.Translator;

namespace TheOtherRoles_Host.Roles.Crewmate;

public static class Scout
{
    private static readonly int Id = 126587;
    private static List<byte> playerIdList = new();
    public static Dictionary<byte, int> ScoutLimit = new();
    private static OptionItem SkillLimitOpt;
    private static OptionItem SkillCooldown;
    private static OptionItem ScoutRadius;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Scout);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "ScoutSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Scout])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimitOpt = IntegerOptionItem.Create(Id + 12, "ScoutSkillLimit", new(1, 990, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Scout])
            .SetValueFormat(OptionFormat.Times);
        ScoutRadius = FloatOptionItem.Create(1265427, "ScoutRadius", new(0.5f, 3f, 0.5f), 2f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Scout])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public static void Init()
    {
        playerIdList = new();
        ScoutLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ScoutLimit.TryAdd(playerId, SkillLimitOpt.GetInt());

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetScoutSellLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(ScoutLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (ScoutLimit.ContainsKey(PlayerId))
            ScoutLimit[PlayerId] = Limit;
        else
            ScoutLimit.Add(PlayerId, SkillLimitOpt.GetInt());
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (ScoutLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? SkillCooldown.GetFloat() : 300f;
    public static string GetSkillLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Scout) : Color.gray, ScoutLimit.TryGetValue(playerId, out var scoutLimit) ? $"({scoutLimit})" : "Invalid");
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (ScoutLimit[killer.PlayerId] < 1) return false;
        ScoutLimit[killer.PlayerId]--;
        killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RPCPlayCustomSound("Detection");
            killer.RpcGuardAndKill(target);
            killer.RpcGuardAndKill(killer);
            if (Main.ForSourcePlague.Contains(killer.PlayerId))
            {
                foreach (var player in Main.AllPlayerControls)
                {
                    if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                    if (player == killer) continue;
                    if (Vector2.Distance(killer.transform.position, player.transform.position) <= ScoutRadius.GetFloat())
                    {
                        if (player.GetCustomRole().IsImpostor())
                        {
                            killer.RpcGuardAndKill(player);
                            Main.ScoutImpotors[killer.PlayerId]++;
                            Main.ForSourcePlague.Add(player.PlayerId);
                        }
                        if (player.GetCustomRole().IsCrewmate())
                        {
                            killer.RpcGuardAndKill(player);
                            Main.ScoutCrewmate[killer.PlayerId]++;
                            Main.ForSourcePlague.Add(player.PlayerId);
                        }
                        if (player.GetCustomRole().IsNeutral())
                        {
                            killer.RpcGuardAndKill(player);
                            Main.ScoutNeutral[killer.PlayerId]++;
                            Main.ForSourcePlague.Add(player.PlayerId);
                        }
                    }
                }
            }
            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                if (player == killer) continue;
                if (Vector2.Distance(killer.transform.position, player.transform.position) <= ScoutRadius.GetFloat())
                {
                    if (player.GetCustomRole().IsImpostor())
                    {
                        killer.RpcGuardAndKill(player);
                        Main.ScoutImpotors[killer.PlayerId]++;
                    }
                    if (player.GetCustomRole().IsCrewmate())
                    {
                        killer.RpcGuardAndKill(player);
                        Main.ScoutCrewmate[killer.PlayerId]++;
                    }
                    if (player.GetCustomRole().IsNeutral())
                    {
                        killer.RpcGuardAndKill(player);
                        Main.ScoutNeutral[killer.PlayerId]++;
                    }
                }
            }
            killer.Notify(string.Format(GetString("ScoutOffGuard"), Main.ScoutImpotors[killer.PlayerId], Main.ScoutCrewmate[killer.PlayerId], Main.ScoutNeutral[killer.PlayerId]));
            if (Main.ScoutNeutral[killer.PlayerId] > 0)
            {
                Main.ScoutNeutral[killer.PlayerId] = 0;
            }
            if (Main.ScoutCrewmate[killer.PlayerId] > 0)
            {
                Main.ScoutCrewmate[killer.PlayerId] = 0;
            }
            if (Main.ScoutImpotors[killer.PlayerId] > 0)
            {
                Main.ScoutImpotors[killer.PlayerId] = 0;
            }
            return false;
   }
}