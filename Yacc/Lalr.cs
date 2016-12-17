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
	class Lalr<ActionType>
	{
#if !lint
		static readonly string sccsid = "@(#)lalr.c	5.3 (Berkeley) 6/1/90";
#endif // not lint

		class Shorts
		{
			public Shorts next;
			public short value;
		}

		public int tokensetsize;
		public short[] lookaheads;
		public short[] LAruleno;
		public uint[] LA;
		public int goto_map;
		public short[] goto_map_inst;
		public State<ActionType>[] from_state;
		public State<ActionType>[] to_state;

		private int infinity;
		private int maxrhs;
		private int ngotos;
		private uint[] F;
		private short[][] includes;
		private Shorts[] lookback;
		private short[][] R;
		private short[] INDEX;
		private short[] VERTICES;
		private int top;

		private Yacc<ActionType> m_Yacc;
		private Lr0<ActionType> m_Lr0;
		private Error m_Error;

		public Lalr(Yacc<ActionType> yacc)
		{
			this.m_Yacc = yacc;
			m_Lr0 = yacc.Lr0;
			m_Error = yacc.Error;
		}

		public void Execute()
		{
			tokensetsize = Defs.WORDSIZE(m_Yacc.m_TokenCount);

			SetMaxrhs();
			InitializeLA();
			SetGotoMap();
			InitializeF();
			BuildRelations();
			ComputeFollows();
			ComputeLookaheads();
		}

		void SetMaxrhs()
		{
			int itemp;
			int item_end;
			int length;
			int max;

			length = 0;
			max = 0;
			item_end = m_Yacc.m_Items.Length;
			for (itemp = 0; itemp < item_end; itemp++) {
				if (m_Yacc.m_Items[itemp] >= 0) {
					length++;
				}
				else {
					if (length > max) max = length;
					length = 0;
				}
			}

			maxrhs = max;
		}

		void InitializeLA()
		{
			int i, j, k;
			Reductions<ActionType> rp;

			lookaheads = new short[m_Lr0.States.Count + 1];

			k = 0;
			for (i = 0; i < m_Lr0.States.Count; i++) {
				lookaheads[i] = (short)k;
				rp = m_Lr0.States[i].Reductions;
				if (rp != null)
					k += rp.Rules.Count;
			}
			lookaheads[m_Lr0.States.Count] = (short)k;

			LA = new uint[k * tokensetsize];
			LAruleno = new short[k];
			lookback = new Shorts[k];

			k = 0;
			for (i = 0; i < m_Lr0.States.Count; i++) {
				rp = m_Lr0.States[i].Reductions;
				if (rp != null) {
					for (j = 0; j < rp.Rules.Count; j++) {
						LAruleno[k] = rp.Rules[j];
						k++;
					}
				}
			}
		}

		void SetGotoMap()
		{
			int i;
			int symbol;
			int k;
			int temp_map;
			short[] temp_map_inst;
			State<ActionType> state2;
			State<ActionType> state1;

			goto_map_inst = new short[m_Yacc.m_VarCount + 1];
			goto_map = -m_Yacc.m_TokenCount;
			temp_map_inst = new short[m_Yacc.m_VarCount + 1];
			temp_map = -m_Yacc.m_TokenCount;

			ngotos = 0;
			foreach (Shifts<ActionType> sp in m_Lr0.Shiftses) {
				for (i = sp.Shift.Length - 1; i >= 0; i--) {
					symbol = sp.Shift[i].AccessingSymbol.Index;

					if (m_Yacc.IsToken(symbol)) break;

					if (ngotos == Defs.MAXSHORT)
						m_Error.Fatal("too many gotos");

					ngotos++;
					goto_map_inst[goto_map + symbol]++;
				}
			}

			k = 0;
			for (i = m_Yacc.m_TokenCount; i < m_Yacc.m_Symbols.Length; i++) {
				temp_map_inst[temp_map + i] = (short)k;
				k += goto_map_inst[goto_map + i];
			}

			for (i = m_Yacc.m_TokenCount; i < m_Yacc.m_Symbols.Length; i++)
				goto_map_inst[goto_map + i] = temp_map_inst[temp_map + i];

			goto_map_inst[goto_map + m_Yacc.m_Symbols.Length] = (short)ngotos;
			temp_map_inst[temp_map + m_Yacc.m_Symbols.Length] = (short)ngotos;

			from_state = new State<ActionType>[ngotos];
			to_state = new State<ActionType>[ngotos];

			foreach (Shifts<ActionType> sp in m_Lr0.Shiftses) {
				state1 = sp.State;
				for (i = sp.Shift.Length - 1; i >= 0; i--) {
					state2 = sp.Shift[i];
					symbol = state2.AccessingSymbol.Index;

					if (m_Yacc.IsToken(symbol)) break;

					k = temp_map_inst[temp_map + symbol]++;
					from_state[k] = state1;
					to_state[k] = state2;
				}
			}
		}

		/*  Map_goto maps a state/symbol pair into its numeric representation.	*/

		int MapGoto(int state, int symbol)
		{
			int high;
			int low;
			int middle;
			int s;

			low = goto_map_inst[goto_map + symbol];
			high = goto_map_inst[goto_map + symbol + 1];

			for (;;) {
				System.Diagnostics.Debug.Assert(low <= high);
				middle = (low + high) >> 1;
				s = from_state[middle].Number;
				if (s == state)
					return middle;
				else if (s < state)
					low = middle + 1;
				else
					high = middle - 1;
			}
		}

		void InitializeF()
		{
			int i;
			int j;
			int k;
			Shifts<ActionType> sp;
			short[] edge;
			int rowp;
			short[] rp;
			short[][] reads;
			int nedges;
			int stateno;
			int symbol;
			int nwords;

			nwords = ngotos * tokensetsize;
			F = new uint[nwords];

			reads = new short[ngotos][];
			edge = new short[ngotos + 1];
			nedges = 0;

			rowp = 0;
			for (i = 0; i < ngotos; i++) {
				stateno = to_state[i].Number;
				sp = m_Lr0.States[stateno].Shifts;

				if (sp != null) {
					k = sp.Shift.Length;

					for (j = 0; j < k; j++) {
						symbol = sp.Shift[j].AccessingSymbol.Index;
						if (m_Yacc.IsVar(symbol))
							break;
						Defs.SETBIT(F, rowp, symbol);
					}

					for (; j < k; j++) {
						symbol = sp.Shift[j].AccessingSymbol.Index;
						if (m_Yacc.m_Symbols[symbol].Nullable)
							edge[nedges++] = (short)MapGoto(stateno, symbol);
					}

					if (nedges != 0) {
						reads[i] = rp = new short[nedges + 1];

						for (j = 0; j < nedges; j++)
							rp[j] = edge[j];

						rp[nedges] = -1;
						nedges = 0;
					}
				}

				rowp += tokensetsize;
			}

			if (F.Length > 0)
				Defs.SETBIT(F, 0, 0);
			Digraph(reads);
		}

		void BuildRelations()
		{
			int i;
			int j;
			int k;
			int rulep;
			int rp;
			Shifts<ActionType> sp;
			int length;
			int nedges;
			bool done;
			State<ActionType> state1;
			State<ActionType> state;
			int symbol1;
			int symbol2;
			short[] shortp;
			short[] edge;
			State<ActionType>[] states;
			short[][] new_includes;

			includes = new short[ngotos][];
			edge = new short[ngotos + 1];
			states = new State<ActionType>[maxrhs + 1];

			for (i = 0; i < ngotos; i++) {
				nedges = 0;
				state1 = from_state[i];
				symbol1 = to_state[i].AccessingSymbol.Index;

				for (rulep = m_Yacc.m_Symbols[symbol1].Derives; m_Yacc.m_Derives[rulep] != null; rulep++) {
					length = 1;
					states[0] = state1;
					state = state1;

					for (rp = m_Yacc.m_Derives[rulep].Rhs; m_Yacc.m_Items[rp] >= 0; rp++) {
						symbol2 = m_Yacc.m_Items[rp];
						sp = state.Shifts;
						k = sp.Shift.Length;

						for (j = 0; j < k; j++) {
							state = sp.Shift[j];
							if (state.AccessingSymbol.Index == symbol2) break;
						}

						states[length++] = state;
					}

					AddLookbackEdge(state.Number, m_Yacc.m_Derives[rulep].Number, i);

					length--;
					done = false;
					while (!done) {
						done = true;
						rp--;
						if (m_Yacc.IsVar(m_Yacc.m_Items[rp])) {
							state = states[--length];
							edge[nedges++] = (short)MapGoto(state.Number, m_Yacc.m_Items[rp]);
							if (m_Yacc.m_Symbols[m_Yacc.m_Items[rp]].Nullable && length > 0) done = false;
						}
					}
				}

				if (nedges != 0) {
					includes[i] = shortp = new short[nedges + 1];
					for (j = 0; j < nedges; j++)
						shortp[j] = edge[j];
					shortp[nedges] = -1;
				}
			}

			new_includes = Transpose(includes, ngotos);

			includes = new_includes;
		}


		void AddLookbackEdge(int stateno, int ruleno, int gotono)
		{
			int i, k;
			bool found;
			Shorts sp;

			i = lookaheads[stateno];
			k = lookaheads[stateno + 1];
			found = false;
			while (!found && i < k) {
				if (LAruleno[i] == ruleno)
					found = true;
				else
					++i;
			}
			System.Diagnostics.Debug.Assert(found);

			sp = new Shorts();
			sp.next = lookback[i];
			sp.value = (short)gotono;
			lookback[i] = sp;
		}

		short[][] Transpose(short[][] R, int n)
		{
			short[][] new_R;
			int[] temp_R;
			short[] nedges;
			short[] sp;
			int spi;
			int i;
			int k;

			nedges = new short[n];

			for (i = 0; i < n; i++) {
				sp = R[i];
				spi = 0;
				if (sp != null) {
					while (sp[spi] >= 0)
						nedges[sp[spi++]]++;
				}
			}

			new_R = new short[n][];
			temp_R = new int[n];

			for (i = 0; i < n; i++) {
				k = nedges[i];
				if (k > 0) {
					sp = new short[k + 1];
					new_R[i] = sp;
					temp_R[i] = 0;
					sp[k] = -1;
				}
			}

			for (i = 0; i < n; i++) {
				sp = R[i];
				spi = 0;
				if (sp != null) {
					int j;
					while ((j = sp[spi]) >= 0) {
						new_R[j][temp_R[j]++] = (short)i;
						spi++;
					}
				}
			}

			return new_R;
		}

		void ComputeFollows()
		{
			Digraph(includes);
		}


		void ComputeLookaheads()
		{
			int i, n;
			int fp1, fp2, fp3;
			Shorts sp;
			int rowp;

			rowp = 0;
			n = lookaheads[m_Lr0.States.Count];
			for (i = 0; i < n; i++) {
				fp3 = rowp + tokensetsize;
				for (sp = lookback[i]; sp != null; sp = sp.next) {
					fp1 = rowp;
					fp2 = tokensetsize * sp.value;
					while (fp1 < fp3)
						LA[fp1++] |= F[fp2++];
				}
				rowp = fp3;
			}
		}


		void Digraph(short[][] relation)
		{
			int i;

			infinity = ngotos + 2;
			INDEX = new short[ngotos + 1];
			VERTICES = new short[ngotos + 1];
			top = 0;

			R = relation;

			for (i = 0; i < ngotos; i++)
				INDEX[i] = 0;

			for (i = 0; i < ngotos; i++) {
				if (INDEX[i] == 0 && R[i] != null)
					Traverse(i);
			}
		}



		void Traverse(int i)
		{
			int fp1;
			int fp2;
			int fp3;
			int j;
			int rp;

			int height;
			int Base;

			VERTICES[++top] = (short)i;
			INDEX[i] = (short)(height = top);

			Base = i * tokensetsize;
			fp3 = Base + tokensetsize;

			rp = 0;
			if (R[i] != null) {
				while ((j = R[i][rp++]) >= 0) {
					if (INDEX[j] == 0)
						Traverse(j);

					if (INDEX[i] > INDEX[j])
						INDEX[i] = INDEX[j];

					fp1 = Base;
					fp2 = j * tokensetsize;

					while (fp1 < fp3)
						F[fp1++] |= F[fp2++];
				}
			}

			if (INDEX[i] == height) {
				for (;;) {
					j = VERTICES[top--];
					INDEX[j] = (short)infinity;

					if (i == j)
						break;

					fp1 = Base;
					fp2 = j * tokensetsize;

					while (fp1 < fp3)
						F[fp2++] = F[fp1++];
				}
			}
		}
	}
}