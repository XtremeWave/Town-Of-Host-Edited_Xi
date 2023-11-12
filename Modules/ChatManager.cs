using System.Collections.Generic;
using System.Linq;
using Hazel;
using System;
using TheOtherRoles_Host.Roles.Impostor;
using TheOtherRoles_Host.Roles.Neutral;
using TheOtherRoles_Host.Roles.Crewmate;
using static TheOtherRoles_Host.Translator;

namespace TheOtherRoles_Host.Modules.ChatManager
{
    public class ChatManager
    {
        public static bool cancel = false;
        private static List<string> chatHistory = new();
        private const int maxHistorySize = 20;
        #region 检查
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
        #endregion
        public static void GetMessage(PlayerControl player, string message)
        {
            List<(string, byte, string)> msgToSend = new();

            void AddMsg(string text, byte sendTo = 255, string title = "")
                => msgToSend.Add((text, sendTo, title));
            if (!player.IsAlive() || !AmongUsClient.Instance.AmHost) return;
            int operate;
            string msg = message;
            if ((
                GuessManager.GuesserMsg(player, msg) ||
                Judge.TrialMsg(player, msg) ||
                Copycat.CopycatMsg(player, msg) ||
                NiceSwapper.SwapMsg(player, msg) ||
                EvilSwapper.SwapMsg(player, msg) ||
                Challenger.ChallengerMsg(player, msg) ||
                ID(player, msg)
                ) && Options.NewHideMsg.GetBool())
            {
                cancel = true;
            }
            else if (Blackmailer.ForBlackmailer.Contains(player.PlayerId))
            {
                SendPreviousMessagesToAll();
                AddMsg(string.Format(GetString("BeBlackMailed"), Main.AllPlayerNames[player.PlayerId]), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Blackmailer), GetString("BlackmaileKillTitle")));
                cancel = true;
            }
            else
            {
                message = message.ToLower().TrimStart().TrimEnd();
                if (!GameStates.IsInGame) operate = 1;
                else if (CheckCommond(ref message, "shoot|guess|bet|st|gs|bt|猜|赌|sp|jj|tl|trial|审判|判|审|xp|效颦|效|颦|sw|换票|换|swap", false)) operate = 2;
                else if (CheckCommond(ref message, "up", false)) operate = 3;
                else if (CheckCommond(ref message, "r|role|m|myrole|n|now")) operate = 4;
                else operate = 1;
                if (operate == 1 && Options.NewHideMsg.GetBool())
                {
                    message = msg;
                    string chatEntry = $"{player.PlayerId}: {message}";
                    chatHistory.Add(chatEntry);
                    if (chatHistory.Count > maxHistorySize)
                    {
                        chatHistory.RemoveAt(0);
                    }
                    cancel = false;
                }
                else if (operate == 2 && Options.NewHideMsg.GetBool())
                {
                    Logger.Info($"包含特殊信息，不记录", "ChatManager");
                    message = msg;
                    SendPreviousMessagesToAll();
                    cancel = true;
                }
                else if (operate == 3)
                {
                    Logger.Info($"指令{msg}，不记录", "ChatManager");
                    message = msg;
                    cancel = false;
                }
                else if (operate == 4 && Options.NewHideMsg.GetBool())
                {
                    Logger.Info($"指令{msg}，不记录", "ChatManager");
                    message = msg;
                    SendPreviousMessagesToAll();
                    cancel = false;
                }

            }

        }
        public static void SendPreviousMessagesToAll()
        {
            var rd = IRandom.Instance;
            string msg;
            for (int i = chatHistory.Count; i < 40; i++)
            {
                msg = $"{GetString("HideMessage")}";
                var player = Main.AllAlivePlayerControls.ToArray()[rd.Next(0, Main.AllAlivePlayerControls.Count())];
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                writer.StartMessage(-1);
                writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                    .Write(msg)
                    .EndRpc();
                writer.EndMessage();
                writer.SendMessage();
            }
            foreach (var entry in chatHistory)
            {
                var entryParts = entry.Split(':');
                var senderId = entryParts[0].Trim();
                var senderMessage = entryParts[1].Trim();

                foreach (var senderPlayer in Main.AllPlayerControls)
                {
                    if (senderPlayer.PlayerId.ToString() == senderId)
                    {
                        if (!senderPlayer.IsAlive())
                        {
                            var deathReason = (PlayerState.DeathReason)senderPlayer.PlayerId;
                            var realkiller = senderPlayer.GetRealKiller();
                            senderPlayer.ReviveV2(senderPlayer.GetCustomRole().GetRoleTypes());
                            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(senderPlayer, senderMessage);
                            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                            writer.StartMessage(-1);
                            writer.StartRpc(senderPlayer.NetId, (byte)RpcCalls.SendChat)
                                .Write(senderMessage)
                                .EndRpc();
                            writer.EndMessage();
                            writer.SendMessage();
                            senderPlayer.RpcExileV2();
                            senderPlayer.Die(DeathReason.Kill, true);
                            senderPlayer.SetRealKiller(realkiller);
                            senderPlayer.Data.IsDead = true;
                            Main.PlayerStates[senderPlayer.PlayerId].deathReason = deathReason;
                        }
                        else
                        {
                            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(senderPlayer, senderMessage);
                            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                            writer.StartMessage(-1);
                            writer.StartRpc(senderPlayer.NetId, (byte)RpcCalls.SendChat)
                                .Write(senderMessage)
                                .EndRpc();
                            writer.EndMessage();
                            writer.SendMessage();
                        }

                    }
                }

            }
        }
        #region ID CHECK
        public static bool ID(PlayerControl pc, string msg, bool isUI = true)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            if (pc == null) return false;
            msg = msg.ToLower().TrimStart().TrimEnd();
            if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id"))
            {
                Utils.SendMessage(GetFormatString(), pc.PlayerId);
                return true;
            }
            else return false;
        }
        public static string GetFormatString()
        {
            string text = GetString("PlayerIdList");
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                string id = pc.PlayerId.ToString();
                string name = pc.GetRealName();
                text += $"\n{id} → {name}";
            }
            return text;
        }
        #endregion

    }

}
