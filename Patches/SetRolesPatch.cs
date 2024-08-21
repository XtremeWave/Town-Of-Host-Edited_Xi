/*using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using MS.Internal.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHEXI.Modules;
using TOHEXI.Modules.ChatManager;
using TOHEXI.Roles.AddOns.Crewmate;
using TOHEXI.Roles.AddOns.Impostor;
using TOHEXI.Roles.Crewmate;
using TOHEXI.Roles.Double;
using TOHEXI.Roles.Impostor;
using TOHEXI.Roles.Neutral;
using static TOHEXI.Modules.CustomRoleSelector;
using static TOHEXI.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHEXI;
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnWaitForHost))]
internal class Wait
{
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        try
        {

        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Select Role Postfix");
            Logger.Fatal(ex.ToString(), "Select Role Postfix");
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SetRole))]
    internal class SetRolesPatch
    {
        public static void Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            try
            {

            }
            catch (Exception ex)
            {
                Utils.ErrorEnd("Select Role Postfix");
                Logger.Fatal(ex.ToString(), "Select Role Postfix");
            }
        }

        public static void AssignDesyncRole(CustomRoles role, PlayerControl player, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
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

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
        public class RpcSetRoleReplacer
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
    public static class SetNewRoles
    {
        public static void SetNewRole(this PlayerControl pc)
        {
            Dictionary<byte, CustomRpcSender> senders = new();

            senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                    .StartMessage(pc.GetClientId());
            Dictionary<(byte, byte), RoleTypes> rolesMap = new();
            foreach (var cp in RoleResult.Where(x => x.Value == CustomRoles.Sidekick))
                SetRolesPatch.AssignDesyncRole(cp.Value, cp.Key, senders, rolesMap, BaseRole: RoleTypes.Impostor, hostBaseRole: RoleTypes.Crewmate);
            pc.ResetKillCooldown();

            var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({pc.Data.PlayerName})");
            pc.SetRole(RoleTypes.Impostor);
        }
    }
}*/


