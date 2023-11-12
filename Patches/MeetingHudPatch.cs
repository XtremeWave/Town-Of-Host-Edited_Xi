using AmongUs.GameOptions;
using HarmonyLib;
using Lotus.Roles.Internals;
using MS.Internal.Xml.XPath;
using Sentry.Internal.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheOtherRoles_Host.Modules;
using TheOtherRoles_Host.Roles.Crewmate;
using TheOtherRoles_Host.Roles.Double;
using TheOtherRoles_Host.Roles.Impostor;
using TheOtherRoles_Host.Roles.Neutral;
using UnityEngine;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;
using static TheOtherRoles_Host.ChatCommands;
using static TheOtherRoles_Host.Translator;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TheOtherRoles_Host;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
class CheckForEndVotingPatch
{
    public static bool Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        var voteLog = Logger.Handler("Vote");
        try
        {
            List<MeetingHud.VoterState> statesList = new();
            MeetingHud.VoterState[] states;
            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                //死んでいないディクテーターが投票済み

                //主动叛变
                if (pva.DidVote && pc.PlayerId == pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    if (Options.MadmateSpawnMode.GetInt() == 2 && Main.MadmateNum < CustomRoles.Madmate.GetCount() && Utils.CanBeMadmate(pc))
                    {
                        Main.MadmateNum++;
                        pc.RpcSetCustomRole(CustomRoles.Madmate);
                        ExtendedPlayerControl.RpcSetCustomRole(pc.PlayerId, CustomRoles.Madmate);
                        Utils.NotifyRoles(true, pc, true);
                        Logger.Info("设置职业:" + pc?.Data?.PlayerName + " = " + pc.GetCustomRole().ToString() + " + " + CustomRoles.Madmate.ToString(), "Assign " + CustomRoles.Madmate.ToString());
                    }
                }

                //催眠师催眠
                //if (pc.Is(CustomRoles.Hypnotist) && Main.HypnotistMax.Count > 0)
                //{
                //    //opt.SetVision(false);
                //    //opt.SetFloat(FloatOptionNames.CrewLightMod, Options.BewilderVision.GetFloat());
                //    //opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.BewilderVision.GetFloat());
                //}

                if (pc.Is(CustomRoles.Dictator) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                    TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pc.PlayerId);
                    statesList.Add(new()
                    {
                        VoterId = pva.TargetPlayerId,
                        VotedForId = pva.VotedFor
                    });
                    states = statesList.ToArray();
                    if (AntiBlackout.OverrideExiledPlayer)
                    {
                        __instance.RpcVotingComplete(states, null, true);
                        ExileControllerWrapUpPatch.AntiBlackout_LastExiled = voteTarget.Data;
                    }
                    else __instance.RpcVotingComplete(states, voteTarget.Data, false); //通常処理

                    Logger.Info($"{voteTarget.GetNameWithRole()} 被独裁者驱逐", "Dictator");
                    CheckForDeathOnExile(PlayerState.DeathReason.Vote, pva.VotedFor);
                    Logger.Info("独裁投票，会议强制结束", "Special Phase");
                    voteTarget.SetRealKiller(pc);
                    Main.LastVotedPlayerInfo = voteTarget.Data;
                    if (Main.LastVotedPlayerInfo != null)
                        ConfirmEjections(Main.LastVotedPlayerInfo);
                    return true;
                }

