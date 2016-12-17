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
	public class Output
	{
#if !lint
		static readonly string sccsid = "@(#)output.c	5.7 (Berkeley) 5/24/93";
#endif // not lint
		private TextWriter m_Out;
		private TextReader m_TemplateReader; /* skeleton */
		private string m_LineFormat = "\t\t\t\t\t// line {0} \"{1}\"\n";

		private int m_Outline;

		private Stream m_PrologStream;  /*  temp files, used to save text until all	    */
		internal TextWriter PrologWriter;
		private Stream m_LocalStream;   /*  symbols have been defined			    */
		internal TextWriter LocalWriter;
		private Stream m_EpilogStream;
		internal TextWriter EpilogWriter;

		public TextWriter VerboseWriter;

		private Yacc<string> m_Yacc;
		private Error m_Error;
		private string token_enum = "Token";

		private ReadOnlyCollection<short> m_Lhs;
		private ReadOnlyCollection<short> m_Len;
		private ReadOnlyCollection<short> m_DefRed;
		private ReadOnlyCollection<short> m_Dgoto;
		private ReadOnlyCollection<short> m_Sindex;
		private ReadOnlyCollection<short> m_Rindex;
		private ReadOnlyCollection<short> m_Gindex;
		private ReadOnlyCollection<short> m_Table;
		private ReadOnlyCollection<short> m_Check;
		private ReadOnlyCollection<string> m_Names;
		private ReadOnlyCollection<string> m_Rule;

		public Output(Yacc<string> yacc)
		{
			this.m_Yacc = yacc;
			m_Error = yacc.Error;
			m_PrologStream = new MemoryStream();
			PrologWriter = new StreamWriter(m_PrologStream);
			m_LocalStream = new MemoryStream();
			LocalWriter = new StreamWriter(m_LocalStream);
			m_EpilogStream = new MemoryStream();
			EpilogWriter = new StreamWriter(m_EpilogStream);
			m_Lhs = yacc.m_Lhs.AsReadOnly();
			m_Len = yacc.m_Len.AsReadOnly();
			m_DefRed = yacc.m_DefRed.AsReadOnly();
			m_Dgoto = yacc.m_Dgoto.AsReadOnly();
			m_Sindex = yacc.m_Sindex.AsReadOnly();
			m_Rindex = yacc.m_Rindex.AsReadOnly();
			m_Gindex = yacc.m_Gindex.AsReadOnly();
			m_Table = yacc.m_Table.AsReadOnly();
			m_Check = yacc.m_Check.AsReadOnly();
			m_Names = yacc.m_Names.AsReadOnly();
			m_Rule = yacc.m_Rule.AsReadOnly();
		}

		public TextWriter Out {
			get { return m_Out; }
			set { m_Out = value; }
		}

		public TextReader TemplateReader {
			get { return m_TemplateReader; }
			set { m_TemplateReader = value; }
		}

		public string LineFormat {
			get { return m_LineFormat; }
			set { m_LineFormat = value; }
		}

		public void Execute()
		{
			int lno = 0;
			string buf;

			while ((buf = m_TemplateReader.ReadLine()) != null) {
				string cp;
				++lno;
				if (buf.Length == 0)
					continue;
				switch (buf[0]) {
				case '#': continue;
				case 't': if (!m_Yacc.tFlag) m_Out.Write("//t"); break;
				case '.': break;
				default:
					cp = strtok(buf, ' ', '\t', '\r', '\n');
					if (cp != null) {
						string prefix = strtok(null, '\r', '\n');
						if (prefix == null) prefix = "";

						if (String.Compare(cp, "actions") == 0) OutputSemanticActions();
						else if (String.Compare(cp, "epilog") == 0) OutputStoredText(EpilogWriter, m_EpilogStream);
						else if (String.Compare(cp, "local") == 0) OutputStoredText(LocalWriter, m_LocalStream);
						else if (String.Compare(cp, "prolog") == 0) OutputStoredText(PrologWriter, m_PrologStream);
						else if (String.Compare(cp, "tokens") == 0) OutputDefines(prefix);
						else if (String.Compare(cp, "shortcuts") == 0) OutputShortcuts(prefix);
						else if (String.Compare(cp, "version") == 0) OutputVersion(prefix);
						else if (String.Compare(cp, "yyCheck") == 0) OutputShortArray("yyCheck", prefix, m_Check);
						else if (String.Compare(cp, "yyDefRed") == 0) OutputShortArray("yyDefRed", prefix, m_DefRed);
						else if (String.Compare(cp, "yyDgoto") == 0) OutputShortArray("yyDgoto", prefix, m_Dgoto);
						else if (String.Compare(cp, "yyFinal") == 0) OutputFinal(prefix);
						else if (String.Compare(cp, "yyGindex") == 0) OutputShortArray("yyGindex", prefix, m_Gindex);
						else if (String.Compare(cp, "yyLen") == 0) OutputShortArray("yyLen", prefix, m_Len);
						else if (String.Compare(cp, "yyLhs") == 0) OutputShortArray("yyLhs", prefix, m_Lhs);
						else if (String.Compare(cp, "yyNames-strings") == 0) OutputNamesStrings(m_Names);
						else if (String.Compare(cp, "yyNames") == 0) OutputNames(prefix);
						else if (String.Compare(cp, "yyRindex") == 0) OutputShortArray("yyRindex", prefix, m_Rindex);
						else if (String.Compare(cp, "yyRule-strings") == 0) OutputRuleStrings();
						else if (String.Compare(cp, "yyRule") == 0) OutputRule(prefix);
						else if (String.Compare(cp, "yySindex") == 0) OutputShortArray("yySindex", prefix, m_Sindex);
						else if (String.Compare(cp, "yyTable") == 0) OutputShortArray("yyTable", prefix, m_Table);
						else
							m_Error.Errors.Add(String.Format("{0}: unknown call ({1}) in line {2}\n", m_Error.MyName, cp, lno));
					}
					continue;
				}
				m_Out.WriteLine(buf.ToCharArray(), 1, buf.Length - 1);
				m_Outline++;
			}
		}

		string strtokbuf;

		private string strtok(string buf, params char[] p)
		{
			if (buf != null) {
				buf = buf.TrimStart(p);
			}
			else if (strtokbuf.Length == 0) {
				return null;
			}
			else {
				buf = strtokbuf;
			}

			string[] strtoks = buf.Split(p, 2);
			switch (strtoks.Length) {
			case 0:
				strtokbuf = "";
				return null;
			case 1:
				strtokbuf = "";
				break;
			default:
				strtokbuf = strtoks[1];
				break;
			}
			return strtoks[0];
		}

		void OutputVersion(string prefix)
		{
			m_Out.Write("// created by jay 1.1.0 (c) 2002-2006 ats@cs.rit.edu\n"
				   + "// skeleton {0}\n", prefix);
			m_Outline += 2;
		}

		void OutputShortArray(string name, string prefix, ReadOnlyCollection<short> list)
		{
			int i, j;

			m_Out.Write("//{0} {1}\n", name, list.Count);
			m_Outline++;

			m_Out.Write("{0}{1,6},", prefix, list[0]);
			j = 1;
			for (i = 1; i < list.Count; ++i) {
				if (j >= 10) {
					m_Out.Write("\n{0}", prefix);
					j = 0;
					m_Outline++;
				}
				m_Out.Write("{0,6},", list[i]);
				++j;
			}
			m_Out.Write('\n');
			m_Outline++;
		}

		bool is_C_identifier(string name)
		{
			int s;
			char c;

			s = 0;
			c = name[s];
			if (c == '"') {
				c = name[++s];
				if (!Char.IsLetter(c) && c != '_' && c != '$')
					return false;
				while ((c = name[++s]) != '"') {
					if (!Char.IsLetterOrDigit(c) && c != '_' && c != '$')
						return false;
				}
				return true;
			}

			if (!Char.IsLetter(c) && c != '_' && c != '$')
				return false;
			foreach (char p in name) {
				if (!Char.IsLetterOrDigit(p) && p != '_' && p != '$')
					return false;
			}
			return true;
		}

		void OutputDefines(string prefix)
		{
			char c;
			int i;
			string name;
			int s;

			token_enum = prefix;

			for (i = 2; i < m_Yacc.m_TokenCount; ++i) {
				name = m_Yacc.m_Symbols[i].Name;
				s = 0;
				if (is_C_identifier(name)) {
					c = name[s];
					if (c == '"') {
						while ((c = name[++s]) != '"') {
							m_Out.Write((char)c);
						}
					}
					else {
						for (;;) {
							m_Out.Write((char)c);
							++s;
							if (s >= name.Length)
								break;
							c = name[s];
						}
					}
					m_Outline++;
					m_Out.Write(" = {0},\n", m_Yacc.m_Symbols[i].Value);
				}
			}

			m_Outline++;
			m_Out.Write("  yyErrorCode = {0}\n", m_Yacc.m_Symbols[1].Value);
		}

		void OutputShortcuts(string prefix)
		{
			char c;
			int i;
			string name;
			int s;

			for (i = 2; i < m_Yacc.m_TokenCount; ++i) {
				StringBuilder temp = new StringBuilder();
				name = m_Yacc.m_Symbols[i].Name;
				s = 0;
				if (is_C_identifier(name)) {
					m_Out.Write("  const {0} ", prefix);
					c = name[s];
					if (c == '"') {
						while ((c = name[++s]) != '"') {
							temp.Append((char)c);
						}
					}
					else {
						for (;;) {
							temp.Append((char)c);
							++s;
							if (s >= name.Length)
								break;
							c = name[s];
						}
					}
					m_Outline++;
					m_Out.Write("{1} = {0}.{1};\n", prefix, temp.ToString());
				}
			}

			m_Outline++;
			m_Out.Write("  const int yyErrorCode = (int){0}.yyErrorCode;\n", prefix);
		}

		void OutputStoredText(TextWriter file, Stream name)
		{
			int c;
			TextReader In;

			file.Flush();
			name.Seek(0, SeekOrigin.Begin);
			In = new StreamReader(name);

			if ((c = In.Read()) != Defs.EOF) {
				if (c == '\n')
					m_Outline++;
				m_Out.Write((char)c);
				while ((c = In.Read()) != Defs.EOF) {
					if (c == '\n')
						m_Outline++;
					m_Out.Write((char)c);
				}
				m_Out.WriteLine("#line default");
			}
			In.Close();
		}

		void OutputFinal(string prefix)
		{
			m_Out.Write("  {0} {1};\n", prefix, m_Yacc.m_FinalState);
			m_Outline++;
		}

		void OutputNames(string prefix)
		{
			int i, max;

			max = 0;
			for (i = 2; i < m_Yacc.m_TokenCount; ++i)
				if (m_Yacc.m_Symbols[i].Value > max)
					max = m_Yacc.m_Symbols[i].Value;

			m_Out.Write("//yyNames {0} {1}\n", max + 1, 1 + m_Yacc.m_TokenCount - 2);
			m_Outline++;
			m_Out.Write("{0} 0 end-of-file\n", prefix);
			m_Outline++;
			for (i = 2; i < m_Yacc.m_TokenCount; ++i) {
				m_Out.Write("{0} {1} {2}\n", prefix, m_Yacc.m_Symbols[i].Value, m_Yacc.m_Symbols[i].Name);
				m_Outline++;
			}
		}

		void OutputNamesStrings(ReadOnlyCollection<string> symnam)
		{
			int i, j, k;
			string name;
			int s;

			j = 0; m_Out.Write("    ");
			for (i = 0; i < symnam.Count; ++i) {
				if ((name = symnam[i]) != null) {
					s = 0;
					if (name[0] == '"') {
						k = 7;
						while (name[++s] != '"') {
							++k;
							if (name[s] == '\\') {
								k += 2;
								if (name[++s] == '\\') ++k;
							}
						}
						j += k;
						if (j > 70) {
							m_Out.Write("\n    ");
							m_Outline++;
							j = k;
						}
						m_Out.Write("\"\\\"");
						name = symnam[i];
						s = 0;
						while (name[++s] != '"') {
							if (name[s] == '\\') {
								m_Out.Write("\\\\");
								if (name[++s] == '\\') m_Out.Write("\\\\");
								else m_Out.Write(name[s]);
							}
							else
								m_Out.Write(name[s]);
						}
						m_Out.Write("\\\"\",");
					}
					else if (name[0] == '\'') {
						if (name[1] == '"') {
							j += 7;
							if (j > 70) {
								m_Out.Write("\n    ");
								m_Outline++;
								j = 7;
							}
							m_Out.Write("\"'\\\"'\",");
						}
						else {
							k = 5;
							while (name[++s] != '\'') {
								++k;
								if (name[s] == '\\') {
									k += 2;
									if (name[++s] == '\\')
										++k;
								}
							}
							j += k;
							if (j > 70) {
								m_Out.Write("\n    ");
								m_Outline++;
								j = k;
							}
							m_Out.Write("\"'");
							name = symnam[i];
							s = 0;
							while (name[++s] != '\'') {
								if (name[s] == '\\') {
									m_Out.Write("\\\\");
									if (name[++s] == '\\') m_Out.Write("\\\\");
									else m_Out.Write(name[s]);
								}
								else
									m_Out.Write(name[s]);
							}
							m_Out.Write("'\",");
						}
					}
					else {
						k = name.Length + 3;
						j += k;
						if (j > 70) {
							m_Out.Write("\n    ");
							m_Outline++;
							j = k;
						}
						m_Out.Write('"');
						do { m_Out.Write(name[s]); ++s; } while (s < name.Length);
						m_Out.Write("\",");
					}
				}
				else {
					j += 5;
					if (j > 70) {
						m_Out.Write("\n    ");
						m_Outline++;
						j = 5;
					}
					m_Out.Write("null,");
				}
			}
			m_Out.Write('\n');
			m_Outline++;
		}

		void OutputRule(string prefix)
		{
			m_Out.Write("//yyRule {0}\n", m_Rule.Count - 2);
			m_Outline++;
			foreach (string rule in m_Rule) {
				m_Out.Write(rule + "\n");
				m_Outline++;
			}
		}

		void OutputRuleStrings()
		{
			int i, j;
			string name;
			int s;
			string prefix = m_Yacc.tFlag ? "" : "//t";

			for (i = 2; i < m_Yacc.m_RuleCount; ++i) {
				m_Out.Write("{0}    \"{1} :", prefix, m_Yacc.m_Rules[i].Lhs.Name);
				for (j = m_Yacc.m_Rules[i].Rhs; m_Yacc.m_Items[j] > 0; ++j) {
					name = m_Yacc.m_Symbols[m_Yacc.m_Items[j]].Name;
					s = 0;
					if (name[0] == '"') {
						m_Out.Write(" \\\"");
						while (name[++s] != '"') {
							if (name[s] == '\\') {
								if (name[1] == '\\') m_Out.Write("\\\\\\\\");
								else m_Out.Write("\\\\{0}", name[1]);
								++s;
							}
							else
								m_Out.Write(name[s]);
						}
						m_Out.Write("\\\"");
					}
					else if (name[0] == '\'') {
						if (name[1] == '"')
							m_Out.Write(" '\\\"'");
						else if (name[1] == '\\') {
							if (name[2] == '\\') m_Out.Write(" '\\\\\\\\");
							else m_Out.Write(" '\\\\{0}", name[2]);
							s += 2;
							while (name[++s] != '\'')
								m_Out.Write(name[s]);
							m_Out.Write('\'');
						}
						else
							m_Out.Write(" '{0}'", name[1]);
					}
					else
						m_Out.Write(" {0}", name.Substring(s));
				}
				m_Out.Write("\",\n");
				m_Outline++;
			}
		}

		void OutputSemanticActions()
		{
			int c, last = 0;

			foreach (Rule<string> rule in m_Yacc.m_Rules) {
				if (rule.Action == null)
					continue;

				StringReader ActionReader = new StringReader(rule.Action);

				if ((c = ActionReader.Read()) == Defs.EOF)
					continue;

				last = c;
				if (c == '\n')
					m_Outline++;
				m_Out.Write((char)c);
				while ((c = ActionReader.Read()) != Defs.EOF) {
					if (c == '\n')
						m_Outline++;
					m_Out.Write((char)c);
					last = c;
				}
			}

			if (last == 0)
				return;

			if (last != '\n') {
				m_Outline++;
				m_Out.Write("\n      ");
			}

			m_Out.WriteLine("#line default");
		}
	}
}
