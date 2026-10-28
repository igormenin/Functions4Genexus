using System;
using System.IO;

namespace Func4Genexus.Theme
{
	internal static class UpdateState
	{
		/// <summary>%LOCALAPPDATA%\GxPropertiesTool (criada se faltar). Troque o nome da pasta.</summary>
		internal static string Dir()
		{
			string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Func4Genexus");
			try { if (!Directory.Exists(d)) Directory.CreateDirectory(d); } catch { }
			return d;
		}
	}
}
