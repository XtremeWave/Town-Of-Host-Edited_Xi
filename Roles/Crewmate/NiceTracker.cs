using AmongUs.GameOptions;
using Hazel;
using Il2CppSystem.Text;
using System.Collections.Generic;
using TOHEXI.Roles.Crewmate;
using UnityEngine;
using static TOHEXI.Options;
using static TOHEXI.Translator;
namespace TOHEXI.Roles.Impostor;

public static class NiceTracker
{
    private static readonly int Id = 418735;
    private static List<byte> playerIdList = new();

    public static OptionItem SkillLimitOpt;
    public static OptionItem SkillCooldown;
    public static OptionItem ResetArrow;
    private static OptionItem OptionCanSeeLastRoomInMeeting;
    public static Dictionary<byte, int> NiceTrackerLimit = new();
    public static bool CanSeeLastRoomInMeeting;
    private static Dictionary<byte, string> lastPlayerName = new();
    public static Dictionary<byte, string> msgToSend = new();
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.NiceTracker);
        SkillCooldown = FloatOptionItem.Create(Id + 42, "NiceTrackerSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.NiceTracker])
           .SetValueFormat(OptionFormat.Seconds);
        SkillLimitOpt = IntegerOptionItem.Create(Id + 44, "NiceTrackerSkillLimit", new(1, 990, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.NiceTracker])
             .SetValueFormat(OptionFormat.Times);
        ResetArrow = BooleanOptionItem.Create(Id + 48, "ResetArrow", false, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.NiceTracker]);
        OptionCanSeeLastRoomInMeeting = BooleanOptionItem.Create(Id + 46, "NiceTrackerCanSeeLastRoomInMeeting", false, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.NiceTracker]).SetHidden(true);
    }
    public static void Init()
    {
        playerIdList = new();
        lastPlayerName = new();
        msgToSend = new();
        NiceTrackerLimit = new();
        CanSeeLastRoomInMeeting = OptionCanSeeLastRoomInMeeting.GetBool();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NiceTrackerLimit.TryAdd(playerId, SkillLimitOpt.GetInt());

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        NiceTrackerLimit.Remove(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Remove(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId, bool add, Vector3 loc = new())
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetNiceTrackerArrow, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(add);
        if (add)
        {
            writer.Write(loc.x);
            writer.Write(loc.y);
            writer.Write(loc.z);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (NiceTrackerLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? SkillCooldown.GetFloat() : 300f;
    public static string GetSkillLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.NiceTracker) : Color.gray, NiceTrackerLimit.TryGetValue(playerId, out var niceTrackerLimit) ? $"({niceTrackerLimit})" : "Invalid");
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        bool add = reader.ReadBoolean();
        if (add)
            LocateArrow.Add(playerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        else
            LocateArrow.RemoveAllTarget(playerId);
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (NiceTrackerLimit.ContainsKey(PlayerId))
            NiceTrackerLimit[PlayerId] = Limit;
        else
            NiceTrackerLimit.Add(PlayerId, SkillLimitOpt.GetInt());
    }
    public static bool IsPlayer(PlayerControl seer, PlayerControl target)
    => seer.IsAlive() && playerIdList.Contains(seer.PlayerId)&& target.IsAlive() && seer != target && (target.Is(CustomRoleTypes.Impostor) || target.Is(CustomRoleTypes.Crewmate) || target.Is(CustomRoleTypes.Neutral));
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        var Tracker = target.PlayerId;
        NiceTrackerLimit[killer.PlayerId]--;
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.RpcGuardAndKill(target);
        Logger.Info($"qwq", "Warlock");
        var pos = killer.GetTruePosition();
        float minDis = float.MaxValue;
        string minName = "";
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == killer.PlayerId) continue;
            var dis = Vector2.Distance(pc.GetTruePosition(), pos);
            if (dis < minDis && dis < 1.5f)
            {
                minDis = dis;
                minName = pc.GetRealName();
            }
        }

        lastPlayerName.TryAdd(killer.PlayerId, minName);
        foreach (var pc in playerIdList)
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null || !player.IsAlive()) continue;
            LocateArrow.Add(pc, killer.transform.position);
            SendRPC(pc, true, killer.transform.position);
        }
        return false;
    }
    public static void OnReportDeadBody(PlayerControl pc, GameData.PlayerInfo target)
    {
        if (ResetArrow.GetBool())
        {
            foreach (var apc in playerIdList)
            {
                Main.ForNiceTracker.Remove(apc);
                LocateArrow.RemoveAllTarget(apc);
                SendRPC(apc, false);
            }
        }
    }
    public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!seer.Is(CustomRoles.NiceTracker)) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (GameStates.IsMeeting) return "";
        return Utils.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), LocateArrow.GetArrows(seer));
    }
    public static string GetArrowAndLastRoom(PlayerControl seer, PlayerControl target)
    {
        string text = Utils.ColorString(new Color32(0, 203, 128, byte.MaxValue), TargetArrow.GetArrows(seer, target.PlayerId));
        var room = Main.PlayerStates[target.PlayerId].LastRoom;
        if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
        else text += Utils.ColorString(new Color32(0, 203, 128, byte.MaxValue), "@" + GetString(room.RoomId.ToString()));
        return text;
    }
}