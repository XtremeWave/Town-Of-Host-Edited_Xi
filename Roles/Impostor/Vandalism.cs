using System;
using System.Collections.Generic;
using static TheOtherRoles_Host.Options;
using Hazel;
using TheOtherRoles_Host.Modules;
using UnityEngine;
using AmongUs.GameOptions;
using InnerNet;
using HarmonyLib;
using System.Linq;

namespace TheOtherRoles_Host;

public static class Vandalism
{
    private static readonly int Id = 132475454;
    public static List<byte> playerIdList = new();
    public static Dictionary<byte, int> VandalismLimit = new();

    private static OptionItem SkillLimitOpt;
    private static OptionItem SkillCooldown;
    public static OptionItem InstanceReactors;
    public static OptionItem InstanceOxygens;
    public static OptionItem InstanceComms;
    public static OptionItem InstanceElectrical;
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Vandalism);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vandalism])
                .SetValueFormat(OptionFormat.Seconds);
        SkillLimitOpt = FloatOptionItem.Create(Id + 11, "VandalismSkillLimitOpt", new(0f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vandalism])
                .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        VandalismLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        VandalismLimit.TryAdd(playerId, SkillLimitOpt.GetInt());

          if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetVandalismSellLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(VandalismLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (VandalismLimit.ContainsKey(PlayerId))
            VandalismLimit[PlayerId] = Limit;
        else
            VandalismLimit.Add(PlayerId, SkillLimitOpt.GetInt());
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = SkillCooldown.GetFloat();
    public static string GetVandalismLimit(byte playerId) => Utils.ColorString((VandalismLimit.TryGetValue(playerId, out var x) && x >= 1) ? Color.red : Color.gray, VandalismLimit.TryGetValue(playerId, out var vandalismLimit) ? $"({vandalismLimit})" : "Invalid");
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (VandalismLimit[killer.PlayerId] <= 0)
        {
            target.RpcMurderPlayerV3(killer);
        }
            VandalismLimit[killer.PlayerId]--;
            MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, killer.GetClientId());
            SabotageFixWriter.Write((byte)SystemTypes.Electrical);
            MessageExtensions.WriteNetObject(SabotageFixWriter, killer);
            AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
            foreach (var player in Main.AllPlayerControls)
            {
                if (player.PlayerId == killer.PlayerId || player.Data.Disconnected) continue;
                SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, player.GetClientId());
                SabotageFixWriter.Write((byte)SystemTypes.Electrical);
                MessageExtensions.WriteNetObject(SabotageFixWriter, player);
                AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
            }
    }
}