using AmongUs.GameOptions;
using System.Linq;
using System.Security.Authentication;
using TheOtherRoles_Host.Roles.Impostor;
using TheOtherRoles_Host.Roles.Neutral;
using static UnityEngine.GraphicsBuffer;

namespace TheOtherRoles_Host;

internal static class CustomRolesHelper
{
    public static CustomRoles GetVNRole(this CustomRoles role) // 对应原版职业
    {
        return role.IsVanilla()
            ? role
            : role switch
            {
                CustomRoles.Sniper => CustomRoles.Shapeshifter,
                CustomRoles.Jester => CustomRoles.Crewmate,
                CustomRoles.Mayor => Options.MayorHasPortableButton.GetBool() && !Options.UsePets.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate,
                CustomRoles.Opportunist => CustomRoles.Crewmate,
                CustomRoles.Snitch => CustomRoles.Crewmate,
                CustomRoles.SabotageMaster => CustomRoles.Engineer,
                CustomRoles.Mafia => CustomRoles.Impostor,
                CustomRoles.Terrorist => CustomRoles.Engineer,
                CustomRoles.Executioner => CustomRoles.Crewmate,
                CustomRoles.Vampire => CustomRoles.Impostor,
                CustomRoles.BountyHunter => CustomRoles.Shapeshifter,
                CustomRoles.Witch => CustomRoles.Impostor,
                CustomRoles.ShapeMaster => CustomRoles.Shapeshifter,
                CustomRoles.Warlock => CustomRoles.Shapeshifter,
                CustomRoles.SerialKiller => CustomRoles.Shapeshifter,
                CustomRoles.FireWorks => CustomRoles.Shapeshifter,
                CustomRoles.SpeedBooster => CustomRoles.Crewmate,
                CustomRoles.Dictator => CustomRoles.Crewmate,
                CustomRoles.Mare => CustomRoles.Impostor,
                CustomRoles.Doctor => CustomRoles.Scientist,
                CustomRoles.Puppeteer => CustomRoles.Impostor,
                CustomRoles.TimeThief => CustomRoles.Impostor,
                CustomRoles.EvilTracker => CustomRoles.Shapeshifter,
                CustomRoles.Paranoia => CustomRoles.Engineer,
                CustomRoles.Miner => CustomRoles.Shapeshifter,
                CustomRoles.Psychic => CustomRoles.Crewmate,
                CustomRoles.Needy => CustomRoles.Crewmate,
                CustomRoles.SuperStar => CustomRoles.Crewmate,
                CustomRoles.Hacker => CustomRoles.Shapeshifter,
                CustomRoles.Assassin => CustomRoles.Shapeshifter,
                CustomRoles.Luckey => CustomRoles.Crewmate,
                CustomRoles.CyberStar => CustomRoles.Crewmate,
                CustomRoles.Escapee => CustomRoles.Shapeshifter,
                CustomRoles.NiceGuesser => CustomRoles.Crewmate,
                CustomRoles.EvilGuesser => CustomRoles.Impostor,
                CustomRoles.Minimalism => CustomRoles.Impostor,
                CustomRoles.God => CustomRoles.Crewmate,
                CustomRoles.Zombie => CustomRoles.Impostor,
                CustomRoles.Mario => CustomRoles.Engineer,
                CustomRoles.AntiAdminer => CustomRoles.Impostor,
                CustomRoles.Sans => CustomRoles.Impostor,
                CustomRoles.Bomber => CustomRoles.Impostor,
                CustomRoles.BoobyTrap => CustomRoles.Impostor,
                CustomRoles.Scavenger => CustomRoles.Impostor,
                CustomRoles.Transporter => CustomRoles.Crewmate,
                CustomRoles.Veteran => Options.UsePets.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate,
                CustomRoles.Capitalism => CustomRoles.Impostor,
                CustomRoles.Bodyguard => CustomRoles.Crewmate,
                CustomRoles.Grenadier => CustomRoles.Engineer,
                CustomRoles.Gangster => CustomRoles.Impostor,
                CustomRoles.Cleaner => CustomRoles.Impostor,
                CustomRoles.Konan => CustomRoles.Crewmate,
                CustomRoles.Divinator => CustomRoles.Crewmate,
                CustomRoles.BallLightning => CustomRoles.Impostor,
                CustomRoles.Greedier => CustomRoles.Impostor,
                CustomRoles.Workaholic => CustomRoles.Engineer,
                CustomRoles.CursedWolf => CustomRoles.Impostor,
                CustomRoles.Collector => CustomRoles.Crewmate,
                CustomRoles.Glitch => CustomRoles.Crewmate,
                CustomRoles.ImperiusCurse => CustomRoles.Shapeshifter,
                CustomRoles.QuickShooter => CustomRoles.Shapeshifter,
                CustomRoles.Concealer => CustomRoles.Shapeshifter,
                CustomRoles.Eraser => CustomRoles.Impostor,
                CustomRoles.OverKiller => CustomRoles.Impostor,
                CustomRoles.Hangman => CustomRoles.Shapeshifter,
                CustomRoles.Sunnyboy => CustomRoles.Scientist,
                CustomRoles.Judge => CustomRoles.Crewmate,
                CustomRoles.Mortician => CustomRoles.Crewmate,
                CustomRoles.Mediumshiper => CustomRoles.Crewmate,
                CustomRoles.Bard => CustomRoles.Impostor,
                CustomRoles.Swooper => CustomRoles.Impostor,
                CustomRoles.Crewpostor => CustomRoles.Crewmate,
                CustomRoles.Observer => CustomRoles.Crewmate,
                CustomRoles.DovesOfNeace => CustomRoles.Engineer,
                CustomRoles.Depressed => CustomRoles.Impostor,
                CustomRoles.LostCrew => CustomRoles.Crewmate,
                CustomRoles.Rudepeople => CustomRoles.Engineer,
                CustomRoles.SpecialAgent => CustomRoles.Crewmate,
                CustomRoles.HatarakiMan => CustomRoles.Crewmate,
                CustomRoles.EvilGambler => CustomRoles.Impostor,
                //CustomRoles.Hypnotist => CustomRoles.Crewmate,
                CustomRoles.Fraudster => CustomRoles.Impostor,
                CustomRoles.XiaoMu => CustomRoles.Crewmate,
                CustomRoles.OpportunistKiller => CustomRoles.Impostor,
                CustomRoles.Vulture => CustomRoles.Engineer,
                CustomRoles.MengJiangGirl => CustomRoles.Crewmate,
                CustomRoles.Indomitable => CustomRoles.Crewmate,
                CustomRoles.Bull => CustomRoles.Crewmate,
                CustomRoles.Masochism => CustomRoles.Crewmate,
                CustomRoles.Dissenter => CustomRoles.Engineer,
                CustomRoles.Cultivator => CustomRoles.Impostor,
                CustomRoles.BadLuck => CustomRoles.Crewmate,
                CustomRoles.FreeMan => CustomRoles.Crewmate,
                CustomRoles.Disorder => CustomRoles.Impostor,
                CustomRoles.NiceShields => CustomRoles.Crewmate,
                CustomRoles.YinLang => CustomRoles.Impostor,
                CustomRoles.Amnesiac => CustomRoles.Shapeshifter,
                CustomRoles.Shifter => CustomRoles.Impostor,
                CustomRoles.TimeStops => CustomRoles.Engineer,
                CustomRoles.Vandalism => CustomRoles.Impostor,
                CustomRoles.Followers => CustomRoles.Impostor,
                CustomRoles.Whoops => CustomRoles.Engineer,
                CustomRoles.Sidekick => CustomRoles.Impostor,
                CustomRoles.TimeMaster => Options.UsePets.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate,
                CustomRoles.DemolitionManiac => CustomRoles.Impostor,
                CustomRoles.SuperPowers => CustomRoles.Crewmate,
                CustomRoles.sabcat => CustomRoles.Impostor,
                CustomRoles.Spellmaster => CustomRoles.Impostor,
                CustomRoles.GlennQuagmire => CustomRoles.Engineer,
                CustomRoles.EIReverso => CustomRoles.Crewmate,
                CustomRoles.SoulSeeker => CustomRoles.Engineer,
                CustomRoles.Wronged => CustomRoles.Crewmate,
                CustomRoles.Solicited => CustomRoles.Crewmate,
                CustomRoles.Undercover => CustomRoles.Crewmate,
                CustomRoles.Mascot => CustomRoles.Crewmate,
                CustomRoles.Cowboy => CustomRoles.Crewmate,
                CustomRoles.Fugitive => CustomRoles.Crewmate,
                CustomRoles.DestinyChooser => CustomRoles.Impostor,
                CustomRoles.Hemophobia => CustomRoles.Impostor,
                CustomRoles.Manipulator => CustomRoles.Crewmate,
                CustomRoles.Spiritualizer => CustomRoles.Crewmate,
                CustomRoles.Guide => CustomRoles.Impostor,
                CustomRoles.Anglers => CustomRoles.Shapeshifter,
                CustomRoles.Nurse => CustomRoles.Crewmate,
                CustomRoles.Strikers => CustomRoles.Impostor,
                CustomRoles.Squeezers => CustomRoles.Impostor,
                CustomRoles.Corpse => CustomRoles.Impostor,
                CustomRoles.Defector => CustomRoles.Impostor,
                CustomRoles.NiceMini => CustomRoles.Crewmate,
                CustomRoles.EvilMini => CustomRoles.Impostor,
                CustomRoles.DoubleKiller => CustomRoles.Impostor,
                CustomRoles.Injured => CustomRoles.Engineer,
                CustomRoles.Buried => CustomRoles.Engineer,
                CustomRoles.Chameleon => CustomRoles.Engineer,
                CustomRoles.Disperser => CustomRoles.Shapeshifter,
                //CustomRoles.Kidnapper => CustomRoles.Impostor,
                CustomRoles.Sleeve => CustomRoles.Shapeshifter,
                CustomRoles.MimicKiller => CustomRoles.Impostor,
                CustomRoles.MimicAss => CustomRoles.Shapeshifter,
                CustomRoles.Medusa => CustomRoles.Shapeshifter,
                CustomRoles.Cluster => CustomRoles.Shapeshifter,
                CustomRoles.Forger => CustomRoles.Shapeshifter,
                CustomRoles.NiceSwapper => CustomRoles.Crewmate,
                CustomRoles.Blackmailer => CustomRoles.Shapeshifter,
                CustomRoles.Tom => CustomRoles.Crewmate,
                CustomRoles.Chatty => CustomRoles.Crewmate,
                CustomRoles.EvilSwapper => CustomRoles.Impostor,
                CustomRoles.Spiritualists => CustomRoles.Engineer,
                CustomRoles.AbandonedCrew => CustomRoles.Impostor,
                CustomRoles.Batter => CustomRoles.Shapeshifter,
                CustomRoles.BloodSeekers => CustomRoles.Impostor,
                CustomRoles.SoulSucker => CustomRoles.Shapeshifter,
                CustomRoles.HangTheDevil => CustomRoles.Impostor,
                CustomRoles.Plumber => CustomRoles.Engineer,
                CustomRoles.ShapeShifters => CustomRoles.Impostor,
                CustomRoles.MagnetMan => CustomRoles.Crewmate,
                CustomRoles.Coldpotato => CustomRoles.Impostor,
               // CustomRoles.Locksmith => CustomRoles.Engineer,
               CustomRoles.Butcher => CustomRoles.Impostor,
               CustomRoles.Fugitives => CustomRoles.Engineer,
                _ => role.IsImpostor() ? CustomRoles.Impostor : CustomRoles.Crewmate,
            };;
    }
    public static RoleTypes GetDYRole(this CustomRoles role) // 对应原版职业（反职业）
    {
        return role switch
        {
            //SoloKombat
            CustomRoles.KB_Normal => RoleTypes.Impostor,
            //烫手的山芋
            CustomRoles.Hotpotato => RoleTypes.Impostor,
            //抓捕
            CustomRoles.runagat => RoleTypes.Impostor,
            CustomRoles.captor => RoleTypes.Impostor,
            //Standard
            CustomRoles.Sheriff => RoleTypes.Impostor,
            CustomRoles.BSR => RoleTypes.Impostor,
            CustomRoles.Arsonist => RoleTypes.Impostor,
            CustomRoles.PlagueDoctor => RoleTypes.Impostor,
            CustomRoles.Jackal => RoleTypes.Impostor,
            CustomRoles.SwordsMan => RoleTypes.Impostor,
            CustomRoles.Innocent => RoleTypes.Impostor,
            CustomRoles.Pelican => RoleTypes.Impostor,
            CustomRoles.Counterfeiter => RoleTypes.Impostor,
            CustomRoles.Revolutionist => RoleTypes.Impostor,
            CustomRoles.FFF => RoleTypes.Impostor,
            CustomRoles.Medic => RoleTypes.Impostor,
            CustomRoles.Gamer => RoleTypes.Impostor,
            CustomRoles.DarkHide => RoleTypes.Impostor,
            CustomRoles.Provocateur => RoleTypes.Impostor,
            CustomRoles.BloodKnight => RoleTypes.Impostor,
            CustomRoles.Totocalcio => RoleTypes.Impostor,
            CustomRoles.Succubus => RoleTypes.Impostor,
            CustomRoles.OpportunistKiller => RoleTypes.Impostor,
            CustomRoles.Prophet => RoleTypes.Impostor,
            CustomRoles.Scout => RoleTypes.Impostor,
            CustomRoles.YinLang => RoleTypes.Impostor,
            CustomRoles.Deputy => RoleTypes.Impostor,
            CustomRoles.Amnesiac => RoleTypes.Shapeshifter,
            CustomRoles.Shifter => RoleTypes.Impostor,
            CustomRoles.DemonHunterm => RoleTypes.Impostor,
            CustomRoles.SchrodingerCat => RoleTypes.Impostor,
            CustomRoles.Crush => RoleTypes.Impostor,
            CustomRoles.Slaveowner => RoleTypes.Impostor,
            CustomRoles.Lawyer => RoleTypes.Impostor,
            CustomRoles.Prosecutors => RoleTypes.Impostor,
            CustomRoles.Jealousy => RoleTypes.Impostor,
            CustomRoles.SourcePlague => RoleTypes.Impostor,
            CustomRoles.PlaguesGod => RoleTypes.Impostor,
            CustomRoles.Captain => RoleTypes.Impostor,
            CustomRoles.ET => RoleTypes.Impostor,
            CustomRoles.King => RoleTypes.Impostor,
            CustomRoles.ElectOfficials => RoleTypes.Impostor,
            CustomRoles.SpeedUp => RoleTypes.Impostor,
            CustomRoles.ChiefOfPolice => RoleTypes.Impostor,
            CustomRoles.Exorcist => RoleTypes.Impostor,
            CustomRoles.NiceTracker =>  RoleTypes.Impostor,
            CustomRoles.Knight => RoleTypes.Impostor,
            CustomRoles.Cupid => RoleTypes.Impostor,
            CustomRoles.Akujo => RoleTypes.Impostor,
            CustomRoles.Hunter => RoleTypes.Impostor,
            CustomRoles.Merchant => RoleTypes.Impostor,
            CustomRoles.Thirsty => RoleTypes.Impostor,
            CustomRoles.Yandere => RoleTypes.Impostor,
            CustomRoles.Fake => RoleTypes.Impostor,
            CustomRoles.RewardOfficer => RoleTypes.Impostor,
            CustomRoles.Copycat => RoleTypes.Shapeshifter,
            CustomRoles.Henry => RoleTypes.Impostor,
            CustomRoles.Loners => RoleTypes.Impostor,
            CustomRoles.Meditator => RoleTypes.Impostor,
            CustomRoles.Challenger => RoleTypes.Impostor,
            CustomRoles.Refuser => RoleTypes.Impostor,
            CustomRoles.ZeyanRefuser => RoleTypes.Impostor,
            CustomRoles.AnimalRefuser => RoleTypes.Impostor,
            CustomRoles.UnanimalRefuser => RoleTypes.Impostor,
            CustomRoles.AttendRefuser => RoleTypes.Impostor,
            CustomRoles.CrazyRefuser => RoleTypes.Impostor,
            CustomRoles.Guardian => RoleTypes.Impostor,
            _ => RoleTypes.GuardianAngel
        };
    }
    public static bool IsAdditionRole(this CustomRoles role) // 是否附加
    {
        return role is
            CustomRoles.Lovers or
             CustomRoles.CrushLovers or
             CustomRoles.CupidLovers or
             CustomRoles.Honmei or
             CustomRoles.Backup or
            CustomRoles.LastImpostor or
            CustomRoles.Ntr or
            CustomRoles.Madmate or
            CustomRoles.Watcher or
            CustomRoles.Flashman or
            CustomRoles.Lighter or
            CustomRoles.Seer or
            CustomRoles.Brakar or
            CustomRoles.Oblivious or
            CustomRoles.Bewilder or
            CustomRoles.Workhorse or
            CustomRoles.Fool or
            CustomRoles.Avanger or
            CustomRoles.Youtuber or
            CustomRoles.Egoist or
            CustomRoles.TicketsStealer or
            CustomRoles.DualPersonality or
            CustomRoles.Mimic or
            CustomRoles.Reach or
            CustomRoles.Charmed or
            CustomRoles.Bait or
            CustomRoles.Trapper or
            CustomRoles.Bitch or
            CustomRoles.Rambler or
            CustomRoles.Destroyers or
            CustomRoles.UnluckyEggs or
            CustomRoles.Fategiver or
            CustomRoles.Detective or
        CustomRoles.Wanderers or
        CustomRoles.Attendant or
        CustomRoles.LostSouls or
        CustomRoles.Executor or
        CustomRoles.OldThousand or
            CustomRoles.involution or
            CustomRoles.Diseased or
            CustomRoles.seniormanagement or
            CustomRoles.Believer or
        CustomRoles.OldImpostor or
        CustomRoles.DeathGhost or
        CustomRoles.Energizer or
        CustomRoles.Originator or
        CustomRoles.QL or
        CustomRoles.Signal or
        CustomRoles.ProfessionGuesser or
        CustomRoles.VIP or
        CustomRoles.Thirsty or
        CustomRoles.Rainbow;
    }
    public static bool IsNK(this CustomRoles role) // 是否带刀中立
    {
        return role is
            CustomRoles.Jackal or//有猫
            CustomRoles.Pelican or//不应该有猫（吞了）
            CustomRoles.FFF or//不应该有猫（紫砂）
            CustomRoles.Gamer or//有猫
            CustomRoles.DarkHide or//有猫
            CustomRoles.Provocateur or//不应该有猫（气死）
            CustomRoles.BloodKnight or//有猫
            CustomRoles.YinLang or//有猫
            CustomRoles.Amnesiac or//偷职业的要什么猫
            CustomRoles.Shifter or//偷职业的要什么猫
        CustomRoles.OpportunistKiller or//有猫
        CustomRoles.Sidekick or//豺狼有猫
        CustomRoles.Jealousy or//不应该有猫（紫砂）
        CustomRoles.PlaguesGod or//有猫
        CustomRoles.Loners or
        CustomRoles.Meditator or
                CustomRoles.Refuser or
        CustomRoles.ZeyanRefuser or
        CustomRoles.CrazyRefuser or
        CustomRoles.UnanimalRefuser or
        CustomRoles.AnimalRefuser or
        CustomRoles.AttendRefuser;
    }
    public static bool IsNeutralKilling(this CustomRoles role) //是否邪恶中立（抢夺或单独胜利的中立）
    {
        return role is
            CustomRoles.Terrorist or
            CustomRoles.Arsonist or
            CustomRoles.PlagueDoctor or
            CustomRoles.Jackal or
            CustomRoles.God or
            CustomRoles.Mario or
            CustomRoles.Innocent or
            CustomRoles.Pelican or
            CustomRoles.Egoist or
            CustomRoles.Gamer or
            CustomRoles.DarkHide or
            CustomRoles.Workaholic or
            CustomRoles.Collector or
            CustomRoles.BloodKnight or
            CustomRoles.Succubus or
            CustomRoles.Vulture or
        CustomRoles.MengJiangGirl or
        CustomRoles.Bull or
        CustomRoles.Masochism or
        CustomRoles.Dissenter or
        CustomRoles.YinLang or
        CustomRoles.StinkyAncestor or
        CustomRoles.Crush or
        CustomRoles.Injured or
        CustomRoles.Cupid or
        CustomRoles.Akujo or
        CustomRoles.Jealousy or
        CustomRoles.SourcePlague or
        CustomRoles.PlaguesGod or
        CustomRoles.King or
        CustomRoles.Exorcist or
        CustomRoles.Henry or
        CustomRoles.Fake or
        CustomRoles.Loners or
            CustomRoles.MrDesperate or
        CustomRoles.Meditator or
        CustomRoles.Challenger;
    }
    public static bool IsCK(this CustomRoles role) // 是否带刀船员
    {
        return role is
            CustomRoles.SwordsMan or
            CustomRoles.Sheriff or
            CustomRoles.BSR;
    }
    public static bool IsGailu(this CustomRoles role)// 是否概率职业
    {
        return role is
            CustomRoles.Disorder or
            CustomRoles.Depressed or
        CustomRoles.SpecialAgent or
        CustomRoles.EvilGambler or
        CustomRoles.Luckey or
        CustomRoles.XiaoMu or
            CustomRoles.MengJiangGirl or
        CustomRoles.HatarakiMan or
        CustomRoles.Mascot;
    }
    public static bool IsMascot(this CustomRoles role)//是否吉祥物
    {
        return role is
            CustomRoles.Mascot;
    }
    public static bool IsShapeshifter(this CustomRoles role) // 是否变形
    {
        return role is
                CustomRoles.Sniper or
                CustomRoles.BountyHunter or
                CustomRoles.ShapeMaster or
                CustomRoles.MimicAss or
                CustomRoles.Warlock or
                CustomRoles.SerialKiller or
                CustomRoles.FireWorks or
                CustomRoles.EvilTracker or
                CustomRoles.Hacker or
                CustomRoles.Miner or
                CustomRoles.Assassin or
                CustomRoles.Escapee or
                CustomRoles.ImperiusCurse or
                CustomRoles.QuickShooter or
                CustomRoles.Concealer or
                CustomRoles.Hangman or
                CustomRoles.Amnesiac or
                CustomRoles.Anglers or
                CustomRoles.Disperser or
                CustomRoles.Medusa or
                CustomRoles.Cluster or
                CustomRoles.Forger;
    }
    public static bool IsMini(this CustomRoles role) // 是否小孩
    {
        return role is
            CustomRoles.Mini;
    }
    public static bool PetActivatedAbility(this CustomRoles role)//宠物事件
    {
        return role is
            CustomRoles.Mayor or
            CustomRoles.Veteran;
    }
    public static bool IsImpostor(this CustomRoles role) // 是否内鬼
    {
        return role is
            CustomRoles.Impostor or
            CustomRoles.Shapeshifter or
            CustomRoles.BountyHunter or
            CustomRoles.Vampire or
            CustomRoles.Witch or
            CustomRoles.ShapeMaster or
            CustomRoles.Zombie or
            CustomRoles.Warlock or
            CustomRoles.Assassin or
            CustomRoles.Hacker or
            CustomRoles.Miner or
            CustomRoles.Escapee or
            CustomRoles.SerialKiller or
            CustomRoles.Mare or
            CustomRoles.Puppeteer or
            CustomRoles.TimeThief or
            CustomRoles.Mafia or
            CustomRoles.Minimalism or
            CustomRoles.FireWorks or
            CustomRoles.Sniper or
            CustomRoles.EvilTracker or
            CustomRoles.EvilGuesser or
            CustomRoles.AntiAdminer or
            CustomRoles.Sans or
            CustomRoles.Bomber or
            CustomRoles.Scavenger or
            CustomRoles.BoobyTrap or
            CustomRoles.Capitalism or
            CustomRoles.Gangster or
            CustomRoles.Cleaner or
            CustomRoles.BallLightning or
            CustomRoles.Greedier or
            CustomRoles.CursedWolf or
            CustomRoles.ImperiusCurse or
            CustomRoles.QuickShooter or
            CustomRoles.Concealer or
            CustomRoles.Eraser or
            CustomRoles.OverKiller or
            CustomRoles.Hangman or
            CustomRoles.Bard or
            CustomRoles.Swooper or
            CustomRoles.Crewpostor or
            CustomRoles.Depressed or
        CustomRoles.SpecialAgent or
        CustomRoles.EvilGambler or
        CustomRoles.Fraudster or
        CustomRoles.Cultivator or
        CustomRoles.sabcat or
        CustomRoles.Disorder or
        CustomRoles.Vandalism or
        CustomRoles.Followers or
        CustomRoles.Spellmaster or
        CustomRoles.DemolitionManiac or
        CustomRoles.DestinyChooser or
        CustomRoles.Hemophobia or
        CustomRoles.Anglers or
        CustomRoles.Guide or
        CustomRoles.Strikers or
        CustomRoles.Squeezers or
        CustomRoles.Corpse or
        CustomRoles.EvilMini or
        CustomRoles.Defector or
        CustomRoles.DoubleKiller or
        CustomRoles.Disperser or
        //CustomRoles.Kidnapper or
        CustomRoles.Sleeve or
        CustomRoles.MimicKiller or
        CustomRoles.MimicAss or
        CustomRoles.Mimics or
        CustomRoles.Medusa or
        CustomRoles.Cluster or
        CustomRoles.Forger or
        CustomRoles.EvilSwapper or
        CustomRoles.Blackmailer or
        CustomRoles.AbandonedCrew or
        CustomRoles.Batter or
        CustomRoles.BloodSeekers or
        CustomRoles.SoulSucker or
        CustomRoles.HangTheDevil or
        CustomRoles.ShapeShifters;
    }
    public static bool IsImpostorForSe(this CustomRoles role) // 是否内鬼
    {
        return role is
            CustomRoles.Impostor or
            CustomRoles.Shapeshifter or
            CustomRoles.BountyHunter or
            CustomRoles.Vampire or
            CustomRoles.Witch or
            CustomRoles.ShapeMaster or
            CustomRoles.Zombie or
            CustomRoles.Warlock or
            CustomRoles.Assassin or
            CustomRoles.Hacker or
            CustomRoles.Miner or
            CustomRoles.Escapee or
            CustomRoles.SerialKiller or
            CustomRoles.Mare or
            CustomRoles.Puppeteer or
            CustomRoles.TimeThief or
            CustomRoles.Mafia or
            CustomRoles.Minimalism or
            CustomRoles.FireWorks or
            CustomRoles.Sniper or
            CustomRoles.EvilTracker or
            CustomRoles.EvilGuesser or
            CustomRoles.AntiAdminer or
            CustomRoles.Sans or
            CustomRoles.Bomber or
            CustomRoles.Scavenger or
            CustomRoles.BoobyTrap or
            CustomRoles.Capitalism or
            CustomRoles.Gangster or
            CustomRoles.Cleaner or
            CustomRoles.BallLightning or
            CustomRoles.Greedier or
            CustomRoles.CursedWolf or
            CustomRoles.ImperiusCurse or
            CustomRoles.QuickShooter or
            CustomRoles.Concealer or
            CustomRoles.Eraser or
            CustomRoles.OverKiller or
            CustomRoles.Hangman or
            CustomRoles.Bard or
            CustomRoles.Swooper or
            CustomRoles.Crewpostor or
            CustomRoles.Depressed or
        CustomRoles.SpecialAgent or
        CustomRoles.EvilGambler or
        CustomRoles.Fraudster or
        CustomRoles.Cultivator or
        CustomRoles.sabcat or
        CustomRoles.Disorder or
        CustomRoles.Vandalism or
        CustomRoles.Followers or
        CustomRoles.Spellmaster or
        CustomRoles.DemolitionManiac or
        CustomRoles.DestinyChooser or
        CustomRoles.Hemophobia or
        CustomRoles.Anglers or
        CustomRoles.Guide or
        CustomRoles.Strikers or
        CustomRoles.Squeezers or
        CustomRoles.Corpse or
        CustomRoles.EvilMini or
        CustomRoles.Defector or
        CustomRoles.DoubleKiller or
        CustomRoles.Disperser or
        //CustomRoles.Kidnapper or
        CustomRoles.Sleeve or
        CustomRoles.MimicKiller or
        CustomRoles.MimicAss or
        CustomRoles.Mimics or
        CustomRoles.Medusa or
        CustomRoles.Cluster or
        CustomRoles.Forger or
        CustomRoles.EvilSwapper or
        CustomRoles.Blackmailer or
        CustomRoles.AbandonedCrew or
        CustomRoles.Batter or
        CustomRoles.BloodSeekers or
        CustomRoles.SoulSucker or
        CustomRoles.HangTheDevil or
        CustomRoles.Undercover or
        CustomRoles.ShapeShifters;
    }
    public static bool IsHotPotato(this CustomRoles role)
    {
        return role is
       CustomRoles.Hotpotato;
    }
    public static bool IsColdPotato(this CustomRoles role)
    {
        return role is
       CustomRoles.Coldpotato;
    }
    public static bool IsImpostornokill(this CustomRoles role) // 是否有刀内鬼
    {
        return role is
            CustomRoles.Impostor or
            CustomRoles.Shapeshifter or
            CustomRoles.BountyHunter or
            CustomRoles.Vampire or
            CustomRoles.Witch or
            CustomRoles.ShapeMaster or
            CustomRoles.Zombie or
            CustomRoles.Warlock or
            CustomRoles.EvilSwapper or
            CustomRoles.Assassin or
            CustomRoles.Hacker or
            CustomRoles.Miner or
            CustomRoles.Escapee or
            CustomRoles.SerialKiller or
            CustomRoles.Mare or
            CustomRoles.Puppeteer or
            CustomRoles.TimeThief or
            CustomRoles.Mafia or
            CustomRoles.Minimalism or
            CustomRoles.FireWorks or
            CustomRoles.Sniper or
            CustomRoles.EvilTracker or
            CustomRoles.EvilGuesser or
            CustomRoles.AntiAdminer or
            CustomRoles.Sans or
            CustomRoles.Bomber or
            CustomRoles.Scavenger or
            CustomRoles.BoobyTrap or
            CustomRoles.Capitalism or
            CustomRoles.Gangster or
            CustomRoles.Cleaner or
            CustomRoles.BallLightning or
            CustomRoles.Greedier or
            CustomRoles.CursedWolf or
            CustomRoles.ImperiusCurse or
            CustomRoles.QuickShooter or
            CustomRoles.Concealer or
            CustomRoles.Eraser or
            CustomRoles.OverKiller or
            CustomRoles.Hangman or
            CustomRoles.Bard or
            CustomRoles.Swooper or
            CustomRoles.Depressed or
        CustomRoles.EvilGambler or
        CustomRoles.Fraudster or
        CustomRoles.Cultivator or
        CustomRoles.Disorder or
        CustomRoles.Followers or
        CustomRoles.Vandalism or
         CustomRoles.sabcat or
         CustomRoles.Spellmaster or
        CustomRoles.DemolitionManiac or
        CustomRoles.Hemophobia or
        CustomRoles.Strikers or
        CustomRoles.EvilMini or
        CustomRoles.Defector or
        CustomRoles.MimicKiller or
        CustomRoles.Blackmailer or
        CustomRoles.ShapeShifters;
    }
    public static bool IsGuress(this CustomRoles role) // 是否赌
    {
        return role is
            CustomRoles.EvilGuesser;
    }
    public static bool IsEnigger(this CustomRoles role) // 是否工程
    {
        return role is
                CustomRoles.SabotageMaster or
                CustomRoles.Terrorist or
                CustomRoles.Paranoia or
                CustomRoles.Mario or
                CustomRoles.Veteran or
                CustomRoles.Grenadier or
                CustomRoles.Workaholic or
                CustomRoles.DovesOfNeace or
                CustomRoles.Rudepeople or
                CustomRoles.Vulture or
                CustomRoles.Dissenter or
                CustomRoles.TimeStops or
                CustomRoles.Whoops or
                CustomRoles.TimeMaster or
                CustomRoles.GlennQuagmire or
                CustomRoles.SoulSeeker or
                CustomRoles.Injured or
                CustomRoles.Buried or
                CustomRoles.Chameleon or
                CustomRoles.Spiritualists;
    }
    public static bool IsNKS(this CustomRoles role) // 是否中立杀手
    {
        return role is
            CustomRoles.Jackal or
            CustomRoles.Pelican or
            CustomRoles.Gamer or
            CustomRoles.DarkHide or
            CustomRoles.BloodKnight or
            CustomRoles.YinLang or
        CustomRoles.OpportunistKiller or
        CustomRoles.Succubus or
        CustomRoles.Sidekick or
         CustomRoles.SourcePlague or
        CustomRoles.PlaguesGod or
        CustomRoles.Yandere or
        CustomRoles.Loners;
    }
    public static bool IsNeutral(this CustomRoles role) // 是否中立
    {
        return role is
            //SoloKombat
            CustomRoles.KB_Normal or
            //烫手的山芋
            CustomRoles.Coldpotato or
            CustomRoles.Hotpotato or
            //抓捕
            CustomRoles.captor or
            CustomRoles.runagat or
            //Standard
            CustomRoles.Amnesiac or
            CustomRoles.Jester or
            CustomRoles.Opportunist or
            CustomRoles.Mario or
            CustomRoles.Terrorist or
            CustomRoles.Executioner or
            CustomRoles.Arsonist or
            CustomRoles.PlagueDoctor or
            CustomRoles.Jackal or
            CustomRoles.God or
            CustomRoles.Innocent or
            CustomRoles.Pelican or
            CustomRoles.Revolutionist or
            CustomRoles.FFF or
            CustomRoles.Konan or
            CustomRoles.Gamer or
            CustomRoles.DarkHide or
            CustomRoles.Workaholic or
            CustomRoles.Collector or
            CustomRoles.Provocateur or
            CustomRoles.Sunnyboy or
            CustomRoles.BloodKnight or
            CustomRoles.Totocalcio or
            CustomRoles.Succubus or
            CustomRoles.OpportunistKiller or
            CustomRoles.Vulture or
        CustomRoles.MengJiangGirl or
        CustomRoles.Bull or
        CustomRoles.Masochism or
        CustomRoles.FreeMan or
        CustomRoles.YinLang or
        CustomRoles.Amnesiac or
        CustomRoles.Shifter or
        CustomRoles.Lawyer or
        CustomRoles.Whoops or
        CustomRoles.Sidekick or
        CustomRoles.SchrodingerCat or
        CustomRoles.Crush or
         CustomRoles.Injured or
        CustomRoles.Cupid or
        CustomRoles.Akujo or
            CustomRoles.Prosecutors or
        CustomRoles.Slaveowner or
        CustomRoles.Jealousy or
        CustomRoles.SourcePlague or
        CustomRoles.PlaguesGod or
        CustomRoles.King or
        CustomRoles.Exorcist or
        CustomRoles.Yandere or
        CustomRoles.Henry or
        CustomRoles.Fake or
        CustomRoles.RewardOfficer or
        CustomRoles.Chatty or
        CustomRoles.MrDesperate or
        CustomRoles.Loners or
        CustomRoles.Meditator or
        CustomRoles.Challenger or
                 CustomRoles.Refuser or
        CustomRoles.ZeyanRefuser or
        CustomRoles.CrazyRefuser or
        CustomRoles.UnanimalRefuser or
        CustomRoles.AnimalRefuser or
        CustomRoles.AttendRefuser;
    }
    public static bool IsAnimal(this CustomRoles role) // 是否动物
    {
        return role is
            CustomRoles.Mare or
            CustomRoles.Vampire or
            CustomRoles.ShapeMaster or
            CustomRoles.Zombie or
            CustomRoles.SchrodingerCat or
            CustomRoles.Medusa or
            CustomRoles.Glitch or
            CustomRoles.DovesOfNeace or
            CustomRoles.ET or
            CustomRoles.Tom or
            CustomRoles.Jackal or
            CustomRoles.Pelican or
            CustomRoles.Vulture or
            CustomRoles.Bull or
            CustomRoles.Whoops or
            CustomRoles.Fake;

    }
    public static bool IsAttend(this CustomRoles role)//whether attend
    {
        return role is
            CustomRoles.Gangster or
            CustomRoles.Madmate or
            CustomRoles.Sheriff or
            CustomRoles.ChiefOfPolice or
            CustomRoles.Captain or
            CustomRoles.Solicited or
            CustomRoles.Mayor or
            CustomRoles.ElectOfficials or
            CustomRoles.Succubus or
            CustomRoles.Charmed or
            CustomRoles.Jackal or
            CustomRoles.Sidekick or
            CustomRoles.Whoops or
            CustomRoles.PlaguesGod or
            CustomRoles.SourcePlague or
            CustomRoles.Revolutionist or
            CustomRoles.Deputy;
    }
    public static bool CheckAddonConfilct(CustomRoles role, PlayerControl pc)
    {
        if (!role.IsAdditionRole()) return false;

        if (pc.Is(CustomRoles.GM) || (pc.HasSubRole() && Options.LimitAddonsNum.GetBool()) && pc.GetCustomSubRoles().Count >= Options.AddonsNumMax.GetInt() || pc.Is(CustomRoles.Needy) || pc.Is(CustomRoles.Captain)) return false;
        if (role is CustomRoles.Lighter && (!pc.GetCustomRole().IsCrewmate() || pc.Is(CustomRoles.Bewilder))) return false;
        if (role is CustomRoles.Bewilder && (pc.GetCustomRole().IsImpostor() || pc.Is(CustomRoles.Lighter))) return false;
        if (role is CustomRoles.involution && (!pc.GetCustomRole().IsCrewmate())) return false;
        if (role is CustomRoles.Ntr && (pc.Is(CustomRoles.Lovers) || pc.Is(CustomRoles.FFF) || pc.Is(CustomRoles.Crush) || pc.Is(CustomRoles.Cupid) || pc.Is(CustomRoles.Akujo) || pc.Is(CustomRoles.Believer))) return false;
        if (role is CustomRoles.Madmate && !Utils.CanBeMadmate(pc)) return false;
        if (role is CustomRoles.Oblivious && (pc.Is(CustomRoles.Detective) || pc.Is(CustomRoles.Cleaner) || pc.Is(CustomRoles.Vulture) || pc.Is(CustomRoles.Mortician) || pc.Is(CustomRoles.Mediumshiper))) return false;
        if (role is CustomRoles.Fool && (pc.GetCustomRole().IsImpostor() || pc.Is(CustomRoles.SabotageMaster))) return false;
        if (role is CustomRoles.Avanger && pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeAvanger.GetBool()) return false;
        if (role is CustomRoles.Brakar && pc.Is(CustomRoles.Dictator)) return false;
        if (role is CustomRoles.Youtuber && (!pc.GetCustomRole().IsCrewmate() || pc.Is(CustomRoles.Madmate) || pc.Is(CustomRoles.Sheriff))) return false;
        if (role is CustomRoles.Egoist && (pc.GetCustomRole().IsNeutral() || pc.Is(CustomRoles.Madmate))) return false;
        if (role is CustomRoles.Egoist && pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeEgoist.GetBool()) return false;
        if (role is CustomRoles.Egoist && pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeEgoist.GetBool()) return false;
        if (role is CustomRoles.Egoist && !pc.Is(CustomRoles.Believer)) return false;
        if (role is CustomRoles.TicketsStealer or CustomRoles.Mimic && !pc.GetCustomRole().IsImpostor()) return false;
        if (role is CustomRoles.TicketsStealer && (pc.Is(CustomRoles.Bomber) || pc.Is(CustomRoles.BoobyTrap) || pc.Is(CustomRoles.Capitalism))) return false;
        if (role is CustomRoles.Mimic && pc.Is(CustomRoles.Mafia)) return false;
        if (role is CustomRoles.DualPersonality && !pc.GetCustomRole().IsCrewmate() || pc.Is(CustomRoles.Madmate)) return false;
        if (role is CustomRoles.DualPersonality && !pc.GetCustomRole().IsCrewmate()) return false;
        if (role is CustomRoles.Seer && pc.Is(CustomRoles.Mortician)) return false;
        if (role is CustomRoles.Seer && ((pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeSeer.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Options.NeutralCanBeSeer.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeSeer.GetBool()))) return false;
        if (role is CustomRoles.Bait && ((pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeBait.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Options.NeutralCanBeBait.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeBait.GetBool()))) return false;
        if (role is CustomRoles.Trapper && ((pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeTrapper.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Options.NeutralCanBeTrapper.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeTrapper.GetBool()))) return false;
        if (role is CustomRoles.Reach && !pc.CanUseKillButton()) return false;
        if (role is CustomRoles.Executor && !pc.CanUseKillButton()) return false;
        if (role is CustomRoles.OldThousand && !pc.GetCustomRole().IsGailu()) return false; 
        if (role is CustomRoles.Flashman && pc.Is(CustomRoles.Swooper)) return false;
        if (role is CustomRoles.Bitch && pc.Is(CustomRoles.Jester)) return false;
        if (role is CustomRoles.Rambler && (pc.Is(CustomRoles.Flashman) || pc.Is(CustomRoles.SpeedBooster))) return false;
        if (role is CustomRoles.Destroyers or CustomRoles.Mimic && !pc.GetCustomRole().IsImpostor()) return false;
        if (role is CustomRoles.Destroyers && (pc.Is(CustomRoles.Bomber) || pc.Is(CustomRoles.BoobyTrap))) return false;
        if (role is CustomRoles.UnluckyEggs && pc.Is(CustomRoles.Luckey) || pc.Is(CustomRoles.Mascot) || pc.Is(CustomRoles.OldThousand)) return false;
        if (role is CustomRoles.OldImpostor or CustomRoles.Mimic && !pc.GetCustomRole().IsImpostor()) return false;
        if (role is CustomRoles.OldImpostor && (pc.Is(CustomRoles.Bomber) || pc.Is(CustomRoles.BoobyTrap))) return false;
        if (role is CustomRoles.Diseased && (!pc.GetCustomRole().IsCrewmate())) return false;
        if (role is CustomRoles.DeathGhost && !pc.GetCustomRole().IsCrewmate() && pc.CanUseKillButton()) return false;
        if (role is CustomRoles.Energizer && (!pc.GetCustomRole().IsCrewmate()) && pc.CanUseKillButton()) return false; 
        if (role is CustomRoles.Believer && pc.Is(CustomRoles.Succubus) || pc.Is(CustomRoles.Jackal)) return false;
        if (role is CustomRoles.QL && pc.Is(CustomRoles.Judge) || pc.Is(CustomRoles.ProfessionGuesser)) return false;
        if (role is CustomRoles.ProfessionGuesser && !pc.GetCustomRole().IsGuress()) return false;
        if (role is CustomRoles.ProfessionGuesser && pc.Is(CustomRoles.QL)) return false;
        if (role is CustomRoles.Thirsty && (!pc.GetCustomRole().IsCrewmate() && pc.CanUseKillButton())) return false; 
        return true;
    }
    public static RoleTypes GetRoleTypes(this CustomRoles role)
        => GetVNRole(role) switch
        {
            CustomRoles.Impostor => RoleTypes.Impostor,
            CustomRoles.Scientist => RoleTypes.Scientist,
            CustomRoles.Engineer => RoleTypes.Engineer,
            CustomRoles.GuardianAngel => RoleTypes.GuardianAngel,
            CustomRoles.Shapeshifter => RoleTypes.Shapeshifter,
            CustomRoles.Crewmate => RoleTypes.Crewmate,
            _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
        };
    public static bool IsDesyncRole(this CustomRoles role) => role.GetDYRole() != RoleTypes.GuardianAngel;
    public static bool IsCrewmate(this CustomRoles role) => !role.IsImpostorTeam() && !role.IsNeutral() || role == CustomRoles.SchrodingerCat && SchrodingerCat.iscrew || role == CustomRoles.Undercover;
    public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role == CustomRoles.Madmate || role == CustomRoles.SchrodingerCat && SchrodingerCat.isimp; 
    public static bool IsNNK(this CustomRoles role) => role.IsNeutral() && !role.IsNK(); // 是否无刀中立
    public static bool IsVanilla(this CustomRoles role) // 是否原版职业
    {
        return role is
            CustomRoles.Crewmate or
            CustomRoles.Engineer or
            CustomRoles.Scientist or
            CustomRoles.GuardianAngel or
            CustomRoles.Impostor or
            CustomRoles.Shapeshifter;
    }
    public static CustomRoleTypes GetCustomRoleTypes(this CustomRoles role)
    {
        CustomRoleTypes type = CustomRoleTypes.Crewmate;
        if (role.IsImpostor()) type = CustomRoleTypes.Impostor;
        if (role.IsNeutral()) type = CustomRoleTypes.Neutral;
        if (role.IsAdditionRole()) type = CustomRoleTypes.Addon;
        return type;
    }
    public static bool RoleExist(this CustomRoles role, bool countDead = false) => Main.AllPlayerControls.Any(x => x.Is(role) && (x.IsAlive() || countDead));
    public static int GetCount(this CustomRoles role)
    {
        if (role.IsVanilla())
        {
            if (Options.DisableVanillaRoles.GetBool()) return 0;
            var roleOpt = Main.NormalOptions.RoleOptions;
            return role switch
            {
                CustomRoles.Engineer => roleOpt.GetNumPerGame(RoleTypes.Engineer),
                CustomRoles.Scientist => roleOpt.GetNumPerGame(RoleTypes.Scientist),
                CustomRoles.Shapeshifter => roleOpt.GetNumPerGame(RoleTypes.Shapeshifter),
                CustomRoles.GuardianAngel => roleOpt.GetNumPerGame(RoleTypes.GuardianAngel),
                CustomRoles.Crewmate => roleOpt.GetNumPerGame(RoleTypes.Crewmate),
                _ => 0
            };
        }
        else
        {
            return Options.GetRoleCount(role);
        }
    }
    public static int GetMode(this CustomRoles role) => Options.GetRoleSpawnMode(role);
    public static float GetChance(this CustomRoles role)
    {
        if (role.IsVanilla())
        {
            var roleOpt = Main.NormalOptions.RoleOptions;
            return role switch
            {
                CustomRoles.Engineer => roleOpt.GetChancePerGame(RoleTypes.Engineer),
                CustomRoles.Scientist => roleOpt.GetChancePerGame(RoleTypes.Scientist),
                CustomRoles.Shapeshifter => roleOpt.GetChancePerGame(RoleTypes.Shapeshifter),
                CustomRoles.GuardianAngel => roleOpt.GetChancePerGame(RoleTypes.GuardianAngel),
                CustomRoles.Crewmate => roleOpt.GetChancePerGame(RoleTypes.Crewmate),
                _ => 0
            } / 100f;
        }
        else
        {
            return Options.GetRoleChance(role);
        }
    }
    public static bool IsEnable(this CustomRoles role) => role.GetCount() > 0;
    public static CountTypes GetCountTypes(this CustomRoles role)
    {
        if (role == CustomRoles.GM)
        {
            return CountTypes.OutOfGame;
        }
        else if (role == CustomRoles.Jackal || role == CustomRoles.Whoops || role == CustomRoles.Sidekick || role == CustomRoles.SchrodingerCat && SchrodingerCat.isjac)
        {
            return CountTypes.Jackal;
        }
        else if (role == CustomRoles.Loners || role == CustomRoles.SchrodingerCat && SchrodingerCat.isln)
        {
            return CountTypes.Loners;
        }
        else if (role == CustomRoles.Meditator)
        {
            return CountTypes.Meditator;
        }
       
        else if (role == CustomRoles.Pelican)
        {
            return CountTypes.Pelican;
        }
        else if (role == CustomRoles.Gamer || (role == CustomRoles.SchrodingerCat && SchrodingerCat.isgam))
        {
            return CountTypes.Gamer;
        }
        else if (role == CustomRoles.BloodKnight || role == CustomRoles.SchrodingerCat && SchrodingerCat.isbk)
        {
            return CountTypes.BloodKnight;
        }
        else if (role == CustomRoles.Succubus)
        {
            return CountTypes.Succubus;
        }
        else if (role == CustomRoles.YinLang || role == CustomRoles.SchrodingerCat && SchrodingerCat.isyl)
        {
            return CountTypes.YinLang;
        }
        else if (role == CustomRoles.PlaguesGod || role == CustomRoles.SchrodingerCat && SchrodingerCat.ispg || role == CustomRoles.SourcePlague)
        {
            return CountTypes.PlaguesGod;
        }
        else if (role == CustomRoles.Yandere)
        {
            return CountTypes.Yandere;
        }
        else
        {
            return role.IsImpostorTeam() ? CountTypes.Impostor : CountTypes.Crew;
        }
    }


    public static bool HasSubRole(this PlayerControl pc) => Main.PlayerStates[pc.PlayerId].SubRoles.Count > 0;
}
public enum CustomRoleTypes
{
    Crewmate,
    Impostor,
    Neutral,
    Addon
}
public enum CountTypes
{
    OutOfGame,
    None,
    Crew,
    Impostor,
    Jackal,
    Pelican,
    Gamer,
    BloodKnight,
    Succubus,
    YinLang,
    Amnesiac,
    PlaguesGod,
    Yandere,
    Loners,
    Meditator,
}