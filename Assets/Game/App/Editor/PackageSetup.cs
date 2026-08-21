using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TrickcalRevive.App.Editor
{
    // UI 작업 착수 전 기반 패키지 준비. TextMeshPro는 Unity 6부터 com.unity.ugui에
    // 번들돼 있어 별도 패키지 설치는 필요 없고, Essential Resources만 있으면 된다.
    //
    // AssetDatabase.ImportPackage와 메뉴(Window/TextMeshPro/...)는 배치모드에서
    // 조용히 아무 일도 안 하는 경우가 있어(그래픽 디바이스 유무와 무관하게 재현됨),
    // .unitypackage(gzip으로 압축한 tar) 안의 GUID 폴더(asset/asset.meta/pathname)를
    // 직접 풀어 Assets 밑에 복사하는 방식으로 우회한다.
    public static class PackageSetup
    {
        [MenuItem("Tools/Trickcal Revive/Setup/Import TMP Essential Resources")]
        public static void ImportTmpEssentials()
        {
            var matches = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
            if (matches.Length == 0)
            {
                Debug.LogError("[PackageSetup] TMP Essential Resources.unitypackage를 PackageCache에서 못 찾았다.");
                EditorApplication.Exit(1);
                return;
            }

            var extractedCount = ExtractUnityPackage(matches[0], "Assets");
            Debug.Log($"[PackageSetup] {extractedCount}개 파일 추출 완료.");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// .unitypackage 내부 각 항목은 GUID 폴더 밑에 pathname(대상 경로 텍스트),
        /// asset(실제 바이트), asset.meta(메타)로 구성된다. tar 엔트리를 순서 상관없이
        /// 모아뒀다가 pathname을 기준으로 asset/asset.meta를 최종 위치에 쓴다.
        /// </summary>
        private static int ExtractUnityPackage(string packagePath, string destinationRoot)
        {
            var entriesByGuid = new Dictionary<string, (string pathname, byte[] asset, byte[] meta)>();

            using (var fileStream = File.OpenRead(packagePath))
            using (var gzip = new GZipStream(fileStream, CompressionMode.Decompress))
            using (var buffered = new MemoryStream())
            {
                gzip.CopyTo(buffered);
                buffered.Position = 0;

                var header = new byte[512];
                while (true)
                {
                    var read = buffered.Read(header, 0, 512);
                    if (read < 512 || IsAllZero(header))
                        break;

                    var name = ReadAsciiZ(header, 0, 100);
                    if (string.IsNullOrEmpty(name))
                        break;

                    var sizeOctal = ReadAsciiZ(header, 124, 12).Trim();
                    var size = string.IsNullOrEmpty(sizeOctal) ? 0 : Convert.ToInt64(sizeOctal, 8);
                    var typeFlag = (char)header[156];

                    byte[] content = Array.Empty<byte>();
                    if (size > 0)
                    {
                        content = new byte[size];
                        var totalRead = 0;
                        while (totalRead < size)
                        {
                            var chunk = buffered.Read(content, totalRead, (int)(size - totalRead));
                            if (chunk <= 0)
                                break;
                            totalRead += chunk;
                        }

                        var padded = (int)((512 - (size % 512)) % 512);
                        if (padded > 0)
                            buffered.Seek(padded, SeekOrigin.Current);
                    }

                    if (typeFlag == '5') // 디렉터리
                        continue;

                    var normalizedName = name.StartsWith("./", StringComparison.Ordinal) ? name.Substring(2) : name;
                    var parts = normalizedName.Split('/');
                    if (parts.Length < 2)
                        continue;

                    var guid = parts[0];
                    var fileName = parts[1];

                    if (!entriesByGuid.TryGetValue(guid, out var entry))
                        entry = (null, null, null);

                    switch (fileName)
                    {
                        case "pathname":
                            entry.pathname = Encoding.UTF8.GetString(content).TrimEnd('\0', '\n', '\r');
                            break;
                        case "asset":
                            entry.asset = content;
                            break;
                        case "asset.meta":
                            entry.meta = content;
                            break;
                    }

                    entriesByGuid[guid] = entry;
                }
            }

            var written = 0;
            foreach (var entry in entriesByGuid.Values)
            {
                if (string.IsNullOrEmpty(entry.pathname) || entry.asset == null)
                    continue;

                // pathname은 "Assets/..." 형태로 이미 포함돼 있다.
                var targetPath = entry.pathname.StartsWith("Assets/", StringComparison.Ordinal)
                    ? entry.pathname
                    : Path.Combine(destinationRoot, entry.pathname).Replace('\\', '/');

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.WriteAllBytes(targetPath, entry.asset);
                written++;

                if (entry.meta != null)
                    File.WriteAllBytes(targetPath + ".meta", entry.meta);
            }

            return written;
        }

        private static bool IsAllZero(byte[] buffer)
        {
            foreach (var b in buffer)
            {
                if (b != 0)
                    return false;
            }
            return true;
        }

        private static string ReadAsciiZ(byte[] buffer, int offset, int length)
        {
            var end = offset;
            var max = offset + length;
            while (end < max && buffer[end] != 0)
                end++;
            return Encoding.ASCII.GetString(buffer, offset, end - offset);
        }
    }
}
