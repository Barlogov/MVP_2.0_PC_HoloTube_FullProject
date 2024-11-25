using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class ProgramManager : MonoBehaviour
{
    /*
    //#if UNITY_STANDALONE_WIN
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool SetDllDirectory(string lpPathName);
    //#endif
    */

    private void Awake()
    {
        /*
        //#if UNITY_STANDALONE_WIN
        SetDllDirectory(System.IO.Path.Combine(Application.dataPath, "Plugins"));
        //#endif
        */
    }
}
