using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using static TheOtherRoles_Host.Translator;

namespace TheOtherRoles_Host;

//参考：https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Patches/LogoAndStampPatch.cs
[HarmonyPatch]
public static class CredentialsPatch
{
    public static GenericPopup popup;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class LogoPatch
    {
        static IEnumerator ViewCredentialsCoro(MainMenuManager __instance)
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (__instance != null)
                {
                    ViewBoosterPatch(__instance);
                }
                break;
            }
        }
        public static string BoosterData = "";
        public static string SponsersData = "";
        public static string DevsData = "";
        public static string TransData = "";

        public static void InitCredentialsData()
        {
            BoosterData = "";
            SponsersData = "";
            DevsData = "";
            TransData = "";

            DevsData += $"<color=#FFC0CB>KARPED1EM</color> - <size=75%>{GetString("EMainDev")}</size>";
            DevsData += $"\n<color=#FFC0CB>IRIDESCENT</color> - <size=75%>{GetString("EArt")}</size>";
            DevsData += $"\nSHAAARKY - <size=75%>{GetString("ERoleDev")}</size>";
            DevsData += $"\n<color={Main.ModColor}>喜</color> - <size=75%>{GetString("MainDev")}</size>";
            DevsData += $"\n<color=#FF0066>Night_瓜</color> - <size=75%>{GetString("Developer")}&{GetString("UpdaterClous")}&{GetString("other")}{GetString("ClousS")}</size>";
            //DevsData += $"\n清风 - <size=75%>{GetString("Server")}</size>";我欢迎你再次回归
            DevsData += $"\n小叨院长 - <size=75%>{GetString("UpGreat")}</size>";
            DevsData += $"\n天寸梦初 - <size=75%>{GetString("RoleDev")}&{GetString("TechSup")}</size>";
            DevsData += $"\n罗寄 - <size=75%>{GetString("EArt")}</size>";

            TransData += $"\nCJ Zeyan - <size=75%>{GetString("TranEN")}</size>";

            BoosterData += $"我们暂时没有DC服务器";

            SponsersData += $"太多了不显示了";
        }

        public static GameObject CreditsPopup;
        static void ViewBoosterPatch(MainMenuManager __instance)
        {
            var template = __instance.transform.FindChild("StatsPopup");
            var obj = Object.Instantiate(template, template.transform.parent).gameObject;
            CreditsPopup = obj;
            Object.Destroy(obj.GetComponent<StatsPopup>());

            var devtitletext = obj.transform.FindChild("StatNumsText_TMP");
            devtitletext.GetComponent<TextMeshPro>().text = GetString("Developer");
            devtitletext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
            devtitletext.localPosition = new Vector3(-2.4f, 1.65f, -2f);
            devtitletext.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            var devtext = obj.transform.FindChild("StatsText_TMP");
            devtext.GetComponent<TextMeshPro>().text = DevsData;
            devtext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Capline;
            devtext.localPosition = new Vector3(-2.4f, 1.27f, -2f);
            devtext.localScale = new Vector3(0.5f, 0.5f, 1f);

            var transtitletext = Object.Instantiate(devtitletext, obj.transform);
            transtitletext.GetComponent<TextMeshPro>().text = GetString("Translator");
            transtitletext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
            transtitletext.localPosition = new Vector3(0f, 1.65f, -2f);
            transtitletext.localScale = new Vector3(0.8f, 0.8f, 1f);

            var transtext = Object.Instantiate(devtext, obj.transform);
            transtext.GetComponent<TextMeshPro>().text = TransData;
            transtext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Capline;
            transtext.localPosition = new Vector3(0f, 1.27f, -2f);
            transtext.localScale = new Vector3(0.5f, 0.5f, 1f);

            //var boostertitletext = Object.Instantiate(devtitletext, obj.transform);
            //boostertitletext.GetComponent<TextMeshPro>().text = GetString("Booster");
            //boostertitletext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
            //boostertitletext.localPosition = new Vector3(-2.4f, -0.7f, -2f);
            //boostertitletext.localScale = new Vector3(0.8f, 0.7f, 1f);

            //var boostertext = Object.Instantiate(devtext, obj.transform);
            //boostertext.GetComponent<TextMeshPro>().text = BoosterData;
            //boostertext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Capline;
            //boostertext.localPosition = new Vector3(-2.4f, -0.98f, -2f);
            //boostertext.localScale = new Vector3(0.5f, 0.5f, 1f);

            //var sponsortitletext = Object.Instantiate(devtitletext, obj.transform);
            //sponsortitletext.GetComponent<TextMeshPro>().text = GetString("Sponsor");
            //sponsortitletext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
            //sponsortitletext.localPosition = new Vector3(2.4f, 1.65f, -2f);
            //sponsortitletext.localScale = new Vector3(0.8f, 0.8f, 1f);

            //var sponsortext = Object.Instantiate(devtext, obj.transform);
            //sponsortext.GetComponent<TextMeshPro>().text = SponsersData;
            //sponsortext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Capline;
            //sponsortext.localPosition = new Vector3(2.4f, 1.27f, -2f);
            //sponsortext.localScale = new Vector3(0.5f, 0.5f, 1f);

            var textobj = obj.transform.FindChild("Title_TMP");
            Object.Destroy(textobj.GetComponent<TextTranslatorTMP>());
            textobj.GetComponent<TextMeshPro>().text = GetString("DevAndSpnTitle");
            textobj.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
            textobj.localScale = new Vector3(1.2f, 1.2f, 1f);
            textobj.localPosition = new Vector3(0f, 2.2f, -2f);
            obj.transform.FindChild("Background").localScale = new Vector3(1.5f, 1f, 1f);
            obj.transform.FindChild("CloseButton").localPosition = new Vector3(-3.75f, 2.65f, 0);
        }
        public static MainMenuManager instance;
        public static void Postfix(MainMenuManager __instance)
        {
            InitCredentialsData();
            instance = __instance;
            AmongUsClient.Instance.StartCoroutine(ViewCredentialsCoro(__instance));
        }
    }
}