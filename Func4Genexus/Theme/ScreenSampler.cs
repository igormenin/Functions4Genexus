using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Func4Genexus.Theme
{
	/// <summary>O que a tela mostra na area PROPRIA de um controle (sem os filhos visiveis).</summary>
	internal sealed class ScreenSample
	{
		internal int Samples;
		internal double LightFraction;
		/// <summary>Retangulo (relativo ao controle) que cobre os pontos claros; vazio se nenhum.</summary>
		internal Rectangle LightBounds;
		internal Color Typical;
		/// <summary>A parte clara e de outra janela/aba por cima (WindowFromPoint nao cai no controle): nao conta.</summary>
		internal bool Covered;
	}

	/// <summary>
	/// Inspetor do tema escuro: le o pixel REAL da tela (o BackColor nem sempre e o que aparece: DevExpress, cabecalho
	/// desenhado pelo GeneXus, MonacoPane). Captura uma vez por janela (CopyFromScreen + LockBits) e amostra em grade.
	/// Analise PURA (Analyze), testavel com pixel injetado. Ver secao 4 do guia.
	/// </summary>
	internal sealed class ScreenSampler
	{
		private const int MaxSamples = 600;
		private readonly int[] _argb;
		private readonly int _width;
		private readonly int _height;
		private readonly Point _origin;

		private ScreenSampler(int[] argb, int width, int height, Point origin)
		{
			_argb = argb;
			_width = width;
			_height = height;
			_origin = origin;
		}

		/// <summary>Captura a area da tela (coordenadas de tela). null se nao der (sem desktop, area vazia).</summary>
		internal static ScreenSampler Capture(Rectangle screen)
		{
			if (screen.Width <= 0 || screen.Height <= 0)
				return null;
			try
			{
				using (Bitmap bmp = new Bitmap(screen.Width, screen.Height, PixelFormat.Format32bppArgb))
				{
					using (Graphics g = Graphics.FromImage(bmp))
						g.CopyFromScreen(screen.Location, Point.Empty, screen.Size);
					BitmapData data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
					try
					{
						int[] argb = new int[screen.Width * screen.Height];
						for (int y = 0; y < screen.Height; y++)
							Marshal.Copy(data.Scan0 + y * data.Stride, argb, y * screen.Width, screen.Width);
						return new ScreenSampler(argb, screen.Width, screen.Height, screen.Location);
					}
					finally { bmp.UnlockBits(data); }
				}
			}
			catch { return null; }
		}

		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(Point p);

		/// <summary>Controle WinForms DESTE processo sob o ponto de tela (null se outra janela/processo).</summary>
		internal static Control ControlAt(Point screen)
		{
			try { return Control.FromChildHandle(WindowFromPoint(screen)); }
			catch { return null; }
		}

		/// <summary>Cor do pixel capturado no ponto de tela; Color.Empty fora da captura.</summary>
		internal Color Pixel(Point screen)
		{
			int x = screen.X - _origin.X, y = screen.Y - _origin.Y;
			return x < 0 || y < 0 || x >= _width || y >= _height ? Color.Empty : Color.FromArgb(_argb[y * _width + x]);
		}

		/// <summary>
		/// Area cliente do controle na tela, sem os filhos visiveis (<paramref name="whole"/>: com os filhos, para o
		/// navegador, cujos filhos sao a propria pagina). Marca Covered se o centro da parte clara e de outro controle.
		/// </summary>
		internal ScreenSample Sample(Control c, bool whole)
		{
			Rectangle own = c.RectangleToScreen(c.ClientRectangle);
			List<Rectangle> children = new List<Rectangle>();
			if (!whole)
				foreach (Control child in c.Controls)
					if (child.Visible)
						children.Add(c.RectangleToScreen(child.Bounds));
			ScreenSample s = Sample(own, children);
			if (!s.LightBounds.IsEmpty)
			{
				Point center = c.PointToScreen(new Point(s.LightBounds.X + s.LightBounds.Width / 2, s.LightBounds.Y + s.LightBounds.Height / 2));
				Control hit = Control.FromChildHandle(WindowFromPoint(center));
				s.Covered = hit == null || !(hit == c || c.Contains(hit));
			}
			return s;
		}

		/// <summary>Amostra a area de tela <paramref name="own"/>, sem <paramref name="children"/> (tudo em coordenadas de tela).</summary>
		internal ScreenSample Sample(Rectangle own, IList<Rectangle> children)
		{
			Rectangle local = own;
			local.Offset(-_origin.X, -_origin.Y);
			local.Intersect(new Rectangle(0, 0, _width, _height));
			List<Rectangle> exclude = new List<Rectangle>();
			foreach (Rectangle r in children)
			{
				Rectangle l = r;
				l.Offset(-_origin.X, -_origin.Y);
				exclude.Add(l);
			}
			ScreenSample s = Analyze((x, y) => Color.FromArgb(_argb[y * _width + x]), local, exclude);
			if (s.Samples > 0 && !s.LightBounds.IsEmpty)
				s.LightBounds.Offset(-(own.X - _origin.X), -(own.Y - _origin.Y)); // relativo ao controle
			return s;
		}

		/// <summary>
		/// PURO: grade de pontos na area, pulando os retangulos excluidos; claro = brilho HSL &gt; 0,6 (a mesma regra de
		/// DarkPalette.IsLight). Passo cresce para nao passar de <see cref="MaxSamples"/> pontos.
		/// </summary>
		internal static ScreenSample Analyze(Func<int, int, Color> pixel, Rectangle area, IList<Rectangle> exclude)
		{
			ScreenSample result = new ScreenSample { LightBounds = Rectangle.Empty, Typical = Color.Empty };
			if (area.Width <= 0 || area.Height <= 0)
				return result;
			int step = Math.Max(4, (int)Math.Ceiling(Math.Sqrt((double)area.Width * area.Height / MaxSamples)));
			int light = 0, minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
			long r = 0, g = 0, b = 0, lr = 0, lg = 0, lb = 0;
			for (int y = area.Top + step / 2; y < area.Bottom; y += step)
				for (int x = area.Left + step / 2; x < area.Right; x += step)
				{
					if (Inside(exclude, x, y))
						continue;
					Color c = pixel(x, y);
					result.Samples++;
					r += c.R; g += c.G; b += c.B;
					if (!DarkPalette.IsLight(c) || !SolidLight(pixel, area, x, y))
						continue;
					light++;
					lr += c.R; lg += c.G; lb += c.B;
					minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
				}
			if (result.Samples == 0)
				return result;
			result.LightFraction = (double)light / result.Samples;
			if (light > 0)
			{
				result.LightBounds = Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
				result.Typical = Color.FromArgb((int)(lr / light), (int)(lg / light), (int)(lb / light));
			}
			else
				result.Typical = Color.FromArgb((int)(r / result.Samples), (int)(g / result.Samples), (int)(b / result.Samples));
			return result;
		}

		private const int Neighbor = 3;

		/// <summary>Area clara de verdade: os vizinhos a 3 px tambem sao claros (traco de letra clara nao conta).</summary>
		private static bool SolidLight(Func<int, int, Color> pixel, Rectangle area, int x, int y)
		{
			int[][] around = { new[] { Neighbor, 0 }, new[] { -Neighbor, 0 }, new[] { 0, Neighbor }, new[] { 0, -Neighbor } };
			foreach (int[] d in around)
			{
				int nx = x + d[0], ny = y + d[1];
				if (area.Contains(nx, ny) && !DarkPalette.IsLight(pixel(nx, ny)))
					return false;
			}
			return true;
		}

		private static bool Inside(IList<Rectangle> rects, int x, int y)
		{
			foreach (Rectangle r in rects)
				if (r.Contains(x, y))
					return true;
			return false;
		}
	}
}
