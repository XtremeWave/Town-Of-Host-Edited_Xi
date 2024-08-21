using Hazel;
using System.Collections.Generic;
using TOHEXI.Modules;
using UnityEngine;

namespace TOHEXI.Roles.Neutral;

public static class SchrodingerCat
{
    private static readonly int Id = 75650050;
    private static List<byte> playerIdList = new();
    public static OptionItem CanKnowKiller;
    public static bool isimp = true;
    public static bool iscrew = true;
    public static bool isjac = true;
    public static bool isbk = true;
    public static bool isgam = true;
    public static bool ispg = true;
    public static bool isok = true;
    public static bool isdh = true;
    public static bool isyl = true;
    public static bool isln = true;
    public static bool noteam = true;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SchrodingerCat);
        CanKnowKiller = BooleanOptionItem.Create(5051505, "SchrodingerCatCanKnowKiller", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
    }
    public static void Init()
    {
        playerIdList = new();
        isimp = false;
        iscrew = false;
        isjac = false;
        isbk = false;
        isgam = false;
        ispg = false;
        isok = false;
        isdh = false;
        isyl = false;
        isln = false;
        noteam = true;
}
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
   
}