using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Status bar nativa (System.Windows.Forms.StatusBar; GxStatusBar da IDE): o controle comum do Windows ignora
	/// BackColor. Sem tema visual (SetWindowTheme "", "") ele aceita SB_SETBKCOLOR; o texto dos paineis Text passa a
	/// ser desenhado por nos (OwnerDraw + DrawItem) em cor clara. GxStatusBar sobrescreve OnDrawItem (API conferida):
	/// se o DrawItem nao chegar no repaint de prova, tudo volta como estava. Ver secao 4 do guia.
	/// </summary>
	internal sealed class StatusBarDarkStyler
	{
		private const int SB_SETBKCOLOR = 0x2001; // CCM_SETBKCOLOR
		private const int CLR_DEFAULT = unchecked((int)0xFF000000);

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
		private static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

		private readonly DarkUndo _undo;

		internal StatusBarDarkStyler(DarkUndo undo)
		{
			_undo = undo;
		}

		/// <summary>Aplica com repaint de prova; devolve true se ficou escura (false = devolvida ao original).</summary>
		internal bool Style(StatusBar bar)
		{
			if (bar.IsDisposed || !bar.IsHandleCreated)
				return false;
			List<Action> undo = new List<Action>();
			if (bar.SizingGrip)
			{
				// Alca de redimensionar sem tema = quadrado claro do Windows; antes do handle (o setter pode recriar).
				bar.SizingGrip = false;
				undo.Add(delegate { bar.SizingGrip = true; });
			}
			IntPtr hwnd = bar.Handle;
			Color back = DarkPalette.Back(SystemColors.Control);
			Color fore = DarkPalette.Fore(SystemColors.ControlText);

			HashSet<StatusBarPanel> ours = new HashSet<StatusBarPanel>();
			bool drawn = false;
			StatusBarDrawItemEventHandler draw = delegate (object sender, StatusBarDrawItemEventArgs e)
			{
				if (e.Panel == null || !ours.Contains(e.Panel))
					return;
				drawn = true;
				DrawPanel(e, back, fore, bar.Font);
			};

			try
			{
				SetWindowTheme(hwnd, "", "");
				undo.Add(delegate { SetWindowTheme(hwnd, null, null); });
				SendMessage(hwnd, SB_SETBKCOLOR, IntPtr.Zero, (IntPtr)ColorTranslator.ToWin32(back));
				undo.Add(delegate { SendMessage(hwnd, SB_SETBKCOLOR, IntPtr.Zero, (IntPtr)CLR_DEFAULT); });

				foreach (StatusBarPanel panel in bar.Panels)
					StylePanel(panel, ours, undo.Add);
				bar.DrawItem += draw;
				undo.Add(delegate { bar.DrawItem -= draw; });

				bar.Refresh(); // repaint sincrono: o WM_DRAWITEM chega aqui se a GxStatusBar repassa ao DrawItem
				if (ours.Count > 0 && !drawn)
				{
					RunReverse(undo);
					return false;
				}
			}
			catch
			{
				RunReverse(undo);
				return false;
			}

			foreach (Action a in undo)
				_undo.Record(bar, a);
			Styled.Add(bar, Marker);
			_ours[bar] = ours;
			_undo.Record(bar, delegate { Styled.Remove(bar); _ours.Remove(bar); bar.Invalidate(); });
			return true;
		}

		private readonly Dictionary<StatusBar, HashSet<StatusBarPanel>> _ours = new Dictionary<StatusBar, HashSet<StatusBarPanel>>();

		/// <summary>
		/// Timer: o GeneXus recria/redefine paineis depois (ao abrir a KB o painel do caminho voltou a Text com borda
		/// Sunken). Painel novo ou redefinido volta a ser nosso. Barra fechada sai do registro.
		/// </summary>
		internal void Maintain()
		{
			foreach (KeyValuePair<StatusBar, HashSet<StatusBarPanel>> entry in new List<KeyValuePair<StatusBar, HashSet<StatusBarPanel>>>(_ours))
			{
				StatusBar bar = entry.Key;
				if (bar.IsDisposed)
				{
					_ours.Remove(bar);
					continue;
				}
				bool changed = false;
				foreach (StatusBarPanel panel in bar.Panels)
					changed |= StylePanel(panel, entry.Value, a => _undo.Record(bar, a));
				if (changed)
					bar.Invalidate();
			}
		}

		/// <summary>
		/// Um painel: borda None (3D sem tema = realce branco); ForeColor publico (ProgressStatusBarPanel, desenhado pelo
		/// GeneXus) clareia; Text vira OwnerDraw nosso. Cada mudanca vai para <paramref name="record"/>. true se mudou.
		/// </summary>
		private static bool StylePanel(StatusBarPanel p, HashSet<StatusBarPanel> ours, Action<Action> record)
		{
			bool changed = false;
			StatusBarPanelBorderStyle border = p.BorderStyle;
			if (border != StatusBarPanelBorderStyle.None)
			{
				p.BorderStyle = StatusBarPanelBorderStyle.None;
				record(delegate { p.BorderStyle = border; });
				changed = true;
			}
			System.Reflection.PropertyInfo panelFore = p.GetType().GetProperty("ForeColor", typeof(Color));
			if (panelFore != null && panelFore.CanWrite && panelFore.CanRead)
			{
				Color was = (Color)panelFore.GetValue(p, null);
				Color light = DarkPalette.Fore(was.IsEmpty ? SystemColors.ControlText : was);
				if (light != was)
				{
					panelFore.SetValue(p, light, null);
					record(delegate { panelFore.SetValue(p, was, null); });
					changed = true;
				}
			}
			if (p.Style == StatusBarPanelStyle.Text)
			{
				p.Style = StatusBarPanelStyle.OwnerDraw;
				ours.Add(p);
				record(delegate { p.Style = StatusBarPanelStyle.Text; });
				changed = true;
			}
			return changed;
		}

		private static readonly object Marker = new object();
		private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<StatusBar, object> Styled =
			new System.Runtime.CompilerServices.ConditionalWeakTable<StatusBar, object>();

		/// <summary>true se a barra esta escura por este styler (o BackColor dela segue Control: o Windows ignora).</summary>
		internal static bool IsStyled(StatusBar bar)
		{
			object ignored;
			return bar != null && Styled.TryGetValue(bar, out ignored);
		}

		private static void DrawPanel(StatusBarDrawItemEventArgs e, Color back, Color fore, Font font)
		{
			Rectangle r = e.Bounds;
			using (SolidBrush b = new SolidBrush(back))
				e.Graphics.FillRectangle(b, r);
			Icon icon = e.Panel.Icon;
			if (icon != null)
			{
				int size = Math.Min(r.Height, 16);
				e.Graphics.DrawIcon(icon, new Rectangle(r.Left + 1, r.Top + (r.Height - size) / 2, size, size));
				r = new Rectangle(r.Left + size + 3, r.Top, Math.Max(0, r.Width - size - 3), r.Height);
			}
			TextRenderer.DrawText(e.Graphics, e.Panel.Text, font, r, fore, TextFlags(e.Panel.Alignment));
		}

		internal static TextFormatFlags TextFlags(HorizontalAlignment alignment)
		{
			TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix
				| TextFormatFlags.SingleLine;
			if (alignment == HorizontalAlignment.Center)
				return flags | TextFormatFlags.HorizontalCenter;
			return alignment == HorizontalAlignment.Right ? flags | TextFormatFlags.Right : flags | TextFormatFlags.Left;
		}

		private static void RunReverse(List<Action> undo)
		{
			for (int i = undo.Count - 1; i >= 0; i--)
				try { undo[i](); } catch { }
		}
	}
}
