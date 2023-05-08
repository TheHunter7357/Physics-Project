using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using GXPEngine;
using Microsoft.Win32;

public static class AssetManager
{
    public static void UpdateRegistry()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT &&
            Environment.OSVersion.Platform != PlatformID.Win32S &&
            Environment.OSVersion.Platform != PlatformID.Win32Windows &&
            Environment.OSVersion.Platform != PlatformID.WinCE)
            return;

        try
        {
            using (var key = Registry.ClassesRoot.CreateSubKey(".gxpa"))
            {
                key.SetValue("", "gxpa" + "_file");

                using (var subkey = Registry.ClassesRoot.CreateSubKey("gxpa_file"))
                {
                    subkey.SetValue("", "GXP Asset used for storing GameObjects");
                    subkey.CreateSubKey("DefaultIcon").SetValue("", "\"" + Settings.AssetsPath.Substring(0, Settings.AssetsPath.LastIndexOf('\\')) + "\\icon.ico" + "\",0");
                    subkey.CreateSubKey(@"shell\open\command").SetValue("", "\"" + Settings.ApplicationPath.Substring(0, Settings.AssetsPath.Length - 1) + "\" \"%1\"");
                }
            }

            Debug.Log(">> Updated registry of GXP Asset");
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }
        catch
        {
            return;
        }
    }
    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    public static GameObject LoadAsset(string filename, bool fullname = false)
    {
        FileStream stream = new FileStream(!fullname? Settings.AssetsPath + filename + ".gxpa" : filename, FileMode.Open);
        GameObject asset = (GameObject)new BinaryFormatter().Deserialize(stream);
        stream.Close();

        Debug.Log("Serialized and loaded asset named "
            + '"'
            + (!fullname ? filename + ".gxpa" : filename.Substring(filename.LastIndexOf('\\') + 1))
            + '"');
        asset.OnSerialized();
        return asset;
    }
    public static void CreateAsset(string filename, GameObject asset)
    {
        FileStream stream = new FileStream(Settings.AssetsPath + filename + ".gxpa", FileMode.Create);
        new BinaryFormatter().Serialize(stream, asset);
        stream.Close();

        Debug.Log("Created asset named " + '"' + filename + ".gxpa" + '"');
    }
    public static void UpdateAsset(string filename, GameObject asset)
    {
        DeleteAsset(filename);
        CreateAsset(filename, asset);

        Debug.Log("Updated asset named " + '"' + filename + ".gxpa" + '"');
    }
    public static void DeleteAsset(string filename)
    {
        File.Delete(Settings.AssetsPath + filename + ".gxpa");
        Debug.Log("Deleted asset named " + '"' + filename + ".gxpa" + '"');
    }
}
