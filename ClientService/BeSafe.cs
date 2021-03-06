﻿using System;
using System.ServiceProcess;
using BeSafe.Core;
using BeSafe.Core.Utils;
using BeSafe.Core.Regulators.ComponentRegulators;
using Common.PipeCommandStructure;
using ExceptionManager;
using NamedPipeWrapper;
using ConfigManager;

namespace ClientService
{
    public partial class BeSafe : ServiceBase
    {
        private readonly PipeServer pipeServer = new PipeServer();

        public BeSafe()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                ConfigApplier();

                pipeServer.ClientMessageEventHandler += OnClientCommandReceived;
                pipeServer.Start();
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        protected override void OnStop()
        {
            pipeServer.Stop();
            ComponentRegulator.ManageComponentsState(null, null, true);
        }

        private void OnClientCommandReceived(NamedPipeConnection<BeSafePipeCommand, BeSafePipeCommand> connection, BeSafePipeCommand command)
        {
            try
            {
                switch (command.Command)
                {
                    case PipeCommands.ComponentConfiguration:
                        {
                            bool sotreSettingResult = new ServiceSetting().StoreConfigFilePath(command.ConfigFilePath);
                            if (sotreSettingResult)
                                ConfigApplier();
                        }
                        break;

                    case PipeCommands.ReloadPlugins:
                        {

                        }
                        break;

                    case PipeCommands.Notification:
                        {

                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private bool ConfigApplier()
        {
            BeSafeConfig configiguration = ConfigLoader.Instance().LoadConfig();
    
            if (configiguration != null)
            {
                ServiceUtils.SystemHiddenDirectory(configiguration.SecureVolumePath);
                ComponentRegulator.ManageComponentsState(configiguration, pipeServer, false);
                return true;
            }

            return false;
        }
    }
}