using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Func4Genexus.Theme
{
	/// <summary>Quem desenha o controle; decide se o tema escuro mexe nele e como.</summary>
	internal enum PaintOwner { WinForms, Infragistics, Actipro, SmartPropertyGrid, Web }

	/// <summary>
	/// Pinta de escuro a arvore de controles de uma janela (secao 4 do guia).
	/// WinForms: BackColor/ForeColor pela DarkPalette; ToolStrip ganha renderer escuro. Infragistics fica com o .isl;
	/// Actipro e SmartPropertyGrid vao para o ThirdPartyDarkStyler; web nao e tocado. Tudo vai para o DarkUndo.
	/// Filho que herda a cor do pai (ambiente) nao e tocado: o pai ja escuro faz o filho escuro.
	/// </summary>
	internal sealed class ControlPainter
	{
		private readonly DarkUndo _undo;
		private readonly ThirdPartyDarkStyler _thirdParty;
		private readonly StatusBarDarkStyler _statusBarStyler;
		private readonly List<StatusBar> _statusBars = new List<StatusBar>();

		internal ControlPainter(DarkUndo undo)
		{
			_undo = undo;
			_thirdParty = new ThirdPartyDarkStyler(undo);
			_statusBarStyler = new StatusBarDarkStyler(undo);
			_devExpress = new DevExpressDarkStyler(undo);
		}

		private readonly DevExpressDarkStyler _devExpress;

		/// <summary>Classifica pelo nome do tipo e de suas bases (o 1o que bater vence).</summary>
		internal static PaintOwner OwnerOf(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
			{
				PaintOwner? owner = OwnerOfName(t.FullName);
				if (owner.HasValue)
					return owner.Value;
			}
			return PaintOwner.WinForms;
		}

		/// <summary>Nucleo puro de OwnerOf (testavel): null = nome que nao decide.</summary>
		internal static PaintOwner? OwnerOfName(string fullName)
		{
			string n = fullName ?? "";
			if (n.StartsWith("Infragistics.", StringComparison.Ordinal)) return PaintOwner.Infragistics;
			if (n.StartsWith("ActiproSoftware.SyntaxEditor.SyntaxEditor", StringComparison.Ordinal)) return PaintOwner.Actipro;
			if (n.StartsWith("VisualHint.SmartPropertyGrid.PropertyGrid", StringComparison.Ordinal)) return PaintOwner.SmartPropertyGrid;
			if (n == "System.Windows.Forms.WebBrowser" || n.StartsWith("CefSharp.", StringComparison.Ordinal)
				|| n.StartsWith("ChromiumBrowser.", StringComparison.Ordinal) || n.StartsWith("Microsoft.Web.WebView2.", StringComparison.Ordinal))
				return PaintOwner.Web;
			return null;
		}

		/// <summary>Pinta o controle e os filhos; devolve quantos objetos novos foram tratados.</summary>
		internal int Paint(Control root)
		{
			int before = _undo.Count;
			Visit(root);
			return _undo.Count - before;
		}

		private void Visit(Control c)
		{
			if (c == null || c.IsDisposed || _undo.Seen(c))
				return;

			switch (OwnerOf(c.GetType()))
			{
				case PaintOwner.Web:
					_thirdParty.StyleWeb(c, ThirdPartyDarkStyler.InStartPage(c)); // Monaco (Output) e a Start Page; o resto tem CSS proprio
					return; // filhos sao do navegador
				case PaintOwner.Infragistics:
					SetColors(c); // BackColor claro proprio (ex.: grid da Search em White); o .isl cuida do resto
					_thirdParty.StyleInfragisticsScrollBars(c);
					_thirdParty.StyleInfragisticsAppearances(c);
					if (c.GetType().FullName == ToolbarUnderlinePainter.DockAreaType)
						_undo.Record(c, ToolbarUnderlinePainter.Attach(c, DarkPalette.Line));
					break; // filhos podem ser WinForms
				case PaintOwner.Actipro:
					_thirdParty.StyleEditor(c);
					break;
				case PaintOwner.SmartPropertyGrid:
					_thirdParty.StyleGrid(c);
					break;
				default:
					PaintWinForms(c);
					_thirdParty.StyleCrownwood(c);
					_thirdParty.StyleHtmlEditor(c);
					_devExpress.Style(c);
					if (c.GetType().FullName == ActiproScrollBarPainter.ScrollBarType)
						_undo.Record(c, ActiproScrollBarPainter.Attach(c));
					break;
			}

			c.ControlAdded += OnControlAdded;
			_undo.Record(c, delegate { c.ControlAdded -= OnControlAdded; });

			foreach (Control child in c.Controls)
				Visit(child);
		}

		/// <summary>
		/// Varre uma janela ja tratada e pinta o que escapou do ControlAdded (controle novo sob pai que nao avisou).
		/// Devolve uma linha por escape (tipo, nome, pai) para o rastro no Output.
		/// </summary>
		internal List<string> Sweep(Control root)
		{
			List<string> escaped = new List<string>();
			SweepCore(root, escaped);
			return escaped;
		}

		private void SweepCore(Control c, List<string> escaped)
		{
			if (c == null || c.IsDisposed)
				return;
			if (!_undo.Seen(c))
			{
				int before = _undo.Count;
				Visit(c);
				if (_undo.Count > before)
					escaped.Add(Describe(c) + " sob " + (c.Parent == null ? "-" : Describe(c.Parent)));
				return;
			}
			if (OwnerOf(c.GetType()) == PaintOwner.Web)
				return;
			foreach (Control child in c.Controls)
				SweepCore(child, escaped);
		}

		internal static string Describe(Control c)
		{
			return c.GetType().FullName + (string.IsNullOrEmpty(c.Name) ? "" : " '" + c.Name + "'");
		}

		private void OnControlAdded(object sender, ControlEventArgs e)
		{
			try { Visit(e.Control); }
			catch { } // best-effort: nunca quebra a IDE por causa do tema
		}

		private void PaintWinForms(Control c)
		{
			TabPage page = c as TabPage;
			if (page != null)
			{
				// O setter de BackColor desliga UseVisualStyleBackColor; registrado ANTES para voltar por ultimo.
				bool visual = page.UseVisualStyleBackColor;
				_undo.Record(page, delegate { page.UseVisualStyleBackColor = visual; });
			}
			SetColors(c);
			PaintOverReadOnlyBack(c);
			FlattenBorder(c);
			ToolStrip strip = c as ToolStrip;
			if (strip != null)
				PaintToolStrip(strip);
			ComboBox combo = c as ComboBox;
			if (combo != null)
				FlattenCombo(combo);
			LinkLabel link = c as LinkLabel;
			if (link != null)
				PaintLink(link);
			Button button = c as Button;
			if (button != null)
				FlattenButton(button);
			StatusBar bar = c as StatusBar;
			if (bar != null)
				_statusBars.Add(bar); // precisa de handle e de estar visivel: Refresh tenta
			TabControl tabs = c as TabControl;
			if (tabs != null)
				PaintTabHeaders(tabs);
			DataGridView grid = c as DataGridView;
			if (grid != null)
				PaintDataGridView(grid);
		}

		/// <summary>
		/// DataGridView (System.Windows.Forms 4.x) nao pinta com BackColor: fundo = BackgroundColor, celulas e cabecalhos =
		/// DefaultCellStyle/ColumnHeadersDefaultCellStyle/RowHeadersDefaultCellStyle (cabecalho com visual style ignora as
		/// cores: EnableHeadersVisualStyles off), linhas = GridColor. Estilo posto pelo dono em linha/celula (ex.: categoria
		/// cinza) passa pelo CellFormatting, que so ajusta o estilo da pintura. Combo de coluna Standard e desenhado pelo
		/// Windows: vira Flat. Ex.: dialogo "Other GeneXus Properties".
		/// </summary>
		private void PaintDataGridView(DataGridView grid)
		{
			Color background = grid.BackgroundColor, gridColor = grid.GridColor;
			bool visualHeaders = grid.EnableHeadersVisualStyles;
			DataGridViewHeaderBorderStyle columnBorder = grid.ColumnHeadersBorderStyle, rowBorder = grid.RowHeadersBorderStyle;
			grid.BackgroundColor = DarkPalette.Back(background);
			grid.GridColor = DarkPalette.Line;
			grid.EnableHeadersVisualStyles = false;
			// Raised (padrao) desenha borda 3D clara no cabecalho; Single usa a GridColor.
			if (columnBorder == DataGridViewHeaderBorderStyle.Raised || columnBorder == DataGridViewHeaderBorderStyle.Sunken)
				grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			if (rowBorder == DataGridViewHeaderBorderStyle.Raised || rowBorder == DataGridViewHeaderBorderStyle.Sunken)
				grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			_undo.Record(grid, delegate
			{
				grid.BackgroundColor = background;
				grid.GridColor = gridColor;
				grid.EnableHeadersVisualStyles = visualHeaders;
				grid.ColumnHeadersBorderStyle = columnBorder;
				grid.RowHeadersBorderStyle = rowBorder;
			});
			DarkenCellStyle(grid, grid.DefaultCellStyle);
			DarkenCellStyle(grid, grid.ColumnHeadersDefaultCellStyle);
			DarkenCellStyle(grid, grid.RowHeadersDefaultCellStyle);

			foreach (DataGridViewColumn column in grid.Columns)
			{
				DataGridViewComboBoxColumn combo = column as DataGridViewComboBoxColumn;
				if (combo == null || combo.FlatStyle != FlatStyle.Standard)
					continue;
				combo.FlatStyle = FlatStyle.Flat;
				_undo.Record(grid, delegate { combo.FlatStyle = FlatStyle.Standard; });
			}

			DataGridViewCellFormattingEventHandler format = delegate (object sender, DataGridViewCellFormattingEventArgs e)
			{
				DarkenCellColors(e.CellStyle);
			};
			grid.CellFormatting += format;
			_undo.Record(grid, delegate { grid.CellFormatting -= format; grid.Invalidate(); });
			grid.Invalidate();
		}

		private void DarkenCellStyle(DataGridView grid, DataGridViewCellStyle style)
		{
			Color back = style.BackColor, fore = style.ForeColor, selBack = style.SelectionBackColor, selFore = style.SelectionForeColor;
			DarkenCellColors(style);
			_undo.Record(grid, delegate
			{
				style.BackColor = back;
				style.ForeColor = fore;
				style.SelectionBackColor = selBack;
				style.SelectionForeColor = selFore;
			});
		}

		/// <summary>Fundo claro escurece e texto escuro clareia; cor vazia (herdada) fica vazia.</summary>
		internal static void DarkenCellColors(DataGridViewCellStyle style)
		{
			if (!style.BackColor.IsEmpty)
				style.BackColor = DarkPalette.Back(style.BackColor);
			if (!style.ForeColor.IsEmpty)
				style.ForeColor = DarkPalette.Fore(style.ForeColor);
			if (!style.SelectionBackColor.IsEmpty)
				style.SelectionBackColor = DarkPalette.Back(style.SelectionBackColor);
			if (!style.SelectionForeColor.IsEmpty)
				style.SelectionForeColor = DarkPalette.Fore(style.SelectionForeColor);
		}

		/// <summary>
		/// TabControl (System.Windows.Forms 4.x): BackColor devolve sempre SystemColors.Control e o setter nao faz nada;
		/// o cabecalho e do Windows. Abas Normal passam a OwnerDrawFixed com fundo escuro e texto claro.
		/// </summary>
		private void PaintTabHeaders(TabControl tabs)
		{
			if (tabs.DrawMode != TabDrawMode.Normal || tabs.Appearance != TabAppearance.Normal)
				return;
			DrawItemEventHandler draw = delegate (object sender, DrawItemEventArgs e) { DrawTabHeader(tabs, e); };
			tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
			tabs.DrawItem += draw;
			_undo.Record(tabs, delegate { tabs.DrawItem -= draw; tabs.DrawMode = TabDrawMode.Normal; tabs.Invalidate(); });
		}

		private static void DrawTabHeader(TabControl tabs, DrawItemEventArgs e)
		{
			if (e.Index < 0 || e.Index >= tabs.TabPages.Count)
				return;
			bool selected = e.Index == tabs.SelectedIndex;
			Color back = selected ? DarkPalette.Back(SystemColors.Window) : DarkPalette.Back(SystemColors.Control);
			using (SolidBrush b = new SolidBrush(back))
				e.Graphics.FillRectangle(b, e.Bounds);
			TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds,
				DarkPalette.Fore(SystemColors.ControlText),
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
		}

		/// <summary>
		/// Button Standard/System desenha o botao claro do Windows (estilo visual ignora BackColor; sem estilo, o 3D
		/// tem realce branco): vira Flat com borda sutil e usa o BackColor (proprio escurecido ou o do pai, escuro).
		/// </summary>
		private void FlattenButton(Button b)
		{
			if (b.FlatStyle != FlatStyle.Standard && b.FlatStyle != FlatStyle.System)
				return;
			FlatStyle style = b.FlatStyle;
			Color border = b.FlatAppearance.BorderColor;
			Action restoreVisual = VisualStyleBackColorRestorer(b);
			b.FlatStyle = FlatStyle.Flat;
			b.UseVisualStyleBackColor = false;
			b.FlatAppearance.BorderColor = DarkPalette.Lift(b.BackColor, 0.25);
			PaintEventHandler disabledText = delegate (object sender, PaintEventArgs e) { RedrawDisabledText(b, e); };
			b.Paint += disabledText;
			_undo.Record(b, delegate { b.Paint -= disabledText; b.FlatAppearance.BorderColor = border; restoreVisual(); b.FlatStyle = style; });
		}

		/// <summary>
		/// Botao Flat desabilitado (WinForms 4.x): o texto sai com ControlPaint.DrawStringDisabled, sombra derivada do
		/// BackColor - no fundo escuro fica preto no preto (ex.: dialogo "Editor" do WWP). O Paint roda depois do
		/// desenho do dono: escreve o texto de novo, por cima, em cinza legivel. Imagem e borda ficam como estao.
		/// </summary>
		private static void RedrawDisabledText(Button b, PaintEventArgs e)
		{
			if (b.Enabled || string.IsNullOrEmpty(b.Text))
				return;
			TextRenderer.DrawText(e.Graphics, b.Text, b.Font, Rectangle.Inflate(b.ClientRectangle, -3, -3),
				DarkPalette.DisabledText, TextFlags(b.TextAlign) | TextFormatFlags.WordBreak);
		}

		internal static TextFormatFlags TextFlags(ContentAlignment align)
		{
			TextFormatFlags flags = TextFormatFlags.EndEllipsis;
			switch (align)
			{
				case ContentAlignment.TopLeft: case ContentAlignment.MiddleLeft: case ContentAlignment.BottomLeft:
					flags |= TextFormatFlags.Left; break;
				case ContentAlignment.TopRight: case ContentAlignment.MiddleRight: case ContentAlignment.BottomRight:
					flags |= TextFormatFlags.Right; break;
				default:
					flags |= TextFormatFlags.HorizontalCenter; break;
			}
			switch (align)
			{
				case ContentAlignment.TopLeft: case ContentAlignment.TopCenter: case ContentAlignment.TopRight:
					flags |= TextFormatFlags.Top; break;
				case ContentAlignment.BottomLeft: case ContentAlignment.BottomCenter: case ContentAlignment.BottomRight:
					flags |= TextFormatFlags.Bottom; break;
				default:
					flags |= TextFormatFlags.VerticalCenter; break;
			}
			return flags;
		}

		/// <summary>
		/// Devolve o estado EXATO de UseVisualStyleBackColor: o getter depende do fundo (Control) e ja le false com o pai
		/// escuro, entao guarda os campos de ButtonBase (isEnableVisualStyleBackgroundSet/enableVisualStyleBackground,
		/// fonte de referencia do System.Windows.Forms 4.x). Sem os campos: devolve o valor publico.
		/// </summary>
		private static Action VisualStyleBackColorRestorer(ButtonBase b)
		{
			const System.Reflection.BindingFlags Field = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
			System.Reflection.FieldInfo isSet = typeof(ButtonBase).GetField("isEnableVisualStyleBackgroundSet", Field);
			System.Reflection.FieldInfo enabled = typeof(ButtonBase).GetField("enableVisualStyleBackground", Field);
			if (isSet == null || enabled == null || isSet.FieldType != typeof(bool) || enabled.FieldType != typeof(bool))
			{
				bool visual = b.UseVisualStyleBackColor;
				return delegate { b.UseVisualStyleBackColor = visual; };
			}
			object wasSet = isSet.GetValue(b), wasEnabled = enabled.GetValue(b);
			return delegate { isSet.SetValue(b, wasSet); enabled.SetValue(b, wasEnabled); b.Invalidate(); };
		}

		/// <summary>Reaplica o que o dono pode ter trocado depois (estilos do editor, status bar ainda sem handle). Timer.</summary>
		internal void Refresh()
		{
			_thirdParty.Refresh();
			_statusBarStyler.Maintain();
			for (int i = _statusBars.Count - 1; i >= 0; i--)
			{
				StatusBar bar = _statusBars[i];
				if (bar.IsDisposed)
					_statusBars.RemoveAt(i);
				else if (bar.IsHandleCreated && bar.Visible)
				{
					_statusBars.RemoveAt(i);
					_statusBarStyler.Style(bar);
				}
			}
		}

		/// <summary>
		/// BackColor so com getter e claro (MonacoPane: override => SystemColors.Window): nao da para
		/// trocar; o Paint pinta o fundo escuro por cima do OnPaintBackground (a faixa branca ao lado do Output).
		/// </summary>
		private void PaintOverReadOnlyBack(Control c)
		{
			System.Reflection.PropertyInfo p;
			try { p = c.GetType().GetProperty("BackColor", typeof(Color)); }
			catch (System.Reflection.AmbiguousMatchException) { return; } // "new BackColor" em subclasse: nao decide
			if (p == null || p.CanWrite || !DarkPalette.IsLight(c.BackColor))
				return;
			PaintEventHandler paint = delegate (object sender, PaintEventArgs e)
			{
				using (SolidBrush b = new SolidBrush(DarkPalette.Back(c.BackColor)))
					e.Graphics.FillRectangle(b, c.ClientRectangle);
			};
			c.Paint += paint;
			c.Invalidate();
			_undo.Record(c, delegate { c.Paint -= paint; c.Invalidate(); });
		}

		/// <summary>Borda 3D desenha o realce quase branco: vira linha simples. Vale para todo controle com BorderStyle.</summary>
		private void FlattenBorder(Control c)
		{
			try
			{
				System.Reflection.PropertyInfo p = c.GetType().GetProperty("BorderStyle", typeof(BorderStyle));
				if (p == null || !p.CanWrite || (BorderStyle)p.GetValue(c, null) != BorderStyle.Fixed3D)
					return;
				p.SetValue(c, BorderStyle.FixedSingle, null);
				_undo.Record(c, delegate { p.SetValue(c, BorderStyle.Fixed3D, null); });
			}
			catch { }
		}

		/// <summary>DropDownList com estilo visual ignora BackColor (desenha como botao claro): FlatStyle.Flat respeita.</summary>
		private void FlattenCombo(ComboBox combo)
		{
			if (combo.DropDownStyle != ComboBoxStyle.DropDownList || combo.FlatStyle == FlatStyle.Flat)
				return;
			FlatStyle original = combo.FlatStyle;
			combo.FlatStyle = FlatStyle.Flat;
			_undo.Record(combo, delegate { combo.FlatStyle = original; });
		}

		private void PaintLink(LinkLabel link)
		{
			Color link0 = link.LinkColor, active = link.ActiveLinkColor, visited = link.VisitedLinkColor;
			link.LinkColor = DarkPalette.Fore(link0);
			link.ActiveLinkColor = DarkPalette.Fore(active);
			link.VisitedLinkColor = DarkPalette.Fore(visited);
			_undo.Record(link, delegate { link.LinkColor = link0; link.ActiveLinkColor = active; link.VisitedLinkColor = visited; });
		}

		/// <summary>
		/// Troca BackColor/ForeColor proprios (nao herdados do pai) pela DarkPalette e MANTEM: se o dono trocar a cor
		/// depois (WWP no hover/selecao das abas, filtro do Properties), o *ColorChanged escurece de novo (KeptColor).
		/// </summary>
		private void SetColors(Control c)
		{
			_undo.Mark(c);
			// TabPage dentro de TabControl: o pai devolve sempre Control (API conferida), entao "igual ao pai" nao e herdado.
			KeptColor back = new KeptColor(() => c.BackColor, v => c.BackColor = v,
				v => c.Parent != null && !(c.Parent is TabControl) && v == c.Parent.BackColor ? v : DarkPalette.Back(v));
			KeptColor fore = new KeptColor(() => c.ForeColor, v => c.ForeColor = v,
				v => c.Parent != null && v == c.Parent.ForeColor ? v : DarkPalette.Fore(v));
			EventHandler onBack = delegate { Keep(back); };
			EventHandler onFore = delegate { Keep(fore); };

			Keep(back);
			Keep(fore);
			c.BackColorChanged += onBack;
			c.ForeColorChanged += onFore;
			// Desfaz na ordem inversa: solta os eventos ANTES de devolver as cores (senao o evento escurece de novo).
			_undo.Record(c, back.Restore);
			_undo.Record(c, fore.Restore);
			_undo.Record(c, delegate { c.BackColorChanged -= onBack; c.ForeColorChanged -= onFore; });
		}

		private static void Keep(KeptColor color)
		{
			try { color.Apply(); }
			catch { } // controle que nao aceita a cor (ex.: transparente) fica como esta
		}

		private void PaintToolStrip(ToolStrip strip)
		{
			ToolStripRenderer original = strip.Renderer;
			strip.Renderer = new ToolStripProfessionalRenderer(new DarkToolStripColors()) { RoundedEdges = false };
			_undo.Record(strip, delegate { strip.Renderer = original; });
			PaintItems(strip.Items, strip);
		}

		private void PaintItems(ToolStripItemCollection items, ToolStrip owner)
		{
			foreach (ToolStripItem item in items)
			{
				if (_undo.Seen(item))
					continue;
				_undo.Mark(item);
				ToolStripItem it = item;
				Color fore = it.ForeColor, back = it.BackColor;
				Color newFore = DarkPalette.Fore(fore), newBack = DarkPalette.Back(back);
				if (newFore != fore)
				{
					it.ForeColor = newFore;
					_undo.Record(it, delegate { it.ForeColor = fore; });
				}
				if (newBack != back && back != owner.BackColor)
				{
					it.BackColor = newBack;
					_undo.Record(it, delegate { it.BackColor = back; });
				}

				ToolStripControlHost host = it as ToolStripControlHost;
				if (host != null)
					Visit(host.Control);

				ToolStripDropDownItem dropDown = it as ToolStripDropDownItem;
				if (dropDown != null && dropDown.HasDropDownItems)
					Visit(dropDown.DropDown);
			}
		}

		/// <summary>Cores do ToolStrip escuro (as gradientes do tema claro viram cor lisa da DarkPalette).</summary>
		private sealed class DarkToolStripColors : ProfessionalColorTable
		{
			// Propriedades (nao campos): a paleta pode mudar com a IDE aberta.
			private static Color Bar { get { return DarkPalette.Back(SystemColors.Control); } }
			private static Color Menu { get { return DarkPalette.Back(SystemColors.Window); } }
			private static Color Line { get { return DarkPalette.Back(SystemColors.ControlDark); } }
			private static Color Hot { get { return ThemePalette.Current.Hover; } }
			private static Color Pressed { get { return ThemePalette.Current.Accent; } }

			public override Color ToolStripGradientBegin { get { return Bar; } }
			public override Color ToolStripGradientMiddle { get { return Bar; } }
			public override Color ToolStripGradientEnd { get { return Bar; } }
			public override Color ToolStripContentPanelGradientBegin { get { return Bar; } }
			public override Color ToolStripContentPanelGradientEnd { get { return Bar; } }
			public override Color ToolStripPanelGradientBegin { get { return Bar; } }
			public override Color ToolStripPanelGradientEnd { get { return Bar; } }
			public override Color MenuStripGradientBegin { get { return Bar; } }
			public override Color MenuStripGradientEnd { get { return Bar; } }
			public override Color StatusStripGradientBegin { get { return Bar; } }
			public override Color StatusStripGradientEnd { get { return Bar; } }
			public override Color ToolStripBorder { get { return Line; } }
			public override Color ToolStripDropDownBackground { get { return Menu; } }
			public override Color ImageMarginGradientBegin { get { return Menu; } }
			public override Color ImageMarginGradientMiddle { get { return Menu; } }
			public override Color ImageMarginGradientEnd { get { return Menu; } }
			public override Color MenuBorder { get { return Line; } }
			public override Color MenuItemBorder { get { return Line; } }
			public override Color MenuItemSelected { get { return Hot; } }
			public override Color MenuItemSelectedGradientBegin { get { return Hot; } }
			public override Color MenuItemSelectedGradientEnd { get { return Hot; } }
			public override Color MenuItemPressedGradientBegin { get { return Menu; } }
			public override Color MenuItemPressedGradientMiddle { get { return Menu; } }
			public override Color MenuItemPressedGradientEnd { get { return Menu; } }
			public override Color ButtonSelectedHighlight { get { return Hot; } }
			public override Color ButtonSelectedGradientBegin { get { return Hot; } }
			public override Color ButtonSelectedGradientMiddle { get { return Hot; } }
			public override Color ButtonSelectedGradientEnd { get { return Hot; } }
			public override Color ButtonSelectedBorder { get { return Line; } }
			public override Color ButtonPressedGradientBegin { get { return Pressed; } }
			public override Color ButtonPressedGradientMiddle { get { return Pressed; } }
			public override Color ButtonPressedGradientEnd { get { return Pressed; } }
			public override Color ButtonCheckedGradientBegin { get { return Hot; } }
			public override Color ButtonCheckedGradientMiddle { get { return Hot; } }
			public override Color ButtonCheckedGradientEnd { get { return Hot; } }
			public override Color ButtonCheckedHighlight { get { return Hot; } }
			public override Color SeparatorDark { get { return Line; } }
			public override Color SeparatorLight { get { return Bar; } }
			public override Color GripDark { get { return Line; } }
			public override Color GripLight { get { return Bar; } }
			public override Color OverflowButtonGradientBegin { get { return Bar; } }
			public override Color OverflowButtonGradientMiddle { get { return Bar; } }
			public override Color OverflowButtonGradientEnd { get { return Bar; } }
		}
	}
}
