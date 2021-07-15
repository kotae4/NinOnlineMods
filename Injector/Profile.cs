using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Injector
{
    public class Profile
    {
        public string GameDirPath = "";
        public string NETRuntimeVersion = "v4.0.30319";
        public string InjectDLLFullPath = "";
        public string Typename_InjectedDLL = "";
        public string EntrypointMethod_InjectedDLL = "";
        public string GameLogin_Username = "";
        public string GameLogin_Password = "";

        public Profile() { }

        public string GetFilePath()
        {
            string fileName = Main.MOD_RELATIVE_PATH + "config" + Main.PROFILE_FILE_EXT;
            string filePath = "";
            try
            {
                filePath = Path.GetFullPath(fileName);
            }
            catch (PathTooLongException ptle)
            {
                filePath = Path.GetFullPath(Main.MOD_RELATIVE_PATH + (Path.GetRandomFileName().Split('.')[0]) + Main.PROFILE_FILE_EXT);
            }
            return filePath;
        }

        public bool Serialize(BinaryWriter writer)
        {
            bool modsSuccessfullySerialized = true;
            try
            {
                writer.Write(GameDirPath);
                writer.Write(NETRuntimeVersion);
                writer.Write(InjectDLLFullPath);
                writer.Write(Typename_InjectedDLL);
                writer.Write(EntrypointMethod_InjectedDLL);
                writer.Write(GameLogin_Username);
                writer.Write(GameLogin_Password);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error: could not serialize Profile\n" + ex.Message + "\n\n" + ex.StackTrace, Main.MESSAGEBOX_CAPTION, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
            return modsSuccessfullySerialized;
        }

        public bool Deserialize(BinaryReader reader)
        {
            try
            {
                GameDirPath = reader.ReadString();
                NETRuntimeVersion = reader.ReadString();
                InjectDLLFullPath = reader.ReadString();
                Typename_InjectedDLL = reader.ReadString();
                EntrypointMethod_InjectedDLL = reader.ReadString();
                GameLogin_Username = reader.ReadString();
                GameLogin_Password = reader.ReadString();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error: could not deserialize Profile\n" + ex.Message + "\n\n" + ex.StackTrace, Main.MESSAGEBOX_CAPTION, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
    }
}
