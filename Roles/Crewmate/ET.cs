using Hazel;
using System.Collections.Generic;
using TOHEXI.Modules;
using UnityEngine;
using System;
using TOHEXI.Roles.Neutral;
using System.Linq;

namespace TOHEXI.Roles.Crewmate;

public static class ET
{
    private static readonly int Id = 756570;
    public static List<byte> playerIdList = new();
    private static OptionItem SkillCooldown;
    private static OptionItem ReduceSkillCooldown;
    private static OptionItem MaxSkillCooldown;
    private static OptionItem ETRadius;
    private static OptionItem ETTime;

    private static Dictionary<byte, float> NowCooldown;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ET);
        SkillCooldown = FloatOptionItem.Create(Id + 2, "ETSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ET])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceSkillCooldown = FloatOptionItem.Create(Id + 3, "ReduceSkillCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ET])
           .SetValueFormat(OptionFormat.Seconds);
        MaxSkillCooldown = FloatOptionItem.Create(Id + 4, "ETMinKillCooldown", new(0f, 180f, 2.5f), 2.5f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ET])
            .SetValueFormat(OptionFormat.Seconds);
        ETRadius = FloatOptionItem.Create(Id + 5, "ETRadiuss", new(0.5f, 3f, 0.5f), 2f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ET])
            .SetValueFormat(OptionFormat.Multiplier);
        ETTime = FloatOptionItem.Create(Id + 6, "ETTime", new(0f, 180f, 2.5f), 2.5f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ET])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        NowCooldown = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NowCooldown.TryAdd(playerId, SkillCooldown.GetFloat());
    }
    public static void SetHudActive(HudManager __instance, bool isActive)
    {
        __instance.SabotageButton.ToggleVisible(isActive);
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public static bool IsEnable() => playerIdList.Count > 0;
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(killer);
        NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] + ReduceSkillCooldown.GetFloat(), MaxSkillCooldown.GetFloat(), SkillCooldown.GetFloat());
        foreach (var player in Main.AllPlayerControls)
        {
            if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
            if (player == killer) continue;
            if (Vector2.Distance(killer.transform.position, player.transform.position) <= ETRadius.GetFloat())
            {
                player.ResetKillCooldown();
                player.SetKillCooldown();
                player.SyncSettings();
                player.RpcGuardAndKill(player);
                var KillTime = Main.AllPlayerKillCooldown[player.PlayerId];
                Main.AllPlayerKillCooldown[player.PlayerId] = 300f;
                Main.ForET.Remove(player.PlayerId);
                Main.ForET.Add(player.PlayerId);
               player.MarkDirtySettings();
                new LateTask(() =>
                {
                    Main.AllPlayerKillCooldown[player.PlayerId] = KillTime;
                    player.ResetKillCooldown();
                    player.SetKillCooldown();
                    player.SyncSettings();
                    player.RpcGuardAndKill(player);
                    Main.ForET.Remove(player.PlayerId);
                    player.MarkDirtySettings();
                },ETTime.GetFloat());
            }
        }
        return false;
    }
}