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
 *
 *	@(#)defs.h	5.6 (Berkeley) 5/24/93
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Yacc
{
	/*  machine-dependent definitions			*/
	/*  the following definitions are for the Tahoe		*/
	/*  they might have to be changed for other machines	*/
	static class Defs
	{
		/*  MAXCHAR is the largest unsigned character value	*/
		public const int MAXCHAR = 255;
		/*  MAXSHORT is the largest value of a C short		*/
		public const int MAXSHORT = 32767;
		/*  MINSHORT is the most negative value of a C short	*/
		public const int MINSHORT = -32768;
		/*  MAXTABLE is the maximum table size			*/
		public const int MAXTABLE = 32500;
		/*  BITS_PER_WORD is the number of bits in a C unsigned */
		public const int BITS_PER_WORD = 32;
		/*  WORDSIZE computes the number of words needed to	*/
		/*	store n bits					*/
		public static int WORDSIZE(int n) { return (((n) + (BITS_PER_WORD - 1)) / BITS_PER_WORD); }
		/*  BIT returns the value of the n-th bit starting	*/
		/*	from r (0-indexed)				*/
		public static uint BIT(uint[] r, int o, int n) { return ((((r)[o + ((n) >> 5)]) >> ((n) & 31)) & 1); }
		/*  SETBIT sets the n-th bit starting from r		*/
		public static void SETBIT(uint[] r, int o, int n) { r[o + ((n) >> 5)] |= (1u << ((n) & 31)); }

		/*  the undefined value  */
		public const int UNDEFINED = -1;

		/* end of file */
		public const int EOF = -1;
	}

	/* keyword codes */
	public enum KeywordCode
	{
		TOKEN = 0,
		LEFT = 1,
		RIGHT = 2,
		NONASSOC = 3,
		MARK = 4,
		TEXT = 5,
		TYPE = 6,
		START = 7,
	}

	/*  symbol classes  */
	public enum SymbolClasse
	{
		UNKNOWN = 0,
		TERM = 1,
		NONTERM = 2,
	}

	/*  action codes  */
	public enum ActionCode
	{
		NONE = 0,
		SHIFT = 1,
		REDUCE = 2,
	}

	/*  the structure of a symbol table entry  */
	public class Symbol
	{
		private string m_Name;
		private short m_Value;
		private string m_Tag;
		private short m_Index;
		private SymbolClasse m_Class;
		private short m_Precedence;
		private KeywordCode m_Association;
		private short m_Shift;
		private bool m_Nullable;
		private int m_Derives;

		public Symbol(string name)
		{
			System.Diagnostics.Debug.Assert(name != null);
			m_Name = name;
			m_Value = Defs.UNDEFINED;
			m_Precedence = 0;
			m_Association = KeywordCode.TOKEN;
			m_Tag = null;
			m_Index = 0;
			m_Class = SymbolClasse.UNKNOWN;
		}

		public Symbol(string name, short value, short prec, KeywordCode associate)
		{
			m_Name = name;
			m_Value = value;
			m_Precedence = prec;
			m_Association = associate;
			m_Tag = null;
			m_Index = 0;
			m_Class = SymbolClasse.UNKNOWN;
		}

		public string Name {
			get { return m_Name; }
		}

		public short Value {
			get { return m_Value; }
			internal set { m_Value = value; }
		}

		public string Tag {
			get { return m_Tag; }
			internal set { m_Tag = value; }
		}

		public short Index {
			get { return m_Index; }
			internal set { m_Index = value; }
		}

		public SymbolClasse Class {
			get { return m_Class; }
			internal set { m_Class = value; }
		}

		public short Precedence {
			get { return m_Precedence; }
			internal set { m_Precedence = value; }
		}

		public KeywordCode Association {
			get { return m_Association; }
			internal set { m_Association = value; }
		}

		public short Shift {
			get { return m_Shift; }
			internal set { m_Shift = value; }
		}

		public bool Nullable {
			get { return m_Nullable; }
			internal set { m_Nullable = value; }
		}

		public int Derives {
			get { return m_Derives; }
			internal set { m_Derives = value; }
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(String.Format("Name : {0}, ", m_Name));
			sb.Append(String.Format("Value : {0}, ", m_Value));
			sb.Append(String.Format("Tag : {0}, ", m_Tag));
			sb.Append(String.Format("Index : {0}, ", m_Index));
			sb.Append(String.Format("Class : {0}, ", m_Class));
			sb.Append(String.Format("Precedence : {0}, ", m_Precedence));
			sb.Append(String.Format("Association : {0}, ", m_Association));
			sb.Append(String.Format("Shift : {0}, ", m_Shift));
			sb.Append(String.Format("Nullable : {0}, ", m_Nullable));
			sb.Append(String.Format("Derives : {0}\n", m_Derives));

			return sb.ToString();
		}
	}

	public delegate void ActionEventHandler<ValueType>(int n, IList<ValueType> vals, ref ValueType val);

	class Rule<ActionType>
	{
		public Symbol Lhs;
		public short Number;
		public short Rhs;
		public short Precedence;
		public KeywordCode Association;
		private ActionType m_Action;
		private int m_Offset;

		internal Rule()
		{
		}

		internal Rule(Symbol lhs, short prec, KeywordCode assoc)
		{
			Lhs = lhs;
			Precedence = prec;
			Association = assoc;
		}

		internal ActionType Action {
			get { return m_Action; }
			set { m_Action = value; }
		}

		internal int Offset {
			get { return m_Offset; }
			set { m_Offset = value; }
		}
	}

	/*  the structure of the LR(0) state machine  */
	class State<ActionType>
	{
		public State<ActionType> Link;
		public short Number;
		public Symbol AccessingSymbol;
		public short[] Items;
		public LinkedList<Action<ActionType>> Parser = new LinkedList<Action<ActionType>>();
		public Shifts<ActionType> Shifts;
		public Reductions<ActionType> Reductions;

		public State(int n)
		{
			Items = new short[n];
		}
	}

	/*  the structure used to record shifts  */
	class Shifts<ActionType>
	{
		public State<ActionType> State;
		public State<ActionType>[] Shift;

		public Shifts(State<ActionType> state, int n)
		{
			State = state;
			state.Shifts = this;
			Shift = new State<ActionType>[n];
		}
	};

	/*  the structure used to store reductions  */
	class Reductions<ActionType>
	{
		public State<ActionType> State;
		public List<short> Rules = new List<short>();

		public Reductions(State<ActionType> state)
		{
			State = state;
		}
	};

	/*  the structure used to represent parser actions  */
	class Action<ActionType>
	{
		public Symbol Symbol;
		public Rule<ActionType> Rule;
		public short Prec;
		public ActionCode ActionCode;
		public KeywordCode Associate;
		public byte Suppressed;
	}
}
