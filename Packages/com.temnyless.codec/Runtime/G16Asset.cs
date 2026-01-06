using UnityEngine;

namespace TemnyLessCodec
{
    public class G16Asset : ScriptableObject
    {
        public string sourceAssetPath;     // "Assets/xxx.g16"
        public string sourceSha1;          // ключ кэша
        public string cacheDirAbsolute;    // Library/TemnyLessCache/g16/<sha1>/
        public string framesDirAbsolute;   // .../<name>_frames/
        public string logPathAbsolute;     // .../<name>.log.txt
        public int frameCount;             // если сумеем посчитать
    }
}
