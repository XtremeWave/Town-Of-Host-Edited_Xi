using Hazel;
using System.Collections.Generic;
using TheOtherRoles_Host.Modules;
using UnityEngine;

namespace TheOtherRoles_Host.Roles.Neutral;

public static class Prosecutors
{
    private static readonly int Id = 7565120;
    private static List<byte> playerIdList = new();
   public static Dictionary<byte, int> ProsecutorsLimit = new();
    public static OptionItem SkillLimitOpt;
    public static OptionItem SkillCooldown;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Prosecutors);
    }
    public static void Init()
    {
        playerIdList = new();
        ProsecutorsLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ProsecutorsLimit.TryAdd(playerId, SkillLimitOpt.GetInt());

          if (!AmongUsClient.Instance.AmHost) return;
       if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetProsecutorsSellLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(ProsecutorsLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (ProsecutorsLimit.ContainsKey(PlayerId))
            ProsecutorsLimit[PlayerId] = Limit;
        else
            ProsecutorsLimit.Add(PlayerId, SkillLimitOpt.GetInt());
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (ProsecutorsLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? SkillCooldown.GetFloat() : 300f;
    public static string GetSkillLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Prosecutors) : Color.gray, ProsecutorsLimit.TryGetValue(playerId, out var prosecutorsLimit) ? $"({prosecutorsLimit})" : "Invalid");
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
   {
        if (ProsecutorsLimit[killer.PlayerId] <= 0)
        {
            NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Prosecutors), ("你没有空包弹了"))); ;
            return false;
        }
       Main.ProsecutorsInProtect.Remove(target.PlayerId);
       Main.ProsecutorsInProtect.Add(target.PlayerId);
      ProsecutorsLimit[killer.PlayerId]--;
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(killer);
        return false;
    }
}