using TOHEXI.Modules;
using UnityEngine;
using static TOHEXI.Options;
using static TOHEXI.Translator;
using static TOHEXI.Utils;

namespace TOHEXI.Roles.Impostor;
//来源：TOHER https://github.com/Loonie-Toons/TownOfHost-ReEdited 谢谢Lonnie!
public static class Disperser
{
    private static readonly int Id = 17000;

    private static OptionItem DisperserShapeshiftCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Disperser);
        DisperserShapeshiftCooldown = FloatOptionItem.Create(Id + 5, "DisCooldown", new(1f, 999f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disperser])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = DisperserShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public static void DispersePlayers(PlayerControl shapeshifter)
    {
        var rd = new System.Random();
        var vents = Object.FindObjectsOfType<Vent>();

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (shapeshifter.PlayerId == pc.PlayerId || pc.Data.IsDead || pc.onLadder || pc.inVent || GameStates.IsMeeting)
            {
                if (!pc.Is(CustomRoles.Disperser))
                    pc.Notify(ColorString(GetRoleColor(CustomRoles.Disperser), string.Format(GetString("ErrorTeleport"), pc.GetRealName())));
                
                continue;
            }

            pc.RPCPlayCustomSound("Teleport");
            var vent = vents[rd.Next(0, vents.Count)];
            TP(pc.NetTransform, new Vector2(vent.transform.position.x, vent.transform.position.y));
            pc.Notify(ColorString(GetRoleColor(CustomRoles.Disperser), string.Format(GetString("TeleportedInRndVentByDisperser"), pc.GetRealName())));
        }
    }
}