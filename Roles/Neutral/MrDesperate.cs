using MS.Internal.Xml.XPath;
using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using Sentry;
using System.Linq;
using TOHEXI.Roles.Neutral;
using UnityEngine;
using static TOHEXI.RandomSpawn;
using static UnityEngine.GraphicsBuffer;
using static TOHEXI.Options;
using LibCpp2IL;

namespace TOHEXI;

public static class MrDesperate
{
    private static readonly int Id = 945920551;
    public static List<byte> playerIdList = new();

    public static OptionItem MrDesperateKillMeCooldown;
    public static OverrideTasksData MrDesperateTasks;
    public static Dictionary<byte,int> KillTime = new();


    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.MrDesperate);
        MrDesperateKillMeCooldown = IntegerOptionItem.Create(Id + 10, "MrDesperateKillMeCooldown", new(0, 180, 2), 65, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.MrDesperate])
            .SetValueFormat(OptionFormat.Seconds);
        SpecialAgentTasks = OverrideTasksData.Create(Id + 114, TabGroup.NeutralRoles, CustomRoles.MrDesperate);
    }
    public static void Init()
    {
        playerIdList = new();
        KillTime = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        KillTime.TryAdd(playerId, MrDesperateKillMeCooldown.GetInt());
    }
    public static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.KillTime, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(KillTime[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (KillTime.ContainsKey(PlayerId))
            KillTime[PlayerId] = Limit;
        else
            KillTime.Add(PlayerId, MrDesperateKillMeCooldown.GetInt());
    }
    public static string GetMrDesperate (byte playerId) => Utils.ColorString((KillTime.TryGetValue(playerId, out var x) && x >= 1) ? Color.red : Color.gray, KillTime.TryGetValue(playerId, out var vandalismLimit) ? $"({vandalismLimit})" : "Invalid");
}
