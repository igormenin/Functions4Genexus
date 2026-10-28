using System;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Deriva um tema escuro do .isl (AppStyling do Infragistics) que a IDE carrega no startup
	/// (GenexusOffice2010Silver.isl). Logica PURA sobre XML: sem Infragistics, sem GeneXus.
	/// Desenho na secao 4 do guia.
	/// </summary>
	internal static class IslDarkTheme
	{
		// Atributos de cor dos resources/appearances: backColor, backColor2, borderColor3DBase, foreColor...
		// Exclui colorCategory, backColorAlpha e office20xxColorScheme (nao sao cor).
		private static readonly Regex ColorAttr = new Regex(@"(?i)color(\d|3DBase)?$");
		// <property name="BackColor|ToolSeparatorColor2|...">valor</property>
		private static readonly Regex ColorProperty = new Regex(@"(?i)color\d*$");

		private const double MinLightness = 0.11;
		private const double LightnessRange = 0.82;

		/// <summary>Devolve o .isl escuro a partir do .isl original.</summary>
		internal static XmlDocument Darken(XmlDocument source)
		{
			return Darken(source, ThemePalette.Dark);
		}

		/// <summary>Devolve o .isl do tema <paramref name="palette"/> a partir do .isl original.</summary>
		internal static XmlDocument Darken(XmlDocument source, ThemePalette palette)
		{
			XmlDocument doc = (XmlDocument)source.Clone();
			XmlElement root = doc.DocumentElement;
			root.SetAttribute("office2007ColorScheme", "Black");
			root.SetAttribute("office2010ColorScheme", "Black");

			foreach (XmlElement e in doc.SelectNodes("//*"))
			{
				foreach (XmlAttribute a in e.Attributes)
					if (ColorAttr.IsMatch(a.Name))
						a.Value = DarkenValue(a.Value, palette);

				if (e.Name == "property" && IsColorProperty(e.GetAttribute("name"))
					&& e.ChildNodes.Count == 1 && e.FirstChild is XmlText)
					e.FirstChild.Value = DarkenValue(e.FirstChild.Value, palette);
			}

			// Gradientes claros em bitmap: sem eles o Infragistics usa a backColor (ja escura).
			foreach (XmlNode img in doc.SelectNodes("//imageBackground"))
				img.ParentNode.RemoveChild(img);
			foreach (XmlElement e in doc.SelectNodes("//*[@imageBackgroundStyle]"))
				e.RemoveAttribute("imageBackgroundStyle");

			ApplicationWins(doc);
			AddMenuIconArea(doc, palette);
			FlattenSplitters(doc, palette);
			FlatGroupPaneTabs(doc);
			DarkMenuLines(doc, palette);
			FlatPaneCaption(doc, palette);
			return doc;
		}

		// Separadores de painel (DockManager/MDI), fundo do painel e area das abas: 214,212,214 escurecido = 64,61,64,
		// claro demais ao lado do fundo 28,28,28. Ficam numa cor de moldura discreta.
		private static readonly string[] SplitterRoles =
		{
			"DockAreaSplitterBarHorizontal", "DockAreaSplitterBarVertical", "DockManagerSplitterBar",
			"MdiSplitterBarHorizontal", "MdiSplitterBarVertical", "DockManagerPane", "TabItemArea", "TabListButton"
		};


		/// <summary>
		/// Bordas/separadores do menu (UltraWinToolbars): ResolvePopupAppearance usa a borderColor do role
		/// PopupMenu e, sem ela, MSColors.PopupMenuBorder (tabela Office, clara); o item aberto no topo
		/// (ToolbarItemPopupMenu, estado Pressed) cai no mesmo MSColors.PopupMenuBorder (PopupToolBase). O separador usa
		/// MenuItemSeparatorColor (Silver, que a formula deixa em 80,80,80). Todos viram traco discreto.
		/// </summary>
		private static void DarkMenuLines(XmlDocument doc, ThemePalette palette)
		{
			string line = ThemePalette.IslText(palette.Line);
			foreach (XmlElement state in doc.SelectNodes(
				"//style[@role='PopupMenu']/states/state[@name='Normal'] | //style[@role='ToolbarItemPopupMenu']/states/state[@name='Pressed']"))
				state.SetAttribute("borderColor", line);
			foreach (XmlElement p in doc.SelectNodes(
				"//componentStyle[@name='UltraToolbarsManager']/properties/property[@name='MenuItemSeparatorColor' or @name='MenuItemSeparatorColor2']"))
				p.InnerText = line;
		}


		/// <summary>
		/// Titulo dos paineis (KB Explorer, Preferences...): role DockPaneCaption com borderStyle="Raised" (borda 3D com
		/// realce claro em cima, a "barra branca") e estado Active azul (92,174,229, a inversao mantem o matiz). No escuro:
		/// sem borda e Active em cinza.
		/// </summary>
		private static void FlatPaneCaption(XmlDocument doc, ThemePalette palette)
		{
			foreach (XmlElement style in doc.SelectNodes("//style[@role='DockPaneCaption']"))
			{
				if (style.HasAttribute("borderStyle"))
					style.SetAttribute("borderStyle", "None");
				XmlElement active = style.SelectSingleNode("states/state[@name='Active']") as XmlElement;
				if (active == null)
					continue;
				foreach (string attr in new[] { "backColor", "borderColor", "borderColor2" })
					if (active.HasAttribute(attr))
						active.SetAttribute(attr, ThemePalette.IslText(palette.Raised));
				if (active.HasAttribute("foreColor"))
					active.SetAttribute("foreColor", ThemePalette.IslText(palette.RaisedText));
			}
		}

		/// <summary>
		/// Abas dos paineis agrupados (KB Explorer/Preferences): o .isl pede GroupPaneTabStyle=Office2007Ribbon, que na
		/// aba selecionada desenha uma borda interna com Office2007ColorTable.TabSelectedInnerBorderColorLight (cor fixa
		/// da tabela, clara) - TabManager.DrawTabBorders. Abas de documento (UltraTabbedMdiManager.TabStyle =
		/// Office2010Ribbon): DrawOffice2010RibbonTabBorders pinta a linha interna com
		/// Office2010ColorTable.RibbonTabAreaBackColorGradientLight (fixa, clara). Flat desenha so a BorderColor da
		/// aparencia (do .isl, escura).
		/// </summary>
		private static void FlatGroupPaneTabs(XmlDocument doc)
		{
			foreach (XmlElement p in doc.SelectNodes("//componentStyle[@name='UltraDockManager']/properties/property[@name='GroupPaneTabStyle']"
				+ " | //componentStyle[@name='UltraTabbedMdiManager']/properties/property[@name='TabStyle']"))
				p.InnerText = "Flat";
		}

		private static void FlattenSplitters(XmlDocument doc, ThemePalette palette)
		{
			string frame = ThemePalette.IslText(palette.Frame);
			foreach (string role in SplitterRoles)
				foreach (XmlElement state in doc.SelectNodes("//style[@role='" + role + "']/states/state[@name='Normal']"))
					foreach (string attr in new[] { "backColor", "borderColor", "borderColor2" })
						if (state.HasAttribute(attr))
							state.SetAttribute(attr, frame);
		}

		/// <summary>
		/// Em TODO styleSet e componentStyle: useOsThemes=False e resolutionOrder=ApplicationThenControl. Fatos
		/// (Infragistics 11.1): com tema do SO o Windows desenha o controle (abas, combos, scrollbars - XPThemes)
		/// e ignora as cores; o componentStyle sem esses atributos herda do styleSet (ComponentStyleInfo), entao o styleSet
		/// cobre ate controle que nao aparece no .isl (ex.: UltraTree do KB Explorer). ApplicationThenControl faz o .isl
		/// vencer a aparencia posta pelo codigo do controle (ex.: grid em White).
		/// </summary>
		private static void ApplicationWins(XmlDocument doc)
		{
			foreach (XmlElement e in doc.SelectNodes("//styleSet | //componentStyle"))
			{
				e.SetAttribute("useOsThemes", "False");
				e.SetAttribute("resolutionOrder", "ApplicationThenControl");
			}
		}

		/// <summary>
		/// Faixa de icones do menu (UltraWinToolbars): sem o role MenuIconArea no .isl (nenhum dos 4 upgrades tem) a cor
		/// vem de MSColors.MenuIconAreaGradientLight (tabela Office, clara) em MenuAgentBase.ResolveIconAreaAppearanceDefaults.
		/// Acrescenta o role no styleSet padrao com o fundo do PopupMenu ja escurecido. Role que ja existe fica como esta.
		/// </summary>
		private static void AddMenuIconArea(XmlDocument doc, ThemePalette palette)
		{
			if (doc.SelectSingleNode("//style[@role='MenuIconArea']") != null)
				return;
			XmlElement sets = doc.SelectSingleNode("//styleSets") as XmlElement;
			string name = sets == null ? "" : sets.GetAttribute("defaultStyleSet");
			XmlElement styles = (doc.SelectSingleNode("//styleSet[@name='" + name + "']/styles")
				?? doc.SelectSingleNode("//styleSet/styles")) as XmlElement;
			if (styles == null)
				return;

			XmlElement popup = styles.SelectSingleNode("style[@role='PopupMenu']/states/state[@name='Normal']") as XmlElement;
			string back = popup != null && popup.HasAttribute("backColor") ? popup.GetAttribute("backColor") : DarkenValue("WhiteSmoke", palette);

			XmlElement state = doc.CreateElement("state");
			state.SetAttribute("name", "Normal");
			state.SetAttribute("backColor", back);
			state.SetAttribute("backGradientStyle", "None");
			state.SetAttribute("backHatchStyle", "None");
			XmlElement states = doc.CreateElement("states");
			states.AppendChild(state);
			XmlElement style = doc.CreateElement("style");
			style.SetAttribute("role", "MenuIconArea");
			style.AppendChild(states);
			styles.AppendChild(style);
		}

		/// <summary>Escala MinimumThumbHeight/Width (16 no .isl) pelo DPI, como a IDE faz antes de carregar.</summary>
		internal static void ScaleThumbs(XmlDocument doc, float dpiY)
		{
			string scaled = ((int)Math.Round(16 * dpiY / 96f)).ToString(CultureInfo.InvariantCulture);
			foreach (XmlElement e in doc.SelectNodes("//property[@name='MinimumThumbHeight' or @name='MinimumThumbWidth']"))
			{
				int dummy;
				if (int.TryParse(e.InnerText, out dummy))
					e.InnerText = scaled;
			}
		}

		private static bool IsColorProperty(string name)
		{
			return ColorProperty.IsMatch(name ?? "")
				&& !string.Equals(name, "ImageTransparentColor", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Escurece um valor de cor do .isl; devolve o mesmo texto se nao for cor ou se deve ficar.</summary>
		internal static string DarkenValue(string value)
		{
			return DarkenValue(value, ThemePalette.Dark);
		}

		/// <summary>Igual, levando a cor para a paleta (Escuro: so a inversao de luminosidade).</summary>
		internal static string DarkenValue(string value, ThemePalette palette)
		{
			Color c;
			if (!TryParse(value, out c))
				return value;
			Color d = palette.Tint(c, InvertLightness(c));
			return value.Split(',').Length == 4
				? string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", c.A, d.R, d.G, d.B)
				: string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}", d.R, d.G, d.B);
		}

		/// <summary>Le "R, G, B", "A, R, G, B" ou nome conhecido NAO-sistema. Transparent e cores de sistema = false.</summary>
		internal static bool TryParse(string value, out Color color)
		{
			color = Color.Empty;
			string v = (value ?? "").Trim();
			if (v.Length == 0)
				return false;

			string[] parts = v.Split(',');
			if (parts.Length == 3 || parts.Length == 4)
			{
				int[] n = new int[parts.Length];
				for (int i = 0; i < parts.Length; i++)
					if (!int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n[i]) || n[i] < 0 || n[i] > 255)
						return false;
				color = parts.Length == 3 ? Color.FromArgb(n[0], n[1], n[2]) : Color.FromArgb(n[0], n[1], n[2], n[3]);
				return true;
			}

			Color named = Color.FromName(v);
			if (!named.IsKnownColor || named.IsSystemColor || named.A == 0)
				return false;
			color = named;
			return true;
		}

		/// <summary>Inverte a luminosidade (HSL) para a faixa escura; matiz e saturacao ficam.</summary>
		internal static Color InvertLightness(Color c)
		{
			return InvertLightness(c, 0);
		}

		/// <summary>Igual a InvertLightness, com piso de luminosidade (texto sobre fundo escuro fica legivel).</summary>
		internal static Color InvertLightness(Color c, double minLightness)
		{
			double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
			double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
			double l = (max + min) / 2, h = 0, s = 0;
			if (max != min)
			{
				double delta = max - min;
				s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);
				if (max == r) h = (g - b) / delta + (g < b ? 6 : 0);
				else if (max == g) h = (b - r) / delta + 2;
				else h = (r - g) / delta + 4;
				h /= 6;
			}

			double nl = Math.Max(minLightness, MinLightness + (1 - l) * LightnessRange);
			return FromHsl(h, s, nl);
		}

		private static Color FromHsl(double h, double s, double l)
		{
			if (s == 0)
			{
				int v = ToByte(l);
				return Color.FromArgb(v, v, v);
			}
			double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
			double p = 2 * l - q;
			return Color.FromArgb(ToByte(Hue(p, q, h + 1.0 / 3)), ToByte(Hue(p, q, h)), ToByte(Hue(p, q, h - 1.0 / 3)));
		}

		private static double Hue(double p, double q, double t)
		{
			if (t < 0) t += 1;
			if (t > 1) t -= 1;
			if (t < 1.0 / 6) return p + (q - p) * 6 * t;
			if (t < 1.0 / 2) return q;
			if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
			return p;
		}

		private static int ToByte(double v)
		{
			return (int)Math.Round(Math.Max(0, Math.Min(1, v)) * 255);
		}
	}
}
