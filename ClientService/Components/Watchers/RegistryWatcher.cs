﻿using System;
using System.Linq;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Win32;
using BeSafe.Core.Utils;
using ExceptionManager;
using SharedTypes.Watchers.RegistryWatcherTypes;

namespace BeSafe.Components.Watchers
{
    public class RegistryWatcher
    {
        public delegate void NewRegistryValueChangeEventHandler(ChangedValueInfo valueInfo);
        public event NewRegistryValueChangeEventHandler ValueChanged;

        readonly List<RegistryMonitorPath> _mustMonitorRegistryPathes;
        private readonly CancellationTokenSource _cancelToken = new CancellationTokenSource();
        private const int ValueRegChangeNotifyFilter = 4;


        public RegistryWatcher(List<RegistryMonitorPath> mustMonitorRegistryPathes)
        {
            _mustMonitorRegistryPathes = mustMonitorRegistryPathes;
        }

        public bool Start()
        {
            try
            {
                foreach (var mustWatchPath in _mustMonitorRegistryPathes)
                {
                    Task.Run(() =>
                    {
                        MonitorKeyLoop(mustWatchPath);
                    }, _cancelToken.Token);
                }
                return true;
            }
            catch (Exception ex)
            {
                ex.Log(ExceptionType.Service);
                return false;
            }
        }

        public bool Stop()
        {
            try
            {
                _cancelToken.Cancel();
                return true;
            }
            catch (Exception ex)
            {
                ex.Log(ExceptionType.Service);
                return false;
            }
        }

        private void MonitorKeyLoop(RegistryMonitorPath registryPath)
        {
            int result = Win32APIDefinitions.RegOpenKeyEx((IntPtr)registryPath.RegistryHive, registryPath.RegistryKeyPath, 0, Win32APIDefinitions.STANDARD_RIGHTS_READ | Win32APIDefinitions.KEY_QUERY_VALUE | Win32APIDefinitions.KEY_NOTIFY,
                                      out var registryKey);

            if (result != 0)
                return;

            try
            {
                AutoResetEvent _eventNotify = new AutoResetEvent(false);
                WaitHandle[] waitHandles = new WaitHandle[] { _eventNotify };
                while (!_cancelToken.IsCancellationRequested)
                {
                    List<RegistryChangedObject> cachedValues = GetNamesAndValuesOfRegistryKey(registryPath.RegistryHive, registryPath.RegistryKeyPath).ToList();

                    result = Win32APIDefinitions.RegNotifyChangeKeyValue(registryKey, true, ValueRegChangeNotifyFilter, _eventNotify.SafeWaitHandle, true);
                    if (result != 0)
                        throw new Win32Exception(result);

                    if (WaitHandle.WaitAny(waitHandles) == 0)
                    {
                        List<RegistryChangedObject> currentValues = GetNamesAndValuesOfRegistryKey(registryPath.RegistryHive, registryPath.RegistryKeyPath).ToList();

                        List<RegistryChangedObject> CachedWithCurrentDiff = currentValues.Except(cachedValues).ToList();

                        cachedValues = currentValues;

                        ValueChanged?.Invoke(new ChangedValueInfo
                        {
                            MonitorPath = registryPath,
                            ChangedObject = CachedWithCurrentDiff.FirstOrDefault()
                        });
                    }
                }
            }
            finally
            {
                if (registryKey != IntPtr.Zero)
                    Win32APIDefinitions.RegCloseKey(registryKey);
            }
        }

        private IEnumerable<RegistryChangedObject> GetNamesAndValuesOfRegistryKey(RegistryHive hive, string path)
        {
            RegistryKey regKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            regKey = regKey.OpenSubKey(path);

            List<string> keyNames = regKey.GetValueNames().ToList();

            foreach (string keyName in keyNames)
            {
                RegistryValueKind valueKind = regKey.GetValueKind(keyName);
                if ((valueKind == RegistryValueKind.String) || (valueKind == RegistryValueKind.ExpandString))
                {
                    string valueOfKey = (string)regKey.GetValue(keyName);
                    yield return new RegistryChangedObject {Key = keyName, Value = valueOfKey };
                }
            }
        }
    }
}
