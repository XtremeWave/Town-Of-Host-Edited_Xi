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
public static class ShapeShifters
{
    private static readonly int Id = 521894;
    public static List<byte> playerIdList = new();
    public static OptionItem SKillColldown;
    public static GameData.PlayerOutfit TargetSkins = new();
    public static GameData.PlayerOutfit KillerSkins = new();
    public static float KillerSpeed = new();
    public static string KillerName = "";
    public static float TargetSpeed = new();
    public static string TargetName = "";
    public static Dictionary<byte, byte> Kill = new();
    public static Dictionary<byte, byte> Assis = new();
    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ShapeShifters, 1, zeroOne: false);
        SKillColldown = FloatOptionItem.Create(Id + 2, "KillCooldown", new(0f, 100f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ShapeShifters])
           .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        TargetSkins = new();
        KillerSkins = new();
        KillerSpeed = new();
        KillerName = "";
        TargetSpeed = new();
        TargetName = "";
}
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable() => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = SKillColldown.GetFloat();
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        KillerSkins = new GameData.PlayerOutfit().Set(killer.GetRealName(), killer.Data.DefaultOutfit.ColorId, killer.Data.DefaultOutfit.HatId, killer.Data.DefaultOutfit.SkinId, killer.Data.DefaultOutfit.VisorId, killer.Data.DefaultOutfit.PetId);
        var outfit2 = KillerSkins;
        TargetSkins = new GameData.PlayerOutfit().Set(target.GetRealName(), target.Data.DefaultOutfit.ColorId, target.Data.DefaultOutfit.HatId, target.Data.DefaultOutfit.SkinId, target.Data.DefaultOutfit.VisorId, target.Data.DefaultOutfit.PetId);
        TargetSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        TargetName = Main.AllPlayerNames[killer.PlayerId];
        KillerSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        KillerName = Main.AllPlayerNames[killer.PlayerId];
        if (!killer.Is(CustomRoles.ShapeShifters)) return;
        GameData.PlayerOutfit outfit = new();
        var sender = CustomRpcSender.Create(name: $"RpcSetSkin({target.Data.PlayerName})");
        byte colorId = (byte)outfit2.ColorId;
        target.SetColor(outfit2.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
            .Write(outfit2.ColorId)
            .EndRpc();
        if (!killer.Is(CustomRoles.ShapeShifters)) return;


        new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = TargetSpeed;
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
            Main.AllPlayerSpeed[target.PlayerId] = KillerSpeed;
         
            //目标变样子
            target.SetName(outfit2.PlayerName);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetName)
                .Write(outfit2.PlayerName)
                .EndRpc();
            Main.AllPlayerNames[target.PlayerId] = KillerName;

            target.SetHat(outfit2.HatId, outfit2.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(outfit2.HatId)
                .EndRpc();

            target.SetSkin(outfit2.SkinId, outfit2.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(outfit2.SkinId)
                .EndRpc();

            target.SetVisor(outfit2.VisorId, outfit2.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(outfit2.VisorId)
                .EndRpc();

            target.SetPet(outfit2.PetId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                .Write(outfit2.PetId)
                .EndRpc();
            sender.SendMessage();
        }, 0.1f, "Clam");
    }
}