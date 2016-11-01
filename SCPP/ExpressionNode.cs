using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yacc;

namespace SCPP
{
	public class ConditionInfo
	{
		public List<string> Enables = new List<string>();
		public List<string> Disables = new List<string>();
		public bool Default;

		internal bool IsEnable(string Token)
		{
			return Enables.Contains(Token);
		}

		internal bool IsDisable(string Token)
		{
			return Disables.Contains(Token);
		}
	}

	public class ExpressionNode : List<ExpressionNode>
	{
		public string Token;
		public Symbol Kind;
		public bool Parenthesis;
		public bool Defined;

		public int GetValue()
		{
			return GetValue(null);
		}

		public int GetValue(ConditionInfo condition)
		{
			if (Kind == DirectiveParser.MUL)
			{
				return this[0].GetValue(condition) * this[1].GetValue(condition);
			}
			else if (Kind == DirectiveParser.DIV)
			{
				return this[0].GetValue(condition) / this[1].GetValue(condition);
			}
			else if (Kind == DirectiveParser.MOD)
			{
				return this[0].GetValue(condition) % this[1].GetValue(condition);
			}
			else if (Kind == DirectiveParser.PLUS)
			{
				return this[0].GetValue(condition) + this[1].GetValue(condition);
			}
			else if (Kind == DirectiveParser.MINUS)
			{
				if (Count == 1)
				{
					return -this[0].GetValue(condition);
				}
				else if (Count == 2)
				{
					return this[0].GetValue(condition)
						- this[1].GetValue(condition);
				}
			}
			else if (Kind == DirectiveParser.LS)
			{
				return this[0].GetValue(condition) << this[1].GetValue(condition);
			}
			else if (Kind == DirectiveParser.RS)
			{
				return this[0].GetValue(condition) >> this[1].GetValue(condition);
			}
			else if (Kind == DirectiveParser.LT)
			{
				return (this[0].GetValue(condition) < this[1].GetValue(condition)) ? 1 : 0;
			}
			else if (Kind == DirectiveParser.GT)
			{
				return (this[0].GetValue(condition) > this[1].GetValue(condition)) ? 1 : 0;
			}
			else if (Kind == DirectiveParser.LE)
			{
				return (this[0].GetValue(condition) <= this[1].GetValue(condition)) ? 1 : 0;
			}
			else if (Kind == DirectiveParser.GE)
			{
				return (this[0].GetValue(condition) >= this[1].GetValue(condition)) ? 1 : 0;
			}
			else if (Kind == DirectiveParser.EQ)
			{
				return (this[0].GetValue(condition) == this[1].GetValue(condition)) ? 1 : 0;
			}
			else if (Kind == DirectiveParser.NE)
			{
				return (this[0].GetValue(condition) != this[1].GetValue(condition)) ? 1 : 0;
			}
			else if (Kind == DirectiveParser.AND)
			{
				return this[0].GetValue(condition) & this[1].GetValue(condition);
			}
			else if (Kind == DirectiveParser.ER)
			{
				return this[0].GetValue(condition) ^ this[1].GetValue(condition);
			}
			else if (Kind == DirectiveParser.OR)
			{
				return this[0].GetValue(condition) | this[1].GetValue(condition);
			}
			else if (Kind == DirectiveParser.ANDAND)
			{
				return (this[0].GetValue(condition) != 0 && this[1].GetValue(condition) != 0) ? 1 : 0;
			}
			else if (Kind == DirectiveParser.OROR)
			{
				return (this[0].GetValue(condition) != 0 || this[1].GetValue(condition) != 0) ? 1 : 0;
			}
			else if (Kind == DirectiveParser.QUEST)
			{
				return this[0].GetValue(condition) != 0 ? this[1].GetValue(condition) : this[2].GetValue(condition);
			}
			else if (Kind == DirectiveParser.CM)
			{
				return this[1].GetValue(condition);
			}
			else if (Kind == DirectiveParser.NOT)
			{
				return this[0].GetValue(condition) == 0 ? 1 : 0;
			}
			else if (Kind == DirectiveParser.COMPL)
			{
				return ~this[0].GetValue(condition);
			}
			else if (Kind == DirectiveParser.INT)
			{
				if ((Token.Length > 1) && (Token[0] == '0')
					&& ((Token[1] == 'x') || (Token[1] == 'X')))
				{
					return Convert.ToInt32(Token.Substring(2), 16);
				}

				return Int32.Parse(Token);
			}
			else if (Kind == DirectiveParser.IDENT)
			{
				if (condition != null)
				{
					if (condition.IsEnable(Token))
						return 1;

					if(condition.IsDisable(Token))
						return 0;

					return condition.Default ? 1 : 0;
				}
				else
					return 0;
			}

			throw new Exception();
		}

		public bool IsSelected(ConditionInfo condition)
		{
			return GetValue(condition) != 0;
		}

		public override string ToString()
		{
			if (Parenthesis)
				return "(" + GetString() + ")";

			return GetString();
		}

		private string GetString()
		{
			if (Kind == DirectiveParser.MUL)
			{
				return this[0].ToString() + " * " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.DIV)
			{
				return this[0].ToString() + " / " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.MOD)
			{
				return this[0].ToString() + " % " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.PLUS)
			{
				return this[0].ToString() + " + " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.MINUS)
			{
				if (Count == 1)
				{
					return "-" + this[0].ToString();
				}
				else if (Count == 2)
				{
					return this[0].ToString() + " - " + this[1].ToString();
				}
			}
			else if (Kind == DirectiveParser.LS)
			{
				return this[0].ToString() + " << " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.RS)
			{
				return this[0].ToString() + " >> " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.LT)
			{
				return this[0].ToString() + " < " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.GT)
			{
				return this[0].ToString() + " > " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.LE)
			{
				return this[0].ToString() + " <= " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.GE)
			{
				return this[0].ToString() + " >= " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.EQ)
			{
				return this[0].ToString() + " == " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.NE)
			{
				return this[0].ToString() + " != " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.AND)
			{
				return this[0].ToString() + " & " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.ER)
			{
				return this[0].ToString() + " ^ " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.OR)
			{
				return this[0].ToString() + " | " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.ANDAND)
			{
				return this[0].ToString() + " && " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.OROR)
			{
				return this[0].ToString() + " || " + this[1].ToString();
			}
			else if (Kind == DirectiveParser.QUEST)
			{
				return this[0].ToString() + " ? " + this[1].ToString() + " : " + this[2].ToString();
			}
			else if (Kind == DirectiveParser.CM)
			{
				return this[1].ToString();
			}
			else if (Kind == DirectiveParser.NOT)
			{
				return "!" + this[0].ToString();
			}
			else if (Kind == DirectiveParser.COMPL)
			{
				return "~" + this[0].ToString();
			}
			else if (Kind == DirectiveParser.INT)
			{
				return Token;
			}
			else if (Kind == DirectiveParser.IDENT)
			{
				if (Defined)
				{
					if (Parenthesis)
						return "defined" + Token + "";

					return "defined(" + Token + ")";
				}
				else
					return Token;
			}

			return base.ToString();
		}

		public bool Contains(ConditionInfo condition)
		{
			if (Kind == DirectiveParser.IDENT)
			{
				if (condition.IsEnable(Token))
					return true;
				if (condition.IsDisable(Token))
					return true;
				return false;
			}

			foreach (ExpressionNode node in this)
			{
				if (node.Contains(condition))
					return true;
			}

			return false;
		}

		public ExpressionNode Clone()
		{
			ExpressionNode result = new ExpressionNode();
			result.Token = Token;
			result.Kind = Kind;
			result.Parenthesis = Parenthesis;
			result.Defined = Defined;

			foreach (ExpressionNode child in this)
			{
				result.Add(child.Clone());
			}

			return result;
		}

		public bool ApplyCondition(ConditionInfo condition)
		{
			if (Kind == DirectiveParser.MUL)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (p0 == 0)
					{
						ToInt(0);
						return true;
					}
					else if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt(p0 * p1);
						return true;
					}
					else if (p0 == 1)
						CopyFrom(this[1]);
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 == 0)
					{
						ToInt(0);
						return true;
					}
					else if (p1 == 1)
						CopyFrom(this[0]);
				}
				return false;
			}
			else if (Kind == DirectiveParser.DIV)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (p0 == 0)
					{
						ToInt(0);
						return true;
					}
					else if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt(p0 / p1);
						return true;
					}
					else if (p0 == 1)
						CopyFrom(this[1]);
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 == 0)
					{
						throw new DivideByZeroException();
					}
					else if (p1 == 1)
						CopyFrom(this[0]);
				}
				return false;
			}
			else if (Kind == DirectiveParser.MOD)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (p0 == 0)
					{
						ToInt(0);
						return true;
					}
					else if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt(p0 % p1);
						return true;
					}
					else if (p0 == 1)
						CopyFrom(this[1]);
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 == 0)
					{
						throw new DivideByZeroException();
					}
					else if (p1 == 1)
						CopyFrom(this[0]);
				}
				return false;
			}
			else if (Kind == DirectiveParser.PLUS)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt(p0 + p1);
						return true;
					}
					else if (p0 == 0)
						CopyFrom(this[1]);
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 == 0)
						CopyFrom(this[0]);
				}
				return false;
			}
			else if (Kind == DirectiveParser.MINUS)
			{
				if (Count == 1)
				{
					if (this[0].ApplyCondition(condition))
					{
						int p0 = this[0].GetValue();
						ToInt(-p0);
						return true;
					}
				}
				else if (Count == 2)
				{
					if (this[0].ApplyCondition(condition))
					{
						int p0 = this[0].GetValue();
						if (this[1].ApplyCondition(condition))
						{
							int p1 = this[1].GetValue();
							ToInt(p0 - p1);
							return true;
						}
						else
							RemoveAt(0);
					}
					else if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						if (p1 == 0)
							CopyFrom(this[0]);
					}
				}
				return false;
			}
			else if (Kind == DirectiveParser.LS)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (p0 == 0)
					{
						ToInt(0);
						return true;
					}
					else if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt(p0 << p1);
						return true;
					}
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 == 0)
						CopyFrom(this[0]);
				}
				return false;
			}
			else if (Kind == DirectiveParser.RS)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (p0 == 0)
					{
						ToInt(0);
						return true;
					}
					else if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt(p0 >> p1);
						return true;
					}
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 == 0)
						CopyFrom(this[0]);
				}
				return false;
			}
			else if (Kind == DirectiveParser.LT)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt((p0 < p1) ? 1 : 0);
						return true;
					}
				}
				else
					this[1].ApplyCondition(condition);
				return false;
			}
			else if (Kind == DirectiveParser.GT)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt((p0 > p1) ? 1 : 0);
						return true;
					}
				}
				else
					this[1].ApplyCondition(condition);
				return false;
			}
			else if (Kind == DirectiveParser.LE)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt((p0 <= p1) ? 1 : 0);
						return true;
					}
				}
				else
					this[1].ApplyCondition(condition);
				return false;
			}
			else if (Kind == DirectiveParser.GE)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt((p0 >= p1) ? 1 : 0);
						return true;
					}
				}
				else
					this[1].ApplyCondition(condition);
				return false;
			}
			else if (Kind == DirectiveParser.EQ)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt((p0 == p1) ? 1 : 0);
						return true;
					}
				}
				else
					this[1].ApplyCondition(condition);
				return false;
			}
			else if (Kind == DirectiveParser.NE)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt((p0 != p1) ? 1 : 0);
						return true;
					}
				}
				else
					this[1].ApplyCondition(condition);
				return false;
			}
			else if (Kind == DirectiveParser.AND)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (p0 == 0)
					{
						ToInt(0);
						return true;
					}
					else if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt(p0 & p1);
						return true;
					}
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 == 0)
					{
						ToInt(0);
						return true;
					}
				}
				return false;
			}
			else if (Kind == DirectiveParser.ER)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt(p0 ^ p1);
						return true;
					}
					else if (p0 == 0)
					{
						CopyFrom(this[1]);
					}
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 == 0)
					{
						CopyFrom(this[0]);
					}
				}
				return false;
			}
			else if (Kind == DirectiveParser.OR)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt(p0 | p1);
						return true;
					}
					else if (p0 == 0)
					{
						CopyFrom(this[1]);
					}
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 == 0)
					{
						CopyFrom(this[0]);
					}
				}
				return false;
			}
			else if (Kind == DirectiveParser.ANDAND)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (p0 == 0)
					{
						ToInt(0);
						return true;
					}
					else if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt((p0 != 0 && p1 != 0) ? 1 : 0);
						return true;
					}
					else
						CopyFrom(this[1]);
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 == 0)
					{
						ToInt(0);
						return true;
					}
					else
						CopyFrom(this[0]);
				}
				return false;
			}
			else if (Kind == DirectiveParser.OROR)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (p0 != 0)
					{
						ToInt(1);
						return true;
					}
					else if (this[1].ApplyCondition(condition))
					{
						int p1 = this[1].GetValue();
						ToInt((p0 != 0 || p1 != 0) ? 1 : 0);
						return true;
					}
					else
						CopyFrom(this[1]);
				}
				else if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					if (p1 != 0)
					{
						ToInt(1);
						return true;
					}
					else
						CopyFrom(this[0]);
				}
				return false;
			}
			else if (Kind == DirectiveParser.QUEST)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					if (p0 != 0)
					{
						if (this[1].ApplyCondition(condition))
						{
							int p1 = this[1].GetValue();
							ToInt(p1);
							return true;
						}
						else
							CopyFrom(this[1]);
					}
					else
					{
						if (this[2].ApplyCondition(condition))
						{
							int p2 = this[2].GetValue();
							ToInt(p2);
							return true;
						}
						else
							CopyFrom(this[2]);
					}
				}
				else
				{
					this[1].ApplyCondition(condition);
					this[2].ApplyCondition(condition);
				}
				return false;
			}
			else if (Kind == DirectiveParser.CM)
			{
				if (this[1].ApplyCondition(condition))
				{
					int p1 = this[1].GetValue();
					ToInt(p1);
					return true;
				}
				return false;
			}
			else if (Kind == DirectiveParser.NOT)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					ToInt(p0 == 0 ? 1 : 0);
					return true;
				}
				return false;
			}
			else if (Kind == DirectiveParser.COMPL)
			{
				if (this[0].ApplyCondition(condition))
				{
					int p0 = this[0].GetValue();
					ToInt(~p0);
					return true;
				}
				return false;
			}
			else if (Kind == DirectiveParser.IDENT)
			{
				if (condition.IsEnable(Token))
				{
					ToInt(1);
					return true;
				}
				else if (condition.IsDisable(Token))
				{
					ToInt(0);
					return true;
				}
				return false;
			}

			return false;
		}

		private void ToInt(int value)
		{
			Clear();

			Token = value.ToString();
			Kind = DirectiveParser.INT;
			Parenthesis = false;
			Defined = false;
		}

		private void CopyFrom(ExpressionNode source)
		{
			Token = source.Token;
			Kind = source.Kind;
			Parenthesis = source.Parenthesis;
			Defined = source.Defined;

			Clear();
			AddRange(source);
		}
	}
}
