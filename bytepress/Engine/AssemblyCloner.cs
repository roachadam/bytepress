using System;
using System.Diagnostics;
using System.Drawing;

namespace bytepress.Engine
{
    public class AssemblyCloner
    {

        private readonly FileVersionInfo _asmInformation;
        private string _asm;

        public AssemblyCloner(string asm)
        {
            _asmInformation = FileVersionInfo.GetVersionInfo(asm);
            _asm = asm;
        }

        public string Company
        {
            get
            {
                string C = _asmInformation.CompanyName;
                if (!string.IsNullOrEmpty(C))
                    return C;
                return string.Empty;
            }
        }
        public string Version
        {
            get
            {
                string C = _asmInformation.FileVersion;
                if (!string.IsNullOrEmpty(C))
                    return C;
                return string.Empty;
            }
        }
        public string Copyright
        {
            get
            {
                string C = _asmInformation.LegalCopyright;
                if (!string.IsNullOrEmpty(C))
                    return C;
                return string.Empty;
            }
        }

        public string Description
        {
            get
            {
                string D = _asmInformation.FileDescription;
                if (!string.IsNullOrEmpty(D))
                    return D;
                return string.Empty;
            }
        }

        public string Product
        {
            get
            {
                string P = _asmInformation.ProductName;
                if (!string.IsNullOrEmpty(P))
                    return P;
                return string.Empty;
            }
        }

        public string Title
        {
            get
            {
                string T = _asmInformation.InternalName.Split('.')[0];
                if (!string.IsNullOrEmpty(T))
                    return T;
                return string.Empty;
            }
        }

        public string Trademark
        {
            get
            {
                string T = _asmInformation.LegalTrademarks;
                if (!string.IsNullOrEmpty(T))
                    return T;
                return string.Empty;
            }
        }
        public Icon Icon
        {
            get
            {
                try
                {
                    IconExtractor ie = new IconExtractor(_asm);
                    Icon ret = null;
                    int height = int.MinValue;
                    int width = int.MinValue;
                    foreach (Icon icon in ie.GetAllIcons())
                    {
                        if (icon.Height <= height || icon.Width <= width) continue;
                        ret = icon;
                        height = icon.Height;
                        width = icon.Width;
                    }
                    return ret;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
