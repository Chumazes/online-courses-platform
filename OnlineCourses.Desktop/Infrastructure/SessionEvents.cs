namespace OnlineCourses.Desktop.Infrastructure;

public static class SessionEvents
{
    public static event Action<string>? SessionExpired;

    public static void RaiseSessionExpired(string message)
    {
        SessionExpired?.Invoke(message);
    }
}
