using System;
using Artech.Architecture.Common.Services;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Wrapper sobre a janela de Output do GeneXus. No GeneXus 18 os metodos exigem
	/// outputId/sectionId; esta classe esconde esses detalhes e PADRONIZA as mensagens.
	///
	/// APPEND-ONLY: o Output "General" so ACUMULA — esta classe NUNCA limpa o conteudo
	/// (ver comentario no construtor). Isso e requisito do projeto.
	///
	/// Dois conjuntos de metodos:
	///  - Primitivos (mantidos por compatibilidade, SEM timestamp): Line/Warn/Error(text)/End.
	///  - Padronizados (com timestamp [HH:mm:ss] e marcador de nivel): Info/Success/Step/
	///    Section/Summary/Error(text, ex). A formatacao fica num unico ponto (Fmt).
	/// </summary>
	public sealed class GxOutput
	{
		private readonly IOutputService _svc;
		private readonly string _outputId;
		private readonly string _section;
		private readonly System.Diagnostics.Stopwatch _sw;

		/// <summary>Espelho OPCIONAL: se setado, cada linha tambem e enviada aqui (ex.: a janela de
		/// atividade do import, para o usuario acompanhar sem olhar o Output). Best-effort, nunca lanca.</summary>
		internal Action<string> Sink;
		private void Tee(string s) { Action<string> k = Sink; if (k != null) { try { k(s); } catch { } } }

		public GxOutput(string section) : this(section, true, null) { }

		/// <summary>
		/// Modo HEADLESS (ex.: MSBuild task fora da IDE): SEM o servico de Output da IDE. Cada linha vai
		/// apenas para o <paramref name="sink"/> (ex.: o Log do MSBuild). As operacoes (Export/Import/...)
		/// continuam recebendo um GxOutput normal — so que aqui ele nao toca a IDE.
		/// </summary>
		public static GxOutput ForSink(string section, Action<string> sink) { return new GxOutput(section, false, sink); }

		private GxOutput(string section, bool useIdeOutput, Action<string> sink)
		{
			_section = section;
			_sw = System.Diagnostics.Stopwatch.StartNew(); // mede a duracao da acao (mostrada no End)
			Sink = sink;
			if (useIdeOutput)
			{
				_svc = CommonServices.Output;
				_outputId = _svc.GetDefaultOutputId(); // extension de OutputService2Extensions (= "General")
				// NAO limpar: tudo vai para o Output "General" e so ACUMULA (append).
				_svc.StartSection(_outputId, _section, _section);
			}
		}

		/// <summary>Tempo decorrido desde a criacao deste GxOutput (inicio da acao).</summary>
		public TimeSpan Elapsed { get { return _sw.Elapsed; } }

		/// <summary>Abre/seleciona a janela de Output (General) para o usuario acompanhar o progresso.</summary>
		public void Show()
		{
			if (_svc == null) return;
			try { _svc.Show(_outputId); _svc.SelectOutput(_outputId); }
			catch { /* best-effort: nao falhar so por causa da janela */ }
		}

		/// <summary>Escreve uma linha no Output da IDE (se houver) e SEMPRE no espelho/sink.</summary>
		private void Emit(string s) { if (_svc != null) _svc.AddLine(_outputId, s); Tee(s); }

		// ===== Primitivos (compatibilidade — SEM timestamp; nao alterar comportamento) =====
		public void Line(string text) { Emit(text); }
		public void Warn(string text) { if (_svc != null) _svc.AddWarningLine(_outputId, text); Tee("AVISO " + text); }
		public void Error(string text) { if (_svc != null) _svc.AddErrorLine(_outputId, text); Tee("ERRO " + text); }
		public void End(bool success)
		{
			if (_sw != null && _sw.IsRunning) _sw.Stop();
			if (_sw != null) Emit(Fmt("==", "Tempo total: " + DurationFormat.Format(_sw.Elapsed)));
			if (_svc != null) _svc.EndSection(_outputId, _section, _section, success);
		}

		// ===== Padronizados (timestamp + nivel; formatacao centralizada em Fmt) =====

		/// <summary>Mensagem informativa: [HH:mm:ss] texto.</summary>
		public void Info(string text) { Emit(Fmt(null, text)); }

		/// <summary>Sucesso: [HH:mm:ss] OK texto (linha normal).</summary>
		public void Success(string text) { Emit(Fmt("OK", text)); }

		/// <summary>Etapa/passo: [HH:mm:ss] >> texto.</summary>
		public void Step(string text) { Emit(Fmt(">>", text)); }

		/// <summary>Aviso padronizado: [HH:mm:ss] AVISO texto (canal de warning).</summary>
		public void WarnLine(string text) { string s = Fmt("AVISO", text); if (_svc != null) _svc.AddWarningLine(_outputId, s); Tee(s); }

		/// <summary>Erro padronizado com excecao resumida (tipo: mensagem), SEM stack trace.</summary>
		public void Error(string text, Exception ex)
		{
			string msg = ex == null ? text : text + " — " + Summarize(ex);
			string s = Fmt("ERRO", msg);
			if (_svc != null) _svc.AddErrorLine(_outputId, s); Tee(s);
		}

		/// <summary>Cabecalho de uma sub-secao visual: linha em branco + [HH:mm:ss] == titulo.</summary>
		public void Section(string title)
		{
			if (_svc != null) _svc.AddLine(_outputId, string.Empty);
			Emit(Fmt("==", title));
		}

		/// <summary>Resumo final: cabecalho + itens indentados (ex.: contagens de um dry-run).</summary>
		public void Summary(string title, params string[] items)
		{
			Section(title);
			if (items != null)
				foreach (string it in items) Emit("   - " + (it ?? string.Empty));
		}

		// ===== Formatacao centralizada =====
		private static string Fmt(string level, string text)
		{
			string ts = DateTime.Now.ToString("HH:mm:ss");
			return string.IsNullOrEmpty(level)
				? string.Format("[{0}] {1}", ts, text)
				: string.Format("[{0}] {1} {2}", ts, level, text);
		}

		/// <summary>Resumo curto de uma excecao (Tipo: Mensagem), sem stack trace.</summary>
		private static string Summarize(Exception ex)
		{
			return ex == null ? string.Empty : ex.GetType().Name + ": " + ex.Message;
		}
	}
}