                if (pva.DidVote && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                    if (voteTarget != null)
                    {
                        switch (pc.GetCustomRole())
                        {
                            case CustomRoles.Divinator:
                                Divinator.OnVote(pc, voteTarget);
                                break;
                            case CustomRoles.Eraser:
                                Eraser.OnVote(pc, voteTarget);
                                break;
                        }
                    }
                }
            }
            foreach (var ps in __instance.playerStates)
            {
                //死んでいないプレイヤーが投票していない
                if (!(Main.PlayerStates[ps.TargetPlayerId].IsDead || ps.DidVote)) return false;
            }

            GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
            bool tie = false;

            for (var i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea ps = __instance.playerStates[i];
                if (ps == null) continue;
                voteLog.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, Utils.PadRightV2($"({Utils.GetVoteName(ps.TargetPlayerId)})", 40), ps.VotedFor, $"({Utils.GetVoteName(ps.VotedFor)})"));
                var voter = Utils.GetPlayerById(ps.TargetPlayerId);
                if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                if (Options.VoteMode.GetBool())
                {
                    if (ps.VotedFor == 253 && !voter.Data.IsDead && //スキップ
                        !(Options.WhenSkipVoteIgnoreFirstMeeting.GetBool() && MeetingStates.FirstMeeting) && //初手会議を除く
                        !(Options.WhenSkipVoteIgnoreNoDeadBody.GetBool() && !MeetingStates.IsExistDeadBody) && //死体がない時を除く
                        !(Options.WhenSkipVoteIgnoreEmergency.GetBool() && MeetingStates.IsEmergencyMeeting) //緊急ボタンを除く
                        )
                    {
                        switch (Options.GetWhenSkipVote())
                        {
                            case VoteMode.Suicide:
                                TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                voteLog.Info($"{voter.GetNameWithRole()}因跳过投票自杀");
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                voteLog.Info($"{voter.GetNameWithRole()}因跳过投票自票");
                                break;
                            default:
                                break;
                        }
                    }
                    if (ps.VotedFor == 254 && !voter.Data.IsDead)//無投票
                    {
                        switch (Options.GetWhenNonVote())
                        {
                            case VoteMode.Suicide:
                                TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票自杀");
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票自票");
                                break;
                            case VoteMode.Skip:
                                ps.VotedFor = 253;
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票跳过");
                                break;
                            default:
                                break;
                        }
                    }
                }

                //隐藏占卜师的票
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Divinator) && Divinator.HideVote.GetBool()) continue;
                //隐藏抹除者的票
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Eraser) && Eraser.HideVote.GetBool()) continue;

                

                //主动叛变模式下自票无效
                if (ps.TargetPlayerId == ps.VotedFor && Options.MadmateSpawnMode.GetInt() == 2) continue;
          
                statesList.Add(new MeetingHud.VoterState()
                {
                    VoterId = ps.TargetPlayerId,
                    VotedForId = ps.VotedFor
                });
                /*#region 正义换票判断
                if (NiceSwapper.Vote.Count > 0 && NiceSwapper.VoteTwo.Count > 0)
                {
                    List<byte> NiceList1 = new();
                    List<byte> BeSwapped = new();
                    var meetingHud = MeetingHud.Instance;
                    PlayerControl swap1 = null;
                    foreach (var playerId in NiceSwapper.Vote)
                    {
                        swap1 = Utils.GetPlayerById(playerId);
                    }
                    PlayerControl swap2 = null;
                    foreach (var playerId in NiceSwapper.VoteTwo)
                    {
                        swap2 = Utils.GetPlayerById(playerId);
                    }
                    if (swap1 != null && swap2 != null)
                    {
                        
                        if (ps.VotedFor == swap1.PlayerId && !BeSwapped.Contains(ps.TargetPlayerId) && voter.IsAlive())
                        {
                            ps.VotedFor = swap2.PlayerId;
                            voteLog.Info($"{voter.GetNameWithRole()}投给{swap1.GetNameWithRole()}的票选交换给了{swap2.GetNameWithRole()}");
                            NiceList1.Add(ps.TargetPlayerId);
                            BeSwapped.Add(ps.TargetPlayerId);
                        }
                        else if (ps.VotedFor == swap2.PlayerId && !NiceList1.Contains(ps.TargetPlayerId) &&!BeSwapped.Contains(ps.TargetPlayerId) && voter.IsAlive())
                        {
                            ps.VotedFor = swap1.PlayerId;
                            BeSwapped.Add(ps.TargetPlayerId);
                            voteLog.Info($"{voter.GetNameWithRole()}投给{swap2.GetNameWithRole()}的票选交换给了{swap1.GetNameWithRole()}");
                        }
                        if (Main.NiceSwapSend == false)
                        {
                            Utils.SendMessage(string.Format(GetString("SwapVote"), swap1.GetRealName(), swap2.GetRealName()), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceSwapper), GetString("SwapTitle")));
                            Main.NiceSwapSend = true;
                            NiceList1.Clear();
                        }
                    }
                }
        
                #endregion*/
                #region 换票
                #region 正义换票判断
                if (NiceSwapper.Vote.Count > 0 && NiceSwapper.VoteTwo.Count > 0)
                {
                    List<byte> NiceList1 = new();
                    List<byte> NiceList2 = new();
                    PlayerVoteArea pva = new();
                    var meetingHud = MeetingHud.Instance;
                        PlayerControl swap1 = null;
                        foreach (var playerId in NiceSwapper.Vote)
                        {
                            var player = Utils.GetPlayerById(playerId);
                            if (player != null)
                            {
                                swap1 = player;
                                break;
                            }
                        }
                        PlayerControl swap2 = null;
                        foreach (var playerId in NiceSwapper.VoteTwo)
                        {
                            var player = Utils.GetPlayerById(playerId);
                            if (player != null)
                            {
                                swap2 = player;
                                break;
                            }
                        }
                    if (swap1 != null && swap2 != null)
                    {
                        for (int ia = 0; ia < __instance.playerStates.Length; ia++) //Loops through all players
                        {
                            PlayerVoteArea playerVoteArea = __instance.playerStates[ia];
                            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
                            if (playerVoteArea.VotedFor == swap1.PlayerId && !playerVoteArea.AmDead)
                            {
                                
                                //playerVoteArea.UnsetVote();
                                //meetingHud.CastVote(voteAreaPlayer.PlayerId, swap2.PlayerId);
                                playerVoteArea.VotedFor = swap2.PlayerId;
                                NiceList1.Add(voteAreaPlayer.PlayerId);
                            }
                        }
                        for (int ia = 0; ia < __instance.playerStates.Length; ia++) //Loops through all players
                        {
                            PlayerVoteArea playerVoteArea = __instance.playerStates[ia];
                            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
                            if (playerVoteArea.VotedFor == swap1.PlayerId && !playerVoteArea.AmDead && !NiceList1.Contains(voteAreaPlayer.PlayerId))
                            {
                              
                               //if (NiceList1.Contains(voteAreaPlayer.PlayerId)) continue;
                                //playerVoteArea.UnsetVote();
                                playerVoteArea.VotedFor = swap1.PlayerId;
                                //meetingHud.CastVote(voteAreaPlayer.PlayerId, swap1.PlayerId);
                                NiceList2.Add(voteAreaPlayer.PlayerId);
                            }
                        }
                        for (int ia = 0; ia < __instance.playerStates.Length; ia++) //Loops through all players
                        {
                            PlayerVoteArea playerVoteArea = __instance.playerStates[ia];
                            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
                            if (playerVoteArea.VotedFor == swap1.PlayerId && !playerVoteArea.AmDead && !NiceList2.Contains(voteAreaPlayer.PlayerId))
                            {

                                //playerVoteArea.UnsetVote();
                                //meetingHud.CastVote(voteAreaPlayer.PlayerId, swap2.PlayerId);
                                playerVoteArea.VotedFor = swap2.PlayerId;
                                NiceList1.Add(voteAreaPlayer.PlayerId);
                            }
                        }
                        if (Main.NiceSwapSend == false)
                        {
                            Utils.SendMessage(string.Format(GetString("SwapVote"), swap1.GetRealName(), swap2.GetRealName()), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceSwapper), GetString("SwapTitle")));
                            Main.NiceSwapSend = true;
                            NiceList1.Clear();
                        }
                        NiceSwapper.Vote.Clear();
                        NiceSwapper.VoteTwo.Clear();
                    }
                }

                #endregion
                #region 邪恶换票判断
                if (EvilSwapper.Vote.Count > 0 && EvilSwapper.VoteTwo.Count > 0)
                {
                    List<byte> NiceList1 = new();
                    List<byte> NiceList2 = new();
                    PlayerVoteArea pva = new();
                    var meetingHud = MeetingHud.Instance;
                    PlayerControl swap1 = null;
                    foreach (var playerId in EvilSwapper.Vote)
                    {
                        var player = Utils.GetPlayerById(playerId);
                        if (player != null)
                        {
                            swap1 = player;
                            break;
                        }
                    }
                    PlayerControl swap2 = null;
                    foreach (var playerId in EvilSwapper.VoteTwo)
                    {
                        var player = Utils.GetPlayerById(playerId);
                        if (player != null)
                        {
                            swap2 = player;
                            break;
                        }
                    }
                    if (swap1 != null && swap2 != null)
                    {
                        for (int ia = 0; ia < __instance.playerStates.Length; ia++) //Loops through all players
                        {
                            PlayerVoteArea playerVoteArea = __instance.playerStates[ia];
                            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
                            if (playerVoteArea.VotedFor == swap1.PlayerId && !playerVoteArea.AmDead)
                            {

                                //playerVoteArea.UnsetVote();
                                //meetingHud.CastVote(voteAreaPlayer.PlayerId, swap2.PlayerId);
                                playerVoteArea.VotedFor = swap2.PlayerId;
                                NiceList1.Add(voteAreaPlayer.PlayerId);
                            }
                        }
                        for (int ia = 0; ia < __instance.playerStates.Length; ia++) //Loops through all players
                        {
                            PlayerVoteArea playerVoteArea = __instance.playerStates[ia];
                            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
                            if (playerVoteArea.VotedFor == swap1.PlayerId && !playerVoteArea.AmDead && !NiceList1.Contains(voteAreaPlayer.PlayerId))
                            {

                                //if (NiceList1.Contains(voteAreaPlayer.PlayerId)) continue;
                                //playerVoteArea.UnsetVote();
                                playerVoteArea.VotedFor = swap1.PlayerId;
                                //meetingHud.CastVote(voteAreaPlayer.PlayerId, swap1.PlayerId);
                                NiceList2.Add(voteAreaPlayer.PlayerId);
                            }
                        }
                        for (int ia = 0; ia < __instance.playerStates.Length; ia++) //Loops through all players
                        {
                            PlayerVoteArea playerVoteArea = __instance.playerStates[ia];
                            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
                            if (playerVoteArea.VotedFor == swap1.PlayerId && !playerVoteArea.AmDead && !NiceList2.Contains(voteAreaPlayer.PlayerId))
                            {

                                //playerVoteArea.UnsetVote();
                                //meetingHud.CastVote(voteAreaPlayer.PlayerId, swap2.PlayerId);
                                playerVoteArea.VotedFor = swap2.PlayerId;
                                NiceList1.Add(voteAreaPlayer.PlayerId);
                            }
                        }
                        if (Main.EvilSwapSend == false)
                        {
                            Utils.SendMessage(string.Format(GetString("SwapVote"), swap1.GetRealName(), swap2.GetRealName()), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceSwapper), GetString("SwapTitle")));
                            Main.EvilSwapSend = true;
                            NiceList1.Clear();
                        }
                        EvilSwapper.Vote.Clear();
                        EvilSwapper.VoteTwo.Clear();
                    }
                   
                }
                #endregion*/
                #endregion
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Mayor) && !Options.MayorHideVote.GetBool()) //Mayorの投票数
                {
                    for (var i2 = 0; i2 < Options.MayorAdditionalVote.GetFloat(); i2++)
                    {
                        statesList.Add(new MeetingHud.VoterState()
                        {
                            VoterId = ps.TargetPlayerId,
                            VotedForId = ps.VotedFor
                        });
                    }
                }
            }
            states = statesList.ToArray();
            var VotingData = __instance.CustomCalculateVotes();

            byte exileId = byte.MaxValue;
            int max = 0;
            voteLog.Info("===决定驱逐玩家处理开始===");
            foreach (var data in VotingData)
            {
                voteLog.Info($"{data.Key}({Utils.GetVoteName(data.Key)}):{data.Value}票");
                if (data.Value > max)
                {
                    voteLog.Info(data.Key + "拥有更高票数(" + data.Value + ")");
                    exileId = data.Key;
                    max = data.Value;
                    tie = false;
                }
                else if (data.Value == max)
                {
                    voteLog.Info(data.Key + "与" + exileId + "的票数相同(" + data.Value + ")");
                    exileId = byte.MaxValue;
                    tie = true;
                }
                voteLog.Info($"驱逐ID: {exileId}, 最大: {max}票");
            }

            voteLog.Info($"决定驱逐玩家: {exileId}({Utils.GetVoteName(exileId)})");

            bool braked = false;
            if (tie) //破平者判断
            {
                byte target = byte.MaxValue;
                foreach (var data in VotingData.Where(x => x.Key < 15 && x.Value == max))
                {
                    if (Main.BrakarVoteFor.Contains(data.Key))
                    {
                        if (target != byte.MaxValue)
                        {
                            target = byte.MaxValue;
                            break;
                        }
                        target = data.Key;
                    }
                }
                if (target != byte.MaxValue)
                {
                    Logger.Info("破平者覆盖驱逐玩家", "Brakar Vote");
                    exiledPlayer = Utils.GetPlayerInfoById(target);
                    tie = false;
                    braked = true;
                }
            }
            
            Collector.CollectAmount(VotingData, __instance);
            

            if (Options.VoteMode.GetBool() && Options.WhenTie.GetBool() && tie)
            {
                switch ((TieMode)Options.WhenTie.GetValue())
                {
                    case TieMode.Default:
                        exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == exileId);
                        break;
                    case TieMode.All:
                        var exileIds = VotingData.Where(x => x.Key < 15 && x.Value == max).Select(kvp => kvp.Key).ToArray();
                        foreach (var playerId in exileIds)
                            Utils.GetPlayerById(playerId).SetRealKiller(null);
                        TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Vote, exileIds);
                        exiledPlayer = null;
                        break;
                    case TieMode.Random:
                        exiledPlayer = GameData.Instance.AllPlayers.ToArray().OrderBy(_ => Guid.NewGuid()).FirstOrDefault(x => VotingData.TryGetValue(x.PlayerId, out int vote) && vote == max);
                        tie = false;
                        break;
                }
            }
            else if (!braked)
                exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);
            exiledPlayer?.Object.SetRealKiller(null);

            //RPC
            if (AntiBlackout.OverrideExiledPlayer)
            {
                __instance.RpcVotingComplete(states, null, true);
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiledPlayer;
            }
            else __instance.RpcVotingComplete(states, exiledPlayer, tie); //通常処理

            CheckForDeathOnExile(PlayerState.DeathReason.Vote, exileId);


            Main.LastVotedPlayerInfo = exiledPlayer;
            if (Main.LastVotedPlayerInfo != null)
                ConfirmEjections(Main.LastVotedPlayerInfo);

            return false;
        }
        catch (Exception ex)
        {
            Logger.SendInGame(string.Format(GetString("Error.MeetingException"), ex.Message), true);
            throw;
        }
    }

    // 参考：https://github.com/music-discussion/TownOfHost-TheOtherRoles
    private static void ConfirmEjections(GameData.PlayerInfo exiledPlayer)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (exiledPlayer == null) return;
        var exileId = exiledPlayer.PlayerId;
        if (exileId is < 0 or > 254) return;
        var realName = exiledPlayer.Object.GetRealName(isMeeting: true);
        Main.LastVotedPlayer = realName;

        var player = Utils.GetPlayerById(exiledPlayer.PlayerId);
        var role = GetString(exiledPlayer.GetCustomRole().ToString());
        var crole = exiledPlayer.GetCustomRole();
        var coloredRole = Utils.GetDisplayRoleName(exileId, true);
        var name = "";
        int impnum = 0;
        int neutralnum = 0;

        if (CustomRolesHelper.RoleExist(CustomRoles.Bard))
        {
            Main.BardCreations++;
            try { name = ModUpdater.Get("https://v.api.aa1.cn/api/api-wenan-wangyiyunreping/index.php?aa1=text"); }
            catch { name = GetString("ByBardGetFailed"); }
            name += "\n\t\t——" + GetString("ByBard");
            goto EndOfSession;
        }

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var pc_role = pc.GetCustomRole();
            if (pc_role.IsImpostor() && pc != exiledPlayer.Object)
                impnum++;
            else if (pc_role.IsNeutralKilling() && pc != exiledPlayer.Object)
                neutralnum++;
        }
        switch (Options.CEMode.GetInt())
        {
            case 0:
                name = string.Format(GetString("PlayerExiled"), realName);
                break;
            case 1:
                if (player.GetCustomRole().IsImpostor())
                    name = string.Format(GetString("BelongTo"), realName, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("TeamImpostor")));
                else if (player.GetCustomRole().IsCrewmate())
                    name = string.Format(GetString("IsGood"), realName);
                else if (player.GetCustomRole().IsNeutral())
                    name = string.Format(GetString("BelongTo"), realName, Utils.ColorString(new Color32(255, 171, 27, byte.MaxValue), GetString("TeamNeutral")));
                break;
            case 2:
                name = string.Format(GetString("PlayerIsRole"), realName, coloredRole);
                if (Options.ShowTeamNextToRoleNameOnEject.GetBool())
                {
                    name += " (";
                    if (player.GetCustomRole().IsImpostor() || player.Is(CustomRoles.Madmate))
                        name += Utils.ColorString(new Color32(255, 25, 25, byte.MaxValue), GetString("TeamImpostor"));
                    else if (player.GetCustomRole().IsNeutral() || player.Is(CustomRoles.Charmed) || player.Is(CustomRoles.Attendant))
                        name += Utils.ColorString(new Color32(255, 171, 27, byte.MaxValue), GetString("TeamNeutral"));
                    else if (player.GetCustomRole().IsCrewmate() || player.Is(CustomRoles.Undercover))
                        name += Utils.ColorString(new Color32(140, 255, 255, byte.MaxValue), GetString("TeamCrewmate"));
                    name += ")";
                }
                break;
        }

        var DecidedWinner = false;
        //小丑胜利
        if (crole == CustomRoles.Jester)
        {
            name = string.Format(GetString("ExiledJester"), realName, coloredRole);
            DecidedWinner = true;
        }
        //处刑人胜利
        if (Executioner.CheckExileTarget(exiledPlayer, DecidedWinner, true))
        {
            name = string.Format(GetString("ExiledExeTarget"), realName, coloredRole);
            DecidedWinner = true;
        }
        //冤罪师胜利
        if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == exileId))
        {
            if (!(!Options.InnocentCanWinByImp.GetBool() && crole.IsImpostor()))
            {
                if (DecidedWinner) name += string.Format(GetString("ExiledInnocentTargetAddBelow"));
                else name = string.Format(GetString("ExiledInnocentTargetInOneLine"), realName, coloredRole);
                DecidedWinner = true;
            }
        }
        //欺诈师文本
        if (crole == CustomRoles.Fraudster)
        {
            name = string.Format(GetString("ExiledFraudster"), realName, coloredRole);
        }

        //流浪人被驱逐
        if (crole == CustomRoles.Wanderers)
        {
            name = string.Format(GetString("ExiledWanderers"), realName, coloredRole);
        }

        //孟姜女被驱逐
        if (crole == CustomRoles.MengJiangGirl)
        {
            name = string.Format(GetString("ExiledMengJiangGirl"), realName, coloredRole);
            var Mg = IRandom.Instance;
            int mengjiang = Mg.Next(0, 15);
            PlayerControl mengjiangp = Utils.GetPlayerById(mengjiang);
            if (Options.MengJiangGirlWinnerPlayerer.GetInt() == 0)
            {
                if (mengjiangp.GetCustomRole().IsCrewmate())
                {
                    DecidedWinner = true;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MengJiangGirl);
                    Logger.Info($"孟姜女被击杀，抽取到船员，设置为船员", "MengJiang");
                }
            }
            else if (Options.MengJiangGirlWinnerPlayerer.GetInt() == 1)
            {
                if (mengjiangp.GetCustomRole().IsImpostor())
                {
                    DecidedWinner = true;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MengJiangGirl);
                    Logger.Info($"孟姜女被击杀，抽取到内鬼，设置为内鬼", "MengJiang");
                }
            }
            else if (Options.MengJiangGirlWinnerPlayerer.GetInt() == 2)
            {
                if (mengjiangp.GetCustomRole().IsNeutral())
                {
                    DecidedWinner = true;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MengJiangGirl);
                    Logger.Info($"孟姜女被击杀，抽取到中立，设置为中立", "MengJiang");                }
            }
            else
            {
                TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.cry, exileId);
            }
        }

        if (DecidedWinner) name += "<size=0>";
        if (Options.ShowImpRemainOnEject.GetBool() && !DecidedWinner)
        {
            name += "\n";
            string comma = neutralnum > 0 ? "，" : "";
            if (impnum == 0) name += GetString("NoImpRemain") + comma;
            else name += string.Format(GetString("ImpRemain"), impnum) + comma;
            if (Options.ShowNKRemainOnEject.GetBool() && neutralnum > 0)
                name += string.Format(GetString("NeutralRemain"), neutralnum);
        }

    EndOfSession:

        name += "<size=0>";
        new LateTask(() =>
        {
            Main.DoBlockNameChange = true;
            if (GameStates.IsInGame) player.RpcSetName(name);
        }, 3.0f, "Change Exiled Player Name");
        new LateTask(() =>
        {
            if (GameStates.IsInGame && !player.Data.Disconnected)
            {
                player.RpcSetName(realName);
                Main.DoBlockNameChange = false;
            }
        }, 11.5f, "Change Exiled Player Name Back");
    }
    public static bool CheckRole(byte id, CustomRoles role)
    {
        var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == id).FirstOrDefault();
        return player != null && player.Is(role);
    }
    public static void TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason deathReason, params byte[] playerIds)
    {
        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
                AddedIdList.Add(playerId);
        CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
    }
    public static void CheckForDeathOnExile(PlayerState.DeathReason deathReason, params byte[] playerIds)
    {
        Witch.OnCheckForEndVoting(deathReason, playerIds);
        
    }
    private static void RevengeOnExile(byte playerId, PlayerState.DeathReason deathReason)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        var target = PickRevengeTarget(player, deathReason);
        if (target == null) return;
        TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Revenge, target.PlayerId);
        target.SetRealKiller(player);
        Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "RevengeOnExile");
    }
    private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer, PlayerState.DeathReason deathReason)//道連れ先選定
    {
        List<PlayerControl> TargetList = new();
        foreach (var candidate in Main.AllAlivePlayerControls)
        {
            if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
        }
        if (TargetList == null || TargetList.Count == 0) return null;
        var rand = IRandom.Instance;
        var target = TargetList[rand.Next(TargetList.Count)];
        return target;
    }
}

