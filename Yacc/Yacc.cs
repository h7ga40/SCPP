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
	public class Yacc<ActionType>
	{
#if ! lint
		static readonly string copyright =
			"@(#) Copyright (c) 1989 The Regents of the University of California.\n"
			+ " All rights reserved.\n";
#endif // not lint

#if ! lint
		static readonly string sccsid = "@(#)main.c	5.5 (Berkeley) 5/24/93";
#endif // not lint
		bool m_tFlag;

		private TextWriter m_TraceWriter;	/*  y.trace					    */

		internal int m_RuleCount;
		internal int m_TokenCount;
		internal int m_VarCount;

		internal int m_StartSymbol;
		internal Symbol[] m_Symbols;

		internal short[] m_Items;
		internal List<Rule<ActionType>> m_Rules;
		internal Rule<ActionType>[] m_Derives;

		internal short m_FinalState;
		internal List<short> m_Lhs = new List<short>();
		internal List<short> m_Len = new List<short>();
		internal List<short> m_DefRed = new List<short>();
		internal List<short> m_Dgoto = new List<short>();
		internal List<short> m_Sindex = new List<short>();
		internal List<short> m_Rindex = new List<short>();
		internal List<short> m_Gindex = new List<short>();
		internal List<short> m_Table = new List<short>();
		internal List<short> m_Check = new List<short>();
		internal List<string> m_Names = new List<string>();
		internal List<string> m_Rule = new List<string>();

		private int m_VectorCount;
		private int m_EntryCount;
		private short[][] m_Froms;
		private short[][] m_Tos;
		private short[] m_Tally;
		private short[] m_Width;
		private short[] m_StateCount;
		private short[] m_Order;
		private short[] m_Base;
		private short[] m_Pos;
		private int m_LowZero;

		private Error m_Error;
		private Grammar<ActionType> m_Grammar;
		private Lr0<ActionType> m_Lr0;
		private Lalr<ActionType> m_Lalr;
		private MakeParser<ActionType> m_MakeParser;
		private Verbose<ActionType> m_Verbose;

		public Yacc()
		{
			m_Error = new Error();
			m_Lr0 = new Lr0<ActionType>(this);
			m_Lalr = new Lalr<ActionType>(this);
			m_MakeParser = new MakeParser<ActionType>(this);
			m_Verbose = new Verbose<ActionType>(this);
		}

		public TextWriter TraceWriter
		{
			get { return m_TraceWriter; }
			set { m_TraceWriter = value; }
		}

		public bool tFlag
		{
			get { return m_tFlag; }
			set { m_tFlag = value; }
		}

		public Error Error
		{
			get { return m_Error; }
		}

		public Grammar<ActionType> Grammar
		{
			get { return m_Grammar; }
			set { m_Grammar = value; }
		}

		internal Lr0<ActionType> Lr0
		{
			get { return m_Lr0; }
		}

		internal Lalr<ActionType> Lalr
		{
			get { return m_Lalr; }
		}

		internal MakeParser<ActionType> MakeParser
		{
			get { return m_MakeParser; }
		}

		public Verbose<ActionType> Verbose
		{
			get { return m_Verbose; }
		}

		public bool IsToken(int s) { return ((s) < m_StartSymbol); }
		public bool IsVar(int s) { return ((s) >= m_StartSymbol); }

		protected void MakeTables()
		{
			MakeLhs();
			MakeLen();
			MakeDefRed();
			MakeDgoto();
			MakeSindex();
			MakeRindex();
			MakeGindex();
			MakeNames();
			MakeRule();
		}

		private void MakeLhs()
		{
			int i;

			m_Lhs.Add(m_Symbols[m_StartSymbol].Value);
			for (i = 3; i < m_RuleCount; ++i)
			{
				m_Lhs.Add(m_Rules[i].Lhs.Value);
			}
		}

		private void MakeLen()
		{
			int i;

			m_Len.Add(2);
			for (i = 3; i < m_RuleCount; ++i)
			{
				m_Len.Add((short)(m_Rules[i + 1].Rhs - m_Rules[i].Rhs - 1));
			}
		}

		private void MakeDefRed()
		{
			int i;

			for (i = 0; i < m_Lr0.States.Count; ++i)
			{
				m_DefRed.Add((short)(m_MakeParser.defred[i] != 0 ? m_MakeParser.defred[i] - 2 : 0));
			}
		}

		private void MakeDgoto()
		{
			m_VectorCount = 2 * m_Lr0.States.Count + m_VarCount;

			m_Froms = new short[m_VectorCount][];
			m_Tos = new short[m_VectorCount][];
			m_Tally = new short[m_VectorCount];
			m_Width = new short[m_VectorCount];

			TokenActions();

			GotoActions();

			SortActions();
			PackTable();
		}


		void TokenActions()
		{
			int i, j;
			int shiftcount, reducecount;
			int max, min;
			short[] shifts, reduces, r, s;
			int ri, si;

			shifts = new short[m_TokenCount];
			reduces = new short[m_TokenCount];
			for (i = 0; i < m_Lr0.States.Count; i++)
			{
				State<ActionType> core = m_Lr0.States[i];
				if (core.Parser.Count == 0)
					continue;

				for (j = 0; j < m_TokenCount; ++j)
				{
					shifts[j] = 0;
					reduces[j] = 0;
				}

				shiftcount = 0;
				reducecount = 0;
				foreach (Action<ActionType> p in core.Parser)
				{
					if (p.Suppressed == 0)
					{
						if (p.ActionCode == ActionCode.SHIFT)
						{
							++shiftcount;
							shifts[p.Symbol.Index] = p.Rule.Number;
						}
						else if (p.ActionCode == ActionCode.REDUCE && p.Rule.Number != m_MakeParser.defred[i])
						{
							++reducecount;
							reduces[p.Symbol.Index] = p.Rule.Number;
						}
					}
				}

				m_Tally[i] = (short)shiftcount;
				m_Tally[m_Lr0.States.Count + i] = (short)reducecount;
				m_Width[i] = 0;
				m_Width[m_Lr0.States.Count + i] = 0;
				if (shiftcount > 0)
				{
					m_Froms[i] = r = new short[shiftcount];
					ri = 0;
					m_Tos[i] = s = new short[shiftcount];
					si = 0;
					min = Defs.MAXSHORT;
					max = 0;
					for (j = 0; j < m_TokenCount; ++j)
					{
						if (shifts[j] != 0)
						{
							if (min > m_Symbols[j].Value)
								min = m_Symbols[j].Value;
							if (max < m_Symbols[j].Value)
								max = m_Symbols[j].Value;
							r[ri++] = m_Symbols[j].Value;
							s[si++] = shifts[j];
						}
					}
					m_Width[i] = (short)(max - min + 1);
				}
				if (reducecount > 0)
				{
					m_Froms[m_Lr0.States.Count + i] = r = new short[reducecount];
					ri = 0;
					m_Tos[m_Lr0.States.Count + i] = s = new short[reducecount];
					si = 0;
					min = Defs.MAXSHORT;
					max = 0;
					for (j = 0; j < m_TokenCount; ++j)
					{
						if (reduces[j] != 0)
						{
							if (min > m_Symbols[j].Value)
								min = m_Symbols[j].Value;
							if (max < m_Symbols[j].Value)
								max = m_Symbols[j].Value;
							r[ri++] = m_Symbols[j].Value;
							s[si++] = (short)(reduces[j] - 2);
						}
					}
					m_Width[m_Lr0.States.Count + i] = (short)(max - min + 1);
				}
			}
		}

		void GotoActions()
		{
			int i, k;

			m_StateCount = new short[m_Lr0.States.Count];

			k = DefaultGoto(m_StartSymbol + 1);

			m_Dgoto.Add((short)k);
			SaveColumn(m_StartSymbol + 1, k);
			for (i = m_StartSymbol + 2; i < m_Symbols.Length; ++i)
			{
				k = DefaultGoto(i);
				m_Dgoto.Add((short)k);
				SaveColumn(i, k);
			}
		}

		int DefaultGoto(int symbol)
		{
			int i;
			int m;
			int n;
			int default_state;
			int max;

			m = m_Lalr.goto_map_inst[m_Lalr.goto_map + symbol];
			n = m_Lalr.goto_map_inst[m_Lalr.goto_map + symbol + 1];

			if (m == n) return 0;

			for (i = 0; i < m_Lr0.States.Count; i++)
				m_StateCount[i] = 0;

			for (i = m; i < n; i++)
				m_StateCount[m_Lalr.to_state[i].Number]++;

			max = 0;
			default_state = 0;
			for (i = 0; i < m_Lr0.States.Count; i++)
			{
				if (m_StateCount[i] > max)
				{
					max = m_StateCount[i];
					default_state = i;
				}
			}

			return default_state;
		}

		void SaveColumn(int symbol, int default_state)
		{
			int i;
			int m;
			int n;
			short[] sp;
			short[] sp1;
			int sp1i;
			short[] sp2;
			int sp2i;
			int count;
			int symno;

			m = m_Lalr.goto_map_inst[m_Lalr.goto_map + symbol];
			n = m_Lalr.goto_map_inst[m_Lalr.goto_map + symbol + 1];

			count = 0;
			for (i = m; i < n; i++)
			{
				if (m_Lalr.to_state[i].Number != default_state)
					++count;
			}
			if (count == 0) return;

			symno = m_Symbols[symbol].Value + 2 * m_Lr0.States.Count;

			m_Froms[symno] = sp1 = sp = new short[count];
			sp1i = 0;
			m_Tos[symno] = sp2 = new short[count];
			sp2i = 0;

			for (i = m; i < n; i++)
			{
				if (m_Lalr.to_state[i].Number != default_state)
				{
					sp1[sp1i++] = m_Lalr.from_state[i].Number;
					sp2[sp2i++] = m_Lalr.to_state[i].Number;
				}
			}

			m_Tally[symno] = (short)count;
			m_Width[symno] = (short)(sp1[sp1i - 1] - sp[0] + 1);
		}

		void SortActions()
		{
			int i;
			int j;
			int k;
			int t;
			int w;

			m_Order = new short[m_VectorCount];
			m_EntryCount = 0;

			for (i = 0; i < m_VectorCount; i++)
			{
				if (m_Tally[i] > 0)
				{
					t = m_Tally[i];
					w = m_Width[i];
					j = m_EntryCount - 1;

					while (j >= 0 && (m_Width[m_Order[j]] < w))
						j--;

					while (j >= 0 && (m_Width[m_Order[j]] == w) && (m_Tally[m_Order[j]] < t))
						j--;

					for (k = m_EntryCount - 1; k > j; k--)
						m_Order[k + 1] = m_Order[k];

					m_Order[j + 1] = (short)i;
					m_EntryCount++;
				}
			}
		}

		void PackTable()
		{
			int i;
			int place;
			int state;

			m_Base = new short[m_VectorCount];
			m_Pos = new short[m_EntryCount];

			m_LowZero = 0;

			for (i = 0; i < m_EntryCount; i++)
			{
				state = MatchingVector(i);

				if (state < 0)
					place = PackVector(i);
				else
					place = m_Base[state];

				m_Pos[i] = (short)place;
				m_Base[m_Order[i]] = (short)place;
			}
		}

		/*  The function matching_vector determines if the vector specified by	*/
		/*  the input parameter matches a previously considered	vector.  The	*/
		/*  test at the start of the function checks if the vector represents	*/
		/*  a row of shifts over terminal symbols or a row of reductions, or a	*/
		/*  column of shifts over a nonterminal symbol.  Berkeley Yacc does not	*/
		/*  m_Check if a column of shifts over a nonterminal symbols matches a	*/
		/*  previously considered vector.  Because of the nature of LR parsing	*/
		/*  tables, no two columns can match.  Therefore, the only possible	*/
		/*  match would be between a row and a column.  Such matches are	*/
		/*  unlikely.  Therefore, to save time, no attempt is made to see if a	*/
		/*  column matches a previously considered vector.			*/
		/*									*/
		/*  Matching_vector is poorly designed.  The test could easily be made	*/
		/*  faster.  Also, it depends on the vectors being in a specific	*/
		/*  order.								*/

		int MatchingVector(int vector)
		{
			int i;
			int j;
			int k;
			int t;
			int w;
			int match;
			int prev;

			i = m_Order[vector];
			if (i >= 2 * m_Lr0.States.Count)
				return -1;

			t = m_Tally[i];
			w = m_Width[i];

			for (prev = vector - 1; prev >= 0; prev--)
			{
				j = m_Order[prev];
				if (m_Width[j] != w || m_Tally[j] != t)
					return -1;

				match = 1;
				for (k = 0; match != 0 && k < t; k++)
				{
					if (m_Tos[j][k] != m_Tos[i][k] || m_Froms[j][k] != m_Froms[i][k])
						match = 0;
				}

				if (match != 0)
					return j;
			}

			return -1;
		}

		int PackVector(int vector)
		{
			int i, j, k, l;
			int t;
			int loc;
			int ok;
			short[] from;
			short[] to;

			i = m_Order[vector];
			t = m_Tally[i];
			System.Diagnostics.Debug.Assert(t != 0);

			from = m_Froms[i];
			to = m_Tos[i];

			j = m_LowZero - from[0];
			for (k = 1; k < t; ++k)
				if (m_LowZero - from[k] > j)
					j = m_LowZero - from[k];
			for (; ; ++j)
			{
				if (j == 0)
					continue;
				ok = 1;
				for (k = 0; ok != 0 && k < t; k++)
				{
					loc = j + from[k];
					if (loc >= m_Table.Count)
					{
						if (loc >= Defs.MAXTABLE)
							m_Error.Fatal("maximum m_Table size exceeded");

						for (l = m_Table.Count; l <= loc; ++l)
						{
							m_Table.Add(0);
							m_Check.Add(-1);
						}
					}

					if (m_Check[loc] != -1)
						ok = 0;
				}
				for (k = 0; ok != 0 && k < vector; k++)
				{
					if (m_Pos[k] == j)
						ok = 0;
				}
				if (ok != 0)
				{
					for (k = 0; k < t; k++)
					{
						loc = j + from[k];
						m_Table[loc] = to[k];
						m_Check[loc] = from[k];
					}

					while (m_Check[m_LowZero] != -1)
						++m_LowZero;

					return j;
				}
			}
		}

		private void MakeSindex()
		{
			int i;

			for (i = 0; i < m_Lr0.States.Count; ++i)
			{
				m_Sindex.Add(m_Base[i]);
			}
		}

		private void MakeRindex()
		{
			int i;

			m_Rindex.Add(m_Base[m_Lr0.States.Count]);
			for (i = m_Lr0.States.Count + 1; i < 2 * m_Lr0.States.Count; ++i)
			{
				m_Rindex.Add(m_Base[i]);
			}
		}

		private void MakeGindex()
		{
			int i;

			m_Gindex.Add(m_Base[2 * m_Lr0.States.Count]);
			for (i = 2 * m_Lr0.States.Count + 1; i < m_VectorCount - 1; ++i)
			{
				m_Gindex.Add(m_Base[i]);
			}
		}

		private void MakeNames()
		{
			int i, max;

			max = 0;
			for (i = 2; i < m_TokenCount; ++i)
				if (m_Symbols[i].Value > max)
					max = m_Symbols[i].Value;

			for (i = 0; i <= max; ++i) /* no need to init [max] */
				m_Names.Add(null);
			for (i = m_TokenCount - 1; i >= 2; --i)
				m_Names[m_Symbols[i].Value] = m_Symbols[i].Name;
			m_Names[0] = "end-of-file";
		}

		private void MakeRule()
		{
			int i, j;
			StringBuilder str;

			for (i = 2; i < m_RuleCount; ++i)
			{
				str = new StringBuilder(m_Rules[i].Lhs.Name);
				str.Append(":");
				for (j = m_Rules[i].Rhs; m_Items[j] > 0; ++j)
				{
					str.Append(" ");
					str.Append(m_Symbols[m_Items[j]].Name);
				}
				m_Rule.Add(str.ToString());
			}
		}

		public void Execute()
		{
			m_Grammar.Execute();
			m_Lr0.Execute();
			m_Lalr.Execute();
			m_MakeParser.Execute();
			m_Verbose.Execute();
			MakeTables();
		}
	}
}
