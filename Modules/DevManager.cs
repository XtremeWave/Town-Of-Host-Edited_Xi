using System.Collections.Generic;
using System.Linq;

namespace TheOtherRoles_Host;

public class DevUser
{
    public string Code { get; set; }
    public string Color { get; set; }
    public string Tag { get; set; }
    public bool IsUp { get; set; }
    public bool IsDev { get; set; }
    public bool DeBug { get; set; }
    public string UpName { get; set; }
    public DevUser(string code = "", string color = "null", string tag = "null", bool isUp = false, bool isDev = false, bool deBug = false, string upName = "未认证用户")
    {
        Code = code;
        Color = color;
        Tag = tag;
        IsUp = isUp;
        IsDev = isDev;
        DeBug = deBug;
        UpName = upName;
    }
    public bool HasTag() => Tag != "null";
    public string GetTag() => Color == "null" ? $"<size=1.7>{Tag}</size>\r\n" : $"<color={Color}><size=1.7>{(Tag == "#Dev" ? Translator.GetString("Developer") : Tag)}</size></color>\r\n";
}

public static class DevManager
{
    public static DevUser DefaultDevUser = new();
    public static List<DevUser> DevUser = new();
    public static void Init()
    {
        //if (!Main.Devtx.Value)
        //{
            // Dev
           
            DevUser.Add(new(code: "teamelder#5856", color: "#0089FF", tag: "Dev_Slok7565", isUp: true, isDev: true, deBug: true, upName: "Slok7565"));

    }
    public static bool IsDevUser(this string code) => DevUser.Any(x => x.Code == code);
    public static DevUser GetDevUser(this string code) => code.IsDevUser() ? DevUser.Find(x => x.Code == code) : DefaultDevUser;
}
