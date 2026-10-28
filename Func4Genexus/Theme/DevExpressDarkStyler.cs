using System;
using System.Drawing;
using System.Reflection;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Tema escuro do DevExpress 7.3 (editores de estrutura: Transaction, SDT, Variables, External Object =
	/// CustomTreeList : XtraTreeList; barra do designer de Layout = XtraBars), por REFLEXAO (o SDK do U11/U12 nao traz as
	/// DLLs; MD5 do XtraTreeList igual nos quatro upgrades). Fatos da API (secao 4 do guia):
	/// o controle ignora BackColor e pinta pelas AppearanceObject (setter de BackColor/ForeColor liga UseBackColor/
	/// UseForeColor quando a cor nao e vazia) e pelo UserLookAndFeel (Style Flat/UltraFlat/Style3D/Office2003/Skin,
	/// UseWindowsXPTheme, UseDefaultLookAndFeel). Barras: BarManager.GetController().AppearancesBar (Dock/Bar/MainMenu/
	/// StatusBar) e o LookAndFeel do controller. Tudo vai para o DarkUndo.
	/// </summary>
	internal sealed class DevExpressDarkStyler
	{
		private const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance;
		private const string AppearanceType = "DevExpress.Utils.AppearanceObject";

		internal static Color Selection { get { return ThemePalette.Current.Selection; } }
		internal static Color Line { get { return ThemePalette.Current.Hover; } }

		private readonly DarkUndo _undo;

		internal DevExpressDarkStyler(DarkUndo undo)
		{
			_undo = undo;
		}

		/// <summary>O tipo ou uma de suas bases e do DevExpress.</summary>
		internal static bool IsDevExpress(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if ((t.FullName ?? "").StartsWith("DevExpress.", StringComparison.Ordinal))
					return true;
			return false;
		}

		/// <summary>Estilo plano sem tema do Windows + aparencias escuras do controle e do gerenciador de barras.</summary>
		internal void Style(object control)
		{
			if (!IsDevExpress(control.GetType()))
				return;
			StyleLookAndFeel(Get(control, "LookAndFeel"));
			StyleAppearances(Get(control, "Appearance"));
			StyleToolTips(Get(control, "ToolTipController"));
			StyleToolTips(DefaultToolTipController(control.GetType()));
			StyleColumnHeaders(control);
			object manager = Get(control, "Manager");
			if (manager != null && !_undo.Seen(manager))
			{
				_undo.Mark(manager);
				object controller = Invoke(manager, "GetController");
				if (controller != null && !_undo.Seen(controller))
				{
					_undo.Mark(controller); // pode ser o BarAndDockingController.Default (compartilhado): trata uma vez
					StyleLookAndFeel(Get(controller, "LookAndFeel"));
					StyleAppearances(Get(controller, "AppearancesBar"));
				}
			}
			System.Windows.Forms.Control c = control as System.Windows.Forms.Control;
			if (c != null)
				try { c.Invalidate(true); } catch { }
		}

		/// <summary>
		/// Dica (ToolTipControllerWindow): cores do ToolTipController.Appearance/AppearanceTitle; quem nao tem o
		/// proprio usa o ToolTipController.DefaultController (estatico, DevExpress.Utils). Fundo de painel, texto claro.
		/// </summary>
		internal void StyleToolTips(object controller)
		{
			if (controller == null || _undo.Seen(controller))
				return;
			_undo.Mark(controller);
			StyleAppearance(Get(controller, "Appearance"), "Appearance");
			StyleAppearance(Get(controller, "AppearanceTitle"), "Appearance");
		}

		private const string GxTreeListType = "Artech.Common.Controls.Editors.CustomTree.CustomTreeList";

		/// <summary>
		/// Cabecalho do CustomTreeList: o GeneXus desenha (CustomTreeListPaintHelper.DrawColumn) com fundo
		/// ColumnHeaderDefaultBackColor 245,245,245 e TEXTO Color.Black fixo. O DevExpress dispara CustomDrawColumnHeader
		/// antes e so chama o helper se nao Handled (TreeListPainter.DrawColumn): desenhamos escuro e marcamos Handled.
		/// </summary>
		internal void StyleColumnHeaders(object control)
		{
			if (!IsType(control.GetType(), GxTreeListType))
				return;
			try
			{
				EventInfo ev = control.GetType().GetEvent("CustomDrawColumnHeader", Instance);
				if (ev == null)
					return;
				Delegate handler = Delegate.CreateDelegate(ev.EventHandlerType, new HeaderRelay(), HeaderRelay.DrawMethod);
				ev.AddEventHandler(control, handler);
				_undo.Record(control, delegate { ev.RemoveEventHandler(control, handler); });
				System.Windows.Forms.Control c = control as System.Windows.Forms.Control;
				if (c != null)
					c.Invalidate();
			}
			catch { } // assinatura inesperada: fica o desenho do GeneXus
		}

		internal static bool IsType(Type type, string fullName)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if (t.FullName == fullName)
					return true;
			return false;
		}

		/// <summary>Desenha o cabecalho escuro a partir do CustomDrawColumnHeaderEventArgs (por reflexao).</summary>
		internal sealed class HeaderRelay
		{
			// Pelo delegate, nao pelo nome do metodo.
			internal static readonly MethodInfo DrawMethod = new Action<object, EventArgs>(new HeaderRelay().Draw).Method;

			public void Draw(object sender, EventArgs e)
			{
				try { DrawHeader(e); }
				catch { } // sem Handled: o GeneXus desenha como sempre
			}
		}

		internal static readonly Color HeaderBack = DarkPalette.Back(SystemColors.Control);

		internal static void DrawHeader(object e)
		{
			Graphics g = Get(e, "Graphics") as Graphics;
			object bounds = Get(Get(e, "ObjectArgs"), "Bounds");
			if (g == null || !(bounds is Rectangle))
				return;
			Rectangle rect = (Rectangle)bounds;
			rect.Width -= 1;
			rect.Height -= 1;
			using (SolidBrush back = new SolidBrush(HeaderBack))
				g.FillRectangle(back, rect);
			using (Pen border = new Pen(Line))
				g.DrawRectangle(border, rect);

			object caption = Get(e, "Caption");
			object captionRect = Get(e, "CaptionRect");
			object column = Get(e, "Column");
			Font font = Get(Get(column, "AppearanceHeader"), "Font") as Font ?? System.Windows.Forms.Control.DefaultFont;
			if (caption is string && captionRect is Rectangle && !Equals(Fmt(Get(e, "ColumnType")), "BehindColumn"))
				System.Windows.Forms.TextRenderer.DrawText(g, (string)caption, font, (Rectangle)captionRect,
					DarkPalette.Fore(SystemColors.ControlText),
					System.Windows.Forms.TextFormatFlags.Left | System.Windows.Forms.TextFormatFlags.VerticalCenter
					| System.Windows.Forms.TextFormatFlags.EndEllipsis | System.Windows.Forms.TextFormatFlags.SingleLine
					| System.Windows.Forms.TextFormatFlags.NoPrefix);
			DrawSortShape(g, Get(e, "SortShapeRect"), Fmt(Get(column, "SortOrder")));
			PropertyInfo handled = e.GetType().GetProperty("Handled", Instance);
			if (handled != null && handled.CanWrite)
				handled.SetValue(e, true, null);
		}

		private static void DrawSortShape(Graphics g, object shape, string order)
		{
			if (!(shape is Rectangle) || (order != "Ascending" && order != "Descending"))
				return;
			Rectangle s = (Rectangle)shape;
			s.Offset(-1, 0);
			PointF[] points = order == "Ascending"
				? new[] { new PointF(s.X, s.Bottom), new PointF(s.X + s.Width / 2f, s.Y), new PointF(s.Right, s.Bottom), new PointF(s.X, s.Bottom) }
				: new[] { new PointF(s.X, s.Y), new PointF(s.X + s.Width / 2f, s.Bottom), new PointF(s.Right, s.Y), new PointF(s.X, s.Y) };
			using (Pen pen = new Pen(DarkPalette.ScrollArrow))
				g.DrawLines(pen, points);
		}

		private static string Fmt(object value)
		{
			return value == null ? null : value.ToString();
		}

		private static object DefaultToolTipController(Type devExpressType)
		{
			try
			{
				foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
				{
					if (!(a.GetName().Name ?? "").StartsWith("DevExpress.Utils.", StringComparison.Ordinal))
						continue;
					Type t = a.GetType("DevExpress.Utils.ToolTipController", false);
					PropertyInfo p = t == null ? null : t.GetProperty("DefaultController", BindingFlags.Public | BindingFlags.Static);
					if (p != null)
						return p.GetValue(null, null);
				}
			}
			catch { }
			return null;
		}

		/// <summary>UltraFlat sem tema do Windows: o desenho passa a usar as cores das aparencias.</summary>
		internal void StyleLookAndFeel(object lookAndFeel)
		{
			if (lookAndFeel == null || _undo.Seen(lookAndFeel))
				return;
			_undo.Mark(lookAndFeel);
			PropertyInfo style = lookAndFeel.GetType().GetProperty("Style", Instance);
			if (style == null || !style.PropertyType.IsEnum || !Enum.IsDefined(style.PropertyType, "UltraFlat"))
				return;
			SetBool(lookAndFeel, "UseDefaultLookAndFeel", false);
			SetBool(lookAndFeel, "UseWindowsXPTheme", false);
			try
			{
				object original = style.GetValue(lookAndFeel, null);
				object flat = Enum.Parse(style.PropertyType, "UltraFlat");
				if (!Equals(original, flat))
				{
					style.SetValue(lookAndFeel, flat, null);
					_undo.Record(lookAndFeel, delegate { style.SetValue(lookAndFeel, original, null); });
				}
			}
			catch { }
		}

		/// <summary>
		/// Aparencia unica (AppearanceObject) ou colecao (TreeListAppearanceCollection, BarManagerAppearances): cada
		/// AppearanceObject publica ganha cor pelo papel do nome (ver RoleColors).
		/// </summary>
		internal void StyleAppearances(object appearances)
		{
			if (appearances == null)
				return;
			if (IsAppearance(appearances.GetType()))
			{
				StyleAppearance(appearances, "Appearance");
				return;
			}
			foreach (PropertyInfo p in appearances.GetType().GetProperties(Instance))
			{
				if (p.GetIndexParameters().Length > 0 || p.PropertyType.IsValueType || p.PropertyType == typeof(string))
					continue;
				object value = Get(appearances, p.Name);
				if (value != null && IsAppearance(value.GetType()))
					StyleAppearance(value, p.Name);
			}
		}

		private void StyleAppearance(object appearance, string role)
		{
			if (appearance == null || _undo.Seen(appearance))
				return;
			_undo.Mark(appearance);
			Color[] colors = RoleColors(role);
			Keep(appearance, "BackColor", colors[0]);
			Keep(appearance, "ForeColor", colors[1]);
			Keep(appearance, "BackColor2", Color.Empty); // gradiente claro: vira cor lisa
			Keep(appearance, "BorderColor", Color.Empty);
		}

		/// <summary>
		/// Cor padrao por papel (fundo, texto) quando a aparencia nao tem cor propria. PURO. Linhas (HorzLine,
		/// VertLine, TreeLine, FixedLine) = cinza discreto; selecao/foco = azul escuro; paineis/barras = fundo de
		/// controle; o resto (Empty, Row, EvenRow, OddRow, Preview...) = fundo de janela.
		/// </summary>
		internal static Color[] RoleColors(string role)
		{
			string r = role ?? "";
			Color text = DarkPalette.Fore(SystemColors.WindowText);
			if (r.EndsWith("Line", StringComparison.Ordinal))
				return new[] { Line, text };
			if (r.StartsWith("Focused", StringComparison.Ordinal) || r.StartsWith("Selected", StringComparison.Ordinal))
				return new[] { Selection, ThemePalette.Current.SelectionText };
			if (r.StartsWith("HideSelection", StringComparison.Ordinal))
				return new[] { Line, text };
			if (r.IndexOf("Panel", StringComparison.Ordinal) >= 0 || r.IndexOf("Footer", StringComparison.Ordinal) >= 0
				|| r == "GroupButton" || r == "Dock" || r == "Bar" || r == "MainMenu" || r == "StatusBar" || r == "Appearance")
				return new[] { DarkPalette.Back(SystemColors.Control), DarkPalette.Fore(SystemColors.ControlText) };
			return new[] { DarkPalette.Back(SystemColors.Window), text };
		}

		/// <summary>
		/// Cor da aparencia: vazia = padrao do papel; propria = regra da DarkPalette (fundo escurece, texto clareia).
		/// Desfazer devolve a cor e o UseXxx original (o setter liga UseXxx sozinho).
		/// </summary>
		private void Keep(object appearance, string property, Color whenEmpty)
		{
			try
			{
				PropertyInfo p = appearance.GetType().GetProperty(property, Instance);
				if (p == null || !p.CanWrite || p.PropertyType != typeof(Color))
					return;
				Color original = (Color)p.GetValue(appearance, null);
				bool isText = property == "ForeColor";
				Color dark = original.IsEmpty ? whenEmpty : (isText ? DarkPalette.Fore(original) : DarkPalette.Back(original));
				if (dark == original)
					return;
				object options = Get(appearance, "Options");
				string use = "Use" + (property == "BackColor2" ? "BackColor" : property);
				PropertyInfo flag = options == null ? null : options.GetType().GetProperty(use, Instance);
				object flagValue = flag == null ? null : flag.GetValue(options, null);
				p.SetValue(appearance, dark, null);
				if (flag != null && flag.CanWrite && !dark.IsEmpty)
					flag.SetValue(options, true, null); // o setter so liga UseXxx fora de IsLoading (API conferida)
				_undo.Record(appearance, delegate
				{
					p.SetValue(appearance, original, null);
					if (flag != null && flag.CanWrite)
						flag.SetValue(options, flagValue, null);
				});
			}
			catch { }
		}

		private void SetBool(object target, string property, bool value)
		{
			try
			{
				PropertyInfo p = target.GetType().GetProperty(property, Instance);
				if (p == null || !p.CanWrite || p.PropertyType != typeof(bool))
					return;
				bool original = (bool)p.GetValue(target, null);
				if (original == value)
					return;
				p.SetValue(target, value, null);
				_undo.Record(target, delegate { p.SetValue(target, original, null); });
			}
			catch { }
		}

		private static bool IsAppearance(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if (t.FullName == AppearanceType)
					return true;
			return false;
		}

		private static object Get(object target, string property)
		{
			if (target == null)
				return null;
			try
			{
				PropertyInfo p = target.GetType().GetProperty(property, Instance);
				return p == null || p.GetIndexParameters().Length > 0 ? null : p.GetValue(target, null);
			}
			catch { return null; }
		}

		private static object Invoke(object target, string method)
		{
			try
			{
				MethodInfo m = target.GetType().GetMethod(method, Instance, null, Type.EmptyTypes, null);
				return m == null ? null : m.Invoke(target, null);
			}
			catch { return null; }
		}
	}
}
