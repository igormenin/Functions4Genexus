using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Sublinhado das toolbars (UltraWinToolbars 11.1): ToolbarUIElement.DrawBackColor desenha, em toda
	/// toolbar que nao e o menu principal nem flutuante, uma linha com MSColors.ToolbarUnderline - cor FIXA da tabela do
	/// Office (172,175,180 no Office2010 Black), que o .isl nao alcanca.
	/// Subclasse da UltraToolbarsDockArea: depois do WM_PAINT do dono, redesenha a mesma linha (mesmo Rect do elemento)
	/// na cor de traco escura. Acompanha recriacao de handle; Detach repinta o original.
	/// </summary>
	internal sealed class ToolbarUnderlinePainter : NativeWindow
	{
		internal const string DockAreaType = "Infragistics.Win.UltraWinToolbars.UltraToolbarsDockArea";
		private const string ToolbarElementType = "Infragistics.Win.UltraWinToolbars.ToolbarUIElement";
		private const int WM_PAINT = 0x000F;
		private const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		private readonly Control _area;
		private readonly Color _color;

		private ToolbarUnderlinePainter(Control area, Color color)
		{
			_area = area;
			_color = color;
		}

		/// <summary>Passa a cobrir o sublinhado das toolbars de <paramref name="area"/>; devolve o desfazer.</summary>
		internal static Action Attach(Control area, Color color)
		{
			ToolbarUnderlinePainter painter = new ToolbarUnderlinePainter(area, color);
			EventHandler created = delegate { painter.Hook(); };
			area.HandleCreated += created;
			if (area.IsHandleCreated)
				painter.Hook();
			return delegate
			{
				area.HandleCreated -= created;
				if (painter.Handle != IntPtr.Zero)
					painter.ReleaseHandle();
				if (!area.IsDisposed)
					area.Invalidate(true);
			};
		}

		private void Hook()
		{
			if (Handle != IntPtr.Zero || _area.IsDisposed || _area.RecreatingHandle)
				return;
			AssignHandle(_area.Handle);
			_area.Invalidate(true);
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == WM_PAINT && Handle != IntPtr.Zero)
				Paint();
		}

		private void Paint()
		{
			try
			{
				object root = Read(_area, "ControlUIElement");
				if (root == null)
					return;
				using (Graphics g = _area.CreateGraphics())
				using (Pen pen = new Pen(_color))
					foreach (Line line in Underlines(root))
						g.DrawLine(pen, line.From, line.To);
			}
			catch { } // cosmetico: nunca derruba a toolbar do dono
		}

		internal struct Line
		{
			internal Point From, To;
		}

		/// <summary>As linhas que o Infragistics desenha (mesma condicao e coordenadas de ToolbarUIElement.DrawBackColor).</summary>
		internal static System.Collections.Generic.List<Line> Underlines(object root)
		{
			System.Collections.Generic.List<Line> lines = new System.Collections.Generic.List<Line>();
			Collect(root, lines, 0);
			return lines;
		}

		private static void Collect(object element, System.Collections.Generic.List<Line> lines, int depth)
		{
			if (element == null || depth > 20)
				return;
			if (element.GetType().FullName == ToolbarElementType)
			{
				Line? line = UnderlineOf(element);
				if (line.HasValue)
					lines.Add(line.Value);
			}
			IEnumerable children = Read(element, "ChildElements") as IEnumerable;
			if (children != null)
				foreach (object child in children)
					Collect(child, lines, depth + 1);
		}

		internal static Line? UnderlineOf(object toolbarElement)
		{
			object toolbar = Read(toolbarElement, "Toolbar");
			if (toolbar == null || true.Equals(Read(toolbar, "IsMainMenuBar")))
				return null;
			string docked = Convert.ToString(Read(toolbar, "DockedPosition"));
			if (docked == "Floating")
				return null;
			object rectValue = Read(toolbarElement, "Rect");
			if (!(rectValue is Rectangle))
				return null;
			Rectangle rect = (Rectangle)rectValue;
			bool vertical = docked == "Left" || docked == "Right";
			return vertical
				? new Line { From = new Point(rect.Right - 1, rect.Top + 1), To = new Point(rect.Right - 1, rect.Bottom) }
				: new Line { From = new Point(rect.Left + 2, rect.Bottom - 1), To = new Point(rect.Right, rect.Bottom - 1) };
		}

		private static object Read(object target, string name)
		{
			if (target == null)
				return null;
			try
			{
				for (Type t = target.GetType(); t != null; t = t.BaseType)
				{
					PropertyInfo p = t.GetProperty(name, AnyInstance | BindingFlags.DeclaredOnly);
					if (p != null && p.GetIndexParameters().Length == 0)
						return p.GetValue(target, null);
				}
			}
			catch { }
			return null;
		}
	}
}
