using System;
using System.Linq;
using System.Runtime.InteropServices;
using Artech.Architecture.Common.Objects;
using Artech.Architecture.Common.Services;
using Artech.Architecture.UI.Framework.Helper;
using Artech.Architecture.UI.Framework.Packages;
using Artech.Architecture.UI.Framework.Services;
using Artech.Common.Framework.Commands;
using Artech.Genexus.Common.Objects;
using Artech.Architecture.Common.Descriptors;
using Func4Genexus.Theme;
using Func4Genexus.UI;
using System.Collections.Generic;

[assembly: Artech.Architecture.Common.Packages.PackageAttribute(typeof(Func4Genexus.SdtGeneratorPackage))]

namespace Func4Genexus
{
    [ComVisible(true)]
    [Guid("4b574246-86c4-4b53-8d02-4fc93f9de27f")]
    public partial class SdtGeneratorPackage : AbstractPackageUI
    {
        public static Guid PackageGuid = new Guid("4b574246-86c4-4b53-8d02-4fc93f9de27f");
        public override string Name => "Func4Genexus";

        public override void Initialize(IGxServiceProvider services)
        {
            base.Initialize(services);

            AddCommand(
                new CommandKey(Id, "Gerar ou Gerenciar SDT"),
                ExecuteGenerateSDT,
                QueryGenerateSDT);

            AddCommand(
                new CommandKey(Id, "Sobre o Func4Genexus"),
                ExecuteAbout,
                QueryAbout);

            RegisterThemeCommand("ThemeLight", ThemePalette.LightName);
            RegisterThemeCommand("ThemeDark", ThemePalette.DarkName);
            RegisterThemeCommand("ThemeDracula", ThemePalette.DraculaName);
            RegisterThemeCommand("ThemeCatppuccinMocha", ThemePalette.CatppuccinMochaName);
            RegisterThemeCommand("ThemeTokyoNight", ThemePalette.TokyoNightName);
            RegisterThemeCommand("ThemeNord", ThemePalette.NordName);
            RegisterThemeCommand("ThemeGruvboxDark", ThemePalette.GruvboxDarkName);
            RegisterThemeCommand("ThemeOneDarkPro", ThemePalette.OneDarkProName);
            RegisterThemeCommand("ThemeRosePine", ThemePalette.RosePineName);

            AddCommand(
                new CommandKey(Id, "InspectDarkMode"),
                delegate { DarkModeService.Inspect(); return true; },
                QuerySimple("Tema escuro: inspecionar controles"));

            AddCommand(
                new CommandKey(Id, "InspectDarkModeLater"),
                delegate { DarkModeService.InspectLater(); return true; },
                QuerySimple("Tema escuro: inspecionar em 10 s (diálogos)"));

            AddCommand(
                new CommandKey(Id, "InspectDarkModePoint"),
                delegate { DarkModeService.InspectPointLater(); return true; },
                QuerySimple("Tema escuro: o que está sob o mouse (em 5 s)"));

            try
            {
                DarkModeService.ApplyAtStartupIfEnabled();
            }
            catch
            {
            }

            // Verifica se há novas versões disponíveis no GitHub em segundo plano
            Func4Genexus.Services.UpdateCheckerService.CheckForUpdatesInBackground();
        }

        private void RegisterThemeCommand(string commandId, string themeName)
        {
            AddCommand(
                new CommandKey(Id, commandId),
                delegate { DarkModeService.SetTheme(themeName); return true; },
                QueryTheme("Tema: " + themeName, () => string.Equals(DarkModeService.CurrentTheme, themeName, StringComparison.OrdinalIgnoreCase)));
        }

        private static QueryHandler QuerySimple(string text)
        {
            return delegate(CommandData data, ref CommandStatus status)
            {
                status = new CommandStatus(CommandState.Enabled);
                status.Text = text;
                return true;
            };
        }

        private static QueryHandler QueryTheme(string text, Func<bool> isChecked)
        {
            return delegate(CommandData data, ref CommandStatus status)
            {
                status = new CommandStatus(CommandState.Enabled);
                status.Text = text;
                try { status.Checked = isChecked(); } catch { status.Checked = false; }
                return true;
            };
        }

        private static bool QueryAbout(CommandData data, ref CommandStatus status)
        {
            status.Visible(true);
            status.Enable(true);
            return true;
        }

        private static bool ExecuteAbout(CommandData data)
        {
            using (var form = new AboutForm())
            {
                form.ShowDialog();
            }
            return true;
        }

        private static bool QueryGenerateSDT(CommandData data, ref CommandStatus status)
        {
            status.Visible(true);
            status.Enable(true); // <--- SEMPRE HABILITADO
            return true;
        }

        private static bool ExecuteGenerateSDT(CommandData data)
        {
            var trn = TryGetTransactionFromContext(data);

            if (trn == null)
            {
                if (UIServices.IsSelectObjectDialogAvailable)
                {
                    var options = new SelectObjectOptions
                    {
                        MultipleSelection = false,
                        DialogTitle = "Selecionar Transaction (Func4Genexus)",
                        SupportCreateAction = false
                    };
                    options.ObjectTypes.Add(KBObjectDescriptor.Get<Transaction>());
                    
                    var selected = UIServices.SelectObjectDialog.SelectObject(options);
                    if (selected != null && selected is Transaction selectedTrn)
                    {
                        trn = selectedTrn;
                    }
                }
            }

            if (trn == null)
            {
                System.Windows.Forms.MessageBox.Show("Por favor, selecione uma Transaction.", "Func4Genexus", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return true;
            }

            using (var form = new SdtManagerForm(trn))
            {
                form.ShowDialog();
            }
            return true;
        }

        private static Transaction TryGetTransactionFromContext(CommandData data)
        {
            if (data?.Context is Transaction t)
                return t;

            if (data?.Context is IEnumerable<KBObject> selectedObjects)
                return selectedObjects.OfType<Transaction>().FirstOrDefault();

            if (data?.Context is IEnumerable<object> objectList)
                return objectList.OfType<Transaction>().FirstOrDefault();

            if (data?.Context is System.Collections.IEnumerable list)
            {
                foreach (var item in list)
                {
                    if (item is Transaction trn)
                        return trn;
                }
            }

            return null;
        }
    }
}
