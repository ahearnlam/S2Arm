// S2Arm（不是上游文件）：大版本首次打开的新功能大窗（11-13 维护者定：启动器首次进入默认弹出，图要大，说清是什么 / 怎么用 / 在哪用；iOS 不做）。
// 内容随包：docs/site/feature.json + feature/ 里的图；弹不弹、每期只弹一次在 pack/jobs/updater.py feature_spotlight / inbox，这里只显示。
// 几页轮播：每页上面一张大图，下面标题 + 三格；底栏页点、「了解更多」、跳过 / 上一步、下一步 / 去出包。关掉（含 Esc、跳过）= 这一期看过。
// Windows 同一份内容与布局：launchers/windows/launcher/App/FeatureSpotlight.cs。
import AppKit
import SwiftUI
import NXLauncher
import S2ArmAppleAdapter

struct FeatureSpotlightView: View {
    @ObservedObject var model: S2ArmLauncherModel
    let language: String
    @State private var index = 0

    private static let gold = Color(red: 0.91, green: 0.72, blue: 0.36)
    private static let gold2 = Color(red: 0.96, green: 0.83, blue: 0.55)
    private static let card = Color(red: 0.125, green: 0.125, blue: 0.14)

    private func t(_ id: String) -> String { BackendText.launcherText(id, language: language) }
    private var popup: [String: Any] { model.updatePopup ?? [:] }
    private var pages: [[String: Any]] { popup["pages"] as? [[String: Any]] ?? [] }
    private var page: [String: Any] { pages.indices.contains(index) ? pages[index] : [:] }
    private var last: Bool { index >= pages.count - 1 }

    /// 一段中英文字：{zh-Hans, en}，「**…**」加粗
    private func text(_ value: Any?) -> AttributedString {
        let item = value as? [String: Any] ?? [:]
        let raw = (language == "en" ? item["en"] as? String : nil) ?? item["zh-Hans"] as? String ?? ""
        return (try? AttributedString(markdown: raw)) ?? AttributedString(raw)
    }

    var body: some View {
        VStack(spacing: 0) {
            art
            VStack(alignment: .leading, spacing: 0) {
                Text(text(page["kicker"])).font(.manrope(12.5, .bold)).tracking(0.8).foregroundStyle(Self.gold)
                Text(text(page["title"])).font(.manrope(26, .heavy)).padding(.top, 6)
                HStack(alignment: .top, spacing: 14) {
                    cell("feature.what", page["what"]); cell("feature.how", page["how"]); cell("feature.where", page["where"])
                }
                .padding(.top, 12).fixedSize(horizontal: false, vertical: true)
            }
            .padding(.horizontal, 32).padding(.top, 20).frame(maxWidth: .infinity, alignment: .leading)
            Spacer(minLength: 16)
            Divider().overlay(Color.white.opacity(0.08))
            footer.padding(.horizontal, 32).frame(height: 72)
        }
        .frame(width: 1040, height: 690)
        .background(LauncherPalette.surface)
        .environment(\.colorScheme, .dark)
        .accessibilityIdentifier("feature.popup")
    }

    private var art: some View {
        ZStack(alignment: .top) {
            if let path = page["image"] as? String, let image = NSImage(contentsOfFile: path) {
                Image(nsImage: image).resizable().aspectRatio(contentMode: .fill)
            } else {
                LauncherPalette.background
            }
            HStack {
                Text(text(popup["badge"])).font(.manrope(12, .bold)).foregroundStyle(LauncherPalette.cream)
                    .padding(.horizontal, 10).padding(.vertical, 4)
                    .background(Color.black.opacity(0.45), in: Capsule()).overlay(Capsule().stroke(Color(red: 0.35, green: 0.33, blue: 0.27)))
                Text("BETA").font(.manrope(12, .heavy)).foregroundStyle(.black).padding(.horizontal, 10).padding(.vertical, 4).background(Self.gold2, in: Capsule())
                Spacer()
                Button { model.closeUpdatePopup() } label: {
                    Image(systemName: "xmark").font(.system(size: 13, weight: .bold)).foregroundStyle(.white.opacity(0.85))
                        .frame(width: 32, height: 32).background(Color.black.opacity(0.45), in: Circle())
                }
                .buttonStyle(.plain).help(t("feature.close")).accessibilityLabel(t("feature.close")).accessibilityIdentifier("feature.close")
            }
            .padding(.horizontal, 24).padding(.top, 18)
        }
        .frame(width: 1040, height: 392).clipped()
    }

    private func cell(_ label: String, _ value: Any?) -> some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(t(label)).font(.manrope(12, .bold)).foregroundStyle(Self.gold2)
            Text(text(value)).font(.manrope(13.5)).foregroundStyle(Color(white: 0.86)).lineSpacing(3).fixedSize(horizontal: false, vertical: true)
        }
        .padding(.horizontal, 13).padding(.vertical, 11).frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
        .background(Self.card, in: RoundedRectangle(cornerRadius: 12))
        .overlay(RoundedRectangle(cornerRadius: 12).stroke(Color.white.opacity(0.07)))
    }

    private var footer: some View {
        HStack(spacing: 12) {
            HStack(spacing: 7) {
                ForEach(pages.indices, id: \.self) { i in
                    Capsule().fill(i == index ? LauncherPalette.cream : Color.white.opacity(0.2)).frame(width: i == index ? 24 : 8, height: 8)
                        .onTapGesture { index = i }
                }
            }
            if let link = (popup["url"] as? String).flatMap(URL.init(string:)) {
                Button { NSWorkspace.shared.open(link) } label: { Text(t("feature.more")).font(.manrope(13)).underline() }
                    .buttonStyle(.plain).foregroundStyle(LauncherPalette.cream).padding(.leading, 10).accessibilityIdentifier("feature.more")
            }
            Spacer()
            Button(index == 0 ? t("feature.skip") : t("feature.back")) {
                if index == 0 { model.closeUpdatePopup() } else { index -= 1 }
            }
            .buttonStyle(.plain).font(.manrope(14.5, .semibold)).foregroundStyle(.secondary).padding(.horizontal, 8)
            .keyboardShortcut(.cancelAction).accessibilityIdentifier("feature.back")
            Button { if last { model.closeFeatureAndExport() } else { index += 1 } } label: {
                Text(last ? t("feature.go") : t("feature.next")).font(.manrope(14.5, .bold)).foregroundStyle(.black)
                    .padding(.horizontal, 24).frame(height: 42).background(LauncherPalette.cream, in: Capsule())
            }
            .buttonStyle(.plain).keyboardShortcut(.defaultAction).accessibilityIdentifier("feature.next")
        }
    }
}
