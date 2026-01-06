using UnityEngine;

namespace TemnyLessCodec
{
    public class G2DAsset : ScriptableObject
    {
        public string sourceAssetPath;
        public string sourceSha1;
        public string cacheDirAbsolute;
        public string framesDirAbsolute;
        public string logPathAbsolute;
        public int frameCount;
    }
}
