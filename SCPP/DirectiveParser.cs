/*
 * parse.y - #if parser for the selective C preprocessor, scpp.
 *
 * Copyright (c) 1985 by
 * Tektronix, Incorporated Beaverton, Oregon 97077
 * All rights reserved.
 *
 * Permission is hereby granted for personal, non-commercial
 * reproduction and use of this program, provided that this
 * notice and all copyright notices are included in any copy.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yacc;
using System.IO;

namespace SCPP
{
	public class DirectiveParser : Parser<ExpressionNode>
	{
		public DirectiveParser()
		{
			Execute();
		}

		/// <summary>*</summary>
		public static Symbol MUL;
		/// <summary>/</summary>
		public static Symbol DIV;
		/// <summary>%</summary>
		public static Symbol MOD;
		/// <summary>+</summary>
		public static Symbol PLUS;
		/// <summary>-</summary>
		public static Symbol MINUS;
		/// <summary>&lt;&lt;</summary>
		public static Symbol LS;
		/// <summary>>></summary>
		public static Symbol RS;
		/// <summary>&amp;</summary>
		public static Symbol AND;
		/// <summary>|</summary>
		public static Symbol OR;
		/// <summary>^</summary>
		public static Symbol ER;
		/// <summary>&lt;</summary>
		public static Symbol LT;
		/// <summary>&lt;=</summary>
		public static Symbol LE;
		/// <summary>></summary>
		public static Symbol GT;
		/// <summary>>=</summary>
		public static Symbol GE;
		/// <summary>==</summary>
		public static Symbol EQ;
		/// <summary>!=</summary>
		public static Symbol NE;
		/// <summary>&&</summary>
		public static Symbol ANDAND;
		/// <summary>||</summary>
		public static Symbol OROR;
		/// <summary>, (comma)</summary>
		public static Symbol CM;
		/// <summary>?</summary>
		public static Symbol QUEST;
		/// <summary>:</summary>
		public static Symbol COLON;
		/// <summary>!</summary>
		public static Symbol NOT;
		/// <summary>~</summary>
		public static Symbol COMPL;
		/// <summary>(</summary>
		public static Symbol LP;
		/// <summary>)</summary>
		public static Symbol RP;
		/// <summary>an integer</summary>
		public static Symbol INT;
		/// <summary>a identifier</summary>
		public static Symbol IDENT;
		/// <summary>an uninterpreted 'defined(x)' invocation</summary>
		public static Symbol DEFMAC;
		/// <summary>a float</summary>
		public static Symbol FLOAT;
		/// <summary>whitespace</summary>
		public static Symbol WHITE;
		/// <summary>a comment</summary>
		public static Symbol COMMENT;
		/// <summary>a double-quote enclosed string constant</summary>
		public static Symbol STRING;
		/// <summary>a single-quote enclosed char constant</summary>
		public static Symbol CHARS;

		void InitToken()
		{
			MUL = LookupLiteralSymbol("*");
			DIV = LookupLiteralSymbol("/");
			MOD = LookupLiteralSymbol("%");
			PLUS = LookupLiteralSymbol("+");
			MINUS = LookupLiteralSymbol("-");
			LS = LookupLiteralSymbol("<<");
			RS = LookupLiteralSymbol(">>");
			AND = LookupLiteralSymbol("&");
			OR = LookupLiteralSymbol("|");
			ER = LookupLiteralSymbol("^");
			LT = LookupLiteralSymbol("<");
			LE = LookupLiteralSymbol("<=");
			GT = LookupLiteralSymbol(">");
			GE = LookupLiteralSymbol(">=");
			EQ = LookupLiteralSymbol("==");
			NE = LookupLiteralSymbol("!=");
			ANDAND = LookupLiteralSymbol("&&");
			OROR = LookupLiteralSymbol("||");
			CM = LookupLiteralSymbol(",");
			QUEST = LookupLiteralSymbol("?");
			COLON = LookupLiteralSymbol(":");
			NOT = LookupLiteralSymbol("!");
			COMPL = LookupLiteralSymbol("~");
			LP = LookupLiteralSymbol("(");
			RP = LookupLiteralSymbol(")");
			INT = LookupSymbol("INT");
			FLOAT = LookupSymbol("FLOAT");
			IDENT = LookupSymbol("IDENT");
			WHITE = LookupSymbol("WHITE");
			COMMENT = LookupSymbol("COMMENT");
			STRING = LookupSymbol("STRING");
			CHARS = LookupSymbol("CHARS");
			DEFMAC = LookupSymbol("DEFMAC");
		}

		private static Symbol exp;
		private static Symbol e;
		private static Symbol term;

		void InitLhs()
		{
			exp = LookupSymbol("exp");
			e = LookupSymbol("e");
			term = LookupSymbol("term");
		}

		protected override void DeclareTokens()
		{
			if (MUL == null)
			{
				InitToken();
				InitLhs();
			}

			DeclareTokens(RP);
			DeclareTokens(INT);
			DeclareTokens(FLOAT);
			DeclareTokens(IDENT);
			DeclareTokens(WHITE);
			DeclareTokens(COMMENT);
			DeclareTokens(STRING);
			DeclareTokens(CHARS);
			DeclareTokens(DEFMAC);

			DeclareLefts(CM);
			DeclareRights(QUEST, COLON);
			DeclareLefts(OROR);
			DeclareLefts(ANDAND);
			DeclareLefts(OR);
			DeclareLefts(ER);
			DeclareLefts(AND);
			DeclareLefts(EQ, NE);
			DeclareLefts(LT, LE, GE, GT);
			DeclareLefts(LS, RS);
			DeclareLefts(PLUS, MINUS);
			DeclareLefts(MUL, DIV, MOD);
			DeclareRights(NOT, COMPL);
			DeclareLefts(LP);

			DeclareStart(exp);
		}

		List<ExpressionNode> nodepool = new List<ExpressionNode>();

		const byte IF_INIF = 1;	/* "in the 'if' clause rather than 'else'" */
		const byte IF_TRUE = 2;	/* "this if is currently true"		   */
		const byte IF_FALSE = 4;	/* "this if is currently false"		   */

		const int TRUE = 1;
		const int FALSE = 0;

		void binop(int n, IList<ExpressionNode> vals, ref ExpressionNode val)
		{
			val = vals[1];
			val.Add(vals[0]);
			val.Add(vals[2]);
		}

		void unop(int n, IList<ExpressionNode> vals, ref ExpressionNode val)
		{
			val = vals[0];
			val.Add(vals[1]);
		}

		protected override void DeclareGrammar()
		{
			Rules lhs;

			lhs = DeclareRule(exp);
			lhs.AddSymbols(e);
			lhs.AddAction(delegate(int n, IList<ExpressionNode> vals, ref ExpressionNode val)
			{
				val = vals[0];
			});
			lhs.EndRule();

			lhs = DeclareRule(e);
			lhs.AddSymbols(e, MUL, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, DIV, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, MOD, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, PLUS, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, MINUS, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, LS, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, RS, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, LT, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, GT, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, LE, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, GE, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, EQ, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, NE, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, AND, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, ER, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, OR, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, ANDAND, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, OROR, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(e, QUEST, e, COLON, e);
			lhs.AddAction(delegate(int n, IList<ExpressionNode> vals, ref ExpressionNode val)
			{
				val = vals[1];
				val.Add(vals[0]);
				val.Add(vals[2]);
				val.Add(vals[4]);
			});
			lhs.EndRule();
			lhs.AddSymbols(e, CM, e);
			lhs.AddAction(binop);
			lhs.EndRule();
			lhs.AddSymbols(term);
			lhs.AddAction(delegate(int n, IList<ExpressionNode> vals, ref ExpressionNode val)
			{
				val = vals[0];
			});
			lhs.EndRule();

			lhs = DeclareRule(term);
			lhs.AddSymbols(MINUS, term);
			lhs.AddAction(unop);
			lhs.EndRule();
			lhs.AddSymbols(NOT, term);
			lhs.AddAction(unop);
			lhs.EndRule();
			lhs.AddSymbols(COMPL, term);
			lhs.AddAction(unop);
			lhs.EndRule();
			lhs.AddSymbols(LP, e, RP);
			lhs.AddAction(delegate(int n, IList<ExpressionNode> vals, ref ExpressionNode val)
			{
				val = vals[1];
				val.Parenthesis = true;
			});
			lhs.EndRule();
			lhs.AddSymbols(INT);
			lhs.AddAction(delegate(int n, IList<ExpressionNode> vals, ref ExpressionNode val)
			{
				val = vals[0];
			});
			lhs.EndRule();
			lhs.AddSymbols(IDENT);
			lhs.AddAction(delegate(int n, IList<ExpressionNode> vals, ref ExpressionNode val)
			{
				val = vals[0];
			});
			lhs.EndRule();
			lhs.AddSymbols(DEFMAC, LP, IDENT, RP);
			lhs.AddAction(delegate(int n, IList<ExpressionNode> vals, ref ExpressionNode val)
			{
				val = vals[2];
				val.Defined = true;
			});
			lhs.EndRule();
			lhs.AddSymbols(DEFMAC, IDENT);
			lhs.AddAction(delegate(int n, IList<ExpressionNode> vals, ref ExpressionNode val)
			{
				val = vals[1];
				val.Defined = true;
			});
			lhs.EndRule();
		}
	}

	public class DebugAdapter : IYaccDebug
	{
		public void push(int state, object value)
		{
			System.Diagnostics.Debug.WriteLine("push\tstate " + state + "\tvalue " + value);
		}

		public void lex(int state, int token, string name, object value)
		{
			System.Diagnostics.Debug.WriteLine("lex\tstate " + state + "\treading " + name + "\tvalue " + value);
		}

		public void shift(int from, int to, int errorFlag)
		{
			switch (errorFlag)
			{
				default:				// normally
					System.Diagnostics.Debug.WriteLine("shift\tfrom state " + from + " to " + to);
					break;
				case 0:
				case 1:
				case 2:		// in error recovery
					System.Diagnostics.Debug.WriteLine("shift\tfrom state " + from + " to " + to
							 + "\t" + errorFlag + " left to recover");
					break;
				case 3:				// normally
					System.Diagnostics.Debug.WriteLine("shift\tfrom state " + from + " to " + to + "\ton error");
					break;
			}
		}

		public void pop(int state)
		{
			System.Diagnostics.Debug.WriteLine("pop\tstate " + state + "\ton error");
		}

		public void discard(int state, int token, string name, object value)
		{
			System.Diagnostics.Debug.WriteLine("discard\tstate " + state + "\ttoken " + name + "\tvalue " + value);
		}

		public void reduce(int from, int to, int rule, string text, int len)
		{
			System.Diagnostics.Debug.WriteLine("reduce\tstate " + from + "\tuncover " + to
				  + "\trule (" + rule + ") " + text);
		}

		public void shift(int from, int to)
		{
			System.Diagnostics.Debug.WriteLine("goto\tfrom state " + from + " to " + to);
		}

		public void accept(object value)
		{
			System.Diagnostics.Debug.WriteLine("accept\tvalue " + value);
		}

		public void error(string message)
		{
			System.Diagnostics.Debug.WriteLine("error\t" + message);
		}

		public void reject()
		{
			System.Diagnostics.Debug.WriteLine("reject");
		}
	}
}
