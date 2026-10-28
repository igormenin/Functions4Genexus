using System;
using System.Drawing;

namespace Func4Genexus.Theme
{
	/// <summary>
	/// Cor que o tema escuro mantem escura mesmo quando o dono troca depois (WWP pinta as abas no hover/selecao, a
	/// caixa de filtro do Properties volta para Window, o GeneXus reaplica a configuracao dos estilos do editor).
	/// Guarda o ultimo valor do dono para devolver ao desligar. PURO (get/set injetados). Ver secao 4 do guia.
	/// </summary>
	internal sealed class KeptColor
	{
		private readonly Func<Color> _get;
		private readonly Action<Color> _set;
		private readonly Func<Color, Color> _rule;
		private Color _owner;
		private Color? _ours;

		internal KeptColor(Func<Color> get, Action<Color> set, Func<Color, Color> rule)
		{
			_get = get;
			_set = set;
			_rule = rule;
		}

		/// <summary>Escurece de novo se o dono trocou a cor desde a ultima vez; true se mudou algo.</summary>
		internal bool Apply()
		{
			Color now = _get();
			if (_ours.HasValue && now == _ours.Value)
				return false; // ainda e a nossa (inclui o evento disparado pelo proprio _set)
			_owner = now;
			_ours = null;
			Color dark = _rule(now);
			if (dark == now)
				return false;
			_ours = dark;
			_set(dark);
			return true;
		}

		/// <summary>Devolve o ultimo valor do dono, se a cor ainda e a que o tema pos.</summary>
		internal void Restore()
		{
			if (_ours.HasValue && _get() == _ours.Value)
				_set(_owner);
			_ours = null;
		}
	}
}
