using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Func4Genexus.Services;

namespace Func4Genexus.UI
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            this.Text = "Sobre o Func4Genexus";
            this.Size = new Size(460, 405);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblTitle = new Label { 
                Text = "Func4Genexus", 
                Font = new Font("Segoe UI", 16, FontStyle.Bold), 
                Left = 20, Top = 16, Width = 400, Height = 32
            };

            var lblVersion = new Label { 
                Text = $"Versão {UpdateCheckerService.CurrentVersionString} ({UpdateCheckerService.EnvironmentName})", 
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Left = 20, Top = 48, Width = 400, Height = 20,
                ForeColor = Color.DarkSlateGray
            };

            var lblFeaturesTitle = new Label { 
                Text = "Funcionalidades disponíveis:", 
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), 
                Left = 20, Top = 82, Width = 400, Height = 20 
            };

            var lblFeatures = new Label { 
                Text = "• Gerar SDT a partir da Transaction (Menu Principal e de Contexto)\n" +
                       "• Seleção de localização de salvamento (Módulo / Pasta)\n" +
                       "• Rastreamento inteligente por GUID (imune a renomeação/movimentação)\n" +
                       "• Mapeamento automático de tipos de dados (AttributeBasedOn)\n" +
                       "• Controle automático de hierarquia, subníveis e histórico de SDTs\n" +
                       "• Temas visuais para a IDE (Claro, Escuro, Dracula, Catppuccin Mocha,\n" +
                       "   Tokyo Night, Nord, Gruvbox Dark, One Dark Pro e Rosé Pine)\n" +
                       "• Persistência automática do tema escolhido ao iniciar o GeneXus\n" +
                       "• Ferramentas de diagnóstico e inspeção visual de controles da IDE", 
                Font = new Font("Segoe UI", 8.5f),
                Left = 25, Top = 106, Width = 400, Height = 190 
            };

            // Botão 1: Ir para o site (abre o navegador e fecha o popup imediatamente)
            var btnGoToSite = new Button { 
                Text = "Ir para o site", 
                Font = new Font("Segoe UI", 8.5f),
                Left = 20, Top = 315, Width = 115, Height = 32 
            };
            btnGoToSite.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/igormenin/Functions4Genexus/releases",
                        UseShellExecute = true
                    });
                }
                catch { }

                // Fecha o popup imediatamente após redirecionar para o site
                this.Close();
            };

            // Botão 2: Verificar Atualizações (se encontrar update e clicar em Ir para o site, fecha o About também)
            var btnCheckUpdates = new Button { 
                Text = "Verificar Atualizações...", 
                Font = new Font("Segoe UI", 8.5f),
                Left = 145, Top = 315, Width = 180, Height = 32 
            };
            btnCheckUpdates.Click += (s, e) =>
            {
                btnCheckUpdates.Enabled = false;
                try
                {
                    bool redirectedToSite = UpdateCheckerService.PerformCheck(silentOnUpToDate: false);
                    if (redirectedToSite)
                    {
                        // Se o usuário clicou em 'Ir para o site' na notificação, fecha o About para não deixar nada aberto
                        this.Close();
                    }
                }
                finally
                {
                    btnCheckUpdates.Enabled = true;
                }
            };

            // Botão 3: Fechar
            var btnClose = new Button { 
                Text = "Fechar", 
                Font = new Font("Segoe UI", 8.5f),
                Left = 335, Top = 315, Width = 90, Height = 32,
                DialogResult = DialogResult.OK 
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblVersion);
            this.Controls.Add(lblFeaturesTitle);
            this.Controls.Add(lblFeatures);
            this.Controls.Add(btnGoToSite);
            this.Controls.Add(btnCheckUpdates);
            this.Controls.Add(btnClose);

            this.CancelButton = btnClose;
        }
    }
}
