using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOtherRoles_Host.Modules;
using TheOtherRoles_Host.Roles.Crewmate;
using TheOtherRoles_Host.Roles.Impostor;
using TheOtherRoles_Host.Roles.Neutral;
using static TheOtherRoles_Host.Translator;

namespace TheOtherRoles_Host;

/*
 * HUGE THANKS TO
 * ImaMapleTree / 단풍잎 / Tealeaf
 * FOR THE CODE
 */

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TryPet))]
class LocalPetPatch
{
    private static readonly Dictionary<byte, long> LastProcess = new();
    public static bool Prefix(PlayerControl __instance)
    {
        if (!Options.UsePets.GetBool()) return true;
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return true;
        if (GameStates.IsLobby) return true;

        if (__instance.petting) return true;
        __instance.petting = true;

        if (!LastProcess.ContainsKey(__instance.PlayerId)) LastProcess.TryAdd(__instance.PlayerId, Utils.GetTimeStamp() - 2);
        if (LastProcess[__instance.PlayerId] + 1 >= Utils.GetTimeStamp()) return true;

        ExternalRpcPetPatch.Prefix(__instance.MyPhysics, (byte)RpcCalls.Pet);

        LastProcess[__instance.PlayerId] = Utils.GetTimeStamp();
        return !__instance.GetCustomRole().PetActivatedAbility();
    }

    public static void Postfix(PlayerControl __instance)
    {
        if (!Options.UsePets.GetBool()) return;
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return;
        __instance.petting = false;
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
class ExternalRpcPetPatch
{
    private static readonly Dictionary<byte, long> LastProcess = new();
    public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callID)
    {
        if (!Options.UsePets.GetBool() || !AmongUsClient.Instance.AmHost || (RpcCalls)callID != RpcCalls.Pet) return;

        var pc = __instance.myPlayer;
        var physics = __instance;

        if (pc == null || physics == null) return;

        if (pc != null
            && !pc.inVent
            && !pc.inMovingPlat
            && !pc.walkingToVent
            && !pc.onLadder
            && !physics.Animations.IsPlayingEnterVentAnimation()
            && !physics.Animations.IsPlayingClimbAnimation()
            && !physics.Animations.IsPlayingAnyLadderAnimation()
            && !Pelican.IsEaten(pc.PlayerId)
            && GameStates.IsInTask
            && pc.GetCustomRole().PetActivatedAbility())
            physics.CancelPet();

        if (!LastProcess.ContainsKey(pc.PlayerId)) LastProcess.TryAdd(pc.PlayerId, Utils.GetTimeStamp() - 2);
        if (LastProcess[pc.PlayerId] + 1 >= Utils.GetTimeStamp()) return;
        LastProcess[pc.PlayerId] = Utils.GetTimeStamp();

        Logger.Info($"Player {pc.GetNameWithRole().RemoveHtmlTags()} petted their pet", "PetActionTrigger");

        _ = new LateTask(() => { OnPetUse(pc); }, 0.2f, $"OnPetUse: {pc.GetNameWithRole().RemoveHtmlTags()}");
    }
    public static void OnPetUse(PlayerControl pc)
    {
        if (pc == null ||
            pc.inVent ||
            pc.inMovingPlat ||
            pc.onLadder ||
            pc.walkingToVent ||
            pc.MyPhysics.Animations.IsPlayingEnterVentAnimation() ||
            pc.MyPhysics.Animations.IsPlayingClimbAnimation() ||
            pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation() ||
            Pelican.IsEaten(pc.PlayerId))
            return;

        switch (pc.GetCustomRole())
        {
            case CustomRoles.Mayor:
                if (Main.MayorUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt())
                {
                    pc?.ReportDeadBody(null);
                }
                break;
            case CustomRoles.Veteran:
                if (!Main.VeteranNumOfUsed.TryGetValue(pc.PlayerId, out var count3) && count3 < 1)
                {
                    Main.VeteranInProtect.Remove(pc.PlayerId);
                    Main.VeteranInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.VeteranNumOfUsed[pc.PlayerId]--;
                    if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
                    pc.RPCPlayCustomSound("Gunload");
                    pc.Notify(GetString("VeteranOnGuard"), Options.VeteranSkillDuration.GetFloat());
                }
                break;
            case CustomRoles.TimeMaster:
                Main.TimeMasterInProtect.Remove(pc.PlayerId);
                Main.TimeMasterInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
                if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
                pc.Notify(GetString("TimeMasterOnGuard"), Options.TimeMasterSkillDuration.GetFloat());
                foreach (var player in Main.AllPlayerControls)
                {
                    if (Main.TimeMasterbacktrack.ContainsKey(player.PlayerId))
                    {
                        var position = Main.TimeMasterbacktrack[player.PlayerId];
                        Utils.TP(player.NetTransform, position);
                        Main.TimeMasterbacktrack.Remove(player.PlayerId);
                    }
                    else
                    {
                        Main.TimeMasterbacktrack.Add(player.PlayerId, player.GetTruePosition());
                    }
                }
                break;
            case CustomRoles.TimeStops:
                CustomSoundsManager.RPCPlayCustomSoundAll("THEWORLD");
                Main.TimeStopsInProtect.Remove(pc.PlayerId);
                Main.TimeStopsInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
                if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
                pc.RPCPlayCustomSound("THEWORLD");
                pc.Notify(GetString("TimeStopsOnGuard"), Options.TimeStopsSkillDuration.GetFloat());
                foreach (var player in Main.AllAlivePlayerControls)
                {
                    if (pc == player) continue;
                    if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                    NameNotifyManager.Notify(player, Utils.ColorString(Utils.GetRoleColor(CustomRoles.TimeStops), GetString("ForTimeStops")));
                    var tmpSpeed1 = Main.AllPlayerSpeed[player.PlayerId];
                    Main.TimeStopsstop.Add(player.PlayerId);
                    Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
                    ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
                    player.MarkDirtySettings();
                    new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - Main.MinSpeed + tmpSpeed1;
                        ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
                        player.MarkDirtySettings();
                        Main.TimeStopsstop.Remove(player.PlayerId);
                        RPC.PlaySoundRPC(player.PlayerId, Sounds.TaskComplete);
                    }, Options.TimeStopsSkillDuration.GetFloat(), "Time Stop");
                }
                break;
        }
    }
}