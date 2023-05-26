using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Xml;
using Microsoft.Win32;
using Wox.Infrastructure.UserSettings;

namespace Wox.Infrastructure.Exception
{
    public class ExceptionFormatter
    {
        private static string _systemLanguage = string.Empty;
        private static string _woxLanguage = string.Empty;
        public static void Initialize(string systemLanguage, string woxLanguage)
        {
            _systemLanguage = systemLanguage;
            _woxLanguage = woxLanguage;
        }
        public static string FormattedException(System.Exception ex)
        {
            return FormattedAllExceptions(ex).ToString();
        }


        private static StringBuilder FormattedAllExceptions(System.Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Exception begin --------------------");
            int index = 1;
            FormattedSingleException(ex, sb, 1);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                sb.Append(Indent(index));
                sb.Append("InnerException ");
                sb.Append(index);
                sb.AppendLine(": ------------------------------------------------------------");
                index++;
                FormattedSingleException(ex, sb, index);
            }

            sb.AppendLine("Exception end ------------------------------------------------------------");
            sb.AppendLine();
            return sb;
        }

        private static string Indent(int indentLevel)
        {
            return new string(' ', indentLevel * 2);
        }

        private static void FormattedSingleException(System.Exception ex, StringBuilder sb, int indentLevel)
        {
            sb.Append(Indent(indentLevel));
            sb.Append(ex.GetType().FullName);
            sb.Append(": ");
            sb.AppendLine(ex.Message);
            sb.Append(Indent(indentLevel));
            sb.Append("HResult: ");
            sb.AppendLine(ex.HResult.ToString());
            foreach(object key in ex.Data.Keys)
            {
                object value = ex.Data[key]!;
                sb.Append(Indent(indentLevel));
                sb.Append("Data: <");
                sb.Append(key);
                sb.Append("> -> <");
                sb.Append(value);
                sb.AppendLine(">");
            }

            if (ex.Source != null)
            {
                sb.Append(Indent(indentLevel));
                sb.Append("Source: ");
                sb.AppendLine(ex.Source);
            }
            if (ex.TargetSite != null)
            {
                sb.Append(Indent(indentLevel));
                sb.Append("TargetAssembly: ");
                sb.AppendLine(ex.TargetSite.Module.Assembly.ToString());
                sb.Append(Indent(indentLevel));
                sb.Append("TargetModule: ");
                sb.AppendLine(ex.TargetSite.Module.ToString());
                sb.Append(Indent(indentLevel));
                sb.Append("TargetSite: ");
                sb.AppendLine(ex.TargetSite.ToString());
            }
            sb.Append(Indent(indentLevel));
            sb.AppendLine("StackTrace: --------------------");
            sb.AppendLine(ex.StackTrace);
        }

        public static StringBuilder RuntimeInfoFull()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(RuntimeInfo());
            sb.AppendLine(SDKInfo());
            sb.Append(AssemblyInfo());
            return sb;
        }

        private static string AssemblyInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("## Assemblies - " + AppDomain.CurrentDomain.FriendlyName);
            sb.AppendLine();
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                sb.Append("* ");
                sb.Append(ass.FullName);
                sb.Append(" (");

                if (ass.IsDynamic)
                {
                    sb.Append("dynamic assembly doesn't has location");
                }
                else if (string.IsNullOrEmpty(ass.Location))
                {
                    sb.Append("location is null or empty");

                }
                else
                {
                    sb.Append(ass.Location);

                }
                sb.AppendLine(")");
            }
            return sb.ToString();
        }

        public static string RuntimeInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("## Runtime Info");
            sb.AppendLine($"* Command Line: {Environment.CommandLine}");
            sb.AppendLine($"* Portable Mode: {DataLocation.PortableDataLocationInUse()}");
            sb.AppendLine($"* Timestamp: {DateTime.Now.ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine($"* Wox version: {Constant.Version}");
            sb.AppendLine($"* OS Version: {Environment.OSVersion.VersionString}");
            sb.AppendLine($"* x64 OS: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"* x64 Process: {Environment.Is64BitProcess}");
            sb.AppendLine($"* System Language: {_systemLanguage}");
            sb.AppendLine($"* Wox Language: {_woxLanguage}");
            sb.AppendLine($"* CLR Version: {Environment.Version}");

            return sb.ToString();
        }

        public static string SDKInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("## SDK Info");
            sb.AppendLine($"* Python Path: {Constant.PythonPath}");
            sb.AppendLine($"* Everything SDK Path: {Constant.EverythingSDKPath}");
            return sb.ToString();
        }

        public static string ExceptionWithRuntimeInfo(System.Exception ex, string id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Error id: ");
            sb.AppendLine(id);
            var formatted = FormattedAllExceptions(ex);
            sb.Append(formatted);
            var info = RuntimeInfoFull();
            sb.Append(info);

            return sb.ToString();
        }
    }
}
