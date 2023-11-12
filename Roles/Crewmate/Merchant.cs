using Hazel;
using LibCpp2IL;
using System.Collections.Generic;
using TheOtherRoles_Host.Modules;
using UnityEngine;
using static TheOtherRoles_Host.Translator;

namespace TheOtherRoles_Host.Roles.Crewmate;

public static class Merchant
{
    private static readonly int Id = 11567789;
    private static List<byte> playerIdList = new();
    public static Dictionary<byte, int> MerchantLimit = new();
    public static OptionItem SkillLimitOpt;
    public static OptionItem SkillCooldown;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Merchant);
        SkillCooldown = FloatOptionItem.Create(Id + 42, "MerchantSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Merchant])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimitOpt = IntegerOptionItem.Create(Id + 44, "MerchantSkillLimit", new(1, 990, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Merchant])
             .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        MerchantLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        MerchantLimit.TryAdd(playerId, SkillLimitOpt.GetInt());

          if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public static void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        MerchantLimit.Remove(playerId);

          if (!AmongUsClient.Instance.AmHost) return;
        if (Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Remove(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (MerchantLimit.ContainsKey(PlayerId))
            MerchantLimit[PlayerId] = Limit;
        else
            MerchantLimit.Add(PlayerId, SkillLimitOpt.GetInt());
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (MerchantLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? SkillCooldown.GetFloat() : 300f;
    public static string GetSkillLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Deputy) : Color.gray, MerchantLimit.TryGetValue(playerId, out var constableLimit) ? $"({constableLimit})" : "Invalid");
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {      
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(killer);
        var Mt = IRandom.Instance;
        int MT = Mt.Next(0, 2);
        var Mn = IRandom.Instance;
        int MN = Mn.Next(1, 5);        
        if (MT == 0 && MN == 2)
        {
            MerchantLimit[killer.PlayerId]--;
            killer.Notify(GetString("OfMerchant"));
            target.Notify(GetString("ForMerchant"));
            Main.MerchantProject.Add(target.PlayerId);
        }
        else if(MT == 1 && MN == 5)
        {
            MerchantLimit[killer.PlayerId]--;
            killer.Notify(GetString("OfMerchant"));
            target.Notify(GetString("ForMerchant"));
            Main.MerchantLeiDa.Add(target.PlayerId);
        }
        else if (MT == 2 && MN == 3)
        {
            MerchantLimit[killer.PlayerId]--;
            killer.Notify(GetString("OfMerchant"));
            target.Notify(GetString("ForMerchant"));
            var taskState = target.GetPlayerTaskState();
            taskState.AllTasksCount -= Main.MerchantTaskMax;
            taskState.CompletedTasksCount++;
            GameData.Instance.RpcSetTasks(target.PlayerId, new byte[0]); //タスクを再配布
            target.SyncSettings();
            Utils.NotifyRoles(target);
        }
        else
        {
            killer.Notify(GetString("NotMoney"));
        }
        return false;
    }
}