using System;
using System.Drawing;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Regra de cor do tema escuro fora do Infragistics (WinForms, editor, Properties). Mesma formula do .isl
	/// (IslDarkTheme.InvertLightness): fundo claro escurece, texto escuro clareia, o resto fica; a paleta em uso
	/// (ThemePalette.Current) da os papeis fixos e, no Dracula, o tom. Ver secao 4 do guia e secao 4 do guia.
	/// </summary>
	internal static class DarkPalette
	{
		/// <summary>
		/// Fundo: escurece se for claro (brilho HSL &gt; 0,6) e tira a maior parte do matiz: pastel claro invertido
		/// vira marrom/oliva/azul-marinho (creme do Layout = 47,40,27; Info da dica = 81,81,0), entao fica so
		/// <see cref="BackHueKept"/> da cor sobre o cinza. Transparente/vazio/escuro fica.
		/// </summary>
		internal static Color Back(Color c)
		{
			if (!IsLight(c))
				return c;
			ThemePalette p = ThemePalette.Current;
			return p.Tinted ? Color.FromArgb(c.A, p.Tint(c, Invert(c))) : Neutralize(Invert(c), BackHueKept);
		}

		internal const double BackHueKept = 0.1;

		/// <summary>Mistura a cor com o cinza de mesma luminancia (0 = cinza, 1 = igual). PURO.</summary>
		internal static Color Neutralize(Color c, double kept)
		{
			double gray = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
			Func<int, int> mix = v => Math.Max(0, Math.Min(255, (int)Math.Round(gray + (v - gray) * kept)));
			return Color.FromArgb(c.A, mix(c.R), mix(c.G), mix(c.B));
		}

		/// <summary>
		/// Texto: clareia se for escuro a olho (luminancia &lt; 0,45: azul puro e verde 0,128,0 contam), com
		/// luminosidade minima 0,70 para ler sobre o fundo escuro. Matiz fica (palavra-chave azul vira azul claro).
		/// </summary>
		internal static Color Fore(Color c)
		{
			if (!Visible(c) || Luminance(c) >= 0.45)
				return c;
			ThemePalette p = ThemePalette.Current;
			return Color.FromArgb(c.A, p.Tinted ? p.TextFor(c) : IslDarkTheme.InvertLightness(c, MinTextLightness));
		}

		private const double MinTextLightness = 0.70;

		/// <summary>Luminancia percebida (Rec. 709, sem gama): 0 = preto, 1 = branco.</summary>
		internal static double Luminance(Color c)
		{
			return (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
		}

		/// <summary>Barra de rolagem escura: trilho/botoes, polegar e seta (papeis da paleta em uso).</summary>
		internal static Color ScrollTrack { get { return ThemePalette.Current.ScrollTrack; } }
		internal static Color ScrollThumb { get { return ThemePalette.Current.ScrollThumb; } }
		internal static Color ScrollArrow { get { return ThemePalette.Current.ScrollArrow; } }

		/// <summary>Traco discreto (linha de grade, borda fina) sobre o fundo escuro.</summary>
		internal static Color Line { get { return ThemePalette.Current.Line; } }

		/// <summary>Texto de controle desabilitado: apagado mas legivel no fundo escuro.</summary>
		internal static Color DisabledText { get { return ThemePalette.Current.TextDim; } }

		/// <summary>Clareia a cor em direcao ao branco (0 = igual, 1 = branco). Traco/realce sutil sobre fundo escuro.</summary>
		internal static Color Lift(Color c, double amount)
		{
			Func<int, int> up = v => (int)Math.Round(v + (255 - v) * amount);
			return Color.FromArgb(c.A, up(c.R), up(c.G), up(c.B));
		}

		/// <summary>true se a cor e um fundo claro (o que o tema escuro troca).</summary>
		internal static bool IsLight(Color c)
		{
			return Visible(c) && c.GetBrightness() > 0.6f;
		}

		private static bool Visible(Color c)
		{
			return !c.IsEmpty && c.A > 0;
		}

		private static Color Invert(Color c)
		{
			return Color.FromArgb(c.A, IslDarkTheme.InvertLightness(c));
		}
	}
}
