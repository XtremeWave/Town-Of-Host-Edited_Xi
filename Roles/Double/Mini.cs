using HarmonyLib;
using Hazel;
using MS.Internal.Xml.XPath;
using Sentry;
using System.Collections.Generic;
using System.Linq;
using TheOtherRoles_Host.Roles.Neutral;
using UnityEngine;
using static TheOtherRoles_Host.RandomSpawn;
using static UnityEngine.GraphicsBuffer;

namespace TheOtherRoles_Host.Roles.Double;
public class Mini
{
    private static readonly int Id = 7565376;
    public static bool IsEvilMini = false;
    public static void SetMiniTeam(float EvilMiniRate)
    {
        EvilMiniRate = EvilMiniSpawnChances.GetFloat();
        IsEvilMini = Random.Range(1, 100) < EvilMiniRate;
    }
    private static List<byte> playerIdList = new();
    public static int GrowUpTime = new();
    public static int GrowUp = new();
    public static int EvilKillCDmin = new();
    private static long LastFixedUpdate = new();
    public static int Age = new();
    public static OptionItem GrowUpDuration;
    public static OptionItem EveryoneCanKnowMini;
    public static OptionItem CountMeetingTime;
    public static OptionItem EvilMiniSpawnChances;
    public static OptionItem CanBeEvil;
    public static OptionItem UpDateAge;
    public static OptionItem MinorCD;
    public static OptionItem MajorCD;
    public static void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mini, 1, zeroOne: false);
        GrowUpDuration = IntegerOptionItem.Create(Id + 100, "GrowUpDuration", new(200, 800, 25), 400, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini])
            .SetValueFormat(OptionFormat.Seconds);
        EveryoneCanKnowMini = BooleanOptionItem.Create(Id + 102, "EveryoneCanKnowMini", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        CanBeEvil = BooleanOptionItem.Create(Id + 106, "CanBeEvil", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        EvilMiniSpawnChances = IntegerOptionItem.Create(Id + 108, "EvilMiniSpawnChances", new(0, 100, 5), 50, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
            .SetValueFormat(OptionFormat.Percent);
        MinorCD = FloatOptionItem.Create(Id + 110, "KillCooldown", new(0f, 180f, 2.5f), 45f, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
            .SetValueFormat(OptionFormat.Seconds);
        MajorCD = FloatOptionItem.Create(Id + 112, "MajorCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
           .SetValueFormat(OptionFormat.Seconds);
        UpDateAge = BooleanOptionItem.Create(Id + 114, "UpDateAge", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        CountMeetingTime = BooleanOptionItem.Create(Id + 116, "CountMeetingTime", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
    }
    public static void Init()
    {
        GrowUpTime = 0;
        playerIdList = new();
        GrowUp = GrowUpDuration.GetInt() / 18;
        Age = 0;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncMiniCrewAge, SendOption.Reliable, -1);
        writer.Write(Age);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        Age = reader.ReadInt32();
    }
    public static void OnFixedUpdate(PlayerControl player)
    {

        if (!GameStates.IsInGame || !AmongUsClient.Instance.AmHost) return;
        if (!player.Is(CustomRoles.NiceMini) && !player.Is(CustomRoles.EvilMini) || !IsEnable) return;
        if (Age >= 18 || (!CountMeetingTime.GetBool() && GameStates.IsMeeting)) return;
        if (LastFixedUpdate == Utils.GetTimeStamp()) return;
        LastFixedUpdate = Utils.GetTimeStamp();
        Mini.GrowUpTime++;
        if ( player.Is(CustomRoles.NiceMini))
        {
            
                
                
                if (Mini.GrowUpTime >= Mini.GrowUpDuration.GetInt() / 18)
                {
                    Mini.Age += 1;
                    Mini.GrowUpTime = 0;
                    player.RpcGuardAndKill();
                    Logger.Info($"年龄增加1", "Child");
                    Mini.SendRPC(player.PlayerId);
                    if (Mini.UpDateAge.GetBool())
                    {
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            if (pc.PlayerId != player.PlayerId) continue;
                            player.Notify(Translator.GetString("MiniUp"));
                        }
                    }
                }
            
            if (!player.IsAlive())
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.NiceMini);
                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                return;
            }
        }
        else if (player.Is(CustomRoles.EvilMini))
        {
            
                if (Main.EvilMiniKillcooldown[player.PlayerId] >= 1f)
                {
                    Main.EvilMiniKillcooldown[player.PlayerId]--;

                }
                if (Mini.GrowUpTime >= Mini.GrowUpDuration.GetInt() / 18)
                {
                    Main.EvilMiniKillcooldownf = Main.EvilMiniKillcooldown[player.PlayerId];
                    Logger.Info($"记录击杀冷却{Main.EvilMiniKillcooldownf}", "Child");
                    Main.AllPlayerKillCooldown[player.PlayerId] = Main.EvilMiniKillcooldownf;
                    Main.EvilMiniKillcooldown[player.PlayerId] = Main.EvilMiniKillcooldownf;
                    player.MarkDirtySettings();
                    Mini.Age += 1;
                    Mini.GrowUpTime = 0;
                    Logger.Info($"年龄增加1", "Child");
                    Mini.SendRPC(player.PlayerId);
                    if (Mini.UpDateAge.GetBool())
                    {
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            if (pc.PlayerId != player.PlayerId) continue;
                            player.Notify(Translator.GetString("MiniUp"));
                        }
                    }
                    Logger.Info($"重置击杀冷却{Main.EvilMiniKillcooldownf - 1f}", "Child");


                }
            
        }
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;

    public static string GetAge(byte playerId) => Utils.ColorString(Color.yellow, Age != 18 ? $"({Age})" : "");
}
