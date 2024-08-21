using HarmonyLib;
using Discord;
using System;

namespace TOHEXI.Patches
{
    [HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
    public class DiscordRPC
    {
        private static string lobbycode = "";
        private static string region = "";
        public static void Prefix([HarmonyArgument(0)] Activity activity)
        {
            var details = $"TOHEXI v{Main.PluginDisplayVersion}";
            activity.Details = details;

            try
            {
                if (activity.State != "In Menus")
                {
                    if (Options.ShowLobbyCode.GetBool())
                    {
                        int maxSize = GameOptionsManager.Instance.currentNormalGameOptions.MaxPlayers;
                        if (GameStates.IsLobby)
                        {
                            lobbycode = GameStartManager.Instance.GameRoomNameCode.text;
                            region = ServerManager.Instance.CurrentRegion.Name;
                            if (region == "North America") region = "North America";
                            if (region == "Europe") region = "Europe";
                            if (region == "Asia") region = "Asia";
                            //if (region == "模组服务器北美洲MNA") region = "Modded North America";
                            //if (region == "模组服务器北美洲MEU") region = "Modded Europe";
                            //if (region == "模组服务器北美洲MAS") region = "Modded Asia";
                        }

                        if (lobbycode != "" && region != "")
                        {
                            details = $"TOHEXI - {lobbycode} ({region})";
                        }

                        activity.Details = details;
                    }
                    else
                    {
                        details = $"TOHEXI v{Main.PluginDisplayVersion}";
                    }
                }
            }

            catch (ArgumentException ex)
            {
                Logger.Error("Error in updating discord rpc", "DiscordPatch");
                Logger.Exception(ex, "DiscordPatch");
                details = $"TOHEXI v{Main.PluginDisplayVersion}";
                activity.Details = details;
            }
        }
    }
}