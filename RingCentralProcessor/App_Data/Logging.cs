using System;
using System.IO;

namespace RingCentralProcessor.App_Data
{
    public class Logging
    {

        private StreamWriter oLogFileWriter;
        private FileStream oLogFile;

        public Logging(string dbname)
        {
            if (!Convert.ToBoolean(Properties.Settings.Default.TurnOnLogging)) return;
            string sDate = null;
            sDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            string sFile = null;
            sFile = Properties.Settings.Default.LogFile;
            sFile = sFile.Insert(0, sDate + "_");
            sFile = sFile.Insert(0, CreateLogFolder(dbname));
            if (File.Exists(sFile))
                File.Delete(sFile);
            oLogFile = new FileStream(sFile, FileMode.OpenOrCreate);
            oLogFileWriter = new StreamWriter(oLogFile);
        }

        public string CreateLogFolder(string dbname)
        {
            if (!Convert.ToBoolean(Properties.Settings.Default.TurnOnLogging)) return "";
            var sPath = Properties.Settings.Default.Logging_Path + dbname + "\\";
            sPath += DateTime.Now.ToString("yyyyMMdd");
            if (!Directory.Exists(sPath))
            {
                Directory.CreateDirectory(sPath);
            }
            return sPath + "\\";
        }

        public void WriteAction(string sAction)
        {
            if (!Convert.ToBoolean(Properties.Settings.Default.TurnOnLogging)) return;
            oLogFileWriter.WriteLine(Convert.ToString(DateTime.Now.ToString("T")) + "\t" + sAction);
            oLogFileWriter.Flush();
        }

        public void CloseWriter()
        {
            if (!Convert.ToBoolean(Properties.Settings.Default.TurnOnLogging)) return;
            oLogFileWriter.Close();
            oLogFileWriter = null;
            oLogFile.Close();
            oLogFile = null;
        }

        public string ReturnException(Exception ex, string ExceptionMessage, bool IsInnerException = false)
        {

            if (IsInnerException)
            {
                ExceptionMessage += "Inner Exception" + Environment.NewLine;
            }

            ExceptionMessage += "Exception: " + ex.Message + Environment.NewLine;
            ExceptionMessage += "Stack Trace: " + ex.StackTrace + Environment.NewLine;

            if (ex.InnerException != null)
            {
                ExceptionMessage += ReturnException(ex.InnerException, ExceptionMessage, true);
            }

            return ExceptionMessage;
        }
    }
}
