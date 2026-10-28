using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Adapter fino sobre o AppStyling do Infragistics 11.1 que a IDE usa. Por REFLEXAO porque o Platform SDK
	/// do U11/U12 (de onde esses canais compilam) nao traz a DLL; no processo da IDE ela sempre esta carregada.
	/// API: StyleManager.Load(Stream) estatico; Office2007ColorTable.ColorScheme
	/// estatico (enum Default/Blue/Black/Silver). Ver secao 4 do guia.
	/// </summary>
	internal static class InfragisticsStyler
	{
		private const string StyleManagerType = "Infragistics.Win.AppStyling.StyleManager";
		private const string ColorTableType = "Infragistics.Win.Office2007ColorTable";

		/// <summary>Carrega o .isl (ja montado em memoria) como tema global de todos os controles Infragistics.</summary>
		internal static void Load(XmlDocument isl)
		{
			Type manager = FindType(StyleManagerType);
			MethodInfo load = manager.GetMethod("Load", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Stream) }, null);
			if (load == null)
				throw new MissingMethodException(StyleManagerType, "Load(Stream)");

			using (MemoryStream ms = new MemoryStream())
			{
				isl.Save(ms);
				ms.Position = 0;
				load.Invoke(null, new object[] { ms });
			}
		}

		/// <summary>Esquema de cor do Office 2007 usado pelos controles que nao pegam cor do .isl ("Silver" = o da IDE).</summary>
		internal static void SetOffice2007Scheme(string scheme)
		{
			Type table = FindType(ColorTableType);
			PropertyInfo prop = table.GetProperty("ColorScheme", BindingFlags.Public | BindingFlags.Static);
			if (prop == null)
				throw new MissingMemberException(ColorTableType, "ColorScheme");
			prop.SetValue(null, Enum.Parse(prop.PropertyType, scheme), null);
		}

		private static Type FindType(string fullName)
		{
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (!a.GetName().Name.StartsWith("Infragistics2.Win.v", StringComparison.OrdinalIgnoreCase))
					continue;
				Type t = a.GetType(fullName, false);
				if (t != null)
					return t;
			}
			throw new TypeLoadException("Infragistics nao carregado no processo: " + fullName);
		}
	}
}
