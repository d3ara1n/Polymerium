# macOS DMG 分发方案

## 背景

当前 Velopack 在 macOS 上产出 `.pkg` 安装包，用户双击会触发 Gatekeeper 报"安装包已损坏"，必须终端执行 `xattr -d com.apple.quarantine` 才能安装，体验极差。

原因是 macOS 对未签名且未公证（Notarization）的 pkg 实施严格拦截，甚至不提供右键"打开"的选项。

## 方案：将 macOS 分发格式从 pkg 改为 DMG

DMG 是 macOS 最常见的分发格式，打开后呈现"拖拽到 /Applications"的可视化引导界面。不带签名不带公证的 DMG，用户右键"打开"即可使用，不会报"已损坏"。

### 产出物变化

| 原来 | 改为 |
|------|------|
| `Polymerium-x.x.x-osx-arm64.pkg` | 删除，不再生成 |
| `Polymerium-x.x.x-osx-arm64-Portable.zip` | `Polymerium-x.x.x-osx-arm64.dmg` |

### CI 流程改动（publish.yml）

在 `Pack with Velopack` 步骤之后、`Upload Build Artifact` 之前，新增一个步骤：

```yaml
- name: Create DMG (macOS only)
  if: matrix.runtime == 'osx-arm64'
  run: |
      # 1. 解压 Portable.zip 得到 .app
      cd Releases
      unzip Polymerium-*-osx-arm64-Portable.zip -d portable_extract
      APP_NAME=$(ls portable_extract/*.app 2>/dev/null | head -1 | xargs basename)
      APP_PATH="portable_extract/$APP_NAME"

      # 2. 准备 DMG 临时目录
      DMG_STAGING="/tmp/dmg-staging"
      rm -rf "$DMG_STAGING"
      mkdir -p "$DMG_STAGING"
      cp -R "$APP_PATH" "$DMG_STAGING/"
      ln -s /Applications "$DMG_STAGING/Applications"

      # 3. 用 hdiutil 创建 DMG
      DMG_OUTPUT="Polymerium-${{ steps.get_version.outputs.version }}-osx-arm64.dmg"
      hdiutil create -volname "Polymerium" \
          -srcfolder "$DMG_STAGING" \
          -ov -format UDZO \
          "$DMG_OUTPUT"

      # 4. 移动 DMG 到 Releases 目录
      mv "$DMG_OUTPUT" .

      # 5. 清理：删除 Portable.zip 和 pkg（可选保留 Portable.zip）
      rm -rf portable_extract
      rm -rf "$DMG_STAGING"
```

Velopack 打包步骤加 `--noInst` 跳过 pkg 生成：

```yaml
elif [ "${{ matrix.runtime }}" = "osx-arm64" ]; then
    pack_extra_args="$pack_extra_args --icon ./src/Polymerium.App/Assets/Icon.icns --bundleId dev.dearain.Polymerium --noInst"
```

### 本地脚本改动（Publish-Velopack.ps1）

macOS 分支加上 `--noInst`，pack 完成后手动生成 DMG：

```powershell
if ($Rid -like "osx-*") {
    $VpkArgs += @("--bundleId", "dev.dearain.Polymerium", "--noInst")
    # ... vpk pack 执行后 ...

    # 生成 DMG
    $PortableZip = Get-ChildItem "Releases/*-Portable.zip" | Select-Object -First 1
    $DmgStaging = "/tmp/dmg-staging"
    New-Item -ItemType Directory -Force -Path $DmgStaging | Out-Null
    Expand-Archive $PortableZip.FullName -DestinationPath "$DmgStaging/app"
    $AppName = (Get-ChildItem "$DmgStaging/app/*.app" | Select-Object -First 1).Name
    Copy-Item -Recurse "$DmgStaging/app/$AppName" "$DmgStaging/"
    New-Item -ItemType SymbolicLink -Path "$DmgStaging/Applications" -Target "/Applications" | Out-Null
    $DmgOutput = "Releases/Polymerium-$Version-osx-arm64.dmg"
    hdiutil create -volname "Polymerium" -srcfolder $DmgStaging -ov -format UDZO $DmgOutput
    Remove-Item -Recurse -Force $DmgStaging
}
```

## 进阶：带背景图的 DMG

上面的方案生成的是朴素 DMG（没有背景布局）。如需经典的 macOS DMG 窗口布局（应用图标在左、Applications 文件夹在右、带背景图），可以用 [create-dmg](https://github.com/create-dmg/create-dmg)：

```bash
brew install create-dmg

create-dmg \
    --volname "Polymerium" \
    --volicon "./src/Polymerium.App/Assets/Icon.icns" \
    --window-pos 200 120 \
    --window-size 600 400 \
    --icon-size 100 \
    --icon "Polymerium.app" 175 190 \
    --hide-extension "Polymerium.app" \
    --app-drop-link 425 190 \
    "Polymerium-x.x.x-osx-arm64.dmg" \
    "portable_extract/"
```

需要准备一张 DMG 背景图（`--background` 参数），通常是 600×400 的 PNG。

## 进阶：Apple 证书签名 + 公证

如有 Apple Developer 账号（$99/年），可以进一步签名和公证 DMG，让用户双击即可使用，无任何警告：

```bash
# 1. 先对 .app 签名
codesign --sign "Developer ID Application: Your Name (TEAMID)" \
    --options runtime \
    --entitlements entitlements.plist \
    --deep --force \
    Polymerium.app

# 2. 公证（提交给 Apple 服务器）
xcrun notarytool submit Polymerium.dmg \
    --apple-id "your@email.com" \
    --team-id "TEAMID" \
    --password "app-specific-password" \
    --wait

# 3. 盖章
xcrun stapler staple Polymerium.dmg
```

Velopack 的 `vpk pack` 已内置 `--signAppIdentity` / `--signInstallIdentity` / `--notaryProfile` 参数，可在打包阶段直接完成签名和公证，无需额外脚本。但此方案前提是有 Apple Developer 证书。

## 参考资料

- [create-dmg](https://github.com/create-dmg/create-dmg) — 生成带背景图的 DMG
- Velopack `vpk pack --help` — 查看 `--noInst`、`--signAppIdentity`、`--notaryProfile` 等参数
- macOS Gatekeeper 机制：未签名 + 未公证 = 阻止运行；已签名但未公证 = 右键可打开；已签名 + 已公证 = 双击直接运行