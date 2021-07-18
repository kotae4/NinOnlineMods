using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Launcher.Logging;
// for registry keys
using Microsoft.Win32;

namespace Launcher
{
    public partial class Main : Form
    {
        public const string MAIN_NAME = "NET-Injector";
        public const string MESSAGEBOX_CAPTION = "NET Injector";
        public const string MOD_FOLDER_NAME = "NinOnlineMods";
        public const string MOD_RELATIVE_PATH = MOD_FOLDER_NAME + "\\";
        public const string PROFILE_FILE_EXT = ".prof";

        public const string GAME_EXE_NAME = "NinOnline.exe";
        public const string GAME_EXE_PATH_FROM_ROOT = "app\\" + GAME_EXE_NAME;
        public const string GAME_REGISTRY_UNINSTALL_KEY_PATH = "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
        public const string GAME_REGISTRY_UNINSTALL_KEY_NAME = "Nin Online";

        // assumes the bootstrapper DLLs are in the same directory as the injector
        public const string BOOTSTRAPPER_X86_NAME = "net_bootstrapper_x86.dll";
        public const string BOOTSTRAPPER_X64_NAME = "net_bootstrapper_x64.dll";
        // the bootstrapper will assume the config file is in the same directory as the game exe
        // so we have to write ActiveProfile.GameDirPath + BOOTSTRAPPER_CONFIG_FILENAME.
        public const string BOOTSTRAPPER_CONFIG_FILENAME = "net_bootstrapper_config.txt";

        // 382, 312
        public const int FORM_MAIN_SHRUNK_HEIGHT = 312;
        // 382, 474
        public const int FORM_MAIN_EXPANDED_HEIGHT = 474;

        private bool IsShrunk = false;

        private Profile m_ActiveProfile = new Profile();
        private Profile ActiveProfile
        {
            get { return m_ActiveProfile; }
            set { m_ActiveProfile = value; RefreshControlsState(); }
        }

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            InitializeProfileSystem();
            if (StartPipeServer() == false)
            {
                Logger.Log.Alert("Could not start pipe server", MESSAGEBOX_CAPTION);
                return;
            }
            GameDirWorker.RunWorkerAsync(ActiveProfile);
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveProfileSystem();
            ShutdownPipeServer();
        }

        private void RefreshControlsState()
        {
            // this is kind of a useless function, need to expand its idea more
            // TO-DO:
            // make every change to ActiveProfile call this function. easy GUI updating.
            txtboxInjectDLLPath.Text = ActiveProfile.InjectDLLFullPath;
            txtboxRuntimeVer.Text = ActiveProfile.NETRuntimeVersion;
            txtboxTypename.Text = ActiveProfile.Typename_InjectedDLL;
            txtboxEntrypointMethod.Text = ActiveProfile.EntrypointMethod_InjectedDLL;
            txtboxUsername.Text = ActiveProfile.GameLogin_Username;
            txtboxPassword.Text = ActiveProfile.GameLogin_Password;
        }

