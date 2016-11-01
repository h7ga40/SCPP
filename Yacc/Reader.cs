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
	public class Reader : Grammar<string>
	{
#if ! lint
		static readonly string sccsid = "@(#)reader.c	5.7 (Berkeley) 1/20/91";
#endif // not lint

		/*  The line size must be a positive integer.  One hundred was chosen	*/
		/*  because few lines in Yacc input grammars exceed 100 characters.	*/
		/*  Note that if a line exceeds LINESIZE characters, the line buffer	*/
		/*  will be expanded to accomodate it.					*/

		private List<string> m_TagTable = new List<string>();

		private int m_ColumnNo;
		private string m_Line;

		private TextReader m_InputFile;	/*  the input file				    */
		private Output m_Output;
		private Rules m_CurrentRule;

		public Reader(Yacc<string> yacc, Output output)
			: base(yacc)
		{
			m_Output = output;
		}

		private static bool IsOctal(int c) { return ((c) >= '0' && (c) <= '7'); }

		public TextReader InputFile
		{
			get { return m_InputFile; }
			set { m_InputFile = value; }
		}

		void GetLine()
		{
			string temp = m_InputFile.ReadLine();
			if (temp != null)
				m_Line = temp + "\n";
			m_ColumnNo = 0;
			++Error.LineNo;
		}

		string DupLine()
		{
			int s;

			if (m_Line == null)
				return null;

			s = m_Line.IndexOf('\n');
			if (s < 0)
				return null;

			return m_Line.Substring(0, s + 1);
		}

		void SkipComment()
		{
			int s;
			int st_lineno = Error.LineNo;
			string st_line = DupLine();
			int st_cptr = m_ColumnNo;

			s = m_ColumnNo + 2;
			for (; ; )
			{
				if (m_Line[s] == '*' && m_Line[s + 1] == '/')
				{
					m_ColumnNo = s + 2;
					return;
				}
				if (m_Line[s] == '\n')
				{
					GetLine();
					if (m_Line == null)
						Error.UnterminatedComment(st_lineno, st_line, st_cptr);
					s = m_ColumnNo;
				}
				else
					++s;
			}
		}

		int Nextc()
		{
			int s;

			if (m_Line == null)
			{
				GetLine();
				if (m_Line == null)
					return Defs.EOF;
			}

			s = m_ColumnNo;
			for (; ; )
			{
				switch (m_Line[s])
				{
					case '\n':
						GetLine();
						if (m_Line == null) return Defs.EOF;
						s = m_ColumnNo;
						break;

					case ' ':
					case '\t':
					case '\f':
					case '\r':
					case '\v':
					case ',':
					case ';':
						++s;
						break;

					case '\\':
						m_ColumnNo = s;
						return '%';

					case '/':
						if (m_Line[s + 1] == '*')
						{
							m_ColumnNo = s;
							SkipComment();
							s = m_ColumnNo;
							break;
						}
						else if (m_Line[s + 1] == '/')
						{
							GetLine();
							if (m_Line == null) return Defs.EOF;
							s = m_ColumnNo;
							break;
						}
						goto default;/* fall through */

					default:
						m_ColumnNo = s;
						return m_Line[s];
				}
			}
		}

		KeywordCode Keyword()
		{
			char c;
			int t_cptr = m_ColumnNo;

			c = m_Line[++m_ColumnNo];
			if (Char.IsLetter(c))
			{
				StringBuilder cache = new StringBuilder();
				for (; ; )
				{
					if (Char.IsLetter(c))
					{
						if (Char.IsUpper(c)) c = Char.ToLower(c);
						cache.Append((char)c);
					}
					else if (Char.IsDigit(c) || c == '_' || c == '.' || c == '$')
						cache.Append((char)c);
					else
						break;
					c = m_Line[++m_ColumnNo];
				}

				if (String.Compare(cache.ToString(), "token") == 0 || String.Compare(cache.ToString(), "term") == 0)
					return KeywordCode.TOKEN;
				if (String.Compare(cache.ToString(), "type") == 0)
					return KeywordCode.TYPE;
				if (String.Compare(cache.ToString(), "left") == 0)
					return KeywordCode.LEFT;
				if (String.Compare(cache.ToString(), "right") == 0)
					return KeywordCode.RIGHT;
				if (String.Compare(cache.ToString(), "nonassoc") == 0 || String.Compare(cache.ToString(), "binary") == 0)
					return KeywordCode.NONASSOC;
				if (String.Compare(cache.ToString(), "start") == 0)
					return KeywordCode.START;
			}
			else
			{
				m_ColumnNo++;
				if (c == '{')
					return KeywordCode.TEXT;
				if (c == '%' || c == '\\')
					return KeywordCode.MARK;
				if (c == '<')
					return KeywordCode.LEFT;
				if (c == '>')
					return KeywordCode.RIGHT;
				if (c == '0')
					return KeywordCode.TOKEN;
				if (c == '2')
					return KeywordCode.NONASSOC;
			}
			Error.SyntaxError(Error.LineNo, m_Line, t_cptr);
			/*NOTREACHED*/
			return (KeywordCode)(-1);
		}

		void CopyText(TextWriter f)
		{
			int c;
			int quote;
			bool need_newline = false;
			int t_lineno = Error.LineNo;
			string t_line = DupLine();
			int t_cptr = m_ColumnNo - 2;

			if (m_Line[m_ColumnNo] == '\n')
			{
				GetLine();
				if (m_Line == null)
					Error.UnterminatedText(t_lineno, t_line, t_cptr);
			}
			f.Write(m_Output.LineFormat, Error.LineNo, Error.InputFileName);

		loop:
			c = m_Line[m_ColumnNo++];
			switch (c)
			{
				case '\n':
				next_line:
					f.Write('\n');
				need_newline = false;
				GetLine();
				if (m_Line != null) goto loop;
				Error.UnterminatedText(t_lineno, t_line, t_cptr);
				break;
				case '\'':
				case '"':
				{
					int s_lineno = Error.LineNo;
					string s_line = DupLine();
					int s_cptr = m_ColumnNo - 1;

					quote = c;
					f.Write((char)c);
					for (; ; )
					{
						c = m_Line[m_ColumnNo++];
						f.Write((char)c);
						if (c == quote)
						{
							need_newline = true;
							goto loop;
						}
						if (c == '\n')
							Error.UnterminatedString(s_lineno, s_line, s_cptr);
						if (c == '\\')
						{
							c = m_Line[m_ColumnNo++];
							f.Write((char)c);
							if (c == '\n')
							{
								GetLine();
								if (m_Line == null)
									Error.UnterminatedString(s_lineno, s_line, s_cptr);
							}
						}
					}
				}
				break;
				case '/':
				f.Write((char)c);
				need_newline = true;
				c = m_Line[m_ColumnNo];
				if (c == '/')
				{
					do f.Write((char)c); while ((c = m_Line[++m_ColumnNo]) != '\n');
					goto next_line;
				}
				if (c == '*')
				{
					int c_lineno = Error.LineNo;
					string c_line = DupLine();
					int c_cptr = m_ColumnNo - 1;

					f.Write('*');
					m_ColumnNo++;
					for (; ; )
					{
						c = m_Line[m_ColumnNo++];
						f.Write((char)c);
						if (c == '*' && m_Line[m_ColumnNo] == '/')
						{
							f.Write('/');
							m_ColumnNo++;
							goto loop;
						}
						if (c == '\n')
						{
							GetLine();
							if (m_Line == null)
								Error.UnterminatedComment(c_lineno, c_line, c_cptr);
						}
					}
				}
				need_newline = true;
				goto loop;

				case '%':
				case '\\':
				if (m_Line[m_ColumnNo] == '}')
				{
					if (need_newline) f.Write('\n');
					m_ColumnNo++;
					return;
				}
				goto default;	/* fall through */

				default:
				f.Write((char)c);
				need_newline = true;
				goto loop;
			}
		}

		int HexVal(int c)
		{
			if (c >= '0' && c <= '9')
				return c - '0';
			if (c >= 'A' && c <= 'F')
				return c - 'A' + 10;
			if (c >= 'a' && c <= 'f')
				return c - 'a' + 10;
			return -1;
		}

		Symbol GetLiteral()
		{
			char c, quote;
			int i;
			int n;
			Symbol bp;
			int s_lineno = Error.LineNo;
			string s_line = DupLine();
			int s_cptr = m_ColumnNo;

			quote = m_Line[m_ColumnNo++];
			StringBuilder cache = new StringBuilder();
			for (; ; )
			{
				c = m_Line[m_ColumnNo++];
				if (c == quote) break;
				if (c == '\n') Error.UnterminatedString(s_lineno, s_line, s_cptr);
				if (c == '\\')
				{
					int c_cptr = m_ColumnNo - 1;

					c = m_Line[m_ColumnNo++];
					switch (c)
					{
						case '\n':
							GetLine();
							if (m_Line == null) Error.UnterminatedString(s_lineno, s_line, s_cptr);
							continue;

						case '0':
						case '1':
						case '2':
						case '3':
						case '4':
						case '5':
						case '6':
						case '7':
							n = c - '0';
							c = m_Line[m_ColumnNo];
							if (IsOctal(c))
							{
								n = (n << 3) + (c - '0');
								c = m_Line[++m_ColumnNo];
								if (IsOctal(c))
								{
									n = (n << 3) + (c - '0');
									m_ColumnNo++;
								}
							}
							if (n > Defs.MAXCHAR) Error.IllegalCharacter(m_Line, c_cptr);
							c = (char)n;
							break;

						case 'x':
							c = m_Line[m_ColumnNo++];
							n = HexVal(c);
							if (n < 0 || n >= 16)
								Error.IllegalCharacter(m_Line, c_cptr);
							for (; ; )
							{
								c = m_Line[m_ColumnNo];
								i = HexVal(c);
								if (i < 0 || i >= 16) break;
								m_ColumnNo++;
								n = (n << 4) + i;
								if (n > Defs.MAXCHAR) Error.IllegalCharacter(m_Line, c_cptr);
							}
							c = (char)n;
							break;

						case 'a': c = '\x7'; break;
						case 'b': c = '\b'; break;
						case 'f': c = '\f'; break;
						case 'n': c = '\n'; break;
						case 'r': c = '\r'; break;
						case 't': c = '\t'; break;
						case 'v': c = '\v'; break;
					}
				}
				cache.Append((char)c);
			}

			bp = LookupLiteralSymbol(cache.ToString());

			return bp;
		}

		Symbol GetName()
		{
			int c;

			StringBuilder cache = new StringBuilder();
			for (c = m_Line[m_ColumnNo]; IsIdent(c); c = m_Line[++m_ColumnNo])
				cache.Append((char)c);

			if (IsReserved(cache.ToString())) Error.UsedReserved(cache.ToString());

			return LookupSymbol(cache.ToString());
		}


		int GetNumber()
		{
			char c;
			int n;

			n = 0;
			for (c = m_Line[m_ColumnNo]; Char.IsDigit(c); c = m_Line[++m_ColumnNo])
				n = 10 * n + (c - '0');

			return n;
		}

		/** ats:
		    maintains tag_table with contents of < tag >.
		    extended to allow nested <> on the same line for use with Java/C# generics.
		  */
		string GetTag(bool emptyOk)
		{
			int c;
			int i;
			string s;
			int t_lineno = Error.LineNo;
			string t_line = DupLine();
			int t_cptr = m_ColumnNo;

			m_ColumnNo++;
			c = Nextc();
			if (c == Defs.EOF) Error.UnexpectedEOF();
			if (emptyOk && c == '>')
			{
				m_ColumnNo++; return null;	// 0 indicates empty tag if emptyOk
			}
			if (!Char.IsLetter((char)c) && c != '_' && c != '$') // << or <. are not allowed
				Error.IllegalTag(t_lineno, t_line, t_cptr);

			StringBuilder cache = new StringBuilder();

			/** ats: was
			do { cachec(c); c = line[++cptr]; } while (Defs.IS_IDENT(c)); */

			for (i = 0; ; )
			{ // count <> nests
				cache.Append((char)c);
				switch (c = m_Line[++m_ColumnNo])
				{
					case '<': // nest
						++i;
						continue;
					case '>': // unnest or exit loop
						if (--i < 0) break;
						continue;
					case ' ':
					case '?':
					case '[':
					case ']':
					case ',': // extra characters for generics
						if (i == 0) break; // but not at outer level
						continue;
					default: // alnum and . _ $ are ok.
						if (!IsIdent(c)) break;
						continue;
				}
				break;
			}

			c = Nextc();
			if (c == Defs.EOF) Error.UnexpectedEOF();
			if (c != '>')
				Error.IllegalTag(t_lineno, t_line, t_cptr);
			m_ColumnNo++;

			for (i = 0; i < m_TagTable.Count; ++i)
			{
				if (String.Compare(cache.ToString(), m_TagTable[i]) == 0)
					return m_TagTable[i];
			}

			s = cache.ToString();
			m_TagTable.Add(s);
			return s;
		}

		void DeclareTokens(KeywordCode assoc)
		{
			int c;
			Symbol bp;
			int value;
			string tag = null;
			List<Symbol> symbols = new List<Symbol>();
			List<int> values = new List<int>();

			c = Nextc();
			if (c == Defs.EOF) Error.UnexpectedEOF();
			if (c == '<')
			{
				tag = GetTag(false);
				c = Nextc();
				if (c == Defs.EOF) Error.UnexpectedEOF();
			}

			for (; ; )
			{
				if (Char.IsLetter((char)c) || c == '_' || c == '.' || c == '$')
					bp = GetName();
				else if (c == '\'' || c == '"')
					bp = GetLiteral();
				else
					break;

				c = Nextc();
				if (c == Defs.EOF) Error.UnexpectedEOF();
				value = bp.Value;
				if (Char.IsDigit((char)c))
				{
					value = GetNumber();
					c = Nextc();
					if (c == Defs.EOF) Error.UnexpectedEOF();
				}

				symbols.Add(bp);
				values.Add(value);
			}

			DeclareTokens(assoc, tag, symbols.ToArray(), values.ToArray());
		}

		void DeclareTypes()
		{
			int c;
			Symbol bp;
			string tag;
			List<Symbol> symbols = new List<Symbol>();

			c = Nextc();
			if (c == Defs.EOF) Error.UnexpectedEOF();
			if (c != '<') Error.SyntaxError(Error.LineNo, m_Line, m_ColumnNo);
			tag = GetTag(false);

			for (; ; )
			{
				c = Nextc();
				if (Char.IsLetter((char)c) || c == '_' || c == '.' || c == '$')
					bp = GetName();
				else if (c == '\'' || c == '"')
					bp = GetLiteral();
				else
					break;

				symbols.Add(bp);
			}

			DeclareTypes(tag, symbols.ToArray());
		}

		void DeclareStart()
		{
			int c;
			Symbol bp;

			c = Nextc();
			if (c == Defs.EOF) Error.UnexpectedEOF();
			if (!Char.IsLetter((char)c) && c != '_' && c != '.' && c != '$')
				Error.SyntaxError(Error.LineNo, m_Line, m_ColumnNo);
			bp = GetName();
			DeclareStart(bp);
		}

		protected override void DeclareTokens()
		{
			int c;
			KeywordCode k;

			StringBuilder cache = new StringBuilder();

			for (; ; )
			{
				c = Nextc();
				if (c == Defs.EOF) Error.UnexpectedEOF();
				if (c != '%') Error.SyntaxError(Error.LineNo, m_Line, m_ColumnNo);
				switch (k = Keyword())
				{
					case KeywordCode.MARK:
						return;

					case KeywordCode.TEXT:
						CopyText(m_Output.PrologWriter);
						break;

					case KeywordCode.TOKEN:
					case KeywordCode.LEFT:
					case KeywordCode.RIGHT:
					case KeywordCode.NONASSOC:
						DeclareTokens(k);
						break;

					case KeywordCode.TYPE:
						DeclareTypes();
						break;

					case KeywordCode.START:
						DeclareStart();
						break;
				}
			}
		}

		void AdvanceToStart()
		{
			int c;
			Symbol bp;
			int s_cptr;
			int s_lineno;

			for (; ; )
			{
				c = Nextc();
				if (c != '%') break;
				s_cptr = m_ColumnNo;
				switch (Keyword())
				{
					case KeywordCode.MARK:
						Error.NoGrammar();
						break;

					case KeywordCode.TEXT:
						CopyText(m_Output.LocalWriter);
						break;

					case KeywordCode.START:
						DeclareStart();
						break;

					default:
						Error.SyntaxError(Error.LineNo, m_Line, s_cptr);
						break;
				}
			}

			c = Nextc();
			if (!Char.IsLetter((char)c) && c != '_' && c != '.' && c != '_')
				Error.SyntaxError(Error.LineNo, m_Line, m_ColumnNo);
			bp = GetName();
			s_lineno = Error.LineNo;
			c = Nextc();
			if (c == Defs.EOF) Error.UnexpectedEOF();
			if (c != ':') Error.SyntaxError(Error.LineNo, m_Line, m_ColumnNo);
			m_CurrentRule = DeclareRule(bp);
			if (m_CurrentRule.Error)
				Error.TerminalLhs(s_lineno);
			m_ColumnNo++;
		}

		void AddSymbol()
		{
			int c;
			Symbol bp;
			int s_lineno = Error.LineNo;

			c = m_Line[m_ColumnNo];
			if (c == '\'' || c == '"')
				bp = GetLiteral();
			else
				bp = GetName();

			c = Nextc();
			if (c == ':')
			{
				m_CurrentRule.EndRule();
				m_CurrentRule = DeclareRule(bp);
				m_ColumnNo++;
				return;
			}

			m_CurrentRule.AddSymbol(bp);
		}

		void CopyAction()
		{
			int c;
			int i, n;
			int depth;
			int quote;
			string tag;
			int a_lineno = Error.LineNo;
			string a_line = DupLine();
			int a_cptr = m_ColumnNo;

			StringBuilder f = new StringBuilder();
#if !OUTPUT_CODE
			f.AppendFormat("case {0}:\n", Yacc.m_Rules.Count - 2);
			f.AppendFormat(m_Output.LineFormat, Error.LineNo, Error.InputFileName);
#endif
			f.Append(' '); f.Append(' ');
			if (m_Line[m_ColumnNo] == '=') m_ColumnNo++;

			n = 0;
			for (i = Items.Count - 1; Items[i] != null; --i) ++n;

			depth = 0;
		loop:
			c = m_Line[m_ColumnNo];
			if (c == '$')
			{
				if (m_Line[m_ColumnNo + 1] == '<')
				{
					int d_lineno = Error.LineNo;
					string d_line = DupLine();
					int d_cptr = m_ColumnNo;

					m_ColumnNo++;
					tag = GetTag(true);
					c = m_Line[m_ColumnNo];
					if (c == '$')
					{
						if (tag != null && String.Compare(tag, "Object") != 0)
#if !OUTPUT_CODE
							f.AppendFormat("(({0})yyVal)", tag);
						else f.Append("yyVal");
#else
							f.AppendFormat("(({0})val)", tag);
						else f.Append("val");
#endif
						m_ColumnNo++;
						goto loop;
					}
					else if (Char.IsDigit((char)c))
					{
						i = GetNumber();
						if (i > n) Error.DollarWarning(d_lineno, i);
						if (tag != null && String.Compare(tag, "Object") != 0)
#if !OUTPUT_CODE
							f.AppendFormat("(({0})yyVals[{1}+yyTop])", tag, i - n);
						else f.AppendFormat("yyVals[{0}+yyTop]", i - n);
#else
							f.AppendFormat("(({0})vals[{1}])", tag, i - 1/* - n*/);
						else f.AppendFormat("vals[{0}]", i - 1/* - n*/);
#endif
						goto loop;
					}
					else if (c == '-' && Char.IsDigit(m_Line[m_ColumnNo + 1]))
					{
						m_ColumnNo++;
#if !OUTPUT_CODE
						i = -GetNumber() - n;
						if (tag != null && String.Compare(tag, "Object") != 0)
							f.AppendFormat("(({0})yyVals[{1}+yyTop])", tag, i);
						else f.AppendFormat("yyVals[{0}+yyTop]", i); // etienne.cochard@ciel.com 3/5/04
#else
						i = -GetNumber() - 1/* - n*/;
						if (tag != null && String.Compare(tag, "Object") != 0)
							f.AppendFormat("(({0})vals[{1}])", tag, i);
						else f.AppendFormat("vals[{0}]", i); // etienne.cochard@ciel.com 3/5/04
#endif
						goto loop;
					}
					else
						Error.DollarError(d_lineno, d_line, d_cptr);
				}
				else if (m_Line[m_ColumnNo + 1] == '$')
				{
					if (m_TagTable.Count != 0 && m_CurrentRule.Rule.Lhs.Tag == null)
						Error.UntypedLhs();
#if !OUTPUT_CODE
					f.Append("yyVal");
#else
					f.Append("val");
#endif
					m_ColumnNo += 2;
					goto loop;
				}
				else if (Char.IsDigit(m_Line[m_ColumnNo + 1]))
				{
					m_ColumnNo++;
					i = GetNumber();
					if (m_TagTable.Count != 0)
					{
						if (i <= 0 || i > n)
							Error.UnknownRhs(i);
						tag = Items[Items.Count + i - n - 1].Tag;
						if (tag == null)
						{
							Error.UntypedRhs(i, Items[Items.Count + i - n - 1].Name);
#if !OUTPUT_CODE
							f.AppendFormat("yyVals[{0}+yyTop]", i - n);
#else
							f.AppendFormat("vals[{0}]", i - 1/* - n*/);
#endif
						}
						else if (String.Compare(tag, "Object") != 0)
#if !OUTPUT_CODE
							f.AppendFormat("(({0})yyVals[{1}+yyTop])", tag, i - n);
#else
							f.AppendFormat("(({0})vals[{1}])", tag, i - 1/* - n*/);
#endif
						else
#if !OUTPUT_CODE
							f.AppendFormat("yyVals[{0}+yyTop]", i - n);
#else
							f.AppendFormat("vals[{0}]", i - 1/* - n*/);
#endif
					}
					else
					{
						if (i > n)
							Error.DollarWarning(Error.LineNo, i);
#if !OUTPUT_CODE
						f.AppendFormat("yyVals[{0}+yyTop]", i - n);
#else
						f.AppendFormat("vals[{0}]", i - 1/* - n*/);
#endif
					}
					goto loop;
				}
				else if (m_Line[m_ColumnNo + 1] == '-')
				{
					m_ColumnNo += 2;
					i = GetNumber();
					if (m_TagTable.Count != 0)
						Error.UnknownRhs(-i);
#if !OUTPUT_CODE
					f.AppendFormat("yyVals[{0}+yyTop]", -i - n);
#else
					f.AppendFormat("vals[{0}]", -i - 1/* - n*/);
#endif
					goto loop;
				}
			}
			if (Char.IsLetter((char)c) || c == '_' || c == '$')
			{
				do
				{
					f.Append((char)c);
					c = m_Line[++m_ColumnNo];
				} while (Char.IsLetterOrDigit((char)c) || c == '_' || c == '$');
				goto loop;
			}
			f.Append((char)c);
			m_ColumnNo++;
			switch (c)
			{
				case '\n':
				next_line:
					GetLine();
				if (m_Line != null) goto loop;
				Error.UnterminatedAction(a_lineno, a_line, a_cptr);
				break;

				case ';':
				if (depth > 0) goto loop;
#if !OUTPUT_CODE
				f.Append("\nbreak;\n");
#endif
				goto epilog;

				case '{':
				++depth;
				goto loop;

				case '}':
				if (--depth > 0) goto loop;
#if !OUTPUT_CODE
				f.Append("\n  break;\n");
#endif
				goto epilog;

				case '\'':
				case '"':
				{
					int s_lineno = Error.LineNo;
					string s_line = DupLine();
					int s_cptr = m_ColumnNo - 1;

					quote = c;
					for (; ; )
					{
						c = m_Line[m_ColumnNo++];
						f.Append((char)c);
						if (c == quote)
						{
							goto loop;
						}
						if (c == '\n')
							Error.UnterminatedString(s_lineno, s_line, s_cptr);
						if (c == '\\')
						{
							c = m_Line[m_ColumnNo++];
							f.Append((char)c);
							if (c == '\n')
							{
								GetLine();
								if (m_Line == null)
									Error.UnterminatedString(s_lineno, s_line, s_cptr);
							}
						}
					}
				}

				case '/':
				c = m_Line[m_ColumnNo];
				if (c == '/')
				{
					f.Append('*');
					while ((c = m_Line[++m_ColumnNo]) != '\n')
					{
						if (c == '*' && m_Line[m_ColumnNo + 1] == '/')
							f.Append("* ");
						else
							f.Append((char)c);
					}
					f.Append("*/\n");
					goto next_line;
				}
				if (c == '*')
				{
					int c_lineno = Error.LineNo;
					string c_line = DupLine();
					int c_cptr = m_ColumnNo - 1;

					f.Append('*');
					m_ColumnNo++;
					for (; ; )
					{
						c = m_Line[m_ColumnNo++];
						f.Append((char)c);
						if (c == '*' && m_Line[m_ColumnNo] == '/')
						{
							f.Append('/');
							m_ColumnNo++;
							goto loop;
						}
						if (c == '\n')
						{
							GetLine();
							if (m_Line == null)
								Error.UnterminatedComment(c_lineno, c_line, c_cptr);
						}
					}
				}
				goto loop;

				default:
				goto loop;
			}

		epilog:
			m_CurrentRule.AddAction(f.ToString());
		}

		bool MarkSymbol()
		{
			int c;
			Symbol bp;

			c = m_Line[m_ColumnNo + 1];
			if (c == '%' || c == '\\')
			{
				m_ColumnNo += 2;
				return true;
			}

			if (c == '=')
				m_ColumnNo += 2;
			else if ((c == 'p' || c == 'P') &&
				 ((c = m_Line[m_ColumnNo + 2]) == 'r' || c == 'R') &&
				 ((c = m_Line[m_ColumnNo + 3]) == 'e' || c == 'E') &&
				 ((c = m_Line[m_ColumnNo + 4]) == 'c' || c == 'C'))
			{
				if (!IsIdent(c = m_Line[m_ColumnNo + 5]))
					m_ColumnNo += 5;
			}
			else
				Error.SyntaxError(Error.LineNo, m_Line, m_ColumnNo);

			c = Nextc();
			if (Char.IsLetter((char)c) || c == '_' || c == '.' || c == '$')
				bp = GetName();
			else if (c == '\'' || c == '"')
				bp = GetLiteral();
			else
			{
				Error.SyntaxError(Error.LineNo, m_Line, m_ColumnNo);
				/*NOTREACHED*/
				bp = null;
			}

			m_CurrentRule.MarkSymbol(bp);

			return false;
		}

		protected override void DeclareGrammar()
		{
			int c;

			AdvanceToStart();

			for (; ; )
			{
				c = Nextc();
				if (c == Defs.EOF) break;
				if (Char.IsLetter((char)c) || c == '_' || c == '.' || c == '$' || c == '\'' ||
					c == '"')
					AddSymbol();
				else if (c == '{' || c == '=')
					CopyAction();
				else if (c == '|')
				{
					m_CurrentRule.EndRule();
					m_CurrentRule.StartRule();
					m_ColumnNo++;
				}
				else if (c == '%')
				{
					if (MarkSymbol()) break;
				}
				else
					Error.SyntaxError(Error.LineNo, m_Line, m_ColumnNo);
			}
			m_CurrentRule.EndRule();

			CopyEpilog(m_Output.EpilogWriter);
		}

		void CopyEpilog(TextWriter f)
		{
			int c, last;
			TextReader In;

			if (m_Line == null)
				return;

			In = m_InputFile;
			c = m_Line[m_ColumnNo];
			if (c == '\n')
			{
				++Error.LineNo;
				if ((c = In.Read()) == Defs.EOF)
					return;
				f.Write(m_Output.LineFormat, Error.LineNo, Error.InputFileName);
				f.Write((char)c);
				last = c;
			}
			else
			{
				f.Write(m_Output.LineFormat, Error.LineNo, Error.InputFileName);
				do { f.Write((char)c); } while ((c = m_Line[++m_ColumnNo]) != '\n');
				f.Write("\n      ");
				last = '\n';
			}

			while ((c = In.Read()) != Defs.EOF)
			{
				f.Write((char)c);
				last = c;
			}

			if (last != '\n')
			{
				f.Write("\n      ");
			}
		}
	}
}
