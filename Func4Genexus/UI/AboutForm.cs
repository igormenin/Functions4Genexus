using System;
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
            this.Size = new Size(420, 340);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblTitle = new Label { 
                Text = "Func4Genexus", 
                Font = new Font("Segoe UI", 16, FontStyle.Bold), 
                Left = 20, Top = 16, Width = 360, Height = 32
            };

            var lblVersion = new Label { 
                Text = $"Versão {UpdateCheckerService.CurrentVersionString} ({UpdateCheckerService.EnvironmentName})", 
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Left = 20, Top = 48, Width = 360, Height = 20,
                ForeColor = Color.DarkSlateGray
            };

            var lblFeaturesTitle = new Label { 
                Text = "Funcionalidades disponíveis:", 
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), 
                Left = 20, Top = 82, Width = 360, Height = 20 
            };

            var lblFeatures = new Label { 
                Text = "• Gerar SDT a partir da Transaction\n• Seleção de localização de salvamento (Módulo / Pasta)\n• Rastreamento inteligente por GUID (imune a renomeação/movimentação)\n• Mapeamento automático de tipos de dados (AttributeBasedOn)\n• Controle automático de hierarquia e subníveis\n• Gerenciamento e Histórico de SDTs vinculados\n• Totalmente integrado à IDE (Menu de Contexto)", 
                Font = new Font("Segoe UI", 8.5f),
                Left = 25, Top = 106, Width = 360, Height = 125 
            };

            var btnCheckUpdates = new Button { 
                Text = "Verificar Atualizações...", 
                Left = 20, Top = 250, Width = 170, Height = 32 
            };
            btnCheckUpdates.Click += (s, e) =>
            {
                btnCheckUpdates.Enabled = false;
                try
                {
                    UpdateCheckerService.PerformCheck(silentOnUpToDate: false);
                }
                finally
                {
                    btnCheckUpdates.Enabled = true;
                }
            };

            var btnClose = new Button { 
                Text = "Fechar", 
                Left = 285, Top = 250, Width = 95, Height = 32,
                DialogResult = DialogResult.OK 
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblVersion);
            this.Controls.Add(lblFeaturesTitle);
            this.Controls.Add(lblFeatures);
            this.Controls.Add(btnCheckUpdates);
            this.Controls.Add(btnClose);

            this.AcceptButton = btnClose;
        }
    }
}
