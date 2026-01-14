 

#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AiToolkit_EnumFallbackPatch
{
    static AiToolkit_EnumFallbackPatch()
    {
        // Даем Unity/пакетам загрузиться
        EditorApplication.delayCall += TryPatch;
    }

    private static void TryPatch()
    {
        try
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "AiEditorToolsSdk", StringComparison.OrdinalIgnoreCase));

            if (asm == null)
            {
                Debug.Log("[AI Toolkit Patch] AiEditorToolsSdk assembly not loaded yet.");
                return;
            }

            int patched = 0;

            foreach (var t in asm.GetTypes())
            {
                // Ищем статические свойства типа JsonSerializerSettings
                var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(p => p.PropertyType.FullName == "Newtonsoft.Json.JsonSerializerSettings" && p.CanRead);

                foreach (var p in props)
                {
                    object val = null;
                    try { val = p.GetValue(null, null); } catch { }

                    if (val is JsonSerializerSettings settings)
                    {
                        if (!settings.Converters.OfType<SafeEnumFallbackConverter>().Any())
                        {
                            // Вставляем первым, чтобы он перехватывал enum до их конвертеров
                            settings.Converters.Insert(0, new SafeEnumFallbackConverter());
                            patched++;
                        }
                    }
                }

                // Ищем методы UpdateJsonSerializerSettings(JsonSerializerSettings) — если есть, дернем после
                var upd = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.Name == "UpdateJsonSerializerSettings" &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType.FullName == "Newtonsoft.Json.JsonSerializerSettings");

                if (upd != null)
                {
                    // Попробуем создать settings через CreateSerializerSettings, если он есть
                    var create = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "CreateSerializerSettings" &&
                                             m.GetParameters().Length == 0 &&
                                             m.ReturnType.FullName == "Newtonsoft.Json.JsonSerializerSettings");

                    if (create != null)
                    {
                        try
                        {
                            var settings = create.Invoke(null, null) as JsonSerializerSettings;
                            if (settings != null && !settings.Converters.OfType<SafeEnumFallbackConverter>().Any())
                            {
                                settings.Converters.Insert(0, new SafeEnumFallbackConverter());
                                // Даем SDK шанс “донастроить”, но наш конвертер уже первым
                                upd.Invoke(null, new object[] { settings });
                                patched++;
                            }
                        }
                        catch { }
                    }
                }
            }

            Debug.Log($"[AI Toolkit Patch] Safe enum fallback injected into {patched} JsonSerializerSettings instance(s).");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Конвертер, который не падает на неизвестных enum значениях.
    /// Особенно важно для CategoryEnumV1 ("Textures" и т.п.).
    /// </summary>
    private sealed class SafeEnumFallbackConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var t = Nullable.GetUnderlyingType(objectType) ?? objectType;
            return t.IsEnum;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;
            var s = (reader.Value ?? "").ToString();

            // Пытаемся распарсить
            if (Enum.TryParse(enumType, s, true, out object parsed))
                return parsed;

            // Fallback: Unknown если есть
            var names = Enum.GetNames(enumType);
            if (names.Any(n => string.Equals(n, "Unknown", StringComparison.OrdinalIgnoreCase)))
                return Enum.Parse(enumType, "Unknown", true);

            // иначе default(0)
            return Activator.CreateInstance(enumType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }
    }
}
#endif
