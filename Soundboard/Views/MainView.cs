using Microsoft.Extensions.Options;
using Soundboard.Options;
using Soundboard.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soundboard.Views
{
    public partial class MainView : Form
    {
        private readonly KeybindingService _keybinds;
        private readonly SoundboardProxyService _soundboard;
        private readonly IOptions<SoundboardOptions> _config;
        private readonly IOptionsMonitor<SoundboardOptions> _configMonitor;

        public MainView(
            KeybindingService keybinds, SoundboardProxyService soundboard,
            IOptions<SoundboardOptions> config,
            IOptionsMonitor<SoundboardOptions> configMonitor)
        {
            _soundboard = soundboard;
            _keybinds = keybinds;
            _config = config;
            _configMonitor = configMonitor;

            _configMonitor.OnChange((cfg, x) =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(delegate { RefreshKeyBindings(); }));
                    return;
                }

                RefreshKeyBindings();
            });

            _keybinds.BindingActivated += async (sender, e) =>
            {
                await _soundboard.SendCommandAsync(e.Binding.Command);
            };

            InitializeComponent();

            mainNotifyIcon.ContextMenuStrip = notifyIconContextMenuStrip;
            mainNotifyIcon.Icon = Icon;
            mainNotifyIcon.MouseUp += (sender, e) =>
            {
                ShowConfiguration();
            };

            RefreshKeyBindings();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            mainNotifyIcon.Visible = false;
        }

        private void RefreshKeyBindings()
        {
            var bindings = _keybinds.GetRegisteredBindings();

            bindingsListView.Items.Clear();
            bindingsListView.Items.AddRange(bindings
                .Select(binding =>
                {
                    var item = new ListViewItem();
                    item.Text = string.Join("+", binding.Keys.Select(k => GetVirtualKeyName(k)));
                    item.SubItems.Add(new ListViewItem.ListViewSubItem(item, binding.Command));
                    return item;
                })
                .ToArray());
        }

        private static string GetVirtualKeyName(int key) =>
            KeybindingService.KeyBindingParser.VirtualKeyMap
                .First(kv => kv.Value == key)
                .Key;
        
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void configureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowConfiguration();
        }

        private void ShowConfiguration()
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void openConfigButton_Click(object sender, EventArgs e)
        {
            try
            {
                var info = new ProcessStartInfo
                {
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    FileName = "config.json",
                    UseShellExecute = true
                };

                Process.Start(info);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    text: "Failed to load configuration. See log for additional details.", 
                    caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            }
        }

        private void quitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
