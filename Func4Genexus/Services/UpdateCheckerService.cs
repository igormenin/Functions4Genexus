using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
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

        private const string GitHubLatestReleaseApiUrl = "https://api.github.com/repos/igormenin/Functions4Genexus/releases/latest";
        private const string GitHubReleasesListApiUrl = "https://api.github.com/repos/igormenin/Functions4Genexus/releases?per_page=5";
        private const string GitHubTagsApiUrl = "https://api.github.com/repos/igormenin/Functions4Genexus/tags?per_page=5";
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
        /// Retorna true se o usuário foi redirecionado para o site do GitHub.
        /// </summary>
        public static bool PerformCheck(bool silentOnUpToDate)
        {
            try
            {
                // Força o uso de TLS 1.2 para compatibilidade com o GitHub
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                if (TryGetLatestVersionFromGitHub(out Version latestVersion, out string releaseUrl, out string statusError))
                {
                    if (latestVersion > CurrentVersion)
                    {
                        return NotifyUpdateAvailable(latestVersion, releaseUrl);
                    }
                    else if (!silentOnUpToDate)
                    {
                        MessageBox.Show(
                            $"Você já está utilizando a versão mais recente do Func4Genexus!\n\n" +
                            $"• Versão instalada: v{CurrentVersionString}\n" +
                            $"• Última versão no GitHub: v{FormatVersion3(latestVersion)}\n" +
                            $"• Ambiente: {EnvironmentName}",
                            "Func4Genexus - Atualizado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
                else if (!silentOnUpToDate)
                {
                    MessageBox.Show(
                        statusError ?? "Não foi possível identificar o formato da versão mais recente no GitHub.",
                        "Func4Genexus - Atualizações",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
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
            return false;
        }

        /// <summary>
        /// Consulta releases/latest, lista de releases e tags do repositório (ignorando cache local e CDN)
        /// e retorna a maior versão disponível. Assim, mesmo que o GitHub Actions ainda esteja gerando a Release
        /// após o git push da tag, a versão nova já é detectada imediatamente.
        /// </summary>
        private static bool TryGetLatestVersionFromGitHub(out Version bestVersion, out string bestUrl, out string statusError)
        {
            bestVersion = null;
            bestUrl = GitHubReleasesPage;
            statusError = null;

            // 1. Consulta a release latest oficial
            if (TryFetchJson(GitHubLatestReleaseApiUrl, out string latestJson, out statusError))
            {
                var releaseObj = JObject.Parse(latestJson);
                string tagName = releaseObj["tag_name"]?.ToString() ?? "";
                string htmlUrl = releaseObj["html_url"]?.ToString() ?? GitHubReleasesPage;
                if (TryParseTagVersion(tagName, out Version v))
                {
                    bestVersion = v;
                    bestUrl = htmlUrl;
                }
            }

            // 2. Se já encontrou uma versão maior que a instalada, retorna imediatamente;
            //    caso contrário, confere também a lista de releases e de tags (cobre o intervalo de 1-2 min do GitHub Actions
            //    ou eventual cache do endpoint /releases/latest).
            if (bestVersion == null || bestVersion <= CurrentVersion)
            {
                if (TryFetchJson(GitHubReleasesListApiUrl, out string listJson, out _))
                {
                    var releasesArr = JArray.Parse(listJson);
                    foreach (var item in releasesArr)
                    {
                        if (item["draft"]?.Value<bool>() == true) continue;
                        string tagName = item["tag_name"]?.ToString() ?? "";
                        string htmlUrl = item["html_url"]?.ToString() ?? GitHubReleasesPage;
                        if (TryParseTagVersion(tagName, out Version v) && (bestVersion == null || v > bestVersion))
                        {
                            bestVersion = v;
                            bestUrl = htmlUrl;
                        }
                    }
                }

                if (bestVersion == null || bestVersion <= CurrentVersion)
                {
                    if (TryFetchJson(GitHubTagsApiUrl, out string tagsJson, out _))
                    {
                        var tagsArr = JArray.Parse(tagsJson);
                        foreach (var item in tagsArr)
                        {
                            string tagName = item["name"]?.ToString() ?? "";
                            if (TryParseTagVersion(tagName, out Version v) && (bestVersion == null || v > bestVersion))
                            {
                                bestVersion = v;
                                bestUrl = $"https://github.com/igormenin/Functions4Genexus/releases/tag/{tagName.Trim()}";
                            }
                        }
                    }
                }
            }

            return bestVersion != null;
        }

        private static bool TryFetchJson(string baseUrl, out string jsonResponse, out string statusError)
        {
            jsonResponse = null;
            statusError = null;
            try
            {
                string sep = baseUrl.Contains("?") ? "&" : "?";
                string nocacheUrl = $"{baseUrl}{sep}_t={DateTime.UtcNow.Ticks}";

                var request = (HttpWebRequest)WebRequest.Create(nocacheUrl);
                request.Method = "GET";
                request.UserAgent = UserAgent;
                request.Timeout = 6000;
                request.ReadWriteTimeout = 6000;
                request.Accept = "application/vnd.github+json";
                request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                request.Headers[HttpRequestHeader.CacheControl] = "no-cache, no-store, must-revalidate";
                request.Headers[HttpRequestHeader.Pragma] = "no-cache";

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        statusError = "Não foi possível verificar atualizações no momento (Código: " + response.StatusCode + ").";
                        return false;
                    }

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        jsonResponse = reader.ReadToEnd();
                    }
                }
                return !string.IsNullOrWhiteSpace(jsonResponse);
            }
            catch (WebException wex)
            {
                if (wex.Response is HttpWebResponse errResp)
                {
                    statusError = "Não foi possível verificar atualizações no momento (Código: " + errResp.StatusCode + ").";
                }
                else
                {
                    statusError = "Erro ao conectar com o GitHub para verificar atualizações:\n" + wex.Message;
                }
                return false;
            }
        }

        private static bool TryParseTagVersion(string tagName, out Version parsedVersion)
        {
            parsedVersion = null;
            if (string.IsNullOrWhiteSpace(tagName)) return false;

            string normalized = tagName.Trim().TrimStart('v', 'V');
            int dashIdx = normalized.IndexOf('-');
            if (dashIdx > 0) normalized = normalized.Substring(0, dashIdx);

            if (Version.TryParse(normalized, out Version v))
            {
                parsedVersion = new Version(v.Major, v.Minor, Math.Max(0, v.Build));
                return true;
            }
            return false;
        }

        private static string FormatVersion3(Version v)
        {
            if (v == null) return "0.0.0";
            return $"{v.Major}.{v.Minor}.{Math.Max(0, v.Build)}";
        }

        private static bool NotifyUpdateAvailable(Version latestVersion, string releaseUrl)
        {
            string latestVersionStr = FormatVersion3(latestVersion);
            try
            {
                using (var form = new Func4Genexus.UI.UpdateNotificationForm(
                    CurrentVersionString,
                    latestVersionStr,
                    EnvironmentName,
                    RecommendedZipName,
                    releaseUrl))
                {
                    return form.ShowDialog() == DialogResult.Yes;
                }
            }
            catch
            {
                // Fallback caso ocorra algum problema de thread/UI
                var msg = $"Nova versão do Func4Genexus disponível: v{latestVersionStr}!\n\n" +
                          $"Para seu ambiente ({EnvironmentName}), baixe: {RecommendedZipName}\n\n" +
                          $"Deseja ir para a página de releases no GitHub?";

                if (MessageBox.Show(msg, "Atualização - Func4Genexus", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = releaseUrl ?? GitHubReleasesPage,
                            UseShellExecute = true
                        });
                        return true;
                    }
                    catch { }
                }
                return false;
            }
        }
    }
}
