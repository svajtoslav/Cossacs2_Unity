using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace TemnyLessCodec
{
    public static class TemnyLessCacheRuntime
    {
        public static string CacheRootAbsolute =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Library", "TemnyLessCache"));

        public static string ToAbsoluteFromAssetPath(string assetPath)
        {
            // "Assets/..." -> absolute
            if (string.IsNullOrWhiteSpace(assetPath)) return assetPath;
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(assetPath, "Assets", StringComparison.OrdinalIgnoreCase))
                return assetPath;

            var projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        public static string ComputeSha1OfFile(string absolutePath)
        {
            using var fs = File.OpenRead(absolutePath);
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(fs);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    /// <summary>
    /// Keeps package code bound to Melinoja.dll without re-declaring CodecFacade locally.
    /// This avoids the duplicate-type conflict while preserving the original decoder entry points.
    /// </summary>
    public static class MelinojaCodecBridge
    {
        private const string CodecFacadeTypeName = "TemnyLessCodec.CodecFacade";
        private const string CodecFacadeAssemblyQualifiedName = "TemnyLessCodec.CodecFacade, Melinoja";

        private static Type _codecFacadeType;
        private static bool _codecFacadeResolved;

        public static bool DecodeG16ToLogAndFrames(string absPath, out string logPath, out string err, bool doubleOverlay)
        {
            object[] args = { absPath, null, null, doubleOverlay };
            bool ok = InvokeBool("DecodeG16ToLogAndFrames", args, out err);
            logPath = args[1] as string ?? string.Empty;
            err = args[2] as string ?? err ?? string.Empty;
            return ok;
        }

        public static bool DecodeG16ToLogAndFramesNationColor(string absPath, byte r, byte g, byte b, out string logPath, out string err, bool doubleOverlay)
        {
            object[] args = { absPath, r, g, b, null, null, doubleOverlay };
            bool ok = InvokeBool("DecodeG16ToLogAndFramesNationColor", args, out err);
            logPath = args[4] as string ?? string.Empty;
            err = args[5] as string ?? err ?? string.Empty;
            return ok;
        }

        public static bool DecodeG2DToLogAndFrames(string absPath, out string logPath, out string err, bool doubleOverlay)
        {
            object[] args = { absPath, null, null, doubleOverlay };
            bool ok = InvokeBool("DecodeG2DToLogAndFrames", args, out err);
            logPath = args[1] as string ?? string.Empty;
            err = args[2] as string ?? err ?? string.Empty;
            return ok;
        }

        public static bool DecodeG2DToLogAndFramesNationColor(string absPath, byte r, byte g, byte b, out string logPath, out string err, bool doubleOverlay)
        {
            object[] args = { absPath, r, g, b, null, null, doubleOverlay };
            bool ok = InvokeBool("DecodeG2DToLogAndFramesNationColor", args, out err);

            // Compatibility fallback:
            // Some Unity setups still load a Melinoja.dll where CodecFacade was not updated,
            // while G2DLib.G2DAnalyzer already contains AnalyzeToLogNationColor.
            // In that case call the analyzer directly instead of silently falling back to uncolored frames.
            if (!ok && !string.IsNullOrEmpty(err) &&
                err.IndexOf("DecodeG2DToLogAndFramesNationColor", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string directErr;
                object[] directArgs = { absPath, r, g, b, null, null, doubleOverlay };
                bool directOk = InvokeStaticBoolByType(
                    "G2DLib.G2DAnalyzer",
                    "AnalyzeToLogNationColor",
                    directArgs,
                    out directErr);

                if (directOk)
                {
                    args = directArgs;
                    err = string.Empty;
                    ok = true;
                }
                else
                {
                    err = err + " | direct G2DAnalyzer fallback failed: " + directErr;
                }
            }

            logPath = args[4] as string ?? string.Empty;
            err = args[5] as string ?? err ?? string.Empty;
            return ok;
        }

        public static bool LoadG16ToMemory(string absPath, out string err, bool doubleOverlay)
        {
            object[] args = { absPath, null, doubleOverlay };
            bool ok = InvokeBool("LoadG16ToMemory", args, out err);
            err = args[1] as string ?? err ?? string.Empty;
            return ok;
        }

        public static bool LoadG16ToMemoryNationColor(string absPath, byte r, byte g, byte b, out string err, bool doubleOverlay)
        {
            object[] args = { absPath, r, g, b, null, doubleOverlay };
            bool ok = InvokeBool("LoadG16ToMemoryNationColor", args, out err);
            err = args[4] as string ?? err ?? string.Empty;
            return ok;
        }

        public static bool TryGetG16FrameRGBA(string absPath, int frameIndex, out int width, out int height, out byte[] rgba, out string err)
        {
            object[] args = { absPath, frameIndex, 0, 0, null, null };
            bool ok = InvokeBool("TryGetG16FrameRGBA", args, out err);
            width = args[2] is int w ? w : 0;
            height = args[3] is int h ? h : 0;
            rgba = args[4] as byte[];
            err = args[5] as string ?? err ?? string.Empty;
            return ok;
        }

        public static bool TryGetG16FrameRGBANationColor(string absPath, int frameIndex, byte r, byte g, byte b, out int width, out int height, out byte[] rgba, out string err)
        {
            object[] args = { absPath, frameIndex, r, g, b, 0, 0, null, null };
            bool ok = InvokeBool("TryGetG16FrameRGBANationColor", args, out err);
            width = args[5] is int w ? w : 0;
            height = args[6] is int h ? h : 0;
            rgba = args[7] as byte[];
            err = args[8] as string ?? err ?? string.Empty;
            return ok;
        }

        public static void ClearG16Memory()
        {
            if (!TryGetFacadeType(out var facadeType, out _))
            {
                return;
            }

            MethodInfo clearMethod = facadeType.GetMethod("ClearG16Memory", BindingFlags.Public | BindingFlags.Static);
            clearMethod?.Invoke(null, null);
        }

        private static bool InvokeBool(string methodName, object[] args, out string err)
        {
            if (!TryGetFacadeType(out var facadeType, out err))
            {
                return false;
            }

            MethodInfo method = facadeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                err = $"Melinoja bridge could not find method '{methodName}' on '{CodecFacadeTypeName}'. " +
                      BuildTypeDiagnostic(facadeType);
                return false;
            }

            try
            {
                object result = method.Invoke(null, args);
                if (result is bool boolResult)
                {
                    return boolResult;
                }

                err = $"Melinoja bridge method '{methodName}' returned a non-bool result.";
                return false;
            }
            catch (TargetInvocationException ex)
            {
                err = ex.InnerException?.Message ?? ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return false;
            }
        }

        private static bool InvokeStaticBoolByType(string typeName, string methodName, object[] args, out string err)
        {
            err = string.Empty;
            Type type = ResolveType(typeName);
            if (type == null)
            {
                err = $"Could not resolve type '{typeName}'.";
                return false;
            }

            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                err = $"Could not find method '{methodName}' on '{typeName}'. " + BuildTypeDiagnostic(type);
                return false;
            }

            try
            {
                object result = method.Invoke(null, args);
                if (result is bool boolResult)
                {
                    return boolResult;
                }

                err = $"Method '{typeName}.{methodName}' returned a non-bool result.";
                return false;
            }
            catch (TargetInvocationException ex)
            {
                err = ex.InnerException?.Message ?? ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return false;
            }
        }

        private static Type ResolveType(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            try
            {
                Assembly melinojaAssembly = Assembly.Load("Melinoja");
                return melinojaAssembly.GetType(typeName, throwOnError: false);
            }
            catch
            {
                return null;
            }
        }

        private static string BuildTypeDiagnostic(Type type)
        {
            try
            {
                string asmLocation = string.Empty;
                try { asmLocation = type.Assembly.Location ?? string.Empty; } catch { }

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                var sb = new StringBuilder();
                int count = 0;
                foreach (var m in methods)
                {
                    string name = m.Name ?? string.Empty;
                    if (name.IndexOf("Nation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("G2D", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("G16", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (count++ > 0) sb.Append(", ");
                        sb.Append(name);
                    }
                }

                return $"assembly='{type.Assembly.FullName}' location='{asmLocation}' matchingMethods=[{sb}]";
            }
            catch (Exception ex)
            {
                return "diagnostic failed: " + ex.Message;
            }
        }

        private static bool TryGetFacadeType(out Type facadeType, out string err)
        {
            if (!_codecFacadeResolved)
            {
                _codecFacadeResolved = true;
                _codecFacadeType = ResolveCodecFacadeType();
            }

            facadeType = _codecFacadeType;
            if (facadeType != null)
            {
                err = string.Empty;
                return true;
            }

            err = $"Melinoja bridge could not resolve '{CodecFacadeAssemblyQualifiedName}'.";
            return false;
        }

        private static Type ResolveCodecFacadeType()
        {
            Type facadeType = Type.GetType(CodecFacadeAssemblyQualifiedName, throwOnError: false);
            if (facadeType != null)
            {
                return facadeType;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                facadeType = assembly.GetType(CodecFacadeTypeName, throwOnError: false);
                if (facadeType != null)
                {
                    return facadeType;
                }
            }

            try
            {
                Assembly melinojaAssembly = Assembly.Load("Melinoja");
                return melinojaAssembly.GetType(CodecFacadeTypeName, throwOnError: false);
            }
            catch
            {
                return null;
            }
        }
    }

}
