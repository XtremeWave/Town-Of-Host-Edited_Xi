using HarmonyLib;
using UnityEngine;

namespace TheOtherRoles_Host;

[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.FixedUpdate))]
public class LobbyFixedUpdatePatch
{
    private static GameObject Paint;
    public static void Postfix()
    {
        if (Paint == null)
        {
            var LeftBox = GameObject.Find("Leftbox");
            if (LeftBox != null)
            {
                Paint = Object.Instantiate(LeftBox, LeftBox.transform.parent.transform);
                Paint.name = "Lobby Paint";
                Paint.transform.localPosition = new Vector3(0.042f, -2.59f, -10.5f);
                SpriteRenderer renderer = Paint.GetComponent<SpriteRenderer>();
                renderer.sprite = Utils.LoadSprite("TheOtherRoles_Host.Resources.Images.LobbyPaint.png", 290f);
            }
        }
    }
}
