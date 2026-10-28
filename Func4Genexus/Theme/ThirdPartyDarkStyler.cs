using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Tema escuro do editor (Actipro SyntaxEditor 4.0.273) e do painel Properties (VisualHint SmartPropertyGrid 11),
	/// por REFLEXAO: o SDK do U11/U12 nao traz essas DLLs e os nomes abaixo foram conferidos por reflexao nos quatro
	/// upgrades (secao 4 do guia). Cada valor trocado vai para o DarkUndo. Membro ausente = pula (nunca lanca).
	/// </summary>
	internal sealed class ThirdPartyDarkStyler
	{
		private const string SolidFillType = "ActiproSoftware.Drawing.SolidColorBackgroundFill";
		private const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance;
		private const BindingFlags Protected = BindingFlags.NonPublic | BindingFlags.Instance;

		private readonly DarkUndo _undo;

		internal ThirdPartyDarkStyler(DarkUndo undo)
		{
			_undo = undo;
		}

		private readonly List<object> _editors = new List<object>();
		private readonly Dictionary<object, Dictionary<string, KeptColor>> _kept =
			new Dictionary<object, Dictionary<string, KeptColor>>(DarkUndo.ByReference.Instance);

		/// <summary>
		/// SyntaxEditor: fundo/texto do controle, cores e fundos do renderer, estilos de sintaxe. O editor fica na lista
		/// do Refresh: a linguagem chega depois do controle e o GeneXus reaplica a configuracao dos estilos.
		/// </summary>
		internal void StyleEditor(object editor)
		{
			_undo.Mark(editor);
			SetColor(editor, "BackColor", DarkPalette.Back);
			SetColor(editor, "ForeColor", DarkPalette.Fore);

			StyleRenderer(Get(editor, "RendererResolved"));
			_editors.Add(editor);
			IComponent component = editor as IComponent;
			if (component != null)
				component.Disposed += delegate { _editors.Remove(editor); };
			_undo.Record(editor, delegate { _editors.Remove(editor); });
			HookLanguageEvents(editor);
			StyleLanguage(EditorLanguage(editor), new HashSet<object>(DarkUndo.ByReference.Instance));
			Invalidate(editor);
		}

		/// <summary>
		/// Sem esperar o timer: SyntaxEditor.DocumentChanged (EventHandler) e Document.SyntaxLanguageChanged
		/// (SyntaxLanguageEventHandler), conferidos na API do Actipro 4.0.273, reaplicam na hora.
		/// </summary>
		private void HookLanguageEvents(object editor)
		{
			object[] document = { null };
			Action onLanguage = delegate { Refresh(); };
			Action hookDocument = delegate
			{
				object doc = Get(editor, "Document");
				if (doc == null || ReferenceEquals(doc, document[0]))
					return;
				document[0] = doc;
				Hook(doc, "SyntaxLanguageChanged", onLanguage);
			};
			hookDocument();
			Hook(editor, "DocumentChanged", delegate { hookDocument(); Refresh(); });
		}

		/// <summary>Assina um evento de qualquer tipo de delegate (object, XxxEventArgs) e registra o desassinar.</summary>
		private void Hook(object target, string eventName, Action action)
		{
			try
			{
				EventInfo ev = target.GetType().GetEvent(eventName, Instance);
				if (ev == null)
					return;
				Delegate handler = Delegate.CreateDelegate(ev.EventHandlerType, new Relay(action), Relay.FireMethod);
				ev.AddEventHandler(target, handler);
				_undo.Record(target, delegate { ev.RemoveEventHandler(target, handler); });
			}
			catch { } // assinatura inesperada: fica o timer
		}

		/// <summary>Alvo de delegate (object, EventArgs): por contravariancia serve para qualquer XxxEventArgs.</summary>
		private sealed class Relay
		{
			// Pelo delegate, nao pelo nome do metodo.
			internal static readonly MethodInfo FireMethod = new Action<object, EventArgs>(new Relay(null).Fire).Method;
			private readonly Action _action;
			internal Relay(Action action) { _action = action; }
			public void Fire(object sender, EventArgs e)
			{
				try { _action(); }
				catch { }
			}
		}

		/// <summary>Timer: reaplica os estilos de todos os editores abertos (linguagem nova, estilo resetado pelo dono).</summary>
		internal void Refresh()
		{
			HashSet<object> visited = new HashSet<object>(DarkUndo.ByReference.Instance);
			bool changed = false;
			foreach (object editor in _editors.ToArray())
				changed |= StyleLanguage(EditorLanguage(editor), visited);
			if (changed)
				foreach (object editor in _editors.ToArray())
					Invalidate(editor); // linguagem pode ser compartilhada: todos repintam
			foreach (object grid in _grids.ToArray())
				if (KeepInfragisticsAppearances(grid))
					Invalidate(grid);
			foreach (object html in _htmlEditors.ToArray())
				ApplyHtmlDarkSheet(html);
		}

		private const string HtmlEditorType = "Artech.Editors.Common.Web.HtmlEditor";
		internal const string HtmlSheetId = "gxptDarkTheme";

		// So o <head>: o Wiki grava apenas body.innerHTML (EditorView: GetBody().GetInnerHTML()). Sem !important,
		// para nao apagar cor posta pelo usuario no texto.
		internal static string HtmlDarkCss { get { return ThemePalette.Current.HtmlCss; } }

		private readonly List<object> _htmlEditors = new List<object>();
		private readonly Dictionary<object, object> _htmlSheets = new Dictionary<object, object>(DarkUndo.ByReference.Instance);

		/// <summary>true se e o editor HTML do GeneXus (Help/Documentation em modo Edit, sobre o NetRix/MSHTML).</summary>
		internal static bool IsHtmlEditor(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if (t.FullName == HtmlEditorType)
					return true;
			return false;
		}

		/// <summary>
		/// Help e Documentation (modo Edit): folha de estilo escura no &lt;head&gt; do documento MSHTML
		/// (IHTMLDocument2.createStyleSheet - o proprio GeneXus poe um &lt;style&gt; no head em FixHiddenScroll).
		/// Reaplica nos campos ReadyStateComplete/DocumentCreated do editor e no timer; desligar esvazia as folhas.
		/// </summary>
		internal void StyleHtmlEditor(object editor)
		{
			if (!IsHtmlEditor(editor.GetType()) || _htmlEditors.Contains(editor))
				return;
			_htmlEditors.Add(editor);
			IComponent component = editor as IComponent;
			if (component != null)
				component.Disposed += delegate { _htmlEditors.Remove(editor); };
			_undo.Record(editor, delegate { _htmlEditors.Remove(editor); });
			EventHandler again = delegate { ApplyHtmlDarkSheet(editor); };
			CombineField(editor, "ReadyStateComplete", again);
			CombineField(editor, "DocumentCreated", again);
			ApplyHtmlDarkSheet(editor);
		}

		private void ApplyHtmlDarkSheet(object editor)
		{
			try
			{
				object doc = Get(editor, "Document");
				if (doc == null || _htmlSheets.ContainsKey(doc))
					return;
				// O wrapper COM pode mudar a cada leitura: o id na pagina e a prova de que a folha ja esta la.
				if (doc.GetType().InvokeMember("getElementById", BindingFlags.InvokeMethod, null, doc, new object[] { HtmlSheetId }) != null)
					return;
				object sheet = doc.GetType().InvokeMember("createStyleSheet", BindingFlags.InvokeMethod, null, doc, new object[] { "", 0 });
				if (sheet == null)
					return;
				sheet.GetType().InvokeMember("cssText", BindingFlags.SetProperty, null, sheet, new object[] { HtmlDarkCss });
				object owner = sheet.GetType().InvokeMember("owningElement", BindingFlags.GetProperty, null, sheet, null);
				if (owner != null)
					owner.GetType().InvokeMember("id", BindingFlags.SetProperty, null, owner, new object[] { HtmlSheetId });
				_htmlSheets[doc] = sheet;
				_undo.Record(editor, delegate
				{
					_htmlSheets.Remove(doc);
					sheet.GetType().InvokeMember("cssText", BindingFlags.SetProperty, null, sheet, new object[] { "" });
					if (owner != null)
						owner.GetType().InvokeMember("id", BindingFlags.SetProperty, null, owner, new object[] { "" });
				});
			}
			catch { } // documento ainda carregando: o ReadyStateComplete/timer tentam de novo
		}

		/// <summary>Campo publico de delegate (o GeneXus usa campo, nao evento): soma o nosso e registra o tirar.</summary>
		private void CombineField(object target, string field, EventHandler handler)
		{
			try
			{
				FieldInfo f = target.GetType().GetField(field, Instance);
				if (f == null || f.FieldType != typeof(EventHandler))
					return;
				f.SetValue(target, Delegate.Combine((Delegate)f.GetValue(target), handler));
				_undo.Record(target, delegate { f.SetValue(target, Delegate.Remove((Delegate)f.GetValue(target), handler)); });
			}
			catch { }
		}

		private static object EditorLanguage(object editor)
		{
			return Get(Get(editor, "Document"), "Language");
		}

		/// <summary>
		/// Linguagem e suas filhas (SyntaxLanguage.ChildLanguages: o token de uma linguagem filha traz o estilo dela;
		/// DynamicSyntaxLanguage.GetHighlightingStyle devolve token.HighlightingStyle), com o BackColor da linguagem.
		/// true se alguma cor mudou.
		/// </summary>
		internal bool StyleLanguage(object language, HashSet<object> visited)
		{
			if (language == null || !visited.Add(language))
				return false;
			bool changed = Keep(language, "BackColor", DarkPalette.Back);
			changed |= StyleHighlighting(Get(language, "HighlightingStyles") as IEnumerable);
			IEnumerable children = Get(language, "ChildLanguages") as IEnumerable;
			if (children != null)
				foreach (object child in children)
					changed |= StyleLanguage(child, visited);
			return changed;
		}

		/// <summary>Renderer do editor (VisualStudio2005SyntaxEditorRenderer): cores e fundos das areas e margens.</summary>
		internal void StyleRenderer(object renderer)
		{
			if (renderer == null || _undo.Seen(renderer))
				return;
			_undo.Mark(renderer); // renderer pode ser compartilhado entre editores: trata uma vez
			foreach (PropertyInfo p in renderer.GetType().GetProperties(Instance))
			{
				if (!p.CanWrite || p.GetIndexParameters().Length > 0)
					continue;
				if (p.PropertyType == typeof(Color))
					SetColor(renderer, p, ColorRuleByName(p.Name));
				else if (p.Name.EndsWith("BackgroundFill", StringComparison.Ordinal))
					SetFill(renderer, p);
			}
		}

		/// <summary>
		/// Estilos de sintaxe (palavra-chave, comentario...): texto clareia, fundo escurece (o GeneXus usa
		/// SystemColors.Window como fundo: o retangulo branco atras de cada token). Mantidos: true se mudou cor.
		/// </summary>
		internal bool StyleHighlighting(IEnumerable styles)
		{
			if (styles == null)
				return false;
			bool changed = false;
			foreach (object style in styles)
				if (style != null)
				{
					changed |= Keep(style, "ForeColor", DarkPalette.Fore);
					changed |= Keep(style, "BackColor", DarkPalette.Back);
				}
			return changed;
		}

		/// <summary>Cor mantida (KeptColor): a 1a vez registra o desfazer; depois so reescurece se o dono trocou.</summary>
		private bool Keep(object target, string property, Func<Color, Color> rule)
		{
			try
			{
				Dictionary<string, KeptColor> byProperty;
				if (!_kept.TryGetValue(target, out byProperty))
					_kept[target] = byProperty = new Dictionary<string, KeptColor>(StringComparer.Ordinal);
				KeptColor kept;
				if (!byProperty.TryGetValue(property, out kept))
				{
					PropertyInfo p = target.GetType().GetProperty(property, Instance);
					if (p == null || !p.CanWrite || p.PropertyType != typeof(Color))
						return false;
					kept = new KeptColor(() => (Color)p.GetValue(target, null), v => p.SetValue(target, v, null), rule);
					byProperty[property] = kept;
					_undo.Record(target, kept.Restore);
					_undo.Record(target, delegate { _kept.Remove(target); });
				}
				return kept.Apply();
			}
			catch { return false; }
		}

		/// <summary>
		/// Infragistics: aparencias com cor clara EXPLICITA (posta pelo codigo do dono) - a do controle e, no grid, a do
		/// layout e das linhas/celulas. Cor vazia (resolvida pelo .isl) fica como esta.
		/// </summary>
		internal void StyleInfragisticsAppearances(object control)
		{
			if (!_grids.Contains(control))
			{
				_grids.Add(control);
				IComponent component = control as IComponent;
				if (component != null)
					component.Disposed += delegate { _grids.Remove(control); };
				_undo.Record(control, delegate { _grids.Remove(control); });
			}
			if (KeepInfragisticsAppearances(control))
				Invalidate(control);
		}

		private readonly List<object> _grids = new List<object>();

		/// <summary>
		/// Mantidas (KeptColor): o Select Object dialog poe layout=White e linha=Window DEPOIS da pintura (relatorio
		/// 23:10:41); o Refresh reescurece. Aparencia trocada por outra: a nova entra no proximo Refresh.
		/// </summary>
		private bool KeepInfragisticsAppearances(object control)
		{
			object layout = Get(control, "DisplayLayout");
			object over = Get(layout, "Override");
			object[] appearances =
			{
				Get(control, "Appearance"), Get(layout, "Appearance"), Get(over, "RowAppearance"),
				Get(over, "RowAlternateAppearance"), Get(over, "CellAppearance"),
			};
			bool changed = false;
			foreach (object appearance in appearances)
			{
				if (appearance == null)
					continue;
				changed |= Keep(appearance, "BackColor", DarkPalette.Back);
				changed |= Keep(appearance, "BackColor2", DarkPalette.Back);
				changed |= Keep(appearance, "ForeColor", DarkPalette.Fore);
			}
			return changed;
		}

		/// <summary>SmartPropertyGrid: cores publicas do grid e o gradiente das categorias do draw manager.</summary>
		internal void StyleGrid(object grid)
		{
			_undo.Mark(grid);
			foreach (PropertyInfo p in grid.GetType().GetProperties(Instance))
				if (p.CanWrite && p.PropertyType == typeof(Color) && p.GetIndexParameters().Length == 0
					&& p.DeclaringType.FullName.StartsWith("VisualHint.", StringComparison.Ordinal))
					SetColor(grid, p, ColorRuleByName(p.Name));
			SetColor(grid, "BackColor", DarkPalette.Back);
			SetColor(grid, "ForeColor", DarkPalette.Fore);
			// Linhas entre propriedades: Back(GridColor) da 80,80,80, claro demais no fundo 28.
			SetColor(grid, "GridColor", delegate { return DarkPalette.Line; });
			StyleGridBorder(grid as System.Windows.Forms.Control);

			object manager = Get(grid, "DrawManager");
			if (manager != null && !_undo.Seen(manager))
			{
				_undo.Mark(manager);
				SetPair(manager, "SetCategoryBackgroundColors", "CategoryLeftBackgroundColor", "CategoryRightBackgroundColor");
				SetPair(manager, "SetSubCategoryBackgroundColors", "SubCategoryLeftBackgroundColor", "SubCategoryRightBackgroundColor");
			}
			Invalidate(grid);
		}

		// Janela interna do SmartPropertyGrid: borda nao-cliente com Color.White fixo e barra nativa.
		internal const string InternalGridType = "VisualHint.SmartPropertyGrid.InternalGrid";

		private void StyleGridBorder(System.Windows.Forms.Control grid)
		{
			if (grid == null)
				return;
			foreach (System.Windows.Forms.Control child in grid.Controls)
				if (child.GetType().FullName == InternalGridType && !_undo.Seen(child))
				{
					_undo.Mark(child);
					_undo.Record(child, NcBorderPainter.Attach(child, DarkPalette.Line, scrollBar: true));
				}
		}

		// Tema do Monaco no escuro: base vs-dark + as regras do OutputPane.html (mesmos tokens, cores claras).
		internal const string MonacoDarkScript =
			"(function(){if(!window.monaco||!monaco.editor)return 'sem-monaco';" +
			"monaco.editor.defineTheme('gxptDark',{base:'vs-dark',inherit:true,rules:[" +
			"{token:'custom-info',foreground:'9A9A9A'},{token:'custom-error',foreground:'FF6B6B',fontStyle:'bold'}," +
			"{token:'custom-warning',foreground:'FFB347'},{token:'custom-infomsg',foreground:'A5FF00'}," +
			"{token:'start-section',foreground:'6FCF6F'}],colors:{}});" +
			"monaco.editor.setTheme('gxptDark');return 'ok';})()";

		// Volta ao tema da pagina: outputTheme no Output (tem ChangeTheme), vs nas outras.
		internal const string MonacoLightScript =
			"(function(){if(!window.monaco||!monaco.editor)return 'sem-monaco';" +
			"monaco.editor.setTheme(typeof ChangeTheme==='function'?'outputTheme':'vs');return 'ok';})()";

		/// <summary>
		/// Navegador do GeneXus (ChromiumBrowser.ChromiumBaseBrowser): se a pagina tem Monaco, poe o tema escuro e
		/// reaplica a cada DocumentCompleted. Pagina sem Monaco: o script nao faz nada.
		/// </summary>
		internal void StyleWeb(object browser)
		{
			StyleWeb(browser, false);
		}

		// Start Page: pagina do proprio GeneXus sem tema escuro. Inversao de cor no <html> (fundo branco vira ~#1a1a1a,
		// texto escuro vira claro, matiz dos links fica pelo hue-rotate); imagens/video voltam ao normal. So no
		// navegador da janela "Start Page" (nunca no preview do WWP, que tem de mostrar o design real).
		internal const string StartPageDarkScript = "(function(){var id='gxptDarkStart';if(document.getElementById(id))return 'ja';" +
			"var s=document.createElement('style');s.id=id;s.textContent='html{filter:invert(0.9) hue-rotate(180deg);background:#fff;}" +
			"img,svg,video,picture,canvas,iframe{filter:invert(1) hue-rotate(180deg);}';" +
			"(document.head||document.documentElement).appendChild(s);return 'ok';})()";

		internal const string StartPageLightScript = "(function(){var s=document.getElementById('gxptDarkStart');" +
			"if(s)s.parentNode.removeChild(s);return 'ok';})()";

		/// <summary>true se o navegador esta dentro da janela "Start Page" do GeneXus (nome do controle, relatorio).</summary>
		internal static bool InStartPage(System.Windows.Forms.Control c)
		{
			for (System.Windows.Forms.Control p = c; p != null; p = p.Parent)
				if (p.Name == "Start Page" && (p.GetType().FullName ?? "").EndsWith(".WebBrowserToolWindow", StringComparison.Ordinal))
					return true;
			return false;
		}

		internal void StyleWeb(object browser, bool startPage)
		{
			_undo.Mark(browser);
			MethodInfo eval = browser.GetType().GetMethod("EvaluateCodeAsync", Instance, null, new[] { typeof(string) }, null);
			EventInfo loaded = browser.GetType().GetEvent("DocumentCompleted", Instance);
			if (eval == null)
				return;

			Action<string> run = delegate (string script)
			{
				try
				{
					System.Threading.Tasks.Task t = eval.Invoke(browser, new object[] { script }) as System.Threading.Tasks.Task;
					if (t != null)
						t.ContinueWith(done => { Exception ignored = done.Exception; }); // pagina ainda carregando: tenta no DocumentCompleted
				}
				catch { }
			};
			Action dark = delegate { run(MonacoDarkScript); if (startPage) run(StartPageDarkScript); };
			dark();
			_undo.Record(browser, delegate { run(MonacoLightScript); if (startPage) run(StartPageLightScript); });

			if (loaded != null && loaded.EventHandlerType == typeof(EventHandler))
			{
				EventHandler again = delegate { dark(); };
				loaded.AddEventHandler(browser, again);
				_undo.Record(browser, delegate { loaded.RemoveEventHandler(browser, again); });
			}
		}

		/// <summary>Barra de rolagem desenhada pelo Infragistics (UltraTree.ScrollBarLook, UltraGrid.DisplayLayout.ScrollBarLook).</summary>
		internal void StyleInfragisticsScrollBars(object control)
		{
			_undo.Mark(control);
			object look = Get(control, "ScrollBarLook") ?? Get(Get(control, "DisplayLayout"), "ScrollBarLook");
			if (look == null || _undo.Seen(look))
				return;
			_undo.Mark(look);
			SetAppearance(look, "Appearance", DarkPalette.ScrollTrack, Color.Empty);
			SetAppearance(look, "TrackAppearance", DarkPalette.ScrollTrack, Color.Empty);
			SetAppearance(look, "ButtonAppearance", DarkPalette.ScrollTrack, DarkPalette.ScrollTrack);
			SetAppearance(look, "ThumbAppearance", DarkPalette.ScrollThumb, DarkPalette.ScrollThumb);
			Invalidate(control);
		}

		/// <summary>
		/// Crownwood (MagicLocalLibrary): TabControl da faixa de partes (texto inativo, hot-track, botoes, tracos claros
		/// _backLightLight/_backLight) e TabPage.ColorTab (fundo da aba selecionada, padrao White).
		/// </summary>
		internal void StyleCrownwood(object control)
		{
			if (!IsCrownwood(control.GetType()))
				return;

			SetColor(control, "ColorTab", DarkPalette.Back);
			SetColor(control, "TextColor", DarkPalette.Fore);
			SetColor(control, "TextInactiveColor", DarkPalette.Fore);
			SetColor(control, "HotTextColor", DarkPalette.Fore);
			SetColor(control, "ButtonActiveColor", DarkPalette.Fore);
			SetColor(control, "ButtonInactiveColor", DarkPalette.Fore);

			System.Windows.Forms.Control c = control as System.Windows.Forms.Control;
			if (c != null)
			{
				SetField(control, "_backLightLight", DarkPalette.Lift(c.BackColor, 0.16));
				SetField(control, "_backLight", DarkPalette.Lift(c.BackColor, 0.08));
			}
			Invalidate(control);
		}

		/// <summary>O tipo ou uma de suas bases e do Crownwood (MagicLocalLibrary).</summary>
		internal static bool IsCrownwood(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if ((t.FullName ?? "").StartsWith("Crownwood.", StringComparison.Ordinal))
					return true;
			return false;
		}

		private void SetField(object target, string field, Color value)
		{
			try
			{
				FieldInfo f = null;
				for (Type t = target.GetType(); t != null && f == null; t = t.BaseType)
					f = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
				if (f == null || f.FieldType != typeof(Color))
					return;
				Color original = (Color)f.GetValue(target);
				if (original == value)
					return;
				f.SetValue(target, value);
				_undo.Record(target, delegate { f.SetValue(target, original); });
			}
			catch { }
		}

		private void SetAppearance(object look, string property, Color back, Color border)
		{
			object appearance = Get(look, property);
			if (appearance == null)
				return;
			SetColor(appearance, "BackColor", delegate { return back; });
			SetColor(appearance, "BackColor2", delegate { return back; });
			SetColor(appearance, "ForeColor", delegate { return DarkPalette.ScrollArrow; });
			if (!border.IsEmpty)
				SetColor(appearance, "BorderColor", delegate { return border; });
		}

		/// <summary>Nome com Fore/Text = cor de texto; o resto (Back, Grid, Border, Highlight...) = fundo/linha.</summary>
		internal static Func<Color, Color> ColorRuleByName(string name)
		{
			return name.IndexOf("Fore", StringComparison.Ordinal) >= 0 || name.IndexOf("Text", StringComparison.Ordinal) >= 0
				? (Func<Color, Color>)DarkPalette.Fore
				: DarkPalette.Back;
		}

		private void SetColor(object target, string property, Func<Color, Color> rule)
		{
			PropertyInfo p = target.GetType().GetProperty(property, Instance);
			if (p != null && p.CanWrite && p.PropertyType == typeof(Color))
				SetColor(target, p, rule);
		}

		private void SetColor(object target, PropertyInfo p, Func<Color, Color> rule)
		{
			try
			{
				Color original = (Color)p.GetValue(target, null);
				Color dark = rule(original);
				if (dark == original)
					return;
				p.SetValue(target, dark, null);
				_undo.Record(target, delegate { p.SetValue(target, original, null); });
			}
			catch { }
		}

		/// <summary>Fundo do renderer: cor lisa escura (a do fill original, se for liso; senao a da janela).</summary>
		private void SetFill(object renderer, PropertyInfo p)
		{
			try
			{
				object original = p.GetValue(renderer, null);
				Color baseColor = SystemColors.Window;
				object c = Get(original, "Color");
				if (c is Color)
					baseColor = (Color)c;
				Color dark = DarkPalette.Back(baseColor);
				if (original != null && dark == baseColor)
					return;

				Type solid = p.PropertyType.Assembly.GetType(SolidFillType, false);
				if (solid == null)
					return;
				p.SetValue(renderer, Activator.CreateInstance(solid, dark), null);
				_undo.Record(renderer, delegate { p.SetValue(renderer, original, null); });
			}
			catch { }
		}

		/// <summary>Par de cores lido por propriedades protegidas e gravado por metodo Set(c1, c2) (LightColorDrawManager).</summary>
		private void SetPair(object target, string setter, string left, string right)
		{
			try
			{
				MethodInfo set = target.GetType().GetMethod(setter, Instance, null, new[] { typeof(Color), typeof(Color) }, null);
				PropertyInfo l = target.GetType().GetProperty(left, Protected | Instance);
				PropertyInfo r = target.GetType().GetProperty(right, Protected | Instance);
				if (set == null || l == null || r == null)
					return;
				Color ol = (Color)l.GetValue(target, null), or = (Color)r.GetValue(target, null);
				Color dl = DarkPalette.Back(ol), dr = DarkPalette.Back(or);
				if (dl == ol && dr == or)
					return;
				set.Invoke(target, new object[] { dl, dr });
				_undo.Record(target, delegate { set.Invoke(target, new object[] { ol, or }); });
			}
			catch { }
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

		private static void Invalidate(object control)
		{
			System.Windows.Forms.Control c = control as System.Windows.Forms.Control;
			if (c != null)
				try { c.Invalidate(true); } catch { }
		}
	}
}
