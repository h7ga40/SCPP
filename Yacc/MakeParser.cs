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

namespace Yacc
{
	class MakeParser<ActionType>
	{
#if ! lint
		static readonly string sccsid = "@(#)mkpar.c	5.3 (Berkeley) 1/20/91";
#endif // not lint

		public int SRtotal;
		public int RRtotal;
		public short[] SRconflicts;
		public short[] RRconflicts;
		public short[] defred;
		public short[] rules_used;
		public short nunused;

		private int SRcount;
		private int RRcount;

		private Yacc<ActionType> m_Yacc;
		private Lalr<ActionType> m_Lalr;
		private Lr0<ActionType> m_Lr0;
		private Error m_Error;

		public MakeParser(Yacc<ActionType> yacc)
		{
			this.m_Yacc = yacc;
			m_Lalr = yacc.Lalr;
			m_Lr0 = yacc.Lr0;
			m_Error = m_Yacc.Error;
		}

		public void Execute()
		{
			foreach (State<ActionType> state in m_Lr0.States)
			{
				ParseActions(state);
			}

			FindFinalState();
			RemoveConflicts();
			UnusedRules();
			if (SRtotal + RRtotal > 0) TotalConflicts();
			Defreds();
		}


		void ParseActions(State<ActionType> state)
		{
			GetShifts(state);
			AddReductions(state);
		}


		void GetShifts(State<ActionType> state)
		{
			Action<ActionType> temp;
			Shifts<ActionType> sp;
			State<ActionType>[] to_state;
			int i, k;
			Symbol symbol;

			sp = state.Shifts;
			if (sp != null)
			{
				to_state = sp.Shift;
				for (i = sp.Shift.Length - 1; i >= 0; i--)
				{
					k = to_state[i].Number;
					symbol = m_Lr0.States[k].AccessingSymbol;
					if (m_Yacc.IsToken(symbol.Index))
					{
						Rule<ActionType> rule = null;
						temp = new Action<ActionType>();
						temp.Symbol = symbol;
						temp.Rule = (k < m_Yacc.m_Rules.Count) ? m_Yacc.m_Rules[k] : (rule = new Rule<ActionType>(symbol, symbol.Precedence, symbol.Association));
						temp.Prec = symbol.Precedence;
						temp.ActionCode = ActionCode.SHIFT;
						temp.Associate = symbol.Association;
						state.Parser.AddFirst(temp);
						if (rule != null)
						{
							rule.Number = (short)k;
						}
					}
				}
			}
		}

		void AddReductions(State<ActionType> state)
		{
			int i, j, m, n;
			int ruleno;
			int rowp;

			m = m_Lalr.lookaheads[state.Number];
			n = m_Lalr.lookaheads[state.Number + 1];
			for (i = m; i < n; i++)
			{
				ruleno = m_Lalr.LAruleno[i];
				rowp = i * m_Lalr.tokensetsize;
				for (j = m_Yacc.m_TokenCount - 1; j >= 0; j--)
				{
					if (Defs.BIT(m_Lalr.LA, rowp, j) != 0)
						AddReduce(state.Parser, ruleno, m_Yacc.m_Symbols[j]);
				}
			}
		}

		void AddReduce(LinkedList<Action<ActionType>> actions, int ruleno, Symbol symbol)
		{
			Action<ActionType> temp;
			LinkedListNode<Action<ActionType>> prev, next;

			prev = null;
			for (next = actions.First; next != null && next.Value.Symbol.Index < symbol.Index; next = next.Next)
				prev = next;

			while (next != null && next.Value.Symbol.Index == symbol.Index && next.Value.ActionCode == ActionCode.SHIFT)
			{
				prev = next;
				next = next.Next;
			}

			while (next != null && next.Value.Symbol.Index == symbol.Index &&
				next.Value.ActionCode == ActionCode.REDUCE && next.Value.Rule.Number < ruleno)
			{
				prev = next;
				next = next.Next;
			}

			Rule<ActionType> rule = m_Yacc.m_Rules[ruleno];
			temp = new Action<ActionType>();
			temp.Symbol = symbol;
			temp.Rule = rule;
			temp.Prec = rule.Precedence;
			temp.ActionCode = ActionCode.REDUCE;
			temp.Associate = rule.Association;

			if (prev != null)
				actions.AddAfter(prev, temp);
			else
				actions.AddFirst(temp);
		}


		void FindFinalState()
		{
			int goal, i;
			State<ActionType>[] to_state;
			Shifts<ActionType> p;

			p = m_Lr0.States[0].Shifts;
			to_state = p.Shift;
			goal = m_Yacc.m_Items[1];
			for (i = p.Shift.Length - 1; i >= 0; --i)
			{
				m_Yacc.m_FinalState = to_state[i].Number;
				if (m_Lr0.States[m_Yacc.m_FinalState].AccessingSymbol.Index == goal) break;
			}
		}


