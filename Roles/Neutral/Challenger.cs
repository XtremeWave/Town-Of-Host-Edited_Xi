using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheOtherRoles_Host.Modules.ChatManager;
using UnityEngine;
using static TheOtherRoles_Host.Translator;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TheOtherRoles_Host.Roles.Crewmate;

public static class Challenger
{
    private static readonly int Id = 2994523;
    public static OptionItem SwapMax;
    public static OptionItem CooldwonMax;
    public static List<byte> playerIdList = new();
    public static List<byte> ForChallenger = new(); 
    public static List<byte> ForChallengerTwo = new();
    public static List<byte> Stone = new();
    public static List<byte> Scissors = new();
    public static List<byte> Paper = new();
    public static Dictionary<byte, int> NiceSwappermax = new();
    public static Dictionary<byte, Vector2> Challengerbacktrack = new();
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Challenger);
        SwapMax = IntegerOptionItem.Create(Id + 3, "ChallengerMax", new(1, 999, 1), 3, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Challenger])
            .SetValueFormat(OptionFormat.Times);
        CooldwonMax = IntegerOptionItem.Create(Id + 8, "CooldwonMax", new(1, 999, 1), 5, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Challenger])
    .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        ForChallenger = new();
        Stone = new();
    Scissors = new();
     Paper = new();
    ForChallengerTwo = new();
    NiceSwappermax = new();
        Challengerbacktrack = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NiceSwappermax.TryAdd(playerId, SwapMax.GetInt());
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static string GetNiceSwappermax(byte playerId) => Utils.ColorString((NiceSwappermax.TryGetValue(playerId, out var x) && x >= 1) ? Color.green : Color.gray, NiceSwappermax.TryGetValue(playerId, out var changermax) ? $"({changermax})" : "Invalid");
    public static bool ChallengerMsg(PlayerControl pc, string msg)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!ForChallenger.Contains(pc.PlayerId) || !ForChallengerTwo.Contains(pc.PlayerId) || Stone.Contains(pc.PlayerId) || Scissors.Contains(pc.PlayerId) || Paper.Contains(pc.PlayerId)) return false;
        if (ForChallenger.Contains(pc.PlayerId))
        {
            ChatManager.SendPreviousMessagesToAll();
            return true;
        }
        int operate = 0;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "石头|Stone|剪刀|Scissors|布|Paper", false)) operate = 1;
        else return false;
        if (operate == 1)
        {
            if (Options.NewHideMsg.GetBool())
            {
                ChatManager.SendPreviousMessagesToAll();
            }
            if (ForChallengerTwo.Contains(pc.PlayerId))
            {
               if (CheckCommond(ref msg, "石头|Stone", false))
                {
                    Stone.Add(pc.PlayerId);
                    pc.ShowPopUp(GetString("ChallengerMsg"));
                    foreach (var player in Main.AllPlayerControls)
                    {
                            if (Paper.Contains(player.PlayerId))
                            {
                                MeetingHud.Instance.RpcClose();
                                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.etc;
                            pc.RpcMurderPlayerV3(pc);
                            if (player.Is(CustomRoles.Challenger))
                            {
                                NiceSwappermax[player.PlayerId]--;
                                if (NiceSwappermax[player.PlayerId] <= 0)
                                {
                                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Challenger);
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }                             
                            }
                                return false;
                            }
                            if (Scissors.Contains(player.PlayerId))
                            {
                                MeetingHud.Instance.RpcClose();
                                Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.etc;
                           player.RpcMurderPlayerV3(player);
                            if (pc.Is(CustomRoles.Challenger))
                            {
                                NiceSwappermax[pc.PlayerId]--;
                                if (NiceSwappermax[pc.PlayerId] <= 0)
                                {
                                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Challenger);
                                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                }
                            }
                            return false;
                            }
                            if (Stone.Contains(player.PlayerId))
                            {
                                MeetingHud.Instance.RpcClose();
                                return false;
                            }
                        
                    }
                }
                if (CheckCommond(ref msg, "剪刀|Scissors", false))
                {
                    Scissors.Add(pc.PlayerId);
                    pc.ShowPopUp(GetString("ChallengerMsg"));
                    foreach (var player in Main.AllPlayerControls)
                    {

                            if (Paper.Contains(player.PlayerId))
                            {
                                MeetingHud.Instance.RpcClose();
                                Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.etc;
                            player.RpcMurderPlayerV3(player);
                            if (pc.Is(CustomRoles.Challenger))
                            {
                                NiceSwappermax[pc.PlayerId]--;
                                if (NiceSwappermax[pc.PlayerId] <= 0)
                                {
                                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Challenger);
                                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                }
                            }
                            return false;
                            }
                            if (Stone.Contains(player.PlayerId))
                            {
                                MeetingHud.Instance.RpcClose();
                                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.etc;
                            pc.RpcMurderPlayerV3(pc);
                            if (player.Is(CustomRoles.Challenger))
                            {
                                NiceSwappermax[player.PlayerId]--;
                                if (NiceSwappermax[player.PlayerId] <= 0)
                                {
                                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Challenger);
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                            }
                            return false;
                            }
                            if (Scissors.Contains(player.PlayerId))
                            {
                                MeetingHud.Instance.RpcClose();
                                return false;
                            }
                        
                    }
                }
                if (CheckCommond(ref msg, "布|Paper", false))
                {
                    Paper.Add(pc.PlayerId);
                    pc.ShowPopUp(GetString("ChallengerMsg"));
                    foreach (var player in Main.AllPlayerControls)
                    {

                            if (Scissors.Contains(player.PlayerId))
                            {
                            MeetingHud.Instance.RpcClose();
                            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.etc;
                            pc.RpcMurderPlayerV3(pc);
                            if (player.Is(CustomRoles.Challenger))
                            {
                                NiceSwappermax[player.PlayerId]--;
                                if (NiceSwappermax[player.PlayerId] <= 0)
                                {
                                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Challenger);
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                            }
                            return false;
                            }
                            if (Stone.Contains(player.PlayerId))
                            {
                            MeetingHud.Instance.RpcClose();
                            Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.etc;
                            player.RpcMurderPlayerV3(player);
                            if (pc.Is(CustomRoles.Challenger))
                            {
                                NiceSwappermax[pc.PlayerId]--;
                                if (NiceSwappermax[pc.PlayerId] <= 0)
                                {
                                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Challenger);
                                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                }
                            }
                            return false;
                            }
                            if (Paper.Contains(player.PlayerId))
                            {
                                MeetingHud.Instance.RpcClose();
                                return false;
                            }
                        
                    }
                }
            }







        }
        return true;
    }
      
    
    private static bool MsgToPlayerAndRole(string msg, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("(石头|Stone|剪刀|Scissors|布|Paper)");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;


        if (!string.Equals(r.ToString(), "石头|Stone|剪刀|Scissors|布|Paper"))
        {
            error = GetString("SwapNull");
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
    public static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Challenger, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        byte PlayerId = reader.ReadByte();
        ChallengerMsg(pc, $"/sw {PlayerId}");
        if (Options.NewHideMsg.GetBool())
        {
            ChatManager.SendPreviousMessagesToAll();
        }
    }
}
