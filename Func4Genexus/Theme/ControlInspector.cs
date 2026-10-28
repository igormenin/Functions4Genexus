using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Func4Genexus.Theme
{
	/// <summary>Um controle visto pelo inspetor do tema escuro.</summary>
	internal sealed class InspectedControl
	{
		internal int Depth;
		internal string TypeName;
		internal string Assembly;
		internal string Name;
		internal bool Visible;
		internal Size Size;
		internal Color Back;
		internal Color Fore;
		internal PaintOwner Owner;
		/// <summary>Cor do que de fato pinta o fundo quando nao e o BackColor (editor: renderer; Properties: grid).</summary>
		internal string Extra;

		/// <summary>Escuro por outro caminho que nao o BackColor (ex.: status bar nativa desenhada pelo tema).</summary>
		internal bool PaintedOver;

		/// <summary>Pixels reais da area propria (sem filhos) na tela; null = sem captura (teste, janela fora da tela).</summary>
		internal ScreenSample Screen;

		/// <summary>Parte clara na tela que conta como "segue claro" (cabecalho de 20 px numa lista grande ja aparece).</summary>
		internal const double LightOnScreen = 0.05;

		/// <summary>
		/// Visivel, com area e ainda claro. Com captura: decide o PIXEL da tela (qualquer dono, inclusive web e o que o
		/// GeneXus desenha por cima). Sem captura: BackColor claro de quem o tema pinta (nao web, nao pintado por cima).
		/// </summary>
		internal bool StillLight
		{
			get
			{
				if (!Visible || Size.Width <= 0 || Size.Height <= 0)
					return false;
				if (Screen != null)
					return Screen.Samples > 0 && !Screen.Covered && Screen.LightFraction >= LightOnScreen; // sem area propria: nada a ver
				return Owner != PaintOwner.Web && !PaintedOver && DarkPalette.IsLight(Back);
			}
		}
	}

	/// <summary>
	/// Inspetor do tema escuro: arvore de controles das janelas abertas com tipo, assembly, cores e quem pinta.
	/// Coleta (WinForms) separada da formatacao (pura, testavel). Ver secao 4 do guia.
	/// </summary>
	internal static class ControlInspector
	{
		/// <summary>Coleta todas as janelas abertas da thread atual.</summary>
		internal static List<InspectedControl> Collect()
		{
			List<InspectedControl> all = new List<InspectedControl>();
			foreach (Form f in DarkWindows.Snapshot())
				if (!f.IsDisposed && !f.InvokeRequired)
				{
					ScreenSampler screen = f.Visible && f.IsHandleCreated && f.WindowState != FormWindowState.Minimized
						? ScreenSampler.Capture(f.RectangleToScreen(f.ClientRectangle))
						: null;
					Collect(f, 0, all, screen);
				}
			return all;
		}

		internal static void Collect(Control c, int depth, List<InspectedControl> into)
		{
			Collect(c, depth, into, null);
		}

		private static void Collect(Control c, int depth, List<InspectedControl> into, ScreenSampler screen)
		{
			Type t = c.GetType();
			InspectedControl node = new InspectedControl
			{
				Depth = depth,
				TypeName = t.FullName,
				Assembly = t.Assembly.GetName().Name,
				Name = c.Name,
				Visible = c.Visible,
				Size = c.Size,
				Back = c.BackColor,
				Fore = c.ForeColor,
				Owner = ControlPainter.OwnerOf(t),
				PaintedOver = StatusBarDarkStyler.IsStyled(c as StatusBar),
			};
			node.Extra = ExtraFor(c, node.Owner);
			if (screen != null && node.Visible && c.IsHandleCreated)
				try { node.Screen = screen.Sample(c, node.Owner == PaintOwner.Web); } catch { }
			into.Add(node);

			if (node.Owner == PaintOwner.Web)
				return;
			foreach (Control child in c.Controls)
				Collect(child, depth + 1, into, screen);
		}

		private static string ExtraFor(Control c, PaintOwner owner)
		{
			try
			{
				if (owner == PaintOwner.Actipro)
				{
					object r = Prop(c, "RendererResolved");
					object language = Prop(Prop(c, "Document"), "Language");
					return (r == null ? "renderer=?" : "renderer=" + r.GetType().Name + " textArea=" + Fmt(Prop(r, "TextAreaBackColor")))
						+ " linguagem=" + (language == null ? "?" : language.GetType().Name)
						+ " estilosComFundoClaro=" + LightStyles(language, new HashSet<object>(DarkUndo.ByReference.Instance));
				}
				if (owner == PaintOwner.SmartPropertyGrid)
				{
					object dm = Prop(c, "DrawManager");
					return "grid=" + Fmt(Prop(c, "GridBackColor")) + " drawManager=" + (dm == null ? "?" : dm.GetType().Name);
				}
				if (owner == PaintOwner.Infragistics)
				{
					object layout = Prop(c, "DisplayLayout");
					object look = Prop(c, "ScrollBarLook") ?? Prop(layout, "ScrollBarLook");
					object thumb = Prop(look, "ThumbAppearance");
					object over = Prop(layout, "Override");
					string extra = (thumb == null ? "" : "scrollThumb=" + Fmt(Prop(thumb, "BackColor")))
						+ AppearanceBack(" aparencia=", Prop(c, "Appearance"))
						+ AppearanceBack(" layout=", Prop(layout, "Appearance"))
						+ AppearanceBack(" linha=", Prop(over, "RowAppearance"))
						+ AppearanceBack(" celula=", Prop(over, "CellAppearance"));
					return extra.Trim().Length == 0 ? null : extra.Trim();
				}
				if (DevExpressDarkStyler.IsDevExpress(c.GetType()))
				{
					object lf = Prop(c, "LookAndFeel");
					object appearance = Prop(c, "Appearance");
					object row = appearance == null ? null : Prop(appearance, "Row");
					return "lookAndFeel=" + (lf == null ? "?" : Fmt(Prop(lf, "Style")) + " xp=" + Fmt(Prop(lf, "UseWindowsXPTheme")))
						+ AppearanceBack(" aparencia=", appearance) + AppearanceBack(" linha=", row)
						+ AppearanceBack(" vazio=", appearance == null ? null : Prop(appearance, "Empty"));
				}
				StatusBar bar = c as StatusBar;
				if (bar != null)
					return "paineis=" + string.Join(",", bar.Panels.Cast<StatusBarPanel>().Select(p => p.GetType().Name + ":" + p.Style)) + " ownerDraw="
						+ bar.Panels.Cast<StatusBarPanel>().Count(p => p.Style == StatusBarPanelStyle.OwnerDraw)
						+ " showPanels=" + bar.ShowPanels;
				if (ThirdPartyDarkStyler.IsCrownwood(c.GetType()) && c.GetType().GetProperty("TextInactiveColor") != null)
					return "textoInativo=" + Fmt(Prop(c, "TextInactiveColor")) + " hot=" + Fmt(Prop(c, "HotTextColor"));
				ToolStrip strip = c as ToolStrip;
				if (strip != null)
					return "renderer=" + (strip.Renderer == null ? "?" : strip.Renderer.GetType().Name) + " itens=" + strip.Items.Count;
			}
			catch { }
			return null;
		}

		/// <summary>Relatorio completo (arvore indentada). PURO.</summary>
		/// <summary>
		/// O que desenha o ponto de tela: cores numa cruz de 4 px (acha linha fina), a cadeia de controles WinForms e, no
		/// primeiro controle Infragistics da cadeia, os UIElements sob o ponto (tipo, retangulo, role do .isl, borda) -
		/// por reflexao (UIElement/ControlUIElement + ElementFromPoint). Nunca lanca.
		/// </summary>
		internal static List<string> DescribePoint(Point screen)
		{
			List<string> lines = new List<string>();
			try
			{
				ScreenSampler shot = ScreenSampler.Capture(new Rectangle(screen.X - 4, screen.Y - 4, 9, 9));
				if (shot != null)
				{
					StringBuilder column = new StringBuilder(), row = new StringBuilder();
					for (int d = -4; d <= 4; d++)
					{
						column.Append(d == 0 ? " [" : " ").Append(Fmt(shot.Pixel(new Point(screen.X, screen.Y + d)))).Append(d == 0 ? "]" : "");
						row.Append(d == 0 ? " [" : " ").Append(Fmt(shot.Pixel(new Point(screen.X + d, screen.Y)))).Append(d == 0 ? "]" : "");
					}
					lines.Add("ponto " + screen.X + "," + screen.Y + " | vertical (y-4..y+4):" + column);
					lines.Add("horizontal (x-4..x+4):" + row);
				}

				Control hit = ScreenSampler.ControlAt(screen);
				if (hit == null)
				{
					lines.Add("Nenhum controle WinForms deste processo sob o mouse (janela de outro processo ou nativa).");
					return lines;
				}
				bool elementsDone = false;
				for (Control c = hit; c != null; c = c.Parent)
				{
					Rectangle r = c.RectangleToScreen(c.ClientRectangle);
					lines.Add((c == hit ? "controle " : "  pai ") + ControlPainter.Describe(c) + " | tela " + r.X + "," + r.Y + " " + r.Width + "x" + r.Height
						+ " | back=" + Fmt(c.BackColor));
					if (!elementsDone)
						elementsDone = AddElements(c, c.PointToClient(screen), lines);
				}
			}
			catch (Exception ex)
			{
				lines.Add("falhou: " + ex.Message);
			}
			return lines;
		}

		private static bool AddElements(Control c, Point client, List<string> lines)
		{
			object root = SafeProp(c, "UIElement") ?? SafeProp(c, "ControlUIElement") ?? NonPublicProp(c, "ControlUIElement");
			if (root == null || !IsInfragisticsElement(root.GetType()))
				return false;
			MethodInfo find = root.GetType().GetMethod("ElementFromPoint", new[] { typeof(Point) });
			object element = find == null ? null : find.Invoke(root, new object[] { client });
			for (int depth = 0; element != null && depth < 30; depth++)
			{
				object role = SafeProp(element, "UIRoleResolved") ?? SafeProp(element, "UIRole");
				lines.Add("    elemento " + element.GetType().FullName + " | rect " + Fmt(SafeProp(element, "Rect"))
					+ " | role " + (role == null ? "-" : Fmt(SafeProp(role, "Name"))) + " | borda " + Fmt(SafeProp(element, "BorderStyle")));
				element = SafeProp(element, "Parent");
			}
			return true;
		}

		private static object SafeProp(object target, string name)
		{
			try { return Prop(target, name); }
			catch { return null; } // getter do dono que lanca nao derruba a inspecao
		}

		/// <summary>ControlUIElement e protected em varios controles do Infragistics (ex.: DockableWindow).</summary>
		private static object NonPublicProp(object target, string name)
		{
			try
			{
				for (Type t = target.GetType(); t != null; t = t.BaseType)
				{
					PropertyInfo p = t.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
					if (p != null && p.GetIndexParameters().Length == 0)
						return p.GetValue(target, null);
				}
			}
			catch { }
			return null;
		}

		private static bool IsInfragisticsElement(Type t)
		{
			for (; t != null; t = t.BaseType)
				if (t.FullName == "Infragistics.Win.UIElement")
					return true;
			return false;
		}

		internal static string Format(IList<InspectedControl> nodes, string header)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(header);
			sb.AppendLine("Colunas: [CLARO] tipo (assembly) nome | visivel tamanho | fundo | texto | quem pinta | extra | tela (pixels reais da area propria, sem filhos)");
			sb.AppendLine();
			foreach (InspectedControl n in nodes)
			{
				sb.Append(new string(' ', n.Depth * 2));
				if (n.StillLight)
					sb.Append("[CLARO] ");
				sb.Append(n.TypeName).Append(" (").Append(n.Assembly).Append(")");
				if (!string.IsNullOrEmpty(n.Name))
					sb.Append(" '").Append(n.Name).Append("'");
				sb.Append(" | ").Append(n.Visible ? "vis" : "oculto").Append(' ')
					.Append(n.Size.Width.ToString(CultureInfo.InvariantCulture)).Append('x')
					.Append(n.Size.Height.ToString(CultureInfo.InvariantCulture));
				sb.Append(" | ").Append(Fmt(n.Back)).Append(" | ").Append(Fmt(n.Fore)).Append(" | ").Append(OwnerName(n.Owner));
				if (!string.IsNullOrEmpty(n.Extra))
					sb.Append(" | ").Append(n.Extra);
				if (n.Screen != null)
					sb.Append(" | tela ").Append(ScreenText(n.Screen));
				sb.AppendLine();
			}
			return sb.ToString();
		}

		/// <summary>Resumo para o Output: tipos que seguem claros, mais frequente primeiro. PURO.</summary>
		internal static List<string> Summarize(IList<InspectedControl> nodes, int max)
		{
			return nodes.Where(n => n.StillLight)
				.GroupBy(n => n.TypeName)
				.OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
				.Take(max)
				.Select(g => g.Count().ToString(CultureInfo.InvariantCulture) + "x " + g.Key + " (" + OwnerName(g.First().Owner) + ", fundo " + Fmt(g.First().Back)
					+ (g.First().Screen != null && g.First().Screen.Samples > 0 ? "; tela " + ScreenText(g.First().Screen) : "") + ")")
				.ToList();
		}

		/// <summary>"claro=12% em 0,0 1669x20 tipico=245,245,245": quanto, onde (relativo ao controle) e que cor. PURO.</summary>
		internal static string ScreenText(ScreenSample s)
		{
			string pct = Math.Round(s.LightFraction * 100).ToString(CultureInfo.InvariantCulture) + "%";
			if (s.Samples == 0)
				return "sem area propria (coberto pelos filhos)";
			if (s.LightBounds.IsEmpty)
				return "claro=" + pct + " tipico=" + Fmt(s.Typical);
			Rectangle b = s.LightBounds;
			return "claro=" + pct + " em " + b.X + "," + b.Y + " " + b.Width + "x" + b.Height + " tipico=" + Fmt(s.Typical)
				+ (s.Covered ? " (coberto por outra janela/aba: nao conta)" : "");
		}

		/// <summary>Nome fixo de cada dono (texto do relatorio).</summary>
		internal static string OwnerName(PaintOwner owner)
		{
			switch (owner)
			{
				case PaintOwner.Infragistics: return "Infragistics";
				case PaintOwner.Actipro: return "Actipro";
				case PaintOwner.SmartPropertyGrid: return "SmartPropertyGrid";
				case PaintOwner.Web: return "Web";
				default: return "WinForms";
			}
		}

		internal static string Fmt(object value)
		{
			if (!(value is Color))
				return value == null ? "?" : value.ToString();
			Color c = (Color)value;
			if (c.IsEmpty) return "vazio";
			string rgb = c.R + "," + c.G + "," + c.B + (c.A < 255 ? " a" + c.A : "");
			return c.IsKnownColor ? c.Name + "=" + rgb : rgb;
		}

		/// <summary>Estilos de sintaxe (linguagem e filhas) com fundo ainda claro.</summary>
		private static int LightStyles(object language, HashSet<object> visited)
		{
			if (language == null || !visited.Add(language))
				return 0;
			int count = 0;
			IEnumerable styles = Prop(language, "HighlightingStyles") as IEnumerable;
			if (styles != null)
				foreach (object s in styles)
				{
					object back = s == null ? null : Prop(s, "BackColor");
					if (back is Color && DarkPalette.IsLight((Color)back))
						count++;
				}
			IEnumerable children = Prop(language, "ChildLanguages") as IEnumerable;
			if (children != null)
				foreach (object child in children)
					count += LightStyles(child, visited);
			return count;
		}

		/// <summary>Fundo de uma aparencia Infragistics so quando posto (vazio = resolvido pelo .isl, nao aparece).</summary>
		private static string AppearanceBack(string label, object appearance)
		{
			object back = Prop(appearance, "BackColor");
			return back is Color && !((Color)back).IsEmpty ? label + Fmt(back) : "";
		}

		private static object Prop(object target, string name)
		{
			if (target == null)
				return null;
			PropertyInfo p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
			return p == null || p.GetIndexParameters().Length > 0 ? null : p.GetValue(target, null);
		}
	}
}
