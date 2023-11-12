using AmongUs.GameOptions;
using Epic.OnlineServices.UserInfo;
using HarmonyLib;
using Hazel;
using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using System.Linq;
using TheOtherRoles_Host.Roles.Double;
using TheOtherRoles_Host.Roles.Neutral;
using static TheOtherRoles_Host.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TheOtherRoles_Host;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]

class GameEndChecker
{
    List<CustomRoles> AAA = new();
    private static GameEndPredicate predicate;
    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        //ゲーム終了判定済みなら中断
        if (predicate == null) return false;

        //ゲーム終了しないモードで廃村以外の場合は中断
        if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.Error) return false;

        //廃村用に初期値を設定
        var reason = GameOverReason.ImpostorByKill;

        //ゲーム終了判定
        predicate.CheckForEndGame(out reason);

        // SoloKombat
        if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            if (CustomWinnerHolder.WinnerIds.Count > 0 || CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                ShipStatus.Instance.enabled = false;
                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }

        //ゲーム終了時
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
        {
            //カモフラージュ強制解除
            Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true));

            if (reason == GameOverReason.ImpostorBySabotage && CustomRoles.Jackal.RoleExist() && Jackal.CanWinBySabotageWhenNoImpAlive.GetBool() && !Main.AllAlivePlayerControls.Any(x => x.GetCustomRole().IsImpostorTeam()))
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.WinnerIds.Clear();
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
            }

            switch (CustomWinnerHolder.WinnerTeam)
            {
                case CustomWinner.Crewmate:
                    Main.AllPlayerControls
                        .Where(pc => (pc.Is(CustomRoleTypes.Crewmate)) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.CrushLovers) && !pc.Is(CustomRoles.Honmei) && !pc.Is(CustomRoles.CupidLovers) && !pc.Is(CustomRoles.Madmate) && !pc.Is(CustomRoles.Charmed) && !pc.Is(CustomRoles.Attendant) && !pc.Is(CustomRoles.Fugitive))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Impostor:
                    Main.AllPlayerControls
                        .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoles.Madmate)) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.CrushLovers) && !pc.Is(CustomRoles.CupidLovers) && !pc.Is(CustomRoles.Charmed) && !pc.Is(CustomRoles.Attendant) && !Main.ForFake.Contains(pc.PlayerId))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Succubus:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.Succubus) || pc.Is(CustomRoles.Charmed))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Jackal:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.Jackal) || pc.Is(CustomRoles.Attendant) || pc.Is(CustomRoles.Whoops) || pc.Is(CustomRoles.Sidekick) || pc.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isjac == true)
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Yandere:
                    Main.AllPlayerControls
                        .Where(pc => Main.ForYandere.Contains(pc.PlayerId) || pc.Is(CustomRoles.Yandere))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Henry:
                    Main.AllPlayerControls
                         .Where(pc => pc.Is(CustomRoles.Henry))
                         .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.CupidLovers:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.Cupid) || pc.Is(CustomRoles.CupidLovers))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Akujo:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.Akujo) || pc.Is(CustomRoles.Honmei))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.YinLang:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.YinLang) || pc.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isyl == true)
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.captor:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.captor))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.runagat:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.runagat))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.MengJiangGirl:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.MengJiangGirl))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.BloodKnight:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.BloodKnight) || pc.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isbk == true)
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Gamer:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.Gamer) || pc.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isgam == true)
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.PlaguesGod:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.PlaguesGod) || pc.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.ispg == true)
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Loners:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.Loners) || pc.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isln == true)
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Meditator:
                    Main.AllPlayerControls
                       .Where(pc => pc.Is(CustomRoles.Meditator) || pc.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isln == true)
                       .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None and not CustomWinner.Error)
            {

                //潜藏者抢夺胜利
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.DarkHide) && !pc.Data.IsDead
                        && ((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorBySabotage)) || CustomWinnerHolder.WinnerTeam == CustomWinner.DarkHide
                        || (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.HumansByTask) && DarkHide.IsWinKill[pc.PlayerId] == true)))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DarkHide);
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        foreach (var player in Main.AllPlayerControls)
                        {
                            if (player.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isdh)
                            {
                                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                            }
                        }
                    }
                    else if (pc.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isdh && !pc.Data.IsDead
                        && ((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorBySabotage)) || CustomWinnerHolder.WinnerTeam == CustomWinner.DarkHide
                        || (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.HumansByTask) && DarkHide.IsWinKill[pc.PlayerId] == true)))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DarkHide);
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        foreach (var player in Main.AllPlayerControls)
                        {
                            if (player.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isdh)
                            {
                                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                            }
                        }
                    }
                }
                //利己主义者抢夺胜利（船员）
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
                {
                    foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsCrewmate()))
                        if (pc.Is(CustomRoles.Egoist))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                }

                //利己主义者抢夺胜利（内鬼）
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
                {
                    foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsImpostor()))
                        if (pc.Is(CustomRoles.Egoist))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                }

                //神抢夺胜利
                if (CustomRolesHelper.RoleExist(CustomRoles.God))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.God);
                    Main.AllPlayerControls
                        .Where(p => p.Is(CustomRoles.God) && p.IsAlive())
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                }
                //恋人抢夺胜利
                if (CustomRolesHelper.RoleExist(CustomRoles.Lovers) && !reason.Equals(GameOverReason.HumansByTask))
                {
                    if (!(!Main.LoversPlayers.ToArray().All(p => p.IsAlive()) && Options.LoverSuicide.GetBool()))
                    {
                        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.Impostor or CustomWinner.Jackal or CustomWinner.Pelican)
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                            Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.Lovers))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));

                        }
                        else if (CustomWinnerHolder.WinnerTeam is CustomWinner.CrushLovers or CustomWinner.CupidLovers or CustomWinner.Akujo)
                        {
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lovers);
                            Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.Lovers))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));

                        }
                    }
                }
                if ((CustomRolesHelper.RoleExist(CustomRoles.Honmei) || CustomRolesHelper.RoleExist(CustomRoles.Akujo)) && !reason.Equals(GameOverReason.HumansByTask))
                {
                    if (!(!Main.AkujoLoversPlayers.ToArray().All(p => p.IsAlive())))
                    {
                        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.Impostor or CustomWinner.Jackal or CustomWinner.Pelican)
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Akujo);
                            Main.AllPlayerControls
                                .Where(p => (p.Is(CustomRoles.Honmei)|| p.Is(CustomRoles.Akujo)))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                        }
                        else if (CustomWinnerHolder.WinnerTeam is CustomWinner.Lovers or CustomWinner.CupidLovers or CustomWinner.CrushLovers)
                        {
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Akujo);
                            Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.Honmei) || p.Is(CustomRoles.Akujo))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));

                        }
                    }
                }
                if (CustomRolesHelper.RoleExist(CustomRoles.CrushLovers) && !reason.Equals(GameOverReason.HumansByTask))
                {
                    if (!(!Main.CrushLoversPlayers.ToArray().All(p => p.IsAlive()) && Options.CrushLoverSuicide.GetBool()))
                    {
                        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.Impostor or CustomWinner.Jackal or CustomWinner.Pelican)
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CrushLovers);
                            Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.CrushLovers))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId)); 
                        }
                        else if (CustomWinnerHolder.WinnerTeam is CustomWinner.Lovers or CustomWinner.CupidLovers or CustomWinner.Akujo)
                        {
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.CrushLovers);
                            Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.CrushLovers))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));

                        }
                    }
                }
                else if (CustomRolesHelper.RoleExist(CustomRoles.CupidLovers) && !reason.Equals(GameOverReason.HumansByTask))
                {
                    if (!(!Main.CupidLoversPlayers.ToArray().All(p => p.IsAlive()) && Options.CupidLoverSuicide.GetBool()))
                    {
                        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.Impostor or CustomWinner.Jackal or CustomWinner.Pelican)
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CupidLovers);
                            Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.CupidLovers) || p.Is(CustomRoles.Cupid))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId)); 
                        }
                        else if (CustomWinnerHolder.WinnerTeam is CustomWinner.Lovers or CustomWinner.CrushLovers or CustomWinner.Akujo)
                        {
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.CupidLovers);
                            Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.CupidLovers) || p.Is(CustomRoles.Cupid))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                           
                        }
                    }
                }
                ////反对者抢夺胜利
                //foreach (var pc in Main.AllPlayerControls)
                //{
                //    if (pc.Is(CustomRoles.Dissenter) && (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.Impostor or CustomWinner.Jackal or CustomWinner.Pelican) && (Main.DissenterInProtect[pc.PlayerId] + Options.DissenterDuration.GetInt() >= Utils.GetTimeStamp(DateTime.Now)))
                //    {
                //        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Dissenter);
                //        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                //    }
                //}

                //追加胜利
                foreach (var pc in Main.AllPlayerControls)
                {
                    //Opportunist
                    if (pc.Is(CustomRoles.Opportunist) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Opportunist);
                    }
                    //起诉人
                    if (pc.Is(CustomRoles.Prosecutors) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Prosecutors);
                    }
                   if (Rudepeople.ForRudepeople.Contains(pc.PlayerId))
                    {
                        CustomWinnerHolder.WinnerIds.Remove(pc.PlayerId);
                    }
                    if (Main.ForFake.Contains(pc.PlayerId))
                    {
                        CustomWinnerHolder.WinnerIds.Remove(pc.PlayerId);
                    }
                    //Sunnyboy
                    if (pc.Is(CustomRoles.Sunnyboy) && !pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Sunnyboy);
                    }
                    //自爆卡车来咯
                    if (pc.Is(CustomRoles.Provocateur) && Main.Provoked.TryGetValue(pc.PlayerId, out var tar))
                    {
                        if (!CustomWinnerHolder.WinnerIds.Contains(tar))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Provocateur);
                        }   
                    }
                    //雇佣兵胜利
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.OpportunistKiller) && pc.IsAlive() || player.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isok == true && player.IsAlive())
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.OpportunistKiller);
                        }
                        else if (pc.Is(CustomRoles.OpportunistKiller) && pc.IsAlive() && player.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isok == true && !player.IsAlive())
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.OpportunistKiller);
                        }
                        else if (pc.Is(CustomRoles.OpportunistKiller) && !pc.IsAlive() && player.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isok == true && player.IsAlive())
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.OpportunistKiller);

                        }
                    }
                    if (pc.Is(CustomRoles.Refuser) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Refuser);
                    }
                    if (pc.Is(CustomRoles.AnimalRefuser) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.AnimalRefuser);
                    }
                    if (pc.Is(CustomRoles.UnanimalRefuser) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.UnanimalRefuser);
                    }
                    if (pc.Is(CustomRoles.AttendRefuser) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.AttendRefuser);
                    }
                    if (pc.Is(CustomRoles.CrazyRefuser) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.CrazyRefuser);
                    }
                    if (pc.Is(CustomRoles.ZeyanRefuser) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.ZeyanRefuser);
                    }
                    //自由人免费辣
                    if (pc.Is(CustomRoles.FreeMan) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.FreeMan);
                    }
                    //伪人
                    if (pc.Is(CustomRoles.Fake) && pc.IsAlive() && Main.NeedFake.TryGetValue(pc.PlayerId, out var tr))
                    {
                            if (!CustomWinnerHolder.WinnerIds.Contains(tr))
                            {
                                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Fake);
                            }
                    }
                    //奴隶主晋级辣
                    if (pc.Is(CustomRoles.Slaveowner) && pc.IsAlive() && Main.SlaveownerMax[pc.PlayerId] >= Options.ForSlaveownerSlav.GetInt() && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Slaveowner);
                    }
                }

                //Lovers follow winner
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Lovers and not CustomWinner.Crewmate and not CustomWinner.Impostor)
                {
                    foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Lovers)))
                    {
                        if (CustomWinnerHolder.WinnerIds.Where(x => Utils.GetPlayerById(x).Is(CustomRoles.Lovers)).Count() > 0)
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lovers);
                        }
                    }
                }
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.CrushLovers and not CustomWinner.Crewmate and not CustomWinner.Impostor)
                {
                    foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.CrushLovers)))
                    {
                        if (CustomWinnerHolder.WinnerIds.Where(x => Utils.GetPlayerById(x).Is(CustomRoles.CrushLovers)).Count() > 0)
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.CrushLovers);
                        }
                    }
                }
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.CupidLovers and not CustomWinner.Crewmate and not CustomWinner.Impostor)
                {
                    foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.CupidLovers)))
                    {
                        if (CustomWinnerHolder.WinnerIds.Where(x => Utils.GetPlayerById(x).Is(CustomRoles.CupidLovers)).Count() > 0)
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.CupidLovers);
                        }
                    }
                }
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Akujo and not CustomWinner.Crewmate and not CustomWinner.Impostor)
                {
                    foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Akujo)))
                    {
                        if (CustomWinnerHolder.WinnerIds.Where(x => Main.AkujoLoversPlayers.Contains(Utils.GetPlayerById(x))).Count() > 0)
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Akujo);
                        }
                    }
                }

                //FFF
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Lovers && !CustomWinnerHolder.AdditionalWinnerTeams.Contains(AdditionalWinners.Lovers) && !CustomRolesHelper.RoleExist(CustomRoles.Lovers) && !CustomRolesHelper.RoleExist(CustomRoles.Ntr))
                {
                    foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.FFF)))
                    {
                        if (Main.AllPlayerControls.Where(x => (x.Is(CustomRoles.Lovers) || x.Is(CustomRoles.Ntr) || x.Is(CustomRoles.CrushLovers) || x.Is(CustomRoles.CupidLovers) || x.Is(CustomRoles.Akujo) || x.Is(CustomRoles.Honmei) || x.Is(CustomRoles.Backup)) && x.GetRealKiller()?.PlayerId == pc.PlayerId).Count() > 0)
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.FFF);
                        }
                    }
                }
                //律师
                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Lawyer)))
                {
                    if (Lawyer.Target.TryGetValue(pc.PlayerId, out var lawyertarget) && pc.IsAlive() &&
                        (CustomWinnerHolder.WinnerIds.Contains(lawyertarget) ||
                        (Main.PlayerStates.TryGetValue(lawyertarget, out var ps) && CustomWinnerHolder.WinnerRoles.Contains(ps.MainRole)
                        )))
                    {
                        CustomWinnerHolder.WinnerIds.Remove(lawyertarget);
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lawyer);
                    }
                    else if (Lawyer.Target.TryGetValue(pc.PlayerId, out lawyertarget) && !pc.IsAlive() &&
                        (CustomWinnerHolder.WinnerIds.Contains(lawyertarget) ||
                        (Main.PlayerStates.TryGetValue(lawyertarget, out ps) && CustomWinnerHolder.WinnerRoles.Contains(ps.MainRole)
                        )))
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lawyer);
                    }
                }

                //赌徒
                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Totocalcio)))
                {
                    if (Totocalcio.BetPlayer.TryGetValue(pc.PlayerId, out var betTarget) && (
                        CustomWinnerHolder.WinnerIds.Contains(betTarget) ||
                        (Main.PlayerStates.TryGetValue(betTarget, out var ps) && CustomWinnerHolder.WinnerRoles.Contains(ps.MainRole)
                        )))
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Totocalcio);
                    }
                }

                //中立共同胜利
                if (Options.NeutralWinTogether.GetBool() && CustomWinnerHolder.WinnerIds.Where(x => Utils.GetPlayerById(x) != null && Utils.GetPlayerById(x).GetCustomRole().IsNeutral()).Count() >= 1)
                {
                    foreach (var pc in Main.AllPlayerControls)
                        if (pc.GetCustomRole().IsNeutral() && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                }
                else if (Options.NeutralRoleWinTogether.GetBool())
                {
                    foreach (var id in CustomWinnerHolder.WinnerIds)
                    {
                        var pc = Utils.GetPlayerById(id);
                        if (pc == null || !pc.GetCustomRole().IsNeutral()) continue;
                        foreach (var tar in Main.AllPlayerControls)
                            if (!CustomWinnerHolder.WinnerIds.Contains(tar.PlayerId) && tar.GetCustomRole() == pc.GetCustomRole())
                                CustomWinnerHolder.WinnerIds.Add(tar.PlayerId);
                    }
                }

                //补充恋人胜利名单
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Lovers || CustomWinnerHolder.AdditionalWinnerTeams.Contains(AdditionalWinners.Lovers))
                {
                    Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.Lovers) && !CustomWinnerHolder.WinnerIds.Contains(p.PlayerId))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                }
                //补充恋人胜利名单
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.CrushLovers || CustomWinnerHolder.AdditionalWinnerTeams.Contains(AdditionalWinners.CrushLovers))
                {
                    Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.CrushLovers) && !CustomWinnerHolder.WinnerIds.Contains(p.PlayerId))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                }
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.CupidLovers || CustomWinnerHolder.AdditionalWinnerTeams.Contains(AdditionalWinners.CupidLovers))
                {
                    Main.AllPlayerControls
                                .Where(p => (p.Is(CustomRoles.CupidLovers) || p.Is(CustomRoles.Cupid))&& !CustomWinnerHolder.WinnerIds.Contains(p.PlayerId))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                }

            }
            ShipStatus.Instance.enabled = false;
            StartEndGame(reason);
            predicate = null;
        }
        return false;
    }
    public static void StartEndGame(GameOverReason reason)
    {
        var sender = new CustomRpcSender("EndGameSender", SendOption.Reliable, true);
        sender.StartMessage(-1); // 5: GameData
        MessageWriter writer = sender.stream;

        //ゴーストロール化
        List<byte> ReviveRequiredPlayerIds = new();
        var winner = CustomWinnerHolder.WinnerTeam;
        foreach (var pc in Main.AllPlayerControls)
        {
            if (winner == CustomWinner.Draw)
            {
                SetGhostRole(ToGhostImpostor: true);
                continue;
            }
            bool canWin = CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) ||
                    CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole());
            bool isCrewmateWin = reason.Equals(GameOverReason.HumansByVote) || reason.Equals(GameOverReason.HumansByTask);
            SetGhostRole(ToGhostImpostor: canWin ^ isCrewmateWin);

            void SetGhostRole(bool ToGhostImpostor)
            {
                if (!pc.Data.IsDead) ReviveRequiredPlayerIds.Add(pc.PlayerId);
                if (ToGhostImpostor)
                {
                    Logger.Info($"{pc.GetNameWithRole()}: ImpostorGhostに変更", "ResetRoleAndEndGame");
                    sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                        .Write((ushort)RoleTypes.ImpostorGhost)
                        .EndRpc();
                    pc.SetRole(RoleTypes.ImpostorGhost);
                }
                else
                {
                    Logger.Info($"{pc.GetNameWithRole()}: CrewmateGhostに変更", "ResetRoleAndEndGame");
                    sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                        .Write((ushort)RoleTypes.CrewmateGhost)
                        .EndRpc();
                    pc.SetRole(RoleTypes.Crewmate);
                }
            }
            SetEverythingUpPatch.LastWinsReason = winner is CustomWinner.Crewmate or CustomWinner.Impostor ? GetString($"GameOverReason.{reason}") : "";
        }

        // CustomWinnerHolderの情報の同期
        sender.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame);
        CustomWinnerHolder.WriteTo(sender.stream);
        sender.EndRpc();

        // GameDataによる蘇生処理
        writer.StartMessage(1); // Data
        {
            writer.WritePacked(GameData.Instance.NetId); // NetId
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (ReviveRequiredPlayerIds.Contains(info.PlayerId))
                {
                    // 蘇生&メッセージ書き込み
                    info.IsDead = false;
                    writer.StartMessage(info.PlayerId);
                    info.Serialize(writer);
                    writer.EndMessage();
                }
            }
            writer.EndMessage();
        }

        sender.EndMessage();

        // バニラ側のゲーム終了RPC
        writer.StartMessage(8); //8: EndGame
        {
            writer.Write(AmongUsClient.Instance.GameId); //GameId
            writer.Write((byte)reason); //GameoverReason
            writer.Write(false); //showAd
        }
        writer.EndMessage();

        sender.SendMessage();
    }

    public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();
    public static void SetPredicateToSoloKombat() => predicate = new SoloKombatGameEndPredicate();
    public static void SetPredicateToHotPotato() => predicate = new HotPotatomeEndPredicate();
    public static void SetPredicateToTheLivingDaylights() => predicate = new HotPotatomeEndPredicate(); 

    // ===== ゲーム終了条件 =====
    // 通常ゲーム用
    class NormalGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
            if (CheckGameEndByLivingPlayers(out reason)) return true;
            if (CheckGameEndByTask(out reason)) return true;
            if (CheckGameEndBySabotage(out reason)) return true;

            return false;
        }

        public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;

            if (CustomRolesHelper.RoleExist(CustomRoles.Sunnyboy) && Main.AllAlivePlayerControls.Count() > 1) return false;

            int Imp = Utils.AlivePlayersCount(CountTypes.Impostor);
            int Jackal = Utils.AlivePlayersCount(CountTypes.Jackal);
            int Pel = Utils.AlivePlayersCount(CountTypes.Pelican);
            int Crew = Utils.AlivePlayersCount(CountTypes.Crew);
            int Gam = Utils.AlivePlayersCount(CountTypes.Gamer);
            int BK = Utils.AlivePlayersCount(CountTypes.BloodKnight);
            int CM = Utils.AlivePlayersCount(CountTypes.Succubus);
            int YinLang = Utils.AlivePlayersCount(CountTypes.YinLang);
            int PG = Utils.AlivePlayersCount(CountTypes.PlaguesGod);
            int Si = Utils.AlivePlayersCount(CountTypes.Yandere);
            int Loners = Utils.AlivePlayersCount(CountTypes.Loners);
            int Meditator = Utils.AlivePlayersCount(CountTypes.Meditator);

            Imp += Main.AllAlivePlayerControls.Count(x => x.GetCustomRole().IsImpostor() && x.Is(CustomRoles.DualPersonality));
            Crew += Main.AllAlivePlayerControls.Count(x => x.GetCustomRole().IsCrewmate() && x.Is(CustomRoles.DualPersonality) && x.Is(CustomRoles.EIReverso));
            CM += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Charmed) && x.Is(CustomRoles.DualPersonality));
            Jackal += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Attendant) && x.Is(CustomRoles.DualPersonality));
            YinLang += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.DualPersonality));
            PG += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.DualPersonality));
            Si += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Yandere) || Main.ForYandere.Contains(x.PlayerId));
            Loners += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.DualPersonality));
            Meditator += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.DualPersonality));

            if (Imp == 0 && Crew == 0 && Jackal == 0 && Pel == 0 && Gam == 0 && BK == 0 && CM == 0 && YinLang == 0 && PG == 0) //全灭
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
            }
            else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers))) //恋人胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
            }
            else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.CrushLovers))) //暗恋恋人胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CrushLovers);
            }
            else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.CupidLovers))) //丘比特恋人胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CupidLovers);
            }
            else if (Main.AllAlivePlayerControls.All(p => Main.AkujoLoversPlayers.Contains(p))) 
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Akujo);
            }
            else if (Jackal == 0 && Pel == 0 && Gam == 0 && BK == 0 && CM == 0 && Crew <= Imp && YinLang == 0 && PG == 0 && Si == 0 && Loners == 0 && Meditator == 0) //内鬼胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
            }
            else if (Imp == 0 && Pel == 0 && Gam == 0 && BK == 0 && CM == 0 && Crew <= Jackal && YinLang == 0 && PG == 0 && Si == 0 && Loners == 0 && Meditator == 0) //豺狼胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
            }
            else if (Imp == 0 && Pel == 0 && Gam == 0 && BK == 0 && CM == 0 && Crew <= YinLang && PG == 0 && Si == 0 && Loners == 0 && Meditator == 0) //银狼胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.YinLang);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.YinLang);
            }
            else if (Imp == 0 && Jackal == 0 && Gam == 0 && BK == 0 && CM == 0 && Crew <= Pel && YinLang == 0 && PG == 0 && Si == 0 && Loners == 0 && Meditator == 0) //鹈鹕胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Pelican);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Pelican);
            }
            else if (Imp == 0 && Jackal == 0 && Pel == 0 && BK == 0 && CM == 0 && Crew <= Gam && YinLang == 0 && PG == 0 && Si == 0 && Loners == 0 && Meditator == 0) //玩家胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Gamer);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Gamer);
            }
            else if (Imp == 0 && Jackal == 0 && Pel == 0 && Gam == 0 && CM == 0 && Crew <= BK && YinLang == 0 && PG == 0 && Si == 0 && Loners == 0 && Meditator == 0) //嗜血骑士胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BloodKnight);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.BloodKnight);
            }
            else if (Imp == 0 && Jackal == 0 && Pel == 0 && Gam == 0 && BK == 0 && Crew <= CM && YinLang == 0 && PG == 0 && Si == 0 && Loners == 0 && Meditator == 0) //魅惑者胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Succubus);
            }
            else if (Jackal == 0 && Pel == 0 && Imp == 0 && BK == 0 && Gam == 0 && CM == 0 && YinLang == 0 && PG == 0 && Si==0 && Loners == 0 && Meditator == 0) //船员胜利
            {
                reason = GameOverReason.HumansByVote;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
            }
            else if (Jackal == 0 && Pel == 0 && Imp == 0 && BK == 0 && Si == 0 && Gam == 0 && Crew <= PG && CM == 0 && YinLang == 0 && Loners ==0 && Meditator == 0) //万疫胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.PlaguesGod);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.PlaguesGod);
            }
            else if (Jackal < Si && Pel < Si && Imp < Si && BK < Si && Gam < Si && Crew < Si && CM < Si && YinLang < Si  && Loners< Si && Meditator < Si) //胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Yandere);
            }
            else if (Jackal == 0 && Pel == 0 && Imp == 0 && BK == 0 && Si == 0 && Gam == 0 && Crew <= Loners && CM == 0 && YinLang == 0 && Meditator==0) //孤独胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Loners);
            }
            else if (Jackal == 0 && Pel == 0 && Imp == 0 && BK == 0 && Si == 0 && Gam == 0 && Crew <= Meditator && CM == 0 && YinLang == 0) //冥想胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Meditator);
            }
            


            else return false; //胜利条件未达成

            return true;
        }
    }
    // 热土豆用
    class HotPotatomeEndPredicate : GameEndPredicate
    {
        
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (CustomWinnerHolder.WinnerIds.Count > 0) return false;
            if (CheckGameEndByColdPotatoPlayers(out reason)) return true;
            return false;
        }

        public bool CheckGameEndByColdPotatoPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            foreach (var player in Main.AllAlivePlayerControls)
            {
                var pcList = Main.AllAlivePlayerControls.ToList();
                if (pcList.Count == 1)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CP);
                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                }
            }
            Main.DoBlockNameChange = true;

            return true;
        }
    }
    // 个人竞技模式用
    class SoloKombatGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (CustomWinnerHolder.WinnerIds.Count > 0) return false;
            if (CheckGameEndByLivingPlayers(out reason)) return true;
            return false;
        }

        public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;

            if (SoloKombatManager.RoundTime > 0) return false;

            var list = Main.AllPlayerControls.Where(x => !x.Is(CustomRoles.GM) && SoloKombatManager.GetRankOfScore(x.PlayerId) == 1);
            var winner = list.FirstOrDefault();

            CustomWinnerHolder.WinnerIds = new()
            {
                winner.PlayerId
            };

            Main.DoBlockNameChange = true;

            return true;
        }
    }
}

