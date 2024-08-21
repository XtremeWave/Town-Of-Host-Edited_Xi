using HarmonyLib;
using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using System.Linq;
using TOHEXI.Roles.Neutral;
using UnityEngine;
using static TOHEXI.RandomSpawn;
using static UnityEngine.GraphicsBuffer;

namespace TOHEXI;

internal static class ModeArrestManager
{
    public static int Time = new();
    public static int killcd = new();
    public static int killerr = new();
    public static int twz = new();

    public static OptionItem Arrestkillcd;//抓捕者数量
    public static OptionItem TD;//总时长;Totalduration


    public static void SetupCustomOption()
    {
        TD = IntegerOptionItem.Create(76_235_0066, "KB_GameTime", new(30, 300, 5), 180, TabGroup.GameSettings, false)
           .SetGameMode(CustomGameMode.ModeArrest)
           .SetColor(new Color32(245, 82, 82, byte.MaxValue))
           .SetValueFormat(OptionFormat.Seconds)
           .SetHeader(true);
        Arrestkillcd = IntegerOptionItem.Create(76_235_034, "Arrestkillcd", new(5, 25, 5), 5, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.ModeArrest)
            .SetColor(new Color32(245, 82, 82, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.ModeArrest) return;
        Time = TD.GetInt();
        killcd = Arrestkillcd.GetInt();
        killerr = 1;
        twz = 0;
    }


    public static void OnPlayerAttack(PlayerControl killer, PlayerControl target)
    {
        if (killer.Is(CustomRoles.captor) && target.Is(CustomRoles.runagat))
        {
            target.RpcSetCustomRole(CustomRoles.captor);
            killerr += 1;
            twz -= 1;
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        private static long LastFixedUpdate = new();
        public static void Postfix(PlayerControl __instance)
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.ModeArrest) return;

            if (AmongUsClient.Instance.AmHost)
            {
                if(twz == 0)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.captor);

                    foreach (var pc in Main.AllAlivePlayerControls)
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                }
                if(Time == 0)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.runagat);
                }

             }

                if (LastFixedUpdate == Utils.GetTimeStamp()) return;
                LastFixedUpdate = Utils.GetTimeStamp();

                // 减少全局倒计时
                Time--;
            }
        }
    }
