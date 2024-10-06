using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zipcopy
{
	class Option
	{
		const string m_RegBase = @"Software\zipcopy\";
		const string m_RegGrpMain = "main";

		const string m_RegGrpMainCopySrc = "CopySrc";
		const string m_RegGrpMainCopyDst = "CopyDst";
		const string m_RegGrpMainSrcZipDel = "SrcZipDel";
		const string m_RegGrpMainDstZipOver = "DstZipOver";
		const string m_RegGrpMainDstZipOpen = "DstZipOpen";
		const string m_RegGrpMainDstZipDel = "DstZipDel";
		const string m_RegGrpMain7ZipUse = "7zipUse";
		const string m_RegGrpMain7ZipPath = "7zipPath";
		const string m_RegGrpMain7ZipPath2 = "7zipPath2";
		const string m_RegGrpMainDelTimeout = "DelTimeOut";
		const string m_RegGrpMainSuccessClose = "SuccessClose";
		public string[] m_RegGrpMainList = new string[7] { "List0", "List1", "List2", "List3", "List4", "List5", "List6" };
		public string m_strCopySrc = "";
		public string m_strCopyDst = "";
		public int m_iSrcZipDel = 1;
		public int m_iDstZipOver = 1;
		public int m_iDstZipOpen = 1;
		public int m_iDstZipDel = 1;
		public int m_i7zipUse = 1;
		public string m_str7zipPath = @"C:\Program Files\7-Zip\7z.exe";
		public string m_str7zipPath2 = @"C:\Program Files\7-Zip\7zG.exe";
		public int[] m_List = new int[7] {115,60,110,80,80,50,250};
		public string[] m_strList = new string[7] { "開始日時", "時間(s)", "操作", "コピー元", "コピー先", "結果", "結果詳細" };
		public int m_iDelTimeout = 180;
		public int m_iSuccessClose = 1;

		public Option()
		{
		}

		public void LoadReg()
		{
			string subkey = m_RegBase + m_RegGrpMain;
			Microsoft.Win32.RegistryKey regkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subkey, false);
			if (regkey == null)
				return;
			string strValue = (string)regkey.GetValue(m_RegGrpMainCopySrc);
			if (strValue != null)
				m_strCopySrc = strValue;
			strValue = (string)regkey.GetValue(m_RegGrpMainCopyDst);
			if (strValue != null)
				m_strCopyDst = strValue;
			int? iValue = (int?)regkey.GetValue(m_RegGrpMainSrcZipDel);
			if (iValue != null)
				m_iSrcZipDel = (int)iValue;
			iValue = (int?)regkey.GetValue(m_RegGrpMainDstZipOver);
			if (iValue != null)
				m_iDstZipOver = (int)iValue;
			iValue = (int?)regkey.GetValue(m_RegGrpMainDstZipOpen);
			if (iValue != null)
				m_iDstZipOpen = (int)iValue;
			iValue = (int?)regkey.GetValue(m_RegGrpMainDstZipDel);
			if (iValue != null)
				m_iDstZipDel = (int)iValue;
			iValue = (int?)regkey.GetValue(m_RegGrpMain7ZipUse);
			if (iValue != null)
				m_i7zipUse = (int)iValue;
			strValue = (string)regkey.GetValue(m_RegGrpMain7ZipPath);
			if (strValue != null)
				m_str7zipPath = strValue;
			strValue = (string)regkey.GetValue(m_RegGrpMain7ZipPath2);
			if (strValue != null)
				m_str7zipPath2 = strValue;
			for (int i = 0; i < 7; i++)
			{
				iValue = (int?)regkey.GetValue(m_RegGrpMainList[i]);
				if (iValue != null)
					m_List[i] = (int)iValue;
			}
			iValue = (int?)regkey.GetValue(m_RegGrpMainDelTimeout);
			if (iValue != null)
				m_iDelTimeout = (int)iValue;
			iValue = (int?)regkey.GetValue(m_RegGrpMainSuccessClose);
			if (iValue != null)
				m_iSuccessClose = (int)iValue;
			regkey.Close();
		}
		public void SaveReg()
		{
			string subkey = m_RegBase + m_RegGrpMain;
			Microsoft.Win32.RegistryKey regkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(subkey);
			regkey.SetValue(m_RegGrpMainCopySrc, m_strCopySrc);
			regkey.SetValue(m_RegGrpMainCopyDst, m_strCopyDst);
			regkey.SetValue(m_RegGrpMainSrcZipDel, m_iSrcZipDel);
			regkey.SetValue(m_RegGrpMainDstZipOver, m_iDstZipOver);
			regkey.SetValue(m_RegGrpMainDstZipOpen, m_iDstZipOpen);
			regkey.SetValue(m_RegGrpMainDstZipDel, m_iDstZipDel);
			regkey.SetValue(m_RegGrpMain7ZipUse, m_i7zipUse);
			regkey.SetValue(m_RegGrpMain7ZipPath, m_str7zipPath);
			regkey.SetValue(m_RegGrpMain7ZipPath2, m_str7zipPath2);
			for (int i = 0; i < 7; i++)
				regkey.SetValue(m_RegGrpMainList[i], m_List[i]);
			regkey.SetValue(m_RegGrpMainDelTimeout, m_iDelTimeout);
			regkey.SetValue(m_RegGrpMainSuccessClose, m_iSuccessClose);
			regkey.Close();
		}
	}
}
