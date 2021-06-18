using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.ComponentModel;

namespace Injector
{
    public partial class Main
    {
        public class ProcessInfoItem
        {
            public Process ActiveProcess = null;
            public int PID = -1;
            public bool Is64Bit = false;
            public string Name = "";
            public string FilePath = "";
            public Icon Icon = null;
        }

        private ProcessInfoItem SelectedProcess;

        public void SetWatchedProcess(string processFileName)
        {
            SelectedProcess = new ProcessInfoItem();
            SelectedProcess.FilePath = processFileName;
            Logger.Log.Write("ProcessWatcher", "SetWatchedProcess", "ProcessWatcher set to watch for '" + processFileName + "'", Logger.ELogType.Info);
            if (File.Exists(processFileName))
            {
                SelectedProcess.Icon = Icon.ExtractAssociatedIcon(processFileName);
                pictureBoxProcessIcon.Image = SelectedProcess.Icon.ToBitmap();
                Logger.Log.Write("ProcessWatcher", "SetWatchedProcess", "ProcessWatcher loaded process icon", Logger.ELogType.Info);
            }
            LostActiveProcess();
        }

        private void SetActiveProcess(Process proc)
        {
            SelectedProcess.ActiveProcess = proc;
            SelectedProcess.PID = SelectedProcess.ActiveProcess.Id;
            PEHeader peHeader = PEHeader.ParseFromProcess(proc);
            SelectedProcess.Is64Bit = peHeader.Is64Bit;
            lblProcessDetails.ForeColor = System.Drawing.Color.Black;
            lblProcessDetails.Text = "Process: " + SelectedProcess.ActiveProcess.ProcessName + "(PID: " + SelectedProcess.PID.ToString() + ", " + (peHeader.Is64Bit ? "x64" : "x86") + ")";
            btnInject.Enabled = true;

            Logger.Log.Write("ProcessWatcher", "SetActiveProcess", "ProcessWatcher found game process '" + SelectedProcess.ActiveProcess.ProcessName + "' with PID " + SelectedProcess.PID.ToString(), Logger.ELogType.Info);

            if (SelectedProcess.Name == "")
            {
                SelectedProcess.Name = SelectedProcess.ActiveProcess.ProcessName;
            }
            try
            {
                SelectedProcess.ActiveProcess.EnableRaisingEvents = true;
                SelectedProcess.ActiveProcess.SynchronizingObject = this;
                SelectedProcess.ActiveProcess.Exited += OnProcessExited;
            }
            catch (Exception ex)
            {
                Logger.Log.Alert("ProcessWatcher", "SetActiveProcess", "Could not subscribe to process' Exited event. Cannot auto-detect when selected process closes or becomes invalid.\n\n" + ex.Message + "\n\n" + ex.StackTrace, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void LostActiveProcess()
        {
            lblProcessDetails.Text = "Waiting for game...";
            lblProcessDetails.ForeColor = System.Drawing.Color.DarkOrange;
            Logger.Log.Write("ProcessWatcher", "LostActiveProcess", "ProcessWatcher lost game process, beginning search again...", Logger.ELogType.Notification);
            btnInject.Enabled = false;
            // check if background worker is busy. if not, give it its task
            // if it is busy, welllll.... why would it be busy?
            if (ProcessPollWorker.IsBusy)
            {
                ProcessPollWorker.CancelAsync();
                Logger.Log.Write("ProcessWatcher", "LostActiveProcess", "ProcessWatcher's worker was busy, spinning until free...", Logger.ELogType.Info);
                while (ProcessPollWorker.IsBusy)
                {
                    System.Windows.Forms.Application.DoEvents();
                }
            }
            // poll continually for new processes matching the old one's filepath
            ProcessPollWorker.RunWorkerAsync(SelectedProcess.FilePath);
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            LostActiveProcess();
        }

        private void ProcessPollWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // TO-DO:
            // better error handling
            // timeout functionality
            string processFilePath = e.Argument as string;
            Logger.Log.Write("ProcessWatcher", "ProcessPollWorker_DoWork", "ProcessWatcher's worker beginning scan for '" + processFilePath + "'", Logger.ELogType.Info);
            Process[] allProcesses;
            while (true)
            {
                if (ProcessPollWorker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                allProcesses = Process.GetProcesses();
                foreach (Process proc in allProcesses)
                {
                    try
                    {
                        if (proc.HasExited) continue;

                        ProcessModule procMainModule = proc.MainModule;
                        if (procMainModule.FileName == processFilePath)
                        {
                            e.Result = proc;
                            return;
                        }
                    }
                    // NOTE:
                    // this exception is thrown when the process is a protected process.
                    // harmless in our case
                    catch (Win32Exception w32e) { }
                    // TO-DO:
                    // error handling
                    catch (Exception ex) { }
                }
                System.Threading.Thread.Sleep(500);
            }
        }

        private void ProcessPollWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Error != null) || (e.Cancelled))
            {
                // TO-DO:
                // handle this
                Logger.Log.Write("ProcessWatcher", "ProcessPollWorker_RunWorkerCompleted", "ProcessWatcher's worker failed or was cancelled", Logger.ELogType.Notification);
                return;
            }
            if (e.Result != null)
            {
                SetActiveProcess(e.Result as Process);
            }
        }
    }
}
