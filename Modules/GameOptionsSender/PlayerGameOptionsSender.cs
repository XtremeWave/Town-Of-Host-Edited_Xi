using AmongUs.GameOptions;
using Hazel;
using Il2CppSystem.Linq;
using InnerNet;
using MS.Internal.Xml.XPath;
using System.Linq;
using TOHEXI.Roles.Impostor;
using TOHEXI.Roles.Neutral;
using static Logger;
using static TOHEXI.Translator;
using Mathf = UnityEngine.Mathf;
using TOHEXI.Roles.Crewmate;
//using UnityEngine;
using System.Threading.Tasks;
using TOHEXI.Modules;
using TOHEXI.Roles.AddOns.Crewmate;
using static UnityEngine.GraphicsBuffer;

namespace TOHEXI.Modules;

public class PlayerGameOptionsSender : GameOptionsSender
{
    public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
    public static void SetDirty(byte playerId) =>
        AllSenders.OfType<PlayerGameOptionsSender>()
        .Where(sender => sender.player.PlayerId == playerId)
        .ToList().ForEach(sender => sender.SetDirty());
    public static void SetDirtyToAll() =>
        AllSenders.OfType<PlayerGameOptionsSender>()
        .ToList().ForEach(sender => sender.SetDirty());

    public override IGameOptions BasedGameOptions =>
        Main.RealOptionsData.Restore(new NormalGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
    public override bool IsDirty { get; protected set; }

    public PlayerControl player;

    public PlayerGameOptionsSender(PlayerControl player)
    {
        this.player = player;
    }
    public void SetDirty() => IsDirty = true;

    public override void SendGameOptions()
    {
        if (player.AmOwner)
        {
            var opt = BuildGameOptions();
            foreach (var com in GameManager.Instance.LogicComponents)
            {
                if (com.TryCast<LogicOptions>(out var lo))
                    lo.SetGameOptions(opt);
            }
            GameOptionsManager.Instance.CurrentGameOptions = opt;
        }
        else base.SendGameOptions();
    }

    public override void SendOptionsArray(byte[] optionArray)
    {
        for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
        {
            if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
            {
                SendOptionsArray(optionArray, i, player.GetClientId());
            }
        }
    }
    public static void RemoveSender(PlayerControl player)
    {
        var sender = AllSenders.OfType<PlayerGameOptionsSender>()
        .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
        if (sender == null) return;
        sender.player = null;
        AllSenders.Remove(sender);
    }
    public override IGameOptions BuildGameOptions()
    {
        if (Main.RealOptionsData == null)
        {
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
        }

        var opt = BasedGameOptions;
        AURoleOptions.SetOpt(opt);
        var state = Main.PlayerStates[player.PlayerId];
        opt.BlackOut(state.IsBlackOut);

        CustomRoles role = player.GetCustomRole();
        switch (role.GetCustomRoleTypes())
        {
            case CustomRoleTypes.Impostor:
                AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                break;
        }

        switch (role)
        {
            case CustomRoles.Terrorist:
            case CustomRoles.SabotageMaster:
            case CustomRoles.Mario:
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.ShapeMaster:
                AURoleOptions.ShapeshifterCooldown = 0f;
                AURoleOptions.ShapeshifterLeaveSkin = false;
                AURoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                break;
            case CustomRoles.MimicAss:
                AURoleOptions.ShapeshifterCooldown = 0f;
                AURoleOptions.ShapeshifterLeaveSkin = false;
                AURoleOptions.ShapeshifterDuration = 999f;
                break;
            case CustomRoles.Warlock:
                AURoleOptions.ShapeshifterCooldown = Main.isCursed ? 1f : Options.DefaultKillCooldown;
                break;
            case CustomRoles.SerialKiller:
                SerialKiller.ApplyGameOptions(player);
                break;
            case CustomRoles.BountyHunter:
                BountyHunter.ApplyGameOptions();
                break;
            case CustomRoles.Sheriff:
            case CustomRoles.SwordsMan:
            case CustomRoles.Arsonist:
            case CustomRoles.Minimalism:
            case CustomRoles.Innocent:
            case CustomRoles.Shifter:
            case CustomRoles.Pelican:
            case CustomRoles.Revolutionist:
            case CustomRoles.Medic:
            case CustomRoles.Prophet:
            case CustomRoles.Scout:
            case CustomRoles.Deputy:
            case CustomRoles.DemonHunterm:
            case CustomRoles.Hunter:
            case CustomRoles.Captain:
            case CustomRoles.Provocateur:
            case CustomRoles.BSR:
            case CustomRoles.Prosecutors:
            case CustomRoles.Lawyer:
            case CustomRoles.NiceTracker:
            case CustomRoles.Knight:
            case CustomRoles.Merchant:
            case CustomRoles.Yandere:
            case CustomRoles.PlagueDoctor:
                opt.SetVision(false);
                break;
            case CustomRoles.Zombie:
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.2f);
                break;
            case CustomRoles.Doctor:
                AURoleOptions.ScientistCooldown = 0f;
                AURoleOptions.ScientistBatteryCharge = Options.DoctorTaskCompletedBatteryCharge.GetFloat();
                break;
            case CustomRoles.Mayor:
                AURoleOptions.EngineerCooldown =
                    !Main.MayorUsedButtonCount.TryGetValue(player.PlayerId, out var count) || count < Options.MayorNumOfUseButton.GetInt()
                    ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Injured:
                AURoleOptions.EngineerCooldown = Options.InjuredVentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.Paranoia:
                AURoleOptions.EngineerCooldown =
                    !Main.ParaUsedButtonCount.TryGetValue(player.PlayerId, out var count2) || count2 < Options.ParanoiaNumOfUseButton.GetInt()
                    ? Options.ParanoiaVentCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.sabcat:
                AURoleOptions.EngineerCooldown =
                    !Main.sabcatNumOfUsed.TryGetValue(player.PlayerId, out var count6) || count6 > 0
                    ? Options.sabcatKillCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Mare:
                Mare.ApplyGameOptions(player.PlayerId);
                break;
            case CustomRoles.EvilTracker:
                EvilTracker.ApplyGameOptions(player.PlayerId);
                break;
            case CustomRoles.Jackal:
                Jackal.ApplyGameOptions(opt);
                break;
            case CustomRoles.YinLang:
                bool YL_canUse = YinLang.YLCanVent.GetBool();
                DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(YL_canUse && !player.Data.IsDead);
                player.Data.Role.CanVent = YL_canUse;
                break;
            case CustomRoles.Veteran:
                AURoleOptions.EngineerCooldown =
                    !Main.VeteranNumOfUsed.TryGetValue(player.PlayerId, out var count3) || count3 > 0
                    ? Options.VeteranSkillCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Grenadier:
                AURoleOptions.EngineerCooldown = Options.GrenadierSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.FFF:
                opt.SetVision(true);
                break;
            case CustomRoles.Gamer:
                Gamer.ApplyGameOptions(opt);
                break;
            case CustomRoles.DarkHide:
                DarkHide.ApplyGameOptions(opt);
                break;
            case CustomRoles.Workaholic:
                AURoleOptions.EngineerCooldown = Options.WorkaholicVentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.ImperiusCurse:
                AURoleOptions.ShapeshifterCooldown = Options.ImperiusCurseShapeshiftCooldown.GetFloat();
                AURoleOptions.ShapeshifterLeaveSkin = false;
                AURoleOptions.ShapeshifterDuration = Options.ShapeImperiusCurseShapeshiftDuration.GetFloat();
                break;
            case CustomRoles.QuickShooter:
                AURoleOptions.ShapeshifterCooldown = QuickShooter.ShapeshiftCooldown.GetFloat();
                break;
            case CustomRoles.Concealer:
                Concealer.ApplyGameOptions();
                break;
            case CustomRoles.Hacker:
                Hacker.ApplyGameOptions();
                break;
            case CustomRoles.Hangman:
                Hangman.ApplyGameOptions();
                break;
            case CustomRoles.Sunnyboy:
                AURoleOptions.ScientistCooldown = 0f;
                AURoleOptions.ScientistBatteryCharge = 60f;
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.ApplyGameOptions(opt);
                break;

            case CustomRoles.DovesOfNeace:
                AURoleOptions.EngineerCooldown =
                    !Main.DovesOfNeaceNumOfUsed.TryGetValue(player.PlayerId, out var count4) || count4 > 0
                    ? Options.DovesOfNeaceCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Rudepeople:
                Rudepeople.SetCooldown(player.PlayerId);
                AURoleOptions.EngineerInVentMaxTime = 1f;
                break;
            case CustomRoles.Vulture:
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.StinkyAncestor:
                AURoleOptions.EngineerCooldown = Options.StinkyAncestorSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.TimeStops:
                AURoleOptions.EngineerCooldown = Options.TimeStopsSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Whoops:
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.TimeMaster:
                AURoleOptions.EngineerCooldown = Options.TimeMasterSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.GlennQuagmire:
                AURoleOptions.EngineerCooldown = Options.GlennQuagmireSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.SoulSeeker:
                AURoleOptions.EngineerCooldown = Options.SoulSeekerCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Anglers:
                AURoleOptions.ShapeshifterCooldown = Options.AnglersShapeshifterCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = 0.3f;
                break;
            case CustomRoles.Assassin:
                AURoleOptions.ShapeshifterCooldown = Options.AssassinateCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = 0.3f;
                break;
            case CustomRoles.Buried:
                Buried.ApplyGameOptions();
                break;
            case CustomRoles.Henry:
                Henry.ApplyGameOptions();
                break;
            case CustomRoles.Chameleon:
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.Disperser:
                Disperser.ApplyGameOptions();
                break;
            case CustomRoles.Sleeve:
                AURoleOptions.ShapeshifterCooldown = Options.SleeveCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.SleeveshifterMax.GetFloat();
                break;
            case CustomRoles.Medusa:
                AURoleOptions.ShapeshifterCooldown = Options.MedusaCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = 1f;
                break;
            case CustomRoles.Cluster:
                AURoleOptions.ShapeshifterCooldown = Options.ClusterCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = 1f;
                break;
            case CustomRoles.Forger:
                AURoleOptions.ShapeshifterCooldown = Options.ForgerCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = 1f;
                break;
            case CustomRoles.Blackmailer:
                Blackmailer.ApplyGameOptions();
                break;
            case CustomRoles.Spiritualists:
                AURoleOptions.EngineerCooldown = Options.SpiritualistsVentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = Options.SpiritualistsVentMaxCooldown.GetFloat();
                break;
            case CustomRoles.Batter:
                AURoleOptions.ShapeshifterCooldown = Options.BatterKillCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = 1f;
                break;
            case CustomRoles.SoulSucker:
                SoulSucker.ApplyGameOptions();
                break;
            case CustomRoles.Plumber:
                AURoleOptions.EngineerCooldown = Options.PlumberCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1f;
                break;
            case CustomRoles.Guardian:
                AURoleOptions.ProtectionDurationSeconds = 114514;
                AURoleOptions.GuardianAngelCooldown = Options.GuardianCooldown.GetFloat();
                AURoleOptions.ImpostorsCanSeeProtect = false;
                break;
          //  case CustomRoles.Locksmith:
         //       AURoleOptions.EngineerCooldown = Options.LocksmithCooldown.GetFloat();
            //    AURoleOptions.EngineerInVentMaxTime = 1f;
            //    break; 
        }

        // 为迷幻者的凶手
        if (Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Bewilder) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId && !x.Is(CustomRoles.Hangman)).Count() > 0)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Options.BewilderVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.BewilderVision.GetFloat());
            player.RpcSetCustomRole(CustomRoles.Bewilder);
            player.SyncSettings();
        }
        // 为漫步者的凶手
        if (Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Rambler) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId).Count() > 0)
        {
            Main.AllPlayerSpeed[player.PlayerId] = Options.RamblerSpeed.GetFloat();
            player.RpcSetCustomRole(CustomRoles.Rambler);
            player.SyncSettings();
        }
        // 为患者的凶手
        if (Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Diseased) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId).Count() > 0)
        {
            Main.AllPlayerKillCooldown[player.PlayerId] *= Options.DiseasedMultiplier.GetFloat();
            player.ResetKillCooldown();
            player.SyncSettings();
            player.RpcSetCustomRole(CustomRoles.Diseased);
        }
        // 为银狼的凶手
        if (Main.AllPlayerControls.Where(x => x.Is(CustomRoles.YinLang) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId).Count() > 0)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, 0f);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0f);
        }
        // 投掷傻瓜蛋啦！！！！！
        if (
            (Main.GrenadierBlinding.Count >= 1 && (player.GetCustomRole().IsImpostor() || player.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isimp ||(player.GetCustomRole().IsNeutral() && Options.GrenadierCanAffectNeutral.GetBool()))) || (Main.MadGrenadierBlinding.Count >= 1 && !player.GetCustomRole().IsImpostorTeam() && !player.Is(CustomRoles.Madmate)))
        {
            {
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, Options.GrenadierCauseVision.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.GrenadierCauseVision.GetFloat());
            }
        }
        // 闪光弹
        if ((Main.GrenadiersInProtect.Count >= 1 && Main.ForGrenadiers.Contains(player.PlayerId) && (player.GetCustomRole().IsCrewmate())))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, 0);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0);
        }

        foreach (var subRole in Main.PlayerStates[player.PlayerId].SubRoles)
        {
            switch (subRole)
            {
                case CustomRoles.Watcher:
                    opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
                case CustomRoles.Flashman:
                    Main.AllPlayerSpeed[player.PlayerId] = Options.FlashmanSpeed.GetFloat();
                    break;
                case CustomRoles.Lighter:
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Options.LighterVision.GetFloat());
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.LighterVision.GetFloat());
                    break;
                case CustomRoles.Bewilder:
                    opt.SetVision(false);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Options.BewilderVision.GetFloat());
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.BewilderVision.GetFloat());
                    break;
                case CustomRoles.Reach:
                    opt.SetInt(Int32OptionNames.KillDistance, 2);
                    break;
                case CustomRoles.Rambler:
                    Main.AllPlayerSpeed[player.PlayerId] = Options.RamblerSpeed.GetFloat();
                    break;
                case CustomRoles.LostSouls:
                    Main.AllPlayerSpeed[player.PlayerId] = 0.5f;
                    opt.SetFloat(FloatOptionNames.CrewLightMod, 0.5f);
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.5f);
                    break;
                case CustomRoles.Signal:
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (GameStates.IsInTask && pc.Is(CustomRoles.Signal))
                        {
                            Main.SignalLocation.Remove(pc.PlayerId);
                            Main.SignalLocation.Add(pc.PlayerId, pc.GetTruePosition());
                        }
                    }
                    break;
            }
        }

        // 当工程跳管冷却为0时无法正常显示图标
        AURoleOptions.EngineerCooldown = Mathf.Max(0.01f, AURoleOptions.EngineerCooldown);

        if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
        {
            AURoleOptions.KillCooldown = Mathf.Max(0.01f, killCooldown);
        }

        if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
        {
            AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 3f);
        }

        state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
        if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead)
            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
        if (Options.AdditionalEmergencyCooldown.GetBool() &&
            Options.AdditionalEmergencyCooldownThreshold.GetInt() <= Utils.AllAlivePlayersCount)
        {
            opt.SetInt(
                Int32OptionNames.EmergencyCooldown,
                Options.AdditionalEmergencyCooldownTime.GetInt());
        }
        if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetValue() <= Options.UsedButtonCount)
        {
            opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
        }
        MeetingTimeManager.ApplyGameOptions(opt);

        AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
        AURoleOptions.ProtectionDurationSeconds = 0f;

        return opt;
    }

    public override bool AmValid()
    {
        return base.AmValid() && player != null && !player.Data.Disconnected && Main.RealOptionsData != null;
    }
}