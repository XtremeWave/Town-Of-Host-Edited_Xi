using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TheOtherRoles_Host.Roles.GameModsRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOtherRoles_Host.Modules;
using TheOtherRoles_Host.Modules.ChatManager;
using TheOtherRoles_Host.Roles.AddOns.Crewmate;
using TheOtherRoles_Host.Roles.AddOns.Impostor;
using TheOtherRoles_Host.Roles.Crewmate;
using TheOtherRoles_Host.Roles.Double;
using TheOtherRoles_Host.Roles.Impostor;
using TheOtherRoles_Host.Roles.Neutral;
using static TheOtherRoles_Host.Modules.CustomRoleSelector;
using static TheOtherRoles_Host.Translator;
using MS.Internal.Xml.XPath;
using TheOtherRoles_Host.GameMode;

namespace TheOtherRoles_Host;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
internal class ChangeRoleSettings
{
    public static void Postfix(AmongUsClient __instance)
    {
        Main.OverrideWelcomeMsg = "";
        try
        {
            //注:この時点では役職は設定されていません。
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
            if (Options.DisableVanillaRoles.GetBool())
            {
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
            }
            //初始化值
            Main.PlayerStates = new();

            Main.ChattyMax = new();
            Main.AllPlayerKillCooldown = new();
            Main.AllPlayerSpeed = new();
            Main.AllPlayerLocation = new();
            Main.WarlockTimer = new();
            Main.AssassinTimer = new();
            Main.isDoused = new();
            Main.isDraw = new();
            Main.ArsonistTimer = new();
            Main.RevolutionistTimer = new();
            Main.RevolutionistStart = new();
            Main.RevolutionistLastTime = new();
            Main.RevolutionistCountdown = new();
            Main.CursedPlayers = new();
            Main.MafiaRevenged = new();
            Main.isCurseAndKill = new();
            Main.isCursed = false;
            Main.PuppeteerList = new();
            Main.DetectiveNotify = new();
            Main.ManipulatorNotify = new();
            Main.CyberStarDead = new();
            Main.CaptainDead = new();
            Main.BoobyTrapBody = new();
            Main.KillerOfBoobyTrapBody = new();
            Main.CleanerBodies = new();
            Main.VultureBodies = new();
            Main.DeputyInProtect = new();
            Main.ProsecutorsInProtect = new();
            Main.TimeStopsstop = new();
            Main.TimeMasterbacktrack = new();
            Main.Spiritualistsbacktrack = new();
            Main.SignalLocation = new();
            Main.DemolitionManiacKill = new();
            Main.CrushMax = new();
            Main.CupidShieldList = new();
            Main.HunterMax = new();
            Main.ForCluster = new();
            Main.SlaveownerMax = new();
            Main.ForSlaveowner = new();
            Main.SpellmasterMax = new();
            Main.LastEnteredVent = new();
            Main.IsShapeShifted = new();
            Main.ForMimicKiller = new();
    Main.LastEnteredVentLocation = new();
            Main.EscapeeLocation = new();
            Main.ForSpellmaster = new();
            Main.SoulSeekerCanKill = new();
          Main.SoulSeekerNotCanKill = new();
        Main.SoulSeekerCanEat = new();
            Main.SoulSeekerDead = new();
            Main.JealousyMax = new();
            Main.ForET = new();
            Main.ForHotPotato = new();
            Main.MaxCowboy = new();
            Main.ForHotCold = new();
            Main.KillDeathGhost = new();
            Main.ForExorcist = new();
            Main.ExorcistMax = new();
            Main.ForDemolition = new();
            Main.ForDestinyChooser = new();
            Main.ForTasksDestinyChooser = new();
            Main.ForLostDeadDestinyChooser = new();
            Main.ForHemophobia = new();
            Main.ManipulatorImpotors = new();
            Main.ManipulatorCrewmate = new();
            Main.ManipulatorNeutral = new();
            Main.GuideMax = new();
            Main.ForYandere = new();
            Main.NeedKillYandere = new();

            Main.AfterMeetingDeathPlayers = new();
            Main.ResetCamPlayerList = new();
            Main.clientIdList = new();
            Main.ForNiceTracker = new();

            Main.CapitalismAddTask = new();
            Main.MerchantTaskMax = 114514;
            Main.MerchantMax = new();
            Main.CapitalismAssignTask = new();
            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();
            Main.SpeedBoostTarget = new();
            Main.MayorUsedButtonCount = new();
            Main.ParaUsedButtonCount = new();
            Main.MarioVentCount = new();
            Main.VeteranInProtect = new();
            Main.VeteranNumOfUsed = new();
            Main.GrenadierBlinding = new();
            Main.MadGrenadierBlinding = new();
            Main.CursedWolfSpellCount = new();
            Main.OverDeadPlayerList = new();
            Main.Provoked = new();
            Main.ShieldPlayer = Options.ShieldPersonDiedFirst.GetBool() ? Main.FirstDied : byte.MaxValue;
            Main.FirstDied = byte.MaxValue;
            Main.MadmateNum = 0;
            Main.BardCreations = 0;
            Main.DovesOfNeaceNumOfUsed = new();
            Main.TimeMasterNum = new();
            Main.VultureEatMax = new();
            Main.BullKillMax = new();
            Main.CultivatorKillMax = new();
            Main.DisorderKillCooldownMax = new();
            Main.StinkyAncestorKill = new();
            Main.ScoutImpotors = new();
            Main.ScoutCrewmate = new();
            Main.ScoutNeutral = new();
            Main.TimeStopsstop = new();
            Main.ForGrenadiers = new();
            Main.isjackalDead = false;
            Main.DoubleKillerMax = new();
            Main.FakeMath = new();
            Main.NotKIller = new();
            Main.FakeMax = new();


    Main.isSheriffDead = false;
            Main.isCrushLoversDead = false;
            Main.isCupidLoversDead = false;
            Main.isAkujoLoversDead = false;
            Main.isseniormanagementDead = false;
            Main.CupidComplete = false;
            Main.ForSpiritualists = new();
            Main.isMKDead = false;
            Main.isHunterDead = false;
            Main.TomKill = new();
            ReportDeadBodyPatch.CanReport = new();
            Main.KilledDiseased = new();
            Main.ForJealousy = new();
            Main.ForSourcePlague = new();
            Main.ForKing = new();
            Main.KingCanpc = new();
            Main.KingCanKill = new();
            Main.ForKnight = new();
            Main.NnurseHelepMax = new();
            Main.ForSqueezers = new();
            Main.TasksSqueezers = new();
            Main.KillForCorpse = new();
            Main.PGuesserMax = new();
            Main.HangTheDevilKiller = new();
            Main.ForHangTheDevil = new();
            Main.ForMagnetMan = new();

            Main.RefuserShields = 0;

            Options.UsedButtonCount = 0;

            GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor = false;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            Main.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

            MeetingTimeManager.Init();
            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();

            Main.currentDousingTarget = byte.MaxValue;
            Main.currentDrawTarget = byte.MaxValue;
            Main.PlayerColors = new();

            //名前の記録
            //Main.AllPlayerNames = new();
            RPC.SyncAllPlayerNames();

            Camouflage.Init();
            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Count() != 0)
            {
                var msg = GetString("Error.InvalidColor");
                Logger.SendInGame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

            foreach (var target in Main.AllPlayerControls)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                var colorId = pc.Data.DefaultOutfit.ColorId;
                if (AmongUsClient.Instance.AmHost && Options.FormatNameMode.GetInt() == 1) pc.RpcSetName(Palette.GetColorName(colorId));
                Main.PlayerStates[pc.PlayerId] = new(pc.PlayerId);
                Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;

                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.NumOfTP.Add(pc.PlayerId, 0);
                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                Main.clientIdList.Add(pc.GetClientId());
            }
            Main.DyingTurns = 0;
            Main.VisibleTasksCount = true;
            ChatManager.cancel = false;
            Main.NiceSwapSend = false;
            Main.EvilSwapSend = false;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                Main.RefixCooldownDelay = 0;
            }
            FallFromLadder.Reset();
            BountyHunter.Init();
            SerialKiller.Init();
            FireWorks.Init();
            Sniper.Init();
            TimeThief.Init();
            Mare.Init();
            Witch.Init();
            SabotageMaster.Init();
            Executioner.Init();
            Jackal.Init();
            SchrodingerCat.Init();
            Sheriff.Init();
            SwordsMan.Init();
            EvilTracker.Init();
            Snitch.Init();
         //   NiceTracker.Init();
            Vampire.Init();
            AntiAdminer.Init();
            TimeManager.Init();
            LastImpostor.Init();
            TargetArrow.Init();
            Mini.Init();
            LocateArrow.Init();
            DoubleTrigger.Init();
            Workhorse.Init();
            Pelican.Init();
            Counterfeiter.Init();
            Gangster.Init();
            Medic.Init();
            Gamer.Init();
            BallLightning.Init();
            DarkHide.Init();
            Greedier.Init();
            Collector.Init();
            QuickShooter.Init();
            Concealer.Init();
            Divinator.Init();
            Eraser.Init();
            Sans.Init();
            Hacker.Init();
            Psychic.Init();
            Hangman.Init();
            Judge.Init();
            Mortician.Init();
            Mediumshiper.Init();
            Swooper.Init();
            BloodKnight.Init();
            Totocalcio.Init();
            Succubus.Init();
            Vulture.Init();
            Prophet.Init();
            Scout.Init();
            Deputy.Init();
            DemonHunterm.Init();
            Vandalism.Init();
            Captain.Init();
            Lawyer.Init();
            Prosecutors.Init();
            BSR.Init();
            ElectOfficials.Init();
            ChiefOfPolice.Init();
            Knight.Init();
            Corpse.Init();
            DoubleKiller.Init();
            EvilGambler.Init();
            Merchant.Init();
            NiceTracker.Init();
            Yandere.Init();
            Buried.Init();
            PlagueDoctor.Init();
            Henry.Init();
            Chameleon.Init();
          //  Kidnapper.Init();
            Mimics.Init();
            ShapeShifters.Init();
            NiceSwapper.Init();
            EvilSwapper.Init();
            Blackmailer.Init();
            RewardOfficer.Init();
            Copycat.Init();
            Loners.Init();
            MrDesperate.Init();
            Meditator.Init();
            Challenger.Init();
            BloodSeekers.Init();
            SoulSucker.Init();
                SoloKombatManager.Init();
            HotPotatoManager.Init();
            TheLivingDaylights.Init();
            Holdpotato.Init();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            NameNotifyManager.Reset();
            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Change Role Setting Postfix");
            Logger.Fatal(ex.ToString(), "Change Role Setting Postfix");
        }
    }
}
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
internal class SelectRolesPatch
{
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        try
        {
            //CustomRpcSenderとRpcSetRoleReplacerの初期化
            Dictionary<byte, CustomRpcSender> senders = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            if (Options.EnableGM.GetBool())
            {
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                PlayerControl.LocalPlayer.Data.IsDead = true;
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            SelectCustomRoles();
            SelectAddonRoles();
            CalculateVanillaRoleCount();

            //指定原版特殊职业数量
            var roleOpt = Main.NormalOptions.roleOptions;
            int ScientistNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Scientist);
            roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum + addScientistNum, addScientistNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Scientist));
            int EngineerNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Engineer);
            roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum + addEngineerNum, addEngineerNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Engineer));
            int ShapeshifterNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
            roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum + addShapeshifterNum, addShapeshifterNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

            Dictionary<(byte, byte), RoleTypes> rolesMap = new();

            // 注册反职业
            foreach (var kv in RoleResult.Where(x => x.Value.IsDesyncRole()))
                AssignDesyncRole(kv.Value, kv.Key, senders, rolesMap, BaseRole: kv.Value.GetDYRole());

            foreach (var cp in RoleResult.Where(x => x.Value == CustomRoles.Crewpostor))
                AssignDesyncRole(cp.Value, cp.Key, senders, rolesMap, BaseRole: RoleTypes.Crewmate, hostBaseRole: RoleTypes.Impostor);
            foreach (var sa in RoleResult.Where(x => x.Value == CustomRoles.SpecialAgent))
                    AssignDesyncRole(sa.Value, sa.Key, senders, rolesMap, BaseRole: RoleTypes.Crewmate, hostBaseRole: RoleTypes.Impostor);

            MakeDesyncSender(senders, rolesMap);

        }
        catch (Exception e)
        {
            Utils.ErrorEnd("Select Role Prefix");
            Logger.Fatal(e.Message, "Select Role Prefix");
        }
        //以下、バニラ側の役職割り当てが入る
    }

    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        try
        {
            List<(PlayerControl, RoleTypes)> newList = new();
            foreach (var sd in RpcSetRoleReplacer.StoragedData)
            {
                var kp = RoleResult.Where(x => x.Key.PlayerId == sd.Item1.PlayerId).FirstOrDefault();
                if (kp.Value.IsDesyncRole() || kp.Value == CustomRoles.Crewpostor)
                {
                    Logger.Warn($"反向原版职业 => {sd.Item1.GetRealName()}: {sd.Item2}", "Override Role Select");
                    continue;
                }
                if (kp.Value.IsDesyncRole() || kp.Value == CustomRoles.SpecialAgent)
                {
                    Logger.Warn($"反向原版职业 => {sd.Item1.GetRealName()}: {sd.Item2}", "Override Role Select");
                    continue;
                }
                newList.Add((sd.Item1, kp.Value.GetRoleTypes()));
                if (sd.Item2 == kp.Value.GetRoleTypes())
                    Logger.Warn($"注册原版职业 => {sd.Item1.GetRealName()}: {sd.Item2}", "Override Role Select");
                else
                    Logger.Warn($"覆盖原版职业 => {sd.Item1.GetRealName()}: {sd.Item2} => {kp.Value.GetRoleTypes()}", "Override Role Select");
            }
            if (Options.EnableGM.GetBool()) newList.Add((PlayerControl.LocalPlayer, RoleTypes.Crewmate));
            RpcSetRoleReplacer.StoragedData = newList;

            RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

            // 不要なオブジェクトの削除
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.OverriddenSenderList = null;
            RpcSetRoleReplacer.StoragedData = null;

            //Utils.ApplySuffix();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する
                if (Main.PlayerStates[pc.PlayerId].MainRole != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ
                var role = CustomRoles.NotAssigned;
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        role = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Impostor:
                        role = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Scientist:
                        role = CustomRoles.Scientist;
                        break;
                    case RoleTypes.Engineer:
                        role = CustomRoles.Engineer;
                        break;
                    case RoleTypes.GuardianAngel:
                        role = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Shapeshifter:
                        role = CustomRoles.Shapeshifter;
                        break;
                    default:
                        Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
                Main.PlayerStates[pc.PlayerId].SetMainRole(role);
            }

            // 个人竞技模式用
            if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
            {
                foreach (var pair in Main.PlayerStates)
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                goto EndOfSelectRolePatch;
            }
            // 热土豆用
            if (Options.CurrentGameMode == CustomGameMode.HotPotato)
            {
                foreach (var pair in Main.PlayerStates)
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                goto EndOfSelectRolePatch;
            }
            if (Options.CurrentGameMode == CustomGameMode.TheLivingDaylights)
            {
                foreach (var pair in Main.PlayerStates)
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                goto EndOfSelectRolePatch;
            }

            var rd = IRandom.Instance;

            foreach (var kv in RoleResult)
            {
                if (kv.Value.IsDesyncRole()) continue;
                AssignCustomRole(kv.Value, kv.Key);
            }

            if (CustomRoles.Lovers.IsEnable() && (CustomRoles.FFF.IsEnable() ? -1 : rd.Next(1, 100)) <= Options.LoverSpawnChances.GetInt()) AssignLoversRolesFromList();
            foreach (var role in AddonRolesList)
            {
                if (rd.Next(1, 100) <= (Options.CustomAdtRoleSpawnRate.TryGetValue(role, out var sc) ? sc.GetFloat() : 0))
                    if (role.IsEnable()) AssignSubRoles(role);
            }



            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.Data.Role.Role == RoleTypes.Shapeshifter) Main.CheckShapeshift.Add(pc.PlayerId, false);
                switch (pc.GetCustomRole())
                {
                    case CustomRoles.BountyHunter:
                        BountyHunter.Add(pc.PlayerId);
                        break;
                    case CustomRoles.SerialKiller:
                        SerialKiller.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Witch:
                        Witch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Warlock:
                        Main.CursedPlayers.Add(pc.PlayerId, null);
                        Main.isCurseAndKill.Add(pc.PlayerId, false);
                        break;
                    case CustomRoles.FireWorks:
                        FireWorks.Add(pc.PlayerId);
                        break;
                    case CustomRoles.TimeThief:
                        TimeThief.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sniper:
                        Sniper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mare:
                        Mare.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Vampire:
                        Vampire.Add(pc.PlayerId);
                        break;
                    case CustomRoles.SwordsMan:
                        SwordsMan.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Arsonist:
                        foreach (var ar in Main.AllPlayerControls)
                            Main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                        break;
                    case CustomRoles.Revolutionist:
                        foreach (var ar in Main.AllPlayerControls)
                            Main.isDraw.Add((pc.PlayerId, ar.PlayerId), false);
                        break;
                    case CustomRoles.Executioner:
                        Executioner.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Jackal:
                        Jackal.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sheriff:
                        Sheriff.Add(pc.PlayerId);
                        break;
                    case CustomRoles.QuickShooter:
                        QuickShooter.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mayor:
                        Main.MayorUsedButtonCount[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Paranoia:
                        Main.ParaUsedButtonCount[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.SabotageMaster:
                        SabotageMaster.Add(pc.PlayerId);
                        break;
                    case CustomRoles.EvilTracker:
                        EvilTracker.Add(pc.PlayerId);
                        break;
              //      case CustomRoles.NiceTracker:
                        //NiceTracker.Add(pc.PlayerId);
               //         break;
                    case CustomRoles.Snitch:
                        Snitch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.AntiAdminer:
                        AntiAdminer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mario:
                        Main.MarioVentCount[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.TimeManager:
                        TimeManager.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pelican:
                        Pelican.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Counterfeiter:
                        Counterfeiter.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Gangster:
                        Gangster.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Medic:
                        Medic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.SchrodingerCat:
                        SchrodingerCat.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Divinator:
                        Divinator.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Gamer:
                        Gamer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.BallLightning:
                        BallLightning.Add(pc.PlayerId);
                        break;
                    case CustomRoles.DarkHide:
                        DarkHide.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Greedier:
                        Greedier.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Collector:
                        Collector.Add(pc.PlayerId);
                        break;
                    case CustomRoles.CursedWolf:
                        Main.CursedWolfSpellCount[pc.PlayerId] = Options.GuardSpellTimes.GetInt();
                        break;
                    case CustomRoles.Concealer:
                        Concealer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Eraser:
                        Eraser.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sans:
                        Sans.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Hacker:
                        Hacker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Psychic:
                        Psychic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Hangman:
                        Hangman.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Judge:
                        Judge.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mortician:
                        Mortician.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mediumshiper:
                        Mediumshiper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Veteran:
                        Main.VeteranNumOfUsed.Add(pc.PlayerId, Options.VeteranSkillMaxOfUseage.GetInt());
                        break;
                    case CustomRoles.Swooper:
                        Swooper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.BloodKnight:
                        BloodKnight.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Totocalcio:
                        Totocalcio.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Succubus:
                        Succubus.Add(pc.PlayerId);
                        break;
                    case CustomRoles.DovesOfNeace:
                        Main.DovesOfNeaceNumOfUsed.Add(pc.PlayerId, Options.DovesOfNeaceMaxOfUseage.GetInt());
                        break;
                    case CustomRoles.Rudepeople:
                        Rudepeople.Add(pc.PlayerId);
                        break;
                    case CustomRoles.TimeMaster:
                        Main.TimeMasterNum[pc.PlayerId] = 0;
                       break;
                    case CustomRoles.Vulture:
                        Vulture.Add(pc.PlayerId);
                        Main.VultureEatMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Bull:
                        Main.BullKillMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Masochism:
                        Main.MasochismKillMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Cultivator:
                        Main.CultivatorKillMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Disorder:
                        Main.DisorderKillCooldownMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Prophet:
                        Prophet.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Scout:
                        Scout.Add(pc.PlayerId);
                        Main.ScoutImpotors[pc.PlayerId] = 0;
                        Main.ScoutCrewmate[pc.PlayerId] = 0;
                        Main.ScoutNeutral[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.StinkyAncestor:
                        Main.StinkyAncestorKill[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Deputy:
                        Deputy.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Prosecutors:
                        Prosecutors.Add(pc.PlayerId);
                        break;
                    case CustomRoles.DemonHunterm:
                        DemonHunterm.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Vandalism:
                        Vandalism.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Lawyer:
                        Lawyer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sidekick:
                        Jackal.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Hunter:
                        Main.HunterMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Crush:
                        Main.CrushMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.PlagueDoctor:
                        PlagueDoctor.Add(pc.PlayerId);
                        PlagueDoctor.CanInfectInt[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Cupid:
                        Main.CupidMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Akujo:
                        Main.AkujoMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Slaveowner:
                        Main.SlaveownerMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Spellmaster:
                        Main.SpellmasterMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.SoulSeeker:
                        Main.SoulSeekerCanKill[pc.PlayerId] = 0;
                        Main.SoulSeekerNotCanKill[pc.PlayerId] = 0;
                        Main.SoulSeekerCanEat[pc.PlayerId] = 0;
                        Main.SoulSeekerDead[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Jealousy:
                        Main.JealousyMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Captain:
                        Captain.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Solicited:
                        Captain.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Cowboy:
                        Main.MaxCowboy[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.ElectOfficials:
                        ElectOfficials.Add(pc.PlayerId);
                        break;
                    case CustomRoles.BSR:
                        BSR.Add(pc.PlayerId);
                        break;
                    case CustomRoles.ChiefOfPolice:
                        ChiefOfPolice.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Exorcist:
                        Main.ExorcistMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Manipulator:
                        Main.ManipulatorImpotors[pc.PlayerId] = 0;
                        Main.ManipulatorCrewmate[pc.PlayerId] = 0;
                        Main.ManipulatorNeutral[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Guide:
                        Main.GuideMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Knight:
                        Knight.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Nurse:
                        Main.NnurseHelepMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Corpse:
                        Corpse.Add(pc.PlayerId);
                        break;
                    case CustomRoles.NiceGuesser:
                        Main.PGuesserMax[pc.PlayerId] = 1;
                        break;
                    case CustomRoles.EvilGuesser:
                        Main.PGuesserMax[pc.PlayerId] = 1;
                        break;
                    case CustomRoles.DoubleKiller:
                        DoubleKiller.Add(pc.PlayerId);                        
                        new LateTask(() =>
                        {
                            Main.DoubleKillerKillSeacond.Add(pc.PlayerId, Utils.GetTimeStamp());
                            Utils.NotifyRoles();
                        }, 5f, ("shuangdao"));
                        break;
                    case CustomRoles.EvilGambler:
                        EvilGambler.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Merchant:
                        Merchant.Add(pc.PlayerId);
                        Main.MerchantMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.NiceTracker:
                        NiceTracker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Yandere:
                        Yandere.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Buried:
                        Buried.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Henry:
                        Henry.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Chameleon:
                        Chameleon.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Hotpotato:
                       Holdpotato.Add(pc.PlayerId);
                      pc.NotifyV2(string.Format(GetString("HotPotatoTimeRemain"),HotPotatoManager.BoomTimes )); ;
                        break;
                    case CustomRoles.Coldpotato:
                        pc.NotifyV2(string.Format(GetString("HotPotatoTimeRemain"), HotPotatoManager.BoomTimes)); ;
                        break;
                    //       case CustomRoles.Kidnapper:
                    //           Kidnapper.Add(pc.PlayerId);
                    //          break;
                    case CustomRoles.MimicKiller:
                        Mimics.Add(pc.PlayerId);
                        break;
                        case CustomRoles.ShapeShifters:
                        ShapeShifters.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Fake:
                        Main.FakeMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.NiceSwapper:
                        NiceSwapper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.EvilSwapper:
                        EvilSwapper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Blackmailer:
                        Blackmailer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Tom:
                        Main.TomKill[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.RewardOfficer:
                        RewardOfficer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Copycat:
                        Copycat.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Chatty:
                        Main.ChattyMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Loners:
                        Loners.Add(pc.PlayerId);
                        break;
                    case CustomRoles.MrDesperate:
                        MrDesperate.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Meditator:
                        Meditator.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Challenger:
                        Challenger.Add(pc.PlayerId);
                        break;
                    case CustomRoles.BloodSeekers:
                        BloodSeekers.Add(pc.PlayerId);
                        break;
                    case CustomRoles.SoulSucker:
                        SoulSucker.Add(pc.PlayerId);
                        break;
                }
                foreach (var subRole in pc.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        // ここに属性のAddを追加
                        default:
                            break;
                    }
                }
            }

        EndOfSelectRolePatch:

            HudManager.Instance.SetHudActive(true);

            foreach (var pc in Main.AllPlayerControls)
                pc.ResetKillCooldown();

            //役職の人数を戻す
            var roleOpt = Main.NormalOptions.roleOptions;
            int ScientistNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Scientist);
            ScientistNum -= addScientistNum;
            roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum, roleOpt.GetChancePerGame(RoleTypes.Scientist));
            int EngineerNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Engineer);
            EngineerNum -= addEngineerNum;
            roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));
            int ShapeshifterNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
            ShapeshifterNum -= addShapeshifterNum;
            roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.Standard:
                    GameEndChecker.SetPredicateToNormal();
                    break;
                case CustomGameMode.SoloKombat:
                    GameEndChecker.SetPredicateToSoloKombat();
                    break;
                case CustomGameMode.HotPotato:
                    GameEndChecker.SetPredicateToHotPotato();
                    break;
                case CustomGameMode.TheLivingDaylights:
                    GameEndChecker.SetPredicateToTheLivingDaylights();
                    break;
            }

            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }

            // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
            Main.ResetCamPlayerList.AddRange(Main.AllPlayerControls.Where(p => p.GetCustomRole() is CustomRoles.Arsonist or CustomRoles.Revolutionist or CustomRoles.Crewpostor or CustomRoles.KB_Normal).Select(p => p.PlayerId));
            Utils.CountAlivePlayers(true);
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Select Role Postfix");
            Logger.Fatal(ex.ToString(), "Select Role Postfix");
        }
    }
    private static void AssignDesyncRole(CustomRoles role, PlayerControl player, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        if (player == null) return;

        var hostId = PlayerControl.LocalPlayer.PlayerId;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);

        var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
        var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

        //Desync役職視点
        foreach (var target in Main.AllPlayerControls)
            rolesMap[(player.PlayerId, target.PlayerId)] = player.PlayerId != target.PlayerId ? othersRole : selfRole;

        //他者視点
        foreach (var seer in Main.AllPlayerControls.Where(x => player.PlayerId != x.PlayerId))
            rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;

        RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
        //ホスト視点はロール決定
        player.SetRole(othersRole);
        player.Data.IsDead = true;

        Logger.Info($"注册模组职业：{player?.Data?.PlayerName} => {role}", "AssignRoles");
    }
    public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            var sender = senders[seer.PlayerId];
            foreach (var target in Main.AllPlayerControls)
            {
                if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var role))
                {
                    sender.RpcSetRole(seer, role, target.GetClientId());
                }
            }
        }
    }

    private static void AssignCustomRole(CustomRoles role, PlayerControl player)
    {
        if (player == null) return;
        SetColorPatch.IsAntiGlitchDisabled = true;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);
        Logger.Info($"注册模组职业：{player?.Data?.PlayerName} => {role}", "AssignRoles");

        SetColorPatch.IsAntiGlitchDisabled = false;
    }

    private static void AssignLoversRolesFromList()
    {
        if (CustomRoles.Lovers.IsEnable())
        {
            //Loversを初期化
            Main.LoversPlayers.Clear();
            Main.isLoversDead = false;
            
            
            //ランダムに2人選出
            AssignLoversRoles(2);
        }
        if (CustomRoles.Crush.IsEnable())
        {
            Main.CrushLoversPlayers.Clear();
            Main.isCrushLoversDead = false;
        }
        if (CustomRoles.Cupid.IsEnable())
        {
            Main.CupidLoversPlayers.Clear();
            Main.isCupidLoversDead = false;
            Main.CupidComplete = false;
        }
        if (CustomRoles.Akujo.IsEnable())
        {
            Main.AkujoLoversPlayers.Clear();
            Main.isAkujoLoversDead = false;
        }
    }

    private static void AssignLoversRoles(int RawCount = -1)
    {
        var allPlayers = new List<PlayerControl>();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.Is(CustomRoles.GM) || (pc.HasSubRole() && Options.LimitAddonsNum.GetBool()) && pc.GetCustomSubRoles().Count >= Options.AddonsNumMax.GetInt() || pc.Is(CustomRoles.Needy) || pc.Is(CustomRoles.Ntr) || pc.Is(CustomRoles.God) || pc.Is(CustomRoles.FFF) || pc.Is(CustomRoles.Captain) || pc.Is(CustomRoles.Believer) || pc.Is(CustomRoles.Crush) || pc.Is(CustomRoles.Cupid) || pc.Is(CustomRoles.Akujo)) continue;
            allPlayers.Add(pc);
        }
        var role = CustomRoles.Lovers;
        var rd = IRandom.Instance;
        var count = Math.Clamp(RawCount, 0, allPlayers.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
        if (count <= 0) return;
        for (var i = 0; i < count; i++)
        {
            var player = allPlayers[rd.Next(0, allPlayers.Count)];
            Main.LoversPlayers.Add(player);
            allPlayers.Remove(player);
            Main.PlayerStates[player.PlayerId].SetSubRole(role);
            Logger.Info("注册恋人:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "AssignLovers");
        }
        RPC.SyncLoversPlayers();
    }
    private static void AssignSubRoles(CustomRoles role, int RawCount = -1)
    {
        var allPlayers = Main.AllAlivePlayerControls.Where(x => CustomRolesHelper.CheckAddonConfilct(role, x)).ToList();
        var count = Math.Clamp(RawCount, 0, allPlayers.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
        if (count <= 0) return;
        for (var i = 0; i < count; i++)
        {
            var player = allPlayers[IRandom.Instance.Next(0, allPlayers.Count)];
            Main.PlayerStates[player.PlayerId].SetSubRole(role);
            Logger.Info("注册附加职业:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "Assign " + role.ToString());
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    private class RpcSetRoleReplacer
    {
        public static bool doReplace = false;
        public static Dictionary<byte, CustomRpcSender> senders;
        public static List<(PlayerControl, RoleTypes)> StoragedData = new();
        // 役職Desyncなど別の処理でSetRoleRpcを書き込み済みなため、追加の書き込みが不要なSenderのリスト
        public static List<CustomRpcSender> OverriddenSenderList;
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
        {
            if (doReplace && senders != null)
            {
                StoragedData.Add((__instance, roleType));
                return false;
            }
            else return true;
        }
        public static void Release()
        {
            foreach (var sender in senders)
            {
                if (OverriddenSenderList.Contains(sender.Value)) continue;
                if (sender.Value.CurrentState != CustomRpcSender.State.InRootMessage)
                    throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

                foreach (var pair in StoragedData)
                {
                    pair.Item1.SetRole(pair.Item2);
                    sender.Value.AutoStartRpc(pair.Item1.NetId, (byte)RpcCalls.SetRole, Utils.GetPlayerById(sender.Key).GetClientId())
                        .Write((ushort)pair.Item2)
                        .EndRpc();
                }
                sender.Value.EndMessage();
            }
            doReplace = false;
        }
        public static void StartReplace(Dictionary<byte, CustomRpcSender> senders)
        {
            RpcSetRoleReplacer.senders = senders;
            StoragedData = new();
            OverriddenSenderList = new();
            doReplace = true;
        }
    }
    
}