# Moe Live Translator 使用说明 🎙️

基于Whisper实现的本地语音识别+实时翻译工具，可以用于自己直播的同时翻译其他语言，也可以用于观看他人直播并实时翻译。

## 使用步骤

1.  **下载模型** (必做!)
    *   软件运行需要 AI 模型文件（`.bin` 格式）。
    *   **下载地址**: [Hugging Face - whisper.cpp Models](https://huggingface.co/ggerganov/whisper.cpp/tree/main)
    *   推荐下载：`ggml-small.bin` ，如果需要更精准的效果，可以下载更大的模型，相应也会占用更多硬件性能（我个人使用的是ggml-large-v3-turbo.bin）。
    *   **存放位置**: 将下载的文件放在 `WhisperModels` 文件夹内。

2.  **运行软件**
    *   双击 `MoeLiveTranslator.exe` 启动。

3.  **填入翻译 Key** (设置)
    *   在打开的软件窗口右键，选择 `设置`。
    *   在设备选项中，选择你的 **麦克风**（听你说话）或 **立体声混音**（听电脑中播放的声音），更推荐使用VB-Cable类的软件自带的虚拟声卡。

5.  **开始**
    *   回到主界面，双击主界面即可开始监听、翻译。

---

## 小贴士
*   **OBS 直播**: 窗口背景支持透明，OBS 添加“游戏捕获”并允许透明即可作为字幕层。
*   **识别优化**: 可以在 `prompts.json` 里添加你的常用游戏术语或人名，让 AI 语音识别得更准确。
