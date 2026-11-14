# native-test-cpatch_levels：帧率补丁按档（cpatch.c fps_pick），与 t_cpatch 同样链 cpatch.c；runtime/build.mk 按 runtime/tests/mk/*.mk 收进来
NATIVE_TEST_OWN += cpatch_levels
.PHONY: native-test-cpatch_levels
native-test-cpatch_levels:
	@mkdir -p $(BUILD)/tests
	$(CC) $(CFLAGS) -Iruntime/include -DS2A_UNIT_TEST -o $(BUILD)/tests/t_cpatch_levels runtime/tests/t_cpatch_levels.c runtime/src/cpatch.c  -Wl,-U,_S2A_Init -Wl,-U,_S2A_InitSession -Wl,-U,_S2A_LdnSetServer -Wl,-U,_mtl_tab_selftest -Wl,-U,_nvn_pso_harvest -Wl,-U,_nxgal_gpu_hold -Wl,-U,_s2a_autoplay_on -Wl,-U,_s2a_autoplay_wait_s -Wl,-U,_s2a_codec_entry -Wl,-U,_s2a_direct_device_ok -Wl,-U,_s2a_gl_entry -Wl,-U,_s2a_gl_lib_ready -Wl,-U,_s2a_gl_stub -Wl,-U,_s2a_gl_xlate -Wl,-U,_s2a_gl_xlate_tag -Wl,-U,_s2a_ios_split_active -Wl,-U,_s2a_nv_entry -Wl,-U,_s2a_nvn_bootstrap_entry -Wl,-U,_s2a_vi_entry -Wl,-U,_s2a_vk_entry -Wl,-U,_s2a_vk_lib_ready -Wl,-U,_s2a_vk_xlate -Wl,-U,_s2a_vk_xlate_vin   
	$(BUILD)/tests/t_cpatch_levels
TEST_TARGETS += native-test-cpatch_levels
