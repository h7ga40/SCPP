/*
 * Copyright (c) 1989 The Regents of the University of California.
 * All rights reserved.
 *
 * This code is derived from software contributed to Berkeley by
 * Robert Paul Corbett.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. All advertising materials mentioning features or use of this software
 *    must display the following acknowledgement:
 *	This product includes software developed by the University of
 *	California, Berkeley and its contributors.
 * 4. Neither the name of the University nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;

namespace Yacc
{
	public abstract class Grammar<ActionType>
	{
		private Yacc<ActionType> m_Yacc;
		private Error m_Error;
		private SymbolTable m_SymbolTable;

		private Symbol m_Goal;
		private int m_Precedence;
		private int m_GenerateSymbol;
		private List<Symbol> m_Items;

		public Grammar(Yacc<ActionType> yacc)
		{
			m_Yacc = yacc;
			m_Error = yacc.Error;
			m_SymbolTable = new SymbolTable();
		}

		internal Yacc<ActionType> Yacc { get { return m_Yacc; } }
		internal Error Error { get { return m_Error; } }
		internal SymbolTable SymbolTable { get { return m_SymbolTable; } }

		internal ReadOnlyCollection<Symbol> Items { get { return m_Items.AsReadOnly(); } }

		protected void DeclareTokens(params Symbol[] symbols)
		{
			DeclareTokens(KeywordCode.TOKEN, null, symbols, null);
		}

		protected void DeclareTokens(string tag, params Symbol[] symbols)
		{
			DeclareTokens(KeywordCode.TOKEN, tag, symbols, null);
		}

		protected void DeclareTokens(string tag, Symbol[] symbols, int[] values)
		{
			DeclareTokens(KeywordCode.TOKEN, tag, symbols, values);
		}

		protected void DeclareLefts(params Symbol[] symbols)
		{
			DeclareTokens(KeywordCode.LEFT, null, symbols, null);
		}

		protected void DeclareLefts(string tag, params Symbol[] symbols)
		{
			DeclareTokens(KeywordCode.LEFT, tag, symbols, null);
		}

		protected void DeclareRights(params Symbol[] symbols)
		{
			DeclareTokens(KeywordCode.RIGHT, null, symbols, null);
		}

		protected void DeclareRights(string tag, params Symbol[] symbols)
		{
			DeclareTokens(KeywordCode.RIGHT, tag, symbols, null);
		}

		protected void DeclareNonAssoc(params Symbol[] symbols)
		{
			DeclareTokens(KeywordCode.NONASSOC, null, symbols, null);
		}

		protected void DeclareNonAssoc(string tag, params Symbol[] symbols)
		{
			DeclareTokens(KeywordCode.NONASSOC, tag, symbols, null);
		}

		protected Rules DeclareRule(Symbol symbol)
		{
			if (m_Goal == null)
			{
				if (symbol.Class == SymbolClasse.TERM)
					m_Error.TerminalStart(symbol.Name);
				m_Goal = symbol;
			}
#if OUTPUT_CODE
			sb.Append("\nlhs = DeclareRule(" + symbol.Name + ");\n");
#endif
			return new Rules(this, symbol);
		}

		protected Symbol LookupLiteralSymbol(string name)
		{
			Symbol bp;
			int n = name.Length;
			StringBuilder cache = new StringBuilder();
			char c;
			int i;

			if (n == 1)
				cache.Append('\'');
			else
				cache.Append('"');

			for (i = 0; i < n; ++i)
			{
				c = name[i];
				if (c == '\\' || c == cache[0])
				{
					cache.Append('\\');
					cache.Append(c);
				}
				else if (IsPrint(c))
					cache.Append(c);
				else
				{
					cache.Append('\\');
					switch (c)
					{
						case '\x7': cache.Append('a'); break;
						case '\b': cache.Append('b'); break;
						case '\f': cache.Append('f'); break;
						case '\n': cache.Append('n'); break;
						case '\r': cache.Append('r'); break;
						case '\t': cache.Append('t'); break;
						case '\v': cache.Append('v'); break;
						default:
							cache.Append(((c >> 6) & 7) + '0');
							cache.Append(((c >> 3) & 7) + '0');
							cache.Append((c & 7) + '0');
							break;
					}
				}
			}

			if (n == 1)
				cache.Append('\'');
			else
				cache.Append('"');
#if OUTPUT_CODE
			Symbol temp;
			if (!m_SymbolTable.TryLookup(name, out temp))
			{
				System.Diagnostics.Debug.Write("LookupLiteralSymbol(\"" + name + "\");\n");
			}
#endif
			bp = m_SymbolTable.Lookup(cache.ToString());
			bp.Class = SymbolClasse.TERM;
			if (n == 1 && bp.Value == Defs.UNDEFINED)
				bp.Value = (short)name[0];

			return bp;
		}

		protected static bool IsPrint(char p)
		{
			return p >= 0x20;
		}

		protected static bool IsIdent(int c)
		{
			return (Char.IsLetterOrDigit((char)c) || (c) == '_' || (c) == '.' || (c) == '$');
		}

		protected static bool IsReserved(string name)
		{
			int s;

			if (String.Compare(name, ".") == 0 ||
				String.Compare(name, "$accept") == 0 ||
				String.Compare(name, "$end") == 0)
				return true;

			if (name[0] == '$' && name[1] == '$' && Char.IsDigit(name[2]))
			{
				s = 3;
				while (Char.IsDigit(name[s])) ++s;
				if (s == name.Length) return true;
			}

			return false;
		}

		protected Symbol LookupSymbol(string name)
		{
			foreach (char c in name)
			{
				if (!IsIdent(c))
					throw new ArgumentException("有効な識別子ではありません。", "name");
			}

			if (IsReserved(name))
				m_Error.UsedReserved(name);
#if OUTPUT_CODE
			Symbol temp;
			if (!m_SymbolTable.TryLookup(name, out temp))
			{
				System.Diagnostics.Debug.Write(name + " = LookupSymbol(\"" + name + "\");\n");
			}
#endif
			return m_SymbolTable.Lookup(name);
		}

		protected void DeclareTokens(KeywordCode assoc, string tag, Symbol[] symbols, int[] values)
		{
			int value;

			if (assoc != KeywordCode.TOKEN) m_Precedence++;

			int i = 0;
			foreach (Symbol bp in symbols)
			{
				if (bp == m_Goal) m_Error.TokenizedStart(bp.Name);
				bp.Class = SymbolClasse.TERM;

				if (tag != null)
				{
					if (bp.Tag != null && tag.CompareTo(bp.Tag) != 0)
						m_Error.RetypedWarning(bp.Name);
					bp.Tag = tag;
				}

				if (assoc != KeywordCode.TOKEN)
				{
					if (bp.Precedence != 0 && m_Precedence != bp.Precedence)
						m_Error.ReprecWarning(bp.Name);
					bp.Association = assoc;
					bp.Precedence = (short)m_Precedence;
				}

				if (values != null)
				{
					value = values[i];
					if (bp.Value != Defs.UNDEFINED && value != bp.Value)
						m_Error.RevaluedWarning(bp.Name);
					bp.Value = (short)value;
				}
				i++;
			}
		}

		protected void DeclareTypes(string tag, params Symbol[] symbols)
		{
			foreach (Symbol bp in symbols)
			{
				if (bp.Tag != null && tag != bp.Tag)
					Error.RetypedWarning(bp.Name);
				bp.Tag = tag;
			}
		}

		protected void DeclareStart(Symbol bp)
		{
			if (bp.Class == SymbolClasse.TERM)
				m_Error.TerminalStart(bp.Name);
			if (m_Goal != null && m_Goal != bp)
				m_Error.RestartedWarning();
			m_Goal = bp;
		}

		void InitializeGrammar()
		{
			m_Items = new List<Symbol>();
			m_Items.Add(null);
			m_Items.Add(null);
			m_Items.Add(null);
			m_Items.Add(null);

			Rule<ActionType> rule;

			m_Yacc.m_Rules = new List<Rule<ActionType>>();

			rule = new Rule<ActionType>(null, 0, KeywordCode.TOKEN);
			rule.Number = (short)m_Yacc.m_Rules.Count;
			m_Yacc.m_Rules.Add(rule);

			rule = new Rule<ActionType>(null, 0, KeywordCode.TOKEN);
			rule.Number = (short)m_Yacc.m_Rules.Count;
			m_Yacc.m_Rules.Add(rule);

			rule = new Rule<ActionType>(null, 0, KeywordCode.TOKEN);
			rule.Number = (short)m_Yacc.m_Rules.Count;
			m_Yacc.m_Rules.Add(rule);
		}

		void CheckSymbols()
		{
			if (m_Goal.Class == SymbolClasse.UNKNOWN)
				m_Error.UndefinedGoal(m_Goal.Name);

			foreach (Symbol bp in m_SymbolTable)
			{
				if (bp.Class == SymbolClasse.UNKNOWN)
				{
					m_Error.UndefinedSymbolWarning(bp.Name);
					bp.Class = SymbolClasse.TERM;
				}
			}
		}

		void PackSymbols()
		{
			Symbol[] v;
			int i, j, k, n;

			int nsyms = 2;
			m_Yacc.m_TokenCount = 1;
			foreach (Symbol bp in m_SymbolTable)
			{
				++nsyms;
				if (bp.Class == SymbolClasse.TERM) ++m_Yacc.m_TokenCount;
			}
			m_Yacc.m_StartSymbol = m_Yacc.m_TokenCount;
			m_Yacc.m_VarCount = nsyms - m_Yacc.m_TokenCount;

			m_Yacc.m_Symbols = new Symbol[nsyms];
#if OUTPUT_CODE
			List<Symbol> sortedSymbols = new List<Symbol>();
			sortedSymbols.AddRange(m_SymbolTable);
			sortedSymbols.Sort((p1, p2) =>
			{
				if (p1.Name.CompareTo("error") == 0)
					if (p2.Name.CompareTo("error") == 0) return 0; else return -1;
				if (p2.Name.CompareTo("error") == 0) return 1;
				return p1.Name.CompareTo(p2.Name);
			});
#endif
			v = new Symbol[nsyms];

			v[0] = null;					// $end
			v[m_Yacc.m_StartSymbol] = null;	// $accept

			i = 1;
			j = m_Yacc.m_StartSymbol + 1;
#if !OUTPUT_CODE
			foreach (Symbol bp in m_SymbolTable)
#else
			foreach (Symbol bp in sortedSymbols)
#endif
            {
				if (bp.Class == SymbolClasse.TERM)
					v[i++] = bp;
				else
					v[j++] = bp;
			}
			System.Diagnostics.Debug.Assert(i == m_Yacc.m_TokenCount && j == nsyms);

			for (i = 1; i < m_Yacc.m_TokenCount; ++i)
				v[i].Index = (short)i;

			m_Goal.Index = (short)(m_Yacc.m_StartSymbol + 1);
			k = m_Yacc.m_StartSymbol + 2;
			while (++i < nsyms)
				if (v[i] != m_Goal)
				{
					v[i].Index = (short)k;
					++k;
				}

			m_Goal.Value = 0;
			k = 1;
			for (i = m_Yacc.m_StartSymbol + 1; i < nsyms; ++i)
			{
				if (v[i] != m_Goal)
				{
					v[i].Value = (short)k;
					++k;
				}
			}

			k = 0;
			for (i = 1; i < m_Yacc.m_TokenCount; ++i)
			{
				n = v[i].Value;
				if (n > 256)
				{
					for (j = k++; j > 0 && v[j - 1].Value > n; --j)
						v[j].Value = v[j - 1].Value;
					v[j].Value = (short)n;
				}
			}

			if (v[1].Value == Defs.UNDEFINED)
				v[1].Value = 256;

			j = 0;
			n = 257;
			for (i = 2; i < m_Yacc.m_TokenCount; ++i)
			{
				if (v[i].Value == Defs.UNDEFINED)
				{
					while (j < k && n == v[j].Value)
					{
						while (++j < k && n == v[j].Value) continue;
						++n;
					}
					v[i].Value = (short)n;
					++n;
				}
			}

			v.CopyTo(m_Yacc.m_Symbols, 0);
			m_Yacc.m_Symbols[0] = new Symbol("$end", 0, 0, KeywordCode.TOKEN);
			m_Yacc.m_Symbols[m_Yacc.m_StartSymbol] = new Symbol("$accept", -1, 0, KeywordCode.TOKEN);
			m_Yacc.m_Symbols[m_Yacc.m_StartSymbol].Index = (short)m_Yacc.m_StartSymbol;

			for (++i; i < nsyms; ++i)
			{
				k = v[i].Index;
				m_Yacc.m_Symbols[k] = v[i];
			}
		}


		void PackGrammar()
		{
			int i, j;
			KeywordCode assoc;
			int prec;

			m_Yacc.m_Items = new short[m_Items.Count];

			m_Yacc.m_RuleCount = m_Yacc.m_Rules.Count;

			m_Yacc.m_Items[0] = -1;
			m_Yacc.m_Items[1] = m_Goal.Index;
			m_Yacc.m_Items[2] = 0;
			m_Yacc.m_Items[3] = -2;

			m_Yacc.m_Rules[0].Lhs = m_Yacc.m_Symbols[0];
			m_Yacc.m_Rules[1].Lhs = m_Yacc.m_Symbols[0];
			m_Yacc.m_Rules[2].Lhs = m_Yacc.m_Symbols[m_Yacc.m_StartSymbol];
			m_Yacc.m_Rules[0].Rhs = 0;
			m_Yacc.m_Rules[1].Rhs = 0;
			m_Yacc.m_Rules[2].Rhs = 1;

			Rule<ActionType> rule;
			j = 4;
			for (i = 3; i < m_Yacc.m_Rules.Count; ++i)
			{
				rule = m_Yacc.m_Rules[i];
				rule.Rhs = (short)j;
				assoc = KeywordCode.TOKEN;
				prec = 0;
				while (m_Items[j] != null)
				{
					m_Yacc.m_Items[j] = m_Items[j].Index;
					if (m_Items[j].Class == SymbolClasse.TERM)
					{
						prec = m_Items[j].Precedence;
						assoc = m_Items[j].Association;
					}
					++j;
				}
				m_Yacc.m_Items[j] = (short)(-i);
				++j;
				if (rule.Precedence == Defs.UNDEFINED)
				{
					rule.Precedence = (short)prec;
					rule.Association = assoc;
				}
			}

			rule = new Rule<ActionType>();
			rule.Number = (short)m_Yacc.m_Rules.Count;
			rule.Rhs = (short)j;
			m_Yacc.m_Rules.Add(rule);
		}

		void PrintItems()
		{
			int j;

			System.Diagnostics.Debug.Write("//m_Items\n");
			j = 0;
			foreach (Symbol item in m_Items)
			{
				if (j >= 10)
				{
					System.Diagnostics.Debug.Write("\n");
					j = 0;
				}
				if (item == null)
					System.Diagnostics.Debug.Write("null,");
				else
					System.Diagnostics.Debug.Write(String.Format("{0},", item.ToString()));
				++j;
			}
			System.Diagnostics.Debug.Write('\n');
		}

		protected abstract void DeclareTokens();
		protected abstract void DeclareGrammar();

		public void Execute()
		{
			DeclareTokens();
			InitializeGrammar();
			DeclareGrammar();
			CheckSymbols();
			PackSymbols();
			PackGrammar();
#if OUTPUT_CODE
			System.Diagnostics.Debug.Write(sb.ToString());
#endif
		}
#if OUTPUT_CODE
		StringBuilder sb = new StringBuilder();
#endif
		protected class Rules
		{
			private Grammar<ActionType> m_Owner;
			private Symbol m_Symbol;
			private Rule<ActionType> m_Rule;
			private bool m_LastWasAction;
			private bool m_Error;

			internal Rules(Grammar<ActionType> owner, Symbol symbol)
			{
				m_Owner = owner;
				m_Symbol = symbol;
				if (!StartRule(m_Symbol))
					m_Error = true;
			}

			public Symbol Symbol { get { return m_Symbol; } }
			internal Rule<ActionType> Rule { get { return m_Rule; } }
			public bool Error { get { return m_Error; } }

			public void AddSymbols(params Symbol[] symbols)
			{
				if (m_Rule == null)
				{
					if (!StartRule(m_Symbol))
						m_Error = true;
				}
				else if (m_LastWasAction)
					InsertEmptyRule();
				m_LastWasAction = false;

				foreach (Symbol symbol in symbols)
				{
					m_Owner.m_Items.Add(symbol);
				}
#if OUTPUT_CODE
				m_Owner.sb.Append("lhs.AddSymbols(");
				int i = 0, e = symbols.Length - 1;
				foreach (Symbol symbol in symbols)
				{
					m_Owner.sb.Append(symbol.Name);
					if (i == e)
						m_Owner.sb.Append(");\n");
					else
						m_Owner.sb.Append(", ");
					i++;
				}
#endif
			}

			public bool StartRule()
			{
				if (m_Rule != null)
					EndRule();
				m_Error = !StartRule(m_Symbol);
				m_LastWasAction = false;
				return !m_Error;
			}

			bool StartRule(Symbol bp)
			{
				if (bp.Class == SymbolClasse.TERM)
					return false;

				bp.Class = SymbolClasse.NONTERM;
				m_Rule = new Rule<ActionType>(bp, Defs.UNDEFINED, KeywordCode.TOKEN);

				return true;
			}

			public void EndRule()
			{
				int i;

				if (!m_LastWasAction && m_Rule.Lhs.Tag != null)
				{
					for (i = m_Owner.m_Items.Count - 1; m_Owner.m_Items[i] != null; --i) continue;
					/** ats: there is no way to check superclasses; therefore, only 'null' draws a warning. Was:
					if (pitem[i + 1] == null || pitem[i + 1].tag != rules[rules.Count].plhs.tag)
					  */
					if (m_Owner.m_Items[i + 1] == null)
						m_Owner.m_Error.DefaultActionWarning();
				}

				m_Owner.m_Items.Add(null);
				m_Rule.Number = (short)m_Owner.m_Yacc.m_Rules.Count;
				m_Owner.m_Yacc.m_Rules.Add(m_Rule);
				m_Rule = null;
#if OUTPUT_CODE
				DebugWriteAddRules();
				m_Owner.sb.Append("lhs.EndRule();\n");
#endif
			}
#if OUTPUT_CODE
			private void DebugWriteAddRules()
			{
				int i;
				if (m_Symbols.Count > 0)
				{
					m_Owner.sb.Append("lhs.AddRules(");
					i = 0;
					int e = m_Symbols.Count - 1;
					foreach (string symbol in m_Symbols)
					{
						m_Owner.sb.Append(symbol);
						if (i == e)
							m_Owner.sb.Append(");\n");
						else
							m_Owner.sb.Append(", ");
						i++;
					}
					m_Symbols.Clear();
				}
			}
#endif
			void InsertEmptyRule()
			{
				Symbol bp;
				int bpp;

				StringBuilder cache = new StringBuilder(String.Format("$${0}", ++m_Owner.m_GenerateSymbol));
				bp = new Symbol(cache.ToString());
				m_Owner.m_SymbolTable.Add(bp);
				bp.Tag = m_Symbol.Tag;
				bp.Class = SymbolClasse.NONTERM;

				m_Owner.m_Items.Add(null);
				m_Owner.m_Items.Add(bp);
				bpp = m_Owner.m_Items.Count - 2;
				while ((m_Owner.m_Items[bpp] = m_Owner.m_Items[bpp - 1]) != null) --bpp;

				Rule<ActionType> rule = new Rule<ActionType>(bp, 0, KeywordCode.TOKEN);
				rule.Number = (short)m_Owner.m_Yacc.m_Rules.Count;
				m_Owner.m_Yacc.m_Rules.Add(rule);
			}
#if OUTPUT_CODE
			List<string> m_Symbols = new List<string>();
#endif
			internal void AddSymbol(Symbol bp)
			{
				if (m_LastWasAction)
					InsertEmptyRule();
				m_LastWasAction = false;

				m_Owner.m_Items.Add(bp);
#if OUTPUT_CODE
				m_Symbols.Add(bp.Name);
#endif
			}

			public void MarkSymbol(Symbol bp)
			{
				if (m_Rule.Precedence != Defs.UNDEFINED && bp.Precedence != m_Rule.Precedence)
					m_Owner.m_Error.PrecRedeclared();

				m_Rule.Precedence = bp.Precedence;
				m_Rule.Association = bp.Association;
			}

			public void AddAction(ActionType action)
			{
				if (m_Rule == null)
				{
					if (!StartRule(m_Symbol))
						m_Error = true;
				}
				else if (m_LastWasAction)
					InsertEmptyRule();
				m_LastWasAction = true;

                Rule<ActionType> rule = m_Owner.m_Yacc.m_Rules[m_Owner.m_Yacc.m_Rules.Count - 1];

                int n = 0;
                for (int i = m_Owner.m_Items.Count - 1; m_Owner.m_Items[i] != null; --i) ++n;

                rule.Offset = n;
                rule.Action = action;
#if OUTPUT_CODE
				DebugWriteAddRules();
				m_Owner.sb.Append("lhs.AddAction(delegate(int n, IList<object> vals, ref object val)\n");
				m_Owner.sb.Append(action.ToString());
				m_Owner.sb.Append(");\n");
#endif
			}
		}
	}
}
