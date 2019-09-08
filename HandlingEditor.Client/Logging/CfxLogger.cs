namespace HandlingEditor.Client
{
    public class CfxLogger : ILogger
    {
        public void Log(string message)
        {
            CitizenFX.Core.Debug.WriteLine($"{Globals.ScriptName}: {message}");
        }
    }
}
