// 音频设备缓冲（11-14 设备缓冲）：① macos/audio_buffer.h 的合法域与解析功能（16 的倍数、16–512；没设 / 不合法 = 0 = 跟随系统；
//   effective 口径没设 = 默认档 512）；② front_mac.m 的接线断言：构造期 defaults → setenv（显式环境优先）、
//   「声音」菜单 6 档下拉（512 默认 / 256 / 128 / 64 / 32 / 16）与勾选现算、动作落 defaults + setenv。
//   真正对设备请求 kAudioDevicePropertyBufferFrameSize 的解析在 C# 侧（AppleDeviceBufferFrames，服务层单测 --apple-audio-buffer-frames）。
// 用法：t_mac_audio_buf <front_mac.m 路径>
#include "audio_buffer.h"
#include <stdio.h>
#include <string.h>

static int checks, failures;
static void check(int ok, const char *what) { checks++; if (!ok) { failures++; fprintf(stderr, "✗ %s\n", what); } }
static int count_in(const char *text, const char *needle) { int n = 0; for (const char *p = text; (p = strstr(p, needle)); p += strlen(needle)) n++; return n; }
static int after(const char *text, const char *key, const char *needle) { const char *p = strstr(text, key); return p && strstr(p, needle); }

int main(int argc, char **argv) {
    if (argc != 2) { fprintf(stderr, "用法：t_mac_audio_buf <front_mac.m 路径>\n"); return 2; }
    FILE *file = fopen(argv[1], "rb");
    if (!file) { perror(argv[1]); return 2; }
    static char source[1 << 20];
    size_t size = fread(source, 1, sizeof source - 1, file);
    fclose(file);
    source[size] = 0;

    // ---- 合法域 ----
    check(s2a_buffer_frames_valid(16) && s2a_buffer_frames_valid(512), "下限 16 / 上限 512 都合法");
    check(s2a_buffer_frames_valid(256) && s2a_buffer_frames_valid(48) && s2a_buffer_frames_valid(496), "中间的 16 倍数合法（48 / 256 / 496）");
    check(!s2a_buffer_frames_valid(0) && !s2a_buffer_frames_valid(15) && !s2a_buffer_frames_valid(513), "0 / 15 / 513 不合法");
    check(!s2a_buffer_frames_valid(17) && !s2a_buffer_frames_valid(100) && !s2a_buffer_frames_valid(-16), "非 16 倍数 / 负数不合法");
    int all = 1; for (int f = 16; f <= 512; f += 16) all = all && s2a_buffer_frames_valid(f);
    check(all, "16…512 步进 16 共 31 档全部合法（菜单列的就是这些）");

    // ---- 解析 ----
    check(s2a_buffer_frames_env(NULL) == 0 && s2a_buffer_frames_env("") == 0, "没设 / 空 = 0（跟随系统）");
    check(s2a_buffer_frames_env("16") == 16 && s2a_buffer_frames_env("512") == 512 && s2a_buffer_frames_env("32") == 32, "合法值原样通过");
    check(s2a_buffer_frames_env("17") == 0 && s2a_buffer_frames_env("100") == 0 && s2a_buffer_frames_env("544") == 0, "不合法（非倍数 / 超界）= 0");
    check(s2a_buffer_frames_env("abc") == 0 && s2a_buffer_frames_env("-16") == 0 && s2a_buffer_frames_env("0") == 0, "垃圾 / 负数 / 0 = 0");
    check(s2a_buffer_frames_effective(NULL) == 512 && s2a_buffer_frames_effective("abc") == 512, "没设 / 不合法 = 默认档 512（菜单勾给它）");
    check(s2a_buffer_frames_effective("16") == 16 && s2a_buffer_frames_effective("256") == 256, "设了 = 原值（菜单勾选照环境变量）");

    // ---- front_mac.m 接线 ----
    check(after(source, "__attribute__((constructor)) static void s2a_mac_audio_buffer_env", "integerForKey:S2AMacAudioBufferFramesKey"),
          "构造期读 defaults（S2AAudioBufferFrames）");
    check(after(source, "__attribute__((constructor)) static void s2a_mac_audio_buffer_env", "setenv(\"S2A_AUDIO_BUFFER_FRAMES\", b, 0)"),
          "构造期转环境变量：显式环境优先（第三参 0，同 envfile.c / settings.m 口径）");
    check(after(source, "if (item.action == @selector(setAudioBufferFrames:))", "s2a_buffer_frames_effective(getenv(\"S2A_AUDIO_BUFFER_FRAMES\"))"),
          "菜单打开时勾选照环境变量现算（没设 = 默认档 512）");
    check(after(source, "- (void)setAudioBufferFrames:(NSMenuItem *)sender", "setenv(\"S2A_AUDIO_BUFFER_FRAMES\", b, 1)"),
          "菜单动作：选定值写进本进程环境（覆盖旧值）");
    check(after(source, "- (void)setAudioBufferFrames:(NSMenuItem *)sender", "v > 0 && v < 512"),
          "512（默认）档 = 清键 + 清环境（回到上游行为：不请求）；只有 16–256 落值");
    check(after(source, "- (void)setAudioBufferFrames:(NSMenuItem *)sender", "unsetenv(\"S2A_AUDIO_BUFFER_FRAMES\")") &&
          after(source, "- (void)setAudioBufferFrames:(NSMenuItem *)sender", "removeObjectForKey:S2AMacAudioBufferFramesKey"),
          "菜单动作：默认档分支清 defaults + 清环境");
    check(after(source, "for (const int *f = (const int[]){512, 256, 128, 64, 32, 16}, *end = f + 6; f < end; f++)", "it.target = delegate; it.tag = *f;"),
          "下拉 6 档：512（默认）/ 256 / 128 / 64 / 32 / 16（tag = 帧数，降序）");
    check(count_in(source, "@\"512（默认）\"") == 1, "512 档标「（默认）」（没设时勾的就是它）");
    check(count_in(source, "@selector(setAudioBufferFrames:)") == 2, "勾选一处 + 6 档共用循环里那一处 selector");
    check(after(source, "initWithTitle:s2a_ui_zh() ? @\"声音\" : @\"Sound\"", "设备缓冲帧数"), "「声音」菜单带提示行（小 = 延迟低、CPU 高；下次启动生效）");

    if (failures) { fprintf(stderr, "t_mac_audio_buf：%d/%d 项不符\n", failures, checks); return 1; }
    printf("✓ t_mac_audio_buf：%d 项（合法域 16 倍数 16–512 / 解析含显式环境优先与默认档 512 / front_mac.m 构造期与「声音」菜单 6 档接线）\n", checks);
    return 0;
}
