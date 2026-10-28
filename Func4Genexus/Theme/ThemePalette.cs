using System;
using System.Drawing;
using System.Globalization;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Paleta de um tema escuro: cor por PAPEL (moldura, traco, selecao, barra de rolagem...) e a regra que leva as cores
	/// claras do GeneXus para o tema. PURA e imutavel. Escuro = a formula de sempre (inverte a luminosidade e mantem o
	/// matiz; saida identica a de antes da paleta - teste). Dracula = mesma luminosidade, mas o cinza vira um ponto da
	/// rampa de fundos da paleta e a cor com matiz vira o destaque mais proximo. Ver secao 4 do guia.
	/// </summary>
	internal sealed class ThemePalette
	{
		internal const string LightName = "Claro";
		internal const string DarkName = "Escuro";
		internal const string DraculaName = "Dracula";

		/// <summary>Abaixo disso (saturacao HSL) a cor conta como cinza e vai para a rampa.</summary>
		internal const double GraySaturation = 0.2;

		internal string Name { get; private set; }
		internal Color Frame { get; private set; }
		internal Color Line { get; private set; }
		internal Color Raised { get; private set; }
		internal Color Hover { get; private set; }
		internal Color RaisedText { get; private set; }
		internal Color TextDim { get; private set; }
		internal Color Selection { get; private set; }
		internal Color SelectionText { get; private set; }
		internal Color Accent { get; private set; }
		internal Color ScrollTrack { get; private set; }
		internal Color ScrollThumb { get; private set; }
		internal Color ScrollArrow { get; private set; }
		internal string HtmlCss { get; private set; }

		/// <summary>Texto claro padrao do tema (so nas paletas com rampa; o Escuro usa a formula).</summary>
		internal Color Text { get; private set; }

		// null = Escuro: formula original (cinza puro, matiz mantido).
		private Color[] _ramp;
		private Color[] _accents;

		internal bool Tinted { get { return _ramp != null; } }

		/// <summary>Paleta em uso pelo tema (startup e menu trocam). Padrao: Escuro.</summary>
		internal static ThemePalette Current = Dark;

		internal static ThemePalette Dark
		{
			get
			{
				return new ThemePalette
				{
					Name = DarkName,
					Frame = Color.FromArgb(38, 38, 40),
					Line = Color.FromArgb(51, 51, 55),
					Raised = Color.FromArgb(62, 62, 66),
					Hover = Color.FromArgb(62, 62, 64),
					RaisedText = Color.FromArgb(240, 240, 240),
					TextDim = Color.FromArgb(128, 128, 128),
					Selection = Color.FromArgb(38, 79, 120),
					SelectionText = Color.White,
					Accent = Color.FromArgb(0, 122, 204),
					ScrollTrack = Color.FromArgb(37, 37, 38),
					ScrollThumb = Color.FromArgb(85, 85, 88),
					ScrollArrow = Color.FromArgb(160, 160, 160),
					HtmlCss = "html,body{background-color:#1e1e1e;color:#dcdcdc;}a{color:#6cb6ff;}"
						+ "table,td,th{border-color:#555555;}",
				};
			}
		}

		/// <summary>Dracula (draculatheme.com, MIT): Background #282A36, Current Line #44475A, Foreground #F8F8F2...</summary>
		internal static ThemePalette Dracula
		{
			get
			{
				Color background = Hex("282A36"), currentLine = Hex("44475A"), foreground = Hex("F8F8F2"), comment = Hex("6272A4");
				return new ThemePalette
				{
					Name = DraculaName,
					Frame = Hex("191A21"),
					Line = currentLine,
					Raised = Hex("343746"),
					Hover = currentLine,
					RaisedText = foreground,
					TextDim = comment,
					Selection = currentLine,
					SelectionText = foreground,
					Accent = Hex("BD93F9"),
					ScrollTrack = Hex("21222C"),
					ScrollThumb = currentLine,
					ScrollArrow = comment,
					Text = foreground,
					HtmlCss = "html,body{background-color:#282a36;color:#f8f8f2;}a{color:#8be9fd;}"
						+ "table,td,th{border-color:#44475a;}",
					_ramp = new[] { Hex("191A21"), Hex("21222C"), background, Hex("343746"), currentLine, comment, foreground },
					_accents = new[] { Hex("8BE9FD"), Hex("50FA7B"), Hex("FFB86C"), Hex("FF79C6"), Hex("BD93F9"), Hex("FF5555"), Hex("F1FA8C") },
				};
			}
		}

		/// <summary>Paleta pelo nome gravado (vazio/desconhecido = Escuro: o arquivo antigo guardava so a data).</summary>
		internal static ThemePalette ByName(string name)
		{
			return string.Equals((name ?? "").Trim(), DraculaName, StringComparison.OrdinalIgnoreCase) ? Dracula : Dark;
		}

		/// <summary>
		/// Cor do .isl (ou fundo claro) ja com a luminosidade invertida (<paramref name="inverted"/>) levada para a paleta:
		/// sem rampa fica como esta; cinza vira o ponto da rampa com essa luminosidade; com matiz vira o destaque mais
		/// proximo na mesma luminosidade.
		/// </summary>
		internal Color Tint(Color original, Color inverted)
		{
			if (!Tinted)
				return inverted;
			double l = Lightness(inverted);
			if (Saturation(original) < GraySaturation)
				return Ramp(l);
			Color accent = NearestAccent(original);
			return FromHsl(accent.GetHue() / 360.0, accent.GetSaturation(), l);
		}

		/// <summary>Texto escuro do GeneXus no tema: cinza vira o texto do tema; com matiz vira o destaque mais proximo.</summary>
		internal Color TextFor(Color original)
		{
			return Saturation(original) < GraySaturation ? Text : NearestAccent(original);
		}

		/// <summary>Ponto da rampa com a luminosidade <paramref name="l"/> (interpola entre os vizinhos; fora = ponta).</summary>
		internal Color Ramp(double l)
		{
			if (l <= Lightness(_ramp[0]))
				return _ramp[0];
			for (int i = 1; i < _ramp.Length; i++)
			{
				double hi = Lightness(_ramp[i]);
				if (l <= hi)
				{
					double lo = Lightness(_ramp[i - 1]);
					double t = hi - lo <= 0 ? 0 : (l - lo) / (hi - lo);
					return Mix(_ramp[i - 1], _ramp[i], t);
				}
			}
			return _ramp[_ramp.Length - 1];
		}

		internal Color NearestAccent(Color c)
		{
			double hue = c.GetHue();
			Color best = _accents[0];
			double bestDistance = double.MaxValue;
			foreach (Color a in _accents)
			{
				double d = Math.Abs(a.GetHue() - hue);
				d = Math.Min(d, 360 - d);
				if (d < bestDistance)
				{
					bestDistance = d;
					best = a;
				}
			}
			return best;
		}

		/// <summary>"R, G, B" do .isl.</summary>
		internal static string IslText(Color c)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}", c.R, c.G, c.B);
		}

		private static Color Mix(Color a, Color b, double t)
		{
			Func<int, int, int> m = (x, y) => (int)Math.Round(x + (y - x) * t);
			return Color.FromArgb(m(a.R, b.R), m(a.G, b.G), m(a.B, b.B));
		}

		internal static double Lightness(Color c)
		{
			return (Math.Max(c.R, Math.Max(c.G, c.B)) + Math.Min(c.R, Math.Min(c.G, c.B))) / 510.0;
		}

		private static double Saturation(Color c)
		{
			double max = Math.Max(c.R, Math.Max(c.G, c.B)) / 255.0, min = Math.Min(c.R, Math.Min(c.G, c.B)) / 255.0;
			double l = (max + min) / 2;
			if (max == min)
				return 0;
			double delta = max - min;
			return l > 0.5 ? delta / (2 - max - min) : delta / (max + min);
		}

		private static Color FromHsl(double h, double s, double l)
		{
			if (s <= 0)
			{
				int v = (int)Math.Round(l * 255);
				return Color.FromArgb(v, v, v);
			}
			double q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
			Func<double, int> channel = t =>
			{
				if (t < 0) t += 1;
				if (t > 1) t -= 1;
				double v = t < 1.0 / 6 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2.0 / 3 ? p + (q - p) * (2.0 / 3 - t) * 6 : p;
				return Math.Max(0, Math.Min(255, (int)Math.Round(v * 255)));
			};
			return Color.FromArgb(channel(h + 1.0 / 3), channel(h), channel(h - 1.0 / 3));
		}

		private static Color Hex(string rgb)
		{
			return Color.FromArgb(int.Parse(rgb.Substring(0, 2), NumberStyles.HexNumber), int.Parse(rgb.Substring(2, 2), NumberStyles.HexNumber),
				int.Parse(rgb.Substring(4, 2), NumberStyles.HexNumber));
		}
	}
}
