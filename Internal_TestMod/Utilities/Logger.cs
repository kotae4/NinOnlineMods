using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NinMods
{
    public class Logger
    {
        const string LOG_FILENAME = Main.MAIN_NAME + "_log.txt";
        const string NETLOG_FILENAME = Main.MAIN_NAME + "_netlog.txt";

        public static readonly Logger Log = new Logger();
        static readonly System.Globalization.CultureInfo DateTimeCultureInfo_German = System.Globalization.CultureInfo.CreateSpecificCulture("de-DE");

        // mostly used for coloring messages
        public enum ELogType
        {
            Info,
            Notification,
            Error,
            Exception
        }

        // TO-DO:
        // * Is StreamWriter thread-safe?
        StreamWriter logWriter;
        StreamWriter netlogWriter;

        Logger()
        {
            try
            {
                // TO-DO:
                // 1. if the file already exists, write some easy to see separator to indicate a new session
                // 2. if either log gets to be too big, then delete it and start fresh. or maybe move it to a zip archive and start fresh.
                // 3. adding on to the above, maybe we should create a new log file for each session, and only keep the last ~10 log files.
                logWriter = new StreamWriter(LOG_FILENAME, true) { AutoFlush = true };
                // NOTE:
                // net log will quickly fill up disk space so we don't want to append. might change this later.
                netlogWriter = new StreamWriter(NETLOG_FILENAME, false) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Could not initialize Logger: " + ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        // NOTE:
        // this is actually pretty bad. C# destructors should only be used to clean up unmanaged resources under unexpected conditions.
        // but i guess the underlying file handle counts as an unmanaged resource, right?
        ~Logger()
        {
            Close();
        }

        public void Close()
        {
            if (logWriter != null)
            {
                if ((logWriter.BaseStream != null) && (logWriter.BaseStream.CanRead))
                    logWriter.Flush();
                logWriter.Close();
            }
            if (netlogWriter != null)
            {
                if ((netlogWriter.BaseStream != null) && (netlogWriter.BaseStream.CanRead))
                    netlogWriter.Flush();
                netlogWriter.Close();
            }
        }

        public void Write(string typeSource, string methodSource, string logString, ELogType type = ELogType.Info, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = false)
        {
            logString = $"[{DateTime.Now.ToString("G", DateTimeCultureInfo_German)}][{typeSource}::{methodSource}]: {logString}\n";
            if (logWriter != null)
            {
                if (logWriter.BaseStream.CanWrite)
                {
                    logWriter.WriteLine(logString);
                }
                if (shouldForceFlush)
                    logWriter.Flush();
            }

            if (rtxtLog != null)
            {
                WinformControl_WriteTextSafe(rtxtLog, logString);
            }
        }

        public void WriteError(string typeSource, string methodSource, string logString, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = false)
        {
            Write(typeSource, methodSource, logString, ELogType.Error, rtxtLog, shouldForceFlush);
        }

        public void WriteException(string typeSource, string methodSource, Exception ex, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = true)
        {
            Write(typeSource, methodSource, "Exception occurred: " + ex.Message + "\n\n" + ex.StackTrace, ELogType.Exception, rtxtLog, shouldForceFlush);
        }

        public void Alert(string typeSource, string methodSource, string logString, string caption, System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon icon = System.Windows.Forms.MessageBoxIcon.Error, ELogType type = ELogType.Info, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = false)
        {
            System.Windows.Forms.MessageBox.Show(logString, caption, buttons, icon);
            Write(typeSource, methodSource, logString, type, rtxtLog, shouldForceFlush);
        }

        public void WriteNetLog(string typeSource, string methodSource, string logString, ELogType type = ELogType.Info, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = false)
        {
            logString = $"[{DateTime.Now.ToString("G", DateTimeCultureInfo_German)}][{typeSource}::{methodSource}]: {logString}\n";
            if (netlogWriter != null)
            {
                if (netlogWriter.BaseStream.CanWrite)
                {
                    netlogWriter.WriteLine(logString);
                }
                if (shouldForceFlush)
                    netlogWriter.Flush();
            }

            if (rtxtLog != null)
            {
                WinformControl_WriteTextSafe(rtxtLog, logString);
            }
        }

        private delegate void SafeCallDelegate(System.Windows.Forms.RichTextBox richtextControl, string text);
        private void WinformControl_WriteTextSafe(System.Windows.Forms.RichTextBox richtextControl, string text)
        {
            if (richtextControl.InvokeRequired)
            {
                var d = new SafeCallDelegate(WinformControl_WriteTextSafe);
                richtextControl.Invoke(d, new object[] { richtextControl, text });
            }
            else
            {
                richtextControl.AppendText(text);
            }
        }
    }
}