static class ExtendedMeetingHud
{
    public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance)
    {
        Logger.Info("===计算票数处理开始===", "Vote");
        Dictionary<byte, int> dic = new();
        Main.BrakarVoteFor = new();
        Collector.CollectorVoteFor = new();
        //| 投票された人 | 投票された回数 |
        for (int i = 0; i < __instance.playerStates.Length; i++)
        {
            PlayerVoteArea ps = __instance.playerStates[i];//该玩家面板里的所有会议中的玩家
            if (ps == null) continue;
            if (ps.VotedFor is not 252 and not byte.MaxValue and not 254)//该玩家面板里是否投了该玩家
            {
                // 默认票数1票
                int VoteNum = 1;

                // 投票给有效玩家时才进行的判断
                var target = Utils.GetPlayerById(ps.VotedFor);
                if (target != null)
                {
                    // 僵尸、活死人以及自由人无法被票
                    if (target.Is(CustomRoles.Zombie) || target.Is(CustomRoles.Glitch) || target.Is(CustomRoles.FreeMan)) VoteNum = 0;
                    // 记录破平者投票
                    if (!Main.BrakarVoteFor.Contains(target.PlayerId))
                        Main.BrakarVoteFor.Add(target.PlayerId);
                    // 投给贱人的票变为114514
                    if (target.Is(CustomRoles.Bitch)) VoteNum = 114514;
                    //投给欺诈师的票
                        if (target.Is(CustomRoles.Fraudster))
                      { 
                        var Fd = IRandom.Instance;
                        if (Fd.Next(0, 100) < Options.FraudsterVoteLose.GetInt())
                        {
                            Logger.Info("=投票失败乐=", "Fraudster");
                            if (target.Is(CustomRoles.Fraudster)) VoteNum = 0;
                        } 
                      }
                            // 集票者记录数据
                            Collector.CollectorVotes(target, ps);
                }

                //市长附加票数
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Mayor)
                    && ps.TargetPlayerId != ps.VotedFor
                    ) VoteNum += Options.MayorAdditionalVote.GetInt();
                //欺诈师附加票数
                    if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Fraudster)
                    && Options.FraudsterCanMayor.GetBool() && ps.TargetPlayerId != ps.VotedFor
                    ) VoteNum += Options.MayorAdditionalVote.GetInt();
                //狈附加票数
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Whoops)
                     && ps.TargetPlayerId != ps.VotedFor
                     ) VoteNum += 1;
                //干部附加票数
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Solicited)
                     && ps.TargetPlayerId != ps.VotedFor
                     ) VoteNum += 1;
                //自由人无法投票
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.FreeMan)
                    && ps.TargetPlayerId != ps.VotedFor
                    ) VoteNum = 0;
                //窃票者附加票数
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.TicketsStealer))
                    VoteNum += (int)(Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == ps.TargetPlayerId) * Options.TicketsPerKill.GetFloat());
                //反转侠投票
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.EIReverso)
                    && ps.TargetPlayerId != ps.VotedFor)
                {
                    RPC.PlaySoundRPC(ps.TargetPlayerId, Sounds.KillSound);
                    var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != ps.TargetPlayerId).ToList();
                    var ER = pcList[IRandom.Instance.Next(0, pcList.Count)];
                    ps.VotedFor = ER.PlayerId;
                }
                    // 命运眷顾票数
                    if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Fategiver)
                    && ps.TargetPlayerId != ps.VotedFor)
                {
                    var Fg = IRandom.Instance;
                    int vote = Fg.Next(1, 10);
                    if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Mayor))
                    {
                        if (vote == 1)
                        {
                            Logger.Info("=眷顾0票=", "Fategiver");
                            VoteNum += 0;
                            Utils.SendMessage("[WARRING]神似乎睡了", ps.TargetPlayerId);
                        }
                        if (vote == 2 || vote == 3)
                        {
                            Logger.Info("=眷顾1票=", "Fategiver");
                            VoteNum += 1;
                            Utils.SendMessage("[ERROR]神 未响应", ps.TargetPlayerId);
                        }
                        else
                        {
                            Logger.Info("=眷顾2票=", "Fategiver");
                            VoteNum += 2;
                            Utils.SendMessage("神回复了你的响应", ps.TargetPlayerId);
                        }
                    }
                    else { 
                        if (vote == 1)
                        {
                           Logger.Info("=眷顾0票=", "Fategiver");
                            VoteNum = 0;
                            Utils.SendMessage("[WARRING]神似乎睡了", ps.TargetPlayerId);
                        }
                        if (vote == 2 || vote == 3)
                        {
                            Logger.Info("=眷顾1票=", "Fategiver");
                            VoteNum = 1;
                            Utils.SendMessage("[ERROR]神 未响应", ps.TargetPlayerId);
                        }
                        else
                        {
                            Logger.Info("=眷顾2票=", "Fategiver");
                            VoteNum = 2;
                            Utils.SendMessage("神回复了你的响应", ps.TargetPlayerId);
                        }
                    }
                }
                    // 主动叛变模式下自票无效
                    if (ps.TargetPlayerId == ps.VotedFor && Options.MadmateSpawnMode.GetInt() == 2) VoteNum = 0;

                //投票を1追加 キーが定義されていない場合は1で上書きして定義
                dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out int num) ? VoteNum : num + VoteNum;//统计该玩家被投的数量
            }
        }
        return dic;
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
class MeetingHudStartPatch
{
    public static void NotifyRoleSkillOnMeetingStart()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        List<(string, byte, string)> msgToSend = new();

