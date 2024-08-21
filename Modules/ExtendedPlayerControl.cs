using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHEXI.Modules;
using TOHEXI.Roles.AddOns.Impostor;
using TOHEXI.Roles.Crewmate;
using TOHEXI.Roles.Double;
using TOHEXI.Roles.Impostor;
using TOHEXI.Roles.Neutral;
using UnityEngine;
using UnityEngine.UIElements.UIR;
using static TOHEXI.Translator;

namespace TOHEXI;

static class ExtendedPlayerControl
{
    public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role)
    {
        if (role < CustomRoles.NotAssigned)
        {
            Main.PlayerStates[player.PlayerId].SetMainRole(role);
        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
        {
            Main.PlayerStates[player.PlayerId].SetSubRole(role);
        }
        if (AmongUsClient.Instance.AmHost)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.WritePacked((int)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, SendOption.Reliable, -1);
            writer.Write(PlayerId);
            writer.WritePacked((int)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }

    public static void RpcExile(this PlayerControl player)
    {
        RPC.ExileAsync(player);
    }
    public static ClientData GetClient(this PlayerControl player)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Character.PlayerId == player.PlayerId).FirstOrDefault();
            return client;
        }
        catch
        {
            return null;
        }
    }
    public static int GetClientId(this PlayerControl player)
    {
        if (player == null) return -1;
        var client = player.GetClient();
        return client == null ? -1 : client.Id;
    }
    public static CustomRoles GetCustomRole(this GameData.PlayerInfo player)
    {
        return player == null || player.Object == null ? CustomRoles.Crewmate : player.Object.GetCustomRole();
    }
    /// <summary>
    /// ※サブロールは取得できません。
    /// </summary>
    public static CustomRoles GetCustomRole(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + "がCustomRoleを取得しようとしましたが、対象がnullでした。", "GetCustomRole");
            return CustomRoles.Crewmate;
        }
        var GetValue = Main.PlayerStates.TryGetValue(player.PlayerId, out var State);

        return GetValue ? State.MainRole : CustomRoles.Crewmate;
    }

    public static List<CustomRoles> GetCustomSubRoles(this PlayerControl player)
    {
        if (player == null)
        {
            Logger.Warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
            return new() { CustomRoles.NotAssigned };
        }
        return Main.PlayerStates[player.PlayerId].SubRoles;
    }
    public static CountTypes GetCountTypes(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + "がCountTypesを取得しようとしましたが、対象がnullでした。", "GetCountTypes");
            return CountTypes.None;
        }

        return Main.PlayerStates.TryGetValue(player.PlayerId, out var State) ? State.countTypes : CountTypes.None;
    }
    public static void RpcSetNameEx(this PlayerControl player, string name)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
        }
        HudManagerPatch.LastSetNameDesyncCount++;

        Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for All", "RpcSetNameEx");
        player.RpcSetName(name);
    }

    public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null, bool force = false)
    {
        //player: 名前の変更対象
        //seer: 上の変更を確認することができるプレイヤー
        if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
        if (seer == null) seer = player;
        if (!force && Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] == name)
        {
            //Logger.info($"Cancel:{player.name}:{name} for {seer.name}", "RpcSetNamePrivate");
            return;
        }
        Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
        HudManagerPatch.LastSetNameDesyncCount++;
        Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for {seer.GetNameWithRole()}", "RpcSetNamePrivate");

        var clientId = seer.GetClientId();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, SendOption.Reliable, clientId);
        writer.Write(name);
        writer.Write(DontShowOnModdedClient);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, int clientId)
    {
        //player: 名前の変更対象

        if (player == null) return;
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            player.SetRole(role);
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, SendOption.Reliable, clientId);
        writer.Write((ushort)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null, int colorId = 0, bool forObserver = false)
    {
        if (target == null) target = killer;
        if (!forObserver && !MeetingStates.FirstMeeting) Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Observer) && killer.PlayerId != x.PlayerId).Do(x => x.RpcGuardAndKill(target, colorId, true));
        // Host
        if (killer.AmOwner)
        {
            killer.ProtectPlayer(target, colorId);
            killer.MurderPlayer(target,ResultFlags);
        }
        // Other Clients
        if (killer.PlayerId != 0)
        {
            var sender = CustomRpcSender.Create("GuardAndKill Sender", SendOption.None);
            sender.StartMessage(killer.GetClientId());
            sender.StartRpc(killer.NetId, (byte)RpcCalls.ProtectPlayer)
                .WriteNetObject(target)
                .Write(colorId)
                .EndRpc();
            sender.StartRpc(killer.NetId, (byte)RpcCalls.MurderPlayer)
                .WriteNetObject(target)
                .Write((int)ResultFlags)
                .EndRpc();
            sender.EndMessage();
            sender.SendMessage();
           
        }
    }
    public static void SetKillCooldown(this PlayerControl player, float time = -1f)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        player.SyncSettings();
        player.RpcGuardAndKill();
        player.ResetKillCooldown();
    }
    public static void SetKillCooldownV2(this PlayerControl player, float time = -1f, PlayerControl target = null, bool forceAnime = false)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (target == null) target = player;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        if (forceAnime || !player.IsModClient())
        {
            player.SyncSettings();
            player.RpcGuardAndKill(target, 11);
        }
        else
        {
            time = Main.AllPlayerKillCooldown[player.PlayerId] / 2;
            if (player.AmOwner) PlayerControl.LocalPlayer.SetKillTimer(time);
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillTimer, SendOption.Reliable, player.GetClientId());
                writer.Write(time);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Observer) && target.PlayerId != x.PlayerId).Do(x => x.RpcGuardAndKill(target, 11, true));
        }
        player.ResetKillCooldown();
    }
    public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target = null, PlayerControl seer = null)
    {
        if (target == null) target = killer;
        if (seer == null) seer = killer;
        if (killer.AmOwner && seer.AmOwner)
        {
            killer.MurderPlayer(target, ResultFlags);
        }
        else
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, seer.GetClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write((int)ResultFlags);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
    }
    public static void RpcSpecificProtectPlayer(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
    {
        if (AmongUsClient.Instance.AmClient)
        {
            killer.ProtectPlayer(target, colorId);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, killer.GetClientId());
        messageWriter.WriteNetObject(target);
        messageWriter.Write(colorId);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void RpcResetAbilityCooldown(this PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return; //ホスト以外が実行しても何も起こさない
        Logger.Info($"アビリティクールダウンのリセット:{target.name}({target.PlayerId})", "RpcResetAbilityCooldown");
        if (PlayerControl.LocalPlayer == target)
        {
            //targetがホストだった場合
            PlayerControl.LocalPlayer.Data.Role.SetCooldown();
        }
        else
        {
            //targetがホスト以外だった場合
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
            writer.WriteNetObject(target);
            writer.Write(0);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        /*
            プレイヤーがバリアを張ったとき、そのプレイヤーの役職に関わらずアビリティーのクールダウンがリセットされます。
            ログの追加により無にバリアを張ることができなくなったため、代わりに自身に0秒バリアを張るように変更しました。
            この変更により、役職としての守護天使が無効化されます。
            ホストのクールダウンは直接リセットします。
        */
    }
    public static void RpcDesyncUpdateSystem(this PlayerControl target, SystemTypes systemType, int amount)
    {
        var messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, target.GetClientId());
        messageWriter.Write((byte)systemType);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((byte)amount);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    /*public static void RpcBeKilled(this PlayerControl player, PlayerControl KilledBy = null) {
        if(!AmongUsClient.Instance.AmHost) return;
        byte KilledById;
        if(KilledBy == null)
            KilledById = byte.MaxValue;
        else
            KilledById = KilledBy.PlayerId;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)CustomRPC.BeKilled, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(KilledById);
        AmongUsClient.Instance.FinishRpcImmediately(writer);

        RPC.BeKilled(player.PlayerId, KilledById);
    }*/
    public static void MarkDirtySettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
    }
    public static void SyncSettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
        GameOptionsSender.SendAllGameOptions();
    }
    public static TaskState GetPlayerTaskState(this PlayerControl player)
    {
        return Main.PlayerStates[player.PlayerId].GetTaskState();
    }

    /*public static GameOptionsData DeepCopy(this GameOptionsData opt)
    {
        var optByte = opt.ToBytes(5);
        return GameOptionsData.FromBytes(optByte);
    }*/

    public static string GetDisplayRoleName(this PlayerControl player, bool pure = false)
    {
        return Utils.GetDisplayRoleName(player.PlayerId, pure);
    }
    public static string GetSubRoleName(this PlayerControl player, bool forUser = false)
    {
        var SubRoles = Main.PlayerStates[player.PlayerId].SubRoles;
        if (SubRoles.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var role in SubRoles)
        {
            if (role == CustomRoles.NotAssigned) continue;
            sb.Append($"{Utils.ColorString(Color.white, " + ")}{Utils.GetRoleName(role, forUser)}");
        }

        return sb.ToString();
    }
    public static string GetAllRoleName(this PlayerControl player, bool forUser = true)
    {
        if (!player) return null;
        var text = Utils.GetRoleName(player.GetCustomRole(), forUser);
        text += player.GetSubRoleName(forUser);
        return text;
    }
    public static string GetNameWithRole(this PlayerControl player, bool forUser = false)
    {
        var ret = $"{player?.Data?.PlayerName}" + (GameStates.IsInGame ? $"({player?.GetAllRoleName(forUser)})" : "");
        return (forUser ? ret : ret.RemoveHtmlTags());
    }
    public static string GetRoleColorCode(this PlayerControl player)
    {
        return Utils.GetRoleColorCode(player.GetCustomRole());
    }
    public static Color GetRoleColor(this PlayerControl player)
    {
        return Utils.GetRoleColor(player.GetCustomRole());
    }
    public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;

        var systemtypes = SystemTypes.Reactor;
        if (Main.NormalOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;

        new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 128);
        }, 0f + delay, "Reactor Desync");

        new LateTask(() =>
        {
            pc.RpcSpecificMurderPlayer();
        }, 0.2f + delay, "Murder To Reset Cam");

        new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 16);
            if (Main.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncUpdateSystem(systemtypes, 17);
        }, 0.4f + delay, "Fix Desync Reactor");
    }
    public static void ReactorFlash(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null) return;
        int clientId = pc.GetClientId();
        // Logger.Info($"{pc}", "ReactorFlash");
        var systemtypes = SystemTypes.Reactor;
        if (Main.NormalOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;
        float FlashDuration = Options.KillFlashDuration.GetFloat();

        pc.RpcDesyncUpdateSystem(systemtypes, 128);

        new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 16);

            if (Main.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncUpdateSystem(systemtypes, 17);
        }, FlashDuration + delay, "Fix Desync Reactor");
    }

    public static string GetRealName(this PlayerControl player, bool isMeeting = false)
    {
        return isMeeting ? player?.Data?.PlayerName : player?.name;
    }
    public static bool CanUseKillButton(this PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel || Pelican.IsEaten(pc.PlayerId)) return false;

        return pc.GetCustomRole() switch
        {
            //SoloKombat
            CustomRoles.KB_Normal => pc.SoloAlive(),
            //Standard
            CustomRoles.FireWorks => FireWorks.CanUseKillButton(pc),
            CustomRoles.Mafia => Utils.CanMafiaKill(),
            CustomRoles.Sniper => Sniper.CanUseKillButton(pc),
            CustomRoles.Sheriff => Sheriff.CanUseKillButton(pc.PlayerId),
            CustomRoles.Pelican => pc.IsAlive(),
            CustomRoles.Arsonist => !pc.IsDouseDone(),
            CustomRoles.Revolutionist => !pc.IsDrawDone(),
            CustomRoles.SwordsMan => pc.IsAlive(),
            CustomRoles.Jackal => pc.IsAlive(),
            CustomRoles.MengJiangGirl => pc.IsAlive(),
            CustomRoles.YinLang => pc.IsAlive(),
            CustomRoles.Innocent => pc.IsAlive(),
            CustomRoles.Counterfeiter => Counterfeiter.CanUseKillButton(pc.PlayerId),
            CustomRoles.FFF => pc.IsAlive(),
            CustomRoles.OpportunistKiller => pc.IsAlive(),
            CustomRoles.Shifter => pc.IsAlive(),
            CustomRoles.Crush => pc.IsAlive(),
            CustomRoles.PlagueDoctor => pc.IsAlive(),
            CustomRoles.Hunter => pc.IsAlive(),
            CustomRoles.EvilMini => pc.IsAlive(),
            CustomRoles.Cupid => pc.IsAlive(),
            CustomRoles.Akujo => pc.IsAlive(),
            CustomRoles.Medic => Medic.CanUseKillButton(pc.PlayerId),
            CustomRoles.Gamer => pc.IsAlive(),
            CustomRoles.DarkHide => DarkHide.CanUseKillButton(pc),
            CustomRoles.Provocateur => pc.IsAlive(),
            CustomRoles.Assassin => Options.AssassinateCanKill.GetBool(),
            CustomRoles.BloodKnight => pc.IsAlive(),
            CustomRoles.Slaveowner => pc.IsAlive(),
            CustomRoles.Crewpostor => false,
            CustomRoles.Totocalcio => Totocalcio.CanUseKillButton(pc),
            CustomRoles.Succubus => Succubus.CanUseKillButton(pc),
            CustomRoles.SpecialAgent => false,
            CustomRoles.Warlock => !Main.isCurseAndKill.TryGetValue(pc.PlayerId, out bool wcs) || !wcs,
            CustomRoles.Prophet => Prophet.CanUseKillButton(pc.PlayerId),
            CustomRoles.Scout => Scout.CanUseKillButton(pc.PlayerId),
            CustomRoles.Deputy => Deputy.CanUseKillButton(pc.PlayerId),
            CustomRoles.DemonHunterm => DemonHunterm.CanUseKillButton(pc.PlayerId),
            CustomRoles.Prosecutors => Prosecutors.CanUseKillButton(pc.PlayerId),
            CustomRoles.Jealousy => pc.IsAlive(),
            CustomRoles.SourcePlague => pc.IsAlive(),
            CustomRoles.PlaguesGod => pc.IsAlive(),
            CustomRoles.Captain => Captain.CanUseKillButton(pc.PlayerId),
            CustomRoles.ET => pc.IsAlive(),
            CustomRoles.Lawyer => false,
            CustomRoles.King => pc.IsAlive(),
            CustomRoles.Hotpotato => pc.IsAlive(),
            CustomRoles.Coldpotato => !pc.IsAlive(),
            CustomRoles.BSR => pc.IsAlive(),
            CustomRoles.MimicAss => false,
            CustomRoles.SchrodingerCat => !SchrodingerCat.noteam,
            CustomRoles.ElectOfficials => ElectOfficials.CanUseKillButton(pc.PlayerId),
            CustomRoles.SpeedUp => pc.IsAlive(),
            CustomRoles.Sidekick => Jackal.SidekickCanKill.GetBool(),
            CustomRoles.ChiefOfPolice => ChiefOfPolice.CanUseKillButton(pc.PlayerId),
            CustomRoles.Exorcist => pc.IsAlive(),
            CustomRoles.Guide => !pc.IsAlive(),
            CustomRoles.Knight => Knight.CanUseKillButton(pc.PlayerId),
            CustomRoles.Merchant => Merchant.CanUseKillButton(pc.PlayerId),
            CustomRoles.Thirsty => pc.IsAlive(),
            CustomRoles.NiceTracker => NiceTracker.CanUseKillButton(pc.PlayerId),
            CustomRoles.Yandere => pc.IsAlive(),
            CustomRoles.Henry => pc.IsAlive(),
            CustomRoles.Fake => pc.IsAlive(),
            CustomRoles.RewardOfficer => pc.IsAlive(),
            CustomRoles.Loners => pc.IsAlive(),
            CustomRoles.Meditator => pc.IsAlive(),
            CustomRoles.Challenger => pc.IsAlive(),
            CustomRoles.AnimalRefuser => pc.IsAlive(),
            CustomRoles.UnanimalRefuser => pc.IsAlive(),
            CustomRoles.AttendRefuser => pc.IsAlive(),
            CustomRoles.CrazyRefuser => pc.IsAlive(),
            CustomRoles.Refuser => pc.IsAlive(),
            CustomRoles.Guardian => pc.IsAlive(),
            _ => pc.Is(CustomRoleTypes.Impostor),

        } ;
    }
    public static bool CanUseImpostorVentButton(this PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel) return false;

        return pc.GetCustomRole() switch
        {
            CustomRoles.Minimalism or
            CustomRoles.Sheriff or
            CustomRoles.Innocent or
            CustomRoles.SwordsMan or
            CustomRoles.FFF or
            CustomRoles.Medic or
            CustomRoles.DarkHide or
            CustomRoles.Provocateur or
            CustomRoles.Totocalcio or
            CustomRoles.Succubus or
            CustomRoles.Crewpostor or
            CustomRoles.SpecialAgent or
            CustomRoles.OpportunistKiller or
            CustomRoles.Prophet or
            CustomRoles.Scout or
            CustomRoles.Deputy or
            CustomRoles.DemonHunterm or
            CustomRoles.Hunter or
            CustomRoles.Crush or
            CustomRoles.Cupid or
            CustomRoles.Akujo or
            CustomRoles.Prosecutors or
            CustomRoles.Jealousy or
            CustomRoles.SourcePlague or
            CustomRoles.BSR or
            CustomRoles.ElectOfficials or
            CustomRoles.SpeedUp or
            CustomRoles.ChiefOfPolice or
            CustomRoles.Exorcist or
            CustomRoles.Lawyer or
            CustomRoles.Knight or
            CustomRoles.Merchant or
            CustomRoles.NiceTracker or
            CustomRoles.Yandere or
            CustomRoles.PlagueDoctor or
            CustomRoles.Fake or
            CustomRoles.RewardOfficer or
            CustomRoles.Loners or
            CustomRoles.Meditator or
            CustomRoles.Challenger or
            CustomRoles.Guardian
            => false,

            CustomRoles.Jackal => Jackal.CanVent.GetBool(),
            CustomRoles.Sidekick => Jackal.SidekickCanVent.GetBool(),
            CustomRoles.Pelican => Pelican.CanVent.GetBool(),
            CustomRoles.Gamer => Gamer.CanVent.GetBool(),
            CustomRoles.BloodKnight => BloodKnight.CanVent.GetBool(),
            CustomRoles.PlaguesGod => Options.PlaguesGodCanVent.GetBool(),

            CustomRoles.Arsonist => pc.IsDouseDone(),
            CustomRoles.Revolutionist => pc.IsDrawDone(),
            CustomRoles.YinLang => YinLang.YLCanVent.GetBool(),
            CustomRoles.Shifter => false,
            CustomRoles.ET => true,
            CustomRoles.Captain => true,
            CustomRoles.King => true,
            CustomRoles.Henry => true,

            //SoloKombat
            CustomRoles.KB_Normal => true,

            //烫手的山芋
            CustomRoles.Coldpotato or
            CustomRoles.Hotpotato => false,

            _ => pc.Is(CustomRoleTypes.Impostor),
        } ;
    }
    public static bool IsDousedPlayer(this PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist == null || target == null || Main.isDoused == null) return false;
        Main.isDoused.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDoused);
        return isDoused;
    }
    public static bool IsDrawPlayer(this PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist == null && target == null && Main.isDraw == null) return false;
        Main.isDraw.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDraw);
        return isDraw;
    }
    public static void RpcSetDousedPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDousedPlayer, SendOption.Reliable, -1);//RPCによる同期
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDoused);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetDrawPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDrawPlayer, SendOption.Reliable, -1);//RPCによる同期
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDoused);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ResetKillCooldown(this PlayerControl player)
    {
        Main.AllPlayerKillCooldown[player.PlayerId] = Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更
        switch (player.GetCustomRole())
        {
            case CustomRoles.SerialKiller:
                SerialKiller.ApplyKillCooldown(player.PlayerId); //シリアルキラーはシリアルキラーのキルクールに。
                break;
            case CustomRoles.TimeThief:
                TimeThief.SetKillCooldown(player.PlayerId); //タイムシーフはタイムシーフのキルクールに。
                break;
            case CustomRoles.Mare:
                Mare.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Arsonist:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ArsonistCooldown.GetFloat(); //アーソニストはアーソニストのキルクールに。
                break;
            case CustomRoles.Revolutionist:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.RevolutionistCooldown.GetFloat();
                break;

            case CustomRoles.Jackal:
                Jackal.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Sidekick:
                Main.AllPlayerKillCooldown[player.PlayerId] = Jackal.SidekickKillCoolDown.GetFloat();
                break;
            case CustomRoles.Sheriff:
                Sheriff.SetKillCooldown(player.PlayerId); //シェリフはシェリフのキルクールに。
                break;
            case CustomRoles.Minimalism:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.MNKillCooldown.GetFloat();
                break;
            case CustomRoles.EvilMini:
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.EvilMini) && Mini.Age == 0)
                    {
                        Main.AllPlayerKillCooldown[player.PlayerId] = Mini.MinorCD.GetFloat();
                        Main.EvilMiniKillcooldown[player.PlayerId] = Mini.MinorCD.GetFloat();

                    }
                    else if (pc.Is(CustomRoles.EvilMini) && Mini.Age != 18 && Mini.Age != 0)
                    {
                        Main.AllPlayerKillCooldown[player.PlayerId] = Main.EvilMiniKillcooldownf;
                        Main.EvilMiniKillcooldown[player.PlayerId] = Main.EvilMiniKillcooldownf;
                        player.MarkDirtySettings();
                    }
                    else if (pc.Is(CustomRoles.EvilMini) && Mini.Age == 18)
                    {                      
                        Main.AllPlayerKillCooldown[player.PlayerId] = Mini.MajorCD.GetFloat();
                        player.MarkDirtySettings();
                        player.SyncSettings();

                    }
                }
                break;
            case CustomRoles.SwordsMan:
                SwordsMan.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Zombie:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ZombieKillCooldown.GetFloat();
                Main.AllPlayerSpeed[player.PlayerId] -= Options.ZombieSpeedReduce.GetFloat();
                break;
            case CustomRoles.Scavenger:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ScavengerKillCooldown.GetFloat();
                break;
            case CustomRoles.Bomber:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.BomberKillCooldown.GetFloat();
                break;
            case CustomRoles.Capitalism:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.CapitalismSkillCooldown.GetFloat();
                break;
            case CustomRoles.Pelican:
                Main.AllPlayerKillCooldown[player.PlayerId] = Pelican.KillCooldown.GetFloat();
                break;
            case CustomRoles.Counterfeiter:
                Counterfeiter.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.FFF:
                Main.AllPlayerKillCooldown[player.PlayerId] = 0f;
                break;
            case CustomRoles.Cleaner:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.CleanerKillCooldown.GetFloat();
                break;
           case CustomRoles.Medic:
                Medic.SetKillCooldown(player.PlayerId);
                break;
         
            case CustomRoles.Gamer:
                Gamer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.BallLightning:
                BallLightning.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.DarkHide:
                DarkHide.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Greedier:
                Greedier.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.QuickShooter:
                QuickShooter.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Provocateur:
                Main.AllPlayerKillCooldown[player.PlayerId] = 0f;
                break;
            case CustomRoles.Sans:
                Sans.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Hacker:
                Hacker.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.KB_Normal:
                Main.AllPlayerKillCooldown[player.PlayerId] = SoloKombatManager.KB_ATKCooldown.GetFloat();
                break;
            case CustomRoles.Bard:
                for (int i = 0; i < Main.BardCreations; i++)
                    Main.AllPlayerKillCooldown[player.PlayerId] /= 2;
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Crewpostor:
                Main.AllPlayerKillCooldown[player.PlayerId] = 300f;
                break;
            case CustomRoles.Totocalcio:
                Totocalcio.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Gangster:
                Gangster.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Succubus:
                Succubus.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Captain:
                Captain.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Depressed:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.DepressedKillCooldown.GetFloat();
                break;
            case CustomRoles.EvilGambler:
                EvilGambler.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Fraudster:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.FraudsterKillCooldown.GetFloat();
                break;
            case CustomRoles.OpportunistKiller:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.OpportunistKillerKillCooldown.GetFloat();
                break;
            case CustomRoles.YinLang:
                Main.AllPlayerKillCooldown[player.PlayerId] = YinLang.YLKillCooldown.GetFloat();
                break;
            case CustomRoles.captor:
                Main.AllPlayerKillCooldown[player.PlayerId] = ModeArrestManager.Arrestkillcd.GetFloat();
                break;
            case CustomRoles.Shifter:
                Main.AllPlayerKillCooldown[player.PlayerId] = Shifter.KillCooldown.GetFloat();
                break;
            case CustomRoles.Cultivator:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.CultivatorKillCooldown.GetFloat();
                break;
            case CustomRoles.Disorder:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.DisorderKillCooldown.GetFloat();
                break;
            case CustomRoles.sabcat:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.sabcatKillCooldown.GetFloat();
                break;
            case CustomRoles.Prophet:
                Prophet.SetKillCooldown(player.PlayerId);
               break;
            case CustomRoles.Scout:
                Scout.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.FreeMan:
                Main.AllPlayerSpeed[player.PlayerId] = 5f;
                break;
            case CustomRoles.Deputy:
                Deputy.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Prosecutors:
                Prosecutors.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.DemonHunterm:
                DemonHunterm.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Vandalism:
                Vandalism.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Followers:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.FollowersSkillCooldown.GetFloat();
                break;
            case CustomRoles.DemolitionManiac:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.DemolitionManiacKillCooldown.GetFloat();
                break;
            case CustomRoles.Hunter:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.HunterSkillCooldown.GetFloat();
                break;
            case CustomRoles.Crush:
                Main.AllPlayerKillCooldown[player.PlayerId] = 1f;
                break;
            case CustomRoles.Cupid:
                Main.AllPlayerKillCooldown[player.PlayerId] = 1f;
                break;
            case CustomRoles.Akujo:
                Main.AllPlayerKillCooldown[player.PlayerId] = 1f;
                break;
            case CustomRoles.Slaveowner:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.SlaveownerKillCooldown.GetFloat();
                break;
            case CustomRoles.Spellmaster:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.SpellmasterKillCooldown.GetFloat();
                break;
            case CustomRoles.Jealousy:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.JealousyKillCooldown.GetInt();
                break;
            case CustomRoles.PlaguesGod:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.PlaguesGodKillCooldown.GetInt();
                break;
            case CustomRoles.SourcePlague:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.SourcePlagueKillCooldown.GetInt();
                break;
            case CustomRoles.King:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.KingKillCooldown.GetInt();
                break;
            case CustomRoles.Hotpotato:
                Main.AllPlayerKillCooldown[player.PlayerId] = 10f;
                break;
            case CustomRoles.Coldpotato:
                Main.AllPlayerKillCooldown[player.PlayerId] = 10f;
                break;
            case CustomRoles.ElectOfficials:
                ElectOfficials.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.BSR:
                BSR.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.SpeedUp:
                SpeedUp.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.ChiefOfPolice:
                ChiefOfPolice.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Exorcist:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ExorcistKillCooldown.GetInt();
                break;
            case CustomRoles.DestinyChooser:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.DestinyChooserKillColldown.GetInt();
                break;
            case CustomRoles.Hemophobia:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.HemophobiaKillColldown.GetInt();
                break;
            case CustomRoles.Guide:
                Main.AllPlayerKillCooldown[player.PlayerId] = 300f;
                break;
            case CustomRoles.Knight:
                Knight.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Squeezers:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.SqueezersKillColldown.GetInt();
                break;
            case CustomRoles.DoubleKiller:
                DoubleKiller.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Merchant:
                Merchant.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.NiceTracker:
                NiceTracker.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Yandere:
                Yandere.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.PlagueDoctor:
                PlagueDoctor.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Henry:
                Henry.SetKillCooldown(player.PlayerId);
                break;
           // case CustomRoles.Kidnapper:
             //   Kidnapper.SetKillCooldown(player.PlayerId);
            //    break;
            case CustomRoles.MimicKiller:
                Mimics.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.ShapeShifters:
                ShapeShifters.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Fake:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.KillColldown.GetInt();
                break;
            case    CustomRoles.RewardOfficer:
                    Main.AllPlayerKillCooldown[player.PlayerId] = 1f;
                break;
            case CustomRoles.Loners:
                Loners.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Meditator:
                Meditator.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Challenger:
                Main.AllPlayerKillCooldown[player.PlayerId] = 10f;
                break;
            case CustomRoles.AnimalRefuser:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.RefuserKillCooldown.GetFloat();
                break;
            case CustomRoles.UnanimalRefuser:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.RefuserKillCooldown.GetFloat();
                break;
            case CustomRoles.CrazyRefuser:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.RefuserKillCooldown.GetFloat();
                break;
            case CustomRoles.ZeyanRefuser:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.RefuserKillCooldown.GetFloat();
                break;
            case CustomRoles.Refuser:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.RefuserKillCooldown.GetFloat();
                break;
            case CustomRoles.Guardian:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.GuardianCooldown.GetFloat();
                break;
        }
        if (player.PlayerId == LastImpostor.currentId)
            LastImpostor.SetKillCooldown();
    }
    public static void TrapperKilled(this PlayerControl killer, PlayerControl target)
    {
        Logger.Info($"{target?.Data?.PlayerName}はTrapperだった", "Trapper");
        var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;    //tmpSpeedで後ほど値を戻すので代入しています。
        ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
        killer.MarkDirtySettings();
        new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
            killer.MarkDirtySettings();
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
        }, Options.TrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
    }
    public static bool IsDouseDone(this PlayerControl player)
    {
        if (!player.Is(CustomRoles.Arsonist)) return false;
        var count = Utils.GetDousedPlayerCount(player.PlayerId);
        return count.Item1 >= count.Item2;
    }
    public static bool IsDrawDone(this PlayerControl player)//判断是否拉拢完成
    {
        if (!player.Is(CustomRoles.Revolutionist)) return false;
        var count = Utils.GetDrawPlayerCount(player.PlayerId, out var _);
        return count.Item1 >= count.Item2;
    }
    public static void RpcExileV2(this PlayerControl player)
    {
        player.Exiled();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.None, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcMurderPlayerV3(this PlayerControl killer, PlayerControl target)
    {
        //用于TOHE的击杀前判断

        if (Options.CurrentGameMode == CustomGameMode.SoloKombat) return;

        if (killer.PlayerId == target.PlayerId && killer.shapeshifting)
        {
            new LateTask(() => { killer.RpcMurderPlayer(target, true); }, 1.5f, "Shapeshifting Suicide Delay");
            return;
        }

        killer.RpcMurderPlayer(target, true);
    }
    public static void SetRoleV2(this PlayerControl target, RoleTypes roleTypes)
    {
        var sender = new CustomRpcSender("SetRoleSender", SendOption.Reliable, true);
        sender.StartMessage(-1); // 5: GameData
        sender.StartRpc(target.NetId, RpcCalls.SetRole)
                        .Write((ushort)roleTypes)
                        .EndRpc();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.SetRole, SendOption.None, -1);
        writer.Write((ushort)roleTypes);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        target.Data.Role.Role = roleTypes;
        target.Data.RoleType = roleTypes;
        target.SetRole(roleTypes);
        target.RpcSetRole(roleTypes);
    }
    public static void ReviveV2(this PlayerControl target, RoleTypes original)
    {
        //MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ReviveV2, SendOption.Reliable, -1);
        
      //  writer.Write(target.Data.IsDead = false);
        //AmongUsClient.Instance.FinishRpcImmediately(writer);
        target.Revive();
        target.Data.IsDead = false;
        target.SetRoleV2(original);
    }
    public static void RpcMurderPlayerV2(this PlayerControl killer, PlayerControl target)
    {
        if (target == null) target = killer;
        if (AmongUsClient.Instance.AmClient)
        {
            killer.MurderPlayer(target, ResultFlags);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((int)ResultFlags);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        Utils.NotifyRoles();
    }
    public static bool RpcCheckAndMurder(this PlayerControl killer, PlayerControl target, bool check = false) => CheckMurderPatch.RpcCheckAndMurder(killer, target, check);
    public static void NoCheckStartMeeting(this PlayerControl reporter, GameData.PlayerInfo target, bool force = false)
    { /*サボタージュ中でも関係なしに会議を起こせるメソッド
        targetがnullの場合はボタンとなる*/
        if (Options.DisableMeeting.GetBool() && !force) return;
        ReportDeadBodyPatch.AfterReportTasks(reporter, target);
        MeetingRoomManager.Instance.AssignSelf(reporter, target);
        DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
        reporter.RpcStartMeeting(target);
    }
    public static bool IsModClient(this PlayerControl player) => Main.playerVersion.ContainsKey(player.PlayerId);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
    ///</summary>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
    ///</summary>
    ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
    {
        var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
        List<PlayerControl> rangePlayers = new();
        player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
        foreach (var pc in rangePlayersIL)
        {
            if (predicate(pc)) rangePlayers.Add(pc);
        }
        return rangePlayers;
    }
    public static bool IsNeutralKiller(this PlayerControl player) => player.GetCustomRole().IsNK();
    public static bool IsNKS(this PlayerControl player) => player.GetCustomRole().IsNKS();
    public static bool KnowDeathReason(this PlayerControl seer, PlayerControl target)
        => (seer.Is(CustomRoles.Doctor)
        || (seer.Data.IsDead && Options.GhostCanSeeDeathReason.GetBool()))
        && target.Data.IsDead;
    public static string GetRoleInfo(this PlayerControl player, bool InfoLong = false)
    {
        var role = player.GetCustomRole();
        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
            InfoLong = false;

        var text = role.ToString();

        var Prefix = "";
        if (!InfoLong)
            switch (role)
            {
                case CustomRoles.Mafia:
                    Prefix = Utils.CanMafiaKill() ? "After" : "Before";
                    break;
            };
        var Info = (role.IsVanilla() ? "Blurb" : "Info") + (InfoLong ? "Long" : "");
        return GetString($"{Prefix}{text}{Info}");
    }
    public static void SetRealKiller(this PlayerControl target, PlayerControl killer, bool NotOverRide = false)
    {
        if (target == null)
        {
            Logger.Info("target=null", "SetRealKiller");
            return;
        }
        var State = Main.PlayerStates[target.PlayerId];
        if (State.RealKiller.Item1 != DateTime.MinValue && NotOverRide) return; //既に値がある場合上書きしない
        byte killerId = killer == null ? byte.MaxValue : killer.PlayerId;
        RPC.SetRealKiller(target.PlayerId, killerId);
    }
    public static PlayerControl GetRealKiller(this PlayerControl target)
    {
        var killerId = Main.PlayerStates[target.PlayerId].GetRealKiller();
        return killerId == byte.MaxValue ? null : Utils.GetPlayerById(killerId);
    }
    public static PlainShipRoom GetPlainShipRoom(this PlayerControl pc)
    {
        if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return null;
        var Rooms = ShipStatus.Instance.AllRooms;
        if (Rooms == null) return null;
        foreach (var room in Rooms)
        {
            if (!room.roomArea) continue;
            if (pc.Collider.IsTouching(room.roomArea))
                return room;
        }
        return null;
    }

    //汎用
    public static bool Is(this PlayerControl target, CustomRoles role) =>
        role > CustomRoles.NotAssigned ? target.GetCustomSubRoles().Contains(role) : target.GetCustomRole() == role;
    public static bool Is(this PlayerControl target, CustomRoleTypes type) { return target.GetCustomRole().GetCustomRoleTypes() == type; }
    public static bool Is(this PlayerControl target, RoleTypes type) { return target.GetCustomRole().GetRoleTypes() == type; }
    public static bool Is(this PlayerControl target, CountTypes type) { return target.GetCountTypes() == type; }
    public static bool IsAlive(this PlayerControl target)
    {
        //ロビーなら生きている
        //targetがnullならば切断者なので生きていない
        //targetがnullでなく取得できない場合は登録前なので生きているとする
        if (target == null || target.Is(CustomRoles.GM) || target.Is(CustomRoles.Glitch)) return false;
        return GameStates.IsLobby || (target != null && (!Main.PlayerStates.TryGetValue(target.PlayerId, out var ps) || !ps.IsDead));
    }
    public const MurderResultFlags ResultFlags = MurderResultFlags.Succeeded | MurderResultFlags.DecisionByHost;
}