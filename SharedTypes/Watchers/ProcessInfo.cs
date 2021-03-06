﻿using System;
using System.IO;

namespace SharedTypes.Watchers
{
    public class ProcessInfo
    {
        public UInt32 ProcessId { get; set; }
        public UInt32 ParentProcessId { get; set; }
        public string ProcessName { get; set; }
        public string ExecutablePath { get; set; }

        public string Sha256Hash
        {
            get
            {
                try
                {
                    if (!File.Exists(ExecutablePath))
                        return null;

                    byte[] fileBuffer = File.ReadAllBytes(ExecutablePath);
                    return HashUtils.GetSHA256(fileBuffer);
                }
                catch(Exception ex)
                {
                    return null;
                }
            }
        }

        public new string ToString()
        {
            return $"PID:{ProcessId},PPID:{ParentProcessId},ProcessName:{ProcessName},ExecutablePath:{ExecutablePath}";
        }
    }
}