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
using TheOtherRoles_Host.Roles.GameModsRoles;
using TheOtherRoles_Host.GameMode;

namespace TheOtherRoles_Host;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
class CheckProtectPatch
{

    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        Logger.Info("CheckProtect発生: " + __instance.GetNameWithRole() + "=>" + target.GetNameWithRole(), "CheckProtect");
        if (__instance.Is(CustomRoles.Sheriff))
        {
            if (__instance.Data.IsDead)
            {
                Logger.Info("守護をブロックしました。", "CheckProtect");
                return false;
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
class CheckMurderPatch
{
    public static int YLLevel = 0;
    public static int YLdj = 1;
    public static int YLCS = 0;
    public static Dictionary<byte, float> TimeSinceLastKill = new();
    private static Dictionary<byte, float> NowCooldown;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public static void Update()
    {
        for (byte i = 0; i < 15; i++)
        {
            if (TimeSinceLastKill.ContainsKey(i))
            {
                TimeSinceLastKill[i] += Time.deltaTime;
                if (15f < TimeSinceLastKill[i]) TimeSinceLastKill.Remove(i);
            }
        }
    }
    #region 击杀技能判定
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        var killer = __instance; //読み替え変数

        Logger.Info($"{killer.GetNameWithRole()} => {target.GetNameWithRole()}", "CheckMurder");

        //死人はキルできない
        if (killer.Data.IsDead)
        {
            Logger.Info($"{killer.GetNameWithRole()}は死亡しているためキャンセルされました。", "CheckMurder");
            return false;
        }

        //不正キル防止処理
        if (target.Data == null || //PlayerDataがnullじゃないか確認
            target.inVent || target.inMovingPlat //targetの状態をチェック
        )
        {
            Logger.Info("目标处于无法被击杀状态，击杀被取消", "CheckMurder");
            return false;
        }
        if (target.Data.IsDead) //同じtargetへの同時キルをブロック
        {
            Logger.Info("目标处于死亡状态，击杀被取消", "CheckMurder");
            return false;
        }
        if (MeetingHud.Instance != null) //会議中でないかの判定
        {
            Logger.Info("会议中，击杀被取消", "CheckMurder");
            return false;
        }

        var divice = Options.CurrentGameMode == CustomGameMode.SoloKombat ? 3000f : 2000f;
        var pivice = Options.CurrentGameMode == CustomGameMode.HotPotato ? 3000f : 2000f;
        var bivice = Options.CurrentGameMode == CustomGameMode.TheLivingDaylights ? 3000f : 2000f; 
        float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / divice * 6f); //※AmongUsClient.Instance.Pingの値はミリ秒(ms)なので÷1000
        //TimeSinceLastKillに値が保存されていない || 保存されている時間がminTime以上 => キルを許可
        //↓許可されない場合
        if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
        {
            Logger.Info("击杀间隔过短，击杀被取消", "CheckMurder");
            return false;
        }
        TimeSinceLastKill[killer.PlayerId] = 0f;

        killer.ResetKillCooldown();

        //キル可能判定
        if (killer.PlayerId != target.PlayerId && !killer.CanUseKillButton())
        {
            Logger.Info(killer.GetNameWithRole() + "击杀者不被允许使用击杀键，击杀被取消", "CheckMurder");
            return false;
        }

        if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            SoloKombatManager.OnPlayerAttack(killer, target);
            return false;
        }
        /*if (Options.CurrentGameMode == CustomGameMode.ModeArrest)
        {
            ModeArrestManager.OnPlayerAttack(killer, target);
            return false;
        }*/

        //実際のキラーとkillerが違う場合の入れ替え処理
        if (Sniper.IsEnable) Sniper.TryGetSniper(target.PlayerId, ref killer);
        if (killer != __instance) Logger.Info($"Real Killer={killer.GetNameWithRole()}", "CheckMurder");

        //鹈鹕肚子里的人无法击杀
        if (Pelican.IsEaten(target.PlayerId))
            return false;
        if (killer.Is(CustomRoles.Coldpotato)) return false;
        //refuser change role
        var change = IRandom.Instance;
        var refusekind = change.Next(1, 5);
        if (killer.Is(CustomRoles.Refuser))
        {
            killer.RpcGuardAndKill(target);
            if (refusekind == 1)
            {
                foreach (var ThisPlayer in Main.AllAlivePlayerControls)
                {
                    if (ThisPlayer.Is(CustomRoles.Refuser))
                    {
                        ThisPlayer.RpcSetCustomRole(CustomRoles.AnimalRefuser);
                        return false;
                    }
                }
            }
            if (refusekind == 2)
            {
                foreach (var ThisPlayer in Main.AllAlivePlayerControls)
                {
                    if (ThisPlayer.Is(CustomRoles.Refuser))
                    {
                        ThisPlayer.RpcSetCustomRole(CustomRoles.UnanimalRefuser);
                        return false;
                    }
                }
            }
            if (refusekind == 3)
            {
                foreach (var ThisPlayer in Main.AllAlivePlayerControls)
                {
                    if (ThisPlayer.Is(CustomRoles.Refuser))
                    {
                        ThisPlayer.RpcSetCustomRole(CustomRoles.AttendRefuser);
                        return false;
                    }
                }
            }
            if (refusekind == 4)
            {
                foreach (var ThisPlayer in Main.AllAlivePlayerControls)
                {
                    if (ThisPlayer.Is(CustomRoles.Refuser))
                    {
                        ThisPlayer.RpcSetCustomRole(CustomRoles.CrazyRefuser);
                        return false;
                    }
                }
            }
            if (refusekind == 5)
            {
                foreach (var ThisPlayer in Main.AllAlivePlayerControls)
                {
                    if (ThisPlayer.Is(CustomRoles.Refuser))
                    {
                        ThisPlayer.RpcSetCustomRole(CustomRoles.ZeyanRefuser);
                        return false;
                    }
                }
            }
        }


        //阻止对活死人的操作
        if (target.Is(CustomRoles.Glitch))
            return false;

        // 赝品检查
        if (Counterfeiter.OnClientMurder(killer)) return false;

        //磁铁人干扰
        if (Main.ForMagnetMan.Contains(killer.PlayerId)) return false;

        //判定凶手技能
        if (killer.PlayerId != target.PlayerId)
        {
            //非自杀场景下才会触发
            switch (killer.GetCustomRole())
            {
                //==========内鬼阵营==========//
                case CustomRoles.BountyHunter: //必须在击杀发生前处理
                    BountyHunter.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.OnCheckMurder(killer);
                    break;
                case CustomRoles.Vampire:
                    if (!Vampire.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Warlock:
                    if (!Main.CheckShapeshift[killer.PlayerId] && !Main.isCurseAndKill[killer.PlayerId])
                    { //Warlockが変身時以外にキルしたら、呪われる処理
                        if (target.Is(CustomRoles.Needy))
                        {
                            killer.RpcGuardAndKill(target);
                            return false;
                        }
                        Main.isCursed = true;
                        killer.SetKillCooldownV2();
                        killer.RPCPlayCustomSound("Line");
                        Main.CursedPlayers[killer.PlayerId] = target;
                        Main.WarlockTimer.Add(killer.PlayerId, 0f);
                        Main.isCurseAndKill[killer.PlayerId] = true;
                        RPC.RpcSyncCurseAndKill();
                        return false;
                    }
                    if (Main.CheckShapeshift[killer.PlayerId])
                    {//呪われてる人がいないくて変身してるときに通常キルになる
                        killer.RpcCheckAndMurder(target);
                        return false;
                    }
                    return false;
                case CustomRoles.Witch:
                    if (!Witch.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Puppeteer:
                    if (target.Is(CustomRoles.Needy)) return false;
                    Main.PuppeteerList[target.PlayerId] = killer.PlayerId;
                    RPC.RpcSyncPuppeteerList();
                    killer.SetKillCooldownV2();
                    killer.RPCPlayCustomSound("Line");
                    Utils.NotifyRoles(SpecifySeer: killer);
                    return false;
                case CustomRoles.Capitalism:
                    if (!Main.CapitalismAddTask.ContainsKey(target.PlayerId))
                        Main.CapitalismAddTask.Add(target.PlayerId, 0);
                    Main.CapitalismAddTask[target.PlayerId]++;
                    if (!Main.CapitalismAssignTask.ContainsKey(target.PlayerId))
                        Main.CapitalismAssignTask.Add(target.PlayerId, 0);
                    Main.CapitalismAssignTask[target.PlayerId]++;
                    Logger.Info($"资本主义 {killer.GetRealName()} 又开始祸害人了：{target.GetRealName()}", "Capitalism Add Task");
                    killer.RpcGuardAndKill(killer);
                    killer.SetKillCooldown();
                    return false;
                case CustomRoles.Mare:
                    if (Mare.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Gangster:
                    if (Gangster.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.BallLightning:
                    if (BallLightning.CheckBallLightningMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Greedier:
                    Greedier.OnCheckMurder(killer);
                    break;
                case CustomRoles.QuickShooter:
                    QuickShooter.QuickShooterKill(killer);
                    break;
                case CustomRoles.Sans:
                    Sans.OnCheckMurder(killer);
                    break;
                case CustomRoles.MimicKiller:
                    Mimics.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.ShapeShifters:
                    ShapeShifters.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.Hangman:
                    if (!Hangman.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Swooper:
                    if (!Swooper.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Vandalism:
                    Vandalism.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.Corpse:
                    Corpse.OnCheckMurder(target);
                    break;
                case CustomRoles.DoubleKiller:
                    DoubleKiller.OnCheckMurder(killer);
                    break;
                case CustomRoles.EvilGambler:
                    EvilGambler.OnCheckMurder(killer);
                    break;
                //       case CustomRoles.Kidnapper:
                //         if (Kidnapper.CheckKidnapperMurder(killer, target))
                //          return false;
                //     break;
                //     

                //==========中立阵营==========//
                case CustomRoles.Loners:
                    Loners.OnCheckMurder(killer);
                    break;
                case CustomRoles.Arsonist:
                    killer.SetKillCooldown(Options.ArsonistDouseTime.GetFloat());
                    if (!Main.isDoused[(killer.PlayerId, target.PlayerId)] && !Main.ArsonistTimer.ContainsKey(killer.PlayerId))
                    {
                        Main.ArsonistTimer.Add(killer.PlayerId, (target, 0f));
                        Utils.NotifyRoles(SpecifySeer: __instance);
                        RPC.SetCurrentDousingTarget(killer.PlayerId, target.PlayerId);
                    }
                    return false;
                case CustomRoles.Revolutionist:
                    killer.SetKillCooldown(Options.RevolutionistDrawTime.GetFloat());
                    if (!Main.isDraw[(killer.PlayerId, target.PlayerId)] && !Main.RevolutionistTimer.ContainsKey(killer.PlayerId))
                    {
                        Main.RevolutionistTimer.TryAdd(killer.PlayerId, (target, 0f));
                        Utils.NotifyRoles(SpecifySeer: __instance);
                        RPC.SetCurrentDrawTarget(killer.PlayerId, target.PlayerId);
                    }
                    return false;
                case CustomRoles.Innocent:
                    target.RpcMurderPlayerV3(killer);
                    return false;
                case CustomRoles.Pelican:
                    if (Pelican.CanEat(killer, target.PlayerId))
                    {
                        Utils.TP(killer.NetTransform, target.GetTruePosition());
                        Pelican.EatPlayer(killer, target);
                        killer.SetKillCooldownV2();
                        killer.RPCPlayCustomSound("Eat");
                        target.RPCPlayCustomSound("Eat");
                    }
                    return false;
                case CustomRoles.FFF:
                    if (!target.Is(CustomRoles.Lovers) && !target.Is(CustomRoles.Ntr) && !target.Is(CustomRoles.CrushLovers))
                    {
                        killer.Data.IsDead = true;
                        Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                        killer.RpcMurderPlayerV3(killer);
                        Main.PlayerStates[killer.PlayerId].SetDead();
                        Logger.Info($"{killer.GetRealName()} 击杀了非目标玩家，壮烈牺牲了（bushi）", "FFF");
                        return false;
                    }
                    break;
                case CustomRoles.Yandere:
                    if (!Main.NeedKillYandere.Contains(target.PlayerId))
                    {
                        killer.Data.IsDead = true;
                        Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                        killer.RpcMurderPlayerV3(killer);
                        Main.PlayerStates[killer.PlayerId].SetDead();
                        Logger.Info($"{killer.GetRealName()} 击杀了非目标玩家，壮烈牺牲了（bushi）", "y");
                        return false;
                    }
                    break;
                case CustomRoles.Gamer:
                    Gamer.CheckGamerMurder(killer, target);
                    return false;
                case CustomRoles.DarkHide:
                    DarkHide.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.Provocateur:
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
                    killer.RpcMurderPlayerV3(target);
                    killer.RpcMurderPlayerV3(killer);
                    killer.SetRealKiller(target);
                    Main.Provoked.TryAdd(killer.PlayerId, target.PlayerId);
                    return false;
                case CustomRoles.Totocalcio:
                    Totocalcio.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Succubus:
                    Succubus.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Jackal:
                    if (Jackal.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.RewardOfficer:
                    if (RewardOfficer.OnCheckMurder(killer, target))
                        return false;
                    break;

                case CustomRoles.Amnesiac:
                    foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
                    {
                        var pos = target.transform.position;
                        var dis = Vector2.Distance(pos, pc.transform.position);
                        killer.SetKillCooldownV2(target: target, forceAnime: true);
                        return false;
                    }
                    return false;
                case CustomRoles.Shifter:
                    foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
                    {
                        var pos = target.transform.position;
                        var dis = Vector2.Distance(pos, pc.transform.position);
                        killer.SetKillCooldownV2(target: target, forceAnime: true);
                        Shifter.OnCheckMurder(killer, target);
                        return false;
                    }

                    break;
                case CustomRoles.Solicited:
                    if (!killer.Is(CustomRoles.Captain))
                    {
                        return true;
                    }
                    break;
                case CustomRoles.SchrodingerCat:
                    if (killer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.noteam == false)
                    {
                        return true;
                    }
                    break;
                case CustomRoles.Exorcist:

                    if (!Main.ForExorcist.Contains(target.PlayerId))
                    {
                        NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Exorcist), GetString("NotExorcist")));
                        return true;
                    }
                    else
                    {
                        Main.ForExorcist.Remove(target.PlayerId);
                        killer.SetKillCooldownV2(target: target, forceAnime: true);
                        Main.ExorcistMax[killer.PlayerId]++;
                        if (Main.ExorcistMax[killer.PlayerId] >= Options.MaxExorcist.GetInt())
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Exorcist);
                            CustomWinnerHolder.WinnerIds.Add(killer.PlayerId);
                        }
                        return true;
                    }

                case CustomRoles.YinLang:
                    //银狼升级
                    if (killer.Is(CustomRoles.YinLang))
                    {
                        if (YLdj >= 1 && YLdj <= 5)
                        {
                            if (killer.PlayerId != target.PlayerId)
                            {
                                foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
                                {
                                    var pos = target.transform.position;
                                    var dis = Vector2.Distance(pos, pc.transform.position);
                                    killer.SetKillCooldownV2(target: target, forceAnime: true);
                                    Logger.Info("银狼击杀开始", "YL");
                                    NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang1")));
                                    Logger.Info($"{target.GetNameWithRole()} |系统警告| => {target.GetNameWithRole()}", "YinLang");
                                    var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
                                    Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
                                    ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
                                    target.MarkDirtySettings();
                                    new LateTask(() =>
                                    {
                                        Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
                                        ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
                                        target.MarkDirtySettings();
                                        RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
                                    }, 10, "Trapper BlockMove");
                                    YLLevel += 1;
                                    YLCS = YinLang.YLSJ.GetInt();
                                    if (YLLevel == YinLang.YLSJ.GetInt())
                                    {
                                        YLdj += 1;
                                        YLLevel = 0;
                                    }
                                    killer.Notify(string.Format(GetString("YinLangLevel"), YLdj, YLLevel, YLCS - YLLevel));
                                    return false;
                                }
                            }
                        }
                        else if (YLdj >= 6 && YLdj <= 10)
                        {
                            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
                            {
                                var pos = target.transform.position;
                                var dis = Vector2.Distance(pos, pc.transform.position);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                Logger.Info("银狼击杀开始", "YL");
                                NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang2")));
                                Logger.Info($"{target.GetNameWithRole()} |是否允许更改| => {target.GetNameWithRole()}", "YinLang");
                                Main.AllPlayerSpeed[target.PlayerId] = 0.01f;
                                var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
                                Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
                                ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
                                target.MarkDirtySettings();
                                //    NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang1")));
                                Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed - 0.01f;
                                ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
                                target.MarkDirtySettings();
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
                                Main.AllPlayerSpeed[target.PlayerId] = 0.01f;
                                YLLevel += 1;
                                YLCS = YinLang.YLSJ.GetInt();
                                if (YLLevel == YinLang.YLSJ.GetInt())
                                {
                                    YLdj += 1;
                                    YLLevel = 0;
                                }
                                killer.Notify(string.Format(GetString("YinLangLevel"), YLdj, YLLevel, YLCS - YLLevel));
                                return false;
                            }
                        }
                        else
                        {
                            Utils.TP(killer.NetTransform, target.GetTruePosition());
                            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                            Utils.TP(target.NetTransform, Pelican.GetBlackRoomPS());
                            target.SetRealKiller(killer);
                            Main.PlayerStates[target.PlayerId].SetDead();
                            target.RpcMurderPlayerV3(target);
                            killer.SetKillCooldownV2();
                            NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang3")));
                            killer.Notify(GetString("YinLangNotMax"));
                            return false;
                        }
                    }
                    return false;
                case CustomRoles.Henry:
                    if (!Henry.OnCheckMurder(killer))
                        return false;
                    break;
                case CustomRoles.Challenger:
                    killer.ResetKillCooldown();
                    killer.SetKillCooldown();
                    killer.RpcGuardAndKill(target);
                    Challenger.ForChallengerTwo.Add(target.PlayerId);
                    Challenger.ForChallengerTwo.Add(killer.PlayerId);
                    Challenger.Challengerbacktrack.Add(killer.PlayerId, killer.GetTruePosition());
                    Challenger.Challengerbacktrack.Add(target.PlayerId, target.GetTruePosition());
                    new LateTask(() =>
                    {
                        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != killer.PlayerId).ToList();
                        var Fr = pcList[IRandom.Instance.Next(0, pcList.Count)];
                        Fr?.ReportDeadBody(null);
                        foreach (var player in Main.AllAlivePlayerControls)
                        {
                            if (player == target || player == killer) continue;
                            Challenger.ForChallenger.Add(player.PlayerId);
                            Challenger.Challengerbacktrack.Add(player.PlayerId, player.GetTruePosition());
                            player.ShowPopUp(GetString("ChallengerReadyMsg"));
                        }
                    }, Challenger.CooldwonMax.GetFloat(), "Trapper BlockMove");

                    Main.Provoked.TryAdd(killer.PlayerId, target.PlayerId);
                    return false;
                case CustomRoles.Hotpotato:
                    Holdpotato.OnCheckMurder(killer, target);
                    return false;
                //==========船员职业==========//
                case CustomRoles.Sheriff:
                    if (!Sheriff.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.SwordsMan:
                    if (!SwordsMan.OnCheckMurder(killer))
                        return false;
                    break;
                case CustomRoles.Medic:
                    Medic.OnCheckMurderFormedicaler(killer, target);
                    return false;
                case CustomRoles.Captain:
                    Captain.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Counterfeiter:
                    if (Counterfeiter.CanBeClient(target) && Counterfeiter.CanSeel(killer.PlayerId))
                        Counterfeiter.SeelToClient(killer, target);
                    return false;

                case CustomRoles.Scout:
                    Scout.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Deputy:
                    Deputy.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Prophet:
                    Prophet.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Prosecutors:
                    Prosecutors.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.ET:
                    ET.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.ElectOfficials:
                    ElectOfficials.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.BSR:
                    BSR.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.SpeedUp:
                    SpeedUp.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.ChiefOfPolice:
                    ChiefOfPolice.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Knight:
                    Knight.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Merchant:
                    Merchant.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.NiceTracker:
                    NiceTracker.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.DemonHunterm:
                    if (DemonHunterm.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Guardian:
                    killer.RpcProtectPlayer(target, killer.PlayerId);
                    return false;
            }
        }

        // 击杀前检查
        if (!killer.RpcCheckAndMurder(target, true))
            return false;

        // 清道夫清理尸体
        if (killer.Is(CustomRoles.Scavenger))
        {
            Utils.TP(killer.NetTransform, target.GetTruePosition());
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
            target.SetRealKiller(killer);
            Main.PlayerStates[target.PlayerId].SetDead();
            target.Exiled();
            var hudManager = DestroyableSingleton<HudManager>.Instance;
            SoundManager.Instance.PlaySound(target.KillSfx, false, 0.8f);
            hudManager.KillOverlay.ShowKillAnimation(killer.Data, target.Data);
            killer.SetKillCooldownV2();
            NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledByScavenger")));
            return false;
        }

        // 肢解者肢解受害者
        if (killer.Is(CustomRoles.OverKiller) && killer.PlayerId != target.PlayerId)
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Dismembered;
            new LateTask(() =>
            {
                if (!Main.OverDeadPlayerList.Contains(target.PlayerId)) Main.OverDeadPlayerList.Add(target.PlayerId);
                var ops = target.GetTruePosition();
                var rd = IRandom.Instance;
                for (int i = 0; i < 20; i++)
                {
                    Vector2 location = new(ops.x + ((float)(rd.Next(0, 201) - 100) / 100), ops.y + ((float)(rd.Next(0, 201) - 100) / 100));
                    location += new Vector2(0, 0.3636f);

                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.None, -1);
                    NetHelpers.WriteVector2(location, writer);
                    writer.Write(target.NetTransform.lastSequenceId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);

                    target.NetTransform.SnapTo(location);
                    killer.MurderPlayer(target,MurderResultFlags.DecisionByHost);

                    if (target.Is(CustomRoles.Avanger))
                    {
                        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId).ToList();
                        var rp = pcList[IRandom.Instance.Next(0, pcList.Count)];
                        Main.PlayerStates[rp.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
                        rp.SetRealKiller(target);
                        rp.RpcMurderPlayerV3(rp);
                    }

                    MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
                    messageWriter.WriteNetObject(target);
                    AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
                }
                Utils.TP(killer.NetTransform, ops);
            }, 0.05f, "OverKiller Murder");
        }
        //抑郁者赌命
        if (killer.Is(CustomRoles.Depressed))
        {
            if (killer.Is(CustomRoles.OldThousand))
            {
                killer.SetKillCooldownV2();
            }
            var rd = IRandom.Instance;
            if (rd.Next(0, 100) < Options.DepressedIdioctoniaProbability.GetInt())
            {
                Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Depression;
                killer.RpcMurderPlayerV3(killer);
                return false;
            }
        }
        //毁尸者毁尸
        if (killer.Is(CustomRoles.Destroyers))
        {
            var Dy = IRandom.Instance;
            int rndNum = Dy.Next(0, 100);
            if (rndNum >= 10 && rndNum < 20)
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
            }
            if (rndNum >= 20 && rndNum < 30)
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
            }
            if (rndNum >= 30 && rndNum < 40)
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
            }
            if (rndNum >= 40 && rndNum < 50)
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.FollowingSuicide;
            }
            if (rndNum >= 50 && rndNum < 60)
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Torched;
            }
            if (rndNum >= 60 && rndNum < 70)
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
            }
            if (rndNum >= 70 && rndNum < 80)
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Quantization;
            }
            if (rndNum >= 80 && rndNum < 90)
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
            }
            if (rndNum >= 90 && rndNum < 100)
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Trialed;
            }
        }
        //倒霉蛋倒霉
        if (killer.Is(CustomRoles.UnluckyEggs))
        {
            var Ue = IRandom.Instance;
            if (Ue.Next(0, 100) < Options.UnluckyEggsKIllUnluckyEggs.GetInt())
            {
                killer.RpcMurderPlayerV3(killer);
                return false;
            }
        }
        //自爆兵自爆
        if (killer.Is(CustomRoles.Bomber))
        {
            Logger.Info("炸弹爆炸了", "Boom");
            CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
            foreach (var player in Main.AllPlayerControls)
            {

                if (!player.IsModClient()) player.KillFlash();
                if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                if (Vector2.Distance(killer.transform.position, player.transform.position) <= Options.BomberRadius.GetFloat())
                {
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    player.SetRealKiller(killer);
                    player.RpcMurderPlayerV3(player);
                    Medic.IsDead(player);
                }
            }
        }
        //修道徒升级
        if (killer.Is(CustomRoles.Cultivator))
        {
            if (Main.CultivatorKillMax[killer.PlayerId] <= Options.CultivatorMax.GetInt())
            {
                Main.CultivatorKillMax[killer.PlayerId]++;
            }
            else
            {
                killer.Notify(GetString("CultivatorNotMax"));
            }
            if (Main.CultivatorKillMax[killer.PlayerId] == 1 && Options.CultivatorOneCanKillCooldown.GetBool())
            {
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.CultivatorOneKillCooldown.GetFloat();
            }
            if (Main.CultivatorKillMax[killer.PlayerId] == 2 && Options.CultivatorTwoCanScavenger.GetBool())
            {
                Utils.TP(killer.NetTransform, target.GetTruePosition());
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                Utils.TP(target.NetTransform, Pelican.GetBlackRoomPS());
                target.SetRealKiller(killer);
                Main.PlayerStates[target.PlayerId].SetDead();
                target.RpcMurderPlayerV3(target);
                killer.SetKillCooldownV2();
                NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultivator), GetString("KilledByCultivator")));
                return false;
            }
            if (Main.CultivatorKillMax[killer.PlayerId] == 3 && Options.CultivatorThreeCanBomber.GetBool())
            {
                Logger.Info("炸弹爆炸了", "Boom");
                CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                foreach (var player in Main.AllPlayerControls)
                {
                    if (!player.IsModClient()) player.KillFlash();
                    if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                    if (player == killer) continue;
                    if (Vector2.Distance(killer.transform.position, player.transform.position) <= Options.BomberRadius.GetFloat())
                    {
                        Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                        player.SetRealKiller(killer);
                        player.RpcMurderPlayerV3(player);
                        Medic.IsDead(player);
                    }
                }
            }
            if (Main.CultivatorKillMax[killer.PlayerId] == 4 && Options.CultivatorFourCanFlash.GetBool())
            {
                Main.AllPlayerSpeed[killer.PlayerId] = Options.CultivatorSpeed.GetFloat();
            }
        }
        //紊乱者击杀
        if (killer.Is(CustomRoles.Disorder))
        {
            var Dd = IRandom.Instance;
            if (Dd.Next(0, 100) < Options.Disorderility.GetInt())
            {
                var Ie = IRandom.Instance;
                int Kl = Ie.Next(0, 100);
                if (killer.Is(CustomRoles.OldThousand))
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DisorderKillCooldown.GetFloat();
                }
                if (Kl >= 10 && Kl < 20)
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = Options.CultivatorKillCooldown.GetFloat();
                }
                if (Kl >= 20 && Kl < 30)
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = EvilGambler.EvilGamblerKillCooldown.GetFloat();
                }
                if (Kl >= 30 && Kl < 40)
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DepressedKillCooldown.GetFloat();
                }
                if (Kl >= 40 && Kl < 50)
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = Options.CleanerKillCooldown.GetFloat();
                }
                if (Kl >= 50 && Kl < 60)
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = Options.BomberKillCooldown.GetFloat();
                }
                if (Kl >= 60 && Kl < 70)
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = Options.CapitalismSkillCooldown.GetFloat();
                }
                if (Kl >= 70 && Kl < 80)
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = Options.ScavengerKillCooldown.GetFloat();
                }
                if (Kl >= 80 && Kl < 90)
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = Options.MNKillCooldown.GetFloat();
                }
                if (Kl >= 90 && Kl < 100)
                {
                    Main.AllPlayerKillCooldown[killer.PlayerId] = Options.RevolutionistCooldown.GetFloat();
                }
            }
        }
        //行刑师击杀
        if (killer.Is(CustomRoles.OldImpostor))
        {
            target.RpcMurderPlayerV3(target);
            target.SetRealKiller(killer);
            killer.SetKillCooldownV2();
            return false;
        }
        //执行者召开会议
        if (killer.Is(CustomRoles.Executor))
        {
            target?.ReportDeadBody(null);
        }
        //俄罗斯大转盘
        if (killer.Is(CustomRoles.Followers))
        {
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
            var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != killer.PlayerId).ToList();
            var Fr = pcList[IRandom.Instance.Next(0, pcList.Count)];
            Main.PlayerStates[Fr.PlayerId].deathReason = PlayerState.DeathReason.Execution;
            Fr.SetRealKiller(killer);
            Fr.RpcMurderPlayerV3(Fr);
            if (Fr.GetCustomRole().IsImpostor())
            {
                killer.RpcMurderPlayerV3(killer);
                return false;
            }
        }
        //医生护盾检查
        if (Medic.OnCheckMurder(killer, target))
            return false;

        if (target.Is(CustomRoles.Medic))
            Medic.IsDead(target);
        //爆破狂技能
        if (killer.Is(CustomRoles.DemolitionManiac))
        {
            if (Options.DemolitionManiacKillPlayerr.GetInt() == 0)
            {
                Main.DemolitionManiacKill.Add(target.PlayerId);
                killer.SetKillCooldownV2(target: target, forceAnime: true);
                return false;
            }
            if (Options.DemolitionManiacKillPlayerr.GetInt() == 1)
            {
                killer.SetKillCooldownV2(target: target, forceAnime: true);
                Main.InBoom.Remove(killer.PlayerId);
                Main.InBoom.Add(killer.PlayerId, Utils.GetTimeStamp());
                Main.ForDemolition.Add(target.PlayerId);
                new LateTask(() =>
                {
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);

                    CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                    foreach (var player in Main.AllPlayerControls)
                    {


                    }
                }, Options.DemolitionManiacWait.GetFloat(), "DemolitionManiacBoom!!!");
                return false;
            }
        }
        if (target.Is(CustomRoles.NiceMini) && Mini.Age != 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("Cantkillkid")));
            return false;
        }
        if (target.Is(CustomRoles.EvilMini) && Mini.Age != 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("Cantkillkid")));
            return false;
        }
        if (killer.Is(CustomRoles.EvilMini) && Mini.Age != 18)
        {
            Main.EvilMiniKillcooldown[killer.PlayerId] = Mini.MinorCD.GetFloat();
            Main.AllPlayerKillCooldown[killer.PlayerId] = Mini.MinorCD.GetFloat();
            Main.EvilMiniKillcooldownf = Mini.MinorCD.GetFloat();
            killer.MarkDirtySettings();
            killer.SetKillCooldown();
            return true;
        }
        if (killer.Is(CustomRoles.EvilMini) && Mini.Age == 18)
        {
            Main.AllPlayerKillCooldown[killer.PlayerId] = Mini.MajorCD.GetFloat();
            killer.MarkDirtySettings();
            killer.SetKillCooldown();
            return true;
        }
        //猎人
        if (killer.Is(CustomRoles.Hunter))
        {
            Main.HunterMax[killer.PlayerId]++;
            if ((Main.HunterMax[killer.PlayerId] <= Options.HunterCanTargetMax.GetInt() || Main.HunterMax[killer.PlayerId] <= Options.HunterCanTargetMaxEveryMeeting.GetInt()) && !Main.HunterTarget.Contains(target))
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                Main.HunterTarget.Add(target);
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#f7eea9");
                return false;
            }
            else if (Main.HunterTarget.Contains(target))
            {
                Main.HunterMax[killer.PlayerId]--;
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Hunter), GetString("InList")));
                return false;
            }
            else if (Main.HunterMax[killer.PlayerId] > Options.HunterCanTargetMax.GetInt() || Main.HunterMax[killer.PlayerId] > Options.HunterCanTargetMaxEveryMeeting.GetInt())
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Hunter), GetString("HunterTargetMax")));
                return false;
            }
        }
        //暗恋者暗恋

        if (killer.Is(CustomRoles.Crush))
        {
            Main.CrushMax[killer.PlayerId]++;
            if (Main.CrushMax[killer.PlayerId] == 1 && !target.Is(CustomRoles.Captain) && !target.Is(CustomRoles.Akujo) && !target.Is(CustomRoles.Honmei) && !target.Is(CustomRoles.Backup) && !target.Is(CustomRoles.Believer) && !target.Is(CustomRoles.Lovers) && !target.Is(CustomRoles.CupidLovers) && !target.Is(CustomRoles.Cupid) && !target.Is(CustomRoles.Ntr) && !target.Is(CustomRoles.God))
            {
                Main.CrushLoversPlayers.Add(killer);
                Main.PlayerStates[killer.PlayerId].SetSubRole(CustomRoles.CrushLovers);
                Main.CrushLoversPlayers.Add(target);
                Main.PlayerStates[target.PlayerId].SetSubRole(CustomRoles.CrushLovers);
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                RPC.SyncCrushLoversPlayers();
                return false;
            }
            else if (target.Is(CustomRoles.Captain) && target.Is(CustomRoles.Believer) && target.Is(CustomRoles.Akujo) && target.Is(CustomRoles.Honmei) && target.Is(CustomRoles.Backup) && target.Is(CustomRoles.Lovers) && target.Is(CustomRoles.CupidLovers) && target.Is(CustomRoles.Cupid) && target.Is(CustomRoles.Ntr) && target.Is(CustomRoles.God))
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Crush), GetString("CrushInvalidTarget")));
                Main.CrushMax[killer.PlayerId]--;
                return false;
            }
            else
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Crush), GetString("CrushInvalidTarget")));
                Main.CrushMax[killer.PlayerId]--;
                return false;
            }
        }
        if (killer.Is(CustomRoles.PlagueDoctor))
        {
            PlagueDoctor.CanInfectInt[killer.PlayerId]++;
            if (PlagueDoctor.CanInfectInt[killer.PlayerId] <= PlagueDoctor.InfectTimes.GetInt() && !PlagueDoctor.InfectList.Contains(target.PlayerId))
            {
                PlagueDoctor.InfectList.Add(target.PlayerId);
                PlagueDoctor.InfectNum += 1;
                PlagueDoctor.SendRPC(killer.PlayerId);
                PlagueDoctor.InfectInt[target.PlayerId] = 100f;
                Logger.Info($"成功感染", "pdd");
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                return false;
            }
            else if (PlagueDoctor.InfectList.Contains(target.PlayerId))
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.PlagueDoctor), GetString("Coutains")));
                PlagueDoctor.CanInfectInt[killer.PlayerId]--;
                PlagueDoctor.SendRPC(killer.PlayerId);
                return false;
            }
            else if (PlagueDoctor.CanInfectInt[killer.PlayerId] > PlagueDoctor.InfectTimes.GetInt())
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.PlagueDoctor), GetString("SkillMax")));
                PlagueDoctor.CanInfectInt[killer.PlayerId]--;
                PlagueDoctor.SendRPC(killer.PlayerId);
                return false;
            }
        }
        if (target.Is(CustomRoles.PlagueDoctor) && PlagueDoctor.Infectmurder.GetBool())
        {
            new LateTask(() =>
            {

                if (!target.IsAlive() && !PlagueDoctor.InfectList.Contains(killer.PlayerId))
                {
                    PlagueDoctor.InfectList.Add(killer.PlayerId);
                    PlagueDoctor.InfectNum += 1;
                    Logger.Info($"成功感染", "pdd");
                    PlagueDoctor.InfectInt[killer.PlayerId] = 100f;
                }

            }, 0.1f);
            return true;
        }

        //丘比特之箭！
        if (killer.Is(CustomRoles.Cupid))
        {
            Main.CupidMax[killer.PlayerId]++;
            if (Main.CupidMax[killer.PlayerId] == 1 && !target.Is(CustomRoles.Captain) && !target.Is(CustomRoles.Akujo) && !target.Is(CustomRoles.Honmei) && !target.Is(CustomRoles.Backup) && !target.Is(CustomRoles.Believer) && !target.Is(CustomRoles.Lovers) && !target.Is(CustomRoles.Crush) && !target.Is(CustomRoles.CrushLovers) && !target.Is(CustomRoles.Ntr) && !target.Is(CustomRoles.God))
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                Main.CupidLoveList.Add(target);
                Main.CupidLoversPlayers.Add(target);
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#ff80c0");
                return false;
            }
            else if (Main.CupidMax[killer.PlayerId] == 2 && !target.Is(CustomRoles.Captain) && !target.Is(CustomRoles.Akujo) && !target.Is(CustomRoles.Honmei) && !target.Is(CustomRoles.Backup) && !target.Is(CustomRoles.Believer) && !target.Is(CustomRoles.Lovers) && !target.Is(CustomRoles.CrushLovers) && !target.Is(CustomRoles.Crush) && !target.Is(CustomRoles.Ntr) && !target.Is(CustomRoles.God))
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                Main.CupidLoveList.Add(target);
                Main.CupidLoversPlayers.Add(target);
                Main.CupidComplete = true;
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#ff80c0");
                foreach (var cupidplayer in Main.CupidLoveList)
                {
                    Main.PlayerStates[cupidplayer.PlayerId].SetSubRole(CustomRoles.CupidLovers);
                }
                RPC.SyncCupidLoversPlayers();
                return false;
            }
            else if (Main.CupidMax[killer.PlayerId] == 3 && target.Is(CustomRoles.CupidLovers) && Options.CupidShield.GetBool())
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                Main.CupidShieldList.Add(target);
                return false;
            }
            else if (target.Is(CustomRoles.Captain) && target.Is(CustomRoles.Believer) && target.Is(CustomRoles.Akujo) && target.Is(CustomRoles.Honmei) && target.Is(CustomRoles.Backup) && target.Is(CustomRoles.Lovers) && target.Is(CustomRoles.CrushLovers) && target.Is(CustomRoles.Crush) && target.Is(CustomRoles.Ntr) && target.Is(CustomRoles.God))
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cupid), GetString("CupidInvalidTarget")));
                Main.CupidMax[killer.PlayerId]--;
                return false;
            }
            else if (Main.CupidMax[killer.PlayerId] > 2 && !Options.CupidShield.GetBool() || Main.CupidMax[killer.PlayerId] > 3 && Options.CupidShield.GetBool())
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cupid), GetString("CupidInvalidTarget")));
                Main.CupidMax[killer.PlayerId]--;
                return false;
            }
            else
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cupid), GetString("CupidInvalidTarget")));
                Main.CupidMax[killer.PlayerId]--;
                return false;
            }
        }
        
        //魅魔
        if (killer.Is(CustomRoles.Akujo))
        {
            Main.AkujoMax[killer.PlayerId]++;
            if (Main.AkujoMax[killer.PlayerId] == 1 && !target.Is(CustomRoles.Captain) && !target.Is(CustomRoles.Believer) && !target.Is(CustomRoles.Lovers) && !target.Is(CustomRoles.Crush) && !target.Is(CustomRoles.CrushLovers) && !target.Is(CustomRoles.Ntr) && !target.Is(CustomRoles.God))
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                Main.AkujoLoversPlayers.Add(target);
                Main.AkujoLoversPlayers.Add(killer);
                Main.PlayerStates[target.PlayerId].SetSubRole(CustomRoles.Honmei);
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#8E4593");
                NameColorManager.Add(target.PlayerId, killer.PlayerId, "#8E4593");
                RPC.SyncAkujoLoversPlayers();
                return false;
            }
            else if (Main.AkujoMax[killer.PlayerId] > 1 && (Main.AkujoMax[killer.PlayerId] <= Options.AkujoLimit.GetInt()) && !target.Is(CustomRoles.Captain) && !target.Is(CustomRoles.Believer) && !target.Is(CustomRoles.Honmei) && !target.Is(CustomRoles.Backup) && !target.Is(CustomRoles.Lovers) && !target.Is(CustomRoles.CrushLovers) && !target.Is(CustomRoles.Crush) && !target.Is(CustomRoles.Ntr) && !target.Is(CustomRoles.God))
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                Main.PlayerStates[target.PlayerId].SetSubRole(CustomRoles.Backup);
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#8E4593");
                NameColorManager.Add(target.PlayerId, killer.PlayerId, "#8E4593");

                return false;
            }

            else if (target.Is(CustomRoles.Captain) && target.Is(CustomRoles.Believer) && target.Is(CustomRoles.Honmei) && target.Is(CustomRoles.Backup) && target.Is(CustomRoles.Lovers) && target.Is(CustomRoles.CrushLovers) && target.Is(CustomRoles.Crush) && target.Is(CustomRoles.Ntr) && target.Is(CustomRoles.God))
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Akujo), GetString("AkujoInvalidTarget")));
                Main.AkujoMax[killer.PlayerId]--;
                return false;
            }
            else if (Main.AkujoMax[killer.PlayerId] > (Options.AkujoLimit.GetInt() + 1))
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Akujo), GetString("AkujoInvalidTarget")));
                Main.AkujoMax[killer.PlayerId]--;
                return false;
            }
            else
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Akujo), GetString("AkujoInvalidTarget")));
                Main.AkujoMax[killer.PlayerId]--;
                return false;
            }
        }

        //奴隶主奴隶
        if (killer.Is(CustomRoles.Slaveowner))
        {
            if (target.GetCustomRole().IsCrewmate())
            {
                if (Main.SlaveownerMax[killer.PlayerId] >= Options.ForSlaveownerSlav.GetInt())
                {
                    return false;
                }
                if (Options.TargetcanSeeSlaveowner.GetBool())
                {
                    NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Slaveowner), GetString("SlavBySlaveowner")));
                    Main.ForSlaveowner.Add(target.PlayerId);
                    Main.SlaveownerMax[killer.PlayerId]++;
                    killer.ResetKillCooldown();
                    killer.SetKillCooldown();
                    killer.RpcGuardAndKill(target);
                    CustomWinnerHolder.WinnerIds.Remove(target.PlayerId);
                    return false;
                }
                else
                {
                    Main.ForSlaveowner.Add(target.PlayerId);
                    Main.SlaveownerMax[killer.PlayerId]++;
                    killer.ResetKillCooldown();
                    killer.SetKillCooldown();
                    killer.RpcGuardAndKill(target);
                    CustomWinnerHolder.WinnerIds.Remove(target.PlayerId);
                    return false;
                }

            }
            else
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
            }
        }
        //咒术师选择替罪羊
        if (killer.Is(CustomRoles.Spellmaster))
        {
            if (Main.SpellmasterMax[killer.PlayerId] >= Options.SpellmasterKillMax.GetInt())
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
            }
            else
            {
                Main.ForSpellmaster.Add(target.PlayerId);
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                Main.SpellmasterMax[killer.PlayerId]++;
                return false;
            }
        }
        //三角恋卷入
        int Lvt = 0;
        if (killer.Is(CustomRoles.Lovers))
        {
            Lvt++;
            if (Options.LoverThree.GetBool() && Lvt == 1)
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                target.RpcSetCustomRole(CustomRoles.Lovers);
                return false;
            }
            else
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
            }
        }
        //嫉妒狂击杀
        if (killer.Is(CustomRoles.Jealousy))
        {
            if (!Main.ForJealousy.Contains(target.PlayerId))
            {
                killer.RpcMurderPlayerV3(killer);
                return false;
            }
            else
            {
                Main.JealousyMax[killer.PlayerId]++;
            }
            if (Main.JealousyMax[killer.PlayerId] >= Options.JealousyKillMax.GetInt())
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jealousy);
                CustomWinnerHolder.WinnerIds.Add(killer.PlayerId);
            }
        }
        //瘟疫之源感染
        if (killer.Is(CustomRoles.SourcePlague))
        {
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            NameColorManager.Add(killer.PlayerId, target.PlayerId, "#999999");
            //是否目标已经被感染了
            if (!Main.ForSourcePlague.Contains(target.PlayerId))
            {
                Main.ForSourcePlague.Add(target.PlayerId);
                bool AllPlayerForWY = true;
                //看看所有玩家有没有有人没被感染
                foreach (var player in Main.AllPlayerControls)
                {
                    if (player == killer) continue;
                    if (Main.ForSourcePlague.Contains(player.PlayerId))
                    {
                        NameColorManager.Add(killer.PlayerId, player.PlayerId, "#999999");
                    }
                    if (!Main.ForSourcePlague.Contains(player.PlayerId))
                    {
                        AllPlayerForWY = false;
                        continue;
                    }
                }
                if (AllPlayerForWY)
                {
                    killer.RpcSetCustomRole(CustomRoles.PlaguesGod);
                }
            }
            else
            {
                return false;
            }
            return false;
        }
        if (killer.Is(CustomRoles.King))
        {
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            NameColorManager.Add(killer.PlayerId, target.PlayerId, "#FFCC00");
            if (Main.KingCanKill.Contains(killer.PlayerId))
            {
                foreach (var player in Main.AllPlayerControls)
                {
                    if (Main.ForKing.Contains(player.PlayerId))
                    {
                        Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Execution;
                        player.SetRealKiller(killer);
                        player.RpcMurderPlayerV3(player);
                    }
                }
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.King);
                CustomWinnerHolder.WinnerIds.Add(killer.PlayerId);
            }
            if (!Main.ForKing.Contains(target.PlayerId))
            {
                Main.ForKing.Add(target.PlayerId);
                bool AllPlayerForKing = true;
                foreach (var player in Main.AllPlayerControls)
                {
                    if (player == killer) continue;
                    if (Main.ForKing.Contains(player.PlayerId))
                    {
                        NameColorManager.Add(killer.PlayerId, player.PlayerId, "#FFCC00");
                    }
                    if (!Main.ForKing.Contains(player.PlayerId))
                    {
                        AllPlayerForKing = false;
                        continue;
                    }
                }
                if (AllPlayerForKing)
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.King), GetString("EnterVentToWinandKillPlayerToWin")));
                    Main.KingCanpc.Add(killer.PlayerId);
                    Main.KingCanKill.Add(killer.PlayerId);
                }
            }
            else
            {
                return false;
            }
            return false;
        }
        //选择你的命运
        if (killer.Is(CustomRoles.DestinyChooser))
        {
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            var DC = IRandom.Instance;
            int Kl = DC.Next(0, 100);
            if (Kl >= 10 && Kl < 40)
            {
                NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledByIll")));
                Main.ForDestinyChooser.Add(target.PlayerId);
                return false;
            }
            if (Kl >= 40 && Kl < 60)
            {
                NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledByForTask")));
                Main.ForTasksDestinyChooser.Add(target.PlayerId);
                return false;
            }
            if (Kl >= 60 && Kl < 101)
            {
                NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledTarget")));
                Main.ForLostDeadDestinyChooser.Add(target.PlayerId);
                return false;
            }
            if (Main.ForDestinyChooser.Contains(target.PlayerId) || Main.ForTasksDestinyChooser.Contains(target.PlayerId) || Main.ForLostDeadDestinyChooser.Contains(target.PlayerId))
            {
                new LateTask(() =>
                {
                    target.RpcMurderPlayerV3(target);
                    Utils.NotifyRoles();
                }, Options.DestinyChooserSeconds.GetInt(), ("Killer"));
            }
            return false;
        }
        //压榨人工
        if (killer.Is(CustomRoles.Squeezers))
        {
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            Main.ForSqueezers.Add(target.PlayerId);
            new LateTask(() =>
            {
                if (!Main.TasksSqueezers.Contains(target.PlayerId))
                {
                    NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("NotAssassin")));
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                }
                else
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Squeezers), GetString("NotSqueezers")));
                    Main.TasksSqueezers.Remove(target.PlayerId);
                }
                Utils.NotifyRoles();
            }, Options.SqueezersMaxSecond.GetInt(), ("Killer"));
            return false;
        }
        //被保护了
        if (Main.MerchantProject.Contains(target.PlayerId))
        {
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            Main.MerchantProject.Remove(target.PlayerId);
            return false;
        }
        //化形者杀手
        /*if (killer.Is(CustomRoles.MimicKiller))
             {
            Utils.TP(killer.NetTransform, target.GetTruePosition());
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
            Utils.TP(target.NetTransform, Pelican.GetBlackRoomPS());
            target.SetRealKiller(killer);
            Main.PlayerStates[target.PlayerId].SetDead();
            target.RpcMurderPlayerV3(target);
            killer.SetKillCooldownV2();
            NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledByMimicKiller")));
            new LateTask(() =>
            {
                if (killer.IsAlive() && !target.IsAlive())
                {
                    Main.ForMimicKiller.Add(target.PlayerId);
                    Main.IsShapeShifted.Add(killer.PlayerId);
                    if (!killer.Data.IsDead)
                        killer.RpcShapeshift(target, true);
                }
            }, 0.3f);

            if (!GameStates.IsMeeting && Main.IsShapeShifted.Contains(killer.PlayerId))
            {
                if (!killer.Data.IsDead)
                    killer.RpcRevertShapeshift(true);
                Main.IsShapeShifted.Remove(killer.PlayerId);
                foreach (var assi in Main.AllAlivePlayerControls)
                {
                    if (assi.Is(CustomRoles.MimicAss) && assi.IsAlive())
                        assi.RpcRevertShapeshift(true);
                }
            }
            return false;
        }*/
        //伪人
        if (killer.Is(CustomRoles.Fake))
        {
            if (Main.FakeMax[killer.PlayerId] < Options.Fakemax.GetInt())
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                Main.FakeMax[killer.PlayerId]++;
                return false;
            }
            else
            {
                Main.NotKIller.Add(killer.PlayerId);
                Main.ForFake.Add(target.PlayerId);
                Main.NeedFake.TryAdd(killer.PlayerId, target.PlayerId);
                Utils.TP(killer.NetTransform, target.GetTruePosition());
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                Utils.TP(target.NetTransform, Pelican.GetBlackRoomPS());
                target.SetRealKiller(killer);
                Main.PlayerStates[target.PlayerId].SetDead();
                target.RpcMurderPlayerV3(target);
                killer.SetKillCooldownV2();
                NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledByFake")));
                return false;
            }
        }
        if (killer.Is(CustomRoles.AbandonedCrew))
        {

            new LateTask(() =>
            {
                target.Revive();
            }, 1f, ("Killer"));


        }
        //复活代码：目前在试验中
        // target.Revive();
        //==キル処理==
        __instance.RpcMurderPlayerV3(target);
        //============

        return false;
    }


    public static bool RpcCheckAndMurder(PlayerControl killer, PlayerControl target, bool check = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (target == null) target = killer;

        //禁止内鬼刀叛徒
        if (killer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Madmate) && !Options.ImpCanKillMadmate.GetBool())
            return false;

        //禁止叛徒刀内鬼
        if (killer.Is(CustomRoles.Madmate) && target.Is(CustomRoleTypes.Impostor) && !Options.MadmateCanKillImp.GetBool())
            return false;


        //被捕快带上手铐
        if (Main.DeputyInProtect.Contains(killer.PlayerId))
            return false;

        //被起诉人上空包弹
        if (Main.ProsecutorsInProtect.Contains(killer.PlayerId))
            return false;

        //护盾
        if (Medic.OnCheckMurder(killer, target))
            return false;
        //凶手被传染
        if (Main.ForSourcePlague.Contains(target.PlayerId))
        {
            Main.ForSourcePlague.Add(killer.PlayerId);
            foreach (var player in Main.AllPlayerControls)
            {
                if (player.Is(CustomRoles.SourcePlague))
                {
                    NameColorManager.Add(player.PlayerId, killer.PlayerId, "#999999");
                }
            }
        }
        //QX Skill
        Main.StrikersShields = Options.StrikersShields.GetInt();
        if (killer.Is(CustomRoles.Strikers) && Main.StrikersShields > 0)
        {
            Main.StrikersShields--;
        }
        if (target.Is(CustomRoles.Strikers) && Main.StrikersShields > 0)
        {
            killer.RpcGuardAndKill(killer);
            Main.StrikersShields--;
            return false;
        }
        if (killer.Is(CustomRoles.Strikers) && Main.StrikersShields == 0)
        {
            killer.RpcMurderPlayerV3(killer);
        }
        //被外星人干扰
        if (Main.ForET.Contains(killer.PlayerId))
        {
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            return false;
        }
        //击杀濒死
        if (Main.ForLostDeadDestinyChooser.Contains(target.PlayerId))
        {
            target.RpcMurderPlayerV3(target);
            target?.ReportDeadBody(null);
        }
        //被守护者杀死
        if (Main.ForKnight.Contains(target.PlayerId))
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (player.Is(CustomRoles.Knight) && player.IsAlive())
                {
                    player.RpcMurderPlayerV3(killer);
                    Main.ForKnight.Remove(target.PlayerId);
                }
            }
            return false;
        }
        if (Main.ForYandere.Contains(target.PlayerId))
        {
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (player.Is(CustomRoles.Yandere))
                {
                    player.RpcMurderPlayerV3(player);

                }
            }

        }
        //内鬼没啦
        if (target.Is(CustomRoleTypes.Impostor))
        {
            int DefectorInt = 0;
            int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
            int ImIntDead = 0;
            ImIntDead++;
            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.IsAlive() && player.GetCustomRole().IsImpostor() && !Main.KillImpostor.Contains(player.PlayerId) && !player.Is(CustomRoles.Defector) && player.PlayerId != target.PlayerId)
                {
                    Main.KillImpostor.Add(player.PlayerId);
                    ImIntDead++;

                    foreach (var partnerPlayer in Main.AllPlayerControls)
                    {
                        if (ImIntDead != optImpNum) continue;
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
        //击杀VIP
        if (target.Is(CustomRoles.VIP) || target.Is(CustomRoles.VIP) && killer.PlayerId == target.PlayerId)
        {
            foreach (var player in Main.AllPlayerControls)
            {
                player.KillFlash();
                if (target.Is(CustomRoleTypes.Impostor))
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("ImpostorDaed")));
                }
                else if (target.Is(CustomRoleTypes.Crewmate))
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Crewmate), GetString("CrewmateDead")));
                }
                else if (target.Is(CountTypes.Jackal))
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("JackalDaed")));
                }
                else if (target.Is(CountTypes.Pelican))
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pelican), GetString("PelicanDaed")));
                }
                else if (target.Is(CountTypes.Gamer))
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gamer), GetString("GamerDaed")));
                }
                else if (target.Is(CountTypes.BloodKnight))
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.BloodKnight), GetString("BloodKnightDaed")));
                }
                else if (target.Is(CountTypes.Succubus))
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("SuccubusDaed")));
                }
                else if (target.Is(CountTypes.YinLang))
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLangDaed")));
                }
                else if (target.Is(CountTypes.PlaguesGod))
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.PlaguesGod), GetString("PlaguesGodDaed")));
                }
                else
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Crewmate), GetString("NotRoles")));
                }
            }
        }
        switch (target.GetCustomRole())
        {
            //击杀幸运儿
            case CustomRoles.Luckey:
                if (target.Is(CustomRoles.OldThousand))
                {
                    Utils.TP(killer.NetTransform, target.GetTruePosition());
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                    killer.SetKillCooldownV2(target: target, forceAnime: true);
                    RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                    target.RpcGuardAndKill(killer);
                    return false;
                }
                var rd = IRandom.Instance;
                if (rd.Next(0, 100) < Options.LuckeyProbability.GetInt())
                {
                    var Lc = IRandom.Instance;
                    if (Lc.Next(0, 100) < Options.LuckeyCanSeeKillility.GetInt())
                    {
                        Utils.TP(killer.NetTransform, target.GetTruePosition());
                        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                        killer.SetKillCooldownV2(target: target, forceAnime: true);
                        RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                        target.RpcGuardAndKill(killer);
                        return false;
                    }
                    else
                    {
                        killer.SetKillCooldownV2(target: target, forceAnime: true);
                        return false;
                    }
                }
                break;
            //mascot be killed
            case CustomRoles.Mascot:
                if (target.Is(CustomRoles.OldThousand))
                {
                    Utils.TP(killer.NetTransform, target.GetTruePosition());
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                    killer.SetKillCooldownV2(target: target, forceAnime: true);
                    RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                    target.RpcGuardAndKill(killer);
                    killer.RpcMurderPlayerV3(killer);
                    return false;
                }
                var rdmas = IRandom.Instance;
                if (rdmas.Next(0, 100) < Options.MascotPro.GetInt())
                {
                    var pcList1 = Main.AllAlivePlayerControls.Where(x => x.PlayerId != killer.PlayerId).ToList();
                    var Fr1 = pcList1[IRandom.Instance.Next(0, pcList1.Count)];

                    if (Options.MascotKiller.GetBool() == true)
                    {
                        Main.PlayerStates[Fr1.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        killer.SetRealKiller(killer);
                        killer.RpcMurderPlayerV3(killer);
                        return false;
                    }
                    else if (Fr1.GetCustomRole().IsMascot())
                    {
                        killer.SetKillCooldownV2(target: target, forceAnime: true);
                        Main.PlayerStates[Fr1.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        killer.SetRealKiller(killer);
                        killer.RpcMurderPlayerV3(killer);
                        return false;
                    }
                    else
                    {
                        Main.PlayerStates[Fr1.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                        Fr1.SetRealKiller(Fr1);
                        Fr1.RpcMurderPlayerV3(Fr1);
                    }
                }
                break;

            //击杀呪狼
            case CustomRoles.CursedWolf:
                if (Main.CursedWolfSpellCount[target.PlayerId] <= 0) break;
                killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(target);
                Main.CursedWolfSpellCount[target.PlayerId] -= 1;
                RPC.SendRPCCursedWolfSpellCount(target.PlayerId);
                Logger.Info($"{target.GetNameWithRole()} : {Main.CursedWolfSpellCount[target.PlayerId]}回目", "CursedWolf");
                Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Spell;
                killer.RpcMurderPlayerV3(killer);
                return false;
            //击杀老兵
            case CustomRoles.Veteran:
                if (Main.VeteranInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
                    if (Main.VeteranInProtect[target.PlayerId] + Options.VeteranSkillDuration.GetInt() >= Utils.GetTimeStamp())
                    {
                        killer.SetRealKiller(target);
                        target.RpcMurderPlayerV3(killer);
                        Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{killer.GetRealName()}", "Veteran Kill");
                        return false;
                    }
                break;
            //检查明星附近是否有人
            case CustomRoles.SuperStar:
                if (Main.AllAlivePlayerControls.Where(x =>
                    x.PlayerId != killer.PlayerId &&
                    x.PlayerId != target.PlayerId &&
                    Vector2.Distance(x.GetTruePosition(), target.GetTruePosition()) < 2f
                    ).ToList().Count >= 1) return false;
                break;
            case CustomRoles.CupidLovers:
                if (Main.CupidShieldList.Contains(target) && Main.CupidComplete && !killer.Is(CustomRoles.Cupid))
                {


                    foreach (var cupid in Main.AllAlivePlayerControls)
                    {
                        if (cupid.Is(CustomRoles.Cupid))
                        {
                            cupid.RpcMurderPlayerV3(cupid);
                            Main.PlayerStates[cupid.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                        }
                        Main.CupidShieldList.Remove(target);
                    }

                }
                break;
            //玩家被击杀事件
            case CustomRoles.Gamer:
                if (!Gamer.CheckMurder(killer, target))
                    return false;
                break;
            //嗜血骑士技能生效中
            case CustomRoles.BloodKnight:
                if (BloodKnight.InProtect(target.PlayerId))
                {
                    killer.RpcGuardAndKill(target);
                    target.RpcGuardAndKill();
                    target.Notify(GetString("BKOffsetKill"));
                    return false;
                }
                break;
            //击杀挑衅者
            case CustomRoles.Rudepeople:
                if (!Rudepeople.CheckMurder(killer, target))
                    return false;
                break;
            //击杀银狼
            case CustomRoles.YinLang:
                killer.RPCPlayCustomSound("ylkq");
                Logger.Info("银狼被杀，开始执行114514", "YinLang");
                Main.AllPlayerKillCooldown[killer.PlayerId] = 114514f;
                NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang4")));
                foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
                {
                    var pos = target.transform.position;
                    var dis = Vector2.Distance(pos, pc.transform.position);
                    Logger.Info("执行减速", "YL");
                    //NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang2")));
                    Logger.Info($"{killer.GetNameWithRole()} |是否允许更改| => {killer.GetNameWithRole()}", "YinLang");
                    Main.AllPlayerSpeed[killer.PlayerId] = 0.01f;
                    var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
                    Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
                    ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
                    killer.MarkDirtySettings();
                    //    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang1")));
                    Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed - 0.01f;
                    ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
                    killer.MarkDirtySettings();
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                    Main.AllPlayerSpeed[killer.PlayerId] = 0.01f;
                    break;
                }
                break;
            //击杀失落的船员
            case CustomRoles.LostCrew:
                new LateTask(() =>
                {
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.RPCPlayCustomSound("SUS");
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.LostCrew), GetString("IAMSUS!!!")));
                    Utils.NotifyRoles();
                }, 5f, ("LOST!!!!"));
                new LateTask(() =>
                {
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.RPCPlayCustomSound("LOST");
                    Utils.NotifyRoles();
                }, 8f, ("SUS!!!!"));
                new LateTask(() =>
                {
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.RPCPlayCustomSound("LOST");
                    Utils.NotifyRoles();
                }, 12f, ("SUS!!!!"));
                new LateTask(() =>
                {
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.RPCPlayCustomSound("LOST");
                    Utils.NotifyRoles();
                }, 14f, ("SUS!!!!"));
                new LateTask(() =>
                {
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.RPCPlayCustomSound("LOST");
                    Utils.NotifyRoles();
                }, 15f, ("SUS!!!!"));
                new LateTask(() =>
                {
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.KillFlash();
                    killer.RPCPlayCustomSound("LOST");
                    target.RpcMurderPlayerV3(killer);
                    Utils.NotifyRoles();
                }, 17f, ("KILLER!!!!!!!!"));
                break;
            //击杀特务
            case CustomRoles.SpecialAgent:
                if (killer.Is(CustomRoles.OldThousand))
                {
                    Utils.TP(killer.NetTransform, target.GetTruePosition());
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                    target.SetRealKiller(killer);
                    Main.PlayerStates[target.PlayerId].SetDead();
                    killer.SetKillCooldownV2();
                    target.RpcMurderPlayerV3(target);
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    return true;
                }
                if (target.Is(CustomRoles.OldThousand))
                {
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayerV3(killer);
                    Logger.Info($"{target.GetRealName()} 特务反杀：{killer.GetRealName()}", "SpecialAgent Kill");
                    return false;
                }
                var pg = IRandom.Instance;
                if (pg.Next(0, 100) < Options.SpecialAgentrobability.GetInt())
                {
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayerV3(killer);
                    Logger.Info($"{target.GetRealName()} 特务反杀：{killer.GetRealName()}", "SpecialAgent Kill");
                    return false;
                }
                break;
            //击杀任务工
            case CustomRoles.HatarakiMan:
                if (killer.Is(CustomRoles.OldThousand))
                {
                    Utils.TP(killer.NetTransform, target.GetTruePosition());
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                    target.SetRealKiller(killer);
                    Main.PlayerStates[target.PlayerId].SetDead();
                    killer.SetKillCooldownV2();
                    target.RpcMurderPlayerV3(target);
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Kill;
                    return true;
                }
                if (target.Is(CustomRoles.OldThousand))
                {
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayerV3(killer);
                    return false;
                }
                var pg1 = IRandom.Instance;
                if (pg1.Next(0, 100) < Options.SpecialAgentrobability.GetInt())
                {
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayerV3(killer);
                    Logger.Info($"{target.GetRealName()} 任务工反杀：{killer.GetRealName()}", "HatarakiMan Kill");
                    return false;
                }
                break;
            //击杀萧暮
            case CustomRoles.XiaoMu:
                if (target.Is(CustomRoles.OldThousand))
                {
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayerV3(killer);
                    return false;
                }
                var Fg = IRandom.Instance;
                int xiaomu = Fg.Next(1, 3);
                if (xiaomu == 1)
                {
                    if (killer.PlayerId != target.PlayerId || target.GetRealKiller()?.GetCustomRole() is CustomRoles.Swooper)
                    {
                        NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.XiaoMu), GetString("YouKillXiaoMu1")));
                        killer.RPCPlayCustomSound("Congrats");
                        target.RPCPlayCustomSound("Congrats");
                        float delay;
                        if (Options.BaitDelayMax.GetFloat() < Options.BaitDelayMin.GetFloat()) delay = 0f;
                        else delay = IRandom.Instance.Next((int)Options.BaitDelayMin.GetFloat(), (int)Options.BaitDelayMax.GetFloat() + 1);
                        delay = Math.Max(delay, 0.15f);
                        if (delay > 0.15f && Options.BaitDelayNotify.GetBool()) killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), string.Format(GetString("KillBaitNotify"), (int)delay)), delay);
                        Logger.Info($"{killer.GetNameWithRole()} 击杀萧暮自动报告 => {target.GetNameWithRole()}", "XiaoMu");
                        new LateTask(() => { if (GameStates.IsInTask) killer.CmdReportDeadBody(target.Data); }, delay, "Bait Self Report");
                    }
                }
                else if (xiaomu == 2)
                {
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.XiaoMu), GetString("YouKillXiaoMu2")));
                    Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发原地不能动 => {target.GetNameWithRole()}", "XiaoMu");
                    var tmpSpeed1 = Main.AllPlayerSpeed[killer.PlayerId];
                    Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
                    ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
                    killer.MarkDirtySettings();
                    new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed1;
                        ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
                        killer.MarkDirtySettings();
                        RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                    }, Options.TrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
                }
                else if (xiaomu == 3)
                {
                    Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发cd114514 => {target.GetNameWithRole()}", "XiaoMu");
                    Main.AllPlayerKillCooldown[killer.PlayerId] = 114514f;
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.XiaoMu), GetString("YouKillXiaoMu3")));
                }
                else
                {
                    Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发若报告尸体则报告人知道凶手是谁 => {target.GetNameWithRole()}", "XiaoMu");
                    NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.XiaoMu), GetString("YouKillXiaoMu4")));
                }
                break;

            //击杀孟姜女
            case CustomRoles.MengJiangGirl:
                var Mg = IRandom.Instance;
                int mengjiang = Mg.Next(0, 15);
                PlayerControl mengjiangp = Utils.GetPlayerById(mengjiang);
                if (Options.MengJiangGirlWinnerPlayerer.GetInt() == 0)
                {
                    if (mengjiangp.GetCustomRole().IsCrewmate())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MengJiangGirl);
                        CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                        Logger.Info($"孟姜女被击杀，抽取到船员，设置为船员", "MengJiang");
                        break;
                    }
                }
                else if (Options.MengJiangGirlWinnerPlayerer.GetInt() == 1)
                {
                    if (mengjiangp.GetCustomRole().IsImpostor())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MengJiangGirl);
                        CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                        Logger.Info($"孟姜女被击杀，抽取到内鬼，设置为内鬼", "MengJiang");
                        break;
                    }
                }
                else if (Options.MengJiangGirlWinnerPlayerer.GetInt() == 2)
                {
                    if (mengjiangp.GetCustomRole().IsNeutral())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MengJiangGirl);
                        CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                        Logger.Info($"孟姜女被击杀，抽取到中立，设置为中立", "MengJiang");
                        break;
                    }
                }
                else
                {
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.cry;
                }
                break;
            //击杀不屈
            case CustomRoles.Indomitable:
                Main.ShieldPlayer = byte.MaxValue;
                Utils.TP(killer.NetTransform, target.GetTruePosition());
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                killer.SetKillCooldownV2(target: target, forceAnime: true);
                target.RpcGuardAndKill();
                new LateTask(() =>
                {
                    target?.NoCheckStartMeeting(target?.Data);
                }, 10.0f, "Skill Remain Message");
                new LateTask(() =>
                {
                    target.RpcMurderPlayerV3(target);
                }, 23.0f, "Skill Remain Message");
                return false;


            //击杀公牛
            case CustomRoles.Bull:
                killer.SetKillCooldownV2(target: target, forceAnime: true);
                foreach (var player in Main.AllPlayerControls)
                {
                    if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                    if (player == target) continue;
                    if (Vector2.Distance(target.transform.position, player.transform.position) <= Options.BullRadius.GetFloat())
                    {
                        player.SetRealKiller(target);
                        player.RpcMurderPlayerV3(player);
                        Main.BullKillMax[target.PlayerId]++;
                    }
                }
                if (Main.BullKillMax[target.PlayerId] >= Options.BullKill.GetInt())
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Bull);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);

                }
                return false;

            //击杀受虐狂
            case CustomRoles.Masochism:
                killer.SetKillCooldownV2(target: target, forceAnime: true);
                Main.MasochismKillMax[target.PlayerId]++;
                killer.RPCPlayCustomSound("DM");
                target.Notify(string.Format(GetString("MasochismKill"), Main.MasochismKillMax[target.PlayerId]));
                if (Main.MasochismKillMax[target.PlayerId] >= Options.KillMasochismMax.GetInt())
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Masochism);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
                return false;

            //击杀修道徒
            case CustomRoles.Cultivator:
                if (Main.CultivatorKillMax[killer.PlayerId] == 5 && Options.CultivatorFiveCanNotKill.GetBool())
                {
                    Utils.TP(killer.NetTransform, target.GetTruePosition());
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                    killer.SetKillCooldownV2(target: target, forceAnime: true);
                    return false;
                }
                break;

            //击杀厄运儿
            case CustomRoles.BadLuck:
                var BL = IRandom.Instance;
                if (BL.Next(0, 100) < 10)
                {
                    killer.SetKillCooldownV2(target: target, forceAnime: true);
                    return false;
                }
                else
                {
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    target.RpcMurderPlayerV3(target);
                    target.SetRealKiller(killer);
                    killer.RpcSetCustomRole(CustomRoles.UnluckyEggs);
                    return false;
                }

            //击杀豺狼
            case CustomRoles.Jackal:
                foreach (var player in Main.AllPlayerControls)
                {
                    Main.isjackalDead = true;
                    if (player.Is(CustomRoles.Sidekick))
                    {
                        player.RpcSetCustomRole(CustomRoles.Jackal);
                        Jackal.Add(player.PlayerId);
                        Jackal.Add(player.PlayerId);
                        player.ResetKillCooldown();
                        player.SetKillCooldown();
                        player.RpcGuardAndKill(player);
                    }
                }
                break;
            //击杀警长
            case CustomRoles.Sheriff:

                foreach (var player in Main.AllPlayerControls)
                {
                    Main.isSheriffDead = true;
                    if (Deputy.DeputyCanBeSheriff.GetBool())
                    {
                        if (player.Is(CustomRoles.Deputy))
                        {
                            player.RpcSetCustomRole(CustomRoles.Sheriff);

                            Sheriff.Add(player.PlayerId);
                            Sheriff.Add(player.PlayerId);

                            player.ResetKillCooldown();
                            player.SetKillCooldown();
                            player.RpcGuardAndKill(player);
                        }
                    }
                }
                break;
            //击杀时间之主
            case CustomRoles.TimeMaster:
                if (Main.TimeMasterInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
                    if (Main.TimeMasterInProtect[target.PlayerId] + Options.TimeMasterSkillDuration.GetInt() >= Utils.GetTimeStamp(DateTime.Now))
                    {
                        foreach (var player in Main.AllPlayerControls)
                        {
                            if (Main.TimeMasterbacktrack.ContainsKey(player.PlayerId))
                            {
                                var position = Main.TimeMasterbacktrack[player.PlayerId];
                                Utils.TP(player.NetTransform, position);
                            }
                        }
                        killer.SetKillCooldownV2(target: target, forceAnime: true);
                        return false;
                    }
                break;
            //击杀薛定谔的猫
            case CustomRoles.SchrodingerCat:
                if (target.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.noteam == true && (!target.Is(CustomRoles.Lovers) || !target.Is(CustomRoles.CrushLovers) || !target.Is(CustomRoles.CupidLovers)))
                {
                    foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                    {
                        if (role == CustomRoles.SchrodingerCat)
                        {
                            if (killer.GetCustomRole().IsCrewmate())
                            {
                                SchrodingerCat.iscrew = true;
                                SchrodingerCat.noteam = false;
                                Utils.TP(killer.NetTransform, target.GetTruePosition());
                                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                                Main.roleColors.TryAdd(role, "#ffffff");
                                NameColorManager.Add(target.PlayerId, target.PlayerId, "#ffffff");
                                target.RpcGuardAndKill(killer);
                                killer.RpcGuardAndKill(target);
                                return false;
                            }
                            else if (killer.GetCustomRole().IsImpostorTeam())
                            {
                                SchrodingerCat.isimp = true;
                                SchrodingerCat.noteam = false;
                                Utils.TP(killer.NetTransform, target.GetTruePosition());
                                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                                Main.roleColors.TryAdd(role, "#FF0000");
                                NameColorManager.Add(target.PlayerId, target.PlayerId, "#FF0000");
                                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#FF0000");
                                target.RpcGuardAndKill(killer);
                                killer.RpcGuardAndKill(target);
                                return false;
                            }
                            else if (killer.Is(CustomRoles.BloodKnight))
                            {
                                SchrodingerCat.isbk = true;
                                SchrodingerCat.noteam = false;
                                Utils.TP(killer.NetTransform, target.GetTruePosition());
                                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                                Main.roleColors.TryAdd(role, "#630000");
                                NameColorManager.Add(target.PlayerId, target.PlayerId, "#630000");
                                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#630000");
                                target.RpcGuardAndKill(killer);
                                killer.RpcGuardAndKill(target);


                                return false;
                            }
                            else if (killer.Is(CustomRoles.Gamer))
                            {
                                SchrodingerCat.isgam = true;
                                SchrodingerCat.noteam = false;
                                Utils.TP(killer.NetTransform, target.GetTruePosition());
                                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                                Main.roleColors.TryAdd(role, "#68bc71");
                                NameColorManager.Add(target.PlayerId, target.PlayerId, "#68bc71");
                                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#68bc71");
                                target.RpcGuardAndKill(killer);
                                killer.RpcGuardAndKill(target);
                                return false;
                            }
                            if (killer.Is(CustomRoles.Jackal) || killer.Is(CustomRoles.Sidekick))
                            {
                                SchrodingerCat.isjac = true;
                                SchrodingerCat.noteam = false;
                                Utils.TP(killer.NetTransform, target.GetTruePosition());
                                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                                Main.roleColors.TryAdd(role, "#00b4eb");
                                NameColorManager.Add(target.PlayerId, target.PlayerId, "#00b4eb");
                                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#00b4eb");
                                target.RpcGuardAndKill(killer);
                                killer.RpcGuardAndKill(target);

                                return false;
                            }
                            if (killer.Is(CustomRoles.Loners))
                            {
                                SchrodingerCat.isln = true;
                                SchrodingerCat.noteam = false;
                                Utils.TP(killer.NetTransform, target.GetTruePosition());
                                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                                Main.roleColors.TryAdd(role, "#B0C4DE");
                                NameColorManager.Add(target.PlayerId, target.PlayerId, "#B0C4DE");
                                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#B0C4DE");
                                target.RpcGuardAndKill(killer);
                                killer.RpcGuardAndKill(target);
                                return false;
                            }
                            if (killer.Is(CustomRoles.YinLang))
                            {
                                SchrodingerCat.isyl = true;
                                SchrodingerCat.noteam = false;
                                Utils.TP(killer.NetTransform, target.GetTruePosition());
                                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                                Main.roleColors.TryAdd(role, "#6A5ACD");
                                NameColorManager.Add(target.PlayerId, target.PlayerId, "#6A5ACD");
                                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#6A5ACD");
                                target.RpcGuardAndKill(killer);
                                killer.RpcGuardAndKill(target);


                                return false;
                            }
                            if (killer.Is(CustomRoles.PlaguesGod))
                            {
                                SchrodingerCat.ispg = true;
                                SchrodingerCat.noteam = false;
                                Utils.TP(killer.NetTransform, target.GetTruePosition());
                                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                                Main.roleColors.TryAdd(role, "#101010");
                                NameColorManager.Add(target.PlayerId, target.PlayerId, "#101010");
                                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#101010");
                                target.RpcGuardAndKill(killer);
                                killer.RpcGuardAndKill(target);

                                return false;
                            }
                            if (killer.Is(CustomRoles.DarkHide))
                            {
                                SchrodingerCat.isdh = true;
                                SchrodingerCat.noteam = false;
                                Utils.TP(killer.NetTransform, target.GetTruePosition());
                                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                                Main.roleColors.TryAdd(role, "#483d8b");
                                NameColorManager.Add(target.PlayerId, target.PlayerId, "#483d8b");
                                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#483d8b");
                                target.RpcGuardAndKill(killer);
                                killer.RpcGuardAndKill(target);
                                return false;
                            }
                            if (killer.Is(CustomRoles.OpportunistKiller))
                            {
                                SchrodingerCat.isok = true;
                                SchrodingerCat.noteam = false;
                                Utils.TP(killer.NetTransform, target.GetTruePosition());
                                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                                Main.roleColors.TryAdd(role, "#CC6600");
                                NameColorManager.Add(target.PlayerId, target.PlayerId, "#CC6600");
                                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#CC6600");
                                target.RpcGuardAndKill(killer);
                                killer.RpcGuardAndKill(target);
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    return true;
                }
                break;
            //击杀奴隶主
            case CustomRoles.Slaveowner:
                foreach (var player in Main.AllPlayerControls)
                {
                    if (Main.ForSlaveowner.Contains(player.PlayerId))
                    {
                        Main.ForSlaveowner.Remove(target.PlayerId);
                        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                    }
                }
                break;
            //击杀咒术师
            case CustomRoles.Spellmaster:
                int Fs = 0;
                foreach (var player in Main.AllPlayerControls)
                {
                    if (player == target) continue;
                    if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                    if (Main.ForSpellmaster.Contains(player.PlayerId))
                    {
                        player.RpcMurderPlayerV3(player);
                        Main.ForSpellmaster.Remove(target.PlayerId);
                        Fs++;
                        killer.SetKillCooldownV2(target: target, forceAnime: true);
                        return false;
                    }
                }
                if (Fs == 1)
                {
                    killer.SetKillCooldownV2(target: target, forceAnime: true);
                    return false;
                }
                break;
            //击杀万疫之神
            case CustomRoles.PlaguesGod:
                killer.SetRealKiller(target);
                target.RpcMurderPlayerV3(killer);
                Logger.Info($"{target.GetRealName()} 万疫之神反杀：{killer.GetRealName()}", "PlagueGod Kill");
                return false;

            //击杀逃犯
            case CustomRoles.Fugitive:
                var Fi = IRandom.Instance;
                int rndNum = Fi.Next(0, 100);
                if (rndNum >= 10 && rndNum < 20)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Masochism);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
                if (rndNum >= 20 && rndNum < 30)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Bull);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
                if (rndNum >= 30 && rndNum < 40)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
                if (rndNum >= 40 && rndNum < 50)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jester);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
                if (rndNum >= 50 && rndNum < 60)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jealousy);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
                if (rndNum >= 60 && rndNum < 70)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BloodKnight);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
                if (rndNum >= 70 && rndNum < 80)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.King);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
                if (rndNum >= 80 && rndNum < 90)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.God);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
                if (rndNum >= 90 && rndNum < 100)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DarkHide);
                    CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                }
                break;
            //击杀汤姆
            case CustomRoles.Tom:
                if (Main.TomKill[target.PlayerId] < Options.TomMax.GetInt())
                {
                    CustomSoundsManager.RPCPlayCustomSoundAll("TomAAA");
                    Main.TomKill[target.PlayerId]++;
                    Utils.TP(killer.NetTransform, target.GetTruePosition());
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                    killer.SetKillCooldownV2(target: target, forceAnime: true);
                    RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
                    target.RpcGuardAndKill(killer);
                    var tmpSpeed2 = Main.AllPlayerSpeed[target.PlayerId];
                    Main.AllPlayerSpeed[target.PlayerId] = Options.TomSpeed.GetInt();
                    target.MarkDirtySettings();
                    new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Options.TomSpeed.GetInt() + tmpSpeed2;
                        target.MarkDirtySettings();
                    }, Options.TomSecond.GetFloat(), "Trapper BlockMove");
                    return false;
                }
                break;
            case CustomRoles.AnimalRefuser:
                if (killer.Is(CustomRoles.AnimalRefuser))
                {
                    if (!target.GetCustomRole().IsAnimal())
                    {
                        killer.RpcMurderPlayerV3(killer);
                        return false;
                    }
                    else
                    {
                        Main.RefuserShields++;
                        return false;
                    }

                }
                if (target.Is(CustomRoles.AnimalRefuser))
                {
                    if (Main.RefuserShields > 0)
                    {
                        Main.RefuserShields--;
                        return false;
                    }
                }
                break;

            case CustomRoles.UnanimalRefuser:
                if (killer.Is(CustomRoles.UnanimalRefuser))
                {
                    if (target.GetCustomRole().IsAnimal())
                    {
                        killer.RpcMurderPlayerV3(killer);
                        return false;
                    }
                    else
                    {
                        Main.RefuserShields++;
                        return false;
                    }
                }
                if (target.Is(CustomRoles.UnanimalRefuser))
                {
                    if (Main.RefuserShields > 0)
                    {
                        Main.RefuserShields--;
                        return false;
                    }
                }
                break;

            case CustomRoles.AttendRefuser:
                if (killer.Is(CustomRoles.AttendRefuser))
                {
                    if (!target.GetCustomRole().IsAttend())
                    {
                        killer.RpcMurderPlayerV3(killer);
                        return false;
                    }
                    else
                    {
                        Main.RefuserShields++;
                        return false;
                    }
                }
                if (target.Is(CustomRoles.AttendRefuser))
                {
                    if (Main.RefuserShields > 0)
                    {
                        Main.RefuserShields--;
                        return false;
                    }
                }
                break;
            case CustomRoles.HangTheDevil:
                Main.HangTheDevilKiller.Add(killer.PlayerId);
                target?.ReportDeadBody(null);
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.Is(CustomRoleTypes.Impostor))
                    {
                        pc.ShowPopUp(GetString("HangTheDevilMsg"));
                        continue;
                    }
                    pc.ShowPopUp(GetString("HangTheDevilMsg"));
                    Main.ForHangTheDevil.Add(pc.PlayerId);
                }
                break;
        }



        //护士急救
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {
                if (pc.Is(CustomRoles.Nurse))
                {
                    if (target.PlayerId == pc.PlayerId || Main.NnurseHelepMax[pc.PlayerId] >= Options.NurseMax.GetInt() || !pc.IsAlive())
                        Logger.Info($"{pc.GetRealName()} 是个叛徒，所以他选择无视杀人现场", "Bodyguard");
                    else
                    {
                        Main.NnurseHelepMax[pc.PlayerId]++;
                        Utils.TP(killer.NetTransform, target.GetTruePosition());
                        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                        Utils.TP(target.NetTransform, Pelican.GetBlackRoomPS());
                        Main.ForNnurse.Add(pc.PlayerId, Utils.GetTimeStamp());
                        Main.NnurseHelep.Add(target.PlayerId);
                        Main.NnurseHelep.Add(pc.PlayerId);
                        killer.SetKillCooldownV2();
                        NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Nurse), GetString("HelpByNurse")));
                        var tmpSpeed1 = Main.AllPlayerSpeed[pc.PlayerId];
                        Main.AllPlayerSpeed[pc.PlayerId] = Main.MinSpeed;
                        var tmpSpeed2 = Main.AllPlayerSpeed[target.PlayerId];
                        Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
                        pc.MarkDirtySettings();
                        target.MarkDirtySettings();
                        new LateTask(() =>
                        {
                            Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] - Main.MinSpeed + tmpSpeed1;
                            pc.MarkDirtySettings();
                            Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed2;
                            target.MarkDirtySettings();
                            Utils.TP(target.NetTransform, pc.GetTruePosition());
                            Utils.TP(target.NetTransform, pc.GetTruePosition());
                            RPC.PlaySoundRPC(pc.PlayerId, Sounds.TaskComplete);
                            RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
                            Main.NnurseHelep.Remove(target.PlayerId);
                            if (!pc.IsAlive())
                            {
                                target.RpcMurderPlayerV3(target);
                            }
                        }, Options.NurseSkillDuration.GetFloat(), "Trapper BlockMove");
                        return false;
                    }
                }
            }
        }
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {
                var pos = target.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > Options.GuideKillRadius.GetFloat()) continue;
                if (pc.Is(CustomRoles.Guide) && killer.GetCustomRole().IsImpostor() && Main.GuideMax[pc.PlayerId] <= Options.GuideKillMax.GetInt())
                {
                    Main.GuideMax[pc.PlayerId]++;
                    foreach (var player in Main.AllAlivePlayerControls)
                    {
                        if (!player.IsModClient()) player.KillFlash();
                        if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                        if (player == killer) continue;
                        if (player == pc) continue;
                        if (player == target) continue;
                        if (Vector2.Distance(pc.transform.position, player.transform.position) <= Options.GuideKillRadius.GetFloat())
                        {
                            Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.ForGuide;
                            player.SetRealKiller(pc);
                            killer.RpcMurderPlayerV3(player);
                        }
                    }
                }
            }
        }
        //灵能者操控灵魂
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {
                var Sr = IRandom.Instance;
                if (pc.Is(CustomRoles.Spiritualizer) && Sr.Next(0, 100) < Options.SpiritualizerProbability.GetInt())
                {
                    Main.ForSpiritualizerCrewmate.Add(target.PlayerId);
                    Main.ForSpiritualizerImpostor.Add(killer.PlayerId);
                }
            }
        }
        //雷达
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {

                if (Main.MerchantLeiDa.Contains(pc.PlayerId))
                {
                    Main.MerchantLeiDa.Remove(pc.PlayerId);
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (player == killer && Vector2.Distance(killer.transform.position, player.transform.position) <= 3)
                        {
                            Main.MerchantMax[pc.PlayerId]++;
                        }
                    }
                    pc.Notify(GetString("MerchantOnGuard"), Main.MerchantMax[pc.PlayerId]);
                }
            }
        }

        //被嫉妒狂看中
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {
                if (pc.Is(CustomRoles.Jealousy))
                {
                    if (killer.PlayerId == pc.PlayerId && Main.ForJealousy.Contains(target.PlayerId))
                    {
                        pc.ResetKillCooldown();
                    }
                    else
                    {
                        Main.ForJealousy.Add(killer.PlayerId);
                        if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
                        var Jealousy = killer.PlayerId;
                        pc.Notify(string.Format(GetString("ForJealousy!!!"), Main.AllPlayerNames[Jealousy]));
                        NameColorManager.Add(pc.PlayerId, killer.PlayerId, "#996666");
                        pc.RPCPlayCustomSound("anagry");
                    }
                }
            }
        }
        //啊啊啊是血！！！！！
        if (killer.PlayerId != target.PlayerId)
        {
            if (killer.Is(CustomRoles.Hemophobia) && !Main.ForHemophobia.Contains(killer.PlayerId)) return false;
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {
                var pos = target.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > Options.HemophobiaRadius.GetFloat()) continue;

                if (pc.Is(CustomRoles.Hemophobia))
                {
                    if (killer.PlayerId == pc.PlayerId)
                        Logger.Info($"{pc.GetRealName()} 是个叛徒，所以他选择无视杀人现场", "Bodyguard");
                    else
                    {
                        Main.HemophobiaInKill.Remove(pc.PlayerId);
                        Main.HemophobiaInKill.Add(pc.PlayerId, Utils.GetTimeStamp());
                        Main.ForHemophobia.Add(pc.PlayerId);
                        if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
                        pc.Notify(GetString("HemophobiaOnGuard"), Options.HemophobiaSeconds.GetFloat());
                    }
                }
            }
        }

        //妖魔鬼怪快离开
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {
                if (pc.Is(CustomRoles.Exorcist))
                {
                    Main.ForExorcist.Add(killer.PlayerId);
                    RPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
                    NameColorManager.Add(pc.PlayerId, killer.PlayerId, "#FF0000");
                }
            }
        }
        //牛仔套圈
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {
                if (pc.Is(CustomRoles.Cowboy))
                {
                    if (Main.MaxCowboy[pc.PlayerId] >= Options.CowboyMax.GetInt() || pc.Is(CustomRoles.Madmate) && killer.GetCustomRole().IsImpostorTeam())
                        Logger.Info($"{pc.GetRealName()} 是个叛徒，所以他选择无视杀人现场", "Bodyguard");
                    else
                    {
                        Main.MaxCowboy[pc.PlayerId]++;
                        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                        killer.SetKillCooldownV2(target: target, forceAnime: true);
                        RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);

                        new LateTask(() =>
                        {
                            Utils.TP(target.NetTransform, pc.GetTruePosition());
                            pc.RpcGuardAndKill();
                            Utils.NotifyRoles();
                        }, 0.2f, ("Come!"));
                        return false;
                    }
                }
            }
        }

        //护盾师保护
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {
                var pos = target.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > Options.NiceShieldsRadius.GetFloat()) continue;
                if (pc.Is(CustomRoles.NiceShields))
                {
                    if (Main.NiceShieldsInProtect.ContainsKey(pc.PlayerId) && killer.PlayerId != target.PlayerId)
                        if (Main.NiceShieldsInProtect[pc.PlayerId] + Options.NiceShieldsSkillDuration.GetInt() >= Utils.GetTimeStamp(DateTime.Now))
                        {
                            if (pc.Is(CustomRoles.Madmate) && killer.GetCustomRole().IsImpostorTeam())
                            {
                                Logger.Info($"{pc.GetRealName()} 是个叛徒，所以他选择无视杀人现场", "Bodyguard");
                            }
                            else
                            {
                                killer.SetKillCooldownV2(target: target, forceAnime: true);
                                return false;
                            }
                        }
                }
            }
        }

        //保镖保护
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {
                var pos = target.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > Options.BodyguardProtectRadius.GetFloat()) continue;
                if (pc.Is(CustomRoles.Bodyguard))
                {
                    if (pc.Is(CustomRoles.Madmate) && killer.GetCustomRole().IsImpostorTeam())
                        Logger.Info($"{pc.GetRealName()} 是个叛徒，所以他选择无视杀人现场", "Bodyguard");
                    else
                    {
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                        pc.RpcMurderPlayerV3(killer);
                        pc.SetRealKiller(killer);
                        pc.RpcMurderPlayerV3(pc);
                        Logger.Info($"{pc.GetRealName()} 挺身而出与歹徒 {killer.GetRealName()} 同归于尽", "Bodyguard");
                        return false;
                    }
                }
            }
        }

        //首刀保护
        if (Main.ShieldPlayer != byte.MaxValue && Main.ShieldPlayer == target.PlayerId && Utils.IsAllAlive)
        {
            Main.ShieldPlayer = byte.MaxValue;
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            target.RpcGuardAndKill();
            return false;
        }
        //UP首刀保护
        if (Main.ShieldPlayer != byte.MaxValue && PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsUp && Utils.IsAllAlive && Options.EnableUpMode.GetBool())
        {
            Main.ShieldPlayer = byte.MaxValue;
            killer.SetKillCooldownV2(target: target, forceAnime: true);
            target.RpcGuardAndKill();
            return false;
        }

        //首刀叛变
        if (Options.MadmateSpawnMode.GetInt() == 1 && Main.MadmateNum < CustomRoles.Madmate.GetCount() && Utils.CanBeMadmate(target))
        {
            Main.MadmateNum++;
            target.RpcSetCustomRole(CustomRoles.Madmate);
            ExtendedPlayerControl.RpcSetCustomRole(target.PlayerId, CustomRoles.Madmate);
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("BecomeMadmateCuzMadmateMode")));
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Madmate.ToString(), "Assign " + CustomRoles.Madmate.ToString());
            return false;
        }

        if (!check) killer.RpcMurderPlayerV3(target);
        return true;
    }
    #endregion
}
