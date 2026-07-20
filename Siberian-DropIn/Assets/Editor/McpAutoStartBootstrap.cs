#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Siberian.EditorTools
{
    /// <summary>
    /// Автозапуск моста MCP For Unity (пакет com.coplaydev.unity-mcp) при загрузке редактора.
    /// Паттерн — по образцу McpAutoStartBootstrap.cs из проекта "Setup Guide In-Editor Tutorial".
    /// Мост поднимает эндпоинт на 127.0.0.1:8080/mcp.
    ///
    /// Пакет менял неймспейсы между версиями, поэтому запуск идёт через reflection
    /// по списку известных имён типов — при обновлении пакета скрипт не сломает компиляцию.
    /// Отключить автозапуск: меню Siberian/MCP/Toggle Auto-Start.
    /// </summary>
    [InitializeOnLoad]
    public static class McpAutoStartBootstrap
    {
        const string AutoStartPref = "Siberian.McpAutoStart";

        static readonly string[] BridgeTypeNames =
        {
            "MCPForUnity.Editor.MCPForUnityBridge",
            "MCPForUnity.Editor.Services.BridgeControlService",
            "MCPForUnity.Editor.MCPServiceManager",
            "UnityMcpBridge.Editor.UnityMcpBridge",
        };

        static readonly string[] StartMethodNames =
        {
            "Start", "StartBridge", "EnsureStarted", "StartAutoConnect", "Connect",
        };

        static McpAutoStartBootstrap()
        {
            if (!EditorPrefs.GetBool(AutoStartPref, true))
                return;
            EditorApplication.delayCall += TryStartBridge;
        }

        [MenuItem("Siberian/MCP/Start Bridge Now")]
        public static void TryStartBridge()
        {
            foreach (var typeName in BridgeTypeNames)
            {
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(typeName, throwOnError: false))
                    .FirstOrDefault(t => t != null);
                if (type == null)
                    continue;

                foreach (var methodName in StartMethodNames)
                {
                    var method = type.GetMethod(methodName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                        binder: null, types: Type.EmptyTypes, modifiers: null);
                    if (method == null)
                        continue;

                    try
                    {
                        method.Invoke(null, null);
                        Debug.Log($"[Siberian MCP] Мост запущен через {type.Name}.{methodName}(). Эндпоинт: http://127.0.0.1:8080/mcp");
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Siberian MCP] {type.Name}.{methodName}() бросил исключение: {e.InnerException?.Message ?? e.Message}");
                    }
                }
            }

            Debug.LogWarning("[Siberian MCP] Пакет com.coplaydev.unity-mcp не найден или его API не распознан. " +
                             "Проверьте, что пакет добавлен в Packages/manifest.json (см. Siberian-DropIn/Tools/manifest-snippet.jsonc), " +
                             "затем откройте Window > MCP For Unity и запустите мост вручную один раз.");
        }

        [MenuItem("Siberian/MCP/Toggle Auto-Start")]
        public static void ToggleAutoStart()
        {
            bool next = !EditorPrefs.GetBool(AutoStartPref, true);
            EditorPrefs.SetBool(AutoStartPref, next);
            Debug.Log($"[Siberian MCP] Автозапуск моста: {(next ? "ВКЛ" : "ВЫКЛ")}");
        }
    }
}
#endif
