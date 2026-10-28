using System;
using System.Drawing;
using System.Globalization;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Paleta de um tema escuro: cor por PAPEL (moldura, traco, selecao, barra de rolagem...) e a regra que leva as cores
	/// claras do GeneXus para o tema. PURA e imutavel. Escuro = a formula de sempre (inverte a luminosidade e mantem o
	/// matiz; saida identica a de antes da paleta - teste). Dracula e demais temas = mesma luminosidade, mas o cinza vira um ponto da
	/// rampa de fundos da paleta e a cor com matiz vira o destaque mais proximo. Ver secao 4 do guia.
	/// </summary>
	internal sealed class ThemePalette
	{
		internal const string LightName = "Claro";
		internal const string DarkName = "Escuro";
		internal const string DraculaName = "Dracula";
		internal const string CatppuccinMochaName = "Catppuccin Mocha";
		internal const string TokyoNightName = "Tokyo Night";
		internal const string NordName = "Nord";
		internal const string GruvboxDarkName = "Gruvbox Dark";
		internal const string OneDarkProName = "One Dark Pro";
		internal const string RosePineName = "Rosé Pine";

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

		/// <summary>Catppuccin Mocha (catppuccin/catppuccin, MIT): Base #1E1E2E, Surface0 #313244, Text #CDD6F4...</summary>
		internal static ThemePalette CatppuccinMocha
		{
			get
			{
				Color baseBg = Hex("1E1E2E"), surface0 = Hex("313244"), surface1 = Hex("45475A"), overlay0 = Hex("6C7086"), text = Hex("CDD6F4");
				return new ThemePalette
				{
					Name = CatppuccinMochaName,
					Frame = Hex("11111B"),
					Line = surface1,
					Raised = surface0,
					Hover = surface1,
					RaisedText = text,
					TextDim = overlay0,
					Selection = surface1,
					SelectionText = text,
					Accent = Hex("CBA6F7"),
					ScrollTrack = Hex("181825"),
					ScrollThumb = surface1,
					ScrollArrow = overlay0,
					Text = text,
					HtmlCss = "html,body{background-color:#1e1e2e;color:#cdd6f4;}a{color:#89b4fa;}"
						+ "table,td,th{border-color:#45475a;}",
					_ramp = new[] { Hex("11111B"), Hex("181825"), baseBg, surface0, surface1, overlay0, text },
					_accents = new[] { Hex("89B4FA"), Hex("94E2D5"), Hex("A6E3A1"), Hex("F9E2AF"), Hex("FAB387"), Hex("F38BA8"), Hex("CBA6F7") },
				};
			}
		}

		/// <summary>Tokyo Night (folke/tokyonight.nvim, MIT): Bg #24283B, Highlight #292E42, Fg #C0CAF5...</summary>
		internal static ThemePalette TokyoNight
		{
			get
			{
				Color bg = Hex("24283B"), highlight = Hex("292E42"), line = Hex("3B4261"), comment = Hex("565F89"), fg = Hex("C0CAF5");
				return new ThemePalette
				{
					Name = TokyoNightName,
					Frame = Hex("16161E"),
					Line = line,
					Raised = highlight,
					Hover = line,
					RaisedText = fg,
					TextDim = comment,
					Selection = Hex("283457"),
					SelectionText = fg,
					Accent = Hex("7AA2F7"),
					ScrollTrack = Hex("1A1B26"),
					ScrollThumb = line,
					ScrollArrow = comment,
					Text = fg,
					HtmlCss = "html,body{background-color:#24283b;color:#c0caf5;}a{color:#7dcfff;}"
						+ "table,td,th{border-color:#3b4261;}",
					_ramp = new[] { Hex("16161E"), Hex("1A1B26"), bg, highlight, line, comment, fg },
					_accents = new[] { Hex("7AA2F7"), Hex("7DCFFF"), Hex("9ECE6A"), Hex("E0AF68"), Hex("FF9E64"), Hex("F7768E"), Hex("BB9AF7") },
				};
			}
		}

		/// <summary>Nord (nordtheme/nord, MIT): Polar Night #2E3440..#4C566A, Snow Storm #ECEFF4, Frost #88C0D0...</summary>
		internal static ThemePalette Nord
		{
			get
			{
				Color nord0 = Hex("2E3440"), nord1 = Hex("3B4252"), nord2 = Hex("434C5E"), nord3 = Hex("4C566A"), dim = Hex("7B88A1"), nord6 = Hex("ECEFF4");
				return new ThemePalette
				{
					Name = NordName,
					Frame = Hex("242933"),
					Line = nord3,
					Raised = nord2,
					Hover = nord3,
					RaisedText = nord6,
					TextDim = dim,
					Selection = nord2,
					SelectionText = nord6,
					Accent = Hex("88C0D0"),
					ScrollTrack = nord0,
					ScrollThumb = nord3,
					ScrollArrow = dim,
					Text = nord6,
					HtmlCss = "html,body{background-color:#2e3440;color:#eceff4;}a{color:#88c0d0;}"
						+ "table,td,th{border-color:#4c566a;}",
					_ramp = new[] { Hex("242933"), nord0, nord1, nord2, nord3, dim, nord6 },
					_accents = new[] { Hex("88C0D0"), Hex("81A1C1"), Hex("A3BE8C"), Hex("EBCB8B"), Hex("D08770"), Hex("BF616A"), Hex("B48EAD") },
				};
			}
		}

		/// <summary>Gruvbox Dark (morhetz/gruvbox, MIT): Dark0 #282828, Dark1 #3C3836, Dark2 #504945, Light1 #EBDBB2...</summary>
		internal static ThemePalette GruvboxDark
		{
			get
			{
				Color dark0 = Hex("282828"), soft = Hex("32302F"), dark1 = Hex("3C3836"), dark2 = Hex("504945"), gray = Hex("928374"), light1 = Hex("EBDBB2");
				return new ThemePalette
				{
					Name = GruvboxDarkName,
					Frame = Hex("1D2021"),
					Line = dark2,
					Raised = dark1,
					Hover = dark2,
					RaisedText = light1,
					TextDim = gray,
					Selection = dark2,
					SelectionText = light1,
					Accent = Hex("FE8019"),
					ScrollTrack = dark0,
					ScrollThumb = dark2,
					ScrollArrow = gray,
					Text = light1,
					HtmlCss = "html,body{background-color:#282828;color:#ebdbb2;}a{color:#83a598;}"
						+ "table,td,th{border-color:#504945;}",
					_ramp = new[] { Hex("1D2021"), dark0, soft, dark1, dark2, gray, light1 },
					_accents = new[] { Hex("8EC07C"), Hex("B8BB26"), Hex("FABD2F"), Hex("FE8019"), Hex("FB4934"), Hex("D3869B"), Hex("83A598") },
				};
			}
		}

		/// <summary>One Dark Pro (binaryify/OneDark-Pro, MIT): Bg #282C34, Gutter #3E4451, Fg #ABB2BF...</summary>
		internal static ThemePalette OneDarkPro
		{
			get
			{
				Color uiBg = Hex("21252B"), bg = Hex("282C34"), raised = Hex("323842"), line = Hex("3E4451"), comment = Hex("5C6370"), fg = Hex("ABB2BF");
				return new ThemePalette
				{
					Name = OneDarkProName,
					Frame = Hex("1E2227"),
					Line = line,
					Raised = raised,
					Hover = line,
					RaisedText = fg,
					TextDim = comment,
					Selection = line,
					SelectionText = fg,
					Accent = Hex("61AFEF"),
					ScrollTrack = uiBg,
					ScrollThumb = line,
					ScrollArrow = comment,
					Text = fg,
					HtmlCss = "html,body{background-color:#282c34;color:#abb2bf;}a{color:#61afef;}"
						+ "table,td,th{border-color:#3e4451;}",
					_ramp = new[] { Hex("1E2227"), uiBg, bg, raised, line, comment, fg },
					_accents = new[] { Hex("56B6C2"), Hex("61AFEF"), Hex("98C379"), Hex("E5C07B"), Hex("D19A66"), Hex("E06C75"), Hex("C678DD") },
				};
			}
		}

		/// <summary>Rosé Pine (rose-pine/rose-pine-theme, MIT): Base #191724, Surface #1F1D2E, Overlay #26233A, Text #E0DEF4...</summary>
		internal static ThemePalette RosePine
		{
			get
			{
				Color baseBg = Hex("191724"), surface = Hex("1F1D2E"), overlay = Hex("26233A"), med = Hex("312F44"), high = Hex("403D52"), muted = Hex("6E6A86"), text = Hex("E0DEF4");
				return new ThemePalette
				{
					Name = RosePineName,
					Frame = baseBg,
					Line = high,
					Raised = med,
					Hover = high,
					RaisedText = text,
					TextDim = muted,
					Selection = high,
					SelectionText = text,
					Accent = Hex("C4A7E7"),
					ScrollTrack = surface,
					ScrollThumb = high,
					ScrollArrow = muted,
					Text = text,
					HtmlCss = "html,body{background-color:#191724;color:#e0def4;}a{color:#9ccfd8;}"
						+ "table,td,th{border-color:#403d52;}",
					_ramp = new[] { baseBg, surface, overlay, med, high, muted, text },
					_accents = new[] { Hex("9CCFD8"), Hex("31748F"), Hex("F6C177"), Hex("EBBCBA"), Hex("EB6F92"), Hex("C4A7E7"), Hex("908CAA") },
				};
			}
		}

		/// <summary>Paleta pelo nome gravado (vazio/desconhecido = Escuro: o arquivo antigo guardava so a data).</summary>
		internal static ThemePalette ByName(string name)
		{
			string n = (name ?? "").Trim();
			if (string.Equals(n, DraculaName, StringComparison.OrdinalIgnoreCase)) return Dracula;
			if (string.Equals(n, CatppuccinMochaName, StringComparison.OrdinalIgnoreCase)) return CatppuccinMocha;
			if (string.Equals(n, TokyoNightName, StringComparison.OrdinalIgnoreCase)) return TokyoNight;
			if (string.Equals(n, NordName, StringComparison.OrdinalIgnoreCase)) return Nord;
			if (string.Equals(n, GruvboxDarkName, StringComparison.OrdinalIgnoreCase)) return GruvboxDark;
			if (string.Equals(n, OneDarkProName, StringComparison.OrdinalIgnoreCase)) return OneDarkPro;
			if (string.Equals(n, RosePineName, StringComparison.OrdinalIgnoreCase) || string.Equals(n, "Rose Pine", StringComparison.OrdinalIgnoreCase)) return RosePine;
			return Dark;
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
