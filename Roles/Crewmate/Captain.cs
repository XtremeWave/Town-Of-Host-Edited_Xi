using Hazel;
using System.Collections.Generic;
using TheOtherRoles_Host.Roles.Double;
using UnityEngine;
using static TheOtherRoles_Host.Options;
using static TheOtherRoles_Host.Translator;

namespace TheOtherRoles_Host.Roles.Crewmate;

public static class Captain
{
    private static readonly int Id = 7565110;
    private static List<byte> playerIdList = new();
    public static Dictionary<byte, int> SolicitLimit = new();

    public static OptionItem SolicitCooldown;
    public static OptionItem SolicitCooldownIncrese;
    public static OptionItem SolicitMax;
    public static OptionItem KnowTargetRole;
    public static OptionItem TargetKnowOtherTarget;


    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Captain, 1, zeroOne: false);
        SolicitCooldown = FloatOptionItem.Create(Id + 10, "CaptainSolicitCooldown", new(0f, 990f, 2.5f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain])
            .SetValueFormat(OptionFormat.Seconds);
        SolicitCooldownIncrese = FloatOptionItem.Create(Id + 11, "CaptainSolicitCooldownIncrese", new(0f, 180f, 2.5f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain])
            .SetValueFormat(OptionFormat.Seconds);
        SolicitMax = IntegerOptionItem.Create(Id + 12, "CaptainSolicitMax", new(1, 15, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "CaptainKnowTargetRole", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain])
            .SetHidden(true);
        TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "CaptainTargetKnowOtherTarget", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Captain]);
    }
    public static void Init()
    {
        playerIdList = new();
        SolicitLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SolicitLimit.TryAdd(playerId, SolicitMax.GetInt());

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCaptainSolicitLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(SolicitLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (SolicitLimit.ContainsKey(PlayerId))
            SolicitLimit[PlayerId] = Limit;
        else
            SolicitLimit.Add(PlayerId, SolicitMax.GetInt());
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (SolicitLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;
    public static void SetKillCooldown(byte id)
    {
        if (!CanUseKillButton(id))
        {
            Main.AllPlayerKillCooldown[id] = 300;
            return;
        }
        float cd = SolicitCooldown.GetFloat();
        cd = SolicitCooldown.GetFloat() + SolicitCooldownIncrese.GetFloat();
        Main.AllPlayerKillCooldown[id] = cd;
    }
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (SolicitLimit[killer.PlayerId] < 1) return;
        if (Mini.Age != 18 && !(target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) || Mini.Age == 18)
        {
                SolicitLimit[killer.PlayerId]--;
                SendRPC(killer.PlayerId);
                    if (!target.CanUseKillButton() && !target.Is(CustomRoles.Solicited) || !target.Is(CustomRoles.Believer) || !target.Is(CustomRoles.NiceMini) && Mini.Age != 18 || !target.Is(CustomRoles.EvilMini) && Mini.Age != 18)
                    {
                        target.RpcSetCustomRole(CustomRoles.Solicited);
                    }
                    if (target.CanUseKillButton() && !target.Is(CustomRoles.Solicited) || !target.Is(CustomRoles.Believer) || !target.Is(CustomRoles.NiceMini) && Mini.Age != 18 || !target.Is(CustomRoles.EvilMini) && Mini.Age != 18)
                    {
                        target.RpcSetCustomRole(CustomRoles.Solicited);
                    }
                

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Captain), GetString("CaptainSolicitedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Captain), GetString("SolicitedByCaptain")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
        }
        else if (Mini.Age != 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Captain), GetString("Cantkillkid")));
        }
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Solicited) && target.Is(CustomRoles.Captain)) return true;
        if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Captain) && target.Is(CustomRoles.Solicited)) return true;
        if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Solicited) && target.Is(CustomRoles.Solicited)) return true;
        return false;
    }
    public static string GetSolicitLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Captain) : Color.gray, SolicitLimit.TryGetValue(playerId, out var solicitLimit) ? $"({solicitLimit})" : "Invalid");
    
}
