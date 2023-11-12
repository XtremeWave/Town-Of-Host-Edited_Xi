using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheOtherRoles_Host.Roles.Neutral;
using TheOtherRoles_Host;
using static TheOtherRoles_Host.Options;
using MS.Internal.Xml.XPath;
using UnityEngine;
using static Il2CppSystem.Net.Http.Headers.Parser;
using static UnityEngine.GraphicsBuffer;
using Sentry.Internal;
using static TheOtherRoles_Host.Translator;


namespace TheOtherRoles_Host.Roles.Neutral;
public static class Henry
{
    private static readonly int Id = 167832056;
    public static List<byte> playerIdList = new();

    public static OptionItem HenryCanSee;
    public static OptionItem TargetKnowsYandere;
    public static OptionItem SkillCooldown;
    private static OptionItem ShapeshiftCooldown;
    public static OptionItem NeedChoose;
    public static int Choose = new();
    public static Dictionary<byte, int> ChooseMax = new();
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Henry);
        HenryCanSee = BooleanOptionItem.Create(Id + 14, "HenryCanSee", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Henry]);
        SkillCooldown = FloatOptionItem.Create(Id + 15, "KillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Henry])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 2, "ShapeshiftCooldown", new(1f, 999f, 1f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Henry])
            .SetValueFormat(OptionFormat.Seconds);
        NeedChoose = IntegerOptionItem.Create(Id + 9, "NeedChoose", new(1, 999, 1), 4, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Henry])
            .SetValueFormat(OptionFormat.Players);
    }
    public static void Init()
    {
        playerIdList = new();
        ChooseMax = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        var Dy = IRandom.Instance;
        int rndNum = Dy.Next(0, 3);
        Choose = rndNum;
        ChooseMax.TryAdd(playerId, NeedChoose.GetInt());

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        ChooseMax.Remove(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Remove(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1;
    }
    public static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetHenrySellLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(ChooseMax[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (ChooseMax.ContainsKey(PlayerId))
            ChooseMax[PlayerId] = Limit;
        else
            ChooseMax.Add(PlayerId, NeedChoose.GetInt());
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = SkillCooldown.GetFloat();
    //显示名字前的技能剩余量awa
    public static string GetHenryLimit(byte playerId) => Utils.ColorString((ChooseMax.TryGetValue(playerId, out var x) && x >= 1) ? Color.white : Color.gray, ChooseMax.TryGetValue(playerId, out var chooseMax) ? $"({chooseMax})" : "Invalid");
    public static bool OnCheckMurder(PlayerControl killer)
    {
        if (ChooseMax[killer.PlayerId] <= 0)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Henry);
            CustomWinnerHolder.WinnerIds.Add(killer.PlayerId);
        }
        if (Choose == 0)
        {
            killer.ResetKillCooldown();
            NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("HenryYES!")));
            ChooseMax[killer.PlayerId]--;
            killer.RpcGuardAndKill(killer);
            SendRPC(killer.PlayerId);
            var Dy = IRandom.Instance;
            int rndNum = Dy.Next(0, 4);
            Choose = rndNum;
            ChooseMax.TryAdd(killer.PlayerId, NeedChoose.GetInt());
            return true;
        }
        else
        {
            NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "FALL"));
            NameNotifyManager.Notify(killer, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "NotKiller"));
            killer.RpcMurderPlayerV3(killer);
            return false;
        }        
    }
    public static void OnShapeshift(PlayerControl pc)
    {
        if (pc == null || !pc.Is(CustomRoles.Henry)) return;
        if (ChooseMax[pc.PlayerId] <= 0)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Henry);
            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
        }
        if (Choose == 1)
        {
            NameNotifyManager.Notify(pc, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("HenryYES!")));
            ChooseMax[pc.PlayerId]--;
            SendRPC(pc.PlayerId);
            pc.RpcGuardAndKill(pc);
            var Dy = IRandom.Instance;
            int rndNum = Dy.Next(0, 4);
            Choose = rndNum;
            ChooseMax.TryAdd(pc.PlayerId, NeedChoose.GetInt());
        }
        else
        {
            new LateTask(() =>
              {
                  NameNotifyManager.Notify(pc, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "FALL"));
                  NameNotifyManager.Notify(pc, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "NotShapeshift"));
                  pc.RpcMurderPlayerV3(pc);
                  Utils.NotifyRoles();
              }, 1.5f, ("LOST!!!!"));
        }
    }
    public static void OnEnterVent(PlayerControl pc)
    {
        if (pc == null || !pc.Is(CustomRoles.Henry)) return;
        if (ChooseMax[pc.PlayerId] <= 0)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Henry);
            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
        }
        if (Choose == 2)
        {
            NameNotifyManager.Notify(pc, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("HenryYES!")));
            ChooseMax[pc.PlayerId]--;
            SendRPC(pc.PlayerId);
            pc.RpcGuardAndKill(pc);
            var Dy = IRandom.Instance;
            int rndNum = Dy.Next(0, 4);
            Choose = rndNum;
            ChooseMax.TryAdd(pc.PlayerId, NeedChoose.GetInt());
        }
        else
        {
            if (pc == null || !pc.Is(CustomRoles.Henry)) return;
            new LateTask(() =>
            {
                NameNotifyManager.Notify(pc, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("FALL")));
                NameNotifyManager.Notify(pc, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("NotVent")));
                pc.RpcMurderPlayerV3(pc);
                Utils.NotifyRoles();
            }, 1.5f, ("亨利自杀"));
        }
    }
}
