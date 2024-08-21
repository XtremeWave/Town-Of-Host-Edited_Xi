using AmongUs.GameOptions;
using HarmonyLib;
using MS.Internal.Xml.XPath;
using Sentry.Internal.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHEXI.Roles.Crewmate;
using TOHEXI.Roles.Impostor;
using TOHEXI.Roles.Neutral;
using UnityEngine;
using static TOHEXI.Translator;
using Hazel;
using InnerNet;
using System.Threading.Tasks;
using TOHEXI.Modules;
using TOHEXI.Roles.AddOns.Crewmate;
using UnityEngine.Profiling;
using System.Runtime.Intrinsics.X86;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UI;
using UnityEngine.Networking.Types;
using TOHEXI.Roles.Double;
using Microsoft.Extensions.Logging;
using Sentry;
using UnityEngine.SocialPlatforms;
using static UnityEngine.ParticleSystem.PlaybackState;
using Cpp2IL.Core.Extensions;

namespace TOHEXI;

[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
class EnterVentPatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
    {

        
    
        Witch.OnEnterVent(pc);
        if (pc.Is(CustomRoles.Mayor))
        {
            if (Main.MayorUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt())
            {
                pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                pc?.ReportDeadBody(null);
            }
        }

        if (pc.Is(CustomRoles.Paranoia))
        {
            if (Main.ParaUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < Options.ParanoiaNumOfUseButton.GetInt())
            {
                Main.ParaUsedButtonCount[pc.PlayerId] += 1;
                if (AmongUsClient.Instance.AmHost)
                {
                    new LateTask(() =>
                    {
                        Utils.SendMessage(GetString("SkillUsedLeft") + (Options.ParanoiaNumOfUseButton.GetInt() - Main.ParaUsedButtonCount[pc.PlayerId]).ToString(), pc.PlayerId);
                    }, 4.0f, "Skill Remain Message");
                }
                pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                pc?.NoCheckStartMeeting(pc?.Data);
            }
        }

        if (pc.Is(CustomRoles.Mario))
        {
            Main.MarioVentCount.TryAdd(pc.PlayerId, 0);
            Main.MarioVentCount[pc.PlayerId]++;
            Utils.NotifyRoles(pc);
            if (pc.AmOwner)
            {
                if (Main.MarioVentCount[pc.PlayerId] % 5 == 0) CustomSoundsManager.Play("MarioCoin");
                else CustomSoundsManager.Play("MarioJump");
            }
            if (AmongUsClient.Instance.AmHost && Main.MarioVentCount[pc.PlayerId] >= Options.MarioVentNumWin.GetInt())
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Mario); //马里奥这个多动症赢了
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
            }
        }

        if (!AmongUsClient.Instance.AmHost) return;

        Logger.Info($"{pc.GetNameWithRole()} EnterVent: {__instance.Id}", "EnterVent");

        Main.LastEnteredVent.Remove(pc.PlayerId);
        Main.LastEnteredVent.Add(pc.PlayerId, __instance);
        Main.LastEnteredVentLocation.Remove(pc.PlayerId);
        Main.LastEnteredVentLocation.Add(pc.PlayerId, pc.GetTruePosition());

        Swooper.OnEnterVent(pc, __instance);
        Buried.OnEnterVent(pc, __instance);
        Henry.OnEnterVent(pc);
        Chameleon.OnEnterVent(pc, __instance);
        Rudepeople.OnEnterVent(pc);

        if (pc.Is(CustomRoles.Veteran))
        {
            Main.VeteranInProtect.Remove(pc.PlayerId);
            Main.VeteranInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
            Main.VeteranNumOfUsed[pc.PlayerId]--;
            if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("Gunload");
            pc.Notify(GetString("VeteranOnGuard"), Options.VeteranSkillDuration.GetFloat());
        }
        if (pc.Is(CustomRoles.Grenadier))
        {
            if (pc.Is(CustomRoles.Madmate))
            {
                Main.MadGrenadierBlinding.Remove(pc.PlayerId);
                Main.MadGrenadierBlinding.Add(pc.PlayerId, Utils.GetTimeStamp());
                Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => !x.GetCustomRole().IsImpostorTeam() && !x.Is(CustomRoles.Madmate)).Do(x => x.RPCPlayCustomSound("FlashBang"));
            }
            else
            {
                Main.GrenadierBlinding.Remove(pc.PlayerId);
                Main.GrenadierBlinding.Add(pc.PlayerId, Utils.GetTimeStamp());
                Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => x.GetCustomRole().IsImpostor() || (x.GetCustomRole().IsNeutral() && Options.GrenadierCanAffectNeutral.GetBool())).Do(x => x.RPCPlayCustomSound("FlashBang"));
            }
            if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("FlashBang");
            pc.Notify(GetString("GrenadierSkillInUse"), Options.GrenadierSkillDuration.GetFloat());
            Utils.MarkEveryoneDirtySettings();
        }
        if (pc.Is(CustomRoles.DovesOfNeace))
        {
            Main.DovesOfNeaceNumOfUsed[pc.PlayerId]--;
            pc.RpcGuardAndKill(pc);
            Main.AllAlivePlayerControls.Where(x =>
            pc.Is(CustomRoles.Madmate) ?
            (x.CanUseKillButton() && x.GetCustomRole().IsCrewmate()) :
            (x.CanUseKillButton())
            ).Do(x =>
            {
                x.RPCPlayCustomSound("Dove");
                x.ResetKillCooldown();
                x.SetKillCooldownV2();
                x.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DovesOfNeace), GetString("DovesOfNeaceSkillNotify")));
            });
            pc.RPCPlayCustomSound("Dove");
            pc.Notify(string.Format(GetString("DovesOfNeaceOnGuard"), Main.DovesOfNeaceNumOfUsed[pc.PlayerId]));
        }
        //扰乱技能启动！
        if (pc.Is(CustomRoles.sabcat))
        {
            Main.sabcatInProtect.Remove(pc.PlayerId);
            Main.sabcatInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
            //Main.sabcatNumOfUsed[pc.PlayerId]--;
            if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("RNM");
            pc.Notify(GetString("sabcatOnGuard"), Options.sabcatCooldown.GetFloat());
        }
        if (pc.Is(CustomRoles.UnluckyEggs))
        {
            var Ue = IRandom.Instance;
            if (Ue.Next(0, 100) < Options.UnluckyEggsKIllUnluckyEggs.GetInt())
            {
                pc.RpcMurderPlayerV3(pc);
            }
        }

        if (pc.Is(CustomRoles.TimeStops))
        {
            CustomSoundsManager.RPCPlayCustomSoundAll("THEWORLD");
            Main.TimeStopsInProtect.Remove(pc.PlayerId);
            Main.TimeStopsInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
            if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("THEWORLD");
            pc.Notify(GetString("TimeStopsOnGuard"), Options.TimeStopsSkillDuration.GetFloat());
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (pc == player) continue;
                if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                NameNotifyManager.Notify(player, Utils.ColorString(Utils.GetRoleColor(CustomRoles.TimeStops), GetString("ForTimeStops")));
                var tmpSpeed1 = Main.AllPlayerSpeed[player.PlayerId];
                Main.TimeStopsstop.Add(player.PlayerId);
                Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
                ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
                player.MarkDirtySettings();
                new LateTask(() =>
                {
                    Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - Main.MinSpeed + tmpSpeed1;
                    ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
                    player.MarkDirtySettings();
                    Main.TimeStopsstop.Remove(player.PlayerId);
                    RPC.PlaySoundRPC(player.PlayerId, Sounds.TaskComplete);
                }, Options.TimeStopsSkillDuration.GetFloat(), "Time Stop");
            }
        }
        if (Main.ForMagnetMan.Contains(pc.PlayerId)) pc?.MyPhysics?.RpcBootFromVent(__instance.Id);

        if (pc.Is(CustomRoles.TimeMaster))
        {
            Main.TimeMasterInProtect.Remove(pc.PlayerId);
            Main.TimeMasterInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
            if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
            pc.Notify(GetString("TimeMasterOnGuard"), Options.TimeMasterSkillDuration.GetFloat());
            foreach (var player in Main.AllPlayerControls)
            {
                if (Main.TimeMasterbacktrack.ContainsKey(player.PlayerId))
                {
                    var position = Main.TimeMasterbacktrack[player.PlayerId];
                    Utils.TP(player.NetTransform, position);
                    Main.TimeMasterbacktrack.Remove(player.PlayerId);
                }
                else
                {
                    Main.TimeMasterbacktrack.Add(player.PlayerId, player.GetTruePosition());
                }
            }
        }
        if (pc.Is(CustomRoles.Spiritualists))
        {
            var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != pc.PlayerId).ToList();
            var Sl = pcList[IRandom.Instance.Next(0, pcList.Count)];
            Main.ForSpiritualists.Add(Sl.PlayerId);
            Main.Spiritualistsbacktrack.Add(pc.PlayerId, pc.GetTruePosition());
            new LateTask(() =>
            {
                var position = Main.Spiritualistsbacktrack[pc.PlayerId];
                Main.Spiritualistsbacktrack.Remove(pc.PlayerId);
                Utils.TP(pc.NetTransform, position);
                Main.ForSpiritualists.Remove(Sl.PlayerId);
                pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                Utils.NotifyRoles();
            }, Options.SpiritualistsVentMaxCooldown.GetInt(), ("NotRight!"));
        }
        if (pc.Is(CustomRoles.DemolitionManiac))
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (Main.DemolitionManiacKill.Contains(player.PlayerId))
                {
                    Main.DemolitionManiacKill.Remove(player.PlayerId);
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    player.SetRealKiller(pc);
                    player.RpcMurderPlayerV3(player);
                }
            }
        }
        if (pc.Is(CustomRoles.GlennQuagmire))
        {
            List<PlayerControl> list = Main.AllAlivePlayerControls.Where(x => x.PlayerId != pc.PlayerId).ToList();
            if (list.Count < 1)
            {
                Logger.Info($"Q哥没有目标", "GlennQuagmire");
            }
            else
            {
                list = list.OrderBy(x => Vector2.Distance(pc.GetTruePosition(), x.GetTruePosition())).ToList();
                var target = list[0];
                if (target.GetCustomRole().IsImpostor())
                {
                    pc.RPCPlayCustomSound("giggity");
                    target.SetRealKiller(pc);
                    target.RpcCheckAndMurder(target);
                    pc.RpcGuardAndKill();
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Creation;
                }
                else
                {
                    if (Main.ForSourcePlague.Contains(pc.PlayerId))
                    {
                        new LateTask(() =>
                        {
                            Utils.TP(pc.NetTransform, target.GetTruePosition());
                            Main.ForSourcePlague.Add(target.PlayerId);
                            Utils.NotifyRoles();
                        }, 2.5f, ("NotRight!"));
                    }
                    new LateTask(() =>
                    {
                        Utils.TP(pc.NetTransform, target.GetTruePosition());
                        Utils.NotifyRoles();
                    }, 2.5f, ("NotRight!"));

                }
            }
        }
        if (pc.Is(CustomRoles.SoulSeeker))
        {
            if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
            foreach (var player in Main.AllPlayerControls)
            {
                if (Pelican.IsEaten(player.PlayerId) && Options.SoulSeekerCanSeeEat.GetBool())
                {
                    Main.SoulSeekerCanEat[pc.PlayerId]++;
                }
                if (!player.IsAlive())
                {
                    Main.SoulSeekerDead[pc.PlayerId]++;
                    if (player.CanUseKillButton())
                    {
                        Main.SoulSeekerCanKill[pc.PlayerId]++;
                    }
                    else
                    {
                        Main.SoulSeekerNotCanKill[pc.PlayerId]++;
                    }
                }
            }
            pc.Notify(string.Format(GetString("SoulSeekerOffGuard"), Main.SoulSeekerDead[pc.PlayerId], Main.SoulSeekerNotCanKill[pc.PlayerId], Main.SoulSeekerCanKill[pc.PlayerId], Main.SoulSeekerCanEat[pc.PlayerId]));
            if (Main.SoulSeekerCanEat[pc.PlayerId] > 0)
            {
                Main.SoulSeekerCanEat[pc.PlayerId] = 0;
            }
            if (Main.SoulSeekerCanKill[pc.PlayerId] > 0)
            {
                Main.SoulSeekerCanKill[pc.PlayerId] = 0;
            }
            if (Main.SoulSeekerNotCanKill[pc.PlayerId] > 0)
            {
                Main.SoulSeekerNotCanKill[pc.PlayerId] = 0;
            }
            if (Main.SoulSeekerDead[pc.PlayerId] > 0)
            {
                Main.SoulSeekerDead[pc.PlayerId] = 0;
            }
        }
        if (pc.Is(CustomRoles.King) && Main.KingCanpc.Contains(pc.PlayerId))
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.King);
            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
            foreach (var player in Main.AllPlayerControls)
            {
                if (Main.ForKing.Contains(player.PlayerId))
                {
                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                }
            }
        }
        if (Main.ForTasksDestinyChooser.Contains(pc.PlayerId))
        {
            pc.RpcMurderPlayerV3(pc);
        }
        if (pc.Is(CustomRoles.Plumber))
        {
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (player.PlayerId == pc.PlayerId || !player.inVent) continue;
                player?.MyPhysics?.RpcBootFromVent(__instance.Id);
            }
        }
       // if (pc.Is(CustomRoles.Locksmith))
      //  {
       //         RepairSystemPatch.OpenDoors((ShipStatus)pc.PlayerId);
      //  }
    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            Logger.Info($"{__instance.myPlayer.GetNameWithRole()} CoEnterVent: {id}", "CoEnterVent");

            if (AmongUsClient.Instance.IsGameStarted &&
                __instance.myPlayer.IsDouseDone())
            {
                CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc != __instance.myPlayer)
                    {
                        //生存者は焼殺
                        pc.SetRealKiller(__instance.myPlayer);
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Torched;
                        pc.RpcMurderPlayerV3(pc);
                        Main.PlayerStates[pc.PlayerId].SetDead();
                    }
                }
                foreach (var pc in Main.AllPlayerControls) pc.KillFlash();
                CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Arsonist); //焼殺で勝利した人も勝利させる
                CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                return true;
            }

            if (AmongUsClient.Instance.IsGameStarted && __instance.myPlayer.IsDrawDone())//完成拉拢任务的玩家跳管后
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Revolutionist);//革命者胜利
                Utils.GetDrawPlayerCount(__instance.myPlayer.PlayerId, out var x);
                CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                foreach (var apc in x) CustomWinnerHolder.WinnerIds.Add(apc.PlayerId);//胜利玩家
                return true;
            }
            //处理弹出管道的阻塞
            if ((__instance.myPlayer.Data.Role.Role != RoleTypes.Engineer && //不是工程师
            !__instance.myPlayer.CanUseImpostorVentButton()) || //不能使用内鬼的跳管按钮
            (__instance.myPlayer.Is(CustomRoles.sabcat)) ||
            (__instance.myPlayer.Is(CustomRoles.Mayor) && Main.MayorUsedButtonCount.TryGetValue(__instance.myPlayer.PlayerId, out var count) && count >= Options.MayorNumOfUseButton.GetInt()) ||
            (__instance.myPlayer.Is(CustomRoles.Paranoia) && Main.ParaUsedButtonCount.TryGetValue(__instance.myPlayer.PlayerId, out var count2) && count2 >= Options.ParanoiaNumOfUseButton.GetInt()) ||
            (__instance.myPlayer.Is(CustomRoles.Veteran) && Main.VeteranNumOfUsed.TryGetValue(__instance.myPlayer.PlayerId, out var count3) && count3 < 1) ||
            (__instance.myPlayer.Is(CustomRoles.DovesOfNeace) && Main.DovesOfNeaceNumOfUsed.TryGetValue(__instance.myPlayer.PlayerId, out var count4) && count4 < 1)
            )
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                writer.WritePacked(127);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                new LateTask(() =>
                {
                    int clientId = __instance.myPlayer.GetClientId();
                    MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                    writer2.Write(id);
                    AmongUsClient.Instance.FinishRpcImmediately(writer2);
                }, 0.5f, "Fix DesyncImpostor Stuck");

                if (__instance.myPlayer.Is(CustomRoles.DovesOfNeace)) __instance.myPlayer.Notify(GetString("DovesOfNeaceMaxUsage"));
                if (__instance.myPlayer.Is(CustomRoles.Veteran)) __instance.myPlayer.Notify(GetString("VeteranMaxUsage"));

                return false;
            }

            if (__instance.myPlayer.Is(CustomRoles.Swooper))
                Swooper.OnCoEnterVent(__instance, id);
            if (__instance.myPlayer.Is(CustomRoles.Chameleon))
                Chameleon.OnCoEnterVent(__instance, id);

            if (Buried.landmineDict.TryGetValue(id, out byte value) && value == 1 && __instance.myPlayer.CanUseImpostorVentButton() || Buried.landmineDict.TryGetValue(id, out byte aalue) && aalue == 1 && __instance.myPlayer.Data.Role.Role == RoleTypes.Engineer)
            {
                new LateTask(() =>
                {
                    __instance.myPlayer.RpcMurderPlayerV3(__instance.myPlayer);
                    Buried.landmineDict[id] = 0;
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (player.Is(CustomRoles.Buried))
                        {
                            __instance.myPlayer.SetRealKiller(player);
                            break;
                        }
                    }
                    Utils.NotifyRoles();
                }, 1f, ("被埋雷炸死"));
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetName))]
    class SetNamePatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] string name)
        {
        }
    }
    [HarmonyPatch(typeof(GameData), nameof(GameData.CompleteTask))]
    class GameDataCompleteTaskPatch
    {
        public static void Postfix(PlayerControl pc)
        {
            Logger.Info($"TaskComplete:{pc.GetNameWithRole()}", "CompleteTask");
            Main.PlayerStates[pc.PlayerId].UpdateTask(pc);
            Utils.NotifyRoles();
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    class PlayerControlCompleteTaskPatch
    {
        public static bool Prefix(PlayerControl __instance)
        {
            var player = __instance;

            if (Workhorse.OnCompleteTask(player)) //タスク勝利をキャンセル
                return false;

            //来自资本主义的任务
            if (Main.CapitalismAddTask.ContainsKey(player.PlayerId))
            {
                var taskState = player.GetPlayerTaskState();
                taskState.AllTasksCount += Main.CapitalismAddTask[player.PlayerId];
                Main.CapitalismAddTask.Remove(player.PlayerId);
                taskState.CompletedTasksCount++;
                GameData.Instance.RpcSetTasks(player.PlayerId, new byte[0]); //タスクを再配布
                player.SyncSettings();
                Utils.NotifyRoles(player);
                return false;
            }

            return true;
        }
        public static void Postfix(PlayerControl __instance)
        {
            var pc = __instance;
            Snitch.OnCompleteTask(pc);

            var isTaskFinish = pc.GetPlayerTaskState().IsTaskFinished;
            if (isTaskFinish && pc.Is(CustomRoles.Snitch) && pc.Is(CustomRoles.Madmate))
            {
                foreach (var impostor in Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)))
                    NameColorManager.Add(impostor.PlayerId, pc.PlayerId, "#ff1919");
                Utils.NotifyRoles(SpecifySeer: pc);
            }
            if ((isTaskFinish &&
                pc.GetCustomRole() is CustomRoles.Doctor or CustomRoles.Sunnyboy) ||
                pc.GetCustomRole() is CustomRoles.SpeedBooster)
            {
                //ライターもしくはスピードブースターもしくはドクターがいる試合のみタスク終了時にCustomSyncAllSettingsを実行する
                Utils.MarkEveryoneDirtySettings();
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
    class PlayerControlProtectPlayerPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}", "ProtectPlayer");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RemoveProtection))]
    class PlayerControlRemoveProtectionPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            Logger.Info($"{__instance.GetNameWithRole()}", "RemoveProtection");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    class PlayerControlSetRolePatch
    {
        public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType)
        {
            var target = __instance;
            var targetName = __instance.GetNameWithRole();
            Logger.Info($"{targetName} =>{roleType}", "PlayerControl.RpcSetRole");
            if (!ShipStatus.Instance.enabled) return true;
            if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
            {
                var targetIsKiller = target.Is(CustomRoleTypes.Impostor) || Main.ResetCamPlayerList.Contains(target.PlayerId);
                var ghostRoles = new Dictionary<PlayerControl, RoleTypes>();
                foreach (var seer in Main.AllPlayerControls)
                {
                    var self = seer.PlayerId == target.PlayerId;
                    var seerIsKiller = seer.Is(CustomRoleTypes.Impostor) || Main.ResetCamPlayerList.Contains(seer.PlayerId);
                    if (target.Is(CustomRoles.Guardian))
                    {
                        ghostRoles[seer] = RoleTypes.GuardianAngel;
                    }
                    if ((self && targetIsKiller) || (!seerIsKiller && target.Is(CustomRoleTypes.Impostor)))
                    {
                        ghostRoles[seer] = RoleTypes.ImpostorGhost;
                    }
                    else
                    {
                        ghostRoles[seer] = RoleTypes.CrewmateGhost;
                    }
                }
                if (ghostRoles.All(kvp => kvp.Value == RoleTypes.CrewmateGhost))
                {
                    roleType = RoleTypes.CrewmateGhost;
                }
                else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.GuardianAngel))
                {
                    roleType = RoleTypes.GuardianAngel;
                }
                else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.ImpostorGhost))
                {
                    roleType = RoleTypes.ImpostorGhost;
                }
                else
                {
                    foreach ((var seer, var role) in ghostRoles)
                    {
                        Logger.Info($"Desync {targetName} =>{role} for{seer.GetNameWithRole()}", "PlayerControl.RpcSetRole");
                        target.RpcSetRoleDesync(role, seer.GetClientId());
                    }
                    return false;
                }
            }
            return true;
        }
    }
}
