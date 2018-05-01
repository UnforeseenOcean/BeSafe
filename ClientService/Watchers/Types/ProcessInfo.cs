﻿using System;

namespace BeSafe.Watchers.Types
{
    public class ProcessInfo
    {
        public UInt32 ProcessId { get; set; }
        public UInt32 ParentProcessId { get; set; }
        public string ProcessName { get; set; }
        public string ExecutablePath { get; set; }
    }
}