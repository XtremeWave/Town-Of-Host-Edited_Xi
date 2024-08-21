using Hazel;
using MS.Internal.Xml.XPath;
using Sentry.Protocol;
using System.Collections.Generic;
using UnityEngine;
using static Logger;
using static TOHEXI.Translator;
using System;

namespace TOHEXI.Roles.Neutral;

public class Shifter
{
    private static readonly int Id = 343456665;
    public static List<byte> playerIdList = new();

    public static OptionItem ShifterTQkiller;
    public static OptionItem ShifterCGTQkiller;
    public static OptionItem KillCooldown;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Shifter);
        ShifterTQkiller = BooleanOptionItem.Create(Id + 15, "ShifterTQkiller", true, TabGroup.NeutralRoles, true).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Shifter]);
        ShifterCGTQkiller = BooleanOptionItem.Create(Id + 15 + 15, "ShifterCGTQkiller", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Shifter]);
        KillCooldown = FloatOptionItem.Create(51923 + 100, "JHSKillCooldown", new(5f, 15f, 2.5f), 5f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Shifter])
            .SetValueFormat(OptionFormat.Seconds);
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

    public static bool OnCheckMurder(PlayerControl pc, PlayerControl target)
    {
        //LocateArrow.RemoveAllTarget(pc);
        //SendRPC(apc, false);
        //foreach (var apc in playerIdList)
        //{
        //    LocateArrow.RemoveAllTarget(apc);
        //    SendRPC(apc, false);
        //}

        Logger.Info("成功进入第一阶段", "syz");
        if (target == null || pc == null) return false;
        Logger.Info("成功进入第2阶段", "syz");
        if (pc.GetCustomRole() != CustomRoles.Shifter) return false;
        if(target.GetCustomRole() == CustomRoles.Crewmate) return false;//如果职业是白板船员则拒绝交互
        Logger.Info("成功进入第3阶段", "syz");
        if (target.GetCustomRole().IsShapeshifter())
        {
            Logger.Info("成功紫砂", "syz");
            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Suicide;//死因：自杀
            pc.RpcMurderPlayerV3(pc);//自杀
            pc.Notify(string.Format(GetString("jhs3")));
        }
        if (target.GetCustomRole().IsNK() || target.GetCustomRole().IsCK() || target.GetCustomRole().IsImpostornokill())
        {
            Logger.Info("成功进入是带刀", "syz");
            if (ShifterTQkiller.GetBool())
            {
                Logger.Info("成功紫砂", "syz");
                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Suicide;//死因：自杀
                pc.RpcMurderPlayerV3(pc);//自杀
            }

            if (ShifterCGTQkiller.GetBool())
            {
                Logger.Info("成功进入第4阶段", "syz");

                new LateTask(() =>
                {
                    string roleName = GetString(Enum.GetName(target.GetCustomRole()));
                    //var typeRole = target.GetCustomRole();
                    pc.RpcSetCustomRole(target.GetCustomRole());
                    //var Roles = target.GetCustomRole();
                    pc.Notify(string.Format(GetString("jhs1"), target.name, GetString(roleName)));

                    target.RpcSetCustomRole(CustomRoles.Shifter);
                    target.Notify(string.Format(GetString("jhs2")));
                    Logger.Info("成功进入第5阶段", "syz");
                    //target;
                    //Logger.Info(typeRole, "syz");
                }, 5f, "Shifter Task");

                return false;
            }
            else
            {
                Logger.Info("成功显示名称", "syz");
                pc.Notify(string.Format(GetString("jhs")));
                return false;
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
            pc.Notify(string.Format(GetString("jhs1"),target.name,GetString(roleName)));

            target.RpcSetCustomRole(CustomRoles.Amnesiac);
            target.Notify(string.Format(GetString("jhs2")));
            Logger.Info("成功进入第5阶段", "syz");
            //target;
            //Logger.Info(typeRole, "syz");
        }, 5f, "Shifter Task");

        return false;
    }
}