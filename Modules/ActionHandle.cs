namespace Lotus.Roles.Internals;

public class ActionHandle
{
    public static ActionHandle NoInit() => new();

    public bool IsCanceled => Cancellation is not CancelType.None;
    public CancelType Cancellation;


    public ActionHandle() { }

    public void Cancel(CancelType cancelType = CancelType.Normal)
    {
        this.Cancellation = cancelType;
    }


    public enum CancelType
    {
        None,
        Normal,
        Complete
    }
}