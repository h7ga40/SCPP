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
using System.Collections.ObjectModel;
using System.IO;

namespace Yacc
{
	class Lr0<ActionType>
	{
#if ! lint
		static readonly string sccsid = "@(#)lr0.c	5.3 (Berkeley) 1/20/91";
#endif //

		private List<State<ActionType>> m_States = new List<State<ActionType>>();
		private List<Shifts<ActionType>> m_Shiftses = new List<Shifts<ActionType>>();

		private State<ActionType>[] m_StateSet;
		private State<ActionType> m_CurrentState;

		private int m_ShiftCount;

		private Shifts<ActionType> m_Shifts;

		private int[] m_KernelBase;
		private int[] m_KernelEnd;
		private short[] m_KernelItems;

		private Yacc<ActionType> m_Yacc;
		private Closure<ActionType> m_Closure;
		private Error m_Error;

		public Lr0(Yacc<ActionType> yacc)
		{
			this.m_Yacc = yacc;
			m_Error = yacc.Error;
			m_Closure = new Closure<ActionType>(yacc);
		}

		public ReadOnlyCollection<State<ActionType>> States
		{
			get { return m_States.AsReadOnly(); }
		}

		public ReadOnlyCollection<Shifts<ActionType>> Shiftses
		{
			get { return m_Shiftses.AsReadOnly(); }
		}

		void AllocateItemsets()
		{
			int itemp;
			int item_end;
			int symbolIndex;
			Symbol symbol;
			int i, j;
			int count;
			short[] symbol_count;

			count = 0;
			symbol_count = new short[m_Yacc.m_Symbols.Length];

			item_end = m_Yacc.m_Items.Length;
			for (itemp = 0; itemp < item_end; itemp++)
			{
				symbolIndex = m_Yacc.m_Items[itemp];
				if (symbolIndex >= 0)
				{
					count++;
					symbol_count[symbolIndex]++;
				}
			}

			m_KernelBase = new int[m_Yacc.m_Symbols.Length];
			m_KernelItems = new short[count];

			count = 0;
			for (i = 0; i < m_Yacc.m_Symbols.Length; i++)
			{
				j = symbol_count[i];
				m_KernelBase[i] = count;
				symbol = m_Yacc.m_Symbols[i];
				symbol.Shift = (short)j;
				count += j;
			}

			m_KernelEnd = new int[m_Yacc.m_Symbols.Length];
		}

		void AllocateStorage()
		{
			AllocateItemsets();
			m_StateSet = new State<ActionType>[m_Yacc.m_Items.Length];
		}

		void AppendStates()
		{
			int i;
			int j;
			int symbol;

#if DEBUG
			if (m_Yacc.TraceWriter != null)
				m_Yacc.TraceWriter.Write("Entering append_states()\n");
#endif
			for (i = 1; i < m_ShiftCount; i++)
			{
				symbol = m_Yacc.m_Symbols[i].Shift;
				j = i;
				while (j > 0 && m_Yacc.m_Symbols[j - 1].Shift > symbol)
				{
					m_Yacc.m_Symbols[j].Shift = m_Yacc.m_Symbols[j - 1].Shift;
					j--;
				}
				m_Yacc.m_Symbols[j].Shift = (short)symbol;
			}

			if (m_ShiftCount > 0)
			{
				m_Shifts = new Shifts<ActionType>(m_CurrentState, m_ShiftCount);

				for (i = 0; i < m_ShiftCount; i++)
				{
					symbol = m_Yacc.m_Symbols[i].Shift;
					m_Shifts.Shift[i] = GetState(m_Yacc.m_Symbols[symbol]);
				}

				m_Shiftses.Add(m_Shifts);
			}
		}

		void GenerateStates()
		{
			AllocateStorage();
			m_Closure.ItemSet = new short[m_Yacc.m_Items.Length];
			m_Closure.RuleSet = new uint[Defs.WORDSIZE(m_Yacc.m_RuleCount)];
			m_Closure.SetFirstDerives();
			InitializeStates();

			while (m_CurrentState != null)
			{
				m_Closure.Execute(m_CurrentState.Items, m_CurrentState.Items.Length);
				SaveReductions();
				NewItemsets();
				AppendStates();

				int i = m_CurrentState.Number + 1;
				if (i >= m_States.Count)
					break;

				m_CurrentState = m_States[i];
			}

			m_Closure.FinalizeClosure();
		}

		State<ActionType> GetState(Symbol symbol)
		{
			int key;
			int isp1;
			int isp2;
			int iend;
			State<ActionType> sp;
			bool found;
			int n;

#if DEBUG
			if (m_Yacc.TraceWriter != null)
				m_Yacc.TraceWriter.Write(String.Format("Entering get_state({0})\n", symbol.Index));
#endif

			isp1 = m_KernelBase[symbol.Index];
			iend = m_KernelEnd[symbol.Index];
			n = iend - isp1;

			key = m_KernelItems[isp1];
			System.Diagnostics.Debug.Assert(0 <= key && key < m_Yacc.m_Items.Length);
			sp = m_StateSet[key];
			if (sp != null)
			{
				found = false;
				while (!found)
				{
					if (sp.Items.Length == n)
					{
						found = true;
						isp1 = m_KernelBase[symbol.Index];
						isp2 = 0;

						while (found && isp1 < iend)
						{
							if (m_KernelItems[isp1++] != sp.Items[isp2++])
								found = false;
						}
					}

					if (!found)
					{
						if (sp.Link != null)
						{
							sp = sp.Link;
						}
						else
						{
							sp = sp.Link = NewState(symbol);
							found = true;
						}
					}
				}
			}
			else
			{
				m_StateSet[key] = sp = NewState(symbol);
			}

			return sp;
		}

		void InitializeStates()
		{
			int i;
			int start_derives;
			State<ActionType> p;

			start_derives = m_Yacc.m_Symbols[m_Yacc.m_StartSymbol].Derives;
			for (i = 0; m_Yacc.m_Derives[start_derives + i] != null; ++i)
				continue;

			p = new State<ActionType>(i);
			p.Link = null;
			p.Number = 0;
			p.AccessingSymbol = m_Yacc.m_Symbols[0];

			for (i = 0; m_Yacc.m_Derives[start_derives + i] != null; ++i)
				p.Items[i] = m_Yacc.m_Derives[start_derives + i].Rhs;

			m_CurrentState = p;
			m_States.Add(m_CurrentState);
		}

		void NewItemsets()
		{
			int i;
			int shiftcount;
			int isp;
			int ksp;
			int symbol;

			for (i = 0; i < m_Yacc.m_Symbols.Length; i++)
				m_KernelEnd[i] = -1;

			shiftcount = 0;
			isp = 0;
			while (isp < m_Closure.ItemSetEnd)
			{
				i = m_Closure.ItemSet[isp++];
				symbol = m_Yacc.m_Items[i];
				if (symbol > 0)
				{
					ksp = m_KernelEnd[symbol];
					if (ksp == -1)
					{
						m_Yacc.m_Symbols[shiftcount++].Shift = m_Yacc.m_Symbols[symbol].Index;
						ksp = m_KernelBase[symbol];
					}

					m_KernelItems[ksp++] = (short)(i + 1);
					m_KernelEnd[symbol] = ksp;
				}
			}

			m_ShiftCount = shiftcount;
		}

		State<ActionType> NewState(Symbol symbol)
		{
			int n;
			State<ActionType> p;
			int isp1;
			int isp2;
			int iend;

#if DEBUG
			if (m_Yacc.TraceWriter != null)
				m_Yacc.TraceWriter.Write("Entering new_state({0})\n", symbol.Index);
#endif

			if (m_States.Count >= Defs.MAXSHORT)
				m_Error.Fatal("too many states");

			isp1 = m_KernelBase[symbol.Index];
			iend = m_KernelEnd[symbol.Index];
			n = iend - isp1;

			p = new State<ActionType>(n);
			p.AccessingSymbol = symbol;
			p.Number = (short)m_States.Count;

			isp2 = 0;
			while (isp1 < iend)
				p.Items[isp2++] = m_KernelItems[isp1++];

			m_States.Add(p);

			return p;
		}


		/* show_cores is used for debugging */

		void ShowCores(TextWriter stream)
		{
			int i, j, k, n;
			int itemno;

			for (k = 0; k < m_States.Count; k++)
			{
				State<ActionType> p = m_States[k];
				if (k != 0) stream.Write("\n");
				stream.Write("state {0}, number = {1}, accessing symbol = {2}\n",
					k, p.Number, p.AccessingSymbol.Name);
				n = p.Items.Length;
				for (i = 0; i < n; ++i)
				{
					itemno = p.Items[i];
					stream.Write("{0,4}  ", itemno);
					j = itemno;
					while (m_Yacc.m_Items[j] >= 0) ++j;
					stream.Write("{0} :", m_Yacc.m_Rules[-m_Yacc.m_Items[j]].Lhs.Name);
					j = m_Yacc.m_Rules[-m_Yacc.m_Items[j]].Rhs;
					while (j < itemno)
						stream.Write(" {0}", m_Yacc.m_Symbols[m_Yacc.m_Items[j++]].Name);
					stream.Write(" .");
					while (m_Yacc.m_Items[j] >= 0)
						stream.Write(" {0}", m_Yacc.m_Symbols[m_Yacc.m_Items[j++]].Name);
					stream.Write("\n");
					stream.Flush();
				}
			}
		}


		/* show_ritems is used for debugging */

		void ShowRItems(TextWriter stream)
		{
			int i;

			for (i = 0; i < m_Yacc.m_Items.Length; ++i)
				stream.Write("ritem[{0}] = {1}\n", i, m_Yacc.m_Items[i]);
		}


		/* show_rrhs is used for debugging */
		void ShowRRhs(TextWriter stream)
		{
			int i;

			for (i = 0; i < m_Yacc.m_RuleCount; ++i)
				stream.Write("rrhs[{0}] = {1}\n", i, m_Yacc.m_Rules[i].Rhs);
		}


		/* show_shifts is used for debugging */

		void ShowShifts(TextWriter stream)
		{
			int i, j, k;

			for (k = 0; k < m_Shiftses.Count; k++)
			{
				Shifts<ActionType> p = m_Shiftses[k];
				if (k != 0) stream.Write("\n");
				stream.Write("shift {0}, number = {1}, nshifts = {2}\n", k, p.State.Number,
					p.Shift.Length);
				j = p.Shift.Length;
				for (i = 0; i < j; ++i)
					stream.Write("\t{0}\n", p.Shift[i]);
			}
		}

		void SaveReductions()
		{
			int isp;
			int item;
			Reductions<ActionType> p;

			p = new Reductions<ActionType>(m_CurrentState);

			for (isp = 0; isp < m_Closure.ItemSetEnd; isp++)
			{
				item = m_Yacc.m_Items[m_Closure.ItemSet[isp]];
				if (item < 0)
				{
					p.Rules.Add((short)(-item));
				}
			}

			if (p.Rules.Count != 0)
			{
				m_CurrentState.Reductions = p;
			}
		}

		void SetDerives()
		{
			int i, k;
			int lhs;

			m_Yacc.m_Derives = new Rule<ActionType>[m_Yacc.m_VarCount + m_Yacc.m_RuleCount];

			k = 0;
			for (lhs = m_Yacc.m_StartSymbol; lhs < m_Yacc.m_Symbols.Length; lhs++)
			{
				m_Yacc.m_Symbols[lhs].Derives = k;
				for (i = 0; i < m_Yacc.m_RuleCount; i++)
				{
					Rule<ActionType> rule = m_Yacc.m_Rules[i];
					if (rule.Lhs.Index == lhs)
					{
						m_Yacc.m_Derives[k] = rule;
						k++;
					}
				}
				m_Yacc.m_Derives[k] = null;
				k++;
			}
#if DEBUG
			PrintDerives();
#endif
		}

#if DEBUG
		void PrintDerives()
		{
			int i;
			int sp;

			if (m_Yacc.TraceWriter == null)
				return;

			m_Yacc.TraceWriter.Write("\nDERIVES\n\n");

			for (i = m_Yacc.m_StartSymbol; i < m_Yacc.m_Symbols.Length; i++)
			{
				m_Yacc.TraceWriter.Write("{0} derives ", m_Yacc.m_Symbols[i].Name);
				for (sp = m_Yacc.m_Symbols[i].Derives; m_Yacc.m_Derives[sp] != null; sp++)
				{
					m_Yacc.TraceWriter.Write("  {0}", m_Yacc.m_Derives[sp].Number);
				}
				m_Yacc.TraceWriter.Write('\n');
			}

			m_Yacc.TraceWriter.Write('\n');
		}
#endif

		void SetNullable()
		{
			int i, j;
			bool empty;
			bool done;

			for (i = 0; i < m_Yacc.m_Symbols.Length; ++i)
				m_Yacc.m_Symbols[i].Nullable = false;

			done = false;
			while (!done)
			{
				done = true;
				for (i = 1; i < m_Yacc.m_Items.Length; i++)
				{
					empty = true;
					while ((j = m_Yacc.m_Items[i]) >= 0)
					{
						if (!m_Yacc.m_Symbols[j].Nullable)
							empty = false;
						++i;
					}
					if (empty)
					{
						Symbol symbol = m_Yacc.m_Rules[-j].Lhs;
						if (!symbol.Nullable)
						{
							symbol.Nullable = true;
							done = false;
						}
					}
				}
			}
#if DEBUG
			PrintNullable();
#endif
		}

#if DEBUG
		void PrintNullable()
		{
			int i;

			if (m_Yacc.TraceWriter == null)
				return;

			for (i = 0; i < m_Yacc.m_Symbols.Length; i++)
			{
				if (m_Yacc.m_Symbols[i].Nullable)
					m_Yacc.TraceWriter.Write("{0} is nullable\n", m_Yacc.m_Symbols[i].Name);
				else
					m_Yacc.TraceWriter.Write("{0} is not nullable\n", m_Yacc.m_Symbols[i].Name);
			}
		}
#endif
        public void Execute()
		{
			SetDerives();
			SetNullable();
			GenerateStates();
		}
	}
}
