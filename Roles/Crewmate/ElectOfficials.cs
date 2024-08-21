using Hazel;
using System.Collections.Generic;
using TOHEXI.Modules;
using UnityEngine;
using static TOHEXI.Translator;

namespace TOHEXI.Roles.Crewmate;

public static class ElectOfficials
{
    private static readonly int Id = 1235874;
    private static List<byte> playerIdList = new();
    public static Dictionary<byte, int> ElectLimit = new();
    public static OptionItem SkillCooldown;
    public static OptionItem CanImpostorAndNeutarl;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ElectOfficials);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "ElectOfficialsSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ElectOfficials])
            .SetValueFormat(OptionFormat.Seconds);
        CanImpostorAndNeutarl = BooleanOptionItem.Create(Id + 16, "CanImpostorAndNeutarl", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ElectOfficials]);
    }
    public static void Init()
    {
        playerIdList = new();
        ElectLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ElectLimit.TryAdd(playerId, 1);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetElectLimlit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(ElectLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (ElectLimit.ContainsKey(PlayerId))
            ElectLimit[PlayerId] = Limit;
        else
            ElectLimit.Add(PlayerId, 1);
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (ElectLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? SkillCooldown.GetFloat() : 300f;
    public static string GetSkillLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.ElectOfficials) : Color.gray, ElectLimit.TryGetValue(playerId, out var electLimit) ? $"({electLimit})" : "Invalid");
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        ElectLimit[killer.PlayerId]--;
        if (target.GetCustomRole().IsCrewmate() || !target.GetCustomRole().IsNeutralKilling() && !target.GetCustomRole().IsImpostor())
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ElectOfficials), GetString("ElectOfficialsSuccessfullyRecruited")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ElectOfficials), GetString("ForElectOfficials")));
            Utils.NotifyRoles();
            target.RpcSetCustomRole(CustomRoles.Mayor);
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
            return true;
        }
        else
        {
            if (CanImpostorAndNeutarl.GetBool())
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ElectOfficials), GetString("ElectOfficialsSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ElectOfficials), GetString("ForElectOfficials")));
                Utils.NotifyRoles();
                target.RpcSetCustomRole(CustomRoles.Mayor);
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);
                return true;
            }
            killer.RpcMurderPlayerV3(killer);
            return true;
        }
    }
}