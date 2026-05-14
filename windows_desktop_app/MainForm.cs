using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace VleraDentDesktop;

public sealed class MainForm : Form
{
    private const string AppUrl = "https://vlera-dent-frontend.drin80614.workers.dev/Auth/Login";
    private readonly WebView2 _webView = new();
    private readonly Panel _topBar = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _status = new();
    private readonly Button _retry = new();

    public MainForm()
    {
        Text = "Vlera Dent";
        MinimumSize = new Size(1100, 720);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(239, 250, 255);

        try
        {
            Icon = new Icon("app_icon.ico");
        }
        catch
        {
            // The application still works if Windows cannot load the icon.
        }

        BuildChrome();
        Shown += async (_, _) => await InitializeWebViewAsync();
    }

    private void BuildChrome()
    {
        _topBar.Dock = DockStyle.Top;
        _topBar.Height = 58;
        _topBar.BackColor = Color.White;
        _topBar.Padding = new Padding(18, 8, 18, 8);

        var title = new Label
        {
            Text = "Vlera Dent",
            AutoSize = true,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 35, 58),
            Dock = DockStyle.Left,
            Padding = new Padding(0, 9, 0, 0)
        };

        _status.Text = "Duke u lidhur...";
        _status.AutoSize = true;
        _status.Dock = DockStyle.Right;
        _status.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _status.ForeColor = Color.FromArgb(96, 112, 137);
        _status.Padding = new Padding(0, 13, 16, 0);

        _retry.Text = "Rifresko";
        _retry.Dock = DockStyle.Right;
        _retry.Width = 96;
        _retry.FlatStyle = FlatStyle.Flat;
        _retry.FlatAppearance.BorderColor = Color.FromArgb(21, 155, 215);
        _retry.ForeColor = Color.FromArgb(46, 49, 146);
        _retry.BackColor = Color.White;
        _retry.Click += (_, _) => _webView.Reload();

        _progress.Dock = DockStyle.Top;
        _progress.Height = 4;
        _progress.Style = ProgressBarStyle.Marquee;
        _progress.MarqueeAnimationSpeed = 20;

        _webView.Dock = DockStyle.Fill;
        _webView.DefaultBackgroundColor = Color.White;

        _topBar.Controls.Add(_status);
        _topBar.Controls.Add(_retry);
        _topBar.Controls.Add(title);
        Controls.Add(_webView);
        Controls.Add(_progress);
        Controls.Add(_topBar);
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VleraDent",
                "WebView2");

            Directory.CreateDirectory(dataFolder);
            var environment = await CoreWebView2Environment.CreateAsync(null, dataFolder);
            await _webView.EnsureCoreWebView2Async(environment);

            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _webView.CoreWebView2.NavigationStarting += (_, _) =>
            {
                _progress.Visible = true;
                _status.Text = "Duke hapur Vlera Dent...";
            };
            _webView.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                _progress.Visible = false;
                _status.Text = args.IsSuccess ? "Online" : "Nuk u hap faqja";
            };
            _webView.CoreWebView2.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                _webView.CoreWebView2.Navigate(args.Uri);
            };

            _webView.CoreWebView2.Navigate(AppUrl);
        }
        catch (Exception ex)
        {
            _progress.Visible = false;
            _status.Text = "WebView2 mungon";
            var result = MessageBox.Show(
                $"Nuk u hap aplikacioni brenda dritares.\n\n{ex.Message}\n\nA don me e hap ne browser?",
                "Vlera Dent",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo(AppUrl) { UseShellExecute = true });
            }
        }
    }
}
