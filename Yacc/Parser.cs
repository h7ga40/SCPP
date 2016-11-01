//
//version	c# 1.1.0 (c) 2002-2006 ats@cs.rit.edu
//
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Yacc
{
	public abstract class Parser<ParserValue> : Grammar<ActionEventHandler<ParserValue>>
	{
		public const int ErrorCode = 256;

		private int m_Final = 0;
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

		/// <summary>
		///   debugging support, requires <c>yyDebug</c>.
		///   Set to <c>null</c> to suppress debugging messages.
		/// </summary>
		protected IYaccDebug m_Debug;

		public Parser()
			: base(new Yacc<ActionEventHandler<ParserValue>>())
		{
			Yacc.Grammar = this;
		}

		/// <summary>
		///   final state of parser.
		/// </summary>
		public int Final { get { return m_Final; } }

		/// <summary>
		///   parser tables.
		///   Order is mandated by jay.
		/// </summary>
		protected ReadOnlyCollection<short> Lhs { get { return m_Lhs; } }
		protected ReadOnlyCollection<short> Len { get { return m_Len; } }
		protected ReadOnlyCollection<short> DefRed { get { return m_DefRed; } }
		protected ReadOnlyCollection<short> Dgoto { get { return m_Dgoto; } }
		protected ReadOnlyCollection<short> Sindex { get { return m_Sindex; } }
		protected ReadOnlyCollection<short> Rindex { get { return m_Rindex; } }
		protected ReadOnlyCollection<short> Gindex { get { return m_Gindex; } }
		protected ReadOnlyCollection<short> Table { get { return m_Table; } }
		protected ReadOnlyCollection<short> Check { get { return m_Check; } }

		/// <summary>
		///   maps symbol value to printable name.
		///   see <c>yyExpecting</c>
		/// </summary>
		protected ReadOnlyCollection<string> Names { get { return m_Names; } }

		/// <summary>
		///   printable rules for debugging.
		/// </summary>
		protected ReadOnlyCollection<string> Rule { get { return m_Rule; } }

		/// <summary>
		///   index-checked interface to <c>yyNames[]</c>.
		/// </summary>
		/// <param name='token'>single character or <c>%token</c> value</param>
		/// <returns>token name or <c>[illegal]</c> or <c>[unknown]</c></returns>
		public string TokenToName(int token)
		{
			if ((token < 0) || (token > m_Names.Count)) return "[illegal]";
			string name;
			if ((name = m_Names[token]) != null) return name;
			return "[unknown]";
		}

		/// <summary>
		///   must be implemented by a scanner object to supply input to the parser.
		/// </summary>
		/// <remarks>
		///   Nested for convenience, does not depend on parser class.
		/// </remarks>
		public interface Input
		{
			/// <summary>
			///   move on to next token.
			/// </summary>
			/// <returns><c>false</c> if positioned beyond tokens</returns>
			/// <exception><c>IOException</c> on input error</exception>
			bool Advance();

			/// <summary>
			///   classifies current token by <c>%token</c> value or single character.
			/// </summary>
			/// <remarks>
			///   Should not be called if <c>Advance()</c> returned false.
			/// </remarks>
			Symbol Token { get; }

			/// <summary>
			///   value associated with current token.
			/// </summary>
			/// <remarks>
			///   Should not be called if <c>Advance()</c> returned false.
			/// </remarks>
			ParserValue Value { get; }
		}

		/// <summary>
		///   (syntax) error message.
		///   Can be overwritten to control message format.
		/// </summary>
		/// <param name='message'>text to be displayed</param>
		/// <param name='expected'>list of acceptable tokens, if available</param>
		public virtual new void Error(string message, params string[] expected)
		{
			throw new YaccException(String.Format(message, (object[])expected));
		}

		/// <summary>
		///   computes list of expected tokens on error by tracing the tables.
		/// </summary>
		/// <param name='state'>for which to compute the list</param>
		/// <returns>list of token names</returns>
		protected string[] Expecting(int state)
		{
			int token, n, len = 0;
			bool[] ok = new bool[m_Names.Count];

			if ((n = m_Sindex[state]) != 0)
				for (token = n < 0 ? -n : 0;
					 (token < m_Names.Count) && (n + token < m_Table.Count); ++token)
					if (m_Check[n + token] == token && !ok[token] && m_Names[token] != null)
					{
						++len;
						ok[token] = true;
					}
			if ((n = m_Rindex[state]) != 0)
				for (token = n < 0 ? -n : 0;
					 (token < m_Names.Count) && (n + token < m_Table.Count); ++token)
					if (m_Check[n + token] == token && !ok[token] && m_Names[token] != null)
					{
						++len;
						ok[token] = true;
					}

			string[] result = new string[len];
			for (n = token = 0; n < len; ++token)
				if (ok[token]) result[n++] = m_Names[token];
			return result;
		}

		/// <summary>
		///   the generated parser, with debugging messages.
		///   Maintains a dynamic state and value stack.
		/// </summary>
		/// <param name='yyLex'>scanner</param>
		/// <param name='yyDebug'>debug message writer implementing <c>yyDebug</c>,
		///   or <c>null</c></param>
		/// <returns>result of the last reduction, if any</returns>
		/// <exceptions><c>yyException</c> on irrecoverable parse error</exceptions>
		public ParserValue Parse(Input Lex, IYaccDebug debug)
		{
			this.m_Debug = debug;
			return Parse(Lex);
		}

		/// <summary>
		///   initial size and increment of the state/value stack [default 256].
		///    This is not final so that it can be overwritten outside of invocations
		///    of <c>yyParse()</c>.
		/// </summary>
		protected int m_Max;

		/// <summary>
		///   executed at the beginning of a reduce action.
		///   Used as <c>$$ = yyDefault($1)</c>, prior to the user-specified action, if any.
		///   Can be overwritten to provide deep copy, etc.
		/// </summary>
		/// <param first value for $1, or null.
		/// <return first.
		protected virtual ParserValue Default(ParserValue first)
		{
			return first;
		}

		protected int m_Token;					// current input
		protected int m_ErrorFlag;				// #tokens to shift
		private ValueList m_Values = new ValueList();

		/// <summary>
		///   the generated parser, with debugging messages.
		///   Maintains a dynamic state and value stack.
		/// </summary>
		/// <param name='yyLex'>scanner</param>
		/// <returns>result of the last reduction, if any</returns>
		/// <exceptions><c>yyException</c> on irrecoverable parse error</exceptions>
		public virtual ParserValue Parse(Input lex)
		{
			int state = 0;											// state stack ptr
			List<int> states = new List<int>();					// state stack 
			ParserValue value = default(ParserValue);				// value stack ptr
			List<ParserValue> values = m_Values.Values;				// value stack
			ActionEventHandler<ParserValue> action;
			m_Token = -1;											// current input
			m_ErrorFlag = 0;										// #tokens to shift

			for (int top = 0; ; top++) /*yyLoop*/
			{
				while (top >= states.Count)
				{
					states.Add(0);
					values.Add(default(ParserValue));
				}
				states[top] = state;
				values[top] = value;
				if (m_Debug != null) m_Debug.push(state, value);

				for (; ; )
				{	// discarding a token does not change stack
					int n;
					if ((n = m_DefRed[state]) == 0)
					{	// else [default] reduce (yyN)
						if (m_Token < 0)
						{
							m_Token = lex.Advance() ? lex.Token.Value : 0;
							if (m_Debug != null)
								m_Debug.lex(state, m_Token, TokenToName(m_Token), lex.Value);
						}
						if ((n = m_Sindex[state]) != 0 && ((n += m_Token) >= 0)
							&& (n < m_Table.Count) && (m_Check[n] == m_Token))
						{
							if (m_Debug != null)
								m_Debug.shift(state, m_Table[n], m_ErrorFlag > 0 ? m_ErrorFlag - 1 : 0);
							state = m_Table[n];		// shift to yyN
							value = lex.Value;
							m_Token = -1;
							if (m_ErrorFlag > 0) --m_ErrorFlag;
							goto continue_yyLoop;
						}
						if ((n = m_Rindex[state]) != 0 && (n += m_Token) >= 0
							&& n < m_Table.Count && m_Check[n] == m_Token)
							n = m_Table[n];			// reduce (yyN)
						else
							switch (m_ErrorFlag)
							{

								case 0:
									Error("syntax error", Expecting(state));
									if (m_Debug != null) m_Debug.error("syntax error");
									goto case 1;
								case 1:
								case 2:
									m_ErrorFlag = 3;
									do
									{
										if ((n = m_Sindex[states[top]]) != 0
											&& (n += ErrorCode) >= 0 && n < m_Table.Count
											&& m_Check[n] == ErrorCode)
										{
											if (m_Debug != null)
												m_Debug.shift(states[top], m_Table[n], 3);
											state = m_Table[n];
											value = lex.Value;
											goto continue_yyLoop;
										}
										if (m_Debug != null) m_Debug.pop(states[top]);
									} while (--top >= 0);
									if (m_Debug != null) m_Debug.reject();
									throw new YaccException("irrecoverable syntax error");

								case 3:
									if (m_Token == 0)
									{
										if (m_Debug != null) m_Debug.reject();
										throw new YaccException("irrecoverable syntax error at end-of-file");
									}
									if (m_Debug != null)
										m_Debug.discard(state, m_Token, TokenToName(m_Token),
													lex.Value);
									m_Token = -1;
									goto continue_yyDiscarded;		// leave stack alone
							}
					}
					int v = top + 1 - m_Len[n];
					if (m_Debug != null)
						m_Debug.reduce(state, states[v - 1], n, m_Rule[n], m_Len[n]);

					m_Values.Offset = top + 1 - Yacc.m_Rules[n + 1].Offset;
					value = Default(v > top ? default(ParserValue) : values[v]);
					action = Yacc.m_Rules[n + 1].Action;
					if (action != null)
						action(n, m_Values, ref value);

					top -= m_Len[n];
					state = states[top];
					int m = m_Lhs[n];
					if (state == 0 && m == 0)
					{
						if (m_Debug != null) m_Debug.shift(0, m_Final);
						state = m_Final;
						if (m_Token < 0)
						{
							m_Token = lex.Advance() ? lex.Token.Value : 0;
							if (m_Debug != null)
								m_Debug.lex(state, m_Token, TokenToName(m_Token), lex.Value);
						}
						if (m_Token == 0)
						{
							if (m_Debug != null) m_Debug.accept(value);
							return value;
						}
						goto continue_yyLoop;
					}
					if (((n = m_Gindex[m]) != 0) && ((n += state) >= 0)
						&& (n < m_Table.Count) && (m_Check[n] == state))
						state = m_Table[n];
					else
						state = m_Dgoto[m];
					if (m_Debug != null) m_Debug.shift(states[top], state);
					goto continue_yyLoop;
				continue_yyDiscarded:
					continue;
				}
			continue_yyLoop:
				continue;
			}
		}

		private void InitTables()
		{
			m_Final = Yacc.m_FinalState;
			m_Lhs = Yacc.m_Lhs.AsReadOnly();
			m_Len = Yacc.m_Len.AsReadOnly();
			m_DefRed = Yacc.m_DefRed.AsReadOnly();
			m_Dgoto = Yacc.m_Dgoto.AsReadOnly();
			m_Sindex = Yacc.m_Sindex.AsReadOnly();
			m_Rindex = Yacc.m_Rindex.AsReadOnly();
			m_Gindex = Yacc.m_Gindex.AsReadOnly();
			m_Table = Yacc.m_Table.AsReadOnly();
			m_Check = Yacc.m_Check.AsReadOnly();
			m_Names = Yacc.m_Names.AsReadOnly();
			m_Rule = Yacc.m_Rule.AsReadOnly();
#if OUTPUT_CODE
			Debug.Write(String.Format("  {0} {1};\n", "", m_Final));
			OutputArray("yyLhs", "", m_Lhs);
			OutputArray("yyLen", "", m_Len);
			OutputArray("yyDefRed", "", m_DefRed);
			OutputArray("yyDgoto", "", m_Dgoto);
			OutputArray("yySindex", "", m_Sindex);
			OutputArray("yyRindex", "", m_Rindex);
			OutputArray("yyGindex", "", m_Gindex);
			OutputArray("yyTable", "", m_Table);
			OutputArray("yyCheck", "", m_Check);
			OutputNamesStrings(m_Names);
			OutputArray("yyRule", "", m_Rule);
#endif
		}

		public new void Execute()
		{
			Yacc.Execute();
			InitTables();
		}
#if OUTPUT_CODE
		void OutputArray(string name, string prefix, ReadOnlyCollection<short> list)
		{
			int i, j;

			Debug.Write(String.Format("//{0} {1}\n", name, list.Count));

			Debug.Write(String.Format("{0}{1,6},", prefix, list[0]));
			j = 1;
			for (i = 1; i < list.Count; ++i)
			{
				if (j >= 10)
				{
					Debug.Write(String.Format("\n{0}", prefix));
					j = 0;
				}
				Debug.Write(String.Format("{0,6},", list[i]));
				++j;
			}
			Debug.Write('\n');
		}

		void OutputArray(string name, string prefix, ReadOnlyCollection<string> list)
		{
			int i;

			Debug.Write(String.Format("//{0} {1}\n", name, list.Count));

			for (i = 0; i < list.Count; ++i)
			{
				Debug.Write(String.Format("{0}\"{1}\",\n", prefix, list[i]));
			}
			Debug.Write('\n');
		}

		void OutputNamesStrings(ReadOnlyCollection<string> symnam)
		{
			int i, j, k;
			string name;
			int s;

			j = 0; Debug.Write("    ");
			for (i = 0; i < symnam.Count; ++i)
			{
				if ((name = symnam[i]) != null)
				{
					s = 0;
					if (name[0] == '"')
					{
						k = 7;
						while (name[++s] != '"')
						{
							++k;
							if (name[s] == '\\')
							{
								k += 2;
								if (name[++s] == '\\') ++k;
							}
						}
						j += k;
						if (j > 70)
						{
							Debug.Write("\n    ");
							j = k;
						}
						Debug.Write("\"\\\"");
						name = symnam[i];
						s = 0;
						while (name[++s] != '"')
						{
							if (name[s] == '\\')
							{
								Debug.Write("\\\\");
								if (name[++s] == '\\') Debug.Write("\\\\");
								else Debug.Write(name[s]);
							}
							else
								Debug.Write(name[s]);
						}
						Debug.Write("\\\"\",");
					}
					else if (name[0] == '\'')
					{
						if (name[1] == '"')
						{
							j += 7;
							if (j > 70)
							{
								Debug.Write("\n    ");
								j = 7;
							}
							Debug.Write("\"'\\\"'\",");
						}
						else
						{
							k = 5;
							while (name[++s] != '\'')
							{
								++k;
								if (name[s] == '\\')
								{
									k += 2;
									if (name[++s] == '\\')
										++k;
								}
							}
							j += k;
							if (j > 70)
							{
								Debug.Write("\n    ");
								j = k;
							}
							Debug.Write("\"'");
							name = symnam[i];
							s = 0;
							while (name[++s] != '\'')
							{
								if (name[s] == '\\')
								{
									Debug.Write("\\\\");
									if (name[++s] == '\\') Debug.Write("\\\\");
									else Debug.Write(name[s]);
								}
								else
									Debug.Write(name[s]);
							}
							Debug.Write("'\",");
						}
					}
					else
					{
						k = name.Length + 3;
						j += k;
						if (j > 70)
						{
							Debug.Write("\n    ");
							j = k;
						}
						Debug.Write('"');
						do { Debug.Write(name[s]); ++s; } while (s < name.Length);
						Debug.Write("\",");
					}
				}
				else
				{
					j += 5;
					if (j > 70)
					{
						Debug.Write("\n    ");
						j = 5;
					}
					Debug.Write("null,");
				}
			}
			Debug.Write('\n');
		}
#endif
		class ValueList : IList<ParserValue>
		{
			List<ParserValue> m_Values;
			int m_Offset;

			public ValueList()
			{
				m_Values = new List<ParserValue>();
				m_Offset = 0;
			}

			public List<ParserValue> Values
			{
				get { return m_Values; }
			}

			public int Offset
			{
				get { return m_Offset; }
				set { m_Offset = value; }
			}

			#region IList<ParserValue> メンバ

			public int IndexOf(ParserValue item)
			{
				return m_Values.IndexOf(item) - m_Offset;
			}

			public void Insert(int index, ParserValue item)
			{
				m_Values.Insert(index + m_Offset, item);
			}

			public void RemoveAt(int index)
			{
				m_Values.RemoveAt(index + m_Offset);
			}

			public ParserValue this[int index]
			{
				get { return m_Values[index + m_Offset]; }
				set { m_Values[index + m_Offset] = value; }
			}

			#endregion

			#region ICollection<ParserValue> メンバ

			public void Add(ParserValue item)
			{
				m_Values.Add(item);
			}

			public void Clear()
			{
				m_Values.Clear();
			}

			public bool Contains(ParserValue item)
			{
				return m_Values.Contains(item);
			}

			public void CopyTo(ParserValue[] array, int arrayIndex)
			{
				m_Values.CopyTo(m_Offset, array, arrayIndex, m_Values.Count - m_Offset);
			}

			public int Count
			{
				get { return m_Values.Count; }
			}

			public bool IsReadOnly
			{
				get { return false; }
			}

			public bool Remove(ParserValue item)
			{
				return m_Values.Remove(item);
			}

			#endregion

			#region IEnumerable<ParserValue> メンバ

			public IEnumerator<ParserValue> GetEnumerator()
			{
				return m_Values.GetEnumerator();
			}

			#endregion

			#region IEnumerable メンバ

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return m_Values.GetEnumerator();
			}

			#endregion
		}
	}
}
