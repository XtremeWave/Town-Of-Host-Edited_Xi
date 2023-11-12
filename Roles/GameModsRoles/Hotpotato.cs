using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheOtherRoles_Host.Roles.GameModsRoles;

public static class Holdpotato
{
    public static List<byte> playerIdList = new();
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (!killer.Is(CustomRoles.Hotpotato)) return false;
        if (target.Is(CustomRoles.Hotpotato)) return false;
        target.RpcSetCustomRole(CustomRoles.Hotpotato);
        killer.RpcSetCustomRole(CustomRoles.Coldpotato);
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
        killer.SetKillCooldownV2(target: target, forceAnime: true);
        Utils.NotifyRoles(killer);
        Utils.NotifyRoles(target);
        return false;
    }
}