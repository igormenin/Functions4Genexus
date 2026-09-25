using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Func4Genexus.UI
{
    public class UpdateNotificationForm : Form
    {
        public UpdateNotificationForm(string currentVersion, string latestVersion, string environmentName, string recommendedZip, string releaseUrl)
        {
            this.Text = "Atualização Disponível - Func4Genexus";
            this.Size = new Size(460, 260);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = true;

            var lblHeader = new Label
            {
                Text = "Nova versão do Func4Genexus disponível!",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Left = 20,
                Top = 18,
                Width = 410,
                Height = 28
            };

            var lblDetails = new Label
            {
                Text = $"Uma nova versão foi publicada no GitHub:\n" +
                       $"• Versão instalada: v{currentVersion}\n" +
                       $"• Versão mais recente: v{latestVersion}\n\n" +
                       $"Para o seu ambiente ({environmentName}), o arquivo a ser baixado é:\n" +
                       $"👉 {recommendedZip}",
                Font = new Font("Segoe UI", 9.5f),
                Left = 20,
                Top = 52,
                Width = 410,
                Height = 110
            };

            var btnGoToSite = new Button
            {
                Text = "Ir para o site",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Left = 195,
                Top = 175,
                Width = 140,
                Height = 32,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnGoToSite.FlatAppearance.BorderSize = 0;
            btnGoToSite.Click += (s, e) =>
            {
                try
                {
                    var targetUrl = string.IsNullOrWhiteSpace(releaseUrl)
                        ? "https://github.com/igormenin/Functions4Genexus/releases"
                        : releaseUrl;

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetUrl,
                        UseShellExecute = true
                    });
                }
                catch { }
                this.Close();
            };

            var btnOk = new Button
            {
                Text = "OK",
                Font = new Font("Segoe UI", 9f),
                Left = 345,
                Top = 175,
                Width = 85,
                Height = 32,
                DialogResult = DialogResult.OK
            };
            btnOk.Click += (s, e) => this.Close();

            this.Controls.Add(lblHeader);
            this.Controls.Add(lblDetails);
            this.Controls.Add(btnGoToSite);
            this.Controls.Add(btnOk);

            this.AcceptButton = btnGoToSite;
            this.CancelButton = btnOk;
        }
    }
}
