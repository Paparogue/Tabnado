using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

namespace Tabnado.Hooks
{
    public sealed class TargetingHook : IDisposable
    {
        private readonly IPluginLog _log;
        private readonly IGameInteropProvider _gameInteropProvider;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte TargetingFunctionDelegate(IntPtr a1, IntPtr a2, IntPtr a3, byte a4);

        private Hook<TargetingFunctionDelegate>? _targetingHook;

        public delegate void TargetingEventHandler(IntPtr a1, IntPtr a2, IntPtr a3, byte a4, ref bool allowOriginal);
        public event TargetingEventHandler? OnTargetingFunction;

        public bool IsEnabled { get; set; } = true;

        public bool BlockOriginalCall { get; set; } = false;

        public TargetingHook(IPluginLog log, IGameInteropProvider gameInteropProvider, ISigScanner sigScanner)
        {
            _log = log;
            _gameInteropProvider = gameInteropProvider;

            try
            {
                var address = sigScanner.ScanText("E8 ?? ?? ?? ?? BA ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 84 C0 0F 84");

                if (address == IntPtr.Zero)
                {
                    _log.Error("Failed to find targeting function signature");
                    return;
                }

                _log.Information($"Found targeting function at address: 0x{address:X}");

                _targetingHook = _gameInteropProvider.HookFromAddress<TargetingFunctionDelegate>(
                    address,
                    TargetingFunctionDetour);

                _targetingHook.Enable();
                _log.Information("Targeting hook enabled successfully");
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to initialize targeting hook: {ex}");
            }
        }

        private byte TargetingFunctionDetour(IntPtr a1, IntPtr a2, IntPtr a3, byte a4)
        {
            if (!IsEnabled)
            {
                return _targetingHook!.Original(a1, a2, a3, a4);
            }

            try
            {
                bool allowOriginal = !BlockOriginalCall;
                OnTargetingFunction?.Invoke(a1, a2, a3, a4, ref allowOriginal);

                if (allowOriginal)
                {
                    return _targetingHook!.Original(a1, a2, a3, a4);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _log.Error($"Exception in targeting detour: {ex}");
                return _targetingHook!.Original(a1, a2, a3, a4);
            }
        }

        public byte CallOriginal(IntPtr a1, IntPtr a2, IntPtr a3, byte a4)
        {
            if (_targetingHook == null)
            {
                _log.Warning("Attempted to call original function but hook is not initialized");
                return 0;
            }

            return _targetingHook.Original(a1, a2, a3, a4);
        }

        public unsafe byte CallOriginal(IntPtr a1, float[] a2Values, int[] a3Values, byte a4)
        {
            if (a2Values == null || a3Values == null)
                throw new ArgumentNullException();

            fixed (float* a2Ptr = a2Values)
            fixed (int* a3Ptr = a3Values)
            {
                return CallOriginal(a1, (IntPtr)a2Ptr, (IntPtr)a3Ptr, a4);
            }
        }

        public void Disable()
        {
            _targetingHook?.Disable();
            _log.Information("Targeting hook disabled");
        }

        public void Enable()
        {
            _targetingHook?.Enable();
            _log.Information("Targeting hook enabled");
        }

        public void Dispose()
        {
            _targetingHook?.Dispose();
            _log.Information("Targeting hook disposed");
        }
    }
}