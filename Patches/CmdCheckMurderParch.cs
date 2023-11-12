//From https://github.com/0xDrMoe/TownofHost-Enhanced

using HarmonyLib;
using TheOtherRoles_Host;
using System;
using UnityEngine;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))] // Modded
class CmdCheckMurderPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        TheOtherRoles_Host.Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}", "Check Murder CMD");

        if (!AmongUsClient.Instance.AmHost) return true;
        return CheckMurderPatch.Prefix(__instance, target);
    }
}