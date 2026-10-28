using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Registro do que o tema escuro mudou, por objeto (controle, renderer, estilo), para desligar devolvendo o
	/// original. Objeto descartado sai do registro (nao segura editor fechado na memoria). Chave por REFERENCIA:
	/// renderer/estilo de terceiros podem sobrescrever Equals/GetHashCode pelo valor (que muda ao trocar a cor).
	/// </summary>
	internal sealed class DarkUndo
	{
		private readonly Dictionary<object, List<Action>> _undo = new Dictionary<object, List<Action>>(ByReference.Instance);

		/// <summary>true se o objeto ja foi tratado nesta sessao (nao repinta).</summary>
		internal bool Seen(object target)
		{
			return _undo.ContainsKey(target);
		}

		/// <summary>Marca o objeto como tratado, mesmo sem mudanca (ex.: cor ja escura).</summary>
		internal void Mark(object target)
		{
			if (_undo.ContainsKey(target))
				return;
			_undo[target] = new List<Action>();
			IComponent component = target as IComponent;
			if (component != null)
				component.Disposed += delegate { _undo.Remove(target); };
		}

		/// <summary>Guarda como desfazer uma mudanca feita no objeto.</summary>
		internal void Record(object target, Action undo)
		{
			Mark(target);
			_undo[target].Add(undo);
		}

		internal int Count { get { return _undo.Count; } }

		/// <summary>Desfaz tudo (ultima mudanca primeiro, por objeto) e limpa. Falha num objeto nao para os outros.</summary>
		internal int RestoreAll()
		{
			int failures = 0;
			foreach (List<Action> actions in new List<List<Action>>(_undo.Values))
				for (int i = actions.Count - 1; i >= 0; i--)
				{
					try { actions[i](); }
					catch { failures++; }
				}
			_undo.Clear();
			return failures;
		}

		/// <summary>Igualdade por referencia (tambem usada nos registros do ThirdPartyDarkStyler).</summary>
		internal sealed class ByReference : IEqualityComparer<object>
		{
			internal static readonly ByReference Instance = new ByReference();
			public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
			public int GetHashCode(object o) { return RuntimeHelpers.GetHashCode(o); }
		}
	}
}
