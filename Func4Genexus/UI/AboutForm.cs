using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace Func4Genexus.UI
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            this.Text = "Sobre o Func4Genexus";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var version = "0.1.0";

            var lblTitle = new Label { 
                Text = "Func4Genexus", 
                Font = new Font("Segoe UI", 16, FontStyle.Bold), 
                Left = 20, Top = 20, Width = 350, Height = 30
            };

            var lblVersion = new Label { 
                Text = "Versão " + version, 
                Left = 20, Top = 50, Width = 350, Height = 20 
            };

            var lblFeaturesTitle = new Label { 
                Text = "Funcionalidades disponíveis:", 
                Font = new Font("Segoe UI", 10, FontStyle.Bold), 
                Left = 20, Top = 90, Width = 350, Height = 20 
            };

            var lblFeatures = new Label { 
                Text = "• Gerar SDT a partir da Transaction\n• Mapeamento automático de tipos de dados (AttributeBasedOn)\n• Controle automático de hierarquia e subníveis\n• Gerenciamento e Histórico de SDTs vinculados\n• Totalmente integrado à IDE (Menu de Contexto)", 
                Left = 30, Top = 120, Width = 340, Height = 100 
            };

            var btnClose = new Button { Text = "Fechar", Left = 145, Top = 220, Width = 90 };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblVersion);
            this.Controls.Add(lblFeaturesTitle);
            this.Controls.Add(lblFeatures);
            this.Controls.Add(btnClose);
        }
    }
}

