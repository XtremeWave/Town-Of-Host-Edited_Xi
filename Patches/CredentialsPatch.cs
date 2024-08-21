using HarmonyLib;
using System.Text;
using TMPro;
using UnityEngine;
using TOHEXI.Modules;
using static TOHEXI.Translator;

namespace TOHEXI;

[HarmonyPatch]
public static class Credentials
{
    public static SpriteRenderer ToheLogo { get; private set; }

    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    class PingTrackerUpdatePatch
    {
        private static readonly StringBuilder sb = new();

        private static void Postfix(PingTracker __instance)
        {
            __instance.text.alignment = TextAlignmentOptions.TopRight;

            sb.Clear();

            sb.Append(Main.credentialsText);

            var ping = AmongUsClient.Instance.Ping;
        string color = "#90EE90";
        string ping1 = "0";
        if (ping < 175)
        {
            color = "#90EE90";
            ping1 = "1";
        }
        else if (ping < 225)
        {
            color = "#A8B644";
            ping1 = "2";
        }
        else if (ping < 500)
        {
            color = "#CD7B29";
            ping1 = "3";
        }
        else if (ping < 650)
        {
            color = "#BA0505";
            ping1 = "4";

        }
        else if (ping < 800)
        {
            color = "#7E0404";
            ping1 = "5";

        }
        else if (ping < 9800)
            {
            color = "#520606";
            ping1 = "6";

            }
        sb.Append($"\r\n").Append($"<color={color}>{GetString($"Ping{ping1}")}</color>\n<color=#9F90FF>{GetString("Ping")}:</color><color={color}> {ping}</color><size=2.3><color=#C3B9FF>{GetString("ms")}</color></size>");

        if (Options.NoGameEnd.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(UnityEngine.Color.red, $"<size=2.5>{GetString("NoGameEnd")}</size>"));
        if (Options.AllowConsole.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(UnityEngine.Color.red, $"<size=2.5>{GetString("AllowConsole")}</size>"));
        if (!GameStates.IsModHost) sb.Append($"\r\n").Append(Utils.ColorString(UnityEngine.Color.red, $"<size=2.5>{GetString("Warning.NoModHost")}</size>"));
        if (DebugModeManager.IsDebugMode) sb.Append("\r\n").Append(Utils.ColorString(UnityEngine.Color.green, $"<size=2.5>{GetString("DebugMode")}</size>"));
        if (Options.LowLoadMode.GetBool()) sb.Append("\r\n").Append(Utils.ColorString(UnityEngine.Color.green, $"<size=2.5>{GetString("LowLoadMode")}</size>"));

            var offset_x = 1.2f; //右端からのオフセット
            if (HudManager.InstanceExists && HudManager._instance.Chat.chatButton.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
            if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; //フレンドリストボタンがある場合の追加オフセット
            __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, 0f, 0f);

            __instance.text.text = sb.ToString();
        }
    }
    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    internal class VersionShowerStartPatch
    {
        public static GameObject OVersionShower;
        private static TextMeshPro SpecialEventText;
        private static TextMeshPro VisitText;

        private static void Postfix(VersionShower __instance)
        {
            if (AmongUsClient.Instance.AmHost) return;
            if (GameStates.IsInTask) return;
            Main.credentialsText = $"\r\n<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginDisplayVersion}";
            if (Main.IsAprilFools) Main.credentialsText = $"\r\n<color=#00bfff>Town Of Host</color> v11.45.14";
            else if (Main.IsTOHEInitialRelease) Main.credentialsText = $"\r\n<color=#ffc0cb>Town Of Host Edited</color> v4.0.23";
            else if (Main.IsInitialRelease) Main.credentialsText += $"\r\n<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginDisplayVersion}\r\n<color=#ffc0cb>Town Of Host Edited</color> v2.3.6";

#if RELEASE
            Main.credentialsText += $"\r\n<size=2.3><color=#ffc0cb>TOHE</color> <color=#8035DF>By <color=#ffc0cb>KARPED1EM</color></size>\r\n<size=2.3><color=#fffcbe>TOHEXI</color> <color=#35dfca>By <color=#fffcbe>{Translator.GetString("xiawa")}</color></size>";
#endif
#if CANARY
        Main.credentialsText += $"\r\n<color=#fffe1e>Canary({ThisAssembly.Git.Commit})</color>";
#endif

#if DEBUG
        Main.credentialsText += $"\r\n<color={Main.ModColor}>{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})</color>";
#endif

#if RELEASE || CANARY
            string additionalCredentials = GetString("TextBelowVersionText");
            if (additionalCredentials != null && additionalCredentials != "*TextBelowVersionText")
            {
                Main.credentialsText += $"\n{additionalCredentials}";
            }
#endif
            var credentials = Object.Instantiate(__instance.text);
            credentials.text = Main.credentialsText;
            credentials.alignment = TMPro.TextAlignmentOptions.TopRight;
            credentials.transform.position = new Vector3(4.6f, 3.2f, 0);

            ErrorText.Create(__instance.text);
            if (Main.hasArgumentException && ErrorText.Instance != null)
                ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);

            if (SpecialEventText == null)
            {
                SpecialEventText = Object.Instantiate(__instance.text);
                SpecialEventText.text = "";
                SpecialEventText.color = UnityEngine.Color.white;
                SpecialEventText.fontSize += 2.5f;
                SpecialEventText.alignment = TextAlignmentOptions.Top;
                SpecialEventText.transform.position = new Vector3(0, 0.5f, 0);
            }
            SpecialEventText.enabled = TitleLogoPatch.amongUsLogo != null;
            if (Main.IsInitialRelease)
            {
                SpecialEventText.text = $"Happy Birthday to {Main.ModName}!{GetString("ThanksKap")}";
                ColorUtility.TryParseHtmlString(Main.ModColor, out var col);
                SpecialEventText.color = col;
            }
            else if (Main.IsTOHEInitialRelease)
            {
                SpecialEventText.text = $"Happy Birthday to Town Of Host Edited!\n Wish Karpe Can Happy Every Day";
                ColorUtility.TryParseHtmlString("#ffc0cb", out var col);
                SpecialEventText.color = col;
            }
            else if (!Main.IsAprilFools)
            {
                SpecialEventText.text = $"{Main.MainMenuText}";
                SpecialEventText.fontSize = 0.9f;
                SpecialEventText.color = UnityEngine.Color.white;
                SpecialEventText.alignment = TextAlignmentOptions.TopRight;
                SpecialEventText.transform.position = new Vector3(4.6f, 2.725f, 0);
            }

            if ((OVersionShower = GameObject.Find("VersionShower")) != null && !Main.IsAprilFools)
            {
                OVersionShower.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
                OVersionShower.transform.position = new Vector3(-5.3f, 2.9f, 0f);
                if (TitleLogoPatch.amongUsLogo != null)
                {
                    if (VisitText == null && ModUpdater.visit > 0)
                    {
                        VisitText = Object.Instantiate(__instance.text);
                        VisitText.text = string.Format(GetString("TOHEVisitorCount"), Main.ModColor, ModUpdater.visit);
                        VisitText.color = UnityEngine.Color.white;
                        VisitText.fontSize = 1.2f;
                        //VisitText.alignment = TMPro.TextAlignmentOptions.Top;
                        OVersionShower.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
                        VisitText.transform.position = new Vector3(-5.3f, 2.75f, 0f);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class TitleLogoPatch
    {
        public static GameObject amongUsLogo;
        public static GameObject Ambience;
        public static GameObject LoadingHint;

        private static void Postfix(MainMenuManager __instance)
        {
            amongUsLogo = GameObject.Find("LOGO-AU");

            var rightpanel = __instance.gameModeButtons.transform.parent;
            var logoObject = new GameObject("titleLogo_TOHE");
            var logoTransform = logoObject.transform;
            ToheLogo = logoObject.AddComponent<SpriteRenderer>();
            logoTransform.parent = rightpanel;
            logoTransform.localPosition = new(-0.16f, 0f, 1f); //new(0f, 0.3f, 1f); new(0f, 0.15f, 1f);
            logoTransform.localScale *= 1.2f;

            if (!Options.IsLoaded)
            {
                LoadingHint = new GameObject("LoadingHint");
                LoadingHint.transform.position = Vector3.down;
                var LoadingHintText = LoadingHint.AddComponent<TextMeshPro>();
                LoadingHintText.text = GetString("Loading");
                LoadingHintText.alignment = TextAlignmentOptions.Center;
                LoadingHintText.fontSize = 2f;
                LoadingHintText.transform.position = amongUsLogo.transform.position;
                LoadingHintText.transform.position += new Vector3 (-0.25f, -0.9f, 0f);
                LoadingHintText.color = new Color32(17, 255, 1, byte.MaxValue);
                __instance.playButton.transform.gameObject.SetActive(false);
            }
            if ((Ambience = GameObject.Find("Ambience")) != null)
            {
                // Show playButton when mod is fully loaded
                if (Options.IsLoaded && LoadingHint != null) __instance.playButton.transform.gameObject.SetActive(true);

                Ambience.SetActive(false);
                //var CustomBG = new GameObject("CustomBG");
                //CustomBG.transform.position = new Vector3(2.095f, -0.25f, 520f);
                //var bgRenderer = CustomBG.AddComponent<SpriteRenderer>();
                //bgRenderer.sprite = Utils.LoadSprite("TOHEXI.Resources.Background.TOH-Background-Old.jpg", 245f);
            }
        }
    }
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    class ModManagerLateUpdatePatch
    {
        public static void Prefix(ModManager __instance)
        {
            __instance.ShowModStamp();

            LateTask.Update(Time.deltaTime);
            CheckMurderPatch.Update();
        }
        public static void Postfix(ModManager __instance)
        {
            var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
            __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
                __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
                new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
        }
    }
}