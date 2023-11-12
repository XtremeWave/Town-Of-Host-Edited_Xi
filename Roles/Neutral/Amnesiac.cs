using Hazel;
using MS.Internal.Xml.XPath;
using Sentry.Protocol;
using System.Collections.Generic;
using UnityEngine;
using static Logger;
using static TheOtherRoles_Host.Translator;
using System;

namespace TheOtherRoles_Host.Roles.Neutral;

public class Amnesiac
{
    //private static readonly int Id = 51923;
    public static List<byte> playerIdList = new();

    public static OptionItem AmnesiacTQkiller;
    public static OptionItem AmnesiacCGTQkiller;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(343456611, TabGroup.NeutralRoles, CustomRoles.Amnesiac);
        AmnesiacTQkiller = BooleanOptionItem.Create(343456632, "AmnesiacTQkiller", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Amnesiac]);
        AmnesiacCGTQkiller = BooleanOptionItem.Create(343456642, "AmnesiacCGTQkiller", true, TabGroup.NeutralRoles, true).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Amnesiac]);
    }
    public static void Init()
    {
        playerIdList = new();
    }

    //private static List<byte> playerIdList = new();
    //private static void SendRPC(byte playerId, bool add, Vector3 loc = new())
    //{
    //    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetVultureArrow, SendOption.Reliable, -1);
    //    writer.Write(playerId);
    //    writer.Write(add);
    //    if (add)
    //    {
    //        writer.Write(loc.x);
    //        writer.Write(loc.y);
    //        writer.Write(loc.z);
    //    }
    //    AmongUsClient.Instance.FinishRpcImmediately(writer);
    //}

    public static void OnReportDeadBody(PlayerControl pc, GameData.PlayerInfo target)
    {
        //LocateArrow.RemoveAllTarget(pc);
        //SendRPC(apc, false);
        //foreach (var apc in playerIdList)
        //{
        //    LocateArrow.RemoveAllTarget(apc);
        //    SendRPC(apc, false);
        //}
        Logger.Info("成功进入第一阶段", "syz");
        if (target == null || pc == null) return;
        Logger.Info("成功进入第2阶段", "syz");
        if (pc.GetCustomRole() != CustomRoles.Amnesiac) return;
        if (target.GetCustomRole() == CustomRoles.Crewmate) return;//如果职业是白板船员则拒绝交互
        if (target.GetCustomRole() == CustomRoles.GM) return;//如果职业是管理员则拒绝交互
        Logger.Info("成功进入第3阶段", "syz");
        if (target.GetCustomRole().IsNK() || target.GetCustomRole().IsCK() || target.GetCustomRole().IsImpostornokill())
        {
            Logger.Info("成功进入是带刀", "syz");
            if (AmnesiacTQkiller.GetBool())
            {
                Logger.Info("成功紫砂", "syz");
                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Suicide;//死因：自杀
                pc.RpcMurderPlayerV3(pc);//自杀
            }

            if (AmnesiacCGTQkiller.GetBool())
            {
                Logger.Info("成功进入第4阶段", "syz");

                new LateTask(() =>
                {
                    string roleName = GetString(Enum.GetName(target.GetCustomRole()));
                    //var typeRole = target.GetCustomRole();
                    pc.RpcSetCustomRole(target.GetCustomRole());
                    //var Roles = target.GetCustomRole();
                    pc.Notify(string.Format(GetString("SYZ1"), target.PlayerName, roleName));

                    Logger.Info("成功进入第5阶段", "syz");
                }, 5f, "Amnesiac Task");

                return;
            }
            else
            {
                Logger.Info("成功显示名称", "syz");
                pc.Notify(string.Format(GetString("SYZ")));
                return;
            }

        }

        //IsImpostornokill
        Logger.Info("成功进入第4阶段", "syz");

        new LateTask(() =>
        {
            string roleName = GetString(Enum.GetName(target.GetCustomRole()));
            //var typeRole = target.GetCustomRole();
            pc.RpcSetCustomRole(target.GetCustomRole());
            //var Roles = target.GetCustomRole();
            pc.Notify(string.Format(GetString("SYZ1"),target.PlayerName,roleName));

            Logger.Info("成功进入第5阶段", "syz");
            //target;
            //Logger.Info(typeRole, "syz");
        }, 5f, "Amnesiac Task");

        return;
    }
}