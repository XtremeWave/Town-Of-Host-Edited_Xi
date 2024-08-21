using Hazel;
using InnerNet;
using MS.Internal.Xml.XPath;
using System.Collections.Generic;

using static TOHEXI.Options;

namespace TOHEXI.Roles.Impostor;

public static class Mare
{
    private static readonly int Id = 2300;
    public static List<byte> playerIdList = new();

    private static OptionItem KillCooldownInLightsOut;
    private static OptionItem SpeedInLightsOut;
    public static bool idAccelerated = false;  //加速済みかフラグ


    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Mare);
        SpeedInLightsOut = FloatOptionItem.Create(Id + 10, "MareAddSpeedInLightsOut", new(0.1f, 0.5f, 0.1f), 0.3f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mare])
            .SetValueFormat(OptionFormat.Multiplier);
        KillCooldownInLightsOut = FloatOptionItem.Create(Id + 11, "MareKillCooldownInLightsOut", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mare])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte mare)
    {
        playerIdList.Add(mare);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static float GetKillCooldown => Utils.IsActive(SystemTypes.Electrical) ? KillCooldownInLightsOut.GetFloat() : DefaultKillCooldown;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = GetKillCooldown;
    public static void ApplyGameOptions(byte playerId)
    {
        if (Utils.IsActive(SystemTypes.Electrical) && !idAccelerated)
        { //停電中で加速済みでない場合
            idAccelerated = true;
            Main.AllPlayerSpeed[playerId] += SpeedInLightsOut.GetFloat();//Mareの速度を加算
        }
        else if (!Utils.IsActive(SystemTypes.Electrical) && idAccelerated)
        { //停電中ではなく加速済みになっている場合
            idAccelerated = false;
            Main.AllPlayerSpeed[playerId] -= SpeedInLightsOut.GetFloat();//Mareの速度を減算
        }
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (idAccelerated == true) return false;
        killer.RpcGuardAndKill(target);
        MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, killer.GetClientId());
            SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, target.GetClientId());
            SabotageFixWriter.Write((byte)SystemTypes.Electrical);
            MessageExtensions.WriteNetObject(SabotageFixWriter, target);
            AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
        return true;
    }
        public static bool KnowTargetRoleColor(PlayerControl target, bool isMeeting)
        => !isMeeting && playerIdList.Contains(target.PlayerId) && Utils.IsActive(SystemTypes.Electrical);
}