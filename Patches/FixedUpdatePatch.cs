using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheOtherRoles_Host.Roles.Crewmate;
using TheOtherRoles_Host.Roles.Double;
using TheOtherRoles_Host.Roles.Impostor;
using TheOtherRoles_Host.Roles.Neutral;
using UnityEngine;
using static TheOtherRoles_Host.Translator;

namespace TheOtherRoles_Host;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
class FixedUpdatePatch
{
    private static long LastFixedUpdate = new();
    private static StringBuilder Mark = new(20);
    private static StringBuilder Suffix = new(120);
    private static int LevelKickBufferTime = 10;
    private static Dictionary<byte, int> BufferTime = new();
    public static void Postfix(PlayerControl __instance)
    {
        var player = __instance;

        if (!GameStates.IsModHost) return;

        bool lowLoad = false;
        if (Options.LowLoadMode.GetBool())
        {
            BufferTime.TryAdd(player.PlayerId, 10);
            BufferTime[player.PlayerId]--;
            if (BufferTime[player.PlayerId] > 0) lowLoad = true;
            else BufferTime[player.PlayerId] = 10;
        }

        Sniper.OnFixedUpdate(player);
        Zoom.OnFixedUpdate();
        if (!lowLoad)
        {
            NameNotifyManager.OnFixedUpdate(player);
            TargetArrow.OnFixedUpdate(player);
            LocateArrow.OnFixedUpdate(player);
        }


        if (AmongUsClient.Instance.AmHost)
        {//実行クライアントがホストの場合のみ実行
            if (GameStates.IsLobby && ((ModUpdater.hasUpdate && ModUpdater.forceUpdate) || ModUpdater.isBroken || !Main.AllowPublicRoom) && AmongUsClient.Instance.IsGamePublic)
                AmongUsClient.Instance.ChangeGamePublic(false);

            if (GameStates.IsInTask && ReportDeadBodyPatch.CanReport[__instance.PlayerId] && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
            {
                var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
                ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
                Logger.Info($"{__instance.GetNameWithRole()}:通報可能になったため通報処理を行います", "ReportDeadbody");
                __instance.ReportDeadBody(info);
            }

            //踢出低等级的人
            if (!lowLoad && GameStates.IsLobby && !player.AmOwner && Options.KickLowLevelPlayer.GetInt() != 0 && (
                (player.Data.PlayerLevel != 0 && player.Data.PlayerLevel < Options.KickLowLevelPlayer.GetInt()) ||
                player.Data.FriendCode == ""
                ))
            {
                LevelKickBufferTime--;
                if (LevelKickBufferTime <= 0)
                {
                    LevelKickBufferTime = 20;
                    AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                    string msg = string.Format(GetString("KickBecauseLowLevel"), player.GetRealName().RemoveHtmlTags());
                    Logger.SendInGame(msg);
                    Logger.Info(msg, "LowLevel Kick");
                }
            }

            DoubleTrigger.OnFixedUpdate(player);
            Vampire.OnFixedUpdate(player);
            BountyHunter.FixedUpdate(player);
            SerialKiller.FixedUpdate(player);
            RewardOfficer.FixedUpdate(player);
            Rudepeople.FixedUpdate(player);

            #region 女巫处理
            if (GameStates.IsInTask && Main.WarlockTimer.ContainsKey(player.PlayerId))//処理を1秒遅らせる
            {
                if (player.IsAlive())
                {
                    if (Main.WarlockTimer[player.PlayerId] >= 1f)
                    {
                        player.RpcResetAbilityCooldown();
                        Main.isCursed = false;//変身クールを１秒に変更
                        player.SyncSettings();
                        Main.WarlockTimer.Remove(player.PlayerId);
                    }
                    else Main.WarlockTimer[player.PlayerId] = Main.WarlockTimer[player.PlayerId] + Time.fixedDeltaTime;//時間をカウント
                }
                else
                {
                    Main.WarlockTimer.Remove(player.PlayerId);
                }
            }
            //ターゲットのリセット
            #endregion

            #region 纵火犯浇油处理
            if (GameStates.IsInTask && Main.ArsonistTimer.ContainsKey(player.PlayerId))//アーソニストが誰かを塗っているとき
            {
                if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId))
                {
                    Main.ArsonistTimer.Remove(player.PlayerId);
                    Utils.NotifyRoles(__instance);
                    RPC.ResetCurrentDousingTarget(player.PlayerId);
                }
                else
                {
                    var ar_target = Main.ArsonistTimer[player.PlayerId].Item1;//塗られる人
                    var ar_time = Main.ArsonistTimer[player.PlayerId].Item2;//塗った時間
                    if (!ar_target.IsAlive())
                    {
                        Main.ArsonistTimer.Remove(player.PlayerId);
                    }
                    else if (ar_time >= Options.ArsonistDouseTime.GetFloat())//時間以上一緒にいて塗れた時
                    {
                        player.SetKillCooldown();
                        Main.ArsonistTimer.Remove(player.PlayerId);//塗が完了したのでDictionaryから削除
                        Main.isDoused[(player.PlayerId, ar_target.PlayerId)] = true;//塗り完了
                        player.RpcSetDousedPlayer(ar_target, true);
                        Utils.NotifyRoles(player);//名前変更
                        RPC.ResetCurrentDousingTarget(player.PlayerId);
                    }
                    else
                    {

                        float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(CustomRoles.Reach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                        float dis = Vector2.Distance(player.transform.position, ar_target.transform.position);//距離を出す
                        if (dis <= range)//一定の距離にターゲットがいるならば時間をカウント
                        {
                            Main.ArsonistTimer[player.PlayerId] = (ar_target, ar_time + Time.fixedDeltaTime);
                        }
                        else//それ以外は削除
                        {
                            Main.ArsonistTimer.Remove(player.PlayerId);
                            Utils.NotifyRoles(player);
                            RPC.ResetCurrentDousingTarget(player.PlayerId);

                            Logger.Info($"Canceled: {player.GetNameWithRole()}", "Arsonist");
                        }
                    }
                }
            }
            #endregion

            #region 革命家拉人处理
            if (GameStates.IsInTask && Main.RevolutionistTimer.ContainsKey(player.PlayerId))//当革命家拉拢一个玩家时
            {
                if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId))
                {
                    Main.RevolutionistTimer.Remove(player.PlayerId);
                    Utils.NotifyRoles(player);
                    RPC.ResetCurrentDrawTarget(player.PlayerId);
                }
                else
                {
                    var rv_target = Main.RevolutionistTimer[player.PlayerId].Item1;//拉拢的人
                    var rv_time = Main.RevolutionistTimer[player.PlayerId].Item2;//拉拢时间
                    if (!rv_target.IsAlive())
                    {
                        Main.RevolutionistTimer.Remove(player.PlayerId);
                    }
                    else if (rv_time >= Options.RevolutionistDrawTime.GetFloat())//在一起时间超过多久
                    {
                        player.SetKillCooldown();
                        Main.RevolutionistTimer.Remove(player.PlayerId);//拉拢完成从字典中删除
                        Main.isDraw[(player.PlayerId, rv_target.PlayerId)] = true;//完成拉拢
                        player.RpcSetDrawPlayer(rv_target, true);
                        Utils.NotifyRoles(player);
                        RPC.ResetCurrentDrawTarget(player.PlayerId);
                        if (IRandom.Instance.Next(1, 100) <= Options.RevolutionistKillProbability.GetInt())
                        {
                            rv_target.SetRealKiller(player);
                            Main.PlayerStates[rv_target.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                            player.RpcMurderPlayerV3(rv_target);
                            Main.PlayerStates[rv_target.PlayerId].SetDead();
                            Logger.Info($"Revolutionist: {player.GetNameWithRole()} killed {rv_target.GetNameWithRole()}", "Revolutionist");
                        }
                    }
                    else
                    {
                        float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(CustomRoles.Reach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                        float dis = Vector2.Distance(player.transform.position, rv_target.transform.position);//超出距离
                        if (dis <= range)//在一定距离内则计算时间
                        {
                            Main.RevolutionistTimer[player.PlayerId] = (rv_target, rv_time + Time.fixedDeltaTime);
                        }
                        else//否则删除
                        {
                            Main.RevolutionistTimer.Remove(player.PlayerId);
                            Utils.NotifyRoles(__instance);
                            RPC.ResetCurrentDrawTarget(player.PlayerId);

                            Logger.Info($"Canceled: {__instance.GetNameWithRole()}", "Revolutionist");
                        }
                    }
                }
            }
            if (GameStates.IsInTask && player.IsDrawDone() && player.IsAlive())
            {
                if (Main.RevolutionistStart.ContainsKey(player.PlayerId)) //如果存在字典
                {
                    if (Main.RevolutionistLastTime.ContainsKey(player.PlayerId))
                    {
                        long nowtime = Utils.GetTimeStamp();
                        if (Main.RevolutionistLastTime[player.PlayerId] != nowtime) Main.RevolutionistLastTime[player.PlayerId] = nowtime;
                        int time = (int)(Main.RevolutionistLastTime[player.PlayerId] - Main.RevolutionistStart[player.PlayerId]);
                        int countdown = Options.RevolutionistVentCountDown.GetInt() - time;
                        Main.RevolutionistCountdown.Clear();
                        if (countdown <= 0)//倒计时结束
                        {
                            Utils.GetDrawPlayerCount(player.PlayerId, out var y);
                            foreach (var pc in y.Where(x => x != null && x.IsAlive()))
                            {
                                pc.Data.IsDead = true;
                                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                                pc.RpcMurderPlayerV3(pc);
                                Main.PlayerStates[pc.PlayerId].SetDead();
                                Utils.NotifyRoles(pc);
                            }
                            player.Data.IsDead = true;
                            Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                            player.RpcMurderPlayerV3(player);
                            Main.PlayerStates[player.PlayerId].SetDead();
                        }
                        else
                        {
                            Main.RevolutionistCountdown.Add(player.PlayerId, countdown);
                        }
                    }
                    else
                    {
                        Main.RevolutionistLastTime.TryAdd(player.PlayerId, Main.RevolutionistStart[player.PlayerId]);
                    }
                }
                else //如果不存在字典
                {
                    Main.RevolutionistStart.TryAdd(player.PlayerId, Utils.GetTimeStamp());
                }
            }
            #endregion

            if (!lowLoad)
            {
                //检查老兵技能是否失效
                if (GameStates.IsInTask && player.Is(CustomRoles.Veteran))
                {
                    if (Main.VeteranInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.VeteranSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.VeteranInProtect.Remove(player.PlayerId);
                        player.RpcGuardAndKill();
                        player.Notify(string.Format(GetString("VeteranOffGuard"), Main.VeteranNumOfUsed[player.PlayerId]));
                    }
                }
                //检查恐血者技能是否失效
                if (GameStates.IsInTask && player.Is(CustomRoles.Hemophobia))
                {
                    if (Main.HemophobiaInKill.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.HemophobiaSeconds.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.HemophobiaInKill.Remove(player.PlayerId);
                        player.RpcGuardAndKill();
                        player.Notify(GetString("HemophobiaSkillStop"));
                        Main.ForHemophobia.Remove(player.PlayerId);
                    }
                }
                if (GameStates.IsInTask && player.Is(CustomRoles.MagnetMan))
                {
                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) continue;
                        if (Vector2.Distance(player.transform.position, pc.transform.position) <= Options.MagnetManRadius.GetFloat())
                        {
                            Main.ForMagnetMan.Add(pc.PlayerId);
                        }
                        else
                        {
                            Main.ForMagnetMan.Remove(pc.PlayerId);
                        }
                    }
                }
                //检查灵化者
                if (GameStates.IsInTask && player.Is(CustomRoles.Spiritualists))
                {
                    foreach (var pc in Main.ForSpiritualists)
                    {
                        var Ss = Utils.GetPlayerById(pc);
                        Utils.TP(player.NetTransform, Ss.GetTruePosition());
                    }
                }
                if (GameStates.IsInTask && player.Is(CustomRoles.Henry))
                {
                    if (Henry.Choose == 0 && Henry.HenryCanSee.GetBool())
                    {
                        player.Notify(GetString("HenryChooseKill"));
                    }
                    else if (Henry.Choose == 1 && Henry.HenryCanSee.GetBool())
                    {
                        player.Notify(GetString("HenryChooseVent"));
                    }
                    else if (Henry.Choose == 2 && Henry.HenryCanSee.GetBool())
                    {
                        player.Notify(GetString("HenryChooseMeet"));
                    }
                    else if (Henry.Choose == 3 && Henry.HenryCanSee.GetBool())
                    {
                        player.Notify(GetString("HenryChooseReport"));
                    }
                    else
                    {
                        player.NotifyV2(GetString("HenryChoose"));
                    }
                }
                if (GameStates.IsInTask && player.Is(CustomRoles.RewardOfficer) && RewardOfficer.RewardOfficerShow.Contains(player.PlayerId))
                {
                    var Rp = player.PlayerId;
                    string roleName = GetString(Enum.GetName(player.GetCustomRole()));
                    if (RewardOfficer.RewardOfficerCanMode.GetInt() == 0)
                    {
                        foreach (var pc in Main.AllAlivePlayerControls)
                        {
                            if (RewardOfficer.ForRewardOfficer.Contains(pc.PlayerId))
                            {
                                roleName = GetString(Enum.GetName(pc.GetCustomRole()));
                            }
                        }
                        player.Notify(string.Format(GetString("RewardOfficerRoles"), roleName));
                    }
                }

                if (GameStates.IsInTask && player.Is(CustomRoles.Bait))
                {

                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (pc.PlayerId == player.PlayerId) continue;
                        if (Vector2.Distance(player.transform.position, pc.transform.position) <= 3f && pc.inVent)
                        {
                            player.Notify(GetString("BaitSeeVentPlayer"));
                        }
                    }
                }
                if (GameStates.IsInTask && player.Is(CustomRoles.MrDesperate))
                {
                    if (player.IsAlive())
                    {
                        if (LastFixedUpdate == Utils.GetTimeStamp()) return;
                        LastFixedUpdate = Utils.GetTimeStamp();
                        MrDesperate.KillTime[player.PlayerId]++;
                        MrDesperate.SendRPC(player.PlayerId);
                        if (MrDesperate.KillTime[player.PlayerId] <= 0)
                        {
                            player.RpcMurderPlayerV3(player);
                        }
                    }
                }
                if (GameStates.IsInTask && PlagueDoctor.SetImmunitytime.GetBool())
                {
                    if (LastFixedUpdate == Utils.GetTimeStamp()) return;
                    LastFixedUpdate = Utils.GetTimeStamp();
                    if (PlagueDoctor.Immunitytimes > 0)
                    {
                        PlagueDoctor.Immunitytimes--;
                        PlagueDoctor.ImmunityGone = false;

                    }
                    else if (PlagueDoctor.Immunitytimes <= 0)
                    {
                        PlagueDoctor.ImmunityGone = true;
                    }
                }


