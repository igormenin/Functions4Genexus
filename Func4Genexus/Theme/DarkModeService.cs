using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Tema escuro da IDE: liga/desliga o .isl escuro derivado do Silver da instalacao (Infragistics) e a
	/// fase 2 (DarkWindows: WinForms, editor Actipro, Properties, barra de titulo), e grava a escolha (arquivo-flag em
	/// %LOCALAPPDATA%\GxPropertiesTool), reaplicada no startup. Nada da instalacao e alterado. Ver secao 4 do guia.
	/// </summary>
	internal static class DarkModeService
	{
		private const string IslFileName = "GenexusOffice2010Silver.isl";

		private static string FlagPath() { return Path.Combine(UpdateState.Dir(), "darkmode.on"); }

		/// <summary>true se um tema escuro (Escuro ou Dracula) esta escolhido (arquivo-flag existe).</summary>
		internal static bool Enabled
		{
			get { try { return File.Exists(FlagPath()); } catch { return false; } }
		}

		/// <summary>
		/// Tema gravado: sem arquivo = Claro; o arquivo guarda o nome (Dracula) - o antigo guardava a data, que vale Escuro.
		/// </summary>
		internal static string CurrentTheme
		{
			get
			{
				try { return File.Exists(FlagPath()) ? ThemePalette.ByName(File.ReadAllText(FlagPath())).Name : ThemePalette.LightName; }
				catch { return ThemePalette.LightName; }
			}
		}

		/// <summary>
		/// Troca o tema (Claro/Escuro/Dracula), aplica e grava. Entre dois escuros desfaz o atual (DarkUndo) e aplica o
		/// novo sem reiniciar. Falha = Output, gravacao fica como estava.
		/// </summary>
		internal static void SetTheme(string name)
		{
			string current = CurrentTheme;
			if (string.Equals(current, name, StringComparison.OrdinalIgnoreCase))
				return;
			GxOutput log = new GxOutput("Tema escuro");
			try
			{
				bool dark = name != ThemePalette.LightName;
				if (dark && current != ThemePalette.LightName)
					DarkWindows.Stop(); // devolve as cores do tema anterior antes de pintar com a nova paleta
				if (dark)
					ThemePalette.Current = ThemePalette.ByName(name);
				Apply(dark, log);
				SetFlag(dark ? ThemePalette.Current.Name : null);
				log.Success(dark
					? "Tema " + ThemePalette.Current.Name + " aplicado. Fica gravado para o proximo startup. O que seguir claro: Instalacao -> Tema escuro: inspecionar controles."
					: "Tema Claro: voltou ao tema original da IDE (" + IslFileName + ").");
			}
			catch (Exception ex)
			{
				log.Error("Nao consegui aplicar o tema " + name + ".", ex);
			}
		}

		/// <summary>No startup (Package.Initialize): aplica o tema escuro gravado. Best-effort, nunca lanca.</summary>
		internal static void ApplyAtStartupIfEnabled()
		{
			if (!Enabled)
				return;
			try
			{
				ThemePalette.Current = ThemePalette.ByName(File.ReadAllText(FlagPath()));
				Apply(true, null);
			}
			catch (Exception ex)
			{
				try { new GxOutput("Tema escuro").Error("Tema escuro gravado como LIGADO, mas falhou no startup; a IDE segue no tema original.", ex); }
				catch { }
			}
		}

		private static void Apply(bool dark, GxOutput log)
		{
			string path = Path.Combine(Application.StartupPath, IslFileName);
			if (!File.Exists(path))
				throw new FileNotFoundException("Tema da IDE nao encontrado.", path);

			XmlDocument isl = new XmlDocument();
			isl.Load(path);
			if (dark)
				isl = IslDarkTheme.Darken(isl, ThemePalette.Current);
			IslDarkTheme.ScaleThumbs(isl, ScreenDpiY());

			if (log != null)
				log.Step((dark ? "Derivando tema escuro de " : "Recarregando tema original ") + path);
			InfragisticsStyler.Load(isl);
			InfragisticsStyler.SetOffice2007Scheme(dark ? "Black" : "Silver");

			if (dark)
			{
				int painted = DarkWindows.Start(Trace);
				if (log != null)
					log.Step("Fora do Infragistics (WinForms, editor, Properties, barra de titulo): " + painted + " objeto(s) tratados; janelas novas seguem o tema.");
			}
			else
			{
				int failures = DarkWindows.Stop();
				if (log != null)
					log.Step("Cores originais devolvidas fora do Infragistics" + (failures > 0 ? " (" + failures + " falha(s) em controle ja fechado)." : "."));
			}
		}

		private const int DelayedInspectSeconds = 10;

		/// <summary>
		/// Inspecao adiada: dialogo modal (Ctrl+O, Select Objects, Options) bloqueia o menu; da tempo de abrir a janela.
		/// Timer de UI de uma vez so. Nunca lanca.
		/// </summary>
		internal static void InspectLater()
		{
			try
			{
				new GxOutput("Tema escuro: inspecao").Info("Inspecao em " + DelayedInspectSeconds
					+ " s: abra agora a janela ou dialogo que segue claro e espere.");
				Timer once = new Timer { Interval = DelayedInspectSeconds * 1000 };
				once.Tick += delegate { once.Stop(); once.Dispose(); Inspect(); };
				once.Start();
			}
			catch (Exception ex)
			{
				try { new GxOutput("Tema escuro: inspecao").Error("Nao consegui agendar a inspecao.", ex); }
				catch { }
			}
		}

		private const int PointInspectSeconds = 5;

		/// <summary>
		/// Em 5 s le o ponto sob o mouse: o dono aponta a linha/borda clara e o Output diz quem a desenha (controle,
		/// UIElement e role do Infragistics, cores em volta). Timer de UI de uma vez so. Nunca lanca.
		/// </summary>
		internal static void InspectPointLater()
		{
			GxOutput log = new GxOutput("Tema escuro: inspecao");
			try
			{
				log.Info("Em " + PointInspectSeconds + " s: deixe a ponta do mouse em cima da parte clara (linha, borda) e nao mexa.");
				Timer once = new Timer { Interval = PointInspectSeconds * 1000 };
				once.Tick += delegate
				{
					once.Stop();
					once.Dispose();
					Point at = Cursor.Position;
					log.Summary("Sob o mouse em " + at.X + "," + at.Y, ControlInspector.DescribePoint(at).ToArray());
				};
				once.Start();
			}
			catch (Exception ex)
			{
				try { log.Error("Nao consegui agendar a inspecao do ponto.", ex); }
				catch { }
			}
		}

		/// <summary>Grava o relatorio do inspetor e resume no Output o que segue claro. Nunca lanca.</summary>
		internal static void Inspect()
		{
			GxOutput log = new GxOutput("Tema escuro: inspecao");
			try
			{
				List<InspectedControl> nodes = ControlInspector.Collect();
				string path = Path.Combine(UpdateState.Dir(), "darkmode-inspecao-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
				string header = "Inspecao do tema escuro " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
					+ " | tema " + (Enabled ? "LIGADO" : "DESLIGADO") + " | " + nodes.Count + " controle(s) em "
					+ DarkWindows.Snapshot().Count + " janela(s)";
				File.WriteAllText(path, ControlInspector.Format(nodes, header));

				List<string> light = ControlInspector.Summarize(nodes, 25);
				log.Step(header);
				if (light.Count == 0)
					log.Info("Nenhum controle visivel fora do Infragistics/web com fundo claro.");
				else
					log.Summary("Tipos que seguem com fundo claro (visiveis)", light.ToArray());
				log.Success("Relatorio completo: " + path);
			}
			catch (Exception ex)
			{
				log.Error("Nao consegui inspecionar os controles.", ex);
			}
		}

		private static GxOutput _trace;

		/// <summary>Rastro do tema (o que escapou do ControlAdded): seção propria no Output, uma linha por tipo.</summary>
		private static void Trace(string line)
		{
			if (_trace == null)
				_trace = new GxOutput("Tema escuro: rastro");
			_trace.Info(line);
		}

		private static float ScreenDpiY()
		{
			try { using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) return g.DpiY; }
			catch { return 96f; }
		}

		/// <summary>Grava o nome do tema escuro; null apaga (Claro).</summary>
		private static void SetFlag(string theme)
		{
			if (theme != null)
				File.WriteAllText(FlagPath(), theme);
			else if (File.Exists(FlagPath()))
				File.Delete(FlagPath());
		}
	}
}