        private void GameDirWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Profile activeProf = e.Argument as Profile;
            if ((activeProf != null) && (!string.IsNullOrEmpty(activeProf.GameDirPath)))
            {
                Logger.Log.Write("Profile's GameDirPath: " + activeProf.GameDirPath, Logger.ELogType.Info);
                if (Directory.Exists(activeProf.GameDirPath))
                {
                    e.Result = activeProf.GameDirPath;
                    Logger.Log.Write("GameDirWorker returning path from ActiveProfile", Logger.ELogType.Info);
                    return;
                }
            }
            try
            {
                // 1. check if game exe is in current directory
                if (File.Exists(GAME_EXE_NAME))
                {
                    e.Result = Path.GetDirectoryName(Path.GetFullPath(GAME_EXE_NAME)) + "\\";
                    Logger.Log.Write("GameDirWorker returning path from local directory", Logger.ELogType.Info);
                    return;
                }
                // 2. if not, check for uninstall registry key
                // note:
                // registry key for uninstaller:
                // path: Computer\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Nin Online Inochi
                // key: InstallLocation (example value: G:\Games\Hitspark Interactive\Nin Online Inochi\)
                using (RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey(GAME_REGISTRY_UNINSTALL_KEY_PATH))
                {
                    foreach (string subkey in uninstallKey.GetSubKeyNames())
                    {
                        if (subkey.Contains(GAME_REGISTRY_UNINSTALL_KEY_NAME))
                        {
                            using (RegistryKey gameKey = uninstallKey.OpenSubKey(subkey))
                            {
                                string installLocation = gameKey.GetValue("InstallLocation").ToString();
                                Logger.Log.Write("GameDirWorker saw registry key with value '" + installLocation + "'", Logger.ELogType.Info);
                                if (!File.Exists(installLocation + GAME_EXE_PATH_FROM_ROOT))
                                {
                                    Logger.Log.WriteError("GameDirWorker couldn't find game exe from registry key");
                                    e.Result = string.Empty;
                                    return;
                                }
                                e.Result = Path.GetDirectoryName(Path.GetFullPath(installLocation + GAME_EXE_PATH_FROM_ROOT)) + "\\";
                                return;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log.Alert("Exception occurred locating game install directory:\n" + ex.Message + "\n\n" + ex.StackTrace, MESSAGEBOX_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error, Logger.ELogType.Exception);
                e.Result = string.Empty;
                return;
            }
        }

        private void GameDirWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Result as string))
            {
                // 4. if not, prompt the user to locate their game install directory
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    if (File.Exists(folderBrowserDialog1.SelectedPath + "\\" + GAME_EXE_NAME))
                    {
                        ActiveProfile.GameDirPath = folderBrowserDialog1.SelectedPath + "\\";
                        Logger.Log.Write("Got GameDir from user: " + ActiveProfile.GameDirPath, Logger.ELogType.Notification);
                        // start up the ProcessWatcher
                        SetWatchedProcess(ActiveProfile.GameDirPath + GAME_EXE_NAME);
                        return;
                    }
                    Logger.Log.Write("User selected wrong game directory when prompted (" + folderBrowserDialog1.SelectedPath + ")", Logger.ELogType.Info);
                }
                Logger.Log.WriteError("Could not locate Among Us directory", null, true);
                Application.Exit();
            }
            else
            {
                ActiveProfile.GameDirPath = e.Result as string;
                Logger.Log.Write("Got GameDir automatically: " + ActiveProfile.GameDirPath, Logger.ELogType.Notification);
                // start up the ProcessWatcher
                SetWatchedProcess(ActiveProfile.GameDirPath + GAME_EXE_NAME);
            }
        }

        private void txtboxInjectDLLPath_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string injectDLLPath = openFileDialog1.FileName;
                if ((Path.IsPathRooted(injectDLLPath)) && (injectDLLPath.IndexOf(":\\") == 1))
                {
                    ActiveProfile.InjectDLLFullPath = openFileDialog1.FileName;
                }
                else
                {
                    Logger.Log.WriteError("Path to inject DLL is not valid '" + injectDLLPath + "'");
                    ActiveProfile.InjectDLLFullPath = string.Empty;
                }
            }
            RefreshControlsState();
        }

        private void btnInject_Click(object sender, EventArgs e)
        {
            if ((SelectedProcess == null) || (SelectedProcess.ActiveProcess == null) || (SelectedProcess.ActiveProcess.HasExited) || (string.IsNullOrEmpty(ActiveProfile.InjectDLLFullPath)))
            {
                Logger.Log.Write("Could not inject because the game is not running", Logger.ELogType.Notification);
                return;
            }
            string bootstrapDLLPath = SelectedProcess.Is64Bit ? BOOTSTRAPPER_X64_NAME : BOOTSTRAPPER_X86_NAME;
            if (!File.Exists(bootstrapDLLPath))
            {
                Logger.Log.WriteError("Could not find bootstrapper DLL in injector's directory (" + bootstrapDLLPath + ")");
                return;
            }
            string bootstrapConfigPath = ActiveProfile.GameDirPath + BOOTSTRAPPER_CONFIG_FILENAME;
            Logger.Log.Write("Creating config file for bootstrapper at " + bootstrapConfigPath);
            try
            {
                using (FileStream fs = File.Open(bootstrapConfigPath, FileMode.Create, FileAccess.Write))
                {
                    // for some reason, using Encoding.UTF8 (which is fully compatible with ASCII) writes 2 garbage bytes to the beginning of the file
                    // this messes up the bootstrapper's parsing of the file completely.
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.ASCII))
                    {
                        // line 1: runtime_version [v4.0.30319]
                        // line 2: path/to/hack.dll
                        // line 3: full type name in hack.dll [MyCompany.MyProject.InjectedLibrary.InjectedClass]
                        // line 4: method to act as entrypoint [MyHackEntrypoint]
                        sw.WriteLine(ActiveProfile.NETRuntimeVersion);
                        sw.WriteLine(ActiveProfile.InjectDLLFullPath);
                        sw.WriteLine(ActiveProfile.Typename_InjectedDLL);
                        sw.WriteLine(ActiveProfile.EntrypointMethod_InjectedDLL);
                        sw.WriteLine(ActiveProfile.GameLogin_Username + ";" + ActiveProfile.GameLogin_Password);
                    }
                }
                string bootstrapDLLFullPath = Path.GetFullPath(bootstrapDLLPath);
                Logger.Log.Write("Injecting bootstrapper (" + bootstrapDLLFullPath + ")");
                btnInject.Enabled = !Injector.Inject(SelectedProcess.ActiveProcess, bootstrapDLLFullPath);
            }
            catch (Exception ex)
            {
                Logger.Log.WriteException(ex);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Made by kotae", MESSAGEBOX_CAPTION);
        }

        private void txtboxRuntimeVer_Leave(object sender, EventArgs e)
        {
            if (txtboxRuntimeVer.Text != ActiveProfile.NETRuntimeVersion)
            {
                ActiveProfile.NETRuntimeVersion = txtboxRuntimeVer.Text;
                RefreshControlsState();
            }
        }

        private void txtboxTypename_Leave(object sender, EventArgs e)
        {
            if (txtboxTypename.Text != ActiveProfile.Typename_InjectedDLL)
            {
                ActiveProfile.Typename_InjectedDLL = txtboxTypename.Text;
                RefreshControlsState();
            }
        }

        private void txtboxEntrypointMethod_Leave(object sender, EventArgs e)
        {
            if (txtboxEntrypointMethod.Text != ActiveProfile.EntrypointMethod_InjectedDLL)
            {
                ActiveProfile.EntrypointMethod_InjectedDLL = txtboxEntrypointMethod.Text;
                RefreshControlsState();
            }
        }

        private void txtboxUsername_Leave(object sender, EventArgs e)
        {
            if (txtboxUsername.Text != ActiveProfile.GameLogin_Username)
            {
                ActiveProfile.GameLogin_Username = txtboxUsername.Text;
                RefreshControlsState();
            }
        }

        private void txtboxPassword_Leave(object sender, EventArgs e)
        {
            if (txtboxPassword.Text != ActiveProfile.GameLogin_Password)
            {
                ActiveProfile.GameLogin_Password = txtboxPassword.Text;
                RefreshControlsState();
            }
        }

        private void btnToggleLogDisplay_Click(object sender, EventArgs e)
        {
            Size = new Size(Size.Width, (IsShrunk ? FORM_MAIN_EXPANDED_HEIGHT : FORM_MAIN_SHRUNK_HEIGHT));
            IsShrunk = !IsShrunk;
        }
    }
}
