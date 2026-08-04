using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace TrickcalRevive.Architecture.Tests
{
    // asmdef가 이미 컴파일 단계에서 계층 위반을 막지만, 이 테스트는 두 가지를 더 한다.
    //   1. 위반이 생겼을 때 "어느 파일이 어느 계층을 참조했는지"를 이름으로 알려준다.
    //      asmdef 오류만 보면 타입을 못 찾는다는 말만 나와 원인을 찾기 어렵다.
    //   2. asmdef 자체가 실수로 지워지거나 references가 추가돼도 규칙을 지킨다.
    public sealed class LayerDependencyTests
    {
        // 각 계층이 참조해도 되는 계층. 자기 자신은 항상 허용.
        private static readonly Dictionary<string, string[]> AllowedReferences =
            new Dictionary<string, string[]>
            {
                ["Core"] = new string[0],
                ["Data"] = new[] { "Core" },
                ["Domain"] = new[] { "Core", "Data" },
                ["Infra"] = new[] { "Core", "Data", "Domain" },
                ["Presentation"] = new[] { "Core", "Data", "Domain", "Infra" },
                ["App"] = new[] { "Core", "Data", "Domain", "Infra", "Presentation" },
            };

        private static readonly Regex UsingPattern =
            new Regex(@"^\s*using\s+TrickcalRevive\.(\w+)", RegexOptions.Multiline);

        [Test]
        public void 계층은_허용된_방향으로만_참조한다()
        {
            var layerRootBase = FindLayerRootBase();
            var violations = new List<string>();

            foreach (var layer in AllowedReferences.Keys)
            {
                var layerRoot = Path.Combine(layerRootBase, layer);
                if (!Directory.Exists(layerRoot))
                    continue;

                var allowed = new HashSet<string>(AllowedReferences[layer]) { layer };

                foreach (var file in Directory.GetFiles(layerRoot, "*.cs", SearchOption.AllDirectories))
                {
                    // Editor 전용 툴은 별도 asmdef로 분리돼 있어 규칙 밖이다.
                    if (file.Replace('\\', '/').Contains("/Editor/"))
                        continue;

                    foreach (Match match in UsingPattern.Matches(File.ReadAllText(file)))
                    {
                        var referenced = match.Groups[1].Value;
                        if (!AllowedReferences.ContainsKey(referenced) || allowed.Contains(referenced))
                            continue;

                        violations.Add($"{layer}/{Path.GetFileName(file)} -> {referenced}");
                    }
                }
            }

            Assert.That(violations, Is.Empty,
                "계층 참조 방향 위반:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
        }

        private static string FindLayerRootBase()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "Game");
                if (Directory.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Assets/Game을 찾지 못했다.");
        }
    }
}
