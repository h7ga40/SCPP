using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CSYacc
{
	class Program
	{
		static readonly string DEFAULT_TEMPLATE_FILE = "-";

		/* defines for constructing filenames */

		static readonly string VERBOSE_SUFFIX = ".output";
		static readonly string TRACE_SUFFIX = ".trace";

		static string FilePrefix = "y";

		static string VerboseFileName;
		static string TemplateFileName;
		static string TraceFileName;

		static void GetArgs(Yacc.Yacc<string> main, string[] argv)
		{
			int i;
			int s;

			//if (argv.Length > 0) main.MyName = argv[0];
			for (i = 0; i < argv.Length; ++i) {
				s = 0;
				if (argv[i][s] != '-') break;
				switch (argv[i][++s]) {
				case '\0':
					m_Reader.InputFile = Console.In;
					if (i < argv.Length) Usage(main);
					return;

				case '-':
					++i;
					goto no_more_options;

				case 'b':
					if (argv[i][++s] != '\0')
						FilePrefix = argv[i].Substring(s);
					else if (++i < argv.Length)
						FilePrefix = argv[i];
					else
						Usage(main);
					continue;

				case 't':
					main.tFlag = true;
					break;

				case 'c':
					m_Output.LineFormat = "#line {0} \"{1}\"\n"; /* duplicate below */
					break;

				case 'v':
					main.vFlag = true;
					break;

				case '0':
					m_Reader.ZeroBase = true;
					break;

				default:
					Usage(main);
					break;
				}

				for (;;) {
					s++;
					if (s >= argv[i].Length)
						break;

					switch (argv[i][s]) {
					case 't':
						main.tFlag = true;
						break;

					case 'v':
						main.vFlag = true;
						break;

					case 'c':
						m_Output.LineFormat = "#line {0} \"{1}\"\n"; /* duplicate above */
						break;

					default:
						Usage(main);
						break;
					}
				}
			}

			no_more_options:;

			if (i + 1 > argv.Length || i + 2 < argv.Length) Usage(main);
			m_Reader.InputFileName = argv[i];
			TemplateFileName = i + 2 > argv.Length ? DEFAULT_TEMPLATE_FILE : argv[i + 1];
		}

		static void Usage(Yacc.Yacc<string> main)
		{
			Console.Error.Write("usage: {0} [-ctv] [-b file_prefix] filename [skeleton]\n", main.MyName);
			Environment.Exit(1);
		}

		static void CreateFileNames()
		{
			int len;

			len = FilePrefix.Length;

			VerboseFileName = FilePrefix;
			VerboseFileName += VERBOSE_SUFFIX;

			TraceFileName = FilePrefix;
			TraceFileName += TRACE_SUFFIX;
		}

		static void OpenFiles(Yacc.Yacc<string> main)
		{
			if (m_Reader.InputFile == null) {
				m_Reader.InputFile = new StreamReader(m_Reader.InputFileName);
			}
			main.Error.InputFileName = m_Reader.InputFileName;

			if (main.vFlag) {
				m_Output.VerboseWriter = new StreamWriter(VerboseFileName);
			}

			m_Output.TemplateReader = String.Compare(TemplateFileName, "-") == 0 ? Console.In : new StreamReader(TemplateFileName);

			main.TraceWriter = new StreamWriter(TraceFileName);
		}

		static void Done(Yacc.Yacc<string> main)
		{
			if (m_Output.VerboseWriter != null) { m_Output.VerboseWriter.Close(); }
			if (main.TraceWriter != null) { main.TraceWriter.Close(); }
		}

		static Yacc.Output m_Output;
		static Yacc.Reader m_Reader;

		static void Main(string[] args)
		{
			Yacc.Yacc<string> main = new Yacc.Yacc<string>();
			m_Output = new Yacc.Output(main);
			m_Reader = new Yacc.Reader(main, m_Output);
			m_Output.Out = Console.Out;

			GetArgs(main, args);

			CreateFileNames();

			try {
				OpenFiles(main);
				main.Grammar = m_Reader;
				main.Execute();
				m_Output.Execute();
			}
			catch (Exception) {
			}

			main.Error.Errors.Write(Console.Error);

			Done(main);
		}
	}
}
