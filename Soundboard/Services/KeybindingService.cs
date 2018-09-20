using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundboard.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Threading;

namespace Soundboard.Services
{
    public class KeybindingService: IDisposable
    {
        public class KeyBindingParser
        {
            private static readonly Regex BindingRegex = new Regex(@"((?<keys>\w+)\+?)*\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            private static readonly Regex MappingRegex = new Regex(@"\s*(\w+)\s*\=\s*(0x[a-fA-F0-9]+)\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

            public static readonly IDictionary<string, int> VirtualKeyMap;

            static KeyBindingParser()
            {
                VirtualKeyMap = new Dictionary<string, int>();

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Soundboard.Resources.vkmap.txt"))
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var match = MappingRegex.Match(line);

                        if (match.Success)
                        {
                            VirtualKeyMap[match.Groups[1].Value] =
                                Convert.ToInt32(match.Groups[2].Value, 16);
                        }
                    }
                }
            }

            public static KeyBinding Parse(string binding, string command)
            {
                if (string.IsNullOrWhiteSpace(binding))
                    return null;

                if (string.IsNullOrWhiteSpace(command))
                    return null;

                var match = BindingRegex.Match(binding);

                if (!match.Success)
                    throw new ArgumentException("Invalid key binding", nameof(binding));

                var keys = new List<int>(match.Groups["keys"].Captures
                    .Cast<Capture>()
                    .Select(c =>
                    {
                        if (VirtualKeyMap.TryGetValue(c.Value, out int virtualKey))
                            return virtualKey;

                        throw new ArgumentException(
                            $"Binding <{binding}> contains invalid or unknown keys",
                            nameof(binding));
                    }));

                return new KeyBinding(keys, command);
            }
        }

        public class KeyBindingActivatedEventArgs: EventArgs
        {
            public readonly KeyBinding Binding;

            public KeyBindingActivatedEventArgs(KeyBinding binding)
            {
                Binding = binding;
            }
        }

        public class KeyBinding
        {
            public readonly ISet<int> Keys;
            public readonly string Command;

            public KeyBinding(IEnumerable<int> keys, string command)
            {
                Keys = new HashSet<int>(keys);
                Command = command ?? string.Empty;
            }
        }

        private readonly IList<KeyBinding> _bindings;
        private readonly ISet<int> _keys;
        private readonly ReaderWriterLockSlim _lock;
        private readonly AutoResetEvent _reset;
        private readonly CancellationTokenSource _cancellationToken;
        private readonly Thread _thread;
        private readonly ILogger _logger;
        private readonly IOptions<SoundboardOptions> _options;
        private readonly IOptionsMonitor<SoundboardOptions> _optionsMonitor;
        private Win32.LowLevelKeyboardProc _hookProc;
        private Win32.HookHandle _hook;

        public event EventHandler<KeyBindingActivatedEventArgs> BindingActivated;

        public KeybindingService(ILoggerFactory loggerFactory,
            IOptions<SoundboardOptions> options,
            IOptionsMonitor<SoundboardOptions> optionsMonitor)
        {
            _options = options;
            _optionsMonitor = optionsMonitor;
            _optionsMonitor.OnChange((cfg, _) => LoadBindings(cfg));
            _logger = loggerFactory.CreateLogger<KeybindingService>();
            _bindings = new List<KeyBinding>();
            _keys = new HashSet<int>();
            _lock = new ReaderWriterLockSlim();
            _reset = new AutoResetEvent(false);
            _thread = new Thread(ProcessBindingActivations);
            _cancellationToken = new CancellationTokenSource();
            _thread.Start();

            LoadBindings(_options.Value);

            using (var process = Process.GetCurrentProcess())
            using (var processModule = process.MainModule)
            {
                _hookProc = OnKeyboardEvent;
                _hook = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _hookProc,
                    Win32.GetModuleHandle(processModule.ModuleName), 0);
            }
        }

        public IEnumerable<KeyBinding> GetRegisteredBindings()
        {
            IEnumerable<KeyBinding> bindings = null;
            _lock.EnterReadLock();
            bindings = _bindings.ToList();
            _lock.ExitReadLock();
            return bindings;
        }

        public bool IsBindingActive(KeyBinding binding)
        {
            return binding.Keys.IsSubsetOf(_keys);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void LoadBindings(SoundboardOptions config)
        {
            if (config == null)
                return;

            _logger.LogInformation("Loading key binding configuration...");
            _lock.EnterWriteLock();

            _bindings.Clear();

            try
            {
                if (config.Bindings != null)
                {
                    foreach (var binding in config.Bindings)
                    {
                        _bindings.Add(
                            KeyBindingParser.Parse(binding.Key, binding.Value));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load key bindings: {0}", ex.Message);
            }

            _lock.ExitWriteLock();
            _logger.LogInformation("Loaded key binding configuration.");
        }

        private void ProcessBindingActivations()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Processing key bindings...");

                _reset.WaitOne();
                _lock.EnterReadLock();

                foreach (var binding in _bindings)
                {
                    if (IsBindingActive(binding))
                        BindingActivated?.Invoke(this, new KeyBindingActivatedEventArgs(binding));
                }

                _lock.ExitReadLock();
            }
        }

        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
        private void Dispose(bool disposing)
        {
            try
            {
                _cancellationToken.Cancel();
                _reset.Set();
                _thread.Join();
            }
            catch (Exception)
            {
                // NOTE: Dispose shouldn't throw.
            }
            finally
            {
                _cancellationToken.Dispose();

                if (_hook != null && !_hook.IsInvalid)
                {
                    _hookProc = null;
                    _hook.Dispose();
                }
            }
        }

        private IntPtr OnKeyboardEvent(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int virtualKeyCode = 0;

                switch (wParam.ToInt32())
                {
                    case Win32.WM_KEYDOWN:
                        virtualKeyCode = Marshal.ReadInt32(lParam);
                        OnKeyDown(virtualKeyCode);
                        break;
                    case Win32.WM_KEYUP:
                        virtualKeyCode = Marshal.ReadInt32(lParam);
                        OnKeyUp(virtualKeyCode);
                        break;
                }
            }

            return Win32.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private void OnKeyDown(int key)
        {
            _lock.EnterWriteLock();
            _keys.Add(key);
            _lock.ExitWriteLock();

            _reset.Set();
        }

        private void OnKeyUp(int key)
        {
            _lock.EnterWriteLock();
            _keys.Remove(key);
            _lock.ExitWriteLock();

            _reset.Set();
        }

    }
}