                //检查双刀手的第二把叨是否已经到时间
                if (GameStates.IsInTask && player.Is(CustomRoles.DoubleKiller))
                {
                    if (Main.DoubleKillerKillSeacond.TryGetValue(player.PlayerId, out var vtime) && vtime + DoubleKiller.TwoDoubleKillerKillColldown.GetInt() < Utils.GetTimeStamp() && !Main.DoubleKillerMax.Contains(player.PlayerId))
                    {
                        Main.DoubleKillerKillSeacond.Remove(player.PlayerId);
                        Main.DoubleKillerMax.Add(player.PlayerId);
                        player.Notify(GetString("DoubleKillerKillColldownTure"));
                        Logger.Info($"aaaaaa", "ReportDeadbody");
                    }
                }
                //彩虹变色 //来源：TOHY https://github.com/Yumenopai/TownOfHost_Y
                if (GameStates.IsInTask && player.Is(CustomRoles.Rainbow))
                {
                    var rain = IRandom.Instance;
                    int rndNum = rain.Next(0, 18);
                    if (rndNum is >= 1 and < 2) player.RpcSetColor(1);
                    else if (rndNum is >= 2 and < 3) player.RpcSetColor(10);
                    else if (rndNum is >= 3 and < 4) player.RpcSetColor(2);
                    else if (rndNum is >= 4 and < 5) player.RpcSetColor(11);
                    else if (rndNum is >= 5 and < 6) player.RpcSetColor(14);
                    else if (rndNum is >= 6 and < 7) player.RpcSetColor(5);
                    else if (rndNum is >= 7 and < 8) player.RpcSetColor(4);
                    else if (rndNum is >= 8 and < 9) player.RpcSetColor(17);
                    else if (rndNum is >= 9 and < 10) player.RpcSetColor(0);
                    else if (rndNum is >= 10 and < 11) player.RpcSetColor(3);
                    else if (rndNum is >= 11 and < 12) player.RpcSetColor(13);
                    else if (rndNum is >= 12 and < 13) player.RpcSetColor(7);
                    else if (rndNum is >= 13 and < 14) player.RpcSetColor(15);
                    else if (rndNum is >= 14 and < 15) player.RpcSetColor(6);
                    else if (rndNum is >= 15 and < 16) player.RpcSetColor(12);
                    else if (rndNum is >= 16 and < 17) player.RpcSetColor(9);
                    else if (rndNum is >= 17 and < 18) player.RpcSetColor(16);
                }
                //套皮者传送
                if (GameStates.IsInTask && player.Is(CustomRoles.Sleeve))
                {
                    foreach (var pc in Main.ForSleeve)
                    {
                        var Sl = Utils.GetPlayerById(pc);
                        Utils.TP(Sl.NetTransform, player.GetTruePosition());
                    }
                }
                if (GameStates.IsInTask && player.Is(CustomRoles.Cluster))
                {
                    foreach (var pc in Main.ForCluster)
                    {
                        var Cl = Utils.GetPlayerById(pc);
                        foreach (var pl in Main.AllAlivePlayerControls)
                        {
                            if (pl.PlayerId == pc) continue;
                            Utils.TP(pl.NetTransform, Cl.GetTruePosition());
                        }
                    }
                }



