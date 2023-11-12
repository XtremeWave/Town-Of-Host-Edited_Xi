using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;
using MS.Internal.Xml.XPath;
using Sentry.Internal.Extensions;
using System.Linq;
using System.Text;
using TheOtherRoles_Host.Roles.Crewmate;
using TheOtherRoles_Host.Roles.Impostor;
using TheOtherRoles_Host.Roles.Neutral;
using static TheOtherRoles_Host.Translator;
using Hazel;
using InnerNet;
using System.Threading.Tasks;
using TheOtherRoles_Host.Modules;
using TheOtherRoles_Host.Roles.AddOns.Crewmate;
using UnityEngine.Profiling;
using System.Runtime.Intrinsics.X86;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UI;
using UnityEngine.Networking.Types;

namespace TheOtherRoles_Host;
[HarmonyPatch(typeof(MainMenuManager))]
//参考TO-HOPE（N让我搬过来））））https://gitee.com/xigua_ya/to-hope
public class MainAN
{   
    [HarmonyPatch(nameof(MainMenuManager.Start))]
    [HarmonyPrefix]
    static void LoadButtons(MainMenuManager __instance)
    {
        Buttons.Clear();
        var template = __instance.creditsButton;
    
        if (!template) return;
        // 示例，创建一个名为Gitee的按钮，点击后打开https://gitee.com/xigua_ya/TheOtherRoles_Host
        CreateButton(__instance, template, GameObject.Find("RightPanel")?.transform, new(0.2f, 0.1f), "Gitee", () => { Application.OpenURL("https://gitee.com/xigua_ya/TheOtherRoles_Host"); }, new Color32(255, 151, 0,byte.MaxValue));
        CreateButton(__instance, template, GameObject.Find("RightPanel")?.transform, new(0.4f, 0.1f), "Github", () => { Application.OpenURL("https://github.com/TheOtherRoles_Host-Official/TownOfHostEdited-Xi"); }, new Color32(0, 0, 0, byte.MaxValue));
        CreateButton(__instance, template, GameObject.Find("RightPanel")?.transform, new(0.6f, 0.1f), "Discord", () => { Application.OpenURL("https://discord.gg/jQbX7aZSKb"); }, new Color32(0, 8, 255, byte.MaxValue));
        CreateButton(__instance, template, GameObject.Find("RightPanel")?.transform, new(0.2f, 0.2f), "QQ", () => { Application.OpenURL("http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=92p_Sv_eLa544FWS83251lPQxpok_i2s&authKey=e918u6eWXT9x2kVo88PPMdEIzg3wZARl0duYhLke9DKhLwujwsmcTKpovM8X01l%2B&noverify=0&group_code=704560281"); }, new Color32(0, 255, 247, byte.MaxValue));
    }
    
    private static readonly List<PassiveButton> Buttons = new();
    /// <summary>
    /// 在主界面创建一个按钮
    /// </summary>
    /// <param name="__instance">MainMenuManager 的实例</param>
    /// <param name="template">按钮模板</param>
    /// <param name="parent">父游戏物体</param>
    /// <param name="anchorPoint">与父游戏物体的相对位置</param>
    /// <param name="text">按钮文本</param>
    /// <param name="action">点击按钮的动作</param>
    /// <returns>返回这个按钮</returns>
    static void CreateButton(MainMenuManager __instance, PassiveButton template, Transform? parent, Vector2 anchorPoint, string text, Action action,Color color)
    {
        if (!parent) return;

        var button = UnityEngine.Object.Instantiate(template, parent);
        button.GetComponent<AspectPosition>().anchorPoint = anchorPoint;
        SpriteRenderer buttonSprite = button.transform.FindChild("Inactive").GetComponent<SpriteRenderer>();
        buttonSprite.color = color;
        __instance.StartCoroutine(Effects.Lerp(0.5f, new Action<float>((p) => {
            button.GetComponentInChildren<TMPro.TMP_Text>().SetText(text);
        })));
        
        button.OnClick = new();
        button.OnClick.AddListener(action);

        Buttons.Add(button);
    }

    [HarmonyPatch(nameof(MainMenuManager.OpenAccountMenu))]
    [HarmonyPatch(nameof(MainMenuManager.OpenCredits))]
    [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu))]
    [HarmonyPostfix]
    static void Hide()
    {
        foreach (var btn in Buttons) btn.gameObject.SetActive(false);
    }
    [HarmonyPatch(nameof(MainMenuManager.ResetScreen))]
    [HarmonyPostfix]
    static void Show()
    {
        foreach (var btn in Buttons)
        {
            if (btn == null || btn.gameObject == null) continue;
            btn.gameObject.SetActive(true);
        }
    }
}