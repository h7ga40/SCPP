using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace SCPP
{
	class Program
	{
		static string BasePathName;
		static List<String> Disables = new List<string>();
		static List<String> Enables = new List<string>();
		static String Prifix = "_Changed";

		static void ReadH(DirectoryInfo f, int step)
		{
			FileInfo[] fc = f.GetFiles();
			foreach (FileInfo item in fc)
			{
				CSourceFileInfo File = new CSourceFileInfo(BasePathName, item.FullName);

				if (File.m_ExtName == ".h" || File.m_ExtName == ".cpp" || File.m_ExtName == ".c")
				{
					//Files.Add(File); // Files
					File.Disables = Disables;
					File.Enables = Enables;
					File.Prifix = Prifix;
					File.Read(step);
				}
			}

			DirectoryInfo[] di = f.GetDirectories();
			foreach (DirectoryInfo item in di)
			{
				ReadH(item, step);
			}
		}

		static void Main(string[] args)
		{
			String ret = "";
			String DirectoryPath = ".";
			int count = 0;
			int p_count = 0;

			try
			{
				foreach (string option in args)
				{
					if ((ret = trim(option, "-D")) != "")
					{
						if (Disables.Contains(ret) == false)
						{
							Disables.Add(ret);
						}
					}
					else if ((ret = trim(option, "-E")) != "")
					{
						if (Enables.Contains(ret) == false)
						{
							Enables.Add(ret);
						}
					}
					else if ((ret = trim(option, "-P")) != "")
					{
						if (p_count == 0)
						{
							p_count++;
							Prifix = ret;
						}
						else
						{
							return;
						}
					}
					else
					{
						if (count == 0)
						{
							count++;
							DirectoryPath = option;
						}
						else
						{
							return;
						}
					}
				}

				// Listを昇順ソート
				Disables.Sort();
				Enables.Sort();

				DirectoryInfo f = new DirectoryInfo(DirectoryPath);

				if (!f.Exists)
					return;

				BasePathName = f.FullName;

				DirectoryInfo t = new DirectoryInfo(BasePathName + Prifix);
				if (t.Exists)
				{
					t.Delete(true);
				}

				ReadH(f, 0);

				ReadH(f, 1);
			}
			catch
			{
				;
			}
		}

		static string trim(string target, string trim_str)
		{
			string valuse = "";

			if (target.StartsWith(trim_str))
			{
				// 引数trim_strの文字列をtargetの頭からトリミング
				valuse = target.Substring(trim_str.Length);
			}
			return valuse;
		}
	}
}
