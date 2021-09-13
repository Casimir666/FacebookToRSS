using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace FacebookToRSS
{
    class Program
    {
        private static readonly CancellationTokenSource Tcs = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            if (Configuration.Default == null)
            {
                Logger.LogMessage("WARNING: configuration file not found!");
            }
            else
            {
                var daemon = new FacebookDaemon();
                RegisterHandlers();

                await daemon.RunAsync(Tcs.Token);
            }
            Logger.LogMessage("Bridge is stopped...");
        }

        private static void RegisterHandlers()
        {
            try
            {
                AssemblyLoadContext.Default.Unloading += SigTermEventHandler;
                Console.CancelKeyPress += SigIntEventHandler;
                AppDomain.CurrentDomain.UnhandledException += UnhandledHandler;
            }
            catch (Exception ex)
            {
                Logger.LogMessage("Cannot register system event handlers");
                Logger.LogMessage(ex.StackTrace);
            }
        }

        private static void UnhandledHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.LogMessage($"Fatal error, {(e.ExceptionObject as Exception)?.Message}");
            Logger.LogMessage((e.ExceptionObject as Exception)?.StackTrace);
        }

        private static void SigIntEventHandler(object sender, ConsoleCancelEventArgs e)
        {
            Logger.LogMessage("Exiting...");
            Tcs.Cancel();
        }

        private static void SigTermEventHandler(AssemblyLoadContext obj)
        {
            Logger.LogMessage("Unloading...");
            Tcs.Cancel();
        }

    }
}
