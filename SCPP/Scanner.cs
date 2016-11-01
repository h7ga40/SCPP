using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Yacc;

namespace SCPP
{
	public class Scanner : DirectiveParser.Input
	{
		StringBuilder m_Token;
		Symbol m_Kind;
		ExpressionNode m_Value;

		TextReader m_Text;
		List<int> m_Unget = new List<int>();
		int m_Position;

		public Scanner(TextReader text)
		{
			m_Text = text;
			m_Position = 0;
		}

		#region Input メンバ

		public bool Advance()
		{
			for (; ; )
			{
				if (!HasMoreTokens)
					return false;

				string token = NextToken();

				if ((m_Kind == DirectiveParser.WHITE)
					|| (m_Kind == DirectiveParser.COMMENT))
					continue;

				m_Value = new ExpressionNode();
				m_Value.Kind = m_Kind;
				m_Value.Token = token;

				return true;
			}
		}

		public Symbol Token
		{
			get { return m_Kind; }
		}

		public ExpressionNode Value
		{
			get { return m_Value; }
		}

		#endregion

		/// <summary>
		/// １文字読み込む
		/// </summary>
		/// <returns></returns>
		int ReadChar()
		{
			int c;

			if (m_Unget.Count > 0)
			{
				int i = m_Unget.Count - 1;
				c = m_Unget[i];
				m_Unget.RemoveAt(i);
			}
			else
			{
				c = m_Text.Read();
			}

			return c;
		}

		/// <summary>
		/// 1文字読まなかったことにする
		/// </summary>
		/// <param name="c"></param>
		void Unget(int c)
		{
			m_Unget.Add(c);
		}

		/// <summary>
		/// 
		/// </summary>
		public int Position
		{
			get { return m_Position; }
		}

		/// <summary>
		/// トークンがあるか
		/// </summary>
		public bool HasMoreTokens
		{
			get
			{
				int c = ReadChar();
				Unget(c);
				return c != -1;
			}
		}

		/// <summary>
		/// スペースの間読み進める
		/// </summary>
		private void UntilSpace()
		{
			int c;

			while ((c = ReadChar()) != -1)
			{
				switch (c)
				{
					case ' ':
					case '\t':
					case '\r':
					case '\n':
					case '\f':
						m_Token.Append((char)c);
						break;
					default:
						Unget(c);
						return;
				}
			}
		}

		/// <summary>
		/// 数値の間読み進める
		/// </summary>
		private void UntilNumber()
		{
			int c;

			while ((c = ReadChar()) != -1)
			{
				if (c >= '0' && c <= '9')
					m_Token.Append((char)c);
				else
				{
					Unget(c);
					return;
				}
			}
		}

		/// <summary>
		/// 16進数の数値の間読み進める
		/// </summary>
		private void UntilHexNumber()
		{
			int c;

			while ((c = ReadChar()) != -1)
			{
				if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
					m_Token.Append((char)c);
				else
				{
					Unget(c);
					return;
				}
			}
		}

		/// <summary>
		/// 文字列の間読み進める
		/// </summary>
		/// <param name="qout"></param>
		private void UntilString(char qout)
		{
			int c;

			while ((c = ReadChar()) != -1)
			{
				m_Token.Append((char)c);
				if (c == '\\')
				{
					if ((c = ReadChar()) == -1)
						return;

					m_Token.Append((char)c);
				}
				else if (c == qout)
				{
					return;
				}
			}
		}

		/// <summary>
		/// コメントの間読み進める
		/// </summary>
		private void UntilComment()
		{
			int c;

			while ((c = ReadChar()) != -1)
			{
				m_Token.Append((char)c);

				if (c == '*')
				{
					if ((c = ReadChar()) == -1)
						return;

					m_Token.Append((char)c);

					if (c == '/')
						return;
				}
			}
		}

		/// <summary>
		/// 行コメントの間読み進める
		/// </summary>
		private void UntilEndOfLine()
		{
			int c;
			bool Esc = false;

			while ((c = ReadChar()) != -1)
			{
				m_Token.Append((char)c);

				switch (c)
				{
					case '\r':
						if ((c = ReadChar()) == -1)
							return;

						if (c == '\n')
						{
							m_Token.Append((char)c);
						}
						else
						{
							Unget(c);
						}
						if (Esc)
							continue;
						return;
					case '\n':
						if (Esc)
							continue;
						return;
					case '\\':
						Esc = true;
						break;
					default:
						Esc = false;
						break;
				}
			}
		}

		/// <summary>
		/// 現在のトークンを取得
		/// </summary>
		/// <returns></returns>
		public string NextToken()
		{
			StringBuilder result = DoNextToken();

			m_Position += result.Length;

			return result.ToString();
		}

		/// <summary>
		/// 現在のトークンを取得の内部処理
		/// </summary>
		/// <returns></returns>
		private StringBuilder DoNextToken()
		{
			int c;

			m_Token = new StringBuilder();

			if ((c = ReadChar()) == -1)
			{
				m_Kind = DirectiveParser.WHITE;
				return m_Token;
			}

			m_Token.Append((char)c);

			switch (c)
			{
				case ' ':
				case '\t':
				case '\r':
				case '\n':
				case '\f':
					m_Kind = DirectiveParser.WHITE;
					UntilSpace();
					return m_Token;
				case '*':
					m_Kind = DirectiveParser.MUL;
					return m_Token;
				case '/':
					m_Kind = DirectiveParser.DIV;
					if ((c = ReadChar()) != -1)
					{
						switch (c)
						{
							case '*':
								m_Kind = DirectiveParser.COMMENT;
								m_Token.Append((char)c);
								UntilComment();
								return m_Token;
							case '/':
								m_Kind = DirectiveParser.COMMENT;
								m_Token.Append((char)c);
								UntilEndOfLine();
								return m_Token;
						}
						Unget(c);
					}
					return m_Token;
				case '%':
					m_Kind = DirectiveParser.MOD;
					return m_Token;
				case '+':
					m_Kind = DirectiveParser.PLUS;
					return m_Token;
				case '-':
					m_Kind = DirectiveParser.MINUS;
					return m_Token;
				case '&':
					m_Kind = DirectiveParser.AND;
					if ((c = ReadChar()) != -1)
					{
						if (c == '&')
						{
							m_Kind = DirectiveParser.ANDAND;
							m_Token.Append((char)c);
						}
						else
							Unget(c);
					}
					return m_Token;
				case '|':
					m_Kind = DirectiveParser.OR;
					if ((c = ReadChar()) != -1)
					{
						if (c == '|')
						{
							m_Kind = DirectiveParser.OROR;
							m_Token.Append((char)c);
						}
						else
							Unget(c);
					}
					return m_Token;
				case '^':
					m_Kind = DirectiveParser.ER;
					return m_Token;
				case '<':
					m_Kind = DirectiveParser.LT;
					if ((c = ReadChar()) != -1)
					{
						if (c == '=')
						{
							m_Kind = DirectiveParser.LE;
							m_Token.Append((char)c);
						}
						else if (c == '<')
						{
							m_Kind = DirectiveParser.LS;
							m_Token.Append((char)c);
						}
						else
							Unget(c);
					}
					return m_Token;
				case '>':
					m_Kind = DirectiveParser.GT;
					if ((c = ReadChar()) != -1)
					{
						if (c == '=')
						{
							m_Kind = DirectiveParser.GE;
							m_Token.Append((char)c);
						}
						else if (c == '>')
						{
							m_Kind = DirectiveParser.RS;
							m_Token.Append((char)c);
						}
						else
							Unget(c);
					}
					return m_Token;
				case '=':
					if ((c = ReadChar()) != -1)
					{
						if (c == '=')
						{
							m_Kind = DirectiveParser.EQ;
							m_Token.Append((char)c);
							return m_Token;
						}
						else
							Unget(c);
					}
					break;
				case ',':
					m_Kind = DirectiveParser.CM;
					return m_Token;
				case '?':
					m_Kind = DirectiveParser.QUEST;
					return m_Token;
				case ':':
					m_Kind = DirectiveParser.COLON;
					return m_Token;
				case '!':
					m_Kind = DirectiveParser.NOT;
					if ((c = ReadChar()) != -1)
					{
						if (c == '=')
						{
							m_Kind = DirectiveParser.NE;
							m_Token.Append((char)c);
						}
						else
							Unget(c);
					}
					return m_Token;
				case '~':
					m_Kind = DirectiveParser.COMPL;
					return m_Token;
				case '(':
					m_Kind = DirectiveParser.LP;
					return m_Token;
				case ')':
					m_Kind = DirectiveParser.RP;
					return m_Token;
				case '0':
					m_Kind = DirectiveParser.INT;
					if ((c = ReadChar()) != -1)
					{
						if ((c == 'x' || c == 'X'))
						{
							UntilHexNumber();
							return m_Token;
						}
						else
							Unget(c);
					}
					goto case '1';
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					m_Kind = DirectiveParser.INT;
					UntilNumber();
					if ((c = ReadChar()) != -1)
					{
						if (c == '.')
						{
							m_Kind = DirectiveParser.FLOAT;
							m_Token.Append((char)c);
							UntilNumber();
						}
						else
							Unget(c);
					}
					return m_Token;
				case '\'':
					m_Kind = DirectiveParser.CHARS;
					UntilString('\'');
					return m_Token;
				case '"':
					m_Kind = DirectiveParser.STRING;
					UntilString('"');
					return m_Token;
			}

			m_Kind = DirectiveParser.IDENT;
			while ((c = ReadChar()) != -1)
			{
				switch (c)
				{
					case ' ':
					case '\t':
					case '\r':
					case '\n':
					case '\f':
					case '*':
					case '%':
					case '+':
					case '-':
					case '&':
					case '|':
					case '^':
					case '<':
					case '>':
					case '=':
					case ',':
					case '?':
					case ':':
					case '!':
					case '~':
					case '(':
					case ')':
					case '\'':
					case '"':
						Unget(c);
						goto loopout;
					case '/':
						int c2 = ReadChar();
						if (c2 == -1)
						{
							m_Token.Append((char)c);
							goto loopout;
						}
						switch (c2)
						{
							case '*':
							case '/':
								Unget(c2);
								Unget(c);
								goto loopout;
							default:
								m_Token.Append((char)c);
								break;
						}
						break;
					default:
						break;
				}
				m_Token.Append((char)c);
			}
		loopout:
			if (m_Token.ToString().CompareTo("defined") == 0)
				m_Kind = DirectiveParser.DEFMAC;

			return m_Token;
		}
	}
}
