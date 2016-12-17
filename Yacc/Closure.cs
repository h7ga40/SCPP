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
	class Closure<ActionType>
	{
#if !lint
		static readonly string sccsid = "@(#)closure.c	5.3 (Berkeley) 5/24/93";
#endif // not lint

		private short[] m_ItemSet;
		private int m_ItemSetEnd;
		private uint[] m_RuleSet;

		private uint[] m_FirstDerivesInst;
		private int m_FirstDerives;
		private uint[] m_EFF;

		private Yacc<ActionType> m_Yacc;

		public Closure(Yacc<ActionType> yacc)
		{
			this.m_Yacc = yacc;
		}

		public short[] ItemSet {
			get { return m_ItemSet; }
			set { m_ItemSet = value; }
		}

		public int ItemSetEnd {
			get { return m_ItemSetEnd; }
			set { m_ItemSetEnd = value; }
		}

		public uint[] RuleSet {
			get { return m_RuleSet; }
			set { m_RuleSet = value; }
		}

		void SetEFF()
		{
			int row;
			int symbol;
			int sp;
			int rowsize;
			int i, j;
			Rule<ActionType> rule;

			rowsize = Defs.WORDSIZE(m_Yacc.m_VarCount);
			m_EFF = new uint[m_Yacc.m_VarCount * rowsize];

			row = 0;
			for (i = m_Yacc.m_StartSymbol; i < m_Yacc.m_Symbols.Length; i++) {
				sp = m_Yacc.m_Symbols[i].Derives;
				for (j = 0, rule = m_Yacc.m_Derives[sp + j]; rule != null; ++j, rule = m_Yacc.m_Derives[sp + j]) {
					symbol = m_Yacc.m_Items[rule.Rhs];
					if (m_Yacc.IsVar(symbol)) {
						symbol -= m_Yacc.m_StartSymbol;
						Defs.SETBIT(m_EFF, row, symbol);
					}
				}
				row += rowsize;
			}

			Warshall.ReflexiveTransitiveClosure(m_EFF, m_Yacc.m_VarCount);

#if DEBUG
			PrintEFF();
#endif
		}

		public void SetFirstDerives()
		{
			int rrow;
			int vrow;
			int j;
			int k;
			uint cword = 0;
			int rp;
			int r;

			Rule<ActionType> rule;
			int i;
			int rulesetsize;
			int varsetsize;

			rulesetsize = Defs.WORDSIZE(m_Yacc.m_RuleCount);
			varsetsize = Defs.WORDSIZE(m_Yacc.m_VarCount);
			m_FirstDerivesInst = new uint[m_Yacc.m_VarCount * rulesetsize];
			m_FirstDerives = -m_Yacc.m_TokenCount * rulesetsize;

			SetEFF();

			rrow = 0;
			for (i = m_Yacc.m_StartSymbol; i < m_Yacc.m_Symbols.Length; i++) {
				vrow = ((i - m_Yacc.m_TokenCount) * varsetsize);
				k = Defs.BITS_PER_WORD;
				for (j = m_Yacc.m_StartSymbol; j < m_Yacc.m_Symbols.Length; k++, j++) {
					if (k >= Defs.BITS_PER_WORD) {
						cword = m_EFF[vrow++];
						k = 0;
					}

					if ((cword & (1u << k)) != 0) {
						rp = m_Yacc.m_Symbols[j].Derives;
						r = 0;
						while ((rule = m_Yacc.m_Derives[rp + (r++)]) != null) {
							Defs.SETBIT(m_FirstDerivesInst, rrow, rule.Number);
						}
					}
				}

				vrow += varsetsize;
				rrow += rulesetsize;
			}

#if DEBUG
			PrintFirstDerives();
#endif
		}

		public void Execute(short[] nucleus, int n)
		{
			int ruleno;
			uint word;
			int i;
			int csp;
			int dsp;
			int rsp;
			int rulesetsize;

			int csend;
			int rsend;
			int symbol;
			int itemno;

			rulesetsize = Defs.WORDSIZE(m_Yacc.m_RuleCount);
			rsp = 0;
			rsend = rulesetsize;
			for (rsp = 0; rsp < rsend; rsp++)
				m_RuleSet[rsp] = 0;

			csend = n;
			for (csp = 0; csp < csend; ++csp) {
				symbol = m_Yacc.m_Items[nucleus[csp]];
				if (m_Yacc.IsVar(symbol)) {
					dsp = m_FirstDerives + symbol * rulesetsize;
					rsp = 0;
					while (rsp < rsend)
						m_RuleSet[rsp++] |= m_FirstDerivesInst[dsp++];
				}
			}

			ruleno = 0;
			m_ItemSetEnd = 0;
			csp = 0;
			for (rsp = 0; rsp < rsend; ++rsp) {
				word = m_RuleSet[rsp];
				if (word != 0) {
					for (i = 0; i < Defs.BITS_PER_WORD; ++i) {
						if ((word & (1 << i)) != 0) {
							itemno = m_Yacc.m_Rules[ruleno + i].Rhs;
							while (csp < csend && nucleus[csp] < itemno)
								m_ItemSet[m_ItemSetEnd++] = nucleus[csp++];
							m_ItemSet[m_ItemSetEnd++] = (short)itemno;
							while (csp < csend && nucleus[csp] == itemno)
								++csp;
						}
					}
				}
				ruleno += Defs.BITS_PER_WORD;
			}

			while (csp < csend)
				m_ItemSet[m_ItemSetEnd++] = nucleus[csp++];

#if DEBUG
			PrintClosure(n);
#endif
		}


		public void FinalizeClosure()
		{
		}

#if DEBUG
		void PrintClosure(int n)
		{
			int isp;

			if (m_Yacc.TraceWriter == null)
				return;

			m_Yacc.TraceWriter.Write("\n\nn = {0}\n\n", n);
			for (isp = 0; isp < m_ItemSetEnd; isp++)
				m_Yacc.TraceWriter.Write("   {0}\n", m_ItemSet[isp]);
		}


		void PrintEFF()
		{
			int i, j;
			int rowp;
			uint word;
			int k;

			if (m_Yacc.TraceWriter == null)
				return;

			m_Yacc.TraceWriter.Write("\n\nEpsilon Free Firsts\n");

			for (i = m_Yacc.m_StartSymbol; i < m_Yacc.m_Symbols.Length; i++) {
				m_Yacc.TraceWriter.Write("\n{0}", m_Yacc.m_Symbols[i].Name);
				rowp = ((i - m_Yacc.m_StartSymbol) * Defs.WORDSIZE(m_Yacc.m_VarCount));
				word = m_EFF[rowp++];

				k = Defs.BITS_PER_WORD;
				for (j = 0; j < m_Yacc.m_VarCount; k++, j++) {
					if (k >= Defs.BITS_PER_WORD) {
						word = (rowp < m_EFF.Length) ? m_EFF[rowp++] : 0;

						k = 0;
					}

					if ((word & (1 << k)) != 0)
						m_Yacc.TraceWriter.Write("  {0}", m_Yacc.m_Symbols[m_Yacc.m_StartSymbol + j].Name);
				}
			}
		}


		void PrintFirstDerives()
		{
			int i;
			int j;
			int rp;
			uint cword = 0;
			int k;

			if (m_Yacc.TraceWriter == null)
				return;

			m_Yacc.TraceWriter.Write("\n\n\nFirst Derives\n");

			for (i = m_Yacc.m_StartSymbol; i < m_Yacc.m_Symbols.Length; i++) {
				m_Yacc.TraceWriter.Write("\n{0} derives\n", m_Yacc.m_Symbols[i].Name);
				rp = m_FirstDerives + i * Defs.WORDSIZE(m_Yacc.m_RuleCount);
				k = Defs.BITS_PER_WORD;
				for (j = 0; j <= m_Yacc.m_RuleCount; k++, j++) {
					if (k >= Defs.BITS_PER_WORD) {
						cword = m_FirstDerivesInst[rp++];
						k = 0;
					}

					if ((cword & (1 << k)) != 0)
						m_Yacc.TraceWriter.Write("   {0}\n", j);
				}
			}

			m_Yacc.TraceWriter.Flush();
		}
#endif
	}
}
