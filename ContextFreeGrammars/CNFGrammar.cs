﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextFreeGrammars {
	public class CNFGrammar {
		List<CNFNonterminalProduction> _nonterminalProductions = new List<CNFNonterminalProduction>();
		List<CNFTerminalProduction> _terminalProductions = new List<CNFTerminalProduction>();
		bool _producesEmpty = false;
		Variable _start;

		//Dictionary<Terminal, ISet<CNFTerminalProduction>> _reverseTerminalProductions = new Dictionary<Terminal, ISet<CNFTerminalProduction>>();


		public List<CNFNonterminalProduction> NonterminalProductions {
			get { return _nonterminalProductions; }
		}

		public List<CNFTerminalProduction> TerminalProductions {
			get { return _terminalProductions; }
		}

		public Variable Start {
			get { return _start; }
			set { _start = value; }
		}

		private CNFGrammar() {
		}

		public CNFGrammar(List<CNFNonterminalProduction> nt, List<CNFTerminalProduction> t, bool producesEmpty, Variable start) {
			_nonterminalProductions = nt;
			_terminalProductions = t;
			_producesEmpty = producesEmpty;
			_start = start;
		}

		public CNFGrammar(Grammar grammar) {
			var productions = CloneGrammar(grammar);
			// var productions = grammar.Productions;
			StepStart(productions);
			// Console.WriteLine(grammar);
			StepTerm(productions);
			// Console.WriteLine(grammar);
			StepBin(productions);
			// Console.WriteLine(grammar);
			StepDel(productions);
			// Console.WriteLine(grammar);
			StepUnit(productions);
			// Console.WriteLine(grammar);
			foreach (var production in productions) {
				if (production.Rhs.Count > 2) {
					throw new Exception("Didn't expect more than 2");
				} else if (production.Rhs.Count == 2) {
					_nonterminalProductions.Add(new CNFNonterminalProduction(production));
				} else if (production.Rhs.Count == 1) {
					var rhs = production.Rhs[0];
					_terminalProductions.Add(new CNFTerminalProduction(production));
				} else if (production.Rhs.Count == 0) {
					_producesEmpty = true;
				}
			}

			// BuildLookups();
		}

		private Dictionary<Terminal, ISet<CNFTerminalProduction>> ReverseTerminalLookups() {
			var retval = new Dictionary<Terminal, ISet<CNFTerminalProduction>>();
			foreach (var production in _terminalProductions) {
				ISet<CNFTerminalProduction> nonterms;
				if (!retval.TryGetValue(production.Rhs, out nonterms)) {
					nonterms = new HashSet<CNFTerminalProduction>();
					retval[production.Rhs] = nonterms;
				}
				nonterms.Add(production);
			}
			return retval;
		}

		/// <summary>
		/// Returns new CNFGrammar containing new immediate data structures, but reusing the same underlying productions
		/// </summary>
		/// <returns></returns>
		public CNFGrammar ShallowClone() {
			var newGrammar = new CNFGrammar();
			newGrammar._nonterminalProductions.AddRange(_nonterminalProductions);
			newGrammar._terminalProductions.AddRange(_terminalProductions);
			newGrammar._start = _start;
			newGrammar._producesEmpty = _producesEmpty;
			// _nonterminalProductions = grammar._nonterminalProductions

			// newGrammar.BuildLookups();

			return newGrammar;
		}

		// https://en.wikipedia.org/wiki/CYK_algorithm
		//let the input be a string S consisting of n characters: a1 ... an.
		//let the grammar contain r nonterminal symbols R1 ... Rr.
		//This grammar contains the subset Rs which is the set of start symbols.
		//let P[n,n,r] be an array of booleans. Initialize all elements of P to false.
		//for each i = 1 to n
		//  for each unit production Rj -> ai
		//	set P[1,i,j] = true
		//for each i = 2 to n -- Length of span
		//  for each j = 1 to n-i+1 -- Start of span
		//	for each k = 1 to i-1 -- Partition of span
		//	  for each production RA -> RB RC
		//		if P[k,j,B] and P[i-k,j+k,C] then set P[i,j,A] = true
		//if any of P[n,1,x] is true (x is iterated over the set s, where s are all the indices for Rs) then
		//  S is member of language
		//else
		//  S is not member of language
		public bool Cyk(Sentence s) {
			if (s.Count == 0) {
				return _producesEmpty;
			}

			var reverseTerminalProductions = ReverseTerminalLookups();

			List<Variable> nonterminals_R = new List<Variable>(GetNonterminals());
			Dictionary<Variable, int> RToJ = new Dictionary<Variable, int>();
			for (int i = 0; i < nonterminals_R.Count; i++) {
				var R = nonterminals_R[i];
				RToJ[R] = i;
			}

			var P = new bool[s.Count, s.Count, nonterminals_R.Count];
			for (int i = 0; i < s.Count; i++) {
				var a_i = (Terminal)s[i];
				ISet<CNFTerminalProduction> yields_a_i;
				if (!reverseTerminalProductions.TryGetValue(a_i, out yields_a_i)) {
					// the grammar can't possibly produce this string if it doesn't know a terminal
					return false;
				}
				foreach (var production in yields_a_i) {
					var j = RToJ[production.Lhs];
					P[0, i, j] = true;
				}
			}

			for (int i = 2; i <= s.Count; i++) {
				for (int j = 1; j <= s.Count - i + 1; j++) {
					for (int k = 1; k <= i - 1; k++) {
						// Console.WriteLine("i, j, k = {0:00}, {1:00}, {2:00}", i, j, k);
						foreach (var production in _nonterminalProductions) {
							var R_A = production.Lhs;
							var R_B = production.Rhs[0];
							var R_C = production.Rhs[1];
							var A = RToJ[R_A];
							var B = RToJ[R_B];
							var C = RToJ[R_C];

							if (P[k-1, j-1, B] && P[i-k-1, j+k-1, C]) {
								P[i - 1, j - 1, A] = true;
							}
						}
					}
				}
			}

			return P[s.Count - 1, 0, RToJ[_start]];
		}

		private HashSet<Variable> GetNonterminals() {
			var results = new HashSet<Variable>();

			foreach (var production in _nonterminalProductions) {
				results.Add(production.Lhs);
				results.Add(production.Rhs[0]);
				results.Add(production.Rhs[1]);
			}
			foreach (var production in _terminalProductions) {
				results.Add(production.Lhs);
			}
			results.Add(_start);

			return results;
		}

		private void StepStart(List<Production> productions) {
			var fresh = Getfresh();
			productions.Add(
				new Production(fresh, new Sentence { Variable.Of("S") })
			);
			_start = fresh;
		}

		private void StepTerm(List<Production> productions) {
			var newProductions = new List<Production>();
			var lookup = new Dictionary<Terminal, Variable>();
			foreach (var production in productions) {
				if (production.Rhs.Count < 2) {
					continue;
				}
				for (int i = 0; i < production.Rhs.Count; i++) {
					var word = production.Rhs[i];
					if (word.IsVariable()) {
						continue;
					}
					Terminal terminal = (Terminal)word;
					Variable fresh;
					if (!lookup.TryGetValue(terminal, out fresh)) {
						fresh = Getfresh();
						lookup[terminal] = fresh;
						newProductions.Add(
							new Production(fresh, new Sentence { terminal })
						);
					}
					production.Rhs[i] = fresh;
				}
			}
			productions.AddRange(newProductions);
		}

		private void StepBin(List<Production> productions) {
			// List<Production> newProductions = new List<Production>();
			List<Production> finalProductions = new List<Production>();
			foreach (var production in productions) {
				if (production.Rhs.Count < 3) {
					finalProductions.Add(production);
					continue;
				}
				var rhs = production.Rhs;
				var curr = production.Lhs;
				for (int i = 0; i < rhs.Count-2; i++) {
					var left = rhs[i];
					var newFresh = Getfresh();
					finalProductions.Add(
						new Production(curr, new Sentence { left, newFresh })
					);
					curr = newFresh;
				}
				finalProductions.Add(
					new Production(curr, new Sentence { rhs[rhs.Count - 2], rhs[rhs.Count - 1] })
				);
			}
			productions.Clear();
			productions.AddRange(finalProductions);
		}
		private void StepDel(List<Production> productions) {
			var nullableSet = GetNullable(productions);
			var newRules = new List<Production>();
			foreach (var production in productions) {
				var toAdd = Nullate(production, nullableSet);
				RemoveExtraneousNulls(toAdd);
				newRules.AddRange(toAdd);
			}
			productions.Clear();
			productions.AddRange(newRules);
		}

		private void StepUnit(List<Production> productions) {
			bool changed = true;
			while (changed) {
				changed = StepUnitOnce(productions);
			}
		}

		private bool StepUnitOnce(List<Production> productions) {
			var table = BuildLookupTable(productions);
			var result = new List<Production>(productions);
			var changed = false;
			
			//if (productions.Count > 100) {
			//	throw new Exception("Something's gone wrong");
			//}

			foreach (var production in productions) {
				if (production.Rhs.Count == 1) {
					var rhs = production.Rhs[0];
					if (!rhs.IsVariable()) {
						continue;
					}
					changed = true;
					result.Remove(production);
					var entries = table[(Variable)rhs];
					foreach (var entry in entries) {
						var newProd = new Production(production.Lhs, entry.Rhs);
						if (!newProd.IsSelfLoop) {
							result.Add(newProd);
						}
					}
					break;
				}
			}
			productions.Clear();
			productions.AddRange(result);
			return changed;
		}

		private Dictionary<Variable, List<Production>> BuildLookupTable(List<Production> productions) {
			var table = new Dictionary<Variable, List<Production>>();
			foreach (var production in productions) {
				List<Production> entries;
				if (!table.TryGetValue(production.Lhs, out entries)) {
					entries = new List<Production>();
					table[production.Lhs] = entries;
				}
				entries.Add(production);
			}
			return table;
		}

		private ISet<Variable> GetNullable(List<Production> originalProductions) {
			var productions = CloneProductions(originalProductions);
			var newProductions = new List<Production>();
			var nullableVariables = new HashSet<Variable>();
			var changed = true;
			while (changed) {
				changed = false;
				foreach (var production in productions) {
					if (production.IsEmpty) {
						nullableVariables.Add(production.Lhs);
						changed = true;
						continue;
					}
					if (production.Rhs.OnlyTerminals()) {
						continue;
					}
					for (int i = production.Rhs.Count-1; i >= 0; i--) {
						var word = production.Rhs[i];
						if (nullableVariables.Contains(word)) {
							production.Rhs.RemoveAt(i);
							changed = true;
						}
					}
					newProductions.Add(production);
				}
				var oldProductions = productions;
				productions = newProductions;
				newProductions = oldProductions;
				newProductions.Clear();
			}
			return nullableVariables;
		}


		// remove A -> X unless A is the start symbol
		private void RemoveExtraneousNulls(List<Production> productions) {
			// var results = new List<Production>();
			if (productions.Count == 0) {
				return;
			}
			for (int i = productions.Count - 1; i >= 0; i--) {
				var production = productions[i];
				if (production.IsEmpty) {
					if (production.Lhs != _start) {
						productions.RemoveAt(i);
					}
				}
				// results.Add(production);
			}
		}

		private List<Production> Nullate(Production originalProduction, ISet<Variable> nullableSet) {
			var results = new List<Production>();
			results.Add(originalProduction);
			if (originalProduction.IsEmpty) {
				return results;
			}
			for (int i = originalProduction.Rhs.Count - 1; i >= 0; i--) {
				var newResults = new List<Production>();
				foreach (var production in results) {
					var word = production.Rhs[i];
					if (!nullableSet.Contains(word)) {
						continue;
					}
					// var with = production.Clone();
					var without = production.Clone();
					without.Rhs.RemoveAt(i);
					newResults.Add(without);
				}
				results.AddRange(newResults);
			}
			// NullateAux(production, nullableSet, 0, result);

			return results;
		}

		// todo: horrible
		private int _freshx = 0;
		private Variable Getfresh() {
			var result = Variable.Of("X_" + _freshx);
			_freshx++;
			return result;
		}

		private static List<Production> CloneGrammar(Grammar grammar) {
			return CloneProductions(grammar.Productions);
		}
		private static List<Production> CloneProductions(List<Production> productions) {
			var result = new List<Production>();
			foreach (var production in productions) {
				// var productions = grammar.Productions;
				result.Add(production.Clone());
			}
			return result;
		}


		public override string ToString() {
			var retval = "CNFGrammar(" + _start + "){\n";

			foreach (var production in _nonterminalProductions) {
				retval += "  " + production.ToString() + "\n";
			}
			foreach (var production in _terminalProductions) {
				retval += "  " + production.ToString() + "\n";
			}
			if (_producesEmpty) {
				retval += "  " + _start + " → ε";
			}
			retval += "}\n";
			return retval;
		}
	}
}