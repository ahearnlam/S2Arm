# native-test-audio_buf：11-14 设备缓冲的 Mac 壳侧口径（macos/audio_buffer.h）功能 + front_mac.m 接线断言（runtime/build.mk 末尾按 runtime/tests/mk/*.mk 收进来）
NATIVE_TEST_OWN += audio_buf
.PHONY: native-test-audio_buf
native-test-audio_buf:
	@mkdir -p $(BUILD)/tests
	$(CC) $(CFLAGS) -Imacos -o $(BUILD)/tests/t_mac_audio_buf runtime/tests/t_mac_audio_buf.c
	$(BUILD)/tests/t_mac_audio_buf macos/front_mac.m
TEST_TARGETS += native-test-audio_buf
