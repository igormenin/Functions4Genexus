using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Area nao-cliente redesenhada escura DEPOIS do dono: borda de 1 px e barra de rolagem vertical NATIVA.
	/// Fatos (SmartPropertyGrid 11): InternalGrid trata WM_NCPAINT chamando
	/// LightColorDrawManager.DrawGridNonClientArea, que pinta a borda com Color.White fixo; a barra e nativa
	/// (ShowScrollBar/SetScrollInfo). DarkMode_Explorer nao escurece barra nativa nesta maquina (Windows build 26100), entao
	/// pintamos por cima: geometria real por GetScrollBarInfo (o Windows segue fazendo o hit-test, o clique nao muda).
	/// O Windows redesenha a barra fora do WM_NCPAINT (rolagem, hover, SetScrollInfo): repinta depois dessas mensagens e
	/// sempre que a geometria mudar. Subclasse por NativeWindow; acompanha recriacao de handle; Detach devolve o original.
	/// </summary>
	internal sealed class NcBorderPainter : NativeWindow
	{
		private const int WM_SIZE = 0x0005, WM_SETFOCUS = 0x0007, WM_KILLFOCUS = 0x0008, WM_ENABLE = 0x000A;
		private const int WM_PAINT = 0x000F, WM_WINDOWPOSCHANGED = 0x0047, WM_NCPAINT = 0x0085;
		private const int WM_NCMOUSEMOVE = 0x00A0, WM_NCLBUTTONDOWN = 0x00A1, WM_NCLBUTTONUP = 0x00A2;
		private const int WM_KEYDOWN = 0x0100, WM_TIMER = 0x0113, WM_VSCROLL = 0x0115, WM_SYSTIMER = 0x0118;
		private const int WM_MOUSEMOVE = 0x0200, WM_MOUSEWHEEL = 0x020A, WM_CAPTURECHANGED = 0x0215;
		private const int WM_NCMOUSELEAVE = 0x02A2, WM_MOUSELEAVE = 0x02A3;
		private const uint RDW_INVALIDATE = 0x0001, RDW_FRAME = 0x0400, RDW_UPDATENOW = 0x0100;
		private const int OBJID_VSCROLL = unchecked((int)0xFFFFFFFB);
		private const int STATE_SYSTEM_INVISIBLE = 0x8000, STATE_SYSTEM_OFFSCREEN = 0x10000;

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindowDC(IntPtr hwnd);

		[DllImport("user32.dll")]
		private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

		[DllImport("user32.dll")]
		private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

		[DllImport("user32.dll")]
		private static extern bool RedrawWindow(IntPtr hwnd, IntPtr rect, IntPtr region, uint flags);

		[DllImport("user32.dll")]
		private static extern bool GetScrollBarInfo(IntPtr hwnd, int idObject, ref SCROLLBARINFO info);

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT { public int Left, Top, Right, Bottom; }

		[StructLayout(LayoutKind.Sequential)]
		private struct SCROLLBARINFO
		{
			public int cbSize;
			public RECT rcScrollBar;
			public int dxyLineButton, xyThumbTop, xyThumbBottom, reserved;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
			public int[] rgstate;
		}

		/// <summary>Geometria da barra vertical em coordenadas da janela (origem no canto do GetWindowRect).</summary>
		internal struct ScrollGeometry
		{
			internal Rectangle Bar;
			internal int Arrow, ThumbTop, ThumbBottom;
			internal bool Visible;
			/// <summary>Barra horizontal: ThumbTop/ThumbBottom e Arrow medidos no eixo X.</summary>
			internal bool Horizontal;
		}

		private readonly Control _control;
		private readonly Color _border;
		private readonly bool _scrollBar;
		private ScrollGeometry _painted;

		private NcBorderPainter(Control control, Color border, bool scrollBar)
		{
			_control = control;
			_border = border;
			_scrollBar = scrollBar;
		}

		/// <summary>Passa a pintar borda (e, se pedido, a barra vertical) de <paramref name="control"/>; devolve o desfazer.</summary>
		internal static Action Attach(Control control, Color border, bool scrollBar = false)
		{
			NcBorderPainter painter = new NcBorderPainter(control, border, scrollBar);
			EventHandler created = delegate { painter.Hook(); };
			control.HandleCreated += created;
			if (control.IsHandleCreated)
				painter.Hook();
			return delegate
			{
				control.HandleCreated -= created;
				painter.Unhook();
			};
		}

		private void Hook()
		{
			if (Handle != IntPtr.Zero || _control.IsDisposed || _control.RecreatingHandle)
				return;
			AssignHandle(_control.Handle);
			Redraw(Handle);
		}

		private void Unhook()
		{
			IntPtr h = Handle;
			if (h == IntPtr.Zero)
				return;
			ReleaseHandle();
			Redraw(h);
		}

		private static void Redraw(IntPtr hwnd)
		{
			try { RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW); }
			catch { } // so repinta antes; o proximo WM_NCPAINT acerta
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (Handle == IntPtr.Zero)
				return; // WM_NCDESTROY ja soltou a janela
			if (m.Msg == WM_NCPAINT)
				Paint(m.HWnd, true);
			else if (_scrollBar && (RepaintsScrollBar(m.Msg) || !Same(Geometry(m.HWnd), _painted)))
				Paint(m.HWnd, false);
		}

		/// <summary>Mensagens depois das quais o Windows pode ter redesenhado a barra (rolagem, hover, foco, tamanho).</summary>
		internal static bool RepaintsScrollBar(int msg)
		{
			switch (msg)
			{
				case WM_SIZE: case WM_SETFOCUS: case WM_KILLFOCUS: case WM_ENABLE: case WM_PAINT:
				case WM_WINDOWPOSCHANGED: case WM_NCMOUSEMOVE: case WM_NCLBUTTONDOWN: case WM_NCLBUTTONUP:
				case WM_KEYDOWN: case WM_TIMER: case WM_VSCROLL: case WM_SYSTIMER: case WM_MOUSEMOVE:
				case WM_MOUSEWHEEL: case WM_CAPTURECHANGED: case WM_NCMOUSELEAVE: case WM_MOUSELEAVE:
					return true;
				default:
					return false;
			}
		}

		private static bool Same(ScrollGeometry a, ScrollGeometry b)
		{
			return a.Visible == b.Visible && a.Bar == b.Bar && a.ThumbTop == b.ThumbTop && a.ThumbBottom == b.ThumbBottom;
		}

		private static ScrollGeometry Geometry(IntPtr hwnd)
		{
			ScrollGeometry g = new ScrollGeometry();
			RECT window;
			SCROLLBARINFO info = new SCROLLBARINFO { rgstate = new int[6] };
			info.cbSize = Marshal.SizeOf(typeof(SCROLLBARINFO));
			if (!GetWindowRect(hwnd, out window) || !GetScrollBarInfo(hwnd, OBJID_VSCROLL, ref info))
				return g;
			if ((info.rgstate[0] & (STATE_SYSTEM_INVISIBLE | STATE_SYSTEM_OFFSCREEN)) != 0)
				return g;
			g.Bar = Rectangle.FromLTRB(info.rcScrollBar.Left - window.Left, info.rcScrollBar.Top - window.Top,
				info.rcScrollBar.Right - window.Left, info.rcScrollBar.Bottom - window.Top);
			g.Arrow = info.dxyLineButton;
			g.ThumbTop = info.xyThumbTop;
			g.ThumbBottom = info.xyThumbBottom;
			g.Visible = g.Bar.Width > 0 && g.Bar.Height > 0;
			return g;
		}

		private void Paint(IntPtr hwnd, bool border)
		{
			RECT r;
			if (!GetWindowRect(hwnd, out r))
				return;
			ScrollGeometry bar = _scrollBar ? Geometry(hwnd) : new ScrollGeometry();
			IntPtr dc = GetWindowDC(hwnd);
			if (dc == IntPtr.Zero)
				return;
			try
			{
				using (Graphics g = Graphics.FromHdc(dc))
				{
					if (bar.Visible)
						DrawScrollBar(g, bar);
					if (border)
						using (Pen pen = new Pen(_border))
							g.DrawRectangle(pen, 0, 0, r.Right - r.Left - 1, r.Bottom - r.Top - 1);
				}
				_painted = bar;
			}
			catch { } // desenho e cosmetico: nunca derruba a janela do dono
			finally { ReleaseDC(hwnd, dc); }
		}

		/// <summary>Trilho, polegar e setas nas cores da barra escura (as mesmas do ScrollBarLook do Infragistics).</summary>
		internal static void DrawScrollBar(Graphics g, ScrollGeometry bar)
		{
			Rectangle b = bar.Bar;
			using (SolidBrush track = new SolidBrush(DarkPalette.ScrollTrack))
				g.FillRectangle(track, b);
			int start = bar.ThumbTop, end = bar.ThumbBottom;
			if (end > start)
				using (SolidBrush thumb = new SolidBrush(DarkPalette.ScrollThumb))
					g.FillRectangle(thumb, bar.Horizontal
						? new Rectangle(b.Left + start + 1, b.Top + 3, Math.Max(1, end - start - 2), Math.Max(1, b.Height - 6))
						: new Rectangle(b.Left + 3, b.Top + start + 1, Math.Max(1, b.Width - 6), Math.Max(1, end - start - 2)));
			if (bar.Arrow <= 0)
				return;
			using (SolidBrush arrow = new SolidBrush(DarkPalette.ScrollArrow))
			{
				int thickness = bar.Horizontal ? b.Height : b.Width;
				int size = Math.Max(2, Math.Min(thickness, bar.Arrow) / 4);
				if (bar.Horizontal)
				{
					int cy = b.Top + b.Height / 2, leftX = b.Left + bar.Arrow / 2, rightX = b.Right - bar.Arrow / 2;
					g.FillPolygon(arrow, new[] { new Point(leftX + size / 2, cy - size), new Point(leftX + size / 2, cy + size), new Point(leftX - size / 2 - 1, cy) });
					g.FillPolygon(arrow, new[] { new Point(rightX - size / 2, cy - size), new Point(rightX - size / 2, cy + size), new Point(rightX + size / 2 + 1, cy) });
				}
				else
				{
					int cx = b.Left + b.Width / 2, upY = b.Top + bar.Arrow / 2, downY = b.Bottom - bar.Arrow / 2;
					g.FillPolygon(arrow, new[] { new Point(cx - size, upY + size / 2), new Point(cx + size, upY + size / 2), new Point(cx, upY - size / 2 - 1) });
					g.FillPolygon(arrow, new[] { new Point(cx - size, downY - size / 2), new Point(cx + size, downY - size / 2), new Point(cx, downY + size / 2 + 1) });
				}
			}
		}
	}
}
