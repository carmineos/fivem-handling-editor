namespace HandlingEditor.Client
{
    public class ScreenNotificationHandler : INotificationHandler
    {
        public void Notify(string message)
        {
            CitizenFX.Core.UI.Screen.ShowNotification($"{Globals.ScriptName}: {message}");
        }
    }
}