                //检查护士是否已经完成救治
                if (GameStates.IsInTask && player.Is(CustomRoles.Nurse))
                {
                    if (Main.ForNnurse.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.NurseSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.ForNnurse.Remove(player.PlayerId);
                        player.RpcGuardAndKill();
                        player.Notify(GetString("ForNnurseCanHelp"));
                        Main.NnurseHelep.Remove(player.PlayerId);
                    }
                }
                //检查爆破狂的炸弹是否到了引爆时间
                if (GameStates.IsInTask && player.Is(CustomRoles.DemolitionManiac))
                {
                    if (Main.InBoom.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.DemolitionManiacWait.GetFloat() < Utils.GetTimeStamp())
                    {

                        foreach (var pc in Main.AllPlayerControls)
                        {
                            if (!pc.IsModClient()) pc.KillFlash();
                            if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) continue;
                            if (Main.ForDemolition.Contains(pc.PlayerId))
                            {
                                foreach (var BoomPlayer in Main.AllPlayerControls)
                                {
                                    if (Vector2.Distance(pc.transform.position, BoomPlayer.transform.position) <= Options.DemolitionManiacRadius.GetFloat())
                                    {
                                        Main.PlayerStates[BoomPlayer.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                                        BoomPlayer.SetRealKiller(player);
                                        BoomPlayer.RpcMurderPlayerV3(BoomPlayer);
                                    }
                                }

                            }
                        }
                    }
                }

                //检查掷雷兵技能是否生效
                if (GameStates.IsInTask && player.Is(CustomRoles.Grenadier))
                {
                    if (Main.GrenadierBlinding.TryGetValue(player.PlayerId, out var gtime) && gtime + Options.GrenadierSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.GrenadierBlinding.Remove(player.PlayerId);
                        player.RpcGuardAndKill();
                        player.Notify(GetString("GrenadierSkillStop"));
                        Utils.MarkEveryoneDirtySettings();
                    }
                    if (Main.MadGrenadierBlinding.TryGetValue(player.PlayerId, out var mgtime) && mgtime + Options.GrenadierSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.MadGrenadierBlinding.Remove(player.PlayerId);
                        player.RpcGuardAndKill();
                        player.Notify(GetString("GrenadierSkillStop"));
                        Utils.MarkEveryoneDirtySettings();
                    }
                }
                //检查掷弹兵技能是否生效
                if (GameStates.IsInTask && player.Is(CustomRoles.Grenadiers))
                {
                    if (Main.GrenadiersInProtect.TryGetValue(player.PlayerId, out var gtime) && gtime + Options.GrenadiersDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.GrenadiersInProtect.Remove(player.PlayerId);
                        player.RpcGuardAndKill();
                        player.Notify(GetString("GrenadierSkillStop"));
                        Utils.MarkEveryoneDirtySettings();
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            Main.ForGrenadiers.Remove(pc.PlayerId);
                        }
                    }
                }
                //检查扰乱者技能是否失效
                if (GameStates.IsInTask && player.Is(CustomRoles.sabcat))
                {
                    if (Main.sabcatInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.sabcatCooldown.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.sabcatInProtect.Remove(player.PlayerId);
                        player.RpcGuardAndKill();
                        player.Notify(string.Format(GetString("sabcatOffGuard"), Main.sabcatNumOfUsed[player.PlayerId]));
                    }
                }
                //检查时间之主技能是否失效
                if (GameStates.IsInTask && player.Is(CustomRoles.TimeMaster))
                {
                    if (Main.TimeMasterInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.TimeMasterSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.TimeMasterInProtect.Remove(player.PlayerId);
                        player.RpcGuardAndKill();
                        player.Notify(GetString("TimeMasterSkillStop"));
                    }
                }
                //检查正义的护盾师的技能是否失效
                if (GameStates.IsInTask && player.Is(CustomRoles.NiceShields))
                {
                    if (Main.NiceShieldsInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.NiceShieldsSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.NiceShieldsInProtect.Remove(player.PlayerId);
                        player.RpcGuardAndKill();
                        player.Notify(GetString("NiceShieldsSkillStop"));
                    }
                }
                //检查时停者的技能是否生效
                if (GameStates.IsInTask && player.Is(CustomRoles.TimeStops))
                {
                    if (Main.TimeStopsInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.TimeStopsSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.TimeStopsInProtect.Remove(player.PlayerId);
                        player.RpcGuardAndKill();
                        player.Notify(GetString("TimeStopsSkillStop"));
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            Main.TimeStopsstop.Remove(pc.PlayerId);
                        }
                    }
                }

                //检查马里奥是否完成
                if (GameStates.IsInTask && player.Is(CustomRoles.Mario) && Main.MarioVentCount[player.PlayerId] > Options.MarioVentNumWin.GetInt())
                {
                    Main.MarioVentCount[player.PlayerId] = Options.MarioVentNumWin.GetInt();
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Mario); //马里奥这个多动症赢了
                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                }
                foreach (var injured in Main.AllAlivePlayerControls)
                {
                    if (GameStates.IsInTask && Main.DyingTurns >= Options.InjuredTurns.GetInt() && injured.Is(CustomRoles.Injured))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Injured);
                        CustomWinnerHolder.WinnerIds.Add(injured.PlayerId);
                    }
                }
                foreach (var mini in Main.AllPlayerControls)
                {
                    if (GameStates.IsInTask && mini.Is(CustomRoles.NiceMini) && Mini.Age < 18 && !mini.IsAlive())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.NiceMini);
                        CustomWinnerHolder.WinnerIds.Add(mini.PlayerId);
                    }
                }

                Pelican.OnFixedUpdate();
                BallLightning.OnFixedUpdate();
                Swooper.OnFixedUpdate(player);
                BloodKnight.OnFixedUpdate(player);
                Yandere.OnFixedUpdate(player);
                PlagueDoctor.OnFixedUpdate(player);
                Mini.OnFixedUpdate(player);
                Chameleon.OnFixedUpdate(player);

                //Kidnapper.OnFixedUpdate(player);

                if (GameStates.IsInTask && player.IsAlive() && Options.LadderDeath.GetBool()) FallFromLadder.FixedUpdate(player);

                if (GameStates.IsInGame) LoversSuicide();
                if (GameStates.IsInGame) JackalSuicide();
                if (GameStates.IsInGame) CaptainSuicide();
                if (GameStates.IsInGame) SheriffSuicide();
                if (GameStates.IsInGame) CrushLoversSuicide();
                if (GameStates.IsInGame) CupidLoversSuicide();
                if (GameStates.IsInGame) AkujoLoversSuicide();
                if (GameStates.IsInGame) ImposotorSuicide();
                if (GameStates.IsInGame) HunterSuicide();
                if (GameStates.IsInGame) MimicSuicide();

                #region 傀儡师处理
                if (GameStates.IsInTask && Main.PuppeteerList.ContainsKey(player.PlayerId))
                {
                    if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId))
                    {
                        Main.PuppeteerList.Remove(player.PlayerId);
                        RPC.RpcSyncPuppeteerList();
                    }
                    else
                    {
                        Vector2 puppeteerPos = player.transform.position;//PuppeteerListのKeyの位置
                        Dictionary<byte, float> targetDistance = new();
                        float dis;
                        foreach (var target in Main.AllAlivePlayerControls)
                        {
                            if (target.PlayerId != player.PlayerId && !target.Is(CountTypes.Impostor))
                            {
                                dis = Vector2.Distance(puppeteerPos, target.transform.position);
                                targetDistance.Add(target.PlayerId, dis);
                            }
                        }
                        if (targetDistance.Count() != 0)
                        {
                            var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                            PlayerControl target = Utils.GetPlayerById(min.Key);
                            var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                            if (min.Value <= KillRange && player.CanMove && target.CanMove)
                            {
                                if (player.RpcCheckAndMurder(target, true))
                                {
                                    var puppeteerId = Main.PuppeteerList[player.PlayerId];
                                    RPC.PlaySoundRPC(puppeteerId, Sounds.KillSound);
                                    target.SetRealKiller(Utils.GetPlayerById(puppeteerId));
                                    player.RpcMurderPlayerV3(target);
                                    Utils.MarkEveryoneDirtySettings();
                                    Main.PuppeteerList.Remove(player.PlayerId);
                                    RPC.RpcSyncPuppeteerList();
                                    Utils.NotifyRoles();
                                }
                            }
                        }
                    }
                }
                #endregion

                if (GameStates.IsInTask && player == PlayerControl.LocalPlayer)
                    DisableDevice.FixedUpdate();
                if (GameStates.IsInTask && player == PlayerControl.LocalPlayer)
                    AntiAdminer.FixedUpdate();

                if (GameStates.IsInGame && Main.RefixCooldownDelay <= 0)
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.Vampire) || pc.Is(CustomRoles.Warlock) || pc.Is(CustomRoles.Assassin))
                            Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown * 2;
                    }

                if (!Main.DoBlockNameChange && AmongUsClient.Instance.AmHost)
                    Utils.ApplySuffix(__instance);
            }
        }
        //LocalPlayer専用
        if (__instance.AmOwner)
        {
            //キルターゲットの上書き処理
            if (GameStates.IsInTask && !__instance.Is(CustomRoleTypes.Impostor) && __instance.CanUseKillButton() && !__instance.Data.IsDead)
            {
                var players = __instance.GetPlayersInAbilityRangeSorted(false);
                PlayerControl closest = players.Count <= 0 ? null : players[0];
                HudManager.Instance.KillButton.SetTarget(closest);
            }
        }

        //役職テキストの表示
        var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
        var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
        if (RoleText != null && __instance != null && !lowLoad)
        {
            if (GameStates.IsLobby)
            {
                if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver))
                {
                    if (Main.ForkId != ver.forkId) // フォークIDが違う場合
                        __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
                    else if (Main.version.CompareTo(ver.version) == 0)
                        __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{__instance.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{__instance?.name}</color>";
                    else __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
                }
                else __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
            }
            if (GameStates.IsInGame)
            {
                var RoleTextData = Utils.GetRoleText(PlayerControl.LocalPlayer.PlayerId, __instance.PlayerId);
                //if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                //{
                //    var hasRole = main.AllPlayerCustomRoles.TryGetValue(__instance.PlayerId, out var role);
                //    if (hasRole) RoleTextData = Utils.GetRoleTextHideAndSeek(__instance.Data.Role.Role, role);
                //}
                RoleText.text = RoleTextData.Item1;
                if (Options.CurrentGameMode == CustomGameMode.SoloKombat) RoleText.text = "";
                RoleText.color = RoleTextData.Item2;
                if (__instance.AmOwner) RoleText.enabled = true; //自分ならロールを表示
                else if (Options.CurrentGameMode == CustomGameMode.SoloKombat) RoleText.enabled = true;
                else if (Main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) RoleText.enabled = true; //他プレイヤーでVisibleTasksCountが有効なおかつ自分が死んでいるならロールを表示
                else if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Lovers) && Options.LoverKnowRoles.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.CrushLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.CrushLovers) && Options.CrushLoverKnowRoles.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.CupidLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.CupidLovers) && Options.CupidLoverKnowRoles.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.CupidLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Cupid) && Options.CanKnowCupid.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoleTypes.Impostor) && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) && Options.ImpKnowAlliesRole.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoleTypes.Impostor) && PlayerControl.LocalPlayer.Is(CustomRoles.Madmate) && Options.MadmateKnowWhosImp.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.Madmate) && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) && Options.ImpKnowWhosMadmate.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.Madmate) && PlayerControl.LocalPlayer.Is(CustomRoles.Madmate) && Options.MadmateKnowWhosMadmate.GetBool()) RoleText.enabled = true;
                else if (Totocalcio.KnowRole(PlayerControl.LocalPlayer, __instance)) RoleText.enabled = true;
                else if (Succubus.KnowRole(PlayerControl.LocalPlayer, __instance)) RoleText.enabled = true;
                else if (Jackal.KnowRole(PlayerControl.LocalPlayer, __instance)) RoleText.enabled = true;
                else if (Corpse.KnowRole(PlayerControl.LocalPlayer, __instance)) RoleText.enabled = true;
                else if (Captain.KnowRole(PlayerControl.LocalPlayer, __instance)) RoleText.enabled = true;
                else if (Lawyer.KnowRole(PlayerControl.LocalPlayer, __instance)) RoleText.enabled = true;
                else if (Yandere.KnowRole(PlayerControl.LocalPlayer, __instance)) RoleText.enabled = true;
                //喵喵队
                //内鬼
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isimp == true && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoleTypes.Impostor) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isimp == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                //豺狼
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isjac == true && PlayerControl.LocalPlayer.Is(CustomRoles.Jackal) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.Jackal) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isjac == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isjac == true && PlayerControl.LocalPlayer.Is(CustomRoles.Sidekick) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.Sidekick) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isjac == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isjac == true && PlayerControl.LocalPlayer.Is(CustomRoles.Whoops) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.Whoops) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isjac == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isjac == true && PlayerControl.LocalPlayer.Is(CustomRoles.Attendant) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.Attendant) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isjac == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                //西风骑士团(bushi)
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isbk == true && PlayerControl.LocalPlayer.Is(CustomRoles.BloodKnight) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.BloodKnight) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isbk == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                //疫情的源头
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.ispg == true && PlayerControl.LocalPlayer.Is(CustomRoles.PlaguesGod) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.PlaguesGod) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.ispg == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                //玩家
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isgam == true && PlayerControl.LocalPlayer.Is(CustomRoles.Gamer) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.Gamer) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isgam == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                //穹P黑客(BUSHI)
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isyl == true && PlayerControl.LocalPlayer.Is(CustomRoles.YinLang) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.YinLang) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isyl == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                //黑，真tm黑
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isdh == true && PlayerControl.LocalPlayer.Is(CustomRoles.DarkHide) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.DarkHide) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isdh == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                //雇佣
                else if (__instance.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isok == true && PlayerControl.LocalPlayer.Is(CustomRoles.OpportunistKiller) && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (__instance.Is(CustomRoles.OpportunistKiller) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isok == true && Options.CanKnowKiller.GetBool()) RoleText.enabled = true;
                else if (PlayerControl.LocalPlayer.Is(CustomRoles.God)) RoleText.enabled = true;
                else if (PlayerControl.LocalPlayer.Is(CustomRoles.GM)) RoleText.enabled = true;
                else if (Main.GodMode.Value) RoleText.enabled = true;
                else RoleText.enabled = false; //そうでなければロールを非表示
                if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                {
                    RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                    if (!__instance.AmOwner) __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                }
                if (Main.VisibleTasksCount) //他プレイヤーでVisibleTasksCountは有効なら
                    RoleText.text += Utils.GetProgressText(__instance); //ロールの横にタスクなど進行状況表示


                //変数定義
                var seer = PlayerControl.LocalPlayer;
                var target = __instance;


                string RealName;
                Mark.Clear();
                Suffix.Clear();

                //名前変更
                RealName = target.GetRealName();

                //名前色変更処理
                //自分自身の名前の色を変更
                if (target.AmOwner && GameStates.IsInTask)
                { //targetが自分自身
                    if (target.Is(CustomRoles.Arsonist) && target.IsDouseDone())
                        RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), GetString("EnterVentToWin"));
                    if (target.Is(CustomRoles.Revolutionist) && target.IsDrawDone())
                        RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Revolutionist), string.Format(GetString("EnterVentWinCountDown"), Main.RevolutionistCountdown.TryGetValue(seer.PlayerId, out var x) ? x : 10));

                    if (Pelican.IsEaten(seer.PlayerId))
                        RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pelican), GetString("EatenByPelican"));
                    if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
                        SoloKombatManager.GetNameNotify(target, ref RealName);
                    if (Options.CurrentGameMode == CustomGameMode.HotPotato)
                        HotPotatoManager.GetNameNotify(target, ref RealName);
                    if (NameNotifyManager.GetNameNotify(target, out var name))
                        RealName = name;
                }

                //NameColorManager準拠の処理
                RealName = RealName.ApplyNameColorData(seer, target, false);

                if (seer.GetCustomRole().IsImpostor()) //seerがインポスター
                {
                    if (target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate) && target.GetPlayerTaskState().IsTaskFinished) //targetがタスクを終わらせたマッドスニッチ
                        Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "★")); //targetにマーク付与
                }
                //インポスター/キル可能なニュートラルがタスクが終わりそうなSnitchを確認できる
                Mark.Append(Snitch.GetWarningMark(seer, target));

                if (seer.Is(CustomRoles.Arsonist))
                {
                    if (seer.IsDousedPlayer(target))
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Arsonist)}>▲</color>");
                    }
                    else if (
                        Main.currentDousingTarget != byte.MaxValue &&
                        Main.currentDousingTarget == target.PlayerId
                    )
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Arsonist)}>△</color>");
                    }
                }
                if (seer.Is(CustomRoles.Revolutionist))
                {
                    if (seer.IsDrawPlayer(target))
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Revolutionist)}>●</color>");
                    }
                    else if (
                        Main.currentDrawTarget != byte.MaxValue &&
                        Main.currentDrawTarget == target.PlayerId
                    )
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Revolutionist)}>○</color>");
                    }
                }

                Mark.Append(Executioner.TargetMark(seer, target));

                Mark.Append(Gamer.TargetMark(seer, target));

                Mark.Append(PlagueDoctor.TargetMark(seer, target));

                Mark.Append(Yandere.TargetMark(seer, target));

                Mark.Append(Totocalcio.TargetMark(seer, target));

                Mark.Append(Lawyer.TargetMark(seer, target));

                if (seer.PlayerId == target.PlayerId && (Medic.InProtect(seer.PlayerId) || Medic.TempMarkProtected == seer.PlayerId) && (Medic.WhoCanSeeProtect.GetInt() == 0 || Medic.WhoCanSeeProtect.GetInt() == 2))
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Medic)}> ●</color>");

                if (seer.Is(CustomRoles.Medic) && (Medic.InProtect(target.PlayerId) || Medic.TempMarkProtected == target.PlayerId) && (Medic.WhoCanSeeProtect.GetInt() == 0 || Medic.WhoCanSeeProtect.GetInt() == 1))
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Medic)}> ●</color>");

                if (seer.Data.IsDead && Medic.InProtect(target.PlayerId) && !seer.Is(CustomRoles.Medic))
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Medic)}> ●</color>");

                if (seer.Is(CustomRoles.Puppeteer))
                {
                    if (seer.Is(CustomRoles.Puppeteer) &&
                    Main.PuppeteerList.ContainsValue(seer.PlayerId) &&
                    Main.PuppeteerList.ContainsKey(target.PlayerId))
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>◆</color>");
                }
                if (Sniper.IsEnable && target.AmOwner)
                {
                    //銃声が聞こえるかチェック
                    Mark.Append(Sniper.GetShotNotify(target.PlayerId));

                }
                if (seer.Is(CustomRoles.EvilTracker)) Mark.Append(EvilTracker.GetTargetMark(seer, target));
                //タスクが終わりそうなSnitchがいるとき、インポスター/キル可能なニュートラルに警告が表示される
                Mark.Append(Snitch.GetWarningArrow(seer, target));

                if (target.Is(CustomRoles.NiceMini) && Mini.EveryoneCanKnowMini.GetBool())
                    Mark.Append(Utils.ColorString(Color.yellow, Mini.Age != 18 ? $"({Mini.Age})" : ""));
                if (target.Is(CustomRoles.EvilMini) && Mini.EveryoneCanKnowMini.GetBool())
                    Mark.Append(Utils.ColorString(Color.yellow, Mini.Age != 18 ? $"({Mini.Age})" : ""));
                if (target.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.SuperStar), "★"));
                if (target.Is(CustomRoles.Captain))
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Captain), " ★Cap★ "));

                if (target.Is(CustomRoles.QL) && Options.EveryOneKnowQL.GetBool())
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.QL), "♛"));

                if (target.Is(CustomRoles.Hotpotato))
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Hotpotato), "●"));

                if (BallLightning.IsGhost(target))
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.BallLightning), "■"));

                //ハートマークを付ける(会議中MOD視点)
                if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Lovers))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>");
                }
                else if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>");
                }
                else if (__instance.Is(CustomRoles.Ntr) || PlayerControl.LocalPlayer.Is(CustomRoles.Ntr))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>");
                }
                else if (__instance == PlayerControl.LocalPlayer && CustomRolesHelper.RoleExist(CustomRoles.Ntr))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>");
                }
                else if (__instance.Is(CustomRoles.CrushLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.CrushLovers))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.CrushLovers)}>♡</color>");
                }
                else if (__instance.Is(CustomRoles.CrushLovers) && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.CrushLovers)}>♡</color>");
                }
                else if (__instance.Is(CustomRoles.CupidLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.CupidLovers))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.CupidLovers)}>♡</color>");
                }
                else if (__instance.Is(CustomRoles.CupidLovers) && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.CupidLovers)}>♡</color>");
                }
                else if (__instance.Is(CustomRoles.Akujo) && PlayerControl.LocalPlayer.Is(CustomRoles.Honmei))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Akujo)}>♥</color>");
                }
                else if (__instance.Is(CustomRoles.Akujo) && PlayerControl.LocalPlayer.Is(CustomRoles.Backup))
                {
                    Mark.Append($"<color={Color.grey}>♥</color>");
                }
                else if ((__instance.Is(CustomRoles.Backup) || (__instance.Is(CustomRoles.Honmei)) && PlayerControl.LocalPlayer.Is(CustomRoles.Akujo)))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Akujo)}>♥</color>");
                }
                //矢印オプションありならタスクが終わったスニッチはインポスター/キル可能なニュートラルの方角がわかる
                Suffix.Append(Snitch.GetSnitchArrow(seer, target));

                Suffix.Append(Mimics.GetTargetArrow(seer, target));

                Suffix.Append(Mimics.GetKillArrow(seer, target));

                Suffix.Append(BountyHunter.GetTargetArrow(seer, target));

                Suffix.Append(Mortician.GetTargetArrow(seer, target));

                Suffix.Append(Vulture.GetTargetArrow(seer, target));

                Suffix.Append(BloodSeekers.GetTargetArrow(seer, target));

                Suffix.Append(EvilTracker.GetTargetArrow(seer, target));

                Suffix.Append(NiceTracker.GetTargetArrow(seer, target));

                Suffix.Append(Yandere.GetTargetArrow(seer, target));

                // Suffix.Append(Mimics.GetTargetArrow(seer, target));

                //     Suffix.Append(NiceTracker.GetTargetArrow(seer, target));

                if (GameStates.IsInTask && seer.Is(CustomRoles.AntiAdminer))
                {
                    AntiAdminer.FixedUpdate();
                    if (target.AmOwner)
                    {
                        if (AntiAdminer.IsAdminWatch) Suffix.Append("★" + GetString("AntiAdminerAD"));
                        if (AntiAdminer.IsVitalWatch) Suffix.Append("★" + GetString("AntiAdminerVI"));
                        if (AntiAdminer.IsDoorLogWatch) Suffix.Append("★" + GetString("AntiAdminerDL"));
                        if (AntiAdminer.IsCameraWatch) Suffix.Append("★" + GetString("AntiAdminerCA"));
                    }
                }

                if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
                    Suffix.Append(SoloKombatManager.GetDisplayHealth(target));

                /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                    Mark = isBlocked ? "(true)" : "(false)";
                }*/
                if ((Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool()) || Concealer.IsHidding)
                    RealName = $"<size=0>{RealName}</size> ";

                string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})" : "";
                //Mark・Suffixの適用
                target.cosmetics.nameText.text = $"{RealName}{DeathReason}{Mark}";

                if (Suffix.ToString() != "")
                {
                    //名前が2行になると役職テキストを上にずらす必要がある
                    RoleText.transform.SetLocalY(0.35f);
                    target.cosmetics.nameText.text += "\r\n" + Suffix.ToString();

                }
                else
                {
                    //役職テキストの座標を初期値に戻す
                    RoleText.transform.SetLocalY(0.2f);
                }
            }
            else
            {
                //役職テキストの座標を初期値に戻す
                RoleText.transform.SetLocalY(0.2f);
            }
        }
    }
    public static void ImposotorSuicide()
    {
        if (Options.CanDefector.GetBool())
        {
            int DefectorInt = 0;
            int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
            int ImIntDead = 0;
            int AlivePlayerRemain = 0;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                AlivePlayerRemain++;
            }
            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.IsAlive() && player.GetCustomRole().IsImpostor() && !Main.KillImpostor.Contains(player.PlayerId) && !player.Is(CustomRoles.Defector))
                {
                    Main.KillImpostor.Add(player.PlayerId);
                    ImIntDead++;
                    foreach (var partnerPlayer in Main.AllAlivePlayerControls)
                    {
                        if (ImIntDead != optImpNum) continue;
                        if (AlivePlayerRemain < Options.DefectorRemain.GetInt()) continue;
                        if (partnerPlayer.GetCustomRole().IsCrewmate() && partnerPlayer.CanUseKillButton() && DefectorInt == 0)
                        {
                            Logger.Info($"背叛了", "我就是大名鼎鼎的.....");
                            DefectorInt++;
                            partnerPlayer.RpcSetCustomRole(CustomRoles.Defector);
                            partnerPlayer.ResetKillCooldown();
                            partnerPlayer.SetKillCooldown();
                            partnerPlayer.RpcGuardAndKill(partnerPlayer);
                            partnerPlayer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("BecomeDft")));
                        }
                    }
                }
            }
        }
    }
    public static void JackalSuicide()
    {
        if (CustomRoles.Jackal.IsEnable() && Jackal.SidekickCanBeJackal.GetBool())
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.IsAlive() && player.Is(CustomRoles.Jackal) && !Jackal.JackalList.Contains(player.PlayerId))
                {
                    Jackal.JackalList.Add(player.PlayerId);
                    foreach (var partnerPlayer in Main.AllAlivePlayerControls)
                    {
                        if (partnerPlayer.Is(CustomRoles.Sidekick))
                        {
                            partnerPlayer.RpcSetCustomRole(CustomRoles.Jackal);
                            Jackal.Add(partnerPlayer.PlayerId);
                            partnerPlayer.ResetKillCooldown();
                            partnerPlayer.SetKillCooldown();
                            partnerPlayer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("BecomeJackal")));
                            partnerPlayer.RpcGuardAndKill(partnerPlayer);
                        }
                    }
                }
            }
        }
    }
    public static void SheriffSuicide()
    {
        if (CustomRoles.Sheriff.IsEnable() && Deputy.DeputyCanBeSheriff.GetBool())
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.IsAlive() && player.Is(CustomRoles.Sheriff))
                {
                    foreach (var partnerPlayer in Main.AllAlivePlayerControls)
                    {
                        if (partnerPlayer.Is(CustomRoles.Deputy))
                        {
                            partnerPlayer.RpcSetCustomRole(CustomRoles.Sheriff);
                            Sheriff.Add(partnerPlayer.PlayerId);
                            partnerPlayer.ResetKillCooldown();
                            partnerPlayer.SetKillCooldown();
                            partnerPlayer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Sheriff), GetString("BecomeSheriff")));
                            partnerPlayer.RpcGuardAndKill(partnerPlayer);
                        }
                    }
                }
            }
        }
    }
    public static void CaptainSuicide()
    {
        if (CustomRoles.Captain.IsEnable())
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.IsAlive() && player.Is(CustomRoles.Captain))
                {
                    foreach (var partnerPlayer in Main.AllAlivePlayerControls)
                    {
                        if (partnerPlayer.Is(CustomRoles.Solicited))
                        {
                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.Ownerless;
                            if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                            {
                                partnerPlayer?.RpcExileV2();
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Ownerless, partnerPlayer.PlayerId);
                            }
                            else
                            {
                                partnerPlayer.RpcMurderPlayerV3(partnerPlayer);
                            }
                        }
                    }
                }
            }
        }
    }
    public static void MimicSuicide()
    {
        if (CustomRoles.Mimics.IsEnable())
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (player.Is(CustomRoles.MimicKiller) && !player.IsAlive())
                {
                    foreach (var partnerPlayer in Main.AllAlivePlayerControls)
                    {

                        if (partnerPlayer.Is(CustomRoles.MimicAss) && Mimics.DiedToge.GetInt() == 0)
                        {
                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                            if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                            {
                                partnerPlayer?.RpcExileV2();
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, partnerPlayer.PlayerId);
                            }
                            else
                            {
                                partnerPlayer.RpcMurderPlayerV3(partnerPlayer);
                            }
                        }
                        else if (partnerPlayer.Is(CustomRoles.MimicAss) && Mimics.DiedToge.GetInt() == 1)
                        {
                            partnerPlayer.RpcSetCustomRole(CustomRoles.MimicKiller);
                            partnerPlayer.ResetKillCooldown();
                            partnerPlayer.SetKillCooldown();
                            partnerPlayer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("MKDIE")));
                            partnerPlayer.RpcGuardAndKill(partnerPlayer);
                        }
                        else if (partnerPlayer.Is(CustomRoles.MimicAss) && Mimics.DiedToge.GetInt() == 2)
                        {
                            partnerPlayer.RpcSetCustomRole(CustomRoles.Shapeshifter);
                            partnerPlayer.ResetKillCooldown();
                            partnerPlayer.SetKillCooldown();
                            partnerPlayer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("MKDIE")));
                            partnerPlayer.RpcGuardAndKill(partnerPlayer);
                        }
                    }
                }
            }
        }
    }
    public static void HunterSuicide()
    {
        if (CustomRoles.Hunter.IsEnable())
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.IsAlive() && player.Is(CustomRoles.Hunter))
                {
                    foreach (var partnerPlayer in Main.HunterTarget)
                    {
                        if (partnerPlayer.IsAlive())
                        {

                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                            if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                            {
                                partnerPlayer?.RpcExileV2();
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, partnerPlayer.PlayerId);
                            }
                            else
                            {
                                partnerPlayer.RpcMurderPlayerV3(partnerPlayer);
                            }
                        }
                    }
                }
            }
        }
    }
    public static void LoversSuicide()
    {
        if (Options.LoverSuicide.GetBool() && CustomRoles.Lovers.IsEnable() && Main.isLoversDead == false)
        {
            foreach (var loversPlayer in Main.LoversPlayers)
            {
                if (!loversPlayer.IsAlive())
                {
                    foreach (var partnerPlayer in Main.LoversPlayers)
                    {
                        if (partnerPlayer.IsAlive())
                        {
                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.FollowingSuicide;
                            if (Main.PlayerStates[loversPlayer.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                            {
                                partnerPlayer.RpcExileV2();
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            }
                            else
                                partnerPlayer.RpcMurderPlayerV3(partnerPlayer);
                        }
                    }
                }
            }
        }
    }
    public static void CrushLoversSuicide()
    {
        if (Options.CrushLoverSuicide.GetBool() && CustomRoles.Crush.IsEnable() && Main.isCrushLoversDead == false)
        {
            foreach (var loversPlayer in Main.CrushLoversPlayers)
            {
                if (!loversPlayer.IsAlive())
                {
                    foreach (var partnerPlayer in Main.CrushLoversPlayers)
                    {
                        if (partnerPlayer.IsAlive())
                        {
                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.FollowingSuicide;
                            if (Main.PlayerStates[loversPlayer.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                            {
                                partnerPlayer.RpcExileV2();
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            }
                            else
                                partnerPlayer.RpcMurderPlayerV3(partnerPlayer);
                        }
                    }
                }
            }
        }
    }

    public static void CupidLoversSuicide(byte deathId = 0x7f, bool isExiled = false, bool now = false)
    {
        foreach (var cu in Main.CupidLoversPlayers)
        {
            if (cu.Is(CustomRoles.Cupid))
            {
                Main.CupidLoversPlayers.Remove(cu);
            }
        }
        if (Options.CupidLoverSuicide.GetBool() && CustomRoles.Cupid.IsEnable() && Main.isCupidLoversDead == false && Main.CupidLoversPlayers.Count >= 2 && Main.CupidComplete)
        {
            foreach (var loversPlayer in Main.CupidLoversPlayers)
            {
                if (!loversPlayer.IsAlive())
                {
                    foreach (var partnerPlayer in Main.CupidLoversPlayers)
                    {
                        if (partnerPlayer.IsAlive())
                        {
                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.FollowingSuicide;
                            if (Main.PlayerStates[loversPlayer.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                            {
                                partnerPlayer.RpcExileV2();
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            }
                            else
                                partnerPlayer.RpcMurderPlayerV3(partnerPlayer);
                        }
                    }
                    foreach (var cupid in Main.AllAlivePlayerControls)
                    {
                        if (cupid.Is(CustomRoles.Cupid))
                        {
                            Main.PlayerStates[cupid.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                            if (Main.PlayerStates[loversPlayer.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                            {
                                cupid.RpcExileV2();
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, cupid.PlayerId);
                            }
                            else
                                cupid.RpcMurderPlayerV3(cupid);
                        }
                    }
                }
            }
        }
    }
    public static void AkujoLoversSuicide(byte deathId = 0x7f, bool isExiled = false, bool now = false)
    {
        if (CustomRoles.Akujo.IsEnable() && Main.isAkujoLoversDead == false)
        {
            foreach (var loversPlayer in Main.AkujoLoversPlayers)
            {
                if (!loversPlayer.IsAlive())
                {
                    foreach (var partnerPlayer in Main.AkujoLoversPlayers)
                    {
                        if (partnerPlayer.IsAlive())
                        {
                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.FollowingSuicide;
                            if (Main.PlayerStates[loversPlayer.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                            {
                                partnerPlayer.RpcExileV2();
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            }
                            else
                                partnerPlayer.RpcMurderPlayerV3(partnerPlayer);
                        }
                    }
                }
            }
        }
    }

}
