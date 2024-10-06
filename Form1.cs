using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Threading;
using Ionic.Zip;
using Ionic.Zlib;
using Microsoft.Win32;

namespace zipcopy
{
	public partial class Form1 : Form
	{
		Option opt = new Option();
		Uty uty = new Uty();
		private bool exec = false;

		delegate void logInfo(DateTime dt, string operation, string src, string dst, bool stat, double time, string detail);
		public void addLogDelegate(DateTime dt, string operation, string src, string dst, bool stat, double time, string detail)
		{
			ListViewItem lvi;
			lvi = listView1.Items.Add(dt.ToString());
			lvi.SubItems.Add(time.ToString());
			lvi.SubItems.Add(operation);
			lvi.SubItems.Add(src);
			lvi.SubItems.Add(dst);
			lvi.SubItems.Add(stat ? "成功" : "失敗");
			lvi.SubItems.Add(detail);
			listView1.EnsureVisible(lvi.Index);
			listView1.Update();
		}

		protected override void WndProc(ref Message m)  // 実行中はクローズボタンを無効にする
		{
			const int WM_SYSCOMMAND = 0x112;
			const long SC_CLOSE = 0xF060L;
			if (exec == true && m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt64() & 0xFFF0L) == SC_CLOSE)
				return;
			base.WndProc(ref m);
		}

		public Form1()
		{
			InitializeComponent();
			opt.LoadReg();

			Bitmap btm1 = new Bitmap(Properties.Resources.img_file);
			Bitmap btm2 = new Bitmap(Properties.Resources.img_folder);
			btm1.MakeTransparent();
			btm2.MakeTransparent();
			button1.Image = btm2;
			button2.Image = btm2;
			button7.Image = btm1;

			ToolTip toolTipMsg = new ToolTip();
			toolTipMsg.SetToolTip(button1, "フォルダを選択");
			toolTipMsg.SetToolTip(button2, "フォルダを選択");
			toolTipMsg.SetToolTip(button7, "ファイルを選択（1つのみ）");

			string strDir = "";
			if (uty.GetParam(ref strDir))  // パラメータで渡ってきたものがフォルダなら、コピー元フォルダにする
				opt.m_strCopySrc = strDir;

			textBox1.Text = opt.m_strCopySrc;
			textBox2.Text = opt.m_strCopyDst;
			checkBox1.Checked = (opt.m_iSrcZipDel == 1) ? true : false;
			checkBox2.Checked = (opt.m_iDstZipOver == 1) ? true : false;
			checkBox3.Checked = (opt.m_iDstZipOpen == 1) ? true : false;
			checkBox4.Checked = (opt.m_iDstZipDel == 1) ? true : false;
			if (checkBox3.Checked)
				checkBox4.Enabled = true;
			else
				checkBox4.Enabled = false;
			checkBox5.Checked = (opt.m_i7zipUse == 1) ? true : false;
			textBox3.Text = opt.m_str7zipPath2;
			if (checkBox5.Checked)
			{
				textBox3.Enabled = true;
				button4.Enabled = true;
			}
			else
			{
				textBox3.Enabled = false;
				button4.Enabled = false;
			}
			UpdateExecButton();

			listView1.View = View.Details;
			for( int i=0; i<7; i++)
				listView1.Columns.Add(opt.m_strList[i], opt.m_List[i]);
			textBox4.Text = opt.m_iDelTimeout.ToString();
			checkBox6.Checked = (opt.m_iSuccessClose == 1) ? true : false;

			if ( !uty.IsAdministrator() )
			{
				button5.Visible = false;
				button6.Visible = false;
			}
			else
				EnabledMenuAddBtn();
		}

		private void UpdateOption()
		{
			opt.m_strCopySrc = textBox1.Text;
			opt.m_strCopyDst = textBox2.Text;
			opt.m_str7zipPath2 = textBox3.Text;
			opt.m_iSrcZipDel = (checkBox1.Checked == true) ? 1 : 0;
			opt.m_iDstZipOver = (checkBox2.Checked == true) ? 1 : 0;
			opt.m_iDstZipOpen = (checkBox3.Checked == true) ? 1 : 0;
			opt.m_iDstZipDel = (checkBox4.Checked == true) ? 1 : 0;
			opt.m_i7zipUse = (checkBox5.Checked == true) ? 1 : 0;
			for (int i = 0; i < 7; i++)
				opt.m_List[i] = listView1.Columns[i].Width;
			if (int.TryParse(textBox4.Text, out opt.m_iDelTimeout) != true)
				opt.m_iDelTimeout = -1;
			opt.m_iSuccessClose = (checkBox6.Checked == true) ? 1 : 0;
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			zipcopy.Properties.Settings.Default.viewState = WindowState;
			if (WindowState == FormWindowState.Normal)
			{
				// ウインドウステートがNormalな場合には位置（location）とサイズ（size）を記憶する
				zipcopy.Properties.Settings.Default.viewLocation = Location;
				zipcopy.Properties.Settings.Default.viewSize = Size;
			}
			else if (WindowState == FormWindowState.Maximized)
			{
				// 最大化（maximized）の場合には、RestoreBoundsを記憶する
				zipcopy.Properties.Settings.Default.viewLocation = RestoreBounds.Location;
				zipcopy.Properties.Settings.Default.viewSize = RestoreBounds.Size;
			}
			else  // 最小化は保存しない
				return;
			zipcopy.Properties.Settings.Default.Save();

			UpdateOption();
			opt.SaveReg();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// この設定は無くても良い
			if (zipcopy.Properties.Settings.Default.viewSize.Width == 0)
				zipcopy.Properties.Settings.Default.Upgrade();
			// もしC#デスクトップアプリをバージョンアップすると、記憶している情報が消え去るが、この↑を
			// 入れておくと引き継がれる（らしい）。

			if (zipcopy.Properties.Settings.Default.viewSize.Width == 0 || zipcopy.Properties.Settings.Default.viewSize.Height == 0)
			{
				// 初回起動時にはここに来るので必要なら初期値を与えても良い。
				// 何も与えない場合には、デザイナーウインドウで指定されている大きさになる。
			}
			else
			{
				WindowState = zipcopy.Properties.Settings.Default.viewState;

				// もし前回終了時に最小化されていても、今回起動時にはNormal状態にしておく
				if (WindowState == FormWindowState.Minimized)
					WindowState = FormWindowState.Normal;
				Location = zipcopy.Properties.Settings.Default.viewLocation;
				Size = zipcopy.Properties.Settings.Default.viewSize;
			}
		}

		private void UpdateExecButton()
		{
			bool enbl = true;
			if (textBox1.Text == "")
				enbl = false;
			if (textBox2.Text == "")
				enbl = false;
			if (opt.m_i7zipUse == 1 && textBox3.Text == "")
				enbl = false;
			button3.Enabled = enbl;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (textBox1.Text != "")
			{
				if (uty.IsSrcFolder(textBox1.Text))
					ofd.InitialDirectory = textBox1.Text;
				else
					ofd.InitialDirectory = Path.GetDirectoryName(textBox1.Text);
			}
			ofd.Filter = "フォルダ|.";
			ofd.Title = "コピー元のフォルダを選択";
			ofd.FileName = "フォルダを選択";
			ofd.CheckFileExists = false;
			ofd.RestoreDirectory = true;
			if (ofd.ShowDialog() != DialogResult.OK)
			{
				ofd.Dispose();
				return;
			}
			textBox1.Text = Path.GetDirectoryName(ofd.FileName);
			ofd.Dispose();
			UpdateExecButton();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (textBox2.Text != "")
				ofd.InitialDirectory = textBox2.Text;
			ofd.Filter = "フォルダ|.";
			ofd.Title = "コピー先のフォルダを選択";
			ofd.FileName = "フォルダを選択";
			ofd.CheckFileExists = false;
			ofd.RestoreDirectory = true;
			if (ofd.ShowDialog() != DialogResult.OK)
			{
				ofd.Dispose();
				return;
			}
			textBox2.Text = Path.GetDirectoryName(ofd.FileName);
			ofd.Dispose();
			UpdateExecButton();
		}