        void AddMsg(string text, byte sendTo = 255, string title = "")
            => msgToSend.Add((text, sendTo, title));

        //首次会议技能提示
        if (Options.SendRoleDescriptionFirstMeeting.GetBool() && MeetingStates.FirstMeeting)
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => !x.IsModClient()))
            {
                var role = pc.GetCustomRole();
                var sb = new StringBuilder();
                sb.Append(GetString(role.ToString()) + Utils.GetRoleMode(role) + pc.GetRoleInfo(true));
                if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                    Utils.ShowChildrenSettings(opt, ref sb, command: true);
                var txt = sb.ToString();
                sb.Clear().Append(txt.RemoveHtmlTags());
                foreach (var subRole in Main.PlayerStates[pc.PlayerId].SubRoles)
                    sb.Append($"\n\n" + GetString($"{subRole}") + Utils.GetRoleMode(subRole) + GetString($"{subRole}InfoLong"));
                if (CustomRolesHelper.RoleExist(CustomRoles.Ntr) && (role is not CustomRoles.GM and not CustomRoles.Ntr))
                    sb.Append($"\n\n" + GetString($"Lovers") + Utils.GetRoleMode(CustomRoles.Lovers) + GetString($"LoversInfoLong"));
                AddMsg(sb.ToString(), pc.PlayerId);
            }
        if (msgToSend.Count >= 1)
        {
            var msgTemp = msgToSend.ToList();
            new LateTask(() => { msgTemp.Do(x => Utils.SendMessage(x.Item1, x.Item2, x.Item3)); }, 3f, "Skill Description First Meeting");
        }
        msgToSend = new();

        //主动叛变模式提示
        if (Options.MadmateSpawnMode.GetInt() == 2 && CustomRoles.Madmate.GetCount() > 0)
            AddMsg(string.Format(GetString("Message.MadmateSelfVoteModeNotify"), GetString("MadmateSpawnMode.SelfVote")));
        //提示神存活
        if (CustomRoles.God.RoleExist() && Options.NotifyGodAlive.GetBool())
            AddMsg(GetString("GodNoticeAlive"), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.God), GetString("GodAliveTitle")));
        string MimicMsg = "";
        foreach (var pc in Main.AllPlayerControls)
        {            
            //黑手党死后技能提示
            if (pc.Is(CustomRoles.Mafia) && !pc.IsAlive())
                AddMsg(GetString("MafiaDeadMsg"), pc.PlayerId);
            //网红死亡消息提示
            foreach (var csId in Main.CyberStarDead)
            {
                if (!Options.ImpKnowCyberStarDead.GetBool() && pc.GetCustomRole().IsImpostor()) continue;
                if (!Options.NeutralKnowCyberStarDead.GetBool() && pc.GetCustomRole().IsNeutral()) continue;
                AddMsg(string.Format(GetString("CyberStarDead"), Main.AllPlayerNames[csId]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.CyberStar), GetString("CyberStarNewsTitle")));
            }

            foreach (var player in Blackmailer.ForBlackmailer)
            {
                
                 AddMsg(string.Format(GetString("BlackmailerDead"), Main.AllPlayerNames[player]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Blackmailer), GetString("BlackmaileKillTitle")));
            }
            //舰长死亡消息提示
            foreach (var csId in Main.CaptainDead)
            {
                if (pc.GetCustomRole().IsImpostor()) continue;
                if (pc.GetCustomRole().IsNeutral()) continue;
                AddMsg(string.Format(GetString("CaptainDead"), Main.AllPlayerNames[csId]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Captain), GetString("CaptainNewsTitle")));
            }
            //操控者信息
            if (Main.ManipulatorNotify.ContainsKey(pc.PlayerId))
                AddMsg(Main.ManipulatorNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Manipulator), GetString("ManipulatorNoticeTitle")));
            //侦探报告线索
            if (Main.DetectiveNotify.ContainsKey(pc.PlayerId))
                AddMsg(Main.DetectiveNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Detective), GetString("DetectiveNoticeTitle")));
            //宝箱怪的消息（记录）
            if (pc.Is(CustomRoles.Mimic) && !pc.IsAlive())
                Main.AllAlivePlayerControls.Where(x => x.GetRealKiller()?.PlayerId == pc.PlayerId).Do(x => MimicMsg += $"\n{x.GetNameWithRole(true)}");
            //入殓师的检查
            if (Mortician.msgToSend.ContainsKey(pc.PlayerId))
                AddMsg(Mortician.msgToSend[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mortician), GetString("MorticianCheckTitle")));
            //通灵师的提示（自己）
            if (Mediumshiper.ContactPlayer.ContainsValue(pc.PlayerId))
                AddMsg(string.Format(GetString("MediumshipNotifySelf"), Main.AllPlayerNames[Mediumshiper.ContactPlayer.Where(x => x.Value == pc.PlayerId).FirstOrDefault().Key], Mediumshiper.ContactLimit[pc.PlayerId]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));
            //通灵师的提示（目标）
            if (Mediumshiper.ContactPlayer.ContainsKey(pc.PlayerId) && (!Mediumshiper.OnlyReceiveMsgFromCrew.GetBool() || pc.GetCustomRole().IsCrewmate()))
                AddMsg(string.Format(GetString("MediumshipNotifyTarget"), Main.AllPlayerNames[Mediumshiper.ContactPlayer[pc.PlayerId]]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));
        }
        //宝箱怪的消息（合并）
        if (MimicMsg != "")
        {
            MimicMsg = GetString("MimicDeadMsg") + "\n" + MimicMsg;
            foreach (var ipc in Main.AllPlayerControls.Where(x => x.GetCustomRole().IsImpostorTeam()))
                AddMsg(MimicMsg, ipc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mimic), GetString("MimicMsgTitle")));
        }

        msgToSend.Do(x => Logger.Info($"To:{x.Item2} {x.Item3} => {x.Item1}", "Skill Notice OnMeeting Start"));

        //总体延迟发送
        new LateTask(() => { msgToSend.Do(x => Utils.SendMessage(x.Item1, x.Item2, x.Item3)); }, 3f, "Skill Notice OnMeeting Start");

        Main.CyberStarDead.Clear();
        Main.CaptainDead.Clear();
        Main.DetectiveNotify.Clear();
        Main.ManipulatorNotify.Clear();
        Mortician.msgToSend.Clear();
    }
    public static void Prefix(MeetingHud __instance)
    {
        Logger.Info("------------会议开始------------", "Phase");
        ChatUpdatePatch.DoBlockChat = true;
        GameStates.AlreadyDied |= !Utils.IsAllAlive;
        Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
        MeetingStates.MeetingCalled = true;
        foreach (var player in Main.AllPlayerControls)
        {
            //被选择了命运
            if (Main.ForDestinyChooser.Contains(player.PlayerId))
            {
                Utils.TP(player.NetTransform, Pelican.GetBlackRoomPS());
                player.RpcMurderPlayerV3(player);
                NameNotifyManager.Notify(player, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledByMY")));
            }
            //操控灵魂击杀凶手
            if (Main.ForSpiritualizerCrewmate.Contains(player.PlayerId))
            {
                foreach (var CrewKiller in Main.AllPlayerControls)
                {
                    if (Main.ForSpiritualizerImpostor.Contains(CrewKiller.PlayerId))
                    {
                        player.RpcMurderPlayerV3(CrewKiller);
                        Main.ForSpiritualizerImpostor.Remove(CrewKiller.PlayerId);
                        Main.ForSpiritualizerCrewmate.Remove(player.PlayerId);
                    }
                }               
            }
            if (Copycat.CopycatFor.Contains(player.PlayerId))
            {
                player.RpcSetCustomRole(CustomRoles.Copycat);
                Copycat.CopycatFor.Remove(player.PlayerId);
            }
            //如果是护士正在救治病人
            if (Main.NnurseHelep.Contains(player.PlayerId) && player.IsAlive())
            {
                Main.NnurseHelep.Remove(player.PlayerId);
                player.RpcMurderPlayerV3(player);
            }

            //操控者
            if (player.Is(CustomRoles.Manipulator))
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (!pc.IsAlive()) continue;
                    if (pc.GetCustomRole().IsImpostor())
                    {
                        var Im = IRandom.Instance;
                        int Ma = Im.Next(0, 100);
                        if (Ma < Options.DepressedIdioctoniaProbability.GetInt())
                        {
                            Main.ManipulatorNeutral[player.PlayerId] += Im.Next(0, 10);
                        }
                        else
                        {
                            Main.ManipulatorImpotors[player.PlayerId]++;
                        }
                    }
                    if (pc.GetCustomRole().IsCrewmate())
                    {
                        var Cr = IRandom.Instance;
                        int Mn = Cr.Next(0, 100);
                        if (Mn < Options.DepressedIdioctoniaProbability.GetInt())
                        {
                            Main.ManipulatorNeutral[player.PlayerId] += Cr.Next(0, 10);
                        }
                        else
                        {
                            Main.ManipulatorCrewmate[player.PlayerId]++;
                        }
                    }
                    if (pc.GetCustomRole().IsNeutral())
                    {
                        var Ne = IRandom.Instance;
                        int Mi = Ne.Next(0, 100);
                        if (Mi < Options.DepressedIdioctoniaProbability.GetInt())
                        {
                            Main.ManipulatorNeutral[player.PlayerId] += Ne.Next(0, 10);
                        }
                        else
                        {
                            Main.ManipulatorNeutral[player.PlayerId]++;
                        }
                    }
                }
                string msg;
                msg = string.Format(GetString("ManipulatorNoticeVictim"), Main.ManipulatorImpotors[player.PlayerId], Main.ManipulatorCrewmate[player.PlayerId], Main.ManipulatorNeutral[player.PlayerId]);
                Main.ManipulatorNotify.Add(player.PlayerId, msg);
                new LateTask(() =>
                {
                    Main.ManipulatorImpotors[player.PlayerId] = 0;
                    Main.ManipulatorNeutral[player.PlayerId] = 0;
                    Main.ManipulatorCrewmate[player.PlayerId] = 0;
                    Utils.NotifyRoles();
                }, 15f, ("清空"));
            }
        }
    }
    public static void Postfix(MeetingHud __instance)
    {
        SoundManager.Instance.ChangeAmbienceVolume(0f);
        if (!GameStates.IsModHost) return;

        //提前储存赌怪游戏组件的模板
        GuessManager.textTemplate = UnityEngine.Object.Instantiate(__instance.playerStates[0].NameText);
        GuessManager.textTemplate.enabled = false;

        foreach (var pva in __instance.playerStates)
        {
            var pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null) continue;
            var RoleTextData = Utils.GetRoleText(PlayerControl.LocalPlayer.PlayerId, pc.PlayerId);
            var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
            roleTextMeeting.transform.SetParent(pva.NameText.transform);
            roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            roleTextMeeting.fontSize = 1.5f;
            roleTextMeeting.text = RoleTextData.Item1;
            if (Main.VisibleTasksCount) roleTextMeeting.text += Utils.GetProgressText(pc);
            roleTextMeeting.color = RoleTextData.Item2;
            roleTextMeeting.gameObject.name = "RoleTextMeeting";
            roleTextMeeting.enableWordWrapping = false;
            roleTextMeeting.enabled =
                pc.AmOwner || //対象がLocalPlayer
                (Main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) || //LocalPlayerが死亡していて幽霊が他人の役職を見れるとき
                (pc.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Lovers) && Options.LoverKnowRoles.GetBool()) ||
                (pc.Is(CustomRoles.CrushLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.CrushLovers) && Options.CrushLoverKnowRoles.GetBool()) ||
                (pc.Is(CustomRoles.CupidLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.CupidLovers) && Options.CupidLoverKnowRoles.GetBool()) ||
                (pc.Is(CustomRoles.CupidLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Cupid) && Options.CanKnowCupid.GetBool()) ||
                (pc.Is(CustomRoles.Akujo) && (PlayerControl.LocalPlayer.Is(CustomRoles.Honmei) || PlayerControl.LocalPlayer.Is(CustomRoles.Backup)) && Options.AkujoCanKnowRole.GetBool()) ||
                ((pc.Is(CustomRoles.Honmei) || pc.Is(CustomRoles.Backup))&& PlayerControl.LocalPlayer.Is(CustomRoles.Akujo) ) ||
                (pc.Is(CustomRoleTypes.Impostor) && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) && Options.ImpKnowAlliesRole.GetBool()) ||
                (pc.Is(CustomRoleTypes.Impostor) && PlayerControl.LocalPlayer.Is(CustomRoles.Madmate) && Options.MadmateKnowWhosImp.GetBool()) ||
                (pc.Is(CustomRoles.Madmate) && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) && Options.ImpKnowWhosMadmate.GetBool()) ||
                (pc.Is(CustomRoles.Madmate) && PlayerControl.LocalPlayer.Is(CustomRoles.Madmate) && Options.MadmateKnowWhosMadmate.GetBool()) ||
                (Totocalcio.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                (Succubus.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                (Captain.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                (Jackal.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                (Corpse.KnowRole(PlayerControl.LocalPlayer, pc)) || 
            //喵喵队
            //内鬼
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isimp == true && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoleTypes.Impostor) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isimp == true && Options.CanKnowKiller.GetBool()) ||
            //豺狼
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isjac == true&& PlayerControl.LocalPlayer.Is(CustomRoles.Jackal) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.Jackal) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isjac == true && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isjac == true && PlayerControl.LocalPlayer.Is(CustomRoles.Sidekick) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.Sidekick) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isjac == true && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isjac == true && PlayerControl.LocalPlayer.Is(CustomRoles.Whoops) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.Whoops) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isjac == true && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isjac == true && PlayerControl.LocalPlayer.Is(CustomRoles.Attendant) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.Attendant) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isjac == true && Options.CanKnowKiller.GetBool()) ||
            //西风骑士团(bushi)
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isbk == true && PlayerControl.LocalPlayer.Is(CustomRoles.BloodKnight) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.BloodKnight) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isbk == true && Options.CanKnowKiller.GetBool()) ||
            //疫情的源头
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.ispg == true && PlayerControl.LocalPlayer.Is(CustomRoles.PlaguesGod) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.PlaguesGod) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.ispg == true && Options.CanKnowKiller.GetBool()) ||
            //玩家
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isgam == true && PlayerControl.LocalPlayer.Is(CustomRoles.Gamer) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.Gamer) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isgam == true && Options.CanKnowKiller.GetBool()) ||
            //穹P黑客(BUSHI)
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isyl == true && PlayerControl.LocalPlayer.Is(CustomRoles.YinLang) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.YinLang) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isyl == true && Options.CanKnowKiller.GetBool()) ||
            //黑，真tm黑
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isdh == true && PlayerControl.LocalPlayer.Is(CustomRoles.DarkHide) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.DarkHide) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isdh == true && Options.CanKnowKiller.GetBool()) ||
            //雇佣
                    (pc.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isok == true && PlayerControl.LocalPlayer.Is(CustomRoles.OpportunistKiller) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.OpportunistKiller) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) &&SchrodingerCat.isok == true && Options.CanKnowKiller.GetBool()) ||
                    //孤独
                    (pc.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isln == true && PlayerControl.LocalPlayer.Is(CustomRoles.Loners) && Options.CanKnowKiller.GetBool()) ||
                    (pc.Is(CustomRoles.Loners) && PlayerControl.LocalPlayer.Is(CustomRoles.SchrodingerCat) && SchrodingerCat.isln == true && Options.CanKnowKiller.GetBool()) ||

                PlayerControl.LocalPlayer.Is(CustomRoles.God) ||
                PlayerControl.LocalPlayer.Is(CustomRoles.GM) ||
                Main.GodMode.Value;
            if (EvilTracker.IsTrackTarget(PlayerControl.LocalPlayer, pc) && EvilTracker.CanSeeLastRoomInMeeting)
            {
                roleTextMeeting.text = EvilTracker.GetArrowAndLastRoom(PlayerControl.LocalPlayer, pc);
                roleTextMeeting.enabled = true;
            }
            if (NiceTracker.IsPlayer(PlayerControl.LocalPlayer, pc) && NiceTracker.CanSeeLastRoomInMeeting)
            {
                roleTextMeeting.text = NiceTracker.GetArrowAndLastRoom(PlayerControl.LocalPlayer, pc);
                roleTextMeeting.enabled = true;
            }
        }

        if (Options.SyncButtonMode.GetBool())
        {
            Utils.SendMessage(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
            Logger.Info("紧急会议剩余 " + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + " 次使用次数", "SyncButtonMode");
        }
        if (AntiBlackout.OverrideExiledPlayer)
        {
            new LateTask(() =>
            {
                Utils.SendMessage(GetString("Warning.OverrideExiledPlayer"), 255, Utils.ColorString(Color.red, GetString("DefaultSystemMessageTitle")));
            }, 5f, "Warning OverrideExiledPlayer");
        }
        if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
        TemplateManager.SendTemplate("OnMeeting", noErr: true);

        if (AmongUsClient.Instance.AmHost)
            NotifyRoleSkillOnMeetingStart();

        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                }
                ChatUpdatePatch.DoBlockChat = false;
            }, 3f, "SetName To Chat");
        }

        foreach (var pva in __instance.playerStates)
        {
            if (pva == null) continue;
            PlayerControl seer = PlayerControl.LocalPlayer;
            PlayerControl target = Utils.GetPlayerById(pva.TargetPlayerId);
            if (target == null) continue;

            var sb = new StringBuilder();

            //会議画面での名前変更
            //自分自身の名前の色を変更
            //NameColorManager準拠の処理
            pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

            //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

            if (seer.KnowDeathReason(target))
                sb.Append($"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})");

            //インポスター表示
            switch (seer.GetCustomRole().GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    if (target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate) && target.GetPlayerTaskState().IsTaskFinished)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "★")); //変更対象にSnitchマークをつける
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;
            }
            switch (seer.GetCustomRole())
            {
                case CustomRoles.Arsonist:
                    if (seer.IsDousedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), "▲"));
                    break;
                case CustomRoles.Executioner:
                    sb.Append(Executioner.TargetMark(seer, target));
                    break;
                case CustomRoles.Lawyer:
                    break;
                case CustomRoles.Yandere:
                    break;
                case CustomRoles.Jackal:
                case CustomRoles.Pelican:
                case CustomRoles.DarkHide:
                case CustomRoles.BloodKnight:
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;
                case CustomRoles.EvilTracker:
                    sb.Append(EvilTracker.GetTargetMark(seer, target));
                    break;
        //        case CustomRoles.NiceTracker:
        //            sb.Append(NiceTracker.GetTargetMark(seer, target));
          //          break;
                case CustomRoles.Revolutionist:
                    if (seer.IsDrawPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Revolutionist), "●"));
                    break;
                case CustomRoles.Psychic:
                    if (target.IsRedForPsy(seer) && !seer.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), pva.NameText.text);
                    break;
                case CustomRoles.Mafia:
                    if (seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.NiceGuesser:
                case CustomRoles.EvilGuesser:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.Judge:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), target.PlayerId.ToString()) + " " + pva.NameText.text;

                    break;
                case CustomRoles.Copycat:
                    if (!seer.Data.IsDead && target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Copycat), target.PlayerId.ToString()) + " " + pva.NameText.text;

                    break;
                case CustomRoles.Strikers :
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), target.PlayerId.ToString()) + " " + pva.NameText.text;

                    break;
                case CustomRoles.NiceSwapper:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceSwapper), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;

                case CustomRoles.EvilSwapper:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.EvilSwapper), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.Gamer:
                    sb.Append(Gamer.TargetMark(seer, target));
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;
                case CustomRoles.PlagueDoctor:
                    sb.Append(PlagueDoctor.TargetMark(seer, target));
                    break;
            }

            bool isLover = false;
            foreach (var subRole in target.GetCustomSubRoles())
            {
                switch (subRole)
                {
                    case CustomRoles.Lovers:
                        if (seer.Is(CustomRoles.Lovers) || seer.Data.IsDead)
                        {
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));
                            isLover = true;
                        }
                        break;
                }
            }
            bool isCrushLover = false;
            foreach (var subRole in target.GetCustomSubRoles())
            {
                switch (subRole)
                {
                    case CustomRoles.CrushLovers:
                        if (seer.Is(CustomRoles.CrushLovers) || seer.Data.IsDead)
                        {
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CrushLovers), "♡"));
                            isCrushLover = true;
                        }
                        break;
                }
            }
            bool isCupidLover = false;
            foreach (var subRole in target.GetCustomSubRoles())
            {
                switch (subRole)
                {
                    case CustomRoles.CupidLovers:
                        if (seer.Is(CustomRoles.CupidLovers) || seer.Data.IsDead)
                        {
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CupidLovers), "♡"));
                            isCupidLover = true;
                        }
                        break;
                }
            }
           

            //海王相关显示
            if ((seer.Is(CustomRoles.Ntr) || target.Is(CustomRoles.Ntr)) && !seer.Data.IsDead && !isLover && !isCrushLover && !isCupidLover && !seer.Is(CustomRoles.Backup) && !seer.Is(CustomRoles.Honmei) && !seer.Is(CustomRoles.Akujo))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));
            else if (seer == target && CustomRolesHelper.RoleExist(CustomRoles.Ntr) && !isLover && !isCrushLover && !isCupidLover && !seer.Is(CustomRoles.Backup) && !seer.Is(CustomRoles.Honmei) && !seer.Is(CustomRoles.Akujo))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));

            if ((seer.Is(CustomRoles.Backup) || seer.Is(CustomRoles.Honmei)) && target.Is(CustomRoles.Akujo))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Akujo), "v"));
            if (seer.Is(CustomRoles.Akujo) && target.Is(CustomRoles.Honmei))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Akujo), "♥"));
            if (seer.Is(CustomRoles.Akujo) && target.Is(CustomRoles.Backup))
                sb.Append(Utils.ColorString(Color.grey, "♥"));

            //呪われている場合
            sb.Append(Witch.GetSpelledMark(target.PlayerId, true));

            //如果是大明星
            if (target.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.SuperStar), "★"));
            if (Blackmailer.ForBlackmailer.Contains(target.PlayerId))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.SuperStar), "╳")); 
            //迷你船员
            if (target.Is(CustomRoles.NiceMini) && Mini.EveryoneCanKnowMini.GetBool())
                sb.Append(Utils.ColorString(Color.yellow, Mini.Age != 18 ? $"({Mini.Age})" : ""));
            //迷你船员
            if (target.Is(CustomRoles.EvilMini) && Mini.EveryoneCanKnowMini.GetBool())
                sb.Append(Utils.ColorString(Color.yellow, Mini.Age != 18 ? $"({Mini.Age})" : ""));
            //if QL
            if (target.Is(CustomRoles.QL) && Options.EveryOneKnowQL.GetBool())
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.QL), "♛"));
            //如果是舰长
            if (target.Is(CustomRoles.Captain))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Captain), " ★Cap★ "));

            //HP
            if (target.Is(CustomRoles.Hotpotato))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Hotpotato), "●"));

            //球状闪电提示
            if (BallLightning.IsGhost(target))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.BallLightning), "■"));

            //医生护盾提示
            if (seer.PlayerId == target.PlayerId && (Medic.InProtect(seer.PlayerId) || Medic.TempMarkProtected == seer.PlayerId) && (Medic.WhoCanSeeProtect.GetInt() == 0 || Medic.WhoCanSeeProtect.GetInt() == 2))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medic), " ●"));

            if (seer.Is(CustomRoles.Medic) && (Medic.InProtect(target.PlayerId) || Medic.TempMarkProtected == target.PlayerId) && (Medic.WhoCanSeeProtect.GetInt() == 0 || Medic.WhoCanSeeProtect.GetInt() == 1))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medic), " ●"));

            if (seer.Data.IsDead && Medic.InProtect(target.PlayerId) && !seer.Is(CustomRoles.Medic))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medic), " ●"));

            //赌徒提示
            sb.Append(Totocalcio.TargetMark(seer, target));

            //律师提示
            sb.Append(Lawyer.TargetMark(seer, target));

            sb.Append(Yandere.TargetMark(seer, target));

            //会議画面ではインポスター自身の名前にSnitchマークはつけません。

            pva.NameText.text += sb.ToString();
            pva.ColorBlindName.transform.localPosition -= new Vector3(1.35f, 0f, 0f);
        }
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
class MeetingHudUpdatePatch
{
    private static int bufferTime = 10;
    public static void ClearShootButton(MeetingHud __instance, bool forceAll = false)
     => __instance.playerStates.ToList().ForEach(x => { if ((forceAll || (!Main.PlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead)) && x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });

    public static void Postfix(MeetingHud __instance)
    {

        if (AmongUsClient.Instance.AmHost && Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
        {
            __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
            {
                var player = Utils.GetPlayerById(x.TargetPlayerId);
                if (player != null && !player.Data.IsDead)
                {
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Execution;
                    player.RpcExileV2();
                    Main.PlayerStates[player.PlayerId].SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();
                }
            });
        }

        //投票结束时销毁全部技能按钮
        if (!GameStates.IsVoting && __instance.lastSecond < 1)
        {
            if (GameObject.Find("ShootButton") != null) ClearShootButton(__instance, true);
            return;
        }

        //会议技能UI处理
        bufferTime--;
        if (bufferTime < 0 && __instance.discussionTimer > 0)
        {
            bufferTime = 10;
            var myRole = PlayerControl.LocalPlayer.GetCustomRole();

            //若某玩家死亡则修复会议该玩家状态
            __instance.playerStates.Where(x => (!Main.PlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead) && !x.AmDead).Do(x => x.SetDead(x.DidReport, true));

            //若玩家死亡则销毁技能按钮
            if (myRole is CustomRoles.NiceGuesser or CustomRoles.EvilGuesser or CustomRoles.Judge or CustomRoles.NiceSwapper or CustomRoles.EvilSwapper && !PlayerControl.LocalPlayer.IsAlive())
                ClearShootButton(__instance, true);


            //若黑手党死亡则创建技能按钮
            if (myRole is CustomRoles.Mafia && !PlayerControl.LocalPlayer.IsAlive() && GameObject.Find("ShootButton") == null)
                MafiaRevengeManager.CreateJudgeButton(__instance);
            
           
            //销毁死亡玩家身上的技能按钮
            ClearShootButton(__instance);

        }
    }
}
[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
class SetHighlightedPatch
{
    public static bool Prefix(PlayerVoteArea __instance, bool value)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!__instance.HighlightedFX) return false;
        __instance.HighlightedFX.enabled = value;
        return false;
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
class MeetingHudOnDestroyPatch
{
    public static void Postfix()
    {
        MeetingStates.FirstMeeting = false;
        Logger.Info("------------会议结束------------", "Phase");
        if (AmongUsClient.Instance.AmHost)
        {
            AntiBlackout.SetIsDead();
            Main.AllPlayerControls.Do(pc => RandomSpawn.CustomNetworkTransformPatch.NumOfTP[pc.PlayerId] = 0);
            //GetRoleByInputName();
            //Main.signallerLocation.Do(pc => RandomSpawn.CustomNetworkTransformPatch.NumOfTP[pc.PlayerId] = 0);
            //CustomRoles.signaller.GetCount();
            //var position = Main.signallerLocation[CustomRoles.signaller.GetCount()];
            //Main.signallerLocation.Remove(CustomRoles.signaller.GetCount());
            //Logger.Msg($"{CustomRoles.signaller.GetNameWithRole()}:{position}", "EscapeeTeleport");
            //Utils.TP(CustomRoles.signaller.NetTransform, position);
            //CustomRoles.signaller.RPCPlayCustomSound("Teleport");
            //用于处理会议过后的特殊技能（赝品等不算）{别问我为什么在这里写，因为我觉得那些巴拉巴拉的一大堆麻烦，又不知道在哪写，只好在这加一个了}（就是说，不要在这个地方加太多东西，不然房主会卡）
            foreach (var player in Main.AllPlayerControls)
            {
                //鼻祖技能
                if (player.Is(CustomRoles.Originator))
                {
                    var Oi = IRandom.Instance;
                    int rndNum = Oi.Next(0, 100);
                    if (rndNum >= 10 && rndNum < 20)
                    {
                        player.RpcSetCustomRole(CustomRoles.Diseased);
                    }
                    if (rndNum >= 20 && rndNum < 30)
                    {
                        player.RpcSetCustomRole(CustomRoles.DeathGhost);
                    }
                    if (rndNum >= 30 && rndNum < 40)
                    {
                        player.RpcSetCustomRole(CustomRoles.involution);
                    }
                    if (rndNum >= 40 && rndNum < 50)
                    {
                        player.RpcSetCustomRole(CustomRoles.Bait);                       
                    }
                    if (rndNum >= 50 && rndNum < 60)
                    {
                        player.RpcSetCustomRole(CustomRoles.Flashman);
                    }
                    if (rndNum >= 60 && rndNum < 70)
                    {
                        player.RpcSetCustomRole(CustomRoles.Lighter);
                    }
                    if (rndNum >= 70 && rndNum < 80)
                    {
                        player.RpcSetCustomRole(CustomRoles.Seer);
                    }
                    if (rndNum >= 80 && rndNum < 90)
                    {
                        player.RpcSetCustomRole(CustomRoles.Avanger);
                    }
                    if (rndNum >= 90 && rndNum < 100)
                    {
                        player.RpcSetCustomRole(CustomRoles.Trapper);
                    }
                }
                //双刀手技能处理
                if (player.Is(CustomRoles.DoubleKiller) && player.IsAlive())
                {
                    Main.DoubleKillerKillSeacond.Remove(player.PlayerId);
                    Main.DoubleKillerKillSeacond.Add(player.PlayerId, Utils.GetTimeStamp());
                    Main.DoubleKillerMax.Remove(player.PlayerId);
                }
                if (Blackmailer.ForBlackmailer.Contains(player.PlayerId))
                {
                    Blackmailer.ForBlackmailer.Remove(player.PlayerId);
                }
                if (Copycat.ForCopycat.Contains(player.PlayerId))
                {
                    foreach (var pz in Main.AllAlivePlayerControls)
                    {
                        if (pz.Is(CustomRoles.Copycat))
                        {
                            pz.RpcSetCustomRole(player.GetCustomRole());
                            foreach (var pt in Copycat.CopycatFor)
                            {
                                var ps = Utils.GetPlayerById(pt);
                                if (ps.Data.Role.Role == RoleTypes.Shapeshifter) Main.CheckShapeshift.Add(ps.PlayerId, false);
                                switch (ps.GetCustomRole())
                                {
                                    case CustomRoles.BountyHunter:
                                        BountyHunter.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.SerialKiller:
                                        SerialKiller.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Witch:
                                        Witch.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Warlock:
                                        Main.CursedPlayers.Add(ps.PlayerId, null);
                                        Main.isCurseAndKill.Add(ps.PlayerId, false);
                                        break;
                                    case CustomRoles.FireWorks:
                                        FireWorks.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.TimeThief:
                                        TimeThief.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Sniper:
                                        Sniper.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Mare:
                                        Mare.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Vampire:
                                        Vampire.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.SwordsMan:
                                        SwordsMan.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Arsonist:
                                        foreach (var ar in Main.AllPlayerControls)
                                            Main.isDoused.Add((ps.PlayerId, ar.PlayerId), false);
                                        break;
                                    case CustomRoles.Revolutionist:
                                        foreach (var ar in Main.AllPlayerControls)
                                            Main.isDraw.Add((ps.PlayerId, ar.PlayerId), false);
                                        break;
                                    case CustomRoles.Executioner:
                                        Executioner.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Jackal:
                                        Jackal.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Sheriff:
                                        Sheriff.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.QuickShooter:
                                        QuickShooter.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Mayor:
                                        Main.MayorUsedButtonCount[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Paranoia:
                                        Main.ParaUsedButtonCount[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.SabotageMaster:
                                        SabotageMaster.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.EvilTracker:
                                        EvilTracker.Add(ps.PlayerId);
                                        break;
                                    //      case CustomRoles.NiceTracker:
                                    //NiceTracker.Add(pc.PlayerId);
                                    //         break;
                                    case CustomRoles.Snitch:
                                        Snitch.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.AntiAdminer:
                                        AntiAdminer.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Mario:
                                        Main.MarioVentCount[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.TimeManager:
                                        TimeManager.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Pelican:
                                        Pelican.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Counterfeiter:
                                        Counterfeiter.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Gangster:
                                        Gangster.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Medic:
                                        Medic.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.SchrodingerCat:
                                        SchrodingerCat.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Divinator:
                                        Divinator.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Gamer:
                                        Gamer.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.BallLightning:
                                        BallLightning.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.DarkHide:
                                        DarkHide.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Greedier:
                                        Greedier.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Collector:
                                        Collector.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.CursedWolf:
                                        Main.CursedWolfSpellCount[ps.PlayerId] = Options.GuardSpellTimes.GetInt();
                                        break;
                                    case CustomRoles.Concealer:
                                        Concealer.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Eraser:
                                        Eraser.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Sans:
                                        Sans.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Hacker:
                                        Hacker.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Psychic:
                                        Psychic.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Hangman:
                                        Hangman.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Judge:
                                        Judge.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Mortician:
                                        Mortician.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Mediumshiper:
                                        Mediumshiper.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Veteran:
                                        Main.VeteranNumOfUsed.Add(ps.PlayerId, Options.VeteranSkillMaxOfUseage.GetInt());
                                        break;
                                    case CustomRoles.Swooper:
                                        Swooper.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.BloodKnight:
                                        BloodKnight.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Totocalcio:
                                        Totocalcio.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Succubus:
                                        Succubus.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.DovesOfNeace:
                                        Main.DovesOfNeaceNumOfUsed.Add(ps.PlayerId, Options.DovesOfNeaceMaxOfUseage.GetInt());
                                        break;
                                    case CustomRoles.Rudepeople:
                                        Rudepeople.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.TimeMaster:
                                        Main.TimeMasterNum[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Vulture:
                                        Vulture.Add(ps.PlayerId);
                                        Main.VultureEatMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Bull:
                                        Main.BullKillMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Masochism:
                                        Main.MasochismKillMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Cultivator:
                                        Main.CultivatorKillMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Disorder:
                                        Main.DisorderKillCooldownMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Prophet:
                                        Prophet.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Scout:
                                        Scout.Add(ps.PlayerId);
                                        Main.ScoutImpotors[ps.PlayerId] = 0;
                                        Main.ScoutCrewmate[ps.PlayerId] = 0;
                                        Main.ScoutNeutral[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.StinkyAncestor:
                                        Main.StinkyAncestorKill[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Deputy:
                                        Deputy.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Prosecutors:
                                        Prosecutors.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.DemonHunterm:
                                        DemonHunterm.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Vandalism:
                                        Vandalism.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Lawyer:
                                        Lawyer.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Sidekick:
                                        Jackal.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Hunter:
                                        Main.HunterMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Crush:
                                        Main.CrushMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.PlagueDoctor:
                                        PlagueDoctor.Add(ps.PlayerId);
                                        PlagueDoctor.CanInfectInt[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Cupid:
                                        Main.CupidMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Akujo:
                                        Main.AkujoMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Slaveowner:
                                        Main.SlaveownerMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Spellmaster:
                                        Main.SpellmasterMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.SoulSeeker:
                                        Main.SoulSeekerCanKill[ps.PlayerId] = 0;
                                        Main.SoulSeekerNotCanKill[ps.PlayerId] = 0;
                                        Main.SoulSeekerCanEat[ps.PlayerId] = 0;
                                        Main.SoulSeekerDead[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Jealousy:
                                        Main.JealousyMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Captain:
                                        Captain.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Solicited:
                                        Captain.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Cowboy:
                                        Main.MaxCowboy[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.ElectOfficials:
                                        ElectOfficials.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.BSR:
                                        BSR.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.ChiefOfPolice:
                                        ChiefOfPolice.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Exorcist:
                                        Main.ExorcistMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Manipulator:
                                        Main.ManipulatorImpotors[ps.PlayerId] = 0;
                                        Main.ManipulatorCrewmate[ps.PlayerId] = 0;
                                        Main.ManipulatorNeutral[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Guide:
                                        Main.GuideMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Knight:
                                        Knight.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Nurse:
                                        Main.NnurseHelepMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.Corpse:
                                        Corpse.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.NiceGuesser:
                                        Main.PGuesserMax[ps.PlayerId] = 1;
                                        break;
                                    case CustomRoles.EvilGuesser:
                                        Main.PGuesserMax[ps.PlayerId] = 1;
                                        break;
                                    case CustomRoles.DoubleKiller:
                                        DoubleKiller.Add(ps.PlayerId);
                                        new LateTask(() =>
                                        {
                                            Main.DoubleKillerKillSeacond.Add(ps.PlayerId, Utils.GetTimeStamp());
                                            Utils.NotifyRoles();
                                        }, 5f, ("shuangdao"));
                                        break;
                                    case CustomRoles.EvilGambler:
                                        EvilGambler.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Merchant:
                                        Merchant.Add(ps.PlayerId);
                                        Main.MerchantMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.NiceTracker:
                                        NiceTracker.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Yandere:
                                        Yandere.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Buried:
                                        Buried.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Henry:
                                        Henry.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Chameleon:
                                        Chameleon.Add(ps.PlayerId);
                                        break;
                                    //       case CustomRoles.Kidnapper:
                                    //           Kidnapper.Add(pc.PlayerId);
                                    //          break;
                                    case CustomRoles.MimicKiller:
                                        Mimics.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.MimicAss:
                                        Mimics.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.ShapeShifters:
                                        ShapeShifters.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Fake:
                                        Main.FakeMax[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.NiceSwapper:
                                        NiceSwapper.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.EvilSwapper:
                                        EvilSwapper.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Blackmailer:
                                        Blackmailer.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Tom:
                                        Main.TomKill[ps.PlayerId] = 0;
                                        break;
                                    case CustomRoles.RewardOfficer:
                                        RewardOfficer.Add(ps.PlayerId);
                                        break;
                                    case CustomRoles.Copycat:
                                        Copycat.Add(ps.PlayerId);
                                        break;
                                }
                            }
                        }
                    }
                    Copycat.ForCopycat.Remove(player.PlayerId);
                }
                if (Challenger.ForChallenger.Contains(player.PlayerId) || Challenger.ForChallengerTwo.Contains(player.PlayerId))
                {
                    Challenger.ForChallenger.Remove(player.PlayerId);
                        Challenger.ForChallengerTwo.Remove(player.PlayerId);
                    var position = Challenger.Challengerbacktrack[player.PlayerId];
                    Utils.TP(player.NetTransform, position);
                    Challenger.Challengerbacktrack.Remove(player.PlayerId);
                }
            }
            Main.LastVotedPlayerInfo = null;
            EAC.MeetingTimes = 0;
        }
    }
}
