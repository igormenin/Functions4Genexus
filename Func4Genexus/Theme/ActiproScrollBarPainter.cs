using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Barra de rolagem do editor de codigo (ActiproSoftware.WinUICore.ScrollBar, WinUICore 4.0): o
	/// WindowsXPScrollBarRenderer desenha pelo tema do Windows (ou cores de sistema) e nao tem cor configuravel; UIControl
	/// sela o OnPaint e nao dispara o evento Paint. Trilho padrao 240 (claro). Subclasse da janela: depois
	/// do WM_PAINT do dono, desenha trilho/polegar/setas escuros na geometria publica da barra (BarrelBeforeThumbBounds /
	/// BarrelAfterThumbBounds / Orientation). O clique continua com o Actipro. Detach repinta o original.
	/// </summary>
	internal sealed class ActiproScrollBarPainter : NativeWindow
	{
		internal const string ScrollBarType = "ActiproSoftware.WinUICore.ScrollBar";
		private const int WM_PAINT = 0x000F;

		private readonly Control _bar;

		private ActiproScrollBarPainter(Control bar)
		{
			_bar = bar;
		}

		/// <summary>Passa a pintar a barra escura sobre <paramref name="bar"/>; devolve o desfazer.</summary>
		internal static Action Attach(Control bar)
		{
			ActiproScrollBarPainter painter = new ActiproScrollBarPainter(bar);
			EventHandler created = delegate { painter.Hook(); };
			bar.HandleCreated += created;
			if (bar.IsHandleCreated)
				painter.Hook();
			return delegate
			{
				bar.HandleCreated -= created;
				if (painter.Handle != IntPtr.Zero)
					painter.ReleaseHandle();
				if (!bar.IsDisposed)
					bar.Invalidate();
			};
		}

		private void Hook()
		{
			if (Handle != IntPtr.Zero || _bar.IsDisposed || _bar.RecreatingHandle)
				return;
			AssignHandle(_bar.Handle);
			_bar.Invalidate();
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
				using (Graphics g = _bar.CreateGraphics())
					NcBorderPainter.DrawScrollBar(g, Geometry(_bar, _bar.ClientRectangle));
			}
			catch { } // cosmetico: nunca derruba o editor
		}

		/// <summary>
		/// Geometria pela API publica: o polegar fica entre o fim do trilho de antes e o inicio do de depois; as setas
		/// ocupam o que sobra nas pontas. Barra desabilitada (trilhos vazios): so trilho e setas.
		/// </summary>
		internal static NcBorderPainter.ScrollGeometry Geometry(object bar, Rectangle client)
		{
			bool horizontal = Convert.ToString(Read(bar, "Orientation")) == "Horizontal";
			object before = Read(bar, "BarrelBeforeThumbBounds"), after = Read(bar, "BarrelAfterThumbBounds");
			Rectangle b = before is Rectangle ? (Rectangle)before : Rectangle.Empty;
			Rectangle a = after is Rectangle ? (Rectangle)after : Rectangle.Empty;
			int arrow = horizontal ? Math.Min(client.Height, client.Width / 2) : Math.Min(client.Width, client.Height / 2);
			NcBorderPainter.ScrollGeometry g = new NcBorderPainter.ScrollGeometry
			{
				Bar = client,
				Horizontal = horizontal,
				Arrow = arrow,
				Visible = client.Width > 0 && client.Height > 0,
			};
			if (b.IsEmpty && a.IsEmpty)
				return g;
			int thumbStart = horizontal ? (b.IsEmpty ? arrow : b.Right) : (b.IsEmpty ? arrow : b.Bottom);
			int thumbEnd = horizontal ? (a.IsEmpty ? client.Width - arrow : a.Left) : (a.IsEmpty ? client.Height - arrow : a.Top);
			g.ThumbTop = thumbStart - (horizontal ? client.Left : client.Top);
			g.ThumbBottom = thumbEnd - (horizontal ? client.Left : client.Top);
			return g;
		}

		private static object Read(object target, string name)
		{
			try
			{
				PropertyInfo p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
				return p == null ? null : p.GetValue(target, null);
			}
			catch { return null; }
		}
	}
}
