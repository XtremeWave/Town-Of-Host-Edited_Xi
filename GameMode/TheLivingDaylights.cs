using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheOtherRoles_Host.Options;

namespace TheOtherRoles_Host.GameMode;


public static class TheLivingDaylights
{
    public static int RoundTime = new();
    public static int BoomTimes = new();
    //设置
    public static OptionItem TD;//总时长;Totalduration

    public static void SetupCustomOption()
    {
      TD  = IntegerOptionItem.Create(644643189, "KB_GameTime", new(30, 300, 5), 180, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.TheLivingDaylights)
            .SetColor(new Color32(245, 82, 82, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetHeader(true);
    }
    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.TheLivingDaylights) return;
        RoundTime = TD.GetInt() + 8;
      
    }
    public static string GetHudText()
    {
        return string.Format(Translator.GetString("KBTimeRemain"), RoundTime.ToString());
    }

}