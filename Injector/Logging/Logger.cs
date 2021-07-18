using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
// for automatic caller name & filepath (CallerMemberNameAttribute & CallerFilePathAttribute)
using System.Runtime.CompilerServices;

namespace Launcher.Logging
{
    public class Logger
    {
        const string LOG_FILENAME = Main.MAIN_NAME + "_log.txt";
        const string NETLOG_FILENAME = Main.MAIN_NAME + "_netlog.txt";
        const string PIPELOG_NAME = "ninbot";

        public static readonly Logger Log = new Logger();
        static readonly System.Globalization.CultureInfo DateTimeCultureInfo_German = System.Globalization.CultureInfo.CreateSpecificCulture("de-DE");

        // mostly used for coloring messages
        public enum ELogType
        {
            Trace,
            Info,
            Warning,
            Error,
            Notification,
            Exception
        }

        // TO-DO:
        // * Is StreamWriter thread-safe?
        StreamWriter logWriter;
        StreamWriter netlogWriter;

        // for thread-safety
        object threadLock = 0;


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

        public void Write(string logString, ELogType type = ELogType.Info, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = false, [CallerFilePath] string sourceFile = "<none>", [CallerMemberName] string sourceMethodName = "<none>")
        {
            string typeSource = sourceFile;
            if (typeSource != "<error>")
                typeSource = Path.GetFileName(typeSource);
            logString = $"[{DateTime.Now.ToString("G", DateTimeCultureInfo_German)}][{typeSource ?? sourceFile}::{sourceMethodName}]: {logString}\n";
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
                WinformControl_WriteTextSafe(rtxtLog, logString, type);
            }
        }

        public void WriteError(string logString, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = false, [CallerFilePath] string sourceFile = "<none>", [CallerMemberName] string sourceMethodName = "<none>")
        {
            Write(logString, ELogType.Error, rtxtLog, shouldForceFlush, sourceFile, sourceMethodName);
        }

        public void WriteException(Exception ex, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = true, [CallerFilePath] string sourceFile = "<none>", [CallerMemberName] string sourceMethodName = "<none>")
        {
            Write("Exception occurred: " + ex.Message + "\n\n" + ex.StackTrace, ELogType.Exception, rtxtLog, shouldForceFlush, sourceFile, sourceMethodName);
        }

        public void Alert(string logString, string caption, System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon icon = System.Windows.Forms.MessageBoxIcon.Error, ELogType type = ELogType.Info, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = false, [CallerFilePath] string sourceFile = "<none>", [CallerMemberName] string sourceMethodName = "<none>")
        {
            System.Windows.Forms.MessageBox.Show(logString, caption, buttons, icon);
            Write(logString, type, rtxtLog, shouldForceFlush, sourceFile, sourceMethodName);
        }

        public void WriteNetLog(string logString, ELogType type = ELogType.Info, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = false, [CallerFilePath] string sourceFile = "<none>", [CallerMemberName] string sourceMethodName = "<none>")
        {
            string typeSource = sourceFile;
            if (typeSource != "<error>")
                typeSource = Path.GetFileName(typeSource);
            logString = $"[{DateTime.Now.ToString("G", DateTimeCultureInfo_German)}][{typeSource ?? sourceFile}::{sourceMethodName}]: {logString}\n";
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
                WinformControl_WriteTextSafe(rtxtLog, logString, type);
            }
        }

        public void WriteThreaded(string logString, ELogType type = ELogType.Info, System.Windows.Forms.RichTextBox rtxtLog = null, bool shouldForceFlush = false, [CallerFilePath] string sourceFile = "<none>", [CallerMemberName] string sourceMethodName = "<none>")
        {
            lock (threadLock)
            {
                Write(logString, type, rtxtLog, shouldForceFlush, sourceFile, sourceMethodName);
            }
        }

        private delegate void SafeCallDelegate(System.Windows.Forms.RichTextBox richtextControl, string text, ELogType logLevel);
        private void WinformControl_WriteTextSafe(System.Windows.Forms.RichTextBox richtextControl, string text, ELogType logLevel)
        {
            if (richtextControl.InvokeRequired)
            {
                var d = new SafeCallDelegate(WinformControl_WriteTextSafe);
                richtextControl.Invoke(d, new object[] { richtextControl, text, logLevel });
            }
            else
            {
                // TO-DO:
                // move this to a utility function
                System.Drawing.Color msgColor = System.Drawing.Color.Black;
                bool shouldBold = false;
                switch (logLevel)
                {
                    case ELogType.Trace:
                        {
                            msgColor = System.Drawing.Color.Silver;
                            break;
                        }
                    case ELogType.Warning:
                        {
                            msgColor = System.Drawing.Color.Orange;
                            break;
                        }
                    case ELogType.Error:
                        {
                            msgColor = System.Drawing.Color.Red;
                            break;
                        }
                    case ELogType.Exception:
                        {
                            msgColor = System.Drawing.Color.Red;
                            shouldBold = true;
                            break;
                        }
                    case ELogType.Notification:
                        {
                            msgColor = System.Drawing.Color.DarkTurquoise;
                            break;
                        }
                }
                int selectionStart = richtextControl.TextLength;
                int selectionLength = text.Length;
                richtextControl.Select(selectionStart, selectionLength);
                richtextControl.SelectionColor = msgColor;
                if (shouldBold)
                {
                    richtextControl.SelectionFont = new System.Drawing.Font(richtextControl.Font, System.Drawing.FontStyle.Bold);
                }

                richtextControl.AppendText(text);
            }
        }
    }
}
