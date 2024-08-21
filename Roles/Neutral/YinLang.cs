using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHEXI.Roles.Neutral;
using UnityEngine;
using static Logger;
using static TOHEXI.Translator;


namespace TOHEXI.Roles.Crewmate;

public static class YinLang
{
    private static readonly int Id = 51923;
    public static List<byte> playerIdList = new();

    public static OptionItem YLKillCooldown;
    public static OptionItem YLCanVent;
    public static OptionItem YLHasImpostorVision;
    public static OptionItem YLSJ;
    public static int YLLevel = 0;
    public static int YLdj = 1;
    public static int YLCS = 0;
    public static void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.YinLang, 1, zeroOne: false);
        YLKillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.YinLang])
            .SetValueFormat(OptionFormat.Seconds);
        YLCanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.YinLang]);
        YLHasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.YinLang]);
        YLSJ = FloatOptionItem.Create(Id + 14, "YLSJ", new(1f, 10f, 1f), 3f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.YinLang]);

    }
    public static void Init()
    {
        playerIdList = new();
    }
    public static bool IsEnable => playerIdList.Count > 0;


    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = YLKillCooldown.GetFloat();

    //public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    //{
    //    if (killer.Is(CustomRoles.YinLang))
    //    {
    //        if (YLdj >= 1 && YLdj <= 5)
    //        {
    //            if (killer.PlayerId != target.PlayerId)
    //            {
    //                foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
    //                {
    //                    var pos = target.transform.position;
    //                    var dis = Vector2.Distance(pos, pc.transform.position);
    //                    killer.SetKillCooldownV2(target: target, forceAnime: true);
    //                    Logger.Info("银狼击杀开始", "YL");
    //                    NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang1")));
    //                    Logger.Info($"{target.GetNameWithRole()} |系统警告| => {target.GetNameWithRole()}", "YinLang");
    //                    var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
    //                    Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
    //                    ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
    //                    target.MarkDirtySettings();
    //                    new LateTask(() =>
    //                    {
    //                        NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang1")));
    //                        Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
    //                        ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
    //                        target.MarkDirtySettings();
    //                        RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
    //                    }, 10, "Trapper BlockMove");
    //                    YLLevel += 1;
    //                    YLCS = YinLang.YLSJ.GetInt();
    //                    if (YLLevel == YinLang.YLSJ.GetInt())
    //                    {
    //                        YLdj += 1;
    //                        YLLevel = 0;
    //                    }
    //                    killer.Notify(string.Format(GetString("YinLangLevel"), YLdj, YLLevel, YLCS - YLLevel));
    //                    return false;
    //                }
    //            }
    //        }
    //        else if (YLdj >= 6 && YLdj <= 10)
    //        {
    //            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
    //            {
    //                var pos = target.transform.position;
    //                var dis = Vector2.Distance(pos, pc.transform.position);
    //                killer.SetKillCooldownV2(target: target, forceAnime: true);
    //                Logger.Info("银狼击杀开始", "YL");
    //                NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang2")));
    //                Logger.Info($"{target.GetNameWithRole()} |是否允许更改| => {target.GetNameWithRole()}", "YinLang");
    //                Main.AllPlayerSpeed[target.PlayerId] = 0.01f;
    //                var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
    //                Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
    //                ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
    //                target.MarkDirtySettings();
    //                NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang1")));
    //                Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed - 0.01f;
    //                ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
    //                target.MarkDirtySettings();
    //                RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
    //                Main.AllPlayerSpeed[target.PlayerId] = 0.01f;
    //                YLLevel += 1;
    //                YLCS = YinLang.YLSJ.GetInt();
    //                if (YLLevel == YinLang.YLSJ.GetInt())
    //                {
    //                    YLdj += 1;
    //                    YLLevel = 0;
    //                }
    //                killer.Notify(string.Format(GetString("YinLangLevel"), YLdj, YLLevel, YLCS - YLLevel));
    //                return false;
    //            }
    //        }
    //        else
    //        {
    //            Utils.TP(killer.NetTransform, target.GetTruePosition());
    //            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
    //            Utils.TP(target.NetTransform, Pelican.GetBlackRoomPS());
    //            target.SetRealKiller(killer);
    //            Main.PlayerStates[target.PlayerId].SetDead();
    //            target.RpcMurderPlayerV3(target);
    //            killer.SetKillCooldownV2();
    //            NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.YinLang), GetString("YinLang3")));
    //            killer.Notify(GetString("YinLangNotMax"));
    //            return false;
    //        }
    //        return false;
    //    }
    //    return false;
    //}

}