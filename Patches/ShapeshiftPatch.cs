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

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
class ShapeshiftPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        Logger.Info($"{__instance?.GetNameWithRole()} => {target?.GetNameWithRole()}", "Shapeshift");

        var shapeshifter = __instance;
        var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

        if (Main.CheckShapeshift.TryGetValue(shapeshifter.PlayerId, out var last) && last == shapeshifting)
        {
            Logger.Info($"{__instance?.GetNameWithRole()}:Cancel Shapeshift.Prefix", "Shapeshift");
            return;
        }

        Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
        Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

        Sniper.OnShapeshift(shapeshifter, shapeshifting);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!shapeshifting) Camouflage.RpcSetSkin(__instance);

        if (Pelican.IsEaten(shapeshifter.PlayerId) || GameStates.IsVoting || Main.ForMagnetMan.Contains(shapeshifter.PlayerId))
            goto End;

        switch (shapeshifter.GetCustomRole())
        {
            case CustomRoles.EvilTracker:
                EvilTracker.OnShapeshift(shapeshifter, target, shapeshifting);
                break;
            case CustomRoles.FireWorks:
                FireWorks.ShapeShiftState(shapeshifter, shapeshifting);
                break;
            case CustomRoles.Warlock:
                if (Main.CursedPlayers[shapeshifter.PlayerId] != null)//呪われた人がいるか確認
                {
                    if (shapeshifting && !Main.CursedPlayers[shapeshifter.PlayerId].Data.IsDead)//変身解除の時に反応しない
                    {
                        var cp = Main.CursedPlayers[shapeshifter.PlayerId];
                        Vector2 cppos = cp.transform.position;//呪われた人の位置
                        Dictionary<PlayerControl, float> cpdistance = new();
                        float dis;
                        foreach (PlayerControl p in Main.AllAlivePlayerControls)
                        {
                            if (p.PlayerId == cp.PlayerId) continue;
                            if (!Options.WarlockCanKillSelf.GetBool() && p.PlayerId == shapeshifter.PlayerId) continue;
                            if (!Options.WarlockCanKillAllies.GetBool() && p.GetCustomRole().IsImpostor()) continue;
                            dis = Vector2.Distance(cppos, p.transform.position);
                            cpdistance.Add(p, dis);
                            Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "Warlock");
                        }
                        if (cpdistance.Count >= 1)
                        {
                            var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                            PlayerControl targetw = min.Key;
                            if (cp.RpcCheckAndMurder(targetw, true))
                            {
                                targetw.SetRealKiller(shapeshifter);
                                Logger.Info($"{targetw.GetNameWithRole()}was killed", "Warlock");
                                cp.RpcMurderPlayerV3(targetw);//殺す
                                shapeshifter.RpcGuardAndKill(shapeshifter);
                                shapeshifter.Notify(GetString("WarlockControlKill"));
                            }
                        }
                        else
                        {
                            shapeshifter.Notify(GetString("WarlockNoTarget"));
                        }
                        Main.isCurseAndKill[shapeshifter.PlayerId] = false;
                        RPC.RpcSyncCurseAndKill();
                    }
                    Main.CursedPlayers[shapeshifter.PlayerId] = null;
                }
                break;
            case CustomRoles.Escapee:
                if (shapeshifting)
                {
                    if (Main.EscapeeLocation.ContainsKey(shapeshifter.PlayerId))
                    {
                        var position = Main.EscapeeLocation[shapeshifter.PlayerId];
                        Main.EscapeeLocation.Remove(shapeshifter.PlayerId);
                        Logger.Msg($"{shapeshifter.GetNameWithRole()}:{position}", "EscapeeTeleport");
                        Utils.TP(shapeshifter.NetTransform, position);
                        shapeshifter.RPCPlayCustomSound("Teleport");
                    }
                    else
                    {
                        Main.EscapeeLocation.Add(shapeshifter.PlayerId, shapeshifter.GetTruePosition());
                    }
                }
                break;
            case CustomRoles.Amnesiac:
                Main.isCursed = false;
                break;
            case CustomRoles.Shifter:
                Main.isCursed = false;
                break;
            case CustomRoles.Miner:
                if (Main.LastEnteredVent.ContainsKey(shapeshifter.PlayerId))
                {
                    int ventId = Main.LastEnteredVent[shapeshifter.PlayerId].Id;
                    var vent = Main.LastEnteredVent[shapeshifter.PlayerId];
                    var position = Main.LastEnteredVentLocation[shapeshifter.PlayerId];
                    Logger.Msg($"{shapeshifter.GetNameWithRole()}:{position}", "MinerTeleport");
                    Utils.TP(shapeshifter.NetTransform, new Vector2(position.x, position.y));
                }
                break;
            case CustomRoles.Assassin:
                if (shapeshifting)
                {
                    if (!target.IsAlive())
                    {
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("NotAssassin")));
                        break;
                    }
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.AtAssassin;
                    target.SetRealKiller(__instance);
                    target.RpcMurderPlayerV3(target);
                }
                break;
            case CustomRoles.ImperiusCurse:
                if (shapeshifting)
                {
                    new LateTask(() =>
                    {
                        if (!(!GameStates.IsInTask || !shapeshifter.IsAlive() || !target.IsAlive() || shapeshifter.inVent || target.inVent))
                        {
                            var originPs = target.GetTruePosition();
                            Utils.TP(target.NetTransform, shapeshifter.GetTruePosition());
                            Utils.TP(shapeshifter.NetTransform, originPs);
                        }
                    }, 1.5f, "ImperiusCurse TP");
                }
                break;
            case CustomRoles.QuickShooter:
                QuickShooter.OnShapeshift(shapeshifter, shapeshifting);
                break;
            case CustomRoles.Concealer:
                Concealer.OnShapeshift(shapeshifting);
                break;
            case CustomRoles.Hacker:
                Hacker.OnShapeshift(shapeshifter, shapeshifting, target);
                break;
            case CustomRoles.Anglers:
                if (shapeshifting)
                {
                    Utils.TP(target.NetTransform, shapeshifter.GetTruePosition());
                }
                break;
            case CustomRoles.Henry:
                Henry.OnShapeshift(shapeshifter);
                break;
            case CustomRoles.Disperser:
                if (shapeshifting)
                    Disperser.DispersePlayers(shapeshifter);
                break;
            case CustomRoles.Sleeve:
                if (shapeshifting)
                {
                    if (!target.IsAlive())
                    {
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("NotAssassin")));
                        break;
                    }
                    new LateTask(() =>
                    {
                        Main.ForSleeve.Add(target.PlayerId);
                        Utils.NotifyRoles();
                    }, 3f, ("LOST!!!!"));
                    new LateTask(() =>
                    {
                        Main.ForSleeve.Remove(target.PlayerId);
                        target.SetRealKiller(__instance);
                        target.RpcMurderPlayerV3(target);
                        Utils.NotifyRoles();
                    }, Options.SleeveshifterCooldown.GetInt(), ("LOST!!!!"));
                }
                break;
            case CustomRoles.Medusa:
                if (shapeshifting)
                {
                    if (target.IsAlive())
                    {
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
                        }, Options.MedusaMax.GetInt(), "石化");
                    }
                    else
                    {
                        Main.ForMedusa.Add(target.PlayerId);
                        new LateTask(() =>
                        {
                            Main.ForMedusa.Remove(target.PlayerId);
                        }, Options.MedusaMax.GetInt(), "石化");
                    }

                }
                break;
            case CustomRoles.Cluster:
                if (shapeshifting)
                {
                    if (!target.IsAlive())
                    {
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("NotAssassin")));
                        break;
                    }
                    new LateTask(() =>
                    {
                        Main.ForCluster.Add(target.PlayerId);
                    }, 4, "石化");

                    new LateTask(() =>
                    {
                        Main.ForCluster.Remove(target.PlayerId);
                    }, Options.ClusterMax.GetInt(), "石化");
                }
                break;
            case CustomRoles.Forger:
                if (shapeshifting)
                {
                    if (!target.IsAlive())
                    {
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("NotAssassin")));
                        break;
                    }
                    var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
                    Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
                    target.MarkDirtySettings();
                    new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
                        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId).ToList();
                        var Fh = pcList[IRandom.Instance.Next(0, pcList.Count)];
                        target.RpcShapeshift(Fh, true);
                        target.MarkDirtySettings();
                    }, 0.5f, "石化");

                    new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
                        target.MarkDirtySettings();
                    }, Options.ForgerMax.GetInt() - 0.5f, "石化");
                    new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
                        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId).ToList();
                        var Fh = pcList[IRandom.Instance.Next(0, pcList.Count)];
                        target.MarkDirtySettings();
                        target.CmdCheckRevertShapeshift(true);
                    }, Options.ForgerMax.GetInt() + 0.5f, "石化");
                }
                break;
            case CustomRoles.Blackmailer:
                if (shapeshifting)
                {
                    if (!target.IsAlive())
                    {
                        NameNotifyManager.Notify(__instance, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("NotAssassin")));
                        break;
                    }
                    Blackmailer.ForBlackmailer.Add(target.PlayerId);
                }
                break;
            case CustomRoles.Batter:
                string playerName = __instance.ToString();
                if (shapeshifting)
                {
                    __instance.SetName(GetString("BatterReady"));
                    new LateTask(() =>
                    {
                        foreach (var player in Main.AllAlivePlayerControls)
                        {
                            if (!player.IsModClient()) player.KillFlash();
                            if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                            if (Vector2.Distance(__instance.transform.position, player.transform.position) <= Options.BomberRadius.GetFloat())
                            {
                                player.SetRealKiller(__instance);
                                Utils.TP(player.NetTransform, Pelican.GetBlackRoomPS());
                                player.RpcMurderPlayerV3(player);
                                NameNotifyManager.Notify(player, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledByBatter")));
                                Medic.IsDead(player);
                            }
                        }
                        __instance.SetName(playerName);
                    }, Options.BatterCooldown.GetInt() + 3f, "石化");
                }
                break;
            case CustomRoles.SoulSucker:
                SoulSucker.OnShapeshift(shapeshifter, target);
                break;
        }

    End:

        //変身解除のタイミングがずれて名前が直せなかった時のために強制書き換え
        if (!shapeshifting)
        {
            new LateTask(() =>
            {
                Utils.NotifyRoles(NoCache: true);
            },
            1.2f, "ShapeShiftNotify");
        }
    }
}
