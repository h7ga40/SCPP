using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SCPP
{
	class SelectInfo
	{
		public enum DirectiveKind
		{
			IFDEF,
			IFNDEF,
			IF,
			ELIF,
			ELSE,
			ENDIF,
		}

		private static DirectiveParser m_Parser;

		public DirectiveKind Kind;
		public LineInfo Line;
		public ExpressionNode Condition;
		public SelectInfo Parent;
		public List<SelectInfo> Group;
		public ExpressionNode NewCondition;
		internal bool m_IsDeleteDirective;
		internal bool m_IsTarget;
		internal bool m_IsSelected;

		static SelectInfo()
		{
			m_Parser = new DirectiveParser();
		}

		public void SetCondition(string condition)
		{
			StringReader sr = new StringReader(condition);
			Scanner sc = new Scanner(sr);

			Condition = m_Parser.Parse(sc/*, new DebugAdapter()*/);
		}

		public void ApplyCondition(ConditionInfo condition)
		{
			NewCondition = Condition.Clone();

			m_IsTarget = Condition.Contains(condition);

			if (m_IsTarget)
			{
				m_IsDeleteDirective = NewCondition.ApplyCondition(condition);
				if (NewCondition.Kind == DirectiveParser.INT)
					m_IsSelected = NewCondition.GetValue() != 0;
				else
					m_IsSelected = true;
			}
			else
			{
				m_IsDeleteDirective = false;
				m_IsSelected = true;
			}
		}

		public bool IsTarget()
		{
			if (Parent != null && Parent.IsTarget())
				return true;

			if (m_IsTarget)
				return true;

			return false;
		}

		public bool IsSelected()
		{
			if (Parent != null && !Parent.IsSelected())
				return false;

			return !m_IsTarget || m_IsSelected;
		}

		public string GetNewCondition()
		{
			return NewCondition.ToString();
		}

		public string GetNewNotCondition()
		{
			if (NewCondition.Kind == DirectiveParser.NOT)
				return NewCondition[0].ToString();

			if ((NewCondition.Parenthesis) || (NewCondition.Kind == DirectiveParser.IDENT))
				return "!" + NewCondition.ToString();

			return "!(" + NewCondition.ToString() + ")";
		}
	}

	class LineInfo
	{
		public int LineNo;
		public string Line;
		public List<LineInfo> CombineLines = new List<LineInfo>();
		private SelectInfo m_Selecter;

		public SelectInfo Selecter
		{
			get { return m_Selecter; }
			set
			{
				m_Selecter = value;
				foreach (LineInfo info in CombineLines)
				{
					info.Selecter = value;
				}
			}
		}

		public string CombineLine
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				if (Line.EndsWith("\\"))
					sb.Append(Line.Substring(0, Line.Length - 1));
				else
					sb.Append(Line);
				foreach (LineInfo line in CombineLines)
				{
					sb.AppendLine();
					if (line.Line.EndsWith("\\"))
						sb.Append(line.Line.Substring(0, line.Line.Length - 1));
					else
						sb.Append(line.Line);
				}
				return sb.ToString();
			}
		}

		internal bool IsOutputLine(out string line)
		{
			if (m_Selecter == null || !m_Selecter.IsTarget())
			{
				line = Line;
				return true;
			}

			bool select = m_Selecter.IsSelected();

			if (m_Selecter.Line != this)
			{
				if (select)
					line = Line;
				else
					line = null;
				return select;
			}

			switch (m_Selecter.Kind)
			{
				case SelectInfo.DirectiveKind.IFDEF:
					if (select && !m_Selecter.m_IsDeleteDirective)
						line = "#ifdef " + m_Selecter.GetNewCondition();
					else
						line = null;
					break;
				case SelectInfo.DirectiveKind.IFNDEF:
					if (select && !m_Selecter.m_IsDeleteDirective)
						line = "#ifndef " + m_Selecter.GetNewNotCondition();
					else
						line = null;
					break;
				case SelectInfo.DirectiveKind.IF:
					if (select && !m_Selecter.m_IsDeleteDirective)
						line = "#if " + m_Selecter.GetNewCondition();
					else
						line = null;
					break;
				case SelectInfo.DirectiveKind.ELIF:
					if (select && !m_Selecter.m_IsDeleteDirective)
						line = "#elif " + m_Selecter.GetNewCondition();
					else
						line = null;
					break;
				case SelectInfo.DirectiveKind.ELSE:
					if (select && !m_Selecter.m_IsDeleteDirective)
						line = "#else";
					else
						line = null;
					break;
				case SelectInfo.DirectiveKind.ENDIF:
					if (select && !m_Selecter.m_IsDeleteDirective)
						line = "#endif";
					else
						line = null;
					break;
				default:
					if (select)
						line = Line;
					else
						line = null;
					break;
			}


			return line != null;
		}

		public void ApplyCondition(ConditionInfo condition)
		{
			if (m_Selecter != null && m_Selecter.NewCondition == null)
				m_Selecter.ApplyCondition(condition);
		}
	}

	class CSourceFileInfo
	{
		string m_BasePathName;
		string m_FileName;
		string m_PathName;
		public string m_ExtName;
		List<LineInfo> m_InputLines = new List<LineInfo>();
		bool m_Changed;
		List<List<SelectInfo>> m_SelectGroups = new List<List<SelectInfo>>();
		public List<String> Disables;
		public List<String> Enables;
		public String Prifix;

		enum State
		{
			PLAIN, STRING, IN_QUOTE, START_COMMENT, COMMENT, END_COMMENT, LINE_COMMENT
		};

		SelectInfo m_Selecter;
		State m_State = State.PLAIN;

		public CSourceFileInfo(string BasePathName, string PathName)
		{
			m_BasePathName = BasePathName;
			m_PathName = PathName;
			m_ExtName = Path.GetExtension(m_PathName).ToLower();
			m_FileName = Path.GetFileNameWithoutExtension(m_PathName);
			m_PathName = m_PathName.Substring(0, m_PathName.Length - m_ExtName.Length);
		}

		public void Read(int step)
		{
			string inText;
			string PathName = m_PathName + m_ExtName;

			if (step != 0)
			{
				PathName = PathName.Substring(m_BasePathName.Length);
				PathName = m_BasePathName + Prifix + PathName;
				FileInfo fi = new FileInfo(PathName);
				if (!fi.Exists)
					PathName = m_PathName + m_ExtName;
			}

			using (StreamReader input = new StreamReader(PathName, Encoding.UTF8))
			{
				while ((inText = input.ReadLine()) != null)
				{
					LineInfo lineInfo = new LineInfo();
					lineInfo.LineNo = m_InputLines.Count + 1;
					lineInfo.Line = inText;
					m_InputLines.Add(lineInfo);
				}
				input.Close();
			}

			m_Changed = false;

			switch (step)
			{
				case 0:
					if (!Process())
						m_Changed = false;
					ConditionInfo condition = new ConditionInfo();
					condition.Default = true;
					condition.Enables.AddRange(Enables);
					condition.Disables.AddRange(Disables);
					ApplyCondition(condition);
					break;
				default:
					return;
			}

			if (m_Changed)
			{
				PathName = m_PathName + m_ExtName;
				PathName = PathName.Substring(m_BasePathName.Length);
				PathName = m_BasePathName + Prifix + PathName;

				string DirName = Path.GetDirectoryName(PathName);
				DirectoryInfo di = new DirectoryInfo(DirName);

				if (!di.Exists)
				{
					di.Create();
				}

				using (StreamWriter output = new StreamWriter(PathName, false, Encoding.UTF8))
				{
					string outText = DeleteOption();
					output.Write(outText);
					output.Close();
				}
			}
		}

		private bool Process()
		{
			LineInfo startLine = null;
			bool Comb = false;

			foreach (LineInfo lineInfo in m_InputLines)
			{
				string line = lineInfo.Line;

				for (int q = 0; q < line.Length; q++)
					switch (m_State)
					{
						case State.PLAIN:
							switch (line[q])
							{
								case '"':
									m_State = State.STRING;
									break;
								case '\'':
									m_State = State.IN_QUOTE;
									break;
								case '/':
									m_State = State.START_COMMENT;
									break;
								case '\\':
									q++;
									break;
							}
							break;
						case State.STRING:
							if (line[q] == '"')
								m_State = State.PLAIN;
							else if (line[q] == '\\')
								q++;
							break;
						case State.IN_QUOTE:
							if (line[q] == '\'')
								m_State = State.PLAIN;
							else if (line[q] == '\\')
								q++;
							break;
						case State.START_COMMENT:
							if (line[q] == '*')
								m_State = State.COMMENT;
							else if (line[q] == '/')
								m_State = State.LINE_COMMENT;
							else
								m_State = State.PLAIN;
							break;
						case State.COMMENT:
							if (line[q] == '*')
								m_State = State.END_COMMENT;
							else
								m_State = State.COMMENT;
							break;
						case State.END_COMMENT:
							if (line[q] == '/')
								m_State = State.PLAIN;
							else if (line[q] != '*')
								m_State = State.COMMENT;
							break;
						case State.LINE_COMMENT:
							q++;
							break;
					}

				if (startLine == null)
					startLine = lineInfo;
				else if (Comb)
					startLine.CombineLines.Add(lineInfo);

				if (lineInfo.LineNo < m_InputLines.Count && line.EndsWith("\\"))
				{
					Comb = true;
					continue;
				}
				else
					Comb = false;

				if (m_State == State.START_COMMENT)
					m_State = State.PLAIN;
				else if (m_State == State.END_COMMENT)
					m_State = State.COMMENT;
				else if (m_State == State.LINE_COMMENT)
					m_State = State.PLAIN;

				switch (m_State)
				{
					case State.PLAIN:
						string tmp = startLine.CombineLine.Trim();
						if (tmp.Length > 0 && tmp[0] == '#')
						{
							if (!Process(startLine, tmp.Substring(1)))
								return false;
						}
						break;
					case State.COMMENT:
						break;
					default:
						return false;
				}

				if (startLine.Selecter == null)
					startLine.Selecter = m_Selecter;

				startLine = null;
			}

			return true;
		}

		private bool Process(LineInfo lineInfo, string str)
		{
			int s, e;

			for (s = 0; s < str.Length; s++)
				if (str[s] != ' ' && str[s] != '\t')
					break;

			for (e = s; e < str.Length; e++)
				if (!Char.IsLetterOrDigit(str[e]) && str[e] != '_')
					break;

			string directive = str.Substring(s, e - s);
			string param = str.Substring(e).Trim();

			switch (directive)
			{
				case "if":
					return ProcessIf(lineInfo, param);
				case "ifdef":
					return ProcessIfdef(lineInfo, param);
				case "ifndef":
					return ProcessIfndef(lineInfo, param);
				case "elif":
					return ProcessElif(lineInfo, param);
				case "else":
					return ProcessElse(lineInfo, param);
				case "endif":
					return ProcessEndif(lineInfo, param);
				default:
					break;
			}

			return true;
		}

		private bool ProcessIf(LineInfo lineInfo, string condition)
		{
			SelectInfo si = new SelectInfo();
			si.Kind = SelectInfo.DirectiveKind.IF;
			si.Line = lineInfo;
			si.SetCondition(condition);
			si.Parent = m_Selecter;
			si.Group = new List<SelectInfo>();
			si.Group.Add(si);

			m_Selecter = si;
			m_SelectGroups.Add(si.Group);

			return true;
		}

		private bool ProcessIfdef(LineInfo lineInfo, string condition)
		{
			SelectInfo si = new SelectInfo();
			si.Kind = SelectInfo.DirectiveKind.IFDEF;
			si.Line = lineInfo;
			si.SetCondition(condition);
			si.Parent = m_Selecter;
			si.Group = new List<SelectInfo>();
			si.Group.Add(si);

			m_Selecter = si;
			m_SelectGroups.Add(si.Group);

			return true;
		}

		private bool ProcessIfndef(LineInfo lineInfo, string condition)
		{
			SelectInfo si = new SelectInfo();
			si.Kind = SelectInfo.DirectiveKind.IFNDEF;
			si.Line = lineInfo;
			si.SetCondition("!" + condition);
			si.Parent = m_Selecter;
			si.Group = new List<SelectInfo>();
			si.Group.Add(si);

			m_Selecter = si;
			m_SelectGroups.Add(si.Group);

			return true;
		}

		private bool ProcessElif(LineInfo lineInfo, string condition)
		{
			SelectInfo lsi = m_Selecter;
			SelectInfo si = new SelectInfo();
			si.Kind = SelectInfo.DirectiveKind.ELIF;
			si.Line = lineInfo;
			si.SetCondition(condition);
			si.Parent = lsi.Parent;
			si.Group = lsi.Group;
			si.Group.Add(si);

			m_Selecter = si;

			return true;
		}

		private bool ProcessElse(LineInfo lineInfo, string condition)
		{
			SelectInfo lsi = m_Selecter;
			SelectInfo si = new SelectInfo();
			si.Kind = SelectInfo.DirectiveKind.ELSE;
			si.Line = lineInfo;
			si.SetCondition("1");
			si.Parent = lsi.Parent;
			si.Group = lsi.Group;
			si.Group.Add(si);

			m_Selecter = si;

			return true;
		}

		private bool ProcessEndif(LineInfo lineInfo, string condition)
		{
			SelectInfo lsi = m_Selecter;
			SelectInfo si = new SelectInfo();
			si.Kind = SelectInfo.DirectiveKind.ENDIF;
			si.Line = lineInfo;
			si.SetCondition("1");
			si.Parent = lsi.Parent;
			si.Group = lsi.Group;
			si.Group.Add(si);

			lineInfo.Selecter = si;

			m_Selecter = lsi.Parent;

			return true;
		}

		private void ApplyCondition(ConditionInfo condition)
		{
			foreach (List<SelectInfo> group in m_SelectGroups)
			{
				bool target = false;
				bool deldir = false;
				List<SelectInfo> select = new List<SelectInfo>();

				foreach (SelectInfo si in group)
				{
					si.ApplyCondition(condition);

					if (!target && si.m_IsTarget)
						target = true;

					if (!deldir && si.m_IsDeleteDirective)
						deldir = true;

					if (deldir && (si.Kind == SelectInfo.DirectiveKind.ELSE)
						&& (select.Count > 0))
						si.m_IsSelected = false;

					if (si.m_IsSelected)
						select.Add(si);
				}

				if (!target)
					continue;

				m_Changed = true;

				if (select.Count == 1)
				{
					SelectInfo si = select[0];
					if (si.Kind != SelectInfo.DirectiveKind.ENDIF)
						throw new Exception();

					si.m_IsSelected = false;
					select.Clear();
					deldir = true;
				}
				else if (deldir)
				{
					if (select.Count != 2)
						deldir = false;
					else if (select[0].Kind == SelectInfo.DirectiveKind.ELIF
						&& !select[0].m_IsDeleteDirective)
						deldir = false;
					else if ((select[0].Kind == SelectInfo.DirectiveKind.IF)
						&& !select[0].m_IsDeleteDirective)
						deldir = false;
				}

				int step = 0;
				foreach (SelectInfo si in group)
				{
					bool orgdeldir = si.m_IsDeleteDirective;

					si.m_IsTarget = true;
					si.m_IsDeleteDirective = deldir;

					if (!select.Contains(si))
						continue;

					switch (step)
					{
						case 0:
							switch (si.Kind)
							{
								case SelectInfo.DirectiveKind.IF:
								case SelectInfo.DirectiveKind.ELIF:
									si.Kind = SelectInfo.DirectiveKind.IF;
									step = 1;
									break;
								case SelectInfo.DirectiveKind.IFDEF:
									if (si.NewCondition.Kind == DirectiveParser.INT)
										si.Kind = SelectInfo.DirectiveKind.IF;
									else
										si.Kind = SelectInfo.DirectiveKind.IFDEF;
									step = 1;
									break;
								case SelectInfo.DirectiveKind.IFNDEF:
									if (si.NewCondition.Kind == DirectiveParser.INT)
										si.Kind = SelectInfo.DirectiveKind.IF;
									else
										si.Kind = SelectInfo.DirectiveKind.IFNDEF;
									step = 1;
									break;
								case SelectInfo.DirectiveKind.ELSE:
									si.Kind = SelectInfo.DirectiveKind.IF;
									step = 2;
									break;
								default:
									throw new Exception();
							}
							break;
						case 1:
							switch (si.Kind)
							{
								case SelectInfo.DirectiveKind.ELSE:
									step = 2;
									break;
								case SelectInfo.DirectiveKind.ELIF:
									if (orgdeldir)
									{
										si.Kind = SelectInfo.DirectiveKind.ELSE;
										step = 10;
									}
									break;
								case SelectInfo.DirectiveKind.ENDIF:
									step = 3;
									break;
								default:
									throw new Exception();
							}
							break;
						case 2:
							switch (si.Kind)
							{
								case SelectInfo.DirectiveKind.ENDIF:
									step = 3;
									break;
								default:
									throw new Exception();
							}
							break;
						case 3:
							throw new Exception();
						case 10:
							switch (si.Kind)
							{
								case SelectInfo.DirectiveKind.ELSE:
									si.m_IsSelected = false;
									break;
								case SelectInfo.DirectiveKind.ELIF:
									si.m_IsSelected = false;
									break;
								case SelectInfo.DirectiveKind.ENDIF:
									step = 3;
									break;
								default:
									throw new Exception();
							}
							break;
					}
				}
			}
		}

		public string DeleteOption()
		{
			StringBuilder sb = new StringBuilder();

			foreach (LineInfo lineInfo in m_InputLines)
			{
				string line;

				if (lineInfo.IsOutputLine(out line))
				{
					sb.AppendLine(line);
				}
			}

			return sb.ToString();
		}
	}
}
