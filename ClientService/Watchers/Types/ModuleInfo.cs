﻿using System;
using System.IO;
using BeSafe.Utils;

namespace BeSafe.Watchers.Types
{
    public class ModuleInfo
    {
        public UInt32 ProcessId { get; set; }
        public string ModulePath { get; set; }

        public string Sha256Hash
        {
            get
            {
                try
                {
                    if (!File.Exists(ModulePath))
                        return null;

                    byte[] fileBuffer = File.ReadAllBytes(ModulePath);
                    return HashHelper.GetSHA256(fileBuffer);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }

        public new string ToString()
        {
            return $"PID:{ProcessId},ModulePath:{ModulePath}";
        }
    }
}