namespace HandlingEditor.Client
{
    public class FeedNotificationHandler : INotificationHandler
    {
        public void Notify(string message)
        {
            CitizenFX.Core.UI.Screen.ShowNotification($"{Globals.ScriptName}: {message}");
        }
    }
}