		void UnusedRules()
		{
			int i;

			rules_used = new short[m_Yacc.m_RuleCount];

			for (i = 0; i < m_Yacc.m_RuleCount; ++i)
				rules_used[i] = 0;

			foreach (State<ActionType> core in m_Lr0.States)
			{
				foreach (Action<ActionType> p in core.Parser)
				{
					if (p.ActionCode == ActionCode.REDUCE && p.Suppressed == 0)
						rules_used[p.Rule.Number] = 1;
				}
			}

			nunused = 0;
			for (i = 3; i < m_Yacc.m_RuleCount; ++i)
				if (rules_used[i] == 0) ++nunused;

			if (nunused != 0)
				if (nunused == 1)
					m_Error.Errors.Add(String.Format("{0}: 1 rule never reduced\n", m_Error.MyName));
				else
					m_Error.Errors.Add(String.Format("{0}: {1} rules never reduced\n", m_Error.MyName, nunused));
		}


		void RemoveConflicts()
		{
			int i;
			Symbol symbol;
			Action<ActionType> pref = null;

			SRtotal = 0;
			RRtotal = 0;
			SRconflicts = new short[m_Lr0.States.Count];
			RRconflicts = new short[m_Lr0.States.Count];
			for (i = 0; i < m_Lr0.States.Count; i++)
			{
				SRcount = 0;
				RRcount = 0;
				symbol = null;
				foreach (Action<ActionType> p in m_Lr0.States[i].Parser)
				{
					if (p.Symbol != symbol)
					{
						pref = p;
						symbol = p.Symbol;
					}
					else if (i == m_Yacc.m_FinalState && symbol == m_Yacc.m_Symbols[0])
					{
						SRcount++;
						p.Suppressed = 1;
					}
					else if (pref.ActionCode == ActionCode.SHIFT)
					{
						if (pref.Prec > 0 && p.Prec > 0)
						{
							if (pref.Prec < p.Prec)
							{
								pref.Suppressed = 2;
								pref = p;
							}
							else if (pref.Prec > p.Prec)
							{
								p.Suppressed = 2;
							}
							else if (pref.Associate == KeywordCode.LEFT)
							{
								pref.Suppressed = 2;
								pref = p;
							}
							else if (pref.Associate == KeywordCode.RIGHT)
							{
								p.Suppressed = 2;
							}
							else
							{
								pref.Suppressed = 2;
								p.Suppressed = 2;
							}
						}
						else
						{
							SRcount++;
							p.Suppressed = 1;
						}
					}
					else
					{
						RRcount++;
						p.Suppressed = 1;
					}
				}
				SRtotal += SRcount;
				RRtotal += RRcount;
				SRconflicts[i] = (short)SRcount;
				RRconflicts[i] = (short)RRcount;
			}
		}

		void TotalConflicts()
		{
			m_Error.Errors.Add(String.Format("{0}: ", m_Error.MyName));
			if (SRtotal == 1)
				m_Error.Errors.Add("1 shift/reduce conflict");
			else if (SRtotal > 1)
				m_Error.Errors.Add(String.Format("{0} shift/reduce conflicts", SRtotal));

			if (SRtotal != 0 && RRtotal != 0)
				m_Error.Errors.Add(", ");

			if (RRtotal == 1)
				m_Error.Errors.Add("1 reduce/reduce conflict");
			else if (RRtotal > 1)
				m_Error.Errors.Add(String.Format("{0} reduce/reduce conflicts", RRtotal));

			m_Error.Errors.Add(".\n");
		}


		int SoleReduction(State<ActionType> state)
		{
			int count, ruleno;

			count = 0;
			ruleno = 0;
			foreach (Action<ActionType> p in state.Parser)
			{
				if (p.ActionCode == ActionCode.SHIFT && p.Suppressed == 0)
					return 0;
				else if (p.ActionCode == ActionCode.REDUCE && p.Suppressed == 0)
				{
					if (ruleno > 0 && p.Rule.Number != ruleno)
						return 0;
					if (p.Symbol.Index != 1)
						++count;
					ruleno = p.Rule.Number;
				}
			}

			if (count == 0)
				return 0;
			return ruleno;
		}

		void Defreds()
		{
			int i;

			defred = new short[m_Lr0.States.Count];
			for (i = 0; i < m_Lr0.States.Count; i++)
				defred[i] = (short)SoleReduction(m_Lr0.States[i]);
		}
	}
}
