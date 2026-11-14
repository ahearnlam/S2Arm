// 音频设备缓冲帧数的 Mac 壳侧口径（11-14 设备缓冲）：S2A_AUDIO_BUFFER_FRAMES = 16 的倍数（16–512），
//   没设 / 不合法 = 0 = 跟随系统默认（Mac 内置扬声器 512 帧 ≈ 10.7 ms @48k，这是出声延迟的地板）。
//   C# 音频后端建队时对输出设备请求这个值（AppleHardwareDeviceSession.S2AApplyDeviceBufferFrames，
//   那侧的解析在 AppleDeviceBufferFrames.FromEnvironment，两处口径一致）。
//   Mac 壳把玩家在「声音」菜单选的值落 NSUserDefaults（S2AAudioBufferFrames），构造期转回环境变量；
//   显式环境变量优先（setenv 第三参 0）：开发 / 单测给的不覆盖。
// 纯 C、头文件即实现，单测 runtime/tests/t_mac_audio_buf.c（功能）+ front_mac.m 接线断言
#pragma once
#include <stdlib.h>

static inline int s2a_buffer_frames_valid(int frames) { return frames >= 16 && frames <= 512 && frames % 16 == 0; }

// 0 = 跟随系统（没设 / 不合法）；合法值 = 请求的设备缓冲帧数
static inline int s2a_buffer_frames_env(const char *value) {
    if (!value || !*value) return 0;
    int v = atoi(value);
    return s2a_buffer_frames_valid(v) ? v : 0;
}

// 菜单勾选用：没设 / 不合法 = 默认档 512（跟随系统；C# 侧对没设值不请求，设备回落自己的默认——
//   Mac 内置扬声器 512 帧 ≈ 10.7 ms，「512（默认）」勾给它看）
static inline int s2a_buffer_frames_effective(const char *value) {
    int v = s2a_buffer_frames_env(value);
    return v > 0 ? v : 512;
}
