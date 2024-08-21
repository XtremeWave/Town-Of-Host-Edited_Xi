using Il2CppSystem.Threading.Tasks;
using System;
using System.Collections.Generic;
using TOHEXI.Modules;
using TOHEXI.Roles.Neutral;
using static TOHEXI.Options;

namespace TOHEXI;

public static class SpeedUp
{
    private static readonly int Id = 13786231;
    private static List<byte> playerIdList = new();
    public static OptionItem SkillCooldown;
    public static OptionItem ForSpeed;
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.SpeedUp);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "SpeedUpSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SpeedUp])
            .SetValueFormat(OptionFormat.Seconds);
        ForSpeed = FloatOptionItem.Create(Id + 11, "ForSpeed", new(0.5f, 3.0f, 0.5f), 0.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SpeedUp])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable() => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = SkillCooldown.GetFloat();
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.RpcGuardAndKill(target);
        Main.AllPlayerSpeed[killer.PlayerId] += ForSpeed.GetFloat();
        new LateTask(() =>
        {
            Main.AllPlayerSpeed[target.PlayerId] -= ForSpeed.GetFloat();
        }, 3f);
        return false;
    }
}