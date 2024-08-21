using TMPro;
using System;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using static TOHEXI.Translator;
using static TOHEXI.Credentials;

namespace TOHEXI;

[HarmonyPatch(typeof(MainMenuManager))]
public static class MainMenuManagerPatch
{
    private static PassiveButton template;
    private static PassiveButton gitHubButton;
    private static PassiveButton discordButton;
    private static PassiveButton websiteButton;
    //private static PassiveButton patreonButton;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
    public static void Postfix(MainMenuManager __instance)
    {
        if (__instance == null) return;
        __instance.playButton.transform.gameObject.SetActive(Options.IsLoaded);
        if (TitleLogoPatch.LoadingHint != null)
            TitleLogoPatch.LoadingHint.SetActive(!Options.IsLoaded);
    }

    [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
    public static void StartPostfix(MainMenuManager __instance)
    {
        if (template == null) template = __instance.quitButton;

        // FPS
        Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;

        __instance.screenTint.gameObject.transform.localPosition += new Vector3(1000f, 0f);
        __instance.screenTint.enabled = false;
        __instance.rightPanelMask.SetActive(true);
        // The background texture (large sprite asset)
        __instance.mainMenuUI.FindChild<SpriteRenderer>("BackgroundTexture").transform.gameObject.SetActive(false);
        // The glint on the Among Us Menu
        __instance.mainMenuUI.FindChild<SpriteRenderer>("WindowShine").transform.gameObject.SetActive(false);
        __instance.mainMenuUI.FindChild<Transform>("ScreenCover").gameObject.SetActive(false);

        GameObject leftPanel = __instance.mainMenuUI.FindChild<Transform>("LeftPanel").gameObject;
        GameObject rightPanel = __instance.mainMenuUI.FindChild<Transform>("RightPanel").gameObject;
        rightPanel.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        GameObject maskedBlackScreen = rightPanel.FindChild<Transform>("MaskedBlackScreen").gameObject;
        maskedBlackScreen.GetComponent<SpriteRenderer>().enabled = false;
        //maskedBlackScreen.transform.localPosition = new Vector3(-3.345f, -2.05f); //= new Vector3(0f, 0f);
        maskedBlackScreen.transform.localScale = new Vector3(7.35f, 4.5f, 4f);

        __instance.mainMenuUI.gameObject.transform.position += new Vector3(-0.2f, 0f);

        leftPanel.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        leftPanel.gameObject.FindChild<SpriteRenderer>("Divider").enabled = false;
        leftPanel.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name == "Shine").ForEach(r => r.enabled = false);

        GameObject splashArt = new("SplashArt");
        splashArt.transform.position = new Vector3(0, 0f, 600f); //= new Vector3(0, 0.40f, 600f);
        var spriteRenderer = splashArt.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Utils.LoadSprite($"TOHEXI.Resources.Images.TOHEXII-BG.jpg", 89f);


        //__instance.playLocalButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.1647f, 0f, 0.7765f);
        //__instance.PlayOnlineButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.1647f, 0f, 0.7765f);
        //__instance.playLocalButton.transform.position = new Vector3(2.095f, -0.25f, 520f);
        //__instance.PlayOnlineButton.transform.position = new Vector3(0f, -0.25f, 0f);


        __instance.playButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 178, 0, 127);
        __instance.playButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color32(252, 255, 140, 127);
        __instance.playButton.activeTextColor = Color.white;
        __instance.playButton.inactiveTextColor = Color.white;
        __instance.playButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 178, 0, 127);

        __instance.inventoryButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 178, 0, 127);
        __instance.inventoryButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color32(252, 255, 140, 127);
        __instance.inventoryButton.activeTextColor = Color.white;
        __instance.inventoryButton.inactiveTextColor = Color.white;
        __instance.inventoryButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 178, 0, 127);

        __instance.shopButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 178, 0, 127);
        __instance.shopButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color32(252, 255, 140, 127);
        __instance.shopButton.activeTextColor = Color.white;
        __instance.shopButton.inactiveTextColor = Color.white;
        __instance.shopButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 178, 0, 127);



        __instance.newsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 98, 153);
        __instance.newsButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 200, 153);
        __instance.newsButton.activeTextColor = Color.white;
        __instance.newsButton.inactiveTextColor = Color.white;

        __instance.myAccountButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 98, 153); ;
        __instance.myAccountButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 200, 153);
        __instance.myAccountButton.activeTextColor = Color.white;
        __instance.myAccountButton.inactiveTextColor = Color.white;
        __instance.accountButtons.transform.position += new Vector3(0f, 0f, -1f);

        __instance.settingsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 98, 153);
        __instance.settingsButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 200, 153);
        __instance.settingsButton.activeTextColor = Color.white;
        __instance.settingsButton.inactiveTextColor = Color.white;



        //__instance.creditsButton.gameObject.SetActive(false);
        //__instance.quitButton.gameObject.SetActive(false);

        __instance.quitButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 98, 153);
        __instance.quitButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 200, 153);

        __instance.quitButton.activeTextColor = Color.white;
        __instance.quitButton.inactiveTextColor = Color.white;

        __instance.creditsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 98, 153);
        __instance.creditsButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 200, 153);
        __instance.creditsButton.activeTextColor = Color.white;
        __instance.creditsButton.inactiveTextColor = Color.white;



        if (template == null) return;

    }

    private static PassiveButton CreateButton(string name, Vector3 localPosition, Color32 normalColor, Color32 hoverColor, Action action, string label, Vector2? scale = null)
    {
        var button = Object.Instantiate(template, Credentials.ToheLogo.transform);
        button.name = name;
        Object.Destroy(button.GetComponent<AspectPosition>());
        button.transform.localPosition = localPosition;

        button.OnClick = new();
        button.OnClick.AddListener(action);

        var buttonText = button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMP_Text>();
        buttonText.DestroyTranslator();
        buttonText.fontSize = buttonText.fontSizeMax = buttonText.fontSizeMin = 3.5f;
        buttonText.enableWordWrapping = false;
        buttonText.text = label;
        var normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();
        var hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();
        normalSprite.color = normalColor;
        hoverSprite.color = hoverColor;

        var container = buttonText.transform.parent;
        Object.Destroy(container.GetComponent<AspectPosition>());
        Object.Destroy(buttonText.GetComponent<AspectPosition>());
        container.SetLocalX(0f);
        buttonText.transform.SetLocalX(0f);
        buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;

        var buttonCollider = button.GetComponent<BoxCollider2D>();
        if (scale.HasValue)
        {
            normalSprite.size = hoverSprite.size = buttonCollider.size = scale.Value;
        }

        buttonCollider.offset = new(0f, 0f);

        return button;
    }
    public static void Modify(this PassiveButton passiveButton, Action action)
    {
        if (passiveButton == null) return;
        passiveButton.OnClick = new Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener(action);
    }
    public static T FindChild<T>(this MonoBehaviour obj, string name) where T : Object
    {
        string name2 = name;
        return obj.GetComponentsInChildren<T>().First((T c) => c.name == name2);
    }
    public static T FindChild<T>(this GameObject obj, string name) where T : Object
    {
        string name2 = name;
        return obj.GetComponentsInChildren<T>().First((T c) => c.name == name2);
    }
    public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
    {
        //if (source == null) throw new ArgumentNullException("source");
        if (source == null) throw new ArgumentNullException(nameof(source));

        IEnumerator<TSource> enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            action(enumerator.Current);
        }

        enumerator.Dispose();
    }

    [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu))]
    [HarmonyPatch(nameof(MainMenuManager.OpenAccountMenu))]
    [HarmonyPatch(nameof(MainMenuManager.OpenCredits))]
    [HarmonyPostfix]
    public static void OpenMenuPostfix()
    {
        if (Credentials.ToheLogo != null) Credentials.ToheLogo.gameObject.SetActive(false);
    }
    [HarmonyPatch(nameof(MainMenuManager.ResetScreen)), HarmonyPostfix]
    public static void ResetScreenPostfix()
    {
        if (Credentials.ToheLogo != null) Credentials.ToheLogo.gameObject.SetActive(true);
    }
}

// 来源：https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Patches/HorseModePatch.cs
[HarmonyPatch(typeof(Constants), nameof(Constants.ShouldHorseAround))]
public static class HorseModePatch
{
    public static bool isHorseMode = false;
    public static bool Prefix(ref bool __result)
    {
        __result = isHorseMode;
        return false;
    }
}
[HarmonyPatch(typeof(Constants), nameof(Constants.ShouldFlipSkeld))]
public static class DleksPatch
{
    public static bool isDleks = false;
    public static bool Prefix(ref bool __result)
    {
        __result = isDleks;
        return false;
    }
}