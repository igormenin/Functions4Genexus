using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Fase 2 do tema escuro: pinta as janelas abertas da IDE (ControlPainter), escurece a barra de titulo pelo DWM e
	/// acompanha janelas novas enquanto ligado. Janela/controle que aparece: gancho WH_CALLWNDPROC pinta no WM_SHOWWINDOW,
	/// antes do 1o quadro (o ShowWindow apaga o fundo de forma sincrona); WH_CBT (HCBT_CREATEWND) agenda uma pintura por
	/// Post para o resto; timer de 1,5 s como rede (Application.OpenForms + varredura do que
	/// escapou do ControlAdded, com rastro no Output); o mesmo timer reaplica o que o dono trocou depois
	/// (ControlPainter.Refresh). Desligar devolve tudo pelo DarkUndo. So na thread de UI. Ver secao 4 do guia.
	/// </summary>
	internal static class DarkWindows
	{
		// DWMWA_USE_IMMERSIVE_DARK_MODE: 20 no Windows 10 20H1+ e 11; 19 nas builds 1809-1909.
		private const int DarkModeAttribute = 20;
		private const int DarkModeAttributeOld = 19;
		private const int WH_CBT = 5;
		private const int WH_CALLWNDPROC = 4;
		private const int WM_SHOWWINDOW = 0x0018;
		private const int HCBT_CREATEWND = 3;

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

		private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr SetWindowsHookEx(int idHook, HookProc proc, IntPtr hMod, int threadId);

		[DllImport("user32.dll")]
		private static extern bool UnhookWindowsHookEx(IntPtr hook);

		[DllImport("user32.dll")]
		private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll")]
		private static extern int GetCurrentThreadId();

		private static DarkUndo _undo;
		private static ControlPainter _painter;
		private static System.Windows.Forms.Timer _timer;
		private static Action<string> _trace;
		private static readonly HashSet<string> _traced = new HashSet<string>(StringComparer.Ordinal);
		private static SynchronizationContext _ui;
		private static HookProc _hookProc; // campo: o delegate nao pode ser coletado enquanto o gancho existe
		private static IntPtr _hook = IntPtr.Zero;
		private static HookProc _showProc;
		private static IntPtr _showHook = IntPtr.Zero;
		private static bool _inShow;
		private static int _pending;

		internal static bool Active { get { return _undo != null; } }

		/// <summary>
		/// Liga: pinta tudo que esta aberto e passa a acompanhar janelas novas. <paramref name="trace"/> recebe cada
		/// controle que escapou do ControlAdded (uma vez por tipo). Devolve os objetos tratados.
		/// </summary>
		internal static int Start(Action<string> trace = null)
		{
			if (_undo == null)
			{
				_undo = new DarkUndo();
				_painter = new ControlPainter(_undo);
				_trace = trace;
				_traced.Clear();
				_timer = new System.Windows.Forms.Timer { Interval = 1500 };
				_timer.Tick += delegate { Tick(); };
				_timer.Start();
				InstallHook();
			}
			int painted = PaintOpenForms();
			_painter.Refresh();
			return painted;
		}

		/// <summary>Desliga: para de acompanhar e devolve as cores originais. Devolve quantas restauracoes falharam.</summary>
		internal static int Stop()
		{
			if (_undo == null)
				return 0;
			RemoveHook();
			_timer.Stop();
			_timer.Dispose();
			int failures = _undo.RestoreAll();
			_undo = null;
			_painter = null;
			_timer = null;
			_trace = null;
			return failures;
		}

		private static void Tick()
		{
			try
			{
				if (_undo == null)
					return;
				if (_hook == IntPtr.Zero)
					InstallHook(); // no startup pode nao haver SynchronizationContext ainda
				PaintOpenForms();
				_painter.Refresh();
			}
			catch { }
		}

		private static int PaintOpenForms()
		{
			int painted = 0;
			foreach (Form f in Snapshot())
			{
				if (f.IsDisposed || f.InvokeRequired)
					continue;
				if (!_undo.Seen(f))
				{
					painted += _painter.Paint(f);
					DarkTitleBar(f);
				}
				else
					Trace(_painter.Sweep(f));
			}
			return painted;
		}

		private static void Trace(List<string> escaped)
		{
			if (_trace == null)
				return;
			foreach (string e in escaped)
			{
				string key = e.Split(new[] { " sob " }, StringSplitOptions.None)[0];
				if (_traced.Add(key))
					try { _trace("pintado so na varredura (escapou do ControlAdded): " + e); } catch { }
			}
		}

		/// <summary>
		/// WH_CBT so da thread de UI: a cada janela criada (HCBT_CREATEWND) agenda UMA pintura por Post (mensagem
		/// postada roda antes do WM_PAINT), sem esperar o timer. Falha ao instalar = fica so o timer.
		/// </summary>
		private static void InstallHook()
		{
			InstallShowHook();
			try
			{
				_ui = SynchronizationContext.Current;
				if (_ui == null || _hook != IntPtr.Zero)
					return;
				_hookProc = OnCbt;
				_hook = SetWindowsHookEx(WH_CBT, _hookProc, IntPtr.Zero, GetCurrentThreadId());
			}
			catch { _hook = IntPtr.Zero; }
		}

		private static void RemoveHook()
		{
			try
			{
				if (_hook != IntPtr.Zero)
					UnhookWindowsHookEx(_hook);
				if (_showHook != IntPtr.Zero)
					UnhookWindowsHookEx(_showHook);
			}
			catch { }
			_hook = IntPtr.Zero;
			_hookProc = null;
			_showHook = IntPtr.Zero;
			_showProc = null;
		}

		/// <summary>
		/// WH_CALLWNDPROC da thread de UI: roda ANTES da janela tratar a mensagem. No WM_SHOWWINDOW (mostrar) a janela
		/// ainda nao apareceu e o ShowWindow vai apagar o fundo de forma sincrona: pintamos ali (janela nova inteira, ou o
		/// que ainda nao foi tratado sob o controle que aparece), sem o quadro claro do Post/timer.
		/// </summary>
		private static void InstallShowHook()
		{
			try
			{
				if (_showHook != IntPtr.Zero)
					return;
				_showProc = OnCallWndProc;
				_showHook = SetWindowsHookEx(WH_CALLWNDPROC, _showProc, IntPtr.Zero, GetCurrentThreadId());
			}
			catch { _showHook = IntPtr.Zero; }
		}

		/// <summary>CWPSTRUCT { LPARAM lParam; WPARAM wParam; UINT message; HWND hwnd } - deslocamentos por IntPtr.Size.</summary>
		internal static int MessageOffset { get { return 2 * IntPtr.Size; } }
		internal static int HwndOffset { get { return 3 * IntPtr.Size; } }

		private static IntPtr OnCallWndProc(int code, IntPtr wParam, IntPtr lParam)
		{
			try
			{
				// Barato primeiro: esta funcao roda em toda mensagem enviada da thread.
				if (code >= 0 && Marshal.ReadInt32(lParam, MessageOffset) == WM_SHOWWINDOW
					&& Marshal.ReadIntPtr(lParam, IntPtr.Size) != IntPtr.Zero && !_inShow && _undo != null)
					PaintOnShow(Marshal.ReadIntPtr(lParam, HwndOffset));
			}
			catch { } // gancho nunca derruba a IDE
			return CallNextHookEx(_showHook, code, wParam, lParam);
		}

		/// <summary>
		/// So JANELA (Form) ainda nao tratada, fora de recriacao de handle. agir no
		/// WM_SHOWWINDOW de controle FILHO durante RecreateHandle (TagCloud trocando o autocomplete de um
		/// ToolStripTextBox ao abrir a KB) fez o CreateHandle falhar com "Error creating window handle". Filho fica com
		/// ControlAdded/varredura/timer; nada de Refresh aqui.
		/// </summary>
		internal static bool ShouldPaintOnShow(Control c)
		{
			Form f = c as Form;
			if (f == null || f.IsDisposed || f.RecreatingHandle || !f.IsHandleCreated)
				return false;
			for (Control p = f.Parent; p != null; p = p.Parent)
				if (p.RecreatingHandle)
					return false;
			return true;
		}

		private static void PaintOnShow(IntPtr hwnd)
		{
			Form f = Control.FromHandle(hwnd) as Form;
			if (f == null || !ShouldPaintOnShow(f) || _undo.Seen(f))
				return;
			_inShow = true;
			try
			{
				_painter.Paint(f);
				DarkTitleBar(f);
			}
			finally { _inShow = false; }
		}

		private static IntPtr OnCbt(int code, IntPtr wParam, IntPtr lParam)
		{
			try
			{
				if (code == HCBT_CREATEWND && _ui != null && Interlocked.Exchange(ref _pending, 1) == 0)
					_ui.Post(delegate { Interlocked.Exchange(ref _pending, 0); Tick(); }, null);
			}
			catch { } // gancho nunca derruba a IDE
			return CallNextHookEx(_hook, code, wParam, lParam);
		}

		/// <summary>Janelas abertas da thread atual (copia: a colecao muda enquanto pinta).</summary>
		internal static List<Form> Snapshot()
		{
			List<Form> forms = new List<Form>();
			foreach (Form f in Application.OpenForms)
				forms.Add(f);
			return forms;
		}

		private static void DarkTitleBar(Form f)
		{
			if (!f.TopLevel)
				return;
			if (!f.IsHandleCreated)
			{
				EventHandler later = null;
				later = delegate { f.HandleCreated -= later; if (_undo != null) DarkTitleBar(f); };
				f.HandleCreated += later;
				return;
			}
			IntPtr hwnd = f.Handle;
			if (!SetImmersiveDark(hwnd, true))
				return;
			_undo.Record(f, delegate { SetImmersiveDark(hwnd, false); });
		}

		private static bool SetImmersiveDark(IntPtr hwnd, bool on)
		{
			try
			{
				int value = on ? 1 : 0;
				return DwmSetWindowAttribute(hwnd, DarkModeAttribute, ref value, sizeof(int)) == 0
					|| DwmSetWindowAttribute(hwnd, DarkModeAttributeOld, ref value, sizeof(int)) == 0;
			}
			catch { return false; } // Windows sem DWM/atributo: barra de titulo fica como esta
		}
	}
}