public abstract class GameEndPredicate
{
    /// <summary>ゲームの終了条件をチェックし、CustomWinnerHolderに値を格納します。</summary>
    /// <params name="reason">バニラのゲーム終了処理に使用するGameOverReason</params>
    /// <returns>ゲーム終了の条件を満たしているかどうか</returns>
    public abstract bool CheckForEndGame(out GameOverReason reason);

    /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
    public virtual bool CheckGameEndByTask(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorByKill;
        if (Options.DisableTaskWin.GetBool() || TaskState.InitialTotalTasks == 0) return false;

        if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
        {
            reason = GameOverReason.HumansByTask;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
            return true;
        }
        return false;
    }
    /// <summary>ShipStatus.Systems内の要素をもとにサボタージュ勝利が可能かを判定します。</summary>
    public virtual bool CheckGameEndBySabotage(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorByKill;
        if (ShipStatus.Instance.Systems == null) return false;

        // TryGetValueは使用不可
        var systems = ShipStatus.Instance.Systems;
        LifeSuppSystemType LifeSupp;
        if (systems.ContainsKey(SystemTypes.LifeSupp) && // サボタージュ存在確認
            (LifeSupp = systems[SystemTypes.LifeSupp].TryCast<LifeSuppSystemType>()) != null && // キャスト可能確認
            LifeSupp.Countdown < 0f) // タイムアップ確認
        {
            // 酸素サボタージュ
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
            reason = GameOverReason.ImpostorBySabotage;
            LifeSupp.Countdown = 10000f;
            return true;
        }

        ISystemType sys = null;
        if (systems.ContainsKey(SystemTypes.Reactor)) sys = systems[SystemTypes.Reactor];
        else if (systems.ContainsKey(SystemTypes.Laboratory)) sys = systems[SystemTypes.Laboratory];

        ICriticalSabotage critical;
        if (sys != null && // サボタージュ存在確認
            (critical = sys.TryCast<ICriticalSabotage>()) != null && // キャスト可能確認
            critical.Countdown < 0f) // タイムアップ確認
        {
            // リアクターサボタージュ
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
            reason = GameOverReason.ImpostorBySabotage;
            critical.ClearSabotage();
            return true;
        }

        return false;
    }
}