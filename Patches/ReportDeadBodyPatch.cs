using AmongUs.GameOptions;
using HarmonyLib;
using MS.Internal.Xml.XPath;
using Sentry.Internal.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheOtherRoles_Host.Roles.Crewmate;
using TheOtherRoles_Host.Roles.Impostor;
using TheOtherRoles_Host.Roles.Neutral;
using UnityEngine;
using static TheOtherRoles_Host.Translator;
using Hazel;
using InnerNet;
using System.Threading.Tasks;
using TheOtherRoles_Host.Modules;
using TheOtherRoles_Host.Roles.AddOns.Crewmate;
using UnityEngine.Profiling;
using System.Runtime.Intrinsics.X86;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UI;
using UnityEngine.Networking.Types;
using TheOtherRoles_Host.Roles.Double;
using Microsoft.Extensions.Logging;
using Sentry;
using UnityEngine.SocialPlatforms;
using static UnityEngine.ParticleSystem.PlaybackState;
using Cpp2IL.Core.Extensions;

namespace TheOtherRoles_Host;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
class ReportDeadBodyPatch
{
    public static Dictionary<byte, bool> CanReport;
    public static Dictionary<byte, List<GameData.PlayerInfo>> WaitReport = new();
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
    {
        if (GameStates.IsMeeting) return false;
        if (Options.DisableMeeting.GetBool()) return false;
        if (Options.CurrentGameMode == CustomGameMode.SoloKombat) return false;
        if (Options.CurrentGameMode == CustomGameMode.HotPotato) return false;
        if (Options.CurrentGameMode == CustomGameMode.TheLivingDaylights) return false;
        if (!CanReport[__instance.PlayerId])
        {
            WaitReport[__instance.PlayerId].Add(target);
            Logger.Warn($"{__instance.GetNameWithRole()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");
            return false;
        }

        Logger.Info($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");

        foreach (var kvp in Main.PlayerStates)
        {
            var pc = Utils.GetPlayerById(kvp.Key);
            kvp.Value.LastRoom = pc.GetPlainShipRoom();
        }
        if (!AmongUsClient.Instance.AmHost)
        {
            foreach (var mimickiller in Main.AllAlivePlayerControls)
            {
                if (mimickiller.Is(CustomRoles.MimicKiller))
                {
                    Mimics.revert();
                }
                return true;
            }
        }

        try
        {
            //通報者が死んでいる場合、本処理で会議がキャンセルされるのでここで止める
            if (__instance.Data.IsDead) return false;

            //=============================================
            //以下检查是否允许本次会议
            //=============================================

            var killer = target?.Object?.GetRealKiller();
            var killerRole = killer?.GetCustomRole();

            //杀戮机器无法报告或拍灯
            if (__instance.Is(CustomRoles.Minimalism)) return false;
            //禁止小黑人报告
            if (((Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool()) || Concealer.IsHidding) && Options.DisableReportWhenCC.GetBool()) return false;
            //  //扰乱技能中禁止报告
            //  if (Main.sabcatInProtect.ContainsKey(target.PlayerId))
            //    if (Main.sabcatInProtect[target.PlayerId] + Options.sabcatCooldown.GetInt() >= //Utils.GetTimeStamp(DateTime.Now))
            //  {
            //     Main.PlayerStates[killer.PlayerId].deathReason = //PlayerState.DeathReason.PissedOff;
            //     return false;
            //}

            if (target == null) //拍灯事件
            {
                if (__instance.Is(CustomRoles.Jester) && !Options.JesterCanUseButton.GetBool()) return false;

                if (__instance.Is(CustomRoles.NiceSwapper) && !NiceSwapper.CanStartMeeting.GetBool()) return false;
                if (__instance.Is(CustomRoles.EIReverso))
                {
                    __instance?.NoCheckStartMeeting(__instance?.Data);
                    return false;
                }
                if (__instance.Is(CustomRoles.Henry))
                {
                    if (Henry.ChooseMax[__instance.PlayerId] <= 0)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Henry);
                        CustomWinnerHolder.WinnerIds.Add(__instance.PlayerId);
                    }
                    if (Henry.Choose == 3)
                    {
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("HenryYES!")));
                        Henry.ChooseMax[__instance.PlayerId]--;
                        Henry.SendRPC(__instance.PlayerId);
                        __instance.RpcGuardAndKill(__instance);
                        var Dy = IRandom.Instance;
                        int rndNum = Dy.Next(0, 4);
                        Henry.Choose = rndNum;
                        Henry.ChooseMax.TryAdd(__instance.PlayerId, Henry.NeedChoose.GetInt());
                    }
                    else
                    {
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "FALL"));
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "NotMeeting"));
                        __instance.RpcMurderPlayerV3(__instance);
                        return false;
                    }
                }
            }
            else //报告尸体事件
            {

                // 清洁工来扫大街咯
                if (__instance.Is(CustomRoles.Cleaner))
                {
                    Main.CleanerBodies.Remove(target.PlayerId);
                    Main.CleanerBodies.Add(target.PlayerId);
                    __instance.RPCPlayCustomSound("Clear");
                    __instance.Notify(GetString("CleanerCleanBody"));
                    Logger.Info($"{__instance.GetRealName()} 清理了 {target.PlayerName} 的尸体", "Cleaner");
                    return false;
                }
                // 秃鹫吞噬尸体
                if (__instance.Is(CustomRoles.Vulture))
                {
                    Main.VultureBodies.Remove(target.PlayerId);
                    Main.VultureBodies.Add(target.PlayerId);
                    __instance.RPCPlayCustomSound("Eat");
                    __instance.Notify(GetString("VultureCleanBody"));
                    Logger.Info($"{__instance.GetRealName()} 吞噬了 {target.PlayerName} 的尸体", "Cleaner");
                    Main.VultureEatMax[__instance.PlayerId]++;
                    if (Main.VultureEatMax[__instance.PlayerId] >= Vulture.VultureEat.GetInt())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Vulture);
                        CustomWinnerHolder.WinnerIds.Add(__instance.PlayerId);
                    }
                    return false;
                }
                // 失忆者无法报告尸体
                if (__instance.Is(CustomRoles.Amnesiac))
                {
                    Amnesiac.OnReportDeadBody(__instance, target);
                    Logger.Info("失忆者正常进入无法报告", "Amnesiac");
                    return false;
                }
                // 失忆者无法报告尸体第二次尝试阻止！！！
                if (__instance.Is(CustomRoles.Amnesiac))
                {
                    Amnesiac.OnReportDeadBody(__instance, target);
                    Logger.Info("失忆者正常进入无法报告--第二次尝试阻止", "Amnesiac");
                    return false;
                }
                //反转侠报告尸体
                if (__instance.Is(CustomRoles.EIReverso))
                {
                    __instance?.ReportDeadBody(null);
                    return false;
                }

                // 被赌杀的尸体无法被报告
                if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Gambled) return false;

                // 清道夫的尸体无法被报告
                if (killerRole == CustomRoles.Scavenger) return false;

                // 银狼的尸体无法被报告
                if (killerRole == CustomRoles.YinLang) return false;

                // 被清理的尸体无法报告
                if (Main.CleanerBodies.Contains(target.PlayerId))
                {
                    NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cleaner), GetString("CleanerNot__instance")));
                    return false;
                }
                //被磁铁人干扰
                if(Main.ForMagnetMan.Contains(__instance.PlayerId)) return false;

                //被吞噬的尸体无法报告
                if (Main.VultureBodies.Contains(target.PlayerId))
                {
                    NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Vulture), GetString("VultureNot__instance")));
                    return false;
                }
                //被石化的尸体无法报告
                if (Main.ForMedusa.Contains(target.PlayerId))
                {
                    NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medusa), GetString("MedusaNot__instance")));
                    return false;
                }
                //亨利报告
                if (__instance.Is(CustomRoles.Henry))
                {
                    if (Henry.ChooseMax[__instance.PlayerId] <= 0)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Henry);
                        CustomWinnerHolder.WinnerIds.Add(__instance.PlayerId);
                    }
                    if (Henry.Choose == 4)
                    {
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("HenryYES!")));
                        Henry.ChooseMax[__instance.PlayerId]--;
                        Henry.SendRPC(__instance.PlayerId);
                        __instance.RpcGuardAndKill(__instance);
                    }
                    else
                    {
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "FALL"));
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "NotReport"));
                        __instance.RpcMurderPlayerV3(__instance);
                        var Dy = IRandom.Instance;
                        int rndNum = Dy.Next(0, 4);
                        Henry.Choose = rndNum;
                        Henry.ChooseMax.TryAdd(__instance.PlayerId, Henry.NeedChoose.GetInt());
                        return false;
                    }
                }

                // 胆小鬼不敢报告
                if (__instance.Is(CustomRoles.Oblivious) && (target?.Object == null || !target.Object.Is(CustomRoles.Bait))) return false;

                // 报告了诡雷尸体
                if (Main.BoobyTrapBody.Contains(target.PlayerId) && __instance.IsAlive())
                {
                    var killerID = Main.KillerOfBoobyTrapBody[target.PlayerId];
                    Main.PlayerStates[__instance.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    __instance.SetRealKiller(Utils.GetPlayerById(killerID));

                    __instance.RpcMurderPlayerV3(__instance);
                    RPC.PlaySoundRPC(killerID, Sounds.KillSound);

                    if (!Main.BoobyTrapBody.Contains(__instance.PlayerId)) Main.BoobyTrapBody.Add(__instance.PlayerId);
                    if (!Main.KillerOfBoobyTrapBody.ContainsKey(__instance.PlayerId)) Main.KillerOfBoobyTrapBody.Add(__instance.PlayerId, killerID);
                    return false;
                }
            }

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.Info("最大:" + Options.SyncedButtonCount.GetInt() + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
                if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
                {
                    Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                    return false;
                }
                else Options.UsedButtonCount++;
                if (Options.SyncedButtonCount.GetFloat() == Options.UsedButtonCount)
                {
                    Logger.Info("使用可能ボタン回数が最大数に達しました。", "ReportDeadBody");
                }
            }

            AfterReportTasks(__instance, target);

        }
        catch (Exception e)
        {
            Logger.Exception(e, "ReportDeadBodyPatch");
            Logger.SendInGame("Error: " + e.ToString());
        }

        foreach (var mimickiller in Main.AllAlivePlayerControls)
        {
            if (mimickiller.Is(CustomRoles.MimicKiller))
            {
                Mimics.revert();
            }


        }
        return true;
    }
    public static void AfterReportTasks(PlayerControl player, GameData.PlayerInfo target)
    {
        //=============================================
        //以下、ボタンが押されることが確定したものとする。
        //=============================================

        if (target == null) //ボタン
        {
            if (player.Is(CustomRoles.Mayor))
            {
                Main.MayorUsedButtonCount[player.PlayerId] += 1;
            }
            //操控者
            /*if (player.Is(CustomRoles.Manipulator) || !player.Is(CustomRoles.Manipulator))
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (!pc.IsAlive()) continue;
                    if (pc.GetCustomRole().IsImpostor())
                    {
                        var Im = IRandom.Instance;
                        int Ma = Im.Next(0, 100);
                        if (Ma < Options.DepressedIdioctoniaProbability.GetInt())
                        {
                            Main.ManipulatorNeutral[player.PlayerId] += Im.Next(0, 10);
                        }
                        else
                        {
                            Main.ManipulatorImpotors[player.PlayerId]++;
                        }
                    }
                    if (pc.GetCustomRole().IsCrewmate())
                    {
                        var Cr = IRandom.Instance;
                        int Mn = Cr.Next(0, 100);
                        if (Mn < Options.DepressedIdioctoniaProbability.GetInt())
                        {
                            Main.ManipulatorNeutral[player.PlayerId] += Cr.Next(0, 10);
                        }
                        else
                        {
                            Main.ManipulatorCrewmate[player.PlayerId]++;
                        }
                    }
                    if (pc.GetCustomRole().IsNeutral())
                    {
                        var Ne = IRandom.Instance;
                        int Mi = Ne.Next(0, 100);
                        if (Mi < Options.DepressedIdioctoniaProbability.GetInt())
                        {
                            Main.ManipulatorNeutral[player.PlayerId] += Ne.Next(0, 10);
                        }
                        else
                        {
                            Main.ManipulatorNeutral[player.PlayerId]++;
                        }
                    }
                }
                string msg;
                msg = string.Format(GetString("ManipulatorNoticeVictim"), Main.ManipulatorImpotors[player.PlayerId], Main.ManipulatorCrewmate[player.PlayerId], Main.ManipulatorNeutral[player.PlayerId]);
                Main.ManipulatorNotify.Add(player.PlayerId, msg);
                new LateTask(() =>
                {
                    Main.ManipulatorImpotors[player.PlayerId] = 0;
                    Main.ManipulatorNeutral[player.PlayerId] = 0;
                    Main.ManipulatorCrewmate[player.PlayerId] = 0;
                    Utils.NotifyRoles();
                }, 15f, ("清空"));
            }*/
        }
        else
        {
            var tpc = Utils.GetPlayerById(target.PlayerId);
            if (tpc != null && !tpc.IsAlive())
            {
                // 撅暮报告
                if (player.Is(CustomRoles.Detective))
                {
                    string msg;
                    Logger.Info("即将进入循环，请稍后", "Detective");
                    msg = string.Format(GetString("DetectiveNoticeVictim"), tpc.GetRealName(), tpc.GetDisplayRoleName());
                    if (Options.DetectiveCanknowKiller.GetBool())
                    {
                        Logger.Info("正常进入循环", "Detective");
                        var realKiller = tpc.GetRealKiller();
                        if (realKiller == null) msg += "；" + GetString("DetectiveNoticeKillerNotFound");
                        else msg += "；" + string.Format(GetString("DetectiveNoticeKiller"), realKiller.GetDisplayRoleName());
                    }
                    Main.DetectiveNotify.Add(player.PlayerId, msg);
                }
                if (player.Is(CustomRoles.DemonHunterm))
                {
                    DemonHunterm.DemonHunterLimit[player.PlayerId]++;
                }
            }
        }
        Main.LastVotedPlayerInfo = null;
        Main.ArsonistTimer.Clear();
        Main.PuppeteerList.Clear();
        Main.GuesserGuessed.Clear();
        Main.VeteranInProtect.Clear();
        Main.GrenadierBlinding.Clear();
        Main.MadGrenadierBlinding.Clear();
        Rudepeople.RudepeopleInProtect.Clear();
        Divinator.didVote.Clear();
        Main.NiceShieldsInProtect.Clear();
        Main.DeputyInProtect.Clear();
        Main.ProsecutorsInProtect.Clear();
        Main.InBoom.Clear();
        Main.ForNnurse.Clear();
        Main.DoubleKillerKillSeacond.Clear();
        Main.TimeMasterbacktrack.Clear();
        Main.TimeMasterInProtect.Clear();

        Main.GrenadiersInProtect.Clear();
        Concealer.OnReportDeadBody();
        //Concealer.OnReportDeadBody();
        Psychic.OnReportDeadBody();
        BountyHunter.OnReportDeadBody();
        SerialKiller.OnReportDeadBody();
        Sniper.OnReportDeadBody();
        Vampire.OnStartMeeting();
        Pelican.OnReportDeadBody();
        Counterfeiter.OnReportDeadBody();
        BallLightning.OnReportDeadBody();
        QuickShooter.OnReportDeadBody();
        Eraser.OnReportDeadBody();
        Hacker.OnReportDeadBody();
        Judge.OnReportDeadBody();
        Greedier.OnReportDeadBody();

        Mortician.OnReportDeadBody(player, target);
        Mediumshiper.OnReportDeadBody(target);
        Vulture.OnReportDeadBody(player, target);
        NiceTracker.OnReportDeadBody(player, target);
        DoubleKiller.OnReportDeadBody(player, target);
        Meditator.OnReportDeadBody(player);
        BloodSeekers.OnReportDeadBody(player, target);

        #region 革命家失败处理
        foreach (var x in Main.RevolutionistStart)
        {
            var tar = Utils.GetPlayerById(x.Key);
            if (tar == null) continue;
            tar.Data.IsDead = true;
            Main.PlayerStates[tar.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
            tar.RpcExileV2();
            Main.PlayerStates[tar.PlayerId].SetDead();
            Logger.Info($"{tar.GetRealName()} 因会议革命失败", "Revolutionist");
        }
        Main.RevolutionistTimer.Clear();
        Main.RevolutionistStart.Clear();
        Main.RevolutionistLastTime.Clear();
        #endregion

        Main.AllPlayerControls
            .Where(pc => Main.CheckShapeshift.ContainsKey(pc.PlayerId))
            .Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true));

        MeetingTimeManager.OnReportDeadBody();

        Utils.NotifyRoles(isForMeeting: true, NoCache: true);

        Utils.SyncAllSettings();

        if (Concealer.IsHidding && !(Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool()))
            Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true));

        if (Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
        { Utils.NotifyRoles(CamouflageisForMeeting: true, CamouflageIsActive: true); }
    }
    public static async void ChangeLocalNameAndRevert(string name, int time)
    {
        //async Taskじゃ警告出るから仕方ないよね。
        var revertName = PlayerControl.LocalPlayer.name;
        PlayerControl.LocalPlayer.RpcSetNameEx(name);
        await Task.Delay(time);
        PlayerControl.LocalPlayer.RpcSetNameEx(revertName);
    }
}
