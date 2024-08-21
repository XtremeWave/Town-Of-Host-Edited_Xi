using AmongUs.Data;
using HarmonyLib;
using System.Linq;
using TOHEXI.Roles.Crewmate;
using TOHEXI.Roles.Impostor;
using TOHEXI.Roles.Neutral;
using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;
using TOHEXI.Roles.Double;
using TOHEXI.Modules;

namespace TOHEXI;

class ExileControllerWrapUpPatch
{
    public static GameData.PlayerInfo AntiBlackout_LastExiled;
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    class BaseExileControllerPatch
    {
        public static void Postfix(ExileController __instance)
        {
            try
            {
                WrapUpPostfix(__instance.exiled);
            }
            finally
            {
                WrapUpFinalizer(__instance.exiled);
            }
        }
    }

    [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
    class AirshipExileControllerPatch
    {
        public static void Postfix(AirshipExileController __instance)
        {
            try
            {
                WrapUpPostfix(__instance.exiled);
            }
            finally
            {
                WrapUpFinalizer(__instance.exiled);
            }
        }
    }
    static void WrapUpPostfix(GameData.PlayerInfo exiled)
    {
        if (AntiBlackout.OverrideExiledPlayer)
        {
            exiled = AntiBlackout_LastExiled;
        }

        bool DecidedWinner = false;
        if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
        AntiBlackout.RestoreIsDead(doSend: false);
        if (!Collector.CollectorWin(false) && exiled != null) //判断集票者胜利
        {
            //霊界用暗転バグ対処
            if (!AntiBlackout.OverrideExiledPlayer && Main.ResetCamPlayerList.Contains(exiled.PlayerId))
                exiled.Object?.ResetPlayerCam(1f);

            exiled.IsDead = true;
            Main.PlayerStates[exiled.PlayerId].deathReason = PlayerState.DeathReason.Vote;
            var role = exiled.GetCustomRole();

            //判断冤罪师胜利
            if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == exiled.PlayerId))
            {
                if (!Options.InnocentCanWinByImp.GetBool() && role.IsImpostor())
                {
                    Logger.Info("冤罪的目标是内鬼，非常可惜啊", "Exeiled Winner Check");
                }
                else
                {
                    if (DecidedWinner) CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Innocent);
                    else CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Innocent);
                    Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == exiled.PlayerId)
                        .Do(x => CustomWinnerHolder.WinnerIds.Add(x.PlayerId));
                    DecidedWinner = true;
                }
            }

            //判断小丑胜利 (EAC封禁名单成为小丑达成胜利条件无法胜利)
            if (role == CustomRoles.Jester)
            {
                if (role == CustomRoles.Lovers)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                }
                else if (role == CustomRoles.CrushLovers)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CrushLovers);
                }
                else if (role == CustomRoles.CupidLovers)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CupidLovers);
                }
                else if (role == CustomRoles.Honmei)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CupidLovers);
                }
                if (DecidedWinner) CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Jester);
                else CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jester);
                CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                DecidedWinner = true;
            }
            
            //判断欺诈师被出内鬼胜利（被魅惑的欺诈师被出魅惑者胜利 || 恋人欺诈师被出恋人胜利）
            if (role == CustomRoles.Fraudster)
            {
                if (role == (CustomRoles.Charmed))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Succubus);
                }
                else if (role == CustomRoles.Lovers)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                }
                else if (role == CustomRoles.CrushLovers)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CrushLovers);
                }
                else if (role == CustomRoles.CupidLovers)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CupidLovers);
                }
                else if (role == CustomRoles.Honmei)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CupidLovers);
                }
                else
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
            }
            //判断豺狼被出
            if (role == CustomRoles.Jackal)
            {
                Main.isjackalDead = true;
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Sidekick))
                    {
                        pc.RpcSetCustomRole(CustomRoles.Jackal);
                        Jackal.Add(pc.PlayerId);
                        Jackal.Add(pc.PlayerId);
                        pc.ResetKillCooldown();
                        pc.SetKillCooldown();
                    }
                }
            }
            if (Main.ForYandere.Contains(exiled.PlayerId))
            {
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                   if (pc.Is(CustomRoles.Yandere))
                   {
                        pc.RpcMurderPlayerV3(pc);
                        
                   }
                   break;
                }
            }
            if (!Main.HangTheDevilKiller.Contains(exiled.PlayerId))
            {
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.Is(CustomRoles.HangTheDevil) && pc.IsAlive() && pc!=null) continue;
                    if (pc.Is(CustomRoles.HangTheDevil) && !pc.IsAlive() && pc != null)
                    {
                        foreach (var player in Main.AllAlivePlayerControls)
                        {
                            if (Main.ForHangTheDevil.Contains(player.PlayerId))
                            {
                                player.RpcMurderPlayerV3(player);
                               player.SetRealKiller(pc);
                                player.RPCPlayCustomSound("Ghost");
                            }
                        }
                    }
                }
            }
            //判断内鬼辈出
            if (exiled.GetCustomRole().IsImpostor())
            {
                int DefectorInt = 0;
                int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
                int ImIntDead = 0;
                int AlivePlayerRemain = 0;
                ImIntDead++;
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    AlivePlayerRemain++;
                }
                foreach (var player in Main.AllPlayerControls)
                {
                    if (!player.IsAlive() && player.GetCustomRole().IsImpostor() && !Main.KillImpostor.Contains(player.PlayerId) && !player.Is(CustomRoles.Defector) && player.PlayerId != exiled.PlayerId)
                    {
                        Main.KillImpostor.Add(player.PlayerId);
                        ImIntDead++;

                        foreach (var partnerPlayer in Main.AllPlayerControls)
                        {
                            if (ImIntDead != optImpNum) continue;
                            if (AlivePlayerRemain < Options.DefectorRemain.GetInt())
                            if (partnerPlayer.GetCustomRole().IsCrewmate() && partnerPlayer.CanUseKillButton() && DefectorInt == 0)
                            {
                                Logger.Info($"qwqwqwq", "Jackal");
                                DefectorInt++;
                                partnerPlayer.RpcSetCustomRole(CustomRoles.Defector);
                                partnerPlayer.ResetKillCooldown();
                                partnerPlayer.SetKillCooldown();
                                partnerPlayer.RpcGuardAndKill(partnerPlayer);
                            }
                        }
                    }
                }
            }
            
            //判断警长被出
            if (role == CustomRoles.Sheriff)
            {
                Main.isSheriffDead = true;
                if (Deputy.DeputyCanBeSheriff.GetBool())
                {
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.Deputy))
                        {
                            pc.RpcSetCustomRole(CustomRoles.Sheriff);
           

                            Sheriff.Add(pc.PlayerId);
                            Sheriff.Add(pc.PlayerId);

                            pc.ResetKillCooldown();
                            pc.SetKillCooldown();
                            pc.RpcGuardAndKill(pc);
                        }
                    }
                }
            }
            //判断处刑人胜利
            if (Executioner.CheckExileTarget(exiled, DecidedWinner)) DecidedWinner = true;

            //判断恐怖分子胜利
            if (role == CustomRoles.Terrorist) Utils.CheckTerroristWin(exiled);

            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) Main.PlayerStates[exiled.PlayerId].SetDead();
        }
        if (AmongUsClient.Instance.AmHost && Main.IsFixedCooldown)
            Main.RefixCooldownDelay = Options.DefaultKillCooldown - 3f;

        Witch.RemoveSpelledPlayer();
        PlagueDoctor.Immunitytimes = PlagueDoctor.Immunitytime.GetInt();
        PlagueDoctor.ImmunityGone = false;
        if (Options.ResetTargetAfterMeeting.GetBool())
        {
            Main.HunterTarget.Clear();
        }
        if (NiceSwapper.Vote.Count > 0 && NiceSwapper.VoteTwo.Count > 0)
        {
            foreach (var swapper in Main.AllAlivePlayerControls)
            {
                if (swapper.Is(CustomRoles.NiceSwapper))
                {
                    NiceSwapper.NiceSwappermax[swapper.PlayerId]--;
                    NiceSwapper.Vote.Clear();
                    NiceSwapper.VoteTwo.Clear();
                    Main.NiceSwapSend = false;
                }
            }
        }
        if (EvilSwapper.Vote.Count > 0 && EvilSwapper.VoteTwo.Count > 0)
        {
            foreach (var swapper in Main.AllAlivePlayerControls)
            {
                if (swapper.Is(CustomRoles.EvilSwapper))
                {
                    EvilSwapper.EvilSwappermax[swapper.PlayerId]--;
                    EvilSwapper.Vote.Clear();
                    EvilSwapper.VoteTwo.Clear();
                    Main.EvilSwapSend = false;
                }
            }
        }
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.Is(CustomRoles.EvilMini) && Mini.Age != 18)
            {
                Main.AllPlayerKillCooldown[pc.PlayerId] = Mini.MinorCD.GetFloat() + 2f;
                Main.EvilMiniKillcooldown[pc.PlayerId] = Mini.MinorCD.GetFloat() + 2f;
                Main.EvilMiniKillcooldownf = Mini.MinorCD.GetFloat();
                pc.MarkDirtySettings();
                pc.SetKillCooldown();
            }
            else if (pc.Is(CustomRoles.EvilMini) && Mini.Age == 18)
            {
                Main.AllPlayerKillCooldown[pc.PlayerId] = Mini.MajorCD.GetFloat();
                pc.MarkDirtySettings();
                pc.SetKillCooldown();
            }
        }
        
        Main.DyingTurns += 1;
 
        foreach (PlayerControl target in Main.WrongedList)
        {
            if (Main.FirstDied == byte.MaxValue && target.GetCustomRole().IsCrewmate() && !target.CanUseKillButton() && Options.CanWronged.GetBool())
            {
                target.RpcSetCustomRole(CustomRoles.Wronged);
                var taskState = target.GetPlayerTaskState();
                taskState.CompletedTasksCount++;
                taskState.CompletedTasksCount++;
                taskState.CompletedTasksCount++;
                taskState.CompletedTasksCount++;
                taskState.CompletedTasksCount++;
                taskState.CompletedTasksCount++;
                taskState.CompletedTasksCount++;
                taskState.CompletedTasksCount++;
                taskState.CompletedTasksCount++;
                taskState.CompletedTasksCount++;
                GameData.Instance.RpcSetTasks(target.PlayerId, new byte[0]); //タスクを再配布
                target.SyncSettings();
                Utils.NotifyRoles(target);
            }
        }
        foreach (var player in Main.AllPlayerControls)
        {
            if (Main.SignalLocation.ContainsKey(player.PlayerId))
            {
                var position = Main.SignalLocation[player.PlayerId];
                Utils.TP(player.NetTransform, position);
            }
        }
        foreach (var pc in Main.AllPlayerControls)
        {
            pc.ResetKillCooldown();
            if (Options.MayorHasPortableButton.GetBool() && pc.Is(CustomRoles.Mayor))
                pc.RpcResetAbilityCooldown();
            if (pc.Is(CustomRoles.Warlock))
            {
                Main.CursedPlayers[pc.PlayerId] = null;
                Main.isCurseAndKill[pc.PlayerId] = false;
                RPC.RpcSyncCurseAndKill();
            }
            if (pc.GetCustomRole() is
                CustomRoles.Paranoia or
                CustomRoles.Veteran or
                CustomRoles.Greedier or
                CustomRoles.DovesOfNeace or
                CustomRoles.QuickShooter
                ) pc.RpcResetAbilityCooldown();
        }
        if (Options.RandomSpawn.GetBool() || Options.CurrentGameMode == CustomGameMode.SoloKombat || Options.CurrentGameMode == CustomGameMode.HotPotato || Options.CurrentGameMode == CustomGameMode.TheLivingDaylights)
        {
            RandomSpawn.SpawnMap map;
            switch (Main.NormalOptions.MapId)
            {
                case 0:
                    map = new RandomSpawn.SkeldSpawnMap();
                    Main.AllPlayerControls.Do(map.RandomTeleport);
                    break;
                case 1:
                    map = new RandomSpawn.MiraHQSpawnMap();
                    Main.AllPlayerControls.Do(map.RandomTeleport);
                    break;
                case 2:
                    map = new RandomSpawn.PolusSpawnMap();
                    Main.AllPlayerControls.Do(map.RandomTeleport);
                    break;
            }
        }
        FallFromLadder.Reset();
        Utils.CountAlivePlayers(true);
        Utils.AfterMeetingTasks();
        Utils.SyncAllSettings();
        Utils.NotifyRoles();
    }

    static void WrapUpFinalizer(GameData.PlayerInfo exiled)
    {
        //WrapUpPostfixで例外が発生しても、この部分だけは確実に実行されます。
        if (AmongUsClient.Instance.AmHost)
        {
            new LateTask(() =>
            {
                exiled = AntiBlackout_LastExiled;
                AntiBlackout.SendGameData();
                if (AntiBlackout.OverrideExiledPlayer && // 追放対象が上書きされる状態 (上書きされない状態なら実行不要)
                    exiled != null && //exiledがnullでない
                    exiled.Object != null) //exiled.Objectがnullでない
                {
                    exiled.Object.RpcExileV2();
                }
            }, 0.5f, "Restore IsDead Task");
            new LateTask(() =>
            {
                Main.AfterMeetingDeathPlayers.Do(x =>
                {
                    var player = Utils.GetPlayerById(x.Key);
                    Logger.Info($"{player.GetNameWithRole()}を{x.Value}で死亡させました", "AfterMeetingDeath");
                    Main.PlayerStates[x.Key].deathReason = x.Value;
                    Main.PlayerStates[x.Key].SetDead();
                    player?.RpcExileV2();
                    if (x.Value == PlayerState.DeathReason.Suicide)
                        player?.SetRealKiller(player, true);
                    if (Main.ResetCamPlayerList.Contains(x.Key))
                        player?.ResetPlayerCam(1f);
                    Utils.AfterPlayerDeathTasks(player);
                });
                Main.AfterMeetingDeathPlayers.Clear();
            }, 0.5f, "AfterMeetingDeathPlayers Task");
        }

        GameStates.AlreadyDied |= !Utils.IsAllAlive;
        RemoveDisableDevicesPatch.UpdateDisableDevices();
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
        Logger.Info("タスクフェイズ開始", "Phase");
    }
}

[HarmonyPatch(typeof(PbExileController), nameof(PbExileController.PlayerSpin))]
class PolusExileHatFixPatch
{
    public static void Prefix(PbExileController __instance)
    {
        __instance.Player.cosmetics.hat.transform.localPosition = new(-0.2f, 0.6f, 1.1f);
    }
}