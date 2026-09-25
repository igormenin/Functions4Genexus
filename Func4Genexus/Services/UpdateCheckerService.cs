using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace Func4Genexus.Services
{
    public static class UpdateCheckerService
    {
        private static readonly Lazy<Version> _currentVersion = new Lazy<Version>(() =>
        {
            try
            {
                var ver = typeof(UpdateCheckerService).Assembly.GetName().Version;
                if (ver != null && (ver.Major > 0 || ver.Minor > 0 || ver.Build > 0))
                {
                    return new Version(ver.Major, ver.Minor, Math.Max(0, ver.Build));
                }
            }
            catch { }
            return new Version(0, 2, 0);
        });

        public static Version CurrentVersion => _currentVersion.Value;
        public static string CurrentVersionString => $"{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

        private const string GitHubApiUrl = "https://api.github.com/repos/igormenin/Functions4Genexus/releases/latest";
        private const string GitHubReleasesPage = "https://github.com/igormenin/Functions4Genexus/releases/latest";
        private const string UserAgent = "Func4Genexus-Extension";

        private static bool _hasCheckedInThisSession = false;

#if GX18_U14
        public const string EnvironmentName = "GeneXus 18 Upgrade 14 ou superior";
        public const string RecommendedZipName = "Func4Genexus_GX18_U14plus.zip";
#else
        public const string EnvironmentName = "GeneXus 18 Upgrade 1 até 13";
        public const string RecommendedZipName = "Func4Genexus_GX18_U1_to_U13.zip";
#endif

        /// <summary>
        /// Inicia uma verificação assíncrona em segundo plano logo após a inicialização do GeneXus.
        /// Aguarda alguns segundos para não competir com a renderização inicial da IDE.
        /// </summary>
        public static void CheckForUpdatesInBackground()
        {
            if (_hasCheckedInThisSession) return;
            _hasCheckedInThisSession = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // Pequeno atraso para a IDE do GeneXus carregar completamente a interface
                    Thread.Sleep(4000);
                    PerformCheck(silentOnUpToDate: true);
                }
                catch
                {
                    // Falha silenciosa em background (sem conexão, offline, etc.)
                }
            });
        }

        /// <summary>
        /// Executa a verificação. Se silentOnUpToDate for false, exibe mensagem caso já esteja atualizado.
        /// </summary>
        public static void PerformCheck(bool silentOnUpToDate)
        {
            try
            {
                // Força o uso de TLS 1.2 para compatibilidade com o GitHub
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var request = (HttpWebRequest)WebRequest.Create(GitHubApiUrl);
                request.Method = "GET";
                request.UserAgent = UserAgent;
                request.Timeout = 6000;
                request.ReadWriteTimeout = 6000;
                request.Accept = "application/vnd.github+json";

                string jsonResponse;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        if (!silentOnUpToDate)
                        {
                            MessageBox.Show("Não foi possível verificar atualizações no momento (Código: " + response.StatusCode + ").",
                                "Func4Genexus - Atualizações", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        return;
                    }

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        jsonResponse = reader.ReadToEnd();
                    }
                }

                var releaseObj = JObject.Parse(jsonResponse);
                string tagName = releaseObj["tag_name"]?.ToString() ?? "";
                string releaseUrl = releaseObj["html_url"]?.ToString() ?? GitHubReleasesPage;

                string normalizedTag = tagName.Trim().TrimStart('v', 'V');
                if (Version.TryParse(normalizedTag, out Version latestVersion))
                {
                    if (latestVersion > CurrentVersion)
                    {
                        NotifyUpdateAvailable(latestVersion, releaseUrl);
                    }
                    else if (!silentOnUpToDate)
                    {
                        MessageBox.Show(
                            $"Você já está utilizando a versão mais recente do Func4Genexus!\n\n" +
                            $"• Versão instalada: v{CurrentVersionString}\n" +
                            $"• Ambiente: {EnvironmentName}",
                            "Func4Genexus - Atualizado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
                else if (!silentOnUpToDate)
                {
                    MessageBox.Show("Não foi possível identificar o formato da versão mais recente no GitHub.",
                        "Func4Genexus - Atualizações", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (!silentOnUpToDate)
                {
                    MessageBox.Show(
                        "Erro ao conectar com o GitHub para verificar atualizações:\n" + ex.Message,
                        "Func4Genexus - Atualizações",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        private static void NotifyUpdateAvailable(Version latestVersion, string releaseUrl)
        {
            try
            {
                using (var form = new Func4Genexus.UI.UpdateNotificationForm(
                    CurrentVersionString,
                    latestVersion.ToString(),
                    EnvironmentName,
                    RecommendedZipName,
                    releaseUrl))
                {
                    form.ShowDialog();
                }
            }
            catch
            {
                // Fallback caso ocorra algum problema de thread/UI
                var msg = $"Nova versão do Func4Genexus disponível: v{latestVersion}!\n\n" +
                          $"Para seu ambiente ({EnvironmentName}), baixe: {RecommendedZipName}\n\n" +
                          $"Deseja ir para a página de releases no GitHub?";

                if (MessageBox.Show(msg, "Atualização - Func4Genexus", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    try { Process.Start(releaseUrl ?? GitHubReleasesPage); } catch { }
                }
            }
        }
    }
}
