using static TOHEXI.Translator;
using System.Collections.Generic;
using TOHEXI.Modules;
using static TOHEXI.Options;
using System;

namespace TOHEXI;


public static class Rudepeople
{
    private static readonly int Id = 11558955;
    public static List<byte> playerIdList = new();

    private static OptionItem DefaultKillCooldown;
    public static Dictionary<byte, long> RudepeopleInProtect = new();
    private static Dictionary<byte, float> NowCooldown;
    public static List<byte> ForRudepeople = new();
    public static OptionItem RudepeopleSkillDuration;
    public static OptionItem RudepeopleSkillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Rudepeople);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "RudepeopleSkillCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rudepeople])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "ReduceCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rudepeople])
    .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, "MaxCooldown", new(0f, 180f, 2.5f), 65f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rudepeople])
            .SetValueFormat(OptionFormat.Seconds);
        RudepeopleSkillDuration = FloatOptionItem.Create(807412747, "RudepeopleSkillDuration", new(1f, 999f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Rudepeople])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        NowCooldown = new();
        RudepeopleInProtect = new();
        ForRudepeople = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
    }
    public static bool IsEnable() => playerIdList.Count > 0;
    public static void SetCooldown(byte id) => AURoleOptions.EngineerCooldown = NowCooldown[id];
    public static void OnEnterVent(PlayerControl pc)
    {
        if (pc == null || !pc.Is(CustomRoles.Rudepeople)) return;
        NowCooldown[pc.PlayerId] = Math.Clamp(NowCooldown[pc.PlayerId] + ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        pc.SyncSettings();
        RudepeopleInProtect.Remove(pc.PlayerId);
        RudepeopleInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
        if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
        pc.RPCPlayCustomSound("RNM");
        pc.Notify(GetString("RudepeopleOnGuard"), RudepeopleSkillDuration.GetFloat());
    }
    public static bool CheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !target.Is(CustomRoles.Rudepeople)) return true;
        if (RudepeopleInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
            if (RudepeopleInProtect[target.PlayerId] + RudepeopleSkillDuration.GetInt() >= Utils.GetTimeStamp(DateTime.Now))
            {
                Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
                killer.RpcMurderPlayerV3(target);
                killer.RpcMurderPlayerV3(killer);
                killer.SetRealKiller(target);
                ForRudepeople.Add(killer.PlayerId);
                return false;
            }
        return false;
    }
    public static void FixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask || !Rudepeople.IsEnable()) return;
        if (GameStates.IsInTask && player.Is(CustomRoles.Rudepeople))
        {
            if (RudepeopleInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + RudepeopleSkillDuration.GetInt() < Utils.GetTimeStamp())
            {
                RudepeopleInProtect.Remove(player.PlayerId);
                player.RpcGuardAndKill();
                player.Notify(string.Format(GetString("RudepeopleOffGuard")));
            }
        }
    }
}

