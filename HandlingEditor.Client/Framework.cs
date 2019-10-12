namespace HandlingEditor.Client
{
    public static class Framework
    {
        //private static Container Container { get; set; }


        public static ILogger Logger { get; set; } //=> Container.GetInstance<ILogger>();
        public static INotificationHandler Notifier { get; set; } //Notifier => Container.GetInstance<INotificationHandler>();

        public static HandlingInfo HandlingInfo { get; set; }

        public static void Build()
        {
            //Container = new Container();

            //Container.Register<ILogger, CfxLogger>(Lifestyle.Singleton);
            //Container.Register<INotificationHandler, ScreenNotificationHandler>(Lifestyle.Singleton);

            Logger = new CfxLogger(new CfxLoggerConfiguration() { LogLevel = LogLevel.Debug });
            Notifier = new FeedNotificationHandler();
            HandlingInfo = new HandlingInfo(Logger);
        }
    }
}
