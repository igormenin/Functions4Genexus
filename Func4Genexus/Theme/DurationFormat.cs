using System;
using System.Globalization;
using System.Text;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Formatacao PURA de duracao (sem WinForms nem API GeneXus) — usada no Output (todas as acoes) e nas
	/// janelas de progresso. Formato: "1d 2h 3m 4s" (omite unidades zero a esquerda); abaixo de 1 min mostra
	/// "1,2 s"; abaixo de 1 s mostra "350 ms". Cultura invariante (ponto decimal). Negativo vira zero.
	/// </summary>
	internal static class DurationFormat
	{
		public static string Format(TimeSpan t)
		{
			if (t.Ticks < 0)
				t = TimeSpan.Zero;
			CultureInfo ci = CultureInfo.InvariantCulture;
			if (t.TotalSeconds < 1)
				return string.Format(ci, "{0} ms", (int)t.TotalMilliseconds);
			if (t.TotalSeconds < 60)
				return string.Format(ci, "{0:0.0} s", t.TotalSeconds);
			StringBuilder sb = new StringBuilder();
			if (t.Days > 0) sb.Append(t.Days).Append("d ");
			if (t.Days > 0 || t.Hours > 0) sb.Append(t.Hours).Append("h ");
			sb.Append(t.Minutes).Append("m ").Append(t.Seconds).Append("s");
			return sb.ToString();
		}
	}
}