		private void button7_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (textBox1.Text != "")
			{
				if(uty.IsSrcFolder(textBox1.Text))
					ofd.InitialDirectory = textBox1.Text;
				else
					ofd.InitialDirectory = Path.GetDirectoryName(textBox1.Text);
			}
			ofd.Filter = "すべて|*.*";
			ofd.Title = "コピー元のファイルを選択";
			ofd.RestoreDirectory = true;
			if (ofd.ShowDialog() != DialogResult.OK)
			{
				ofd.Dispose();
				return;
			}
			textBox1.Text = ofd.FileName;
			ofd.Dispose();
			UpdateExecButton();
		}

		[System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		public static extern bool GetDiskFreeSpaceEx(
			string lpDirectoryName,
			out ulong lpFreeBytesAvailable,
			out ulong lpTotalNumberOfBytes,
			out ulong lpTotalNumberOfFreeBytes);

		private bool isEnoughDiskSpace()
		{
			long srcsize = 0;
			if (uty.IsSrcFolder(opt.m_strCopySrc))
			{
				// コピー元フォルダのサイズ
				DirectoryInfo di = new DirectoryInfo(opt.m_strCopySrc);
				srcsize = uty.GetDirectorySize(di);
			}
			else
			{
				// コピー元ファイルのサイズ
				FileInfo file = new FileInfo(opt.m_strCopySrc);
				srcsize = file.Length;
			}
			// コピー先の空き容量取得
			long dstfreesize = 0;
			string sRoot = Path.GetPathRoot(opt.m_strCopyDst);
			if (sRoot.Length > 2 && sRoot[1] == ':' && sRoot[2] == '\\')  // ドライブ表記だ
			{
				DriveInfo drive = new DriveInfo(sRoot.Substring(0));
				if (drive.IsReady)
					dstfreesize = drive.TotalFreeSpace;
			}
			else  // UNCパスだ
			{
				ulong freeBytesAvailable = 0;
				ulong totalNumberOfBytes = 0;
				ulong totalNumberOfFreeBytes = 0;
				GetDiskFreeSpaceEx(sRoot, out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes);
				dstfreesize = (long)freeBytesAvailable;
			}
			if (srcsize <= dstfreesize)
				return true;
			return false;
		}

		private bool PreCheck(ref string err)
		{
			if (uty.IsSrcFolder(opt.m_strCopySrc))
			{
				if (!Directory.Exists(opt.m_strCopySrc))
				{
					err = "コピー元フォルダがありません。";
					return false;
				}
			}
			else
			{
				if (!File.Exists(opt.m_strCopySrc))
				{
					err = "コピー元ファイルがありません。";
					return false;
				}
			}
			if (uty.IsSrcFolder(opt.m_strCopySrc) && !Directory.EnumerateFileSystemEntries(opt.m_strCopySrc).Any())
			{
				err = "コピー元フォルダが空です。";
				return false;
			}
			if (!Directory.Exists(opt.m_strCopyDst))
			{
				err = "コピー先フォルダがありません。";
				return false;
			}
			if (opt.m_i7zipUse == 1 && !File.Exists(opt.m_str7zipPath2))
			{
				err = "7zipのexeファイルがありません。";
				return false;
			}
			if (!isEnoughDiskSpace())
			{
				err = "コピー先の空き容量が不足しています。";
				return false;
			}
			int timeout = int.Parse(textBox4.Text);
			if(timeout < 0 || 1800 < timeout)
			{
				err = "タイムアウトは0以上、1800以下である必要があります";
				return false;
			}

			return true;
		}

		// コピー元でzip圧縮
		private bool Compress(ref string strZip)
		{
			strZip = uty.GetZipName(opt.m_strCopySrc);
			string detail = "";
			bool stat = true;
			Stopwatch sw = uty.timeStart();
			DateTime dt = DateTime.Now;
			try
			{
				if (opt.m_i7zipUse == 1)
				{
					string param1 = "\"" + strZip + "\"";
					string param2 = "\"" + opt.m_strCopySrc + "\"";
					Process p = Process.Start(opt.m_str7zipPath2, "a " + param1 + " " + param2);
					p.WaitForExit();
					if(p.ExitCode != 0)  // 失敗
					{
						p.Dispose();
						throw new FormatException("zip圧縮に失敗");
					}
					p.Dispose();
				}
				else
				{
					ZipFile zip = new ZipFile(Encoding.GetEncoding("shift_jis"));
					zip.CompressionLevel = CompressionLevel.BestCompression;
					if (Environment.Is64BitProcess)
						zip.UseZip64WhenSaving = Zip64Option.AsNecessary;
					if (uty.IsSrcFolder(opt.m_strCopySrc))
					{
						string strFolder = Path.GetFileName(opt.m_strCopySrc);
						zip.AddDirectory(opt.m_strCopySrc, strFolder);
					}
					else
						zip.AddFile(opt.m_strCopySrc, "");
					zip.Save(strZip);
					zip.Dispose();
				}
			}
			catch (Exception ex)
			{
				stat = false;
				detail = string.Format("例外エラー（{0}）", ex.Message);
			}
			finally
			{
				Invoke(new logInfo(addLogDelegate), dt, "コピー元で圧縮", opt.m_strCopySrc, opt.m_strCopyDst, stat, uty.timeEnd(sw), detail);
			}
			return stat;
		}

		// zipファイルコピー
		private bool CopyFile(string strZip, ref string dstFull)
		{
			string dstFile = Path.GetFileName(strZip);
			dstFull = Path.Combine(opt.m_strCopyDst, dstFile);
			string detail = "";
			bool stat = true;
			Stopwatch sw = uty.timeStart();
			DateTime dt = DateTime.Now;
			try
			{
				if (opt.m_iDstZipOver == 0 && File.Exists(dstFull))  // 上書き確認しない場合は、コピー前にファイル削除する
					uty.FileDelete(listView1, dstFull, opt.m_iDelTimeout);
				FileSystem.CopyFile(strZip, dstFull, UIOption.AllDialogs);
			}
			catch (Exception ex)
			{
				stat = false;
				detail = string.Format("例外エラー（{0}）", ex.Message);
			}
			finally
			{
				Invoke(new logInfo(addLogDelegate), dt, "圧縮ファイルをコピー", opt.m_strCopySrc, opt.m_strCopyDst, stat, uty.timeEnd(sw), detail);
			}
			return stat;
		}

		// コピー先でzip解凍
		private bool Extract(string dstFull)
		{
			string detail = "";
			bool stat = true;
			Stopwatch sw = uty.timeStart();
			DateTime dt = DateTime.Now;
			try
			{
				if (opt.m_i7zipUse == 1)
				{
					string param1 = "\"" + opt.m_strCopyDst + "\"";
					string param2 = "\"" + dstFull + "\"";
					Process p = Process.Start(opt.m_str7zipPath2, "x -y -o" + param1 + " " + param2);
					p.WaitForExit();
					if (p.ExitCode != 0)  // 失敗
					{
						p.Dispose();
						throw new FormatException("zip解答に失敗");
					}
					p.Dispose();
				}
				else
				{
					var enc = new ReadOptions() { Encoding = Encoding.GetEncoding("shift_jis") };
					ZipFile zip = ZipFile.Read(dstFull, enc);
					zip.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
					zip.ExtractAll(opt.m_strCopyDst);
					zip.Dispose();
				}
			}
			catch (Exception ex)
			{
				stat = false;
				detail = string.Format("例外エラー（{0}）", ex.Message);
			}
			finally
			{
				Invoke(new logInfo(addLogDelegate), dt, "コピー先で解凍", opt.m_strCopySrc, opt.m_strCopyDst, stat, uty.timeEnd(sw), detail);
			}
			return stat;
		}

		// 実行
		private async void button3_Click(object sender, EventArgs e)
		{
			DateTime dt = DateTime.Now;
			UpdateOption();
			string errMsg = "";
			if (!PreCheck(ref errMsg))
			{
				Invoke(new logInfo(addLogDelegate), dt, "事前チェック", opt.m_strCopySrc, opt.m_strCopyDst, false, 0, errMsg);
				return;
			}

			bool stat = false;
			button3.Enabled = false;
			this.Text += "　【実行中】";
			exec = true;
			try
			{
				await Task.Run(() =>
				{
					string strZip = "";
					if (Compress(ref strZip))
					{
						string dstFull = "";
						if (CopyFile(strZip, ref dstFull))
						{
							stat = true;
							if (opt.m_iSrcZipDel == 1)
								stat = uty.FileDelete(listView1, strZip, opt.m_iDelTimeout);
							if (opt.m_iDstZipOpen == 1 && Extract(dstFull))
							{
								if (opt.m_iDstZipDel == 1)
									stat = uty.FileDelete(listView1, dstFull, opt.m_iDelTimeout);
							}
						}
					}
				});
			}
			catch (Exception)
			{
				Invoke(new logInfo(addLogDelegate), dt, "スレッド実行", opt.m_strCopySrc, opt.m_strCopyDst, false, 0, "原因不明な例外エラー");
			}
			finally
			{
				button3.Enabled = true;
				int idx = this.Text.IndexOf("　【実行中】");
				if (idx != 0)
					this.Text = this.Text.Substring(0, idx);
				exec = false;
				if (stat && opt.m_iSuccessClose == 1)  // 成功したからダイアログを閉じる
					this.Close();
			}
		}

		private void checkBox3_CheckStateChanged(object sender, EventArgs e)
		{
			if (checkBox3.Checked)
				checkBox4.Enabled = true;
			else
				checkBox4.Enabled = false;
		}

		private void button4_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.InitialDirectory = @"C:\";
			if (textBox3.Text != "")
				ofd.InitialDirectory = Path.GetDirectoryName(textBox3.Text);
			ofd.Filter = "7zip exe(GUI)|7zG.exe|7zip exe(Console)|7z.exe";
			ofd.Title = "7zipのexeファイルを選択";
			ofd.FileName = "";
			ofd.RestoreDirectory = true;
			if (ofd.ShowDialog() != DialogResult.OK)
			{
				ofd.Dispose();
				return;
			}
			textBox3.Text = ofd.FileName;
			ofd.Dispose();
			UpdateExecButton();
		}

		private void button5_Click(object sender, EventArgs e)
		{
			DialogResult result = MessageBox.Show("エクスプローラの右クリックメニューに、「圧縮してコピー(zipcopy)」を追加しますか？",
				"確認",	MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
			if (result == DialogResult.Yes)
			{
				string commandline = "\"" + Application.ExecutablePath + "\" \"%1\"";
				string verb = "zipcopy";
				string description = "圧縮してコピー(zipcopy)";
				RegistryKey cmdkey = Registry.ClassesRoot.CreateSubKey("Directory\\shell\\" + verb + "\\command");
				cmdkey.SetValue("", commandline);
				cmdkey.Close();

				RegistryKey verbkey = Registry.ClassesRoot.CreateSubKey("Directory\\shell\\" + verb);
				verbkey.SetValue("", description);
				verbkey.Close();

				cmdkey = Registry.ClassesRoot.CreateSubKey("*\\shell\\" + verb + "\\command");
				cmdkey.SetValue("", commandline);
				cmdkey.Close();

				verbkey = Registry.ClassesRoot.CreateSubKey("*\\shell\\" + verb);
				verbkey.SetValue("", description);
				verbkey.Close();
			}
			EnabledMenuAddBtn();
		}

		private void button6_Click(object sender, EventArgs e)
		{
			DialogResult result = MessageBox.Show("エクスプローラの右クリックメニューから、「圧縮してコピー(zipcopy)」を削除しますか？",
				"確認", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
			if (result == DialogResult.Yes)
			{
				string verb = "zipcopy";
				uty.DelRegSubkey("Directory\\shell\\" + verb);
				uty.DelRegSubkey("*\\shell\\" + verb);
			}
			EnabledMenuAddBtn();
		}

		private bool IsAddedMenu()
		{
			string verb = "zipcopy";
			RegistryKey regkey1 = Registry.ClassesRoot.OpenSubKey("Directory\\shell\\" + verb);
			RegistryKey regkey2 = Registry.ClassesRoot.OpenSubKey("*\\shell\\" + verb);
			if (regkey1 == null || regkey2 == null)
				return false;
			string stringValue1 = (string)regkey1.GetValue("");
			string stringValue2 = (string)regkey2.GetValue("");
			if (stringValue1 == null || stringValue2 == null)
				return false;
			regkey1.Close();
			regkey2.Close();
			return true;
		}

		private void EnabledMenuAddBtn()
		{
			bool isAdd = IsAddedMenu();
			button5.Enabled = !isAdd;
			button6.Enabled = isAdd;
		}

		private void textBox1_DragDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			if (files.Length > 0)
			{
				string path = files[0];
				bool isDirectory = File
					.GetAttributes(path)
					.HasFlag(FileAttributes.Directory)
				;
				if(!isDirectory)
					textBox1.Text = Path.GetDirectoryName(files[0]);
				else
					textBox1.Text = files[0];
			}
		}

		private void textBox1_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.All;
			else
				e.Effect = DragDropEffects.None;
		}

		private void textBox2_DragDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			if (files.Length > 0)
			{
				string path = files[0];
				bool isDirectory = File
					.GetAttributes(path)
					.HasFlag(FileAttributes.Directory)
				;
				if (!isDirectory)
					textBox2.Text = Path.GetDirectoryName(files[0]);
				else
					textBox2.Text = files[0];
			}
		}

		private void textBox2_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.All;
			else
				e.Effect = DragDropEffects.None;
		}

		private void checkBox5_CheckStateChanged(object sender, EventArgs e)
		{
			if (checkBox5.Checked)
			{
				textBox3.Enabled = true;
				button4.Enabled = true;
			}
			else
			{
				textBox3.Enabled = false;
				button4.Enabled = false;
			}
		}
	}
}
