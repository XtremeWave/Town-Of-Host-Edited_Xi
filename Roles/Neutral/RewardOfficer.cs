using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheOtherRoles_Host.Roles.Neutral;
using TheOtherRoles_Host;
using static TheOtherRoles_Host.Options;
using MS.Internal.Xml.XPath;
using UnityEngine;
using static Il2CppSystem.Net.Http.Headers.Parser;
using static UnityEngine.GraphicsBuffer;
using Sentry.Internal;
using static TheOtherRoles_Host.Translator;


namespace TheOtherRoles_Host.Roles.Neutral;
public static class RewardOfficer
{
    private static readonly int Id = 452056;
    public static List<byte> playerIdList = new();

    public static OptionItem HenryCanSee;
    public static OptionItem TargetKnowsYandere;
    public static OptionItem SkillCooldown;
    public static OptionItem NeedChoose;
    public static OptionItem RewardOfficerCanMode;
    public static readonly string[] rewardOfficerCountMode =
      {
        "RewardOfficerCanMode.Roles",
        "RewardOfficerCanMode.Player",
    };
    public static List<byte> ForRewardOfficer = new();
    public static List<byte> RewardOfficerShow = new(); 
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.RewardOfficer);
        RewardOfficerCanMode = StringOptionItem.Create(Id + 18, "RewardOfficerCanMode", rewardOfficerCountMode, 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.RewardOfficer]);
    }
    public static void Init()
    {
        playerIdList = new();
        ForRewardOfficer = new();
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != playerId).ToList();
        var Ro = pcList[IRandom.Instance.Next(0, pcList.Count)];
        ForRewardOfficer.Add(Ro.PlayerId);
        RewardOfficerShow.Add(playerId);
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
     if (ForRewardOfficer.Contains(target.PlayerId))
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.RewardOfficer);
            CustomWinnerHolder.WinnerIds.Add(killer.PlayerId);
            return false;
        }
     else
        {
            killer.RpcMurderPlayerV3(killer);
            return true;
        }
    }
    public static void FixedUpdate(PlayerControl player)
    {
        if (player.Is(CustomRoles.RewardOfficer)) return; //以下、バウンティハンターのみ実行
        if (ForRewardOfficer.Contains(player.PlayerId) && !player.IsAlive())
        {
            ForRewardOfficer.Remove(player.PlayerId);
            var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != player.PlayerId).ToList();
            var Ro = pcList[IRandom.Instance.Next(0, pcList.Count)];
            ForRewardOfficer.Add(Ro.PlayerId);
        }
    }

}



