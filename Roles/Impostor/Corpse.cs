using System;
using System.Collections.Generic;
using static TheOtherRoles_Host.Options;

namespace TheOtherRoles_Host;

public static class Corpse
{
    private static readonly int Id = 15648979;
    public static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Corpse);
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
    public static void OnCheckMurder(PlayerControl target)
    {
        Main.KillForCorpse.Add(target.PlayerId);
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Corpse) && Main.KillForCorpse.Contains(target.PlayerId)) return true;
        return false;
    }
}
