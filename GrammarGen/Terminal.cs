﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrammarGen {
	class Terminal : Word {
		private static Dictionary<string, Terminal> _history = new Dictionary<string, Terminal>();

		private readonly string _name;

		private Terminal(string v) {
			_name = v;
		}

		public string Name {
			get { return _name; }
		}

		internal static Terminal Of(string v) {
			Terminal terminal;
			if (!_history.TryGetValue(v, out terminal)) {
				terminal = new Terminal(v);
				_history[v] = terminal;
			}
			return terminal;
		}

		public override string ToString() {
			return string.Format("Trm({0})", _name);
		}

		public Sentence ProduceBy(Grammar grammar) {
			return new Sentence { this };
		}
		public bool IsVariable() {
			return false;
		}
	}
}