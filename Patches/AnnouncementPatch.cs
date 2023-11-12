using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using AmongUs.Data.Player;
using Assets.InnerNet;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Reflection;

namespace TheOtherRoles_Host;

// ##https://github.com/Yumenopai/TownOfHost_Y
//来源：TOHY（谢谢！）
public class ModNewsSCn
{
    public int Number;
    public int BeforeNumber;
    public string Title;
    public string SubTitle;
    public string ShortTitle;
    public string Text;
    public string Date;
    public uint Language;
   public Announcement ToAnnouncement()
    {
        var result = new Announcement
        {
            Number = Number,

            Title = Title,
            SubTitle = SubTitle,
            ShortTitle = ShortTitle,
            Text = Text,
            Language = /*(uint)SupportedLangs.SChinese == */(uint)DataManager.Settings.Language.CurrentLanguage, 
            Date = Date,
            Id = "ModNews"
        };

        return result;
    }
}
[HarmonyPatch]
public class ModNewsHistorySCn
{
    public static List<ModNewsSCn> AllModNews = new();
    public static void Init()
    {
        //当你创建新的公告时，你不能删除旧的公告
        /*
        *每当你创建新公告时，请按照以下格式：
        {
        var news = new ModNews
            {
            Number = xxx,         //（编号）
            Title = "",           //（标题）
            SubTitle = "",        //（副标题）
            ShortTitle = "",      //（短标题）
            Test = ""            //（文本）
            + "\n"        
            ......
            + "\n",
        *注意，文本在最后一个+""以后添加逗号    
            Date = "",            //（日期）
            };
        AllModNews.Add(news);
        }
        *别忘了标点！！！！！
        */
        {
            var news = new ModNewsSCn
            {
                Number = 100010,
                Title = "TownOfHostEdited-Xi v2.1.0",
                SubTitle = "★★★★Bug修复★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.1.0★\n<size=75%>简体中文</size>",
                Text =
                "简体中文"
                + "\n-----------------------------"
                + "\n<size=125%>欢迎来到TheOtherRoles_Host,感谢您的游玩</size>"
                + "\n-----------------------------"
                + "\n终于解决了!"
                + "\n\n\n### 对应官方版本"
                + "\n- 基于TOH版本 v4.1.2"
                + "\n- 基于TOHE版本 v2.3.55"
                + "\n- 适配Among Us版本 v2023.10.24"
                + "\n\n### 修复"
                + "\n- 修复了击杀检查失效的问题"
                + "\n- 修复了原版职业工程师为模型的职业不可使用通风口的问题"
                + "\n- 修复了迷你船员长大不显示的问题",
                Date = "2023-11-09T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {
            var news = new ModNewsSCn
            {
                Number = 100009,
                Title = "TownOfHostEdited-Xi v2.1.0",
                SubTitle = "★★★★Bugs Fix★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.1.0★\n<size=75%>English</size>",
                Text =
                "English"
                + "\n-----------------------------"
                 + "\n<size=125%>Welcome To TheOtherRoles_Host,Thank ya For Playing!</size>"
                + "\n-----------------------------"
                + "\nOOOOOOOKKKKKAAAAYYYYY!"
               + "\n\n\n### Support Among Us Version"                + "\n- Based On TOH v4.1.2"                + "\n- Based On TOHE v2.3.55"                + "\n- Support Among Us v2023.10.24"                + "\n\n### Bugs Fix"
                + "\n- Check Murder Lose Efficacy Bug Fix"
                + "\n- Engineer Model's Role Can't Vent Bug Fix"
                + "\n- When Mini Grows Up Didn't Displayed In Mod Client Bug Fix",

                Date = "2023-11-09T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {

            var news = new ModNewsSCn
            {
                Number = 100008,
                Title = "TownOfHostEdited-Xi v2.0.5",
                SubTitle = "★★★★Bug修复★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.0.5★\n<size=75%>简体中文</size>",
                Text =
                "简体中文"
                + "\n-----------------------------"
                + "\n<size=125%>欢迎来到TheOtherRoles_Host,感谢您的游玩</size>"
                + "\n-----------------------------"
                + "\n终于解决了!"
                + "\n\n\n### 对应官方版本"
                + "\n- 基于TOH版本 v4.1.2"
                + "\n- 基于TOHE版本 v2.3.55"
                + "\n- 适配Among Us版本 v2023.10.24"
                + "\n\n### 修复"
                + "\n- 修复了击杀检查失效的问题"
                + "\n- 修复了原版职业工程师为模型的职业不可使用通风口的问题"
                + "\n- 修复了迷你船员长大不显示的问题",
                Date = "2023-11-09T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {
            var news = new ModNewsSCn
            {
                Number = 100007,
                Title = "TownOfHostEdited-Xi v2.0.5",
                SubTitle = "★★★★Bugs Fix★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.0.5★\n<size=75%>English</size>",
                Text =
                "English"
                + "\n-----------------------------"
                 + "\n<size=125%>Welcome To TheOtherRoles_Host,Thank ya For Playing!</size>"
                + "\n-----------------------------"
                + "\nOOOOOOOKKKKKAAAAYYYYY!"
               + "\n\n\n### Support Among Us Version"                + "\n- Based On TOH v4.1.2"                + "\n- Based On TOHE v2.3.55"                + "\n- Support Among Us v2023.10.24"                + "\n\n### Bugs Fix"
                + "\n- Check Murder Lose Efficacy Bug Fix"
                + "\n- Engineer Model's Role Can't Vent Bug Fix"
                + "\n- When Mini Grows Up Didn't Displayed In Mod Client Bug Fix",
                 
                Date = "2023-11-09T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {
            var news = new ModNewsSCn
            {
                Number = 100006,
                Title = "TownOfHostEdited-Xi v2.0.4",
                SubTitle = "★★★★真菌世界！★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.0.4★\n<size=75%>简体中文</size>",
                Text =
                "简体中文"
                + "\n-----------------------------"
                + "\n<size=125%>欢迎来到TheOtherRoles_Host,感谢您的游玩</size>"
                + "\n-----------------------------"
                + "\n喜是歌姬真菌!"
                + "\n\n\n## 对应官方版本"
                + "\n- 基于TOH版本 v4.1.2"
                + "\n- 基于TOHE版本 v2.3.55"
                + "\n- 适配Among Us版本 v2023.10.24"
                + "\n\n## 修复"
                + "\n- 修复了猎人可以击杀的问题"
                + "\n- 修复了魅魔无法选择备胎的问题"
                 + "\n- 修复了丘比特开局死亡的问题"
                           + "\n- 修复了热土豆的问题"
                + "\n\n## 新增职业"
                +"\n- 内鬼阵营：模仿者团队"
                + "\n- 内鬼阵营：化形者"
                         + "\n- 船员阵营：磁铁人"
                + "\n\n## 更改"
                 + "\n- 梦魇在未关灯的情况下可以给人施加停电buff"
                  + "\n- 诱饵可以看到视野范围里的管道里有没有人"
                 + "\n- 暂时删除音效",
                Date = "2023-10-29T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {
            var news = new ModNewsSCn
            {
                Number = 100005,
                Title = "TownOfHostEdited-Xi v2.0.4",
                SubTitle = "★★★★Fungle World!★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.0.4★\n<size=75%>English</size>",
                Text =
                "English"
                + "\n-----------------------------"
                 + "\n<size=125%>Welcome To TheOtherRoles_Host,Thank ya For Playing!</size>"
                + "\n-----------------------------"
                + "\nNew Map!"
               + "\n\n\n## Support Among Us Version"                + "\n- Based On TOH v4.1.2"                + "\n- Based On TOHE v2.3.55"                + "\n- Support Among Us v2023.10.24"                + "\n\n## Bugs Fix"
                + "\n- Hunter Can Kill Bug Fix"
                + "\n- Akujo Can't Keep Bug Fix"
                 + "\n- Cupid Die When Game Start Bug Fix"
                 + "\n\n## New Roles"
                 + "\n- Impostor:Mimics"
                 + "\n- Impostor:Insteader"
                 + "\n- Crewmate:Magneter"
                 + "\n\n## Changes" 
                 + "\n- Mare has the ability to temporarily blind others"
                 + "\n- Temporarily Remove Sounds"
                 + "\n- Bait can see player in vent"
                 + "\n-Support English",
                Date = "2023-10-29T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {
            var news = new ModNewsSCn
            {
                Number = 100004,
                Title = "TownOfHostEdited-Xi v2.0.3",
                SubTitle = "★★★★公开回归！★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.0.3★\n<size=75%>简体中文</size>",
                Text =
                "简体中文"
                + "\n-----------------------------"
                + "\n<size=125%>欢迎来到TheOtherRoles_Host,感谢您的游玩</size>"
                + "\n-----------------------------"
                + "\n再次公开!"
                + "\n\n\n## 对应官方版本"
                + "\n- 基于TOH版本 v4.1.2"
                + "\n- 基于TOHE版本 v2.3.55"
                + "\n- 适配Among Us版本 v2023.7.11及以上版本"
                + "\n\n## 修复"
                + "\n- 修复大量bug"
                + "\n- 可以公开"
                + "\n\n## 新增职业"
                + "\n- 船员阵营：管道工"
                + "\n\n## 写在最后"
                + "\n芜湖"
                + "\n                                                       ——喜",
                Date = "2023-8-25T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {
            var news = new ModNewsSCn
            {
                Number = 100003,
                Title = "TownOfHostEdited-Xi v2.0.3",
                SubTitle = "★★★★Public Again!★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.0.3★\n<size=75%>English</size>",
                Text =
                "English"
                + "\n-----------------------------"
                 + "\n<size=125%>Welcome To TheOtherRoles_Host,Thank ya For Playing!</size>"
                + "\n-----------------------------"
                + "\nLet's Play Public Games!"
               + "\n\n\n## Support Among Us Version"                + "\n- Based On TOH v4.1.2"                + "\n- Based On TOHE v2.3.55"                + "\n- Support Among Us v2023.7.11 And Above"                + "\n\n## Bugs Fix"
                + "\n- Lots of Bugs fixed"
                         + "\n- We Can PUBLIC Now"
                + "\n\n## New Roles"
                     + "\n- Crewmates：Plumber",
                Date = "2023-8-25T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {
            var news = new ModNewsSCn
            {
                Number = 100002,
                Title = "TownOfHostEdited-Xi v2.0.2",
                SubTitle = "★★★★全新的TheOtherRoles_Host！★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.0.2★\n<size=75%>简体中文</size>",
                Text = 
                "简体中文"
                + "\n-----------------------------"
                + "\n<size=125%>欢迎来到TheOtherRoles_Host,感谢您的游玩</size>"
                + "\n-----------------------------"
                + "\n全新的TheOtherRoles_Host!"
                + "\n\n\n## 对应官方版本"
                + "\n- 基于TOH版本 v4.1.2"
                + "\n- 基于TOHE版本 v2.3.55"
                + "\n- 适配Among Us版本 v2023.7.11及以上版本"
                + "\n\n## 修复"
                + "\n- 修复亨利等名字提示无法显示问题"
                + "\n\n## 新增、回归职业"
                     + "\n- 中立阵营：抗拒者"
                          + "\n- 内鬼阵营：击球手"
                               + "\n- 内鬼阵营：寻血者"
                                    + "\n- 内鬼阵营：吸魂者"
                                     + "\n- 内鬼阵营：吊死鬼"
                + "\n\n## 写在最后"
                + "\n快国庆了，但是树懒也快换引擎了，不想做了"
                + "\n                                                       ——喜",
                Date = "2023-8-25T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {
            var news = new ModNewsSCn
            {
                Number = 100001,
                Title = "TownOfHostEdited-Xi v2.0.0",
                SubTitle = "★★★★全新的TheOtherRoles_Host！★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.0.0★\n<size=75%>简体中文</size>",
                Text =
                "简体中文"
                + "\n-----------------------------"
                + "\n<size=125%>欢迎来到TheOtherRoles_Host,感谢您的游玩</size>"
                + "\n-----------------------------"
                + "\n全新的TheOtherRoles_Host!"
                + "\n\n\n## 对应官方版本"
                + "\n- 基于TOH版本 v4.1.2"
                + "\n- 基于TOHE版本 v2.3.55"
                + "\n- 适配Among Us版本 v2023.7.11及以上版本"
                + "\n\n## 修复"
                + "\n- 修复了坏迷你船员长大时会重置CD的问题"
                + "\n- 修复了跟班不上位的问题"
                + "\n- 修复了豺狼只能传一代的问题"
                + "\n- 修复了赌薛定谔的猫赌怪自杀的问题"
                + "\n- 修复了薛定谔的猫被刀不显示颜色的问题"
                + "\n- 修复了部分职业技能图标显示问题"
                + "\n- 修复了法官导致游戏崩溃的问题"
                + "\n- 修复了自由人投票有效的问题"
                + "\n- 修复了游戏开始黑屏的问题"
                + "\n- 修复了非中文设置界面崩溃的问题"
                + "\n\n## 新增、回归职业"
                //  + "\n- 内鬼阵营：模仿者(杀手)(来自TOH-TOR)"
                //+ "\n- 内鬼阵营：模仿者(助手)"
                + "\n- 内鬼阵营：分散者(来自TOH-RE)"
                + "\n- 内鬼阵营：套皮者"
                + "\n- 内鬼阵营：美杜莎"
                + "\n- 内鬼阵营：伪造师"
                + "\n- 内鬼阵营：集束者"
                + "\n- 内鬼阵营：勒索者"
                + "\n- 内鬼阵营：邪恶的换票师"
                + "\n- 中立阵营：病娇"
                + "\n- 中立阵营：病危者"
                + "\n- 中立阵营：疫医"
                + "\n- 中立阵营：亨利"
                + "\n- 中立阵营：伪人"
                + "\n- 中立阵营：悬赏官"
                + "\n- 中立阵营：孤独者"
                + "\n- 中立阵营：绝望先生"
                + "\n- 中立阵营：魅魔"
                + "\n- 中立阵营：薛定谔的猫(重制)"
                + "\n- 船员阵营：正义的追踪者"
                + "\n- 船员阵营：汤姆"
                + "\n- 船员阵营：商人"
                + "\n- 船员阵营：效颦者(试验性)"
                + "\n- 船员阵营：埋雷兵"
                + "\n- 船员阵营：正义的换票师"
                + "\n- 船员阵营：时停者(回归)"
                + "\n- 船员阵营：医生(来自TOH-RE)"
                + "\n- 船员阵营：研究员(来自TOH-RE)"
                + "\n- 附加职业：调色师(来自TOH_Y)"
                + "\n\n## 新增功能"
                + "\n- 游戏设置：职业指令技能混淆"
                + "\n- 豺狼设置：跟班可以上位"
                + "\n- 豺狼设置：跟班可以击杀"
                + "\n- 豺狼设置：跟班击杀冷却时间"
                + "\n- 豺狼设置：跟班可以使用管道"
                + "\n- 丘比特设置：丘比特可以给恋人护盾"
                + "\n- 迷你船员设置：实时更新年龄(试验性)"
                + "\n\n## 更改"
                + "\n- 删除 混淆正义的赌怪指令"
                + "\n- 删除 混淆邪恶的赌怪指令"
                + "\n- 删除 混淆法官指令"
                + "\n- 原本的魅魔更名为魅惑者"
                + "\n- 守护者更名为骑士"
                + "\n- 操控者更名为电报员"
                + "\n- 专业刺客现在只能是邪恶的赌怪"
                + "\n- 修改部分职业颜色"
                + "\n- 全新的开场动画阵营文字"
                + "\n- 增加开场动画补充文字"
                + "\n- 补充部分职业技能图标"

                + "\n- 开场动画背景颜色跟随职业颜色"
                + "\n- 全新的胜利文本(简体中文)"
                + "\n\n## 写在最后"
                + "\n事实上TheOtherRoles_Host已经凉了有一阵子了，"
                + "\n没人来测试，但有人来各种诋毁、甚至辱骂开发者"
                + "\n咔皮呆说的对，做模组真的很累"
                + "\n但愿200版本能让玩家回流一些吧"
                + "\n看到这里，我们要说一声谢谢，是你们给了我们开发的动力，"
                + "\n祝您游玩愉快！"
                + "\n                                                       ——TheOtherRoles_Host开发组",
                Date = "2023-8-25T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {
            var news = new ModNewsSCn
            {
                Number = 100000,
                Title = "TownOfHostEdited-Xi v2.0.0",
                SubTitle = "★★★★NEW TheOtherRoles_Host!★★★★",
                ShortTitle = "★TheOtherRoles_Host v2.0.0★\n<size=75%>English</size>",
                Text =
                "English"                + "\n-----------------------------"                + "\n<size=125%>Welcome To TheOtherRoles_Host,Thank ya For Playing!</size>"                + "\n-----------------------------"                + "\nNew TheOtherRoles_Host!"                + "\n\n\n## Support Among Us Version"                + "\n- Based On TOH v4.1.2"                + "\n- Based On TOHE v2.3.55"                + "\n- Support Among Us v2023.7.11 And Above"                + "\n\n## Bugs Fix"                + "\n- When Evil Mini Grows Up Reset Kill Cooldown Bug Fix"                + "\n- Sidekick Can't Become Jackal Bug Fix"                + "\n\n We're so sorry about we haven't completed English Translate yet and brings you terrible experience, we have no time"
                + "\n if you wanna help us, please PR your trans in Github, Thanks For your support!",
                Date = "2023-8-15T00:00:00Z",
            };
            AllModNews.Add(news);
        }
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
    {
        if (AllModNews.Count < 1)
        {
             Init();
            AllModNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });
        }

        List<Announcement> FinalAllNews = new();
        AllModNews.Do(n => FinalAllNews.Add(n.ToAnnouncement()));
        foreach (var news in aRange)
        {
            if (!AllModNews.Any(x => x.Number == news.Number))
                FinalAllNews.Add(news);
        }
        FinalAllNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });
        aRange = new(FinalAllNews.Count);
        for (int i = 0; i < FinalAllNews.Count; i++)
            aRange[i] = FinalAllNews[i];

        return true;
    }
}