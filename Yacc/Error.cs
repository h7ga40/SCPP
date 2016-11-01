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
using System.Diagnostics;
using System.IO;

namespace Yacc
{
	public class Error
	{
#if ! lint
		static readonly string sccsid = "@(#)error.c	5.3 (Berkeley) 6/1/90";
#endif // not lint

		private string m_MyName = "yacc";
		private string m_InputFileName = "";
		private int m_LineNo;
		private YaccErrors m_Errors = new YaccErrors();

		public Error()
		{
		}

		public string MyName
		{
			get { return m_MyName; }
			set { m_MyName = value; }
		}

		public string InputFileName
		{
			get { return m_InputFileName; }
			set { m_InputFileName = value; }
		}

		public int LineNo
		{
			get { return m_LineNo; }
			set { m_LineNo = value; }
		}

		public YaccErrors Errors
		{
			get { return m_Errors; }
		}

		/* routines for printing error messages  */

		public void Fatal(string msg)
		{
			string text = String.Format("{0}: f - {1}\n", m_MyName, msg);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void UnexpectedEOF()
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", unexpected end-of-file\n",
				m_MyName, m_LineNo, m_InputFileName);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		private string PrintPos(string st_line, int st_cptr)
		{
			StringBuilder result = new StringBuilder();
			int s;

			if (st_line == null) return "";
			for (s = 0; st_line[s] != '\n'; ++s)
			{
				if (IsPrint(st_line[s]) || st_line[s] == '\t')
					result.Append(st_line[s]);
				else
					result.Append('?');
			}
			result.Append('\n');
			for (s = 0; s < st_cptr; ++s)
			{
				if (st_line[s] == '\t')
					result.Append('\t');
				else
					result.Append(' ');
			}
			result.Append('^');
			result.Append('\n');

			return result.ToString();
		}

		private bool IsPrint(char p)
		{
			return p >= 0x20;
		}

		public void SyntaxError(int st_lineno, string st_line, int st_cptr)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", syntax error\n",
							m_MyName, st_lineno, m_InputFileName) + PrintPos(st_line, st_cptr);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void UnterminatedComment(int c_lineno, string c_line, int c_cptr)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", unmatched /*\n",
				m_MyName, c_lineno, m_InputFileName) + PrintPos(c_line, c_cptr);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void UnterminatedString(int s_lineno, string s_line, int s_cptr)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", unterminated string\n",
				m_MyName, s_lineno, m_InputFileName) + PrintPos(s_line, s_cptr);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void UnterminatedText(int t_lineno, string t_line, int t_cptr)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", unmatched %{{\n",
				m_MyName, t_lineno, m_InputFileName) + PrintPos(t_line, t_cptr);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void IllegalTag(int t_lineno, string t_line, int t_cptr)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", illegal tag\n",
				m_MyName, t_lineno, m_InputFileName) + PrintPos(t_line, t_cptr);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void IllegalCharacter(string line, int c_cptr)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", illegal character\n",
				m_MyName, m_LineNo, m_InputFileName) + PrintPos(line, c_cptr);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void UsedReserved(string s)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", illegal use of reserved symbol "
				+ "{3}\n", m_MyName, m_LineNo, m_InputFileName, s);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void TokenizedStart(string s)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", the start symbol {3} cannot be "
			   + "declared to be a token\n", m_MyName, m_LineNo, m_InputFileName, s);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void RetypedWarning(string s)
		{
			m_Errors.Add(String.Format("{0}: w - line {1} of \"{2}\", the type of {3} has been "
				+ "redeclared\n", m_MyName, m_LineNo, m_InputFileName, s));
		}

		public void ReprecWarning(string s)
		{
			m_Errors.Add(String.Format("{0}: w - line {1} of \"{2}\", the precedence of {3} has been "
				+ "redeclared\n", m_MyName, m_LineNo, m_InputFileName, s));
		}

		public void RevaluedWarning(string s)
		{
			m_Errors.Add(String.Format("{0}: w - line {1} of \"{2}\", the value of {3} has been "
				+ "redeclared\n", m_MyName, m_LineNo, m_InputFileName, s));
		}

		public void TerminalStart(string s)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", the start symbol {3} is a "
				+ "token\n", m_MyName, m_LineNo, m_InputFileName, s);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void RestartedWarning()
		{
			m_Errors.Add(String.Format("{0}: w - line {1} of \"{2}\", the start symbol has been "
				+ "redeclared\n", m_MyName, m_LineNo, m_InputFileName));
		}

		public void NoGrammar()
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", no grammar has been "
				+ "specified\n", m_MyName, m_LineNo, m_InputFileName);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void TerminalLhs(int s_lineno)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", a token appears on the lhs "
				+ "of a production\n", m_MyName, s_lineno, m_InputFileName);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void PrecRedeclared()
		{
			m_Errors.Add(String.Format("{0}: w - line {1} of  \"{2}\", conflicting %prec "
				+ "specifiers\n", m_MyName, m_LineNo, m_InputFileName));
		}

		public void UnterminatedAction(int a_lineno, string a_line, int a_cptr)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", unterminated action\n",
				m_MyName, a_lineno, m_InputFileName) + PrintPos(a_line, a_cptr);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void DollarWarning(int a_lineno, int i)
		{
			m_Errors.Add(String.Format("{0}: w - line {1} of \"{2}\", ${3} references beyond the "
				+ "end of the current rule\n", m_MyName, a_lineno, m_InputFileName, i));
		}

		public void DollarError(int a_lineno, string a_line, int a_cptr)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", illegal $-name\n",
				m_MyName, a_lineno, m_InputFileName) + PrintPos(a_line, a_cptr);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void UntypedLhs()
		{
			m_Errors.Add(String.Format("{0}: w - line {1} of \"{2}\", $$ is untyped\n",
				m_MyName, m_LineNo, m_InputFileName));
			/** Main.done(1); */
		}

		public void UntypedRhs(int i, string s)
		{
			m_Errors.Add(String.Format("{0}: w - line {1} of \"{2}\", ${3} ({4}) is untyped\n",
				m_MyName, m_LineNo, m_InputFileName, i, s));
			/** Main.done(1); */
		}

		public void UnknownRhs(int i)
		{
			string text = String.Format("{0}: e - line {1} of \"{2}\", ${3} is untyped\n",
				m_MyName, m_LineNo, m_InputFileName, i);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void DefaultActionWarning()
		{
			m_Errors.Add(String.Format("{0}: w - line {1} of \"{2}\", the default action assigns an "
				+ "undefined value to $$\n", m_MyName, m_LineNo, m_InputFileName));
		}

		public void UndefinedGoal(string s)
		{
			string text = String.Format("{0}: e - the start symbol {1} is undefined\n", m_MyName, s);
			m_Errors.Add(text);
			throw new YaccException(text);
		}

		public void UndefinedSymbolWarning(string s)
		{
			m_Errors.Add(String.Format("{0}: w - the symbol {1} is undefined\n", m_MyName, s));
		}
	}

	public class YaccErrors
	{
		List<string> m_List = new List<string>();

		public YaccErrors()
		{
		}

		public void Add(string text)
		{
			m_List.Add(text);
		}

		public void Write(TextWriter Out)
		{
			foreach (string text in m_List)
				Out.Write(text);
		}
	}

	public class YaccException : Exception
	{
		public YaccException(string message)
			: base(message)
		{
		}
	}
}
