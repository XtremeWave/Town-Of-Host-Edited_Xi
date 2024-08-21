using Hazel;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using static TOHEXI.Translator;
using static UnityEngine.GraphicsBuffer;
using TOHEXI.Modules.ChatManager;

namespace TOHEXI.Roles.Crewmate;

public static class Copycat
{
    private static readonly int Id = 215886523;
    public static List<byte> playerIdList = new();
    public static List<byte> CopycatFor = new();
    public static List<byte> ForCopycat = new();
    public static OptionItem CopycatCountMode;
    public static OptionItem CanImpostorAndNeutarl;
    public static OptionItem CanEniggerOrSheriffEtc;

    public static readonly string[] copycatCountMode =
    {
         "ChiefOfPoliceCountMode.KillKiller",
        "ChiefOfPoliceCountMode.Warn",
    };
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Copycat);
        CanImpostorAndNeutarl = BooleanOptionItem.Create(Id + 16, "CopycatCanImpostorAndNeutarl", false, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Copycat]);
        CanEniggerOrSheriffEtc = BooleanOptionItem.Create(Id + 14, "CopycatCanEniggerOrSheriffEtc", false, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Copycat]);
        CopycatCountMode = StringOptionItem.Create(Id + 18, "CopycatCountMode", copycatCountMode, 0, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Copycat]);
    }
    public static void Init()
    {
        playerIdList = new();
        CopycatFor = new();
        ForCopycat = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool CopycatMsg(PlayerControl pc, string msg)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Copycat)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (CheckCommond(ref msg, "xp|效颦|效|颦", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("CopycatNotCopy"), pc.PlayerId);
            return true;
        }
        if (operate == 1)
        {
    
            new LateTask(() =>
            {
                if (Options.NewHideMsg.GetBool())
                {
                    ChatManager.SendPreviousMessagesToAll();
                }
                Utils.SendMessage(GuessManager.GetFormatString(), pc.PlayerId);
                
            }, 1f, "石化");     
            return true;
        }
        else if (operate == 2)
        {
            if (Options.NewHideMsg.GetBool())
            {
                ChatManager.SendPreviousMessagesToAll();
            }
            if (!MsgToPlayerAndRole(msg, out byte targetId, out string error))
            {

                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }
            var target = Utils.GetPlayerById(targetId);
            if (target != null)
            {

                bool judgeSuicide = true;
                if (pc.PlayerId == target.PlayerId)
                {
                    Utils.SendMessage(GetString("CopycatNotMe"), pc.PlayerId);
                    return true;
                }
                if (target.IsAlive())
                {
                    Utils.SendMessage(GetString("CopycatNotDead"), pc.PlayerId);
                    return true;
                }
                else if (pc.Is(CustomRoles.Madmate)) judgeSuicide = false;
                else if (target.Is(CustomRoles.Madmate) && CanImpostorAndNeutarl.GetBool()) judgeSuicide = false;
                else if (target.Is(CustomRoles.Charmed) && CanImpostorAndNeutarl.GetBool()) judgeSuicide = false;
                else if (target.Is(CustomRoles.Attendant) && CanImpostorAndNeutarl.GetBool()) judgeSuicide = false;
                else if (target.Is(CustomRoles.Attendant) && CanImpostorAndNeutarl.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsCK() && CanImpostorAndNeutarl.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNKS() && CanImpostorAndNeutarl.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNNK() && CanImpostorAndNeutarl.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsImpostor() && CanImpostorAndNeutarl.GetBool()) judgeSuicide = false;
                else if (target.CanUseKillButton() && target.GetCustomRole().IsCrewmate() && CanEniggerOrSheriffEtc.GetBool()) judgeSuicide = false;
                else if (target.Is(CustomRoles.NiceGuesser) || target.Is(CustomRoles.Judge)) judgeSuicide = true;
                else judgeSuicide = true;

                var dp = judgeSuicide ? pc : target;
                target = dp;

                string Name = dp.GetRealName();
                new LateTask(() =>
                {
                    if (judgeSuicide)
                    {
                        if (CopycatCountMode.GetInt() == 0)
                        {
                            dp.SetRealKiller(dp);
                            GuessManager.RpcGuesserMurderPlayer(dp);
                            return;
                        }
                        if (CopycatCountMode.GetInt() == 1)
                        {
                            Utils.SendMessage(GetString("CopycatNotCan"), dp.PlayerId);
                            return;
                        }
                    }
                    else if(!ForCopycat.Contains(dp.PlayerId))
                    {
                        ForCopycat.Add(dp.PlayerId);
                        CopycatFor.Add(pc.PlayerId);
                        Utils.SendMessage(GetString("CopycatRest"), pc.PlayerId);
                        Logger.Info($"{pc.GetNameWithRole()} 选择 {target.GetNameWithRole()}", "CopyCat");
                        return;
                    }
                    else if (ForCopycat.Contains(dp.PlayerId))
                    {
                        ForCopycat.Remove(dp.PlayerId);
                        CopycatFor.Remove(pc.PlayerId);
                        Utils.SendMessage(GetString("CopycatNotRest"), pc.PlayerId);
                        Logger.Info($"{pc.GetNameWithRole()} 取消选择 {target.GetNameWithRole()}", "CopyCat");
                        return;
                    }
                }, 0.2f, "Trial Kill")
                {

                };
            }
        }

            return true;
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MaxValue;
            error = GetString("CopycatHelp");
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || !target.Data.IsDead)
        {
            error = GetString("CopycatNull");
            return false;
        }

        error = string.Empty;
        return true;
    }
    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Count(); i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Copycat, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

}
