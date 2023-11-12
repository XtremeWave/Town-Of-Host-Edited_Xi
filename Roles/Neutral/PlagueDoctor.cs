using System.Collections.Generic;
using System.Linq;
using static TheOtherRoles_Host.Options;
using UnityEngine;
using Hazel;

namespace TheOtherRoles_Host.Roles.Neutral;
public static class PlagueDoctor
{
    private static readonly int Id = 75650040;
    public static List<byte> playerIdList = new();
    public static List<byte> InfectList = new();
    public static Dictionary<byte, float> InfectInt = new();
    public static float currentProgress = new();
    public static int InfectNum = new();
    public static Dictionary<byte, int> CanInfectInt = new();
    public static int Immunitytimes= new();
    public static bool ImmunityGone = new();

    public static OptionItem InfectCooldown;
    public static OptionItem InfectTargetinfectcooldown;
    public static OptionItem CanWinAfterDead;
    public static OptionItem Infectmurder;
    public static OptionItem InfectTimes;
    public static OptionItem InfectSelf;
    public static OptionItem SetImmunitytime;
    public static OptionItem Immunitytime;
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PlagueDoctor);
        InfectCooldown = FloatOptionItem.Create(Id + 2, "infectCooldown", new(0f, 900f, 2.5f), 7.5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PlagueDoctor])
            .SetValueFormat(OptionFormat.Seconds);
        InfectTargetinfectcooldown = FloatOptionItem.Create(Id + 3, "InfectTargetinfectcooldown", new(0f, 25f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PlagueDoctor])
            .SetValueFormat(OptionFormat.Seconds);
        InfectTimes = IntegerOptionItem.Create(Id + 6, "InfectTimes", new(1, 15, 1), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PlagueDoctor])
            .SetValueFormat(OptionFormat.Times);
        CanWinAfterDead = BooleanOptionItem.Create(Id + 4, "CanWinAfterDead", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PlagueDoctor]);
        Infectmurder = BooleanOptionItem.Create(Id + 5, "Infectmurder", false, TabGroup.NeutralRoles, false).SetParent(CanWinAfterDead);
        InfectSelf = BooleanOptionItem.Create(Id + 7, "InfectSelf", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PlagueDoctor]);
        SetImmunitytime = BooleanOptionItem.Create(Id + 9, "SetImmunitytime", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PlagueDoctor]);
        Immunitytime = FloatOptionItem.Create(Id + 8, "Immunitytime", new(0f, 15f, 1f), 8f, TabGroup.NeutralRoles, false).SetParent(SetImmunitytime)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        InfectList = new();
        InfectNum = 0;
        Immunitytimes = Immunitytime.GetInt();
        currentProgress = 0f;
        InfectInt = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        foreach (var pc in Main.AllAlivePlayerControls)
            InfectInt.TryAdd(pc.PlayerId, 0f);
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetInfect, SendOption.Reliable, -1);
        writer.Write(playerId);
        if (InfectInt.ContainsKey(playerId))     
            writer.Write(InfectInt[playerId]);
        writer.Write(CanInfectInt[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Health = reader.ReadInt32();
        if (InfectInt.ContainsKey(PlayerId))
            InfectInt[PlayerId] = Health;
        int Limit = reader.ReadInt32();
        CanInfectInt.TryAdd(PlayerId, Limit);
        CanInfectInt[PlayerId] = Limit;
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = InfectCooldown.GetFloat();
    public static void OnFixedUpdate(PlayerControl pdd)
    {
        if (!IsEnable || !GameStates.IsInTask || !GameStates.InGame) return;

        foreach (var pd in Main.AllPlayerControls)
        {
            if (GameStates.IsInTask && pd.Is(CustomRoles.PlagueDoctor) && (pd.IsAlive() && InfectNum >= (Main.AllAlivePlayerControls.ToList().Count - 1) || !pd.IsAlive() && InfectNum >= (Main.AllAlivePlayerControls.ToList().Count - 1) && CanWinAfterDead.GetBool()))
            {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.PlagueDoctor);
                    CustomWinnerHolder.WinnerIds.Add(pd.PlayerId);
            }
            if (InfectSelf.GetBool() && pd.Is(CustomRoles.PlagueDoctor) && !InfectList.Contains(pd.PlayerId) && pd.IsAlive())
            {
                InfectList.Add(pd.PlayerId);
                Logger.Info($"自身为感染源", "pdd");
            }
        }

        if (ImmunityGone == true && SetImmunitytime.GetBool() || !SetImmunitytime.GetBool())
        {
            foreach (var pc in InfectList)
            {
                var infect = Utils.GetPlayerById(pc);
                foreach (var player in Main.AllAlivePlayerControls)
                {
                    var posi = infect.transform.position;
                    var diss = Vector2.Distance(posi, player.transform.position);
                    if (diss > 0.5f) continue;
                    if (InfectList.Count < 1) continue;
                    if (InfectList.Contains(player.PlayerId)) continue;
                    if (!InfectList.Contains(player.PlayerId) && !player.Is(CustomRoles.PlagueDoctor))
                    {
                        float timeThreshold = InfectTargetinfectcooldown.GetFloat();
                        int frameRate;
                        bool unlockFPS = Main.UnlockFPS.Value;
                        if (unlockFPS)
                        {
                            frameRate = 165;
                        }
                        else
                        {
                            frameRate = 60;
                        }
                        currentProgress = InfectInt[player.PlayerId];
                        float increasePerFrame = 100f / (timeThreshold * frameRate);
                        currentProgress += increasePerFrame;
                        InfectInt[player.PlayerId] = currentProgress;
                        if (InfectInt[player.PlayerId] >= 100f)
                        {
                            InfectList.Add(player.PlayerId);
                            Logger.Info($"成功感染", "pdd");
                            InfectNum += 1;

                            foreach (var pd in Main.AllPlayerControls)
                            {
                                if (pd.Is(CustomRoles.PlagueDoctor))
                                {
                                    pd.RpcGuardAndKill(pd);
                                    pd.RpcGuardAndKill(player);
                                }
                            }
                        }
                    }
                }
            }
        }

        List<PlayerControl> elementsToRemove = new();
        foreach (var infect in InfectList)
        {
            var infectplayer = Utils.GetPlayerById(infect);
            if (!infectplayer.IsAlive() && !infectplayer.Is(CustomRoles.PlagueDoctor))
            {
                elementsToRemove.Add(infectplayer);
                InfectNum -= 1;
            }
        }

        foreach (var element in elementsToRemove)
        {
            InfectList.Remove(element.PlayerId);
        }
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
    {
        if (!seer.Is(CustomRoles.PlagueDoctor)) return "";
        if (seer.PlayerId == target.PlayerId)
        {
            return "";
        }
        else
        {
            var getValue = InfectInt.TryGetValue(target.PlayerId, out var value);
            if (getValue && value >= 100f)
            {
                return Utils.ColorString(GetColor(value), Translator.GetString("Infected"));
            }
            else if (getValue && value >= 0f)
            {
                return Utils.ColorString(GetColor(value), $"【{value.ToString("F1")}%】");
            }
            else
            {
                return "";
            }
        }
    }
    private static Color32 GetColor(float Health)
    {
        int R = (int)((Health / 100f) * 255);
        int G = (int)(((100f - Health) / 100f) * 255);
        int B = 0;
        return new Color32((byte)R, (byte)G, (byte)B, byte.MaxValue);
    }
}