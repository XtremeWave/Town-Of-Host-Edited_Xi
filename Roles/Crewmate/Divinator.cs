using System.Collections.Generic;
using static TheOtherRoles_Host.Options;
using static TheOtherRoles_Host.Translator;

namespace TheOtherRoles_Host.Roles.Crewmate;

public static class Divinator
{
    private static readonly int Id = 8022560;
    private static List<byte> playerIdList = new();

    private static OptionItem CheckLimitOpt;
    private static OptionItem AccurateCheckMode;
    public static OptionItem HideVote;

    public static List<byte> didVote = new();
    private static Dictionary<byte, int> CheckLimit = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Divinator);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 10, "DivinatorSkillLimit", new(1, 990, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Divinator])
            .SetValueFormat(OptionFormat.Times);
        AccurateCheckMode = BooleanOptionItem.Create(Id + 12, "AccurateCheckMode", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Divinator]);
        HideVote = BooleanOptionItem.Create(Id + 14, "DivinatorHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Divinator]);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Divinator);
    }
    public static void Init()
    {
        playerIdList = new();
        CheckLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CheckLimit.TryAdd(playerId, CheckLimitOpt.GetInt());
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void OnVote(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return;
        if (didVote.Contains(player.PlayerId)) return;
        didVote.Add(player.PlayerId);

        if (CheckLimit[player.PlayerId] < 1)
        {
            Utils.SendMessage(GetString("DivinatorCheckReachLimit"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Divinator), GetString("DivinatorCheckMsgTitle")));
            return;
        }

        CheckLimit[player.PlayerId]--;

        if (player.PlayerId == target.PlayerId)
        {
            Utils.SendMessage(GetString("DivinatorCheckSelfMsg") + "\n\n" + string.Format(GetString("DivinatorCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Divinator), GetString("DivinatorCheckMsgTitle")));
            return;
        }

        string msg;

        if (player.AllTasksCompleted() || AccurateCheckMode.GetBool())
        {
            msg = string.Format(GetString("DivinatorCheck.TaskDone"), target.GetRealName(), GetString(target.GetCustomRole().ToString()));
        }
        else
        {
            string text = target.GetCustomRole() switch
            {
                CustomRoles.TimeThief or
                CustomRoles.AntiAdminer or
                CustomRoles.SuperStar or
                CustomRoles.Mayor or
                CustomRoles.Snitch or
                CustomRoles.Counterfeiter or
                CustomRoles.God or
                CustomRoles.Judge or
                CustomRoles.Observer or
                CustomRoles.DovesOfNeace
                => "HideMsg",

                CustomRoles.Miner or
                CustomRoles.Scavenger or
                CustomRoles.Luckey or
                CustomRoles.Needy or
                CustomRoles.SabotageMaster or
                CustomRoles.Jackal or
                CustomRoles.Mario or
                CustomRoles.Cleaner or
                CustomRoles.Crewpostor or
                 CustomRoles.SpecialAgent or
                 CustomRoles.BadLuck or
                 CustomRoles.Followers or
                 CustomRoles.Mascot
                => "Honest",

                CustomRoles.SerialKiller or
                CustomRoles.BountyHunter or
                CustomRoles.Minimalism or
                CustomRoles.Sans or
                CustomRoles.SpeedBooster or
                CustomRoles.Sheriff or
                CustomRoles.Arsonist or
                CustomRoles.Innocent or
                CustomRoles.FFF or
                CustomRoles.Greedier or
                CustomRoles.Rudepeople or
                CustomRoles.FreeMan or
                CustomRoles.Vandalism
                => "Impulse",

                CustomRoles.Vampire or
                CustomRoles.Assassin or
                CustomRoles.Escapee or
                CustomRoles.Sniper or
                CustomRoles.SwordsMan or
                CustomRoles.Bodyguard or
                CustomRoles.Opportunist or
                CustomRoles.Pelican or
                CustomRoles.ImperiusCurse or
                CustomRoles.OpportunistKiller or
                CustomRoles.Vulture or
                CustomRoles.FreeMan
                => "Weirdo",

                CustomRoles.EvilGuesser or
                CustomRoles.Bomber or
                CustomRoles.Capitalism or
                CustomRoles.NiceGuesser or
                CustomRoles.Grenadier or
                CustomRoles.Terrorist or
                CustomRoles.Revolutionist or
                CustomRoles.Gamer or
                CustomRoles.Eraser or
                CustomRoles.EvilGambler or
                CustomRoles.StinkyAncestor
                => "Blockbuster",

                CustomRoles.Warlock or
                CustomRoles.Hacker or
                CustomRoles.Mafia or
                CustomRoles.Doctor or
                CustomRoles.Transporter or
                CustomRoles.Veteran or
                CustomRoles.Divinator or
                CustomRoles.QuickShooter or
                CustomRoles.Mediumshiper or
                CustomRoles.Judge or
                CustomRoles.BloodKnight or
                CustomRoles.Fraudster or
                CustomRoles.Cultivator or
                CustomRoles.Bull or
                CustomRoles.Prophet or
                CustomRoles.Scout or
                CustomRoles.Deputy or
                CustomRoles.DemonHunterm or
                CustomRoles.TimeStops or
                CustomRoles.Grenadiers or
                CustomRoles.PlagueDoctor or
                CustomRoles.NiceSwapper or
                CustomRoles.EvilSwapper
                => "Strong",

                CustomRoles.Witch or
                CustomRoles.Puppeteer or
                CustomRoles.ShapeMaster or
                CustomRoles.Paranoia or
                CustomRoles.Psychic or
                CustomRoles.Executioner or
                CustomRoles.BallLightning or
                CustomRoles.Workaholic or
                CustomRoles.Provocateur or
                CustomRoles.Lawyer or
                CustomRoles.Prosecutors or
                CustomRoles.Masochism or
                CustomRoles.Yandere
                => "Incomprehensible",

                CustomRoles.FireWorks or
                CustomRoles.EvilTracker or
                CustomRoles.Gangster or
                CustomRoles.Dictator or
                CustomRoles.CyberStar or
                CustomRoles.Collector or
                CustomRoles.Sunnyboy or
                CustomRoles.Bard or
                CustomRoles.Totocalcio
                => "Enthusiasm",

                CustomRoles.BoobyTrap or
                CustomRoles.Zombie or
                CustomRoles.Mare or
                CustomRoles.Detective or
                CustomRoles.TimeManager or
                CustomRoles.Jester or
                CustomRoles.Medic or
                CustomRoles.DarkHide or
                CustomRoles.CursedWolf or
                CustomRoles.OverKiller or
                CustomRoles.Hangman or
                CustomRoles.Mortician or
                CustomRoles.LostCrew or
                 CustomRoles.XiaoMu or
                 CustomRoles.Disorder or
                 CustomRoles.Prophet
                => "Disturbed",

                CustomRoles.Glitch or
                CustomRoles.Concealer or
                CustomRoles.Swooper
                => "Glitch",

                CustomRoles.Succubus
                => "Love",

                CustomRoles.Captain or
                CustomRoles.Solicited
                => "Captain",


                _ => "None",
            };
            msg = string.Format(GetString("DivinatorCheck." + text), target.GetRealName());
        }

        Utils.SendMessage(GetString("DivinatorCheck") + "\n" + msg + "\n\n" + string.Format(GetString("DivinatorCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Divinator), GetString("DivinatorCheckMsgTitle")));
    }
}