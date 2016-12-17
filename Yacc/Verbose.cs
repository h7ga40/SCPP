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

namespace Yacc
{
	public class Verbose<ActionType>
	{
#if !lint
		static readonly string sccsid = "@(#)verbose.c	5.3 (Berkeley) 1/20/91";
#endif // not lint

		private short[] null_rules;

		private Yacc<ActionType> m_Yacc;
		private Lr0<ActionType> m_Lr0;
		private Lalr<ActionType> m_Lalr;
		private MakeParser<ActionType> m_MakeParser;

		private TextWriter m_VerboseWriter; /*  y.output					    */

		public Verbose(Yacc<ActionType> yacc)
		{
			this.m_Yacc = yacc;
			m_MakeParser = yacc.MakeParser;
			m_Lalr = yacc.Lalr;
			m_Lr0 = yacc.Lr0;
		}

		public TextWriter VerboseWriter {
			get { return m_VerboseWriter; }
			set { m_VerboseWriter = value; }
		}

		public void Execute()
		{
			if (m_VerboseWriter == null) return;

			PrintGrammar();

			null_rules = new short[m_Yacc.m_RuleCount];
			m_VerboseWriter.Write("\f\n");
			foreach (State<ActionType> core in m_Lr0.States)
				PrintState(core);

			if (m_MakeParser.nunused != 0)
				LogUnused();
			if (m_MakeParser.SRtotal != 0 || m_MakeParser.RRtotal != 0)
				LogConflicts();

			m_VerboseWriter.Write("\n\n{0} terminals, {1} nonterminals\n", m_Yacc.m_TokenCount,
					m_Yacc.m_VarCount);
			m_VerboseWriter.Write("{0} grammar rules, {1} states\n", m_Yacc.m_RuleCount - 2, m_Lr0.States.Count);
		}

		void PrintGrammar()
		{
			int i, j, k;
			int spacing = 0;

			k = 1;
			for (i = 2; i < m_Yacc.m_RuleCount; ++i) {
				if (m_Yacc.m_Rules[i].Lhs != m_Yacc.m_Rules[i - 1].Lhs) {
					if (i != 2) m_VerboseWriter.Write("\n");
					m_VerboseWriter.Write("{0,4}  {1} :", i - 2, m_Yacc.m_Rules[i].Lhs.Name);
					spacing = m_Yacc.m_Rules[i].Lhs.Name.Length + 1;
				}
				else {
					m_VerboseWriter.Write("{0,4}  ", i - 2);
					j = spacing;
					while (--j >= 0) m_VerboseWriter.Write(' ');
					m_VerboseWriter.Write('|');
				}

				while (m_Yacc.m_Items[k] >= 0) {
					m_VerboseWriter.Write(" {0}", m_Yacc.m_Symbols[m_Yacc.m_Items[k]].Name);
					++k;
				}
				++k;
				m_VerboseWriter.Write('\n');
			}
		}

		void LogUnused()
		{
			int i;
			int p;

			m_VerboseWriter.Write("\n\nRules never reduced:\n");
			for (i = 3; i < m_Yacc.m_RuleCount; ++i) {
				if (m_MakeParser.rules_used[i] == 0) {
					m_VerboseWriter.Write("\t{0} :", m_Yacc.m_Rules[i].Lhs.Name);
					for (p = m_Yacc.m_Rules[i].Rhs; m_Yacc.m_Items[p] >= 0; ++p)
						m_VerboseWriter.Write(" {0}", m_Yacc.m_Symbols[m_Yacc.m_Items[p]].Name);
					m_VerboseWriter.Write("  ({0})\n", i - 2);
				}
			}
		}

		void LogConflicts()
		{
			int i;

			m_VerboseWriter.Write("\n\n");
			for (i = 0; i < m_Lr0.States.Count; i++) {
				if (m_MakeParser.SRconflicts[i] != 0 || m_MakeParser.RRconflicts[i] != 0) {
					m_VerboseWriter.Write("State {0} contains ", i);
					if (m_MakeParser.SRconflicts[i] == 1)
						m_VerboseWriter.Write("1 shift/reduce conflict");
					else if (m_MakeParser.SRconflicts[i] > 1)
						m_VerboseWriter.Write("{0} shift/reduce conflicts",
						m_MakeParser.SRconflicts[i]);
					if (m_MakeParser.SRconflicts[i] != 0 && m_MakeParser.RRconflicts[i] != 0)
						m_VerboseWriter.Write(", ");
					if (m_MakeParser.RRconflicts[i] == 1)
						m_VerboseWriter.Write("1 reduce/reduce conflict");
					else if (m_MakeParser.RRconflicts[i] > 1)
						m_VerboseWriter.Write("{0} reduce/reduce conflicts",
						m_MakeParser.RRconflicts[i]);
					m_VerboseWriter.Write(".\n");
				}
			}
		}

		void PrintState(State<ActionType> core)
		{
			if (core.Number != 0)
				m_VerboseWriter.Write("\n\n");
			if (m_MakeParser.SRconflicts[core.Number] != 0 || m_MakeParser.RRconflicts[core.Number] != 0)
				PrintConflicts(core);
			m_VerboseWriter.Write("state {0}\n", core.Number);
			PrintCore(core.Number);
			PrintNulls(core);
			PrintActions(core);
		}

		void PrintConflicts(State<ActionType> core)
		{
			Symbol symbol;
			int number = 0;
			ActionCode act = ActionCode.NONE;

			symbol = null;
			foreach (Action<ActionType> p in core.Parser) {
				if (p.Suppressed == 2)
					continue;

				if (p.Symbol != symbol) {
					symbol = p.Symbol;
					number = p.Rule.Number;
					if (p.ActionCode == ActionCode.SHIFT)
						act = ActionCode.SHIFT;
					else
						act = ActionCode.REDUCE;
				}
				else if (p.Suppressed == 1) {
					if (core.Number == m_Yacc.m_FinalState && symbol == m_Yacc.m_Symbols[0]) {
						m_VerboseWriter.Write("{0}: shift/reduce conflict "
							+ "(accept, reduce {1}) on $end\n", core.Number, p.Rule.Number - 2);
					}
					else {
						if (act == ActionCode.SHIFT) {
							m_VerboseWriter.Write("{0}: shift/reduce conflict "
								+ "(shift {1}, reduce {2}) on {3}\n", core.Number, number, p.Rule.Number - 2,
								symbol.Name);
						}
						else {
							m_VerboseWriter.Write("{0}: reduce/reduce conflict "
								+ "(reduce {1}, reduce {2}) on {3}\n", core.Number, number - 2, p.Rule.Number - 2,
								symbol.Name);
						}
					}
				}
			}
		}

		void PrintCore(int state)
		{
			int i;
			int k;
			int rule;
			State<ActionType> statep;
			int sp;
			int sp1;

			statep = m_Lr0.States[state];
			k = statep.Items.Length;

			for (i = 0; i < k; i++) {
				sp1 = sp = statep.Items[i];

				while (m_Yacc.m_Items[sp] >= 0) ++sp;
				rule = -(m_Yacc.m_Items[sp]);
				m_VerboseWriter.Write("\t{0} : ", m_Yacc.m_Rules[rule].Lhs.Name);

				for (sp = m_Yacc.m_Rules[rule].Rhs; sp < sp1; sp++)
					m_VerboseWriter.Write("{0} ", m_Yacc.m_Symbols[m_Yacc.m_Items[sp]].Name);

				m_VerboseWriter.Write('.');

				while (m_Yacc.m_Items[sp] >= 0) {
					m_VerboseWriter.Write(" {0}", m_Yacc.m_Symbols[m_Yacc.m_Items[sp]].Name);
					sp++;
				}
				m_VerboseWriter.Write("  ({0})\n", -2 - m_Yacc.m_Items[sp]);
			}
		}

