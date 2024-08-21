using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hazel;
using UnityEngine;
using static TOHEXI.Options;
using Microsoft.Extensions.Logging;
using static UnityEngine.GraphicsBuffer;

namespace TOHEXI.Roles.Crewmate;

    public static class Buried
    {
    private static readonly int Id = 9165789;
    private static OptionItem BuriedCooldown;
    private static List<byte> playerIdList = new();
    public static Dictionary<int, byte> landmineDict = new Dictionary<int, byte>();
    private static Dictionary<byte, int> ventedId = new();
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Buried);
        BuriedCooldown = FloatOptionItem.Create(Id + 2, "BuriedCooldown", new(1f, 999f, 1f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Buried])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        ventedId = new();
        landmineDict = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void ApplyGameOptions()
    {
        AURoleOptions.EngineerCooldown = BuriedCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public static void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if(pc == null || !pc.Is(CustomRoles.Buried)) return;
        // 将管道ID添加到字典中，标识为 1
        landmineDict[vent.Id] = 1;
         // 发送RPC消息通知客户端
         MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, 34, SendOption.Reliable, pc.GetClientId());
        writer.WritePacked(vent.Id);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
}
