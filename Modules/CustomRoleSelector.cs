using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using TheOtherRoles_Host.Roles.Crewmate;
using TheOtherRoles_Host.Roles.Double;
using static UnityEngine.GraphicsBuffer;

namespace TheOtherRoles_Host.Modules;

internal class CustomRoleSelector
{
    public static Dictionary<PlayerControl, CustomRoles> RoleResult;
    public static IReadOnlyList<CustomRoles> AllRoles => RoleResult.Values.ToList();

    public static void SelectCustomRoles()
    {
        CheckMurderPatch.YLLevel = 0;
        CheckMurderPatch.YLdj = 1;
        CheckMurderPatch.YLCS = 0;
        // 开始职业抽取
        RoleResult = new();
        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Count();
        int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        int optNeutralNum = 0;
        int optNKNum = 0;
        int optHPNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        int optBtNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        //int Count = -1;
        if (Options.NeutralRolesMaxPlayer.GetInt() > 0 && Options.NeutralRolesMaxPlayer.GetInt() >= Options.NeutralRolesMinPlayer.GetInt())
            optNeutralNum = rd.Next(Options.NeutralRolesMinPlayer.GetInt(), Options.NeutralRolesMaxPlayer.GetInt() + 1);
        if (Options.NeutralKillersMaxPlayer.GetInt() > 0 && Options.NeutralKillersMaxPlayer.GetInt() >= Options.NeutralKillersMinPlayer.GetInt())
            optNKNum = rd.Next(Options.NeutralKillersMinPlayer.GetInt(), Options.NeutralKillersMaxPlayer.GetInt() + 1);

        int readyRoleNum = 0;
        int readyDeputyNum = 0;
        int readyMKNum = 0;
        int readyMANum = 0;
        int readyNeutralNum = 0;
        int readyNKNum = 0;
       

        List<CustomRoles> rolesToAssign = new();
        List<CustomRoles> potatosToAssign = new();

        List<CustomRoles> roleList = new();
        List<CustomRoles> potatoList = new();
        List<CustomRoles> roleOnList = new();
        List<CustomRoles> ImpOnList = new();
        List<CustomRoles> MiniOnList = new();
        List<CustomRoles> NeutralOnList = new();
        List<CustomRoles> NKOnList = new();
        List<CustomRoles> HPotatoOnList = new();
        List<CustomRoles> CPotatoOnList = new();

        List<CustomRoles> roleRateList = new();
        List<CustomRoles> ImpRateList = new();
        List<CustomRoles> MiniRateList = new();
        List<CustomRoles> NeutralRateList = new();
        List<CustomRoles> NKRateList = new();
        List<CustomRoles> PotatoRateList = new();
        List<CustomRoles> allPlayers = new();

        if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            RoleResult = new();
            foreach (var pc in Main.AllAlivePlayerControls)
                RoleResult.Add(pc, CustomRoles.KB_Normal);
            return;
        }
        if (Options.CurrentGameMode == CustomGameMode.HotPotato)
        {
            List<PlayerControl> HotPotatoList = new();

            RoleResult = new();

           for(int i=0;i<optHPNum;i++)
            {
                 var pcList = Main.AllAlivePlayerControls.Where(x => x.GetCustomRole() != CustomRoles.Hotpotato).ToList();
                var Ho = pcList[IRandom.Instance.Next(0, pcList.Count)];
                HotPotatoList.Add(Ho);
                RoleResult.Add(Ho, CustomRoles.Hotpotato);
            }  
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (HotPotatoList.Contains(pc)) continue;
                RoleResult.Add(pc, CustomRoles.Coldpotato);
            }
            return;
        }
        if (Options.CurrentGameMode == CustomGameMode.TheLivingDaylights)
        {
            List<PlayerControl> ButcherList = new();

            RoleResult = new();
            for (int i = 0; i < optBtNum; i++)
            {
                var pcList = Main.AllAlivePlayerControls.Where(x => x.GetCustomRole() != CustomRoles.Butcher).ToList();
                var Ho = pcList[IRandom.Instance.Next(0, pcList.Count)];
                ButcherList.Add(Ho);
                RoleResult.Add(Ho, CustomRoles.Butcher);
            }
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (ButcherList.Contains(pc)) continue;
                RoleResult.Add(pc, CustomRoles.Fugitives);
            }
            return;
        }

        foreach (var cr in Enum.GetValues(typeof(CustomRoles)))
        {
            CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (role.IsVanilla() || role.IsAdditionRole()) continue;
            if (role is CustomRoles.GM or CustomRoles.NotAssigned) continue;
            for (int i = 0; i < role.GetCount(); i++)
                roleList.Add(role);
        }
        // 职业设置为：优先
        foreach (var role in roleList) if (role.GetMode() == 2)
            {


                if (role.IsImpostorForSe()) ImpOnList.Add(role);
                else if (role.IsMini()) MiniOnList.Add(role);
                else if (!role.IsNKS() && role.IsNeutral()) NeutralOnList.Add(role);
                else if (role.IsNKS()) NKOnList.Add(role);
                else roleOnList.Add(role);
                Logger.Warn("职业设置为：优先", "2");

            }
        // 职业设置为：启用
        foreach (var role in roleList) if (role.GetMode() == 1)
            {
                
                if (role.IsImpostorForSe()) ImpRateList.Add(role);
                else if (role.IsMini()) MiniRateList.Add(role);
                else if (!role.IsNKS() && role.IsNeutral()) NeutralRateList.Add(role);
                else if (role.IsNKS()) NKRateList.Add(role);
                else roleRateList.Add(role);
                Logger.Warn("职业设置为：启用", "1");
            }



        //RPCによる同期
        foreach (var pair in Main.PlayerStates)
        {
            ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

            foreach (var subRole in pair.Value.SubRoles)
                ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
            Logger.Warn("职业设置为：附加", "1");
        }
        while (MiniOnList.Count == 1)
        {
            var select = MiniOnList[rd.Next(0, MiniOnList.Count)];
            MiniOnList.Remove(select);
            Mini.SetMiniTeam(Mini.EvilMiniSpawnChances.GetFloat());
            if (!Mini.IsEvilMini)
            {
                roleOnList.Add(CustomRoles.NiceMini);
            }
            if (Mini.IsEvilMini)
            {
                ImpOnList.Add(CustomRoles.EvilMini);
            }
        }
        while (MiniRateList.Count ==1)
        {
            var select = MiniRateList[rd.Next(0, MiniRateList.Count)];
            MiniRateList.Remove(select);
            Mini.SetMiniTeam(Mini.EvilMiniSpawnChances.GetFloat());
            if (!Mini.IsEvilMini)
            {
                roleRateList.Add(CustomRoles.NiceMini);
            }
            if (Mini.IsEvilMini)
            {
                ImpRateList.Add(CustomRoles.EvilMini);
            }
        }
        // 抽取优先职业（内鬼）
        while (ImpOnList.Count > 0)
        {
            var select = ImpOnList[rd.Next(0, ImpOnList.Count)];
            ImpOnList.Remove(select);
            rolesToAssign.Add(select);
            if (select != CustomRoles.Undercover && select != CustomRoles.Mimics && select != CustomRoles.AbandonedCrew)
            {
                readyRoleNum++;
                Logger.Info(select.ToString() + " 不是卧底，加入待选", "CustomRoleSelector");
            }
            else if (select == CustomRoles.Mimics && optImpNum >= 2 && playerCount >= 2)
            {
                rolesToAssign.Clear();
                while (readyMKNum < 1)
                {
                    rolesToAssign.Add(CustomRoles.MimicKiller);
                    readyMKNum++;
                }
                while (readyMANum < 1)
                {
                    rolesToAssign.Add(CustomRoles.MimicAss);
                    readyMANum++;
                }
                
                readyRoleNum = 2;
            }
            else if (select == CustomRoles.Mimics && (optImpNum < 2 || playerCount < 2))
            {
                rolesToAssign.Remove(select);
            }
            else if (select == CustomRoles.Undercover)
            {
                Logger.Info(select.ToString() + " 是卧底，不执行判断！", "CustomRoleSelector");
            }
            else if (select == CustomRoles.AbandonedCrew)
            {
                Logger.Info(select.ToString() + " 是被抛弃的船员，不执行判断！", "CustomRoleSelector");
            }
            

            //Logger.Info(select.ToString() + " 加入内鬼职业待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyRoleNum >= optImpNum) break;
        }
        // 优先职业不足以分配，开始分配启用的职业（内鬼）
        if (readyRoleNum < playerCount && readyRoleNum < optImpNum)
        {
            while (ImpRateList.Count > 0)
            {
                var select = ImpRateList[rd.Next(0, ImpRateList.Count)];
                ImpRateList.Remove(select);
                rolesToAssign.Add(select);
                if (select != CustomRoles.Undercover && select != CustomRoles.Mimics && select != CustomRoles.AbandonedCrew)
                {
                    readyRoleNum++;
                    Logger.Info(select.ToString() + " 不是卧底，加入待选", "CustomRoleSelector");
                }
                else if (select == CustomRoles.Mimics && optImpNum >= 2 && playerCount >= 2)
                {
                    rolesToAssign.Clear();
                    while (readyMKNum < 1)
                    {
                        rolesToAssign.Add(CustomRoles.MimicKiller);
                        readyMKNum++;
                    }
                    while (readyMANum < 1)
                    {
                        rolesToAssign.Add(CustomRoles.MimicAss);
                        readyMANum++;
                    }
                    readyRoleNum = 2;
                }
                else if (select == CustomRoles.Mimics && (optImpNum < 2 || playerCount < 2))
                {
                    rolesToAssign.Remove(select);
                }
                else if (select == CustomRoles.Undercover)
                {
                    Logger.Info(select.ToString() + " 是卧底，不执行判断！", "CustomRoleSelector");
                }
                else if (select == CustomRoles.AbandonedCrew)
                {
                    Logger.Info(select.ToString() + " 是被抛弃的船员，不执行判断！", "CustomRoleSelector");
                }
                

                //readyRoleNum++;
                //Logger.Info(select.ToString() + " 加入内鬼职业待选列表", "CustomRoleSelector");

                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyRoleNum >= optImpNum) break;
            }
        }
        // 抽取优先职业（中立杀手）
        while (NKOnList.Count > 0 && optNKNum > 0)
        {
            var select = NKOnList[rd.Next(0, NKOnList.Count)];
            NKOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            readyNKNum += select.GetCount();
            Logger.Info(select.ToString() + " 加入中立杀手待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyNKNum >= optNKNum) break;
        }
        // 优先职业不足以分配，开始分配启用的职业（中立杀手）
        if (readyRoleNum < playerCount && readyNKNum < optNKNum)
        {
            while (NKRateList.Count > 0 && optNKNum > 0)
            {
                var select = NKRateList[rd.Next(0, NKRateList.Count)];
                NKRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                readyNKNum += select.GetCount();
                Logger.Info(select.ToString() + " 加入中立杀手待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyNKNum >= optNKNum) break;
            }
        }
        // 抽取优先职业（中立）
        while (NeutralOnList.Count > 0 && optNeutralNum > 0)
        {
            var select = NeutralOnList[rd.Next(0, NeutralOnList.Count)];
            NeutralOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            readyNeutralNum += select.GetCount();
            Logger.Info(select.ToString() + " 加入中立职业待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyNeutralNum >= optNeutralNum) break;
        }
        // 优先职业不足以分配，开始分配启用的职业（中立）
        if (readyRoleNum < playerCount && readyNeutralNum < optNeutralNum)
        {
            while (NeutralRateList.Count > 0 && optNeutralNum > 0)
            {
                var select = NeutralRateList[rd.Next(0, NeutralRateList.Count)];
                NeutralRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                readyNeutralNum += select.GetCount();
                Logger.Info(select.ToString() + " 加入中立职业待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyNeutralNum >= optNeutralNum) break;
            }
        }
        if (Sheriff.HasDeputy.GetBool())
        {
            while (readyDeputyNum < 1)
            {
                rolesToAssign.Add(CustomRoles.Deputy);
                readyRoleNum++;
                readyDeputyNum++;
                if (readyDeputyNum >= 1) break;
                if (readyRoleNum >= playerCount) goto EndOfAssign;
            }
        }//*/
        // 抽取优先职业
        if (readyRoleNum < playerCount)
        {
            while (roleOnList.Count > 0)
            {
                var select = roleOnList[rd.Next(0, roleOnList.Count)];
                roleOnList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                Logger.Info(select.ToString() + " 加入船员职业待选列表（优先）", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
            }
        }
        // 优先职业不足以分配，开始分配启用的职业
        if (readyRoleNum < playerCount)
        {
            while (roleRateList.Count > 0)
            {
                var select = roleRateList[rd.Next(0, roleRateList.Count)];
                roleRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                Logger.Info(select.ToString() + " 加入船员职业待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
            }
        }

    // 职业抽取结束
    EndOfAssign:

        // 隐藏职业
        if (!Options.DisableHiddenRoles.GetBool())
        {
            if (rd.Next(0, 100) < 50 && rolesToAssign.Remove(CustomRoles.Jester)) rolesToAssign.Add(CustomRoles.Sunnyboy);
            if (rd.Next(0, 100) < 70 && rolesToAssign.Remove(CustomRoles.Sans)) rolesToAssign.Add(CustomRoles.Bard);
            if (rd.Next(0, 100) < 80 && rolesToAssign.Remove(CustomRoles.CyberStar)) rolesToAssign.Add(CustomRoles.LostCrew);
            if (rd.Next(0, 100) < Options.LuckeyProbability.GetInt() && rolesToAssign.Remove(CustomRoles.Luckey)) rolesToAssign.Add(CustomRoles.BadLuck);
        }
    /*
        // EAC封禁名单玩家开房将被起飞
        if (BanManager.CheckEACList(PlayerControl.LocalPlayer.FriendCode))
        {
            AmongUsClient.Instance.KickPlayer(PlayerControl.LocalPlayer.PlayerId, true);
            Logger.Info($"{PlayerControl.LocalPlayer.name}存在于EAC封禁名单", "BAN");
            return;
        }
    */

        // Dev Roles List Edit
        foreach (var dr in Main.DevRole)
        {
            if (dr.Key == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;
            if (rolesToAssign.Contains(dr.Value))
            {
                rolesToAssign.Remove(dr.Value);
                rolesToAssign.Insert(0, dr.Value);
                Logger.Info("职业列表提高优先：" + dr.Value, "Dev Role");
                continue;
            }
            for (int i = 0; i < rolesToAssign.Count; i++)
            {
                var role = rolesToAssign[i];
                if (dr.Value.GetMode() != role.GetMode()) continue;
                if (
                    (dr.Value.IsMini() && role.IsMini()) ||
                    (dr.Value.IsImpostor() && role.IsImpostor()) ||
                    (dr.Value.IsNeutral() && role.IsNeutral()) ||
                    (dr.Value.IsCrewmate() & role.IsCrewmate())
                    )
                {
                    rolesToAssign.RemoveAt(i);
                    rolesToAssign.Insert(0, dr.Value);
                    Logger.Info("覆盖职业列表：" + i + " " + role.ToString() + " => " + dr.Value, "Dev Role");
                    break;
                }
            }
        }

        var AllPlayer = Main.AllAlivePlayerControls.ToList();

        while (AllPlayer.Count() > 0 && rolesToAssign.Count > 0)
        {
            PlayerControl delPc = null;
            foreach (var pc in AllPlayer)
                foreach (var dr in Main.DevRole.Where(x => pc.PlayerId == x.Key))
                {
                    if (dr.Key == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;
                    var id = rolesToAssign.IndexOf(dr.Value);
                    if (id == -1) continue;
                    RoleResult.Add(pc, rolesToAssign[id]);
                    Logger.Info($"职业优先分配：{AllPlayer[0].GetRealName()} => {rolesToAssign[id]}", "CustomRoleSelector");
                    delPc = pc;
                    rolesToAssign.RemoveAt(id);
                    goto EndOfWhile;
                }

            var roleId = rd.Next(0, rolesToAssign.Count);
            RoleResult.Add(AllPlayer[0], rolesToAssign[roleId]);
            Logger.Info($"职业分配：{AllPlayer[0].GetRealName()} => {rolesToAssign[roleId]}", "CustomRoleSelector");
            AllPlayer.RemoveAt(0);
            rolesToAssign.RemoveAt(roleId);

        EndOfWhile:;
            if (delPc != null)
            {
                AllPlayer.Remove(delPc);
                Main.DevRole.Remove(delPc.PlayerId);
            }
        }

        if (AllPlayer.Count() > 0)
            Logger.Error("职业分配错误：存在未被分配职业的玩家", "CustomRoleSelector");
        if (rolesToAssign.Count > 0)
            Logger.Error("职业分配错误：存在未被分配的职业", "CustomRoleSelector");


    }

    public static int addScientistNum = 0;
    public static int addEngineerNum = 0;
    public static int addShapeshifterNum = 0;
    public static void CalculateVanillaRoleCount()
    {
        // 计算原版特殊职业数量
        addEngineerNum = 0;
        addScientistNum = 0;
        addShapeshifterNum = 0;
        foreach (var role in AllRoles)
        {
            switch (CustomRolesHelper.GetVNRole(role))
            {
                case CustomRoles.Scientist: addScientistNum++; break;
                case CustomRoles.Engineer: addEngineerNum++; break;
                case CustomRoles.Shapeshifter: addShapeshifterNum++; break;
            }
        }
    }

    public static List<CustomRoles> AddonRolesList = new();
    public static void SelectAddonRoles()
    {
        if (Options.CurrentGameMode == CustomGameMode.SoloKombat) return;

        if (Options.CurrentGameMode == CustomGameMode.HotPotato) return;

        if (Options.CurrentGameMode == CustomGameMode.TheLivingDaylights) return;
        AddonRolesList = new();
        foreach (var cr in Enum.GetValues(typeof(CustomRoles)))
        {
            CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (!role.IsAdditionRole()) continue;
            if (role is CustomRoles.Madmate && Options.MadmateSpawnMode.GetInt() != 0) continue;
            if (role is CustomRoles.Lovers or CustomRoles.LastImpostor or CustomRoles.Workhorse) continue;
            AddonRolesList.Add(role);
        }
    }
}
