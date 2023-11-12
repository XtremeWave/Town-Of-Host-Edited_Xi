using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using Steamworks;
using System;
using System.Collections.Generic;
using TheOtherRoles_Host.Roles.Neutral;
using static TheOtherRoles_Host.Options;
using static UnityEngine.GraphicsBuffer;

namespace TheOtherRoles_Host.Roles.Impostor;
public static class Mimics
{
    private static readonly int Id = 574687;
    public static List<byte> playerIdList = new();
    public static OptionItem SKillColldown;
    public static OptionItem DiedToge;
    public static OptionItem Arrow;
    public static GameData.PlayerOutfit TargetSkins = new();
    public static GameData.PlayerOutfit KillerSkins = new();
    public static float KillerSpeed = new();
    public static string KillerName = "";
    public static Dictionary<byte, byte> Kill = new();
    public static Dictionary<byte, byte> Assis = new();
    public static readonly string[] MedicWhoCanSeeProtectName =
{
        "DieSus",
        "BecomeKiller",
        "BecomeImp",
    };
    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Mimics, 1, zeroOne: false);
        SKillColldown = FloatOptionItem.Create(Id + 2, "KillCooldown", new(0f, 100f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mimics])
           .SetValueFormat(OptionFormat.Seconds);
        DiedToge = StringOptionItem.Create(Id + 4, "DieTogether", MedicWhoCanSeeProtectName, 0, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mimics]);
        Arrow = BooleanOptionItem.Create(Id + 3, "HaveArrow", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mimics]);
    }
    public static void Init()
    {
        playerIdList = new();
        TargetSkins = new();
        KillerSkins = new();
        KillerSpeed = new();
        KillerName = "";
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable() => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = SKillColldown.GetFloat();
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (!killer.Is(CustomRoles.MimicKiller)) return;
        GameData.PlayerOutfit outfit = new();
        var sender = CustomRpcSender.Create(name: $"RpcSetSkin({target.Data.PlayerName})");
        KillerSkins = new GameData.PlayerOutfit().Set(killer.GetRealName(), killer.Data.DefaultOutfit.ColorId, killer.Data.DefaultOutfit.HatId, killer.Data.DefaultOutfit.SkinId, killer.Data.DefaultOutfit.VisorId, killer.Data.DefaultOutfit.PetId);
        KillerSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        KillerName = Main.AllPlayerNames[killer.PlayerId];
        TargetSkins = new GameData.PlayerOutfit().Set(target.GetRealName(), target.Data.DefaultOutfit.ColorId, target.Data.DefaultOutfit.HatId, target.Data.DefaultOutfit.SkinId, target.Data.DefaultOutfit.VisorId, target.Data.DefaultOutfit.PetId);
        new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[target.PlayerId];
            var outfit = TargetSkins;
            //凶手变样子
            killer.SetName(outfit.PlayerName);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetName)
                .Write(outfit.PlayerName)
                .EndRpc();
            Main.AllPlayerNames[killer.PlayerId] = Main.AllPlayerNames[target.PlayerId];
            killer.SetColor(outfit.ColorId);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetColor)
                .Write(outfit.ColorId)
                .EndRpc();

            killer.SetHat(outfit.HatId, outfit.ColorId);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetHatStr)
                .Write(outfit.HatId)
                .EndRpc();

            killer.SetSkin(outfit.SkinId, outfit.ColorId);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(outfit.SkinId)
                .EndRpc();

            killer.SetVisor(outfit.VisorId, outfit.ColorId);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(outfit.VisorId)
                .EndRpc();

            killer.SetPet(outfit.PetId);
            sender.AutoStartRpc(killer.NetId, (byte)RpcCalls.SetPetStr)
                .Write(outfit.PetId)
                .EndRpc();
            sender.SendMessage();
        }, 0.1f, "Clam");
        Utils.TP(killer.NetTransform, target.GetTruePosition());
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        Utils.TP(target.NetTransform, Pelican.GetBlackRoomPS());
        target.SetRealKiller(killer);
        Main.PlayerStates[target.PlayerId].SetDead();
        target.RpcMurderPlayerV3(target);
        killer.SetKillCooldownV2();
        NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), Translator.GetString("KilledByMimics")));
        return;

    }
    public static void revert()
    {

        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.Is(CustomRoles.MimicKiller) && pc.IsAlive())
            {
                var sender = CustomRpcSender.Create(name: $"RpcSetSkin({pc.Data.PlayerName})");
                Main.AllPlayerSpeed[pc.PlayerId] = KillerSpeed;
                var outfit = KillerSkins;
                //凶手变样子
                pc.SetName(outfit.PlayerName);
                sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetName)
                    .Write(outfit.PlayerName)
                    .EndRpc();
                Main.AllPlayerNames[pc.PlayerId] = KillerName;
                pc.SetColor(outfit.ColorId);
                sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetColor)
                    .Write(outfit.ColorId)
                    .EndRpc();

                pc.SetHat(outfit.HatId, outfit.ColorId);
                sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetHatStr)
                    .Write(outfit.HatId)
                    .EndRpc();

                pc.SetSkin(outfit.SkinId, outfit.ColorId);
                sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetSkinStr)
                    .Write(outfit.SkinId)
                    .EndRpc();

                pc.SetVisor(outfit.VisorId, outfit.ColorId);
                sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetVisorStr)
                    .Write(outfit.VisorId)
                    .EndRpc();

                pc.SetPet(outfit.PetId);
                sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetPetStr)
                    .Write(outfit.PetId)
                    .EndRpc();
                sender.SendMessage();
            }
        }

    }
   
    public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!(seer.Is(CustomRoles.MimicKiller))) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (!Arrow.GetBool() || GameStates.IsMeeting) return "";
        if (!target.Is(CustomRoles.MimicAss) || target.Is(CustomRoles.MimicAss) && target.Data.IsDead) return "";
        //seerがtarget自身でBountyHunterのとき、
        //矢印オプションがありミーティング以外で矢印表示
        var targetId = target.PlayerId;
        return TargetArrow.GetArrows(seer, targetId);
    }
    public static string GetKillArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!(seer.Is(CustomRoles.MimicAss))) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (!Arrow.GetBool() || GameStates.IsMeeting) return "";
        if (!target.Is(CustomRoles.MimicKiller) || target.Is(CustomRoles.MimicKiller) && target.Data.IsDead) return "";
        //seerがtarget自身でBountyHunterのとき、
        //矢印オプションがありミーティング以外で矢印表示
        var targetId = target.PlayerId;
        return TargetArrow.GetArrows(seer, targetId);
    }
}