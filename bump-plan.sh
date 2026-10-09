#!/bin/bash
# ============================================================
# bump-plan.sh — 分析项目依赖链路，输出版本更新与发包计划（只读，不实际修改）
# 用法: ./bump-plan.sh <变更项目名称> [patch|minor|major]
# 示例: ./bump-plan.sh Kurisu.AspNetCore.Abstractions patch
#       ./bump-plan.sh Kurisu.AspNetCore.Abstractions minor
# ============================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")" && pwd)"
CHANGED="${1:?用法: $0 <变更项目名称> [patch|minor|major]}"
BUMP="${2:-patch}"

if [[ "$BUMP" != "patch" && "$BUMP" != "minor" && "$BUMP" != "major" ]]; then
    echo "✗ 无效的升级类型: $BUMP (可选: patch, minor, major)"
    exit 1
fi

# ---- 语义化版本 bump 函数 ----
bump_version() {
    local ver="$1" type="$2"
    # 非标准版本号（无Version标签等）直接标记为 bump
    if [[ ! "$ver" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        echo "bump"
        return
    fi
    local major minor patch
    IFS='.' read -r major minor patch <<< "$ver"
    case "$type" in
        major) echo "$((major + 1)).0.0" ;;
        minor) echo "${major}.$((minor + 1)).0" ;;
        patch) echo "${major}.${minor}.$((patch + 1))" ;;
    esac
}

BUMP_LABEL="$(echo "$BUMP" | tr '[:lower:]' '[:upper:]')"

# ---- 1. 扫描所有 csproj，提取 name / version / ProjectReference ----
declare -A VERSION DEPS RDEPS CS_PATH

while IFS= read -r csproj; do
    name=$(basename "$csproj" .csproj)
    CS_PATH["$name"]="$csproj"
    ver=$(grep -oP '<Version>\K[^<]+' "$csproj" 2>/dev/null || echo "?.?.?")
    VERSION["$name"]="$ver"

    refs=""
    while IFS= read -r ref_name; do
        [[ -n "$ref_name" ]] && refs="$refs $ref_name"
    done < <(grep 'ProjectReference Include=' "$csproj" 2>/dev/null | sed 's/.*[\\/]\([^\\/"]*\)\.csproj.*/\1/' || true)
    DEPS["$name"]="$refs"
done < <(find "$REPO_ROOT/src" -name "*.csproj" -not -path "*/Kurisu.Transaction.Analyzer.Sample/*" -not -path "*/Kurisu.Transaction.Analyzer.Tests/*" | sort)

# ---- 2. 构建反向依赖图 ----
for proj in "${!DEPS[@]}"; do
    for dep in ${DEPS[$proj]}; do
        RDEPS["$dep"]="${RDEPS[$dep]:-} $proj"
    done
done

# ---- 3. 检查输入项目是否存在 ----
if [[ -z "${VERSION[$CHANGED]:-}" ]]; then
    echo "✗ 找不到项目: $CHANGED"
    echo "  可用项目:"
    for p in $(echo "${!VERSION[@]}" | tr ' ' '\n' | sort); do
        printf "    %-52s %s\n" "$p" "${VERSION[$p]}"
    done
    exit 1
fi

# ---- 4. 计算新版本号 ----
declare -A NEW_VERSION
NEW_VERSION["$CHANGED"]=$(bump_version "${VERSION[$CHANGED]}" "$BUMP")

# ---- 5. BFS 找出所有传递依赖者并分配 round ----
declare -A ROUND VISITED
queue=("$CHANGED")
VISITED["$CHANGED"]=1
ROUND["$CHANGED"]=0

while [[ ${#queue[@]} -gt 0 ]]; do
    cur="${queue[0]}"
    queue=("${queue[@]:1}")
    cur_round=${ROUND[$cur]}

    for rdep in ${RDEPS[$cur]:-}; do
        candidate=$((cur_round + 1))
        if [[ -z "${ROUND[$rdep]:-}" ]] || [[ $candidate -gt ${ROUND[$rdep]} ]]; then
            ROUND["$rdep"]=$candidate
        fi
        if [[ -z "${VISITED[$rdep]:-}" ]]; then
            VISITED["$rdep"]=1
            queue+=("$rdep")
        fi
    done
done

# 为所有受影响项目计算新版本（统一用相同 bump 类型）
for proj in "${!ROUND[@]}"; do
    if [[ "$proj" != "$CHANGED" ]]; then
        NEW_VERSION["$proj"]=$(bump_version "${VERSION[$proj]}" "$BUMP")
    fi
done

# ---- 6. 计算 max_round / 收集不受影响列表 ----
max_round=0
for p in "${!ROUND[@]}"; do
    [[ ${ROUND[$p]} -gt $max_round ]] && max_round=${ROUND[$p]}
done

unaffected=""
for proj in $(echo "${!VERSION[@]}" | tr ' ' '\n' | sort); do
    [[ -z "${ROUND[$proj]:-}" ]] && unaffected="$unaffected $proj"
done

# ---- 7. 不受影响项目表格（置顶） ----
echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  版本更新与发包计划（DRY-RUN）                               ║"
echo "║  变更: $CHANGED  升级: $BUMP_LABEL"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

if [[ -n "$unaffected" ]]; then
    echo "━━━ 不受影响，无需更新 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    printf "  %-50s │ %10s\n" "项目" "版本"
    printf "  %-50s─┼─%s\n" "--------------------------------------------------" "----------"
    for proj in $unaffected; do
        printf "  %-50s │ %10s\n" "$proj" "${VERSION[$proj]}"
    done
else
    echo "━━━ 不受影响: (无，所有项目均受影响) ━━━"
fi

# ---- 8. 更新链路详情（按轮次） ----
echo ""
echo "━━━ 更新链路详情 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

for round in $(seq 1 $max_round); do
    echo ""
    echo "▶ Round $round $([[ $round -eq 1 ]] && echo "(直接依赖者)" || echo "(间接受影响)")"

    for proj in $(echo "${!ROUND[@]}" | tr ' ' '\n' | sort); do
        if [[ ${ROUND[$proj]} -eq $round ]]; then
            affected_deps=""
            for dep in ${DEPS[$proj]}; do
                if [[ -n "${ROUND[$dep]:-}" ]]; then
                    affected_deps="$affected_deps $dep(${VERSION[$dep]}→${NEW_VERSION[$dep]})"
                fi
            done
            printf "  %-48s %s → %s\n" "$proj" "${VERSION[$proj]}" "${NEW_VERSION[$proj]}"
            if [[ -n "$affected_deps" ]]; then
                printf "    ↳ 变更触发自:%s\n" "$affected_deps"
            fi
        fi
    done
done

# ---- 9. 依赖关系可视化 ----
echo ""
echo "━━━ 受影响子图的依赖链路 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
print_tree() {
    local proj="$1" indent="$2" prefix="$3"

    if [[ -n "${ROUND[$proj]:-}" ]]; then
        local round_label=""
        [[ ${ROUND[$proj]} -gt 0 ]] && round_label=" [R${ROUND[$proj]}]"
        echo "${indent}${prefix}${proj} ${VERSION[$proj]} → ${NEW_VERSION[$proj]}${round_label}"

        local children="${RDEPS[$proj]:-}"
        local count=0
        for child in $children; do
            [[ -n "${ROUND[$child]:-}" ]] && [[ ${ROUND[$child]} -gt ${ROUND[$proj]} ]] && count=$((count + 1))
        done

        local i=0
        for child in $children; do
            if [[ -n "${ROUND[$child]:-}" ]] && [[ ${ROUND[$child]} -gt ${ROUND[$proj]} ]]; then
                i=$((i + 1))
                [[ $i -eq $count ]] && print_tree "$child" "${indent}    " "└── " || print_tree "$child" "${indent}    " "├── "
            fi
        done
    fi
}

print_tree "$CHANGED" "" ""

# ---- 10. 更新计划表格（末尾） ----
echo ""
echo "━━━ 需要更新版本并发包 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
printf "  %-4s │ %-50s │ %10s │ %10s │ %s\n" "轮次" "项目" "当前版本" "新版本" "状态"
printf "  %-4s─┼─%s─┼─%s─┼─%s─┼─%s\n" "----" "--------------------------------------------------" "----------" "----------" "--------"

for round in $(seq 0 $max_round); do
    for proj in $(echo "${!ROUND[@]}" | tr ' ' '\n' | sort); do
        if [[ ${ROUND[$proj]} -eq $round ]]; then
            if [[ $round -eq 0 ]]; then
                printf "  %-4s │ %-50s │ %10s │ %10s │ %s\n" "R$round" "$proj" "${VERSION[$proj]}" "${NEW_VERSION[$proj]}" "变更起点"
            elif [[ $round -eq 1 ]]; then
                printf "  %-4s │ %-50s │ %10s │ %10s │ %s\n" "R$round" "$proj" "${VERSION[$proj]}" "${NEW_VERSION[$proj]}" "直接依赖"
            else
                printf "  %-4s │ %-50s │ %10s │ %10s │ %s\n" "R$round" "$proj" "${VERSION[$proj]}" "${NEW_VERSION[$proj]}" "间接依赖"
            fi
        fi
    done
done

echo ""
echo "  --------------------------------------------------------------"
printf "  共 %d 轮，%d 个项目需更新版本并发包\n" "$(($max_round + 1))" "${#ROUND[@]}"
echo "  每轮内可并行，轮间必须串行（下游 nuspec 依赖上游新版本号）"

# ---- 11. 生成 package.bump.json ----
JSON_FILE="package.bump.json"

cat > "$JSON_FILE" << JSONEOF
{
  "changed": "$CHANGED",
  "bump": "$BUMP",
  "rounds": $(($max_round + 1)),
  "total": $((${#ROUND[@]})),
  "projects": [
JSONEOF

first=1
for round in $(seq 0 $max_round); do
    for proj in $(echo "${!ROUND[@]}" | tr ' ' '\n' | sort); do
        if [[ ${ROUND[$proj]} -eq $round ]]; then
            [[ $first -eq 1 ]] && first=0 || echo "," >> "$JSON_FILE"
            cat >> "$JSON_FILE" << JSONEOF
    { "name": "$proj", "current": "${VERSION[$proj]}", "new": "${NEW_VERSION[$proj]}", "round": $round, "updated": false, "csproj": "${CS_PATH[$proj]#$REPO_ROOT/}" }
JSONEOF
        fi
    done
done

cat >> "$JSON_FILE" << JSONEOF
  ]
}
JSONEOF

echo ""
echo "━━━ 已生成 package.bump.json ━━━━━━━━━━━━━━━━━━━━━━━━━━"

# ---- 12. 生成发布脚本 ----
SCRIPT="do-bump-${CHANGED}-${BUMP}.sh"

# 按 round 排序
round_sorted=""
for round in $(seq 0 $max_round); do
    for proj in $(echo "${!ROUND[@]}" | tr ' ' '\n' | sort); do
        [[ ${ROUND[$proj]} -eq $round ]] && round_sorted="$round_sorted $proj"
    done
done

cat > "$SCRIPT" << SCRIPTEOF
#!/bin/bash
# ============================================================
# 自动生成于: $(date '+%Y-%m-%d %H:%M:%S')
# 变更项目: $CHANGED
# 升级类型: $BUMP_LABEL
# 受影响: $(echo $round_sorted | wc -w) 个项目，$(($max_round + 1)) 轮
#
# 幂等: 可重复执行，已更新项目自动跳过
#
# 环境变量:
#   NUGET_SERVER_URL   NuGet 服务器地址 (必须设置)
#   NUGET_API_KEY      NuGet API Key   (必须设置)
# ============================================================
set -euo pipefail
REPO_ROOT="\$(cd "\$(dirname "\$0")" && pwd)"
JSON_FILE="\$REPO_ROOT/${JSON_FILE}"
NUGET_SERVER_URL="\${NUGET_SERVER_URL:?请设置环境变量 NUGET_SERVER_URL}"
NUGET_API_KEY="\${NUGET_API_KEY:?请设置环境变量 NUGET_API_KEY}"

# ---- Step 1: 更新 csproj Version（幂等，依赖 python3） ----
echo ""
echo "===== Step 1: 更新 csproj Version ====="
python3 -c "
import json, subprocess, sys, os

with open('\$JSON_FILE') as f:
    data = json.load(f)

any_update = False
for p in data['projects']:
    name = p['name']
    csproj = os.path.join('\$REPO_ROOT', p['csproj'])
    old_v = p['current']
    new_v = p['new']

    if not os.path.exists(csproj):
        print(f'  ✗ {name}: 文件不存在 {csproj}')
        sys.exit(1)

    content = open(csproj).read()
    marker = f'<Version>{old_v}</Version>'

    if marker in content:
        open(csproj, 'w').write(content.replace(marker, f'<Version>{new_v}</Version>'))
        p['updated'] = True
        any_update = True
        print(f'  ✓ {name}  {old_v} → {new_v}')
    elif f'<Version>{new_v}</Version>' in content:
        p['updated'] = True
        print(f'  - {name}  已更新，跳过')
    else:
        print(f'  ✗ {name}: csproj中Version不是{old_v}也不是{new_v}，请手动处理')
        sys.exit(1)

if any_update:
    with open('\$JSON_FILE', 'w') as f:
        json.dump(data, f, indent=2)
        f.write(chr(10))

if not any_update:
    print('  所有项目已是最新版本，无需更新')
"

# ---- Step 2: 按轮次 pack 并 push NuGet ----
echo ""
echo "===== Step 2: 打包并发包 ====="
SCRIPTEOF

round_num=0
for proj in $round_sorted; do
    new="${NEW_VERSION[$proj]}"
    file="${CS_PATH[$proj]}"
    round=${ROUND[$proj]}
    csproj_rel="${file#$REPO_ROOT/}"

    if [[ $round -gt $round_num ]]; then
        round_num=$round
        cat >> "$SCRIPT" << SCRIPTEOF
echo ""
echo "━━━ Round $round ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
SCRIPTEOF
    fi

    cat >> "$SCRIPT" << SCRIPTEOF
dotnet build "\$REPO_ROOT/${csproj_rel}" -c Release
dotnet nuget push "\$REPO_ROOT/src/${proj}/bin/Release/${proj}.${new}.nupkg" --source "\$NUGET_SERVER_URL" --api-key "\$NUGET_API_KEY" --skip-duplicate --no-service-endpoint
SCRIPTEOF
done

cat >> "$SCRIPT" << SCRIPTEOF

echo ""
echo "===== 发包完成 ====="
SCRIPTEOF

chmod +x "$SCRIPT"

echo ""
echo "━━━ 已生成发布脚本 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  ./$SCRIPT"
echo "  执行该脚本会: 1) 更新 csproj Version  2) 按轮次 pack + push NuGet"
echo "  支持幂等: 重复执行自动跳过已更新项目"