		void PrintNulls(State<ActionType> core)
		{
			short i;
			int j, k, nnulls;

			nnulls = 0;
			foreach (Action<ActionType> p in core.Parser) {
				if (p.ActionCode == ActionCode.REDUCE &&
					(p.Suppressed == 0 || p.Suppressed == 1)) {
					i = p.Rule.Number;
					if (m_Yacc.m_Rules[i].Rhs + 1 == m_Yacc.m_Rules[i + 1].Rhs) {
						for (j = 0; j < nnulls && i > null_rules[j]; ++j)
							continue;

						if (j == nnulls) {
							++nnulls;
							null_rules[j] = i;
						}
						else if (i != null_rules[j]) {
							++nnulls;
							for (k = nnulls - 1; k > j; --k)
								null_rules[k] = null_rules[k - 1];
							null_rules[j] = i;
						}
					}
				}
			}

			for (i = 0; i < nnulls; ++i) {
				j = null_rules[i];
				m_VerboseWriter.Write("\t{0} : .  ({1})\n", m_Yacc.m_Rules[j].Lhs.Name,
					j - 2);
			}
			m_VerboseWriter.Write("\n");
		}

		void PrintActions(State<ActionType> core)
		{
			LinkedList<Action<ActionType>> p;
			Shifts<ActionType> sp;
			int As;

			if (core.Number == m_Yacc.m_FinalState)
				m_VerboseWriter.Write("\t$end  accept\n");

			p = core.Parser;
			if (p.Count != 0) {
				PrintShifts(p);
				PrintReductions(p, m_MakeParser.defred[core.Number]);
			}

			sp = m_Lr0.States[core.Number].Shifts;
			if (sp != null && sp.Shift.Length > 0) {
				As = sp.Shift[sp.Shift.Length - 1].AccessingSymbol.Index;
				if (m_Yacc.IsVar(As))
					PrintGotos(core.Number);
			}
		}

		void PrintShifts(LinkedList<Action<ActionType>> p)
		{
			int count;

			count = 0;
			foreach (Action<ActionType> q in p) {
				if (q.Suppressed < 2 && q.ActionCode == ActionCode.SHIFT)
					++count;
			}

			if (count > 0) {
				foreach (Action<ActionType> q in p) {
					if (q.ActionCode == ActionCode.SHIFT && q.Suppressed == 0)
						m_VerboseWriter.Write("\t{0}  shift {1}\n",
							q.Symbol.Name, q.Rule.Number);
				}
			}
		}

		void PrintReductions(LinkedList<Action<ActionType>> p, int defred)
		{
			int k, anyreds;

			anyreds = 0;
			foreach (Action<ActionType> q in p) {
				if (q.ActionCode == ActionCode.REDUCE && q.Suppressed < 2) {
					anyreds = 1;
					break;
				}
			}

			if (anyreds == 0)
				m_VerboseWriter.Write("\t.  error\n");
			else {
				foreach (Action<ActionType> q in p) {
					if (q.ActionCode == ActionCode.REDUCE && q.Rule.Number != defred) {
						k = q.Rule.Number - 2;
						if (q.Suppressed == 0)
							m_VerboseWriter.Write("\t{0}  reduce {1}\n",
								q.Symbol.Name, k);
					}
				}

				if (defred > 0)
					m_VerboseWriter.Write("\t.  reduce {0}\n", defred - 2);
			}
		}

		void PrintGotos(int stateno)
		{
			int i;
			State<ActionType> k;
			int As;
			State<ActionType>[] to_state;
			Shifts<ActionType> sp;

			m_VerboseWriter.Write('\n');
			sp = m_Lr0.States[stateno].Shifts;
			to_state = sp.Shift;
			for (i = 0; i < sp.Shift.Length; ++i) {
				k = to_state[i];
				As = k.AccessingSymbol.Index;
				if (m_Yacc.IsVar(As))
					m_VerboseWriter.Write("\t{0}  goto {1}\n", m_Yacc.m_Symbols[As].Name, k.Number);
			}
		}
	}
}
