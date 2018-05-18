﻿using System;
using System.Linq;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ConfigManager;
using BeSafe.Components.Watchers;
using SharedTypes.Watchers.RegistryWatcherTypes;
using BeSafe.Core.Regulators.PluginRegulators;
using PluginSDK;
using BeSafe.Core.Utils;
using Common.PipeCommandStructure;

namespace BeSafe.Core.Regulators.ComponentRegulators
{
    public class RegistryRegulator : IComponentRegulator
    {
        #region Singleton
        private static RegistryRegulator SingletonInstance;
        public static RegistryRegulator Instance() => (SingletonInstance ?? (SingletonInstance = new RegistryRegulator()));
        #endregion

        private RegistryWatcher registryWatcher;
        private ConcurrentStack<ChangedValueInfo> ChangedValuesStack = new ConcurrentStack<ChangedValueInfo>();

        private BeSafeConfig _config;
        private PipeServer _pipeServer;

        public void Config(BeSafeConfig config, PipeServer pipeServer, bool stoppingService)
        {
            _config = config;
            _pipeServer = pipeServer;

            bool stateResult;

            if ((config?.ComponentsState.RegistryWatcher == true) && (stoppingService == false))
            {
                string userSID = config.UserSID;

                registryWatcher = new RegistryWatcher(new List<RegistryMonitorPath>
                {
                    // CurrentUser keys
                    new RegistryMonitorPath{RegistryHive = RegistryHive.Users, RegistryKeyPath = $@"{userSID}\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.Users, RegistryKeyPath = $@"{userSID}\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.Users, RegistryKeyPath = $@"{userSID}\Software\Microsoft\Windows\CurrentVersion\RunServices"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.Users, RegistryKeyPath = $@"{userSID}\Software\Microsoft\Windows\CurrentVersion\Run"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.Users, RegistryKeyPath = $@"{userSID}\Software\Microsoft\Windows\CurrentVersion\RunOnce"},

                    // LocalMachine keys
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\explorer\Shell Folders"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\explorer\User Shell Folders"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\RunServices"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\RunServicesOnce"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\RunOnce"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"SOFTWARE\Classes\cplfile\shell\cplopen\command"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"SOFTWARE\Classes\batfile\shell\open\command"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"Software\CLASSES\comfile\shell\open\command"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"Software\CLASSES\exefile\shell\open\command"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"Software\CLASSES\htafile\Shell\Open\Command"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"Software\CLASSES\piffile\shell\open\command"},
                    new RegistryMonitorPath{RegistryHive = RegistryHive.LocalMachine, RegistryKeyPath = @"SOFTWARE\Classes\scrfile\shell\open\command"},
                });

                registryWatcher.ValueChanged += ValueChangedArrived;
                stateResult = registryWatcher.Start();

                Task.Run(() => StackScanner(ChangedValuesStack));
                return;
            }

            stateResult = registryWatcher != null ? registryWatcher.Stop() : false;
        }

        private void ValueChangedArrived(ChangedValueInfo valueInfo)
        {
            ChangedValuesStack.Push(valueInfo);
        }

        private void StackScanner(ConcurrentStack<ChangedValueInfo> stack)
        {
            while (true)
            {
                ChangedValueInfo valueInfo = null;
                stack.TryPop(out valueInfo);
                if (valueInfo == null)
                    continue;

                PluginResult scanResult = PluginProxy.Instance(_config).Scan(valueInfo, PluginType.Registry);

                if (scanResult.RiskRate != ThreatRiskRates.NoRisk)
                {
                    bool? sendCommandResult = _pipeServer?.SendCommandToUI(new BeSafePipeCommand
                    {
                        CommandId = Guid.NewGuid(),
                        Command = PipeCommands.PluginScanResult,
                        PluginScanResult = scanResult
                    });
                }

                Thread.Sleep(500);
            }
        }
    }
}