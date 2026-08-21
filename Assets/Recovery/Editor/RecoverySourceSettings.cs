using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrickcalRevive.Recovery.Editor
{
    internal static class RecoverySourceSettings
    {
        private const string EnvironmentVariable = "TRICKCAL_REFERENCE_ROOT";
        private const string SettingsRelativePath = "UserSettings/TrickcalRecoverySettings.json";

        [Serializable]
        private sealed class SettingsData
        {
            public string sourceRoot = string.Empty;
        }

        internal static bool TryResolve(out string sourceRoot, out string error)
        {
            var environmentRoot = Environment.GetEnvironmentVariable(EnvironmentVariable);
            if (IsValid(environmentRoot))
            {
                sourceRoot = Path.GetFullPath(environmentRoot);
                error = string.Empty;
                return true;
            }

            var settingsPath = GetProjectPath(SettingsRelativePath);
            if (File.Exists(settingsPath))
            {
                try
                {
                    var settings = JsonUtility.FromJson<SettingsData>(File.ReadAllText(settingsPath));
                    if (settings != null && IsValid(settings.sourceRoot))
                    {
                        sourceRoot = Path.GetFullPath(settings.sourceRoot);
                        error = string.Empty;
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    sourceRoot = string.Empty;
                    error = $"로컬 복구 설정을 읽지 못했습니다: {exception.Message}";
                    return false;
                }
            }

            sourceRoot = string.Empty;
            error = $"{EnvironmentVariable} 또는 {SettingsRelativePath}에 유효한 원본 경로를 설정하세요.";
            return false;
        }

        [MenuItem("Tools/Trickcal Revive/Recovery/Configure Source Root")]
        private static void ConfigureSourceRoot()
        {
            var selected = EditorUtility.OpenFolderPanel("Trickcal Reference 원본 선택", string.Empty, string.Empty);
            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }

            if (!IsValid(selected))
            {
                EditorUtility.DisplayDialog(
                    "유효하지 않은 원본",
                    "00_original_apk/trickcal.apk와 file_manifest.csv가 모두 필요합니다.",
                    "확인");
                return;
            }

            var settingsPath = GetProjectPath(SettingsRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath) ?? GetProjectRoot());
            var json = JsonUtility.ToJson(new SettingsData { sourceRoot = Path.GetFullPath(selected) }, true);
            File.WriteAllText(settingsPath, json);
            Debug.Log($"복구 원본 경로를 로컬 UserSettings에 저장했습니다: {settingsPath}");
        }

        [MenuItem("Tools/Trickcal Revive/Recovery/Validate Source Root")]
        private static void ValidateSourceRoot()
        {
            if (!TryResolve(out var sourceRoot, out var error))
            {
                throw new InvalidOperationException(error);
            }

            Debug.Log($"복구 원본 경로가 유효합니다: {sourceRoot}");
        }

        private static bool IsValid(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            try
            {
                var root = Path.GetFullPath(candidate);
                return File.Exists(Path.Combine(root, "00_original_apk", "trickcal.apk")) &&
                       File.Exists(Path.Combine(root, "file_manifest.csv"));
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ?? throw new InvalidOperationException("Unity 프로젝트 루트를 찾을 수 없습니다.");
        }

        private static string GetProjectPath(string relativePath)
        {
            return Path.Combine(GetProjectRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
