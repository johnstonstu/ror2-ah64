using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AH64.Survivors.Components
{
    internal static class AH64RotorBank
    {
        private static bool loaded;
        private static int attempts;
        private static float retryAt;

        internal static bool EnsureLoaded()
        {
            if (loaded) return true;
            if (!AkSoundEngine.IsInitialized() || attempts >= 3 || Time.unscaledTime < retryAt)
                return false;
            attempts++;
            retryAt = Time.unscaledTime + 5f;
            string path = Path.Combine(Path.GetDirectoryName(typeof(AH64Plugin).Assembly.Location),
                "SoundBanks", "AH64Rotor.bnk");
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length < 12 || System.Text.Encoding.ASCII.GetString(bytes, 0, 4) != "BKHD"
                    || BitConverter.ToUInt32(bytes, 8) != 150)
                    throw new InvalidDataException("Expected Wwise 2023.1 soundbank format 150.");
                // MemoryCopy owns its native copy after return; no pinned managed buffer survives.
                GCHandle pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                AKRESULT result;
                uint bankId;
                try { result = AkSoundEngine.LoadBankMemoryCopy(pinned.AddrOfPinnedObject(), (uint)bytes.Length, out bankId); }
                finally { pinned.Free(); }
                loaded = result == AKRESULT.AK_Success || result == AKRESULT.AK_BankAlreadyLoaded;
                Log.Info($"AH-64 rotor bank: result={result}, id={bankId}, bytes={bytes.Length}, path={path}.");
                return loaded;
            }
            catch (Exception exception)
            {
                Log.Error($"Loading AH-64 rotor bank failed at {path}: {exception}");
                throw;
            }
        }
    }
}
