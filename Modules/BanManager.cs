using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static TheOtherRoles_Host.Translator;

namespace TheOtherRoles_Host;

public static class BanManager
{
    private static readonly string DENY_NAME_LIST_PATH = @"./TheOtherRoles_Host_Data/DenyName.txt";
    private static readonly string BAN_LIST_PATH = @"./TheOtherRoles_Host_Data/BanList.txt";
    private static List<string> EACList = new();
    public static void Init()
    {
        try
        {
            Directory.CreateDirectory("TheOtherRoles_Host_Data");

            if (!File.Exists(BAN_LIST_PATH))
            {
                Logger.Warn("创建新的 BanList.txt 文件", "BanManager");
                File.Create(BAN_LIST_PATH).Close();
            }
            if (!File.Exists(DENY_NAME_LIST_PATH))
            {
                Logger.Warn("创建新的 DenyName.txt 文件", "BanManager");
                File.Create(DENY_NAME_LIST_PATH).Close();
                File.WriteAllText(DENY_NAME_LIST_PATH, GetResourcesTxt("TheOtherRoles_Host.Resources.Config.DenyName.txt"));
            }

            //读取EAC名单
            var stream = ModUpdater.remark;//将EAC设为云端
            //stream.Position = 0;
            using StreamReader sr = new(stream, Encoding.UTF8);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "" || line.StartsWith("#")) continue;
                if (DevManager.DevUser.Any(x => x.IsDev && line.Contains(x.Code))) continue;
                EACList.Add(line);
            }

        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "BanManager");
        }
    }
    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
    public static void AddBanPlayer(InnerNet.ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || player == null) return;
        if (!CheckBanList(player?.FriendCode) && player.FriendCode != "")
        {
            File.AppendAllText(BAN_LIST_PATH, $"{player.FriendCode},{player.PlayerName}\n");
            Logger.SendInGame(string.Format(GetString("Message.AddedPlayerToBanList"), player.PlayerName));
        }
    }
    public static void CheckDenyNamePlayer(InnerNet.ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !Options.ApplyDenyNameList.GetBool()) return;
        try
        {
            Directory.CreateDirectory("TheOtherRoles_Host_Data");
            if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
            using StreamReader sr = new(DENY_NAME_LIST_PATH);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (DevManager.DevUser.Any(x => x.IsDev && line.Contains(x.Code))) continue;
                if (Regex.IsMatch(player.PlayerName, line))
                {
                    AmongUsClient.Instance.KickPlayer(player.Id, false);
                    Logger.SendInGame(string.Format(GetString("Message.KickedByDenyName"), player.PlayerName, line));
                    Logger.Info($"{player.PlayerName}は名前が「{line}」に一致したためキックされました。", "Kick");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckDenyNamePlayer");
        }
    }
    public static void CheckBanPlayer(InnerNet.ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !Options.ApplyBanList.GetBool()) return;
        if (CheckBanList(player?.FriendCode))
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            Logger.SendInGame(string.Format(GetString("Message.BanedByBanList"), player.PlayerName));
            Logger.Info($"{player.PlayerName}は過去にBAN済みのためBANされました。", "BAN");
            return;
        }
        if (CheckEACList(player?.FriendCode))
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            Logger.SendInGame(string.Format(GetString("Message.BanedByEACList"), player.PlayerName));
            Logger.Info($"{player.PlayerName}存在于EAC封禁名单", "BAN");
            return;
        }
    }
    public static bool CheckBanList(string code)
    {
        if (code == "") return false;
        try
        {
            Directory.CreateDirectory("TheOtherRoles_Host_Data");
            if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
            using StreamReader sr = new(BAN_LIST_PATH);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (DevManager.DevUser.Any(x => x.IsDev && line.Contains(x.Code))) continue;
                if (line.Contains(code)) return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckBanList");
        }
        return false;
    }
    public static bool CheckEACList(string code)
    {
        if (code == "") return false;
        return EACList.Any(x => x.Contains(code));
    }
}
[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
class BanMenuSelectPatch
{
    public static void Postfix(BanMenu __instance, int clientId)
    {
        InnerNet.ClientData recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
        if (recentClient == null) return;
        if (!BanManager.CheckBanList(recentClient?.FriendCode)) __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();
    }
}