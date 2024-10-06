using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;
using System.Windows.Forms;

namespace zipcopy
{
	class Uty
	{
		public bool IsAdministrator()
		{
			var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
			var principal = new System.Security.Principal.WindowsPrincipal(identity);
			return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
		}

		public bool GetParam(ref string strDir)
		{
			bool stat = false;
			string[] files = Environment.GetCommandLineArgs();
			if (files.Length <= 1)
				return stat;
			if (Directory.Exists(files[1]))
			{
				strDir = files[1];
				stat = true;
			}
			else if (File.Exists(files[1]))
			{
				strDir = files[1];
				stat = true;
			}
			return stat;
		}

		// 時間計測開始＆終了
		public Stopwatch timeStart()
		{
			Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			return sw;
		}
		public double timeEnd(Stopwatch sw)
		{
			sw.Stop();
			TimeSpan ts = sw.Elapsed;
			double time = (double)ts.TotalMilliseconds / 1000.0;
			time = Math.Round(time, 2, MidpointRounding.AwayFromZero);
			return time;
		}

		public long GetDirectorySize(DirectoryInfo dirInfo)
		{
			long size = 0;
			foreach (FileInfo fi in dirInfo.GetFiles())
				size += fi.Length;
			foreach (DirectoryInfo di in dirInfo.GetDirectories())
				size += GetDirectorySize(di);
			return size;
		}

		public bool IsSrcFolder(string path)
		{
			bool stat = true;
			if (File.Exists(path))
				stat = false;
			else if (Directory.Exists(path))
				stat = true;
			return stat;
		}

		public string GetZipName(string srcPath)
		{
			string ans;
			if (IsSrcFolder(srcPath))
				ans = srcPath + ".zip";
			else
				ans = Path.GetDirectoryName(srcPath) + "\\" + Path.GetFileNameWithoutExtension(srcPath) + ".zip";
			if (!File.Exists(ans))
				return ans;
			for (int i = 2; ; i++)
			{
				ans = srcPath + "(" + i.ToString() + ")" + ".zip";
				if (!File.Exists(ans))
					return ans;
			}
		}

		public void DelRegSubkey(string key)
		{
			RegistryKey regkey = Registry.CurrentUser.OpenSubKey(key, false);
			if (regkey != null)
				Registry.ClassesRoot.DeleteSubKeyTree(key);
		}

		// ログ追加
		public void addLog(ListView lv, DateTime dt, string operation, string src, string dst, bool stat, double time, string detail)
		{
			ListViewItem lvi;
			lvi = lv.Items.Add(dt.ToString());
			lvi.SubItems.Add(time.ToString());
			lvi.SubItems.Add(operation);
			lvi.SubItems.Add(src);
			lvi.SubItems.Add(dst);
			lvi.SubItems.Add(stat ? "成功" : "失敗");
			lvi.SubItems.Add(detail);
			lv.EnsureVisible(lvi.Index);
			lv.Update();
		}

		public bool FileDelete(ListView lv, string file, int timeout)
		{
			string detail = "";
			bool stat = true;
			Stopwatch sw = timeStart();
			DateTime dt = DateTime.Now;
			for (int i = 0; i < timeout; i++)  // トータル30秒リトライする
			{
				try
				{
					stat = true;
					File.Delete(file);
				}
				catch (Exception ex)
				{
					stat = false;
					Thread.Sleep(1000);  // 1秒待ってリトライ
					if (i == timeout - 1)
						detail = string.Format("例外エラー（{0}, {1}）", ex.Message, file);
				}
				if (stat)
					break;
			}
			if (!stat)
				addLog(lv, dt, "ファイル削除", "", "", stat, timeEnd(sw), detail);
			return stat;
		}

	}
}
