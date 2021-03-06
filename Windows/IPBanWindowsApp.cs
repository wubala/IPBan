﻿#region Imports

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

#endregion Imports

namespace IPBan
{
    public class IPBanWindowsApp : ServiceBase
    {
        private static IPBanService service;
        private static IPBanWindowsEventViewer eventViewer;

        private static void CreateService(bool testing)
        {
            if (service != null)
            {
                service.Dispose();
            }
            service = IPBanService.CreateService(testing);
            service.Start();
            eventViewer = new IPBanWindowsEventViewer(service);
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            CreateService(false);
        }

        protected override void OnStop()
        {
            service.Stop();
            service = null;
            base.OnStop();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }

        protected virtual void Preshutdown()
        {
        }

        protected override void OnCustomCommand(int command)
        {
            // command is SERVICE_CONTROL_PRESHUTDOWN
            if (command == 0x0000000F)
            {
                Preshutdown();
            }
            else
            {
                base.OnCustomCommand(command);
            }
        }

        public IPBanWindowsApp()
        {
            CanShutdown = false;
            CanStop = CanHandleSessionChangeEvent = CanHandlePowerEvent = true;
            var acceptedCommandsField = typeof(ServiceBase).GetField("acceptedCommands", BindingFlags.Instance | BindingFlags.NonPublic);
            if (acceptedCommandsField != null)
            {
                int acceptedCommands = (int)acceptedCommandsField.GetValue(this);
                acceptedCommands |= 0x00000100; // SERVICE_ACCEPT_PRESHUTDOWN;
                acceptedCommandsField.SetValue(this, acceptedCommands);
            }
        }

        public static int RunWindowsService(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            System.ServiceProcess.ServiceBase[] ServicesToRun;
            ServicesToRun = new System.ServiceProcess.ServiceBase[] { new IPBanWindowsApp() };
            System.ServiceProcess.ServiceBase.Run(ServicesToRun);
            return 0;
        }

        public static int RunConsole(string[] args)
        {
            bool test = args.Contains("test", StringComparer.OrdinalIgnoreCase);
            bool test2 = args.Contains("test-eventViewer", StringComparer.OrdinalIgnoreCase);
            CreateService(test || test2);
            if (test)
            {
                eventViewer.RunTests();
            }
            else if (test2)
            {
                eventViewer.TestAllEntries();
            }
            else
            {
                Console.WriteLine("Press ENTER to quit");
                Console.ReadLine();
            }
            service.Stop();
            return 0;
        }

        public static int ServiceEntryPoint(string[] args)
        {
            if (Console.IsInputRedirected)
            {
                return IPBanWindowsApp.RunWindowsService(args);
            }
            else
            {
                return IPBanWindowsApp.RunConsole(args);
            }
        }

        public static int WindowsMain(string[] args)
        {
            return ServiceEntryPoint(args);
        }
    }
}
