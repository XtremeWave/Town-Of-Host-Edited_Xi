using HarmonyLib;
using TOHEXI.Modules;
using TOHEXI.Roles.Impostor;
using TOHEXI.Roles.Neutral;
using UnityEngine;

namespace TOHEXI;

public static class CustomButton
{
    public static Sprite Get(string name) => Utils.LoadSprite($"TOHEXI.Resources.Images.Skills.{name}.png", 115f);
}

[HarmonyPriority(520)]
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudSpritePatch
{
    private static Sprite Kill;
    private static Sprite Ability;
    private static Sprite Vent;
    private static Sprite Report;
    public static void Postfix(HudManager __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || !GameStates.IsModHost) return;
        if (!SetHudActivePatch.IsActive || !player.IsAlive()) return;
        if (!AmongUsClient.Instance.IsGameStarted || !Main.introDestroyed)
        {
            Kill = null;
            Ability = null;
            Vent = null;
            Report = null;
            return;
        }

        bool shapeshifting = Main.CheckShapeshift.TryGetValue(player.PlayerId, out bool ss) && ss;

        if (!Kill) Kill = __instance.KillButton.graphic.sprite;
        if (!Ability) Ability = __instance.AbilityButton.graphic.sprite;
        if (!Vent) Vent = __instance.ImpostorVentButton.graphic.sprite;
        if (!Report) Report = __instance.ReportButton.graphic.sprite; 

        Sprite newKillButton = Kill;
        Sprite newAbilityButton = Ability;
        Sprite newVentButton = Vent;
        Sprite newReportButton = Report;

        if (!Main.EnableCustomButton.Value) goto EndOfSelectImg;

        switch (player.GetCustomRole())
        {
            case CustomRoles.Assassin:
                newAbilityButton = CustomButton.Get("Assassinate");
                break;
            case CustomRoles.Bomber:
                newKillButton = CustomButton.Get("Bomb");
                break;
            case CustomRoles.Concealer:
                newAbilityButton = CustomButton.Get("Camo");
                break;
            case CustomRoles.Arsonist:
                newKillButton = CustomButton.Get("Douse");
                if (player.IsDouseDone()) newVentButton = CustomButton.Get("Ignite");
                break;
            case CustomRoles.FireWorks:
                if (FireWorks.nowFireWorksCount[player.PlayerId] == 0)
                    newAbilityButton = CustomButton.Get("FireworkD");
                else
                    newAbilityButton = CustomButton.Get("FireworkP");
                break;
            case CustomRoles.Hacker:
                newAbilityButton = CustomButton.Get("Hack");
                break;
            case CustomRoles.Hangman:
                if (shapeshifting) newAbilityButton = CustomButton.Get("Hangman");
                break;
            case CustomRoles.Paranoia:
                newAbilityButton = CustomButton.Get("Paranoid");
                break;
            case CustomRoles.Puppeteer:
                newKillButton = CustomButton.Get("Puttpuer");
                break;
            case CustomRoles.Medic:
                newKillButton = CustomButton.Get("Shield");
                break;
            case CustomRoles.Gangster:
                if (Gangster.CanRecruit(player.PlayerId)) newKillButton = CustomButton.Get("Sidekick");
                break;
            case CustomRoles.Succubus:
                newKillButton = CustomButton.Get("Subbus");
                break;
            case CustomRoles.Innocent:
                newKillButton = CustomButton.Get("Suidce");
                break;
            case CustomRoles.EvilTracker:
                newAbilityButton = CustomButton.Get("Track");
                break;
            case CustomRoles.NiceTracker:
                newKillButton = CustomButton.Get("Track");
                break;
            case CustomRoles.Vampire:
                newKillButton = CustomButton.Get("Bite");
                break;
            case CustomRoles.Veteran:
                newAbilityButton = CustomButton.Get("Veteran");
                break;
            case CustomRoles.Pelican:
                newKillButton = CustomButton.Get("Vulture");
                break;
            case CustomRoles.Warlock:
                if (!shapeshifting)
                {
                    newKillButton = CustomButton.Get("Curse");
                    if (Main.isCurseAndKill.TryGetValue(player.PlayerId, out bool curse) && curse)
                        newAbilityButton = CustomButton.Get("CurseKill");
                }
                break;
            case CustomRoles.Rudepeople:
                newAbilityButton = CustomButton.Get("Rude");
                break;
            case CustomRoles.DovesOfNeace:
                newAbilityButton = CustomButton.Get("Neace");
                break;
            case CustomRoles.Jackal:
                if (Jackal.CanAttendant(player.PlayerId)) newKillButton = CustomButton.Get("Sidekick");
                break;
            case CustomRoles.Deputy:
                newKillButton = CustomButton.Get("Deputy");
                break;
         //   case CustomRoles.Prosecutors:
           //     newKillButton = CustomButton.Get("Prosecutors");
           //     break;
            case CustomRoles.Vulture:
                newReportButton = CustomButton.Get("VultureEat");
                break;
            case CustomRoles.Grenadier:
                newAbilityButton = CustomButton.Get("Gangstar");
                break;
            case CustomRoles.TimeMaster:
                newAbilityButton = CustomButton.Get("KingOfTime");
                break;
            case CustomRoles.Amnesiac:
                newReportButton = CustomButton.Get("WohAmI");
                break;
            case CustomRoles.Cleaner:
                newReportButton = CustomButton.Get("Clear");
                break;
            case CustomRoles.TimeStops:
                newAbilityButton = CustomButton.Get("TheWorld");
                break;
            case CustomRoles.Prophet:
                newKillButton = CustomButton.Get("SeeBadorGood");
                break;
            case CustomRoles.Crush:
                newKillButton = CustomButton.Get("Subbus");
                break;
            case CustomRoles.PlagueDoctor:
                newKillButton = CustomButton.Get("InfectButton");
                break;
            case CustomRoles.Captain:
                newKillButton = CustomButton.Get("Sidekick");
                break;
            case CustomRoles.Cupid:
                if (Main.CupidMax[player.PlayerId] < 2)
                {
                    newKillButton = CustomButton.Get("CupidButton");
                }
                if (Main.CupidMax[player.PlayerId] >= 2 && Options.CupidShield.GetBool())
                {
                    newKillButton = CustomButton.Get("Shield");
                }
                break;
            case CustomRoles.Akujo:
                if (Main.AkujoMax[player.PlayerId] < 1)
                {
                    newKillButton = CustomButton.Get("Ho");
                }
                else if (Main.AkujoMax[player.PlayerId] >= 1)
                {
                    newKillButton = CustomButton.Get("sb");
                }
                break;
            case CustomRoles.DestinyChooser:
                newKillButton = CustomButton.Get("Curse");
                break;
            case CustomRoles.Prosecutors:
                newKillButton = CustomButton.Get("Blank");
                break;
            case CustomRoles.Medusa:
                newAbilityButton = CustomButton.Get("Strong");
                break;
        }

    EndOfSelectImg:

        __instance.KillButton.graphic.sprite = newKillButton;
        __instance.AbilityButton.graphic.sprite = newAbilityButton;
        __instance.ImpostorVentButton.graphic.sprite = newVentButton;
        __instance.ReportButton.graphic.sprite = newReportButton;
    }
}
