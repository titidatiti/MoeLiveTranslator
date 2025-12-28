using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Dsp;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using LiveTranslator.Models;
using System.Linq;
using System.Text;

using Whisper.net;
using System.Threading.Tasks;

namespace LiveTranslator.Services
{
    public class AudioService : IDisposable
    {
        private WaveInEvent _waveIn;

        private bool _isListening;
        private int _sampleRate = 48000;

        // Whisper
        private WhisperFactory _whisperFactory;
        private WhisperProcessor _whisperProcessor;


        public event EventHandler<string> OnSpeechRecognized;
        public event EventHandler<string> OnError;
        public event EventHandler<string> OnSystemMessage;
        public event EventHandler<bool> OnListeningStatusChanged;
        public event Action<string> OnLog;
        public event EventHandler<int> OnAudioLevel;

        public bool IsListening => _isListening;

        public AudioService()
        {
        }

        public List<string> GetInputDevices()
        {
            var devices = new List<string>();
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                devices.Add(caps.ProductName);
            }
            return devices;
        }

        public List<string> GetWhisperModels()
        {
            var list = new List<string>();
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // Search root
                var files = new List<string>(Directory.GetFiles(baseDir, "ggml-*.bin"));

                // Search 'WhisperModels' subdirectory
                var modelsDir = Path.Combine(baseDir, "WhisperModels");
                if (Directory.Exists(modelsDir))
                {
                    files.AddRange(Directory.GetFiles(modelsDir, "ggml-*.bin"));
                }

                foreach (var f in files)
                {
                    // If it's in a subdirectory, we might want to store relative path or full path.
                    // Storing just filename might be ambiguous if duplicates exist.
                    // For simplicity in UI, we verify existence later using the same logic or store full path.
                    // Let's store just the filename for display if unique, or handling in Start is needed.
                    // For now, let's assume unique filenames or standard storage.
                    list.Add(Path.GetFileName(f));
                }
            }
            catch { }

            if (list.Count == 0) list.Add("ggml-base.bin");
            return list;
        }

        public void Start(int deviceIndex, string languageCode, int noiseThresholdPercent, string whisperModelName)
        {
            if (_isListening) Stop();

            // Reload prompt from config
            string prompt = LoadPrompt(languageCode);

            Console.WriteLine($"[AudioService] Request Start. Model: {whisperModelName}");
            OnLog?.Invoke($"Initializing Whisper Engine (Prompt Len: {prompt.Length})...");

            // Offload capture initialization and processing to a background thread
            // to avoid blocking the UI thread or being blocked by it.
            Task.Run(() =>
            {
                try
                {
                    OnLog?.Invoke("Checking available models...");

                    // 1. Whisper Init


                    // 2. Detect Native Sample Rate
                    // WaveIn names are truncated (31 chars). We match with MMDevice to get the real MixFormat.
                    _sampleRate = 48000; // Default fallback
                    try
                    {
                        var waveInCaps = WaveIn.GetCapabilities(deviceIndex);
                        string waveInName = waveInCaps.ProductName;

                        using (var enumerator = new MMDeviceEnumerator())
                        {
                            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                            {
                                // Match Logic: waveInName is only 31 chars.
                                string friendlyName = device.FriendlyName;
                                if (friendlyName.Length > 31) friendlyName = friendlyName.Substring(0, 31);

                                // Simple prefix/substring match
                                if (waveInName.StartsWith(friendlyName) || friendlyName.StartsWith(waveInName))
                                {
                                    _sampleRate = device.AudioClient.MixFormat.SampleRate;
                                    Console.WriteLine($"[AudioService] Detected Native Rate: {_sampleRate}Hz for {device.FriendlyName}");
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AudioService] Rate Detection Failed: {ex.Message}. Keeping {_sampleRate}Hz.");
                    }

                    // 2. Setup Recognizer (Whisper)
                    // Load Whisper (User Selected)
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string targetModel = string.IsNullOrWhiteSpace(whisperModelName) ? "ggml-base.bin" : whisperModelName;

                    // Search in 'WhisperModels' first, then root
                    string modelsDir = Path.Combine(baseDir, "WhisperModels");
                    string whisperPath = Path.Combine(modelsDir, targetModel);

                    if (!File.Exists(whisperPath))
                    {
                        // Fallback to root
                        whisperPath = Path.Combine(baseDir, targetModel);
                    }

                    if (!File.Exists(whisperPath))
                    {
                        OnError?.Invoke(this, $"Whisper model '{targetModel}' not found. Please download from huggingface.co/ggerganov/whisper.cpp and place in app folder.");
                        return;
                    }
                    OnLog?.Invoke($"Loading Whisper Model ({targetModel})...");
                    _whisperFactory = WhisperFactory.FromPath(whisperPath);

                    string lang = languageCode.Contains("-") ? languageCode.Split('-')[0] : languageCode;

                    var builder = _whisperFactory.CreateBuilder()
                        .WithLanguage(lang);

                    if (!string.IsNullOrWhiteSpace(prompt))
                    {
                        Console.WriteLine($"[Whisper] Using Prompt: {prompt.Replace("\n", " ")}");
                        builder.WithPrompt(prompt);

                        // Notify UI for verification
                        string promptPreview = prompt.Length > 50 ? prompt.Substring(0, 50) + "..." : prompt;
                        OnSystemMessage?.Invoke(this, $"Loaded Prompt ({prompt.Length} chars): \"{promptPreview}\"");
                    }
                    else
                    {
                        OnSystemMessage?.Invoke(this, "No custom prompt loaded (using default).");
                    }

                    _whisperProcessor = builder.Build();

                    _whisperSegments = new System.Collections.Concurrent.BlockingCollection<byte[]>();

                    Task.Run(ProcessWhisperLoop);

                    // 3. Setup Audio Capture
                    if (deviceIndex < 0 || deviceIndex >= WaveIn.DeviceCount)
                    {
                        OnError?.Invoke(this, $"Invalid Audio Device Index: {deviceIndex}");
                        return;
                    }

                    _isListening = true;
                    OnListeningStatusChanged?.Invoke(this, true);

                    _waveIn = new WaveInEvent();
                    _waveIn.DeviceNumber = deviceIndex;
                    _waveIn.WaveFormat = new WaveFormat(_sampleRate, 16, 1);
                    _waveIn.BufferMilliseconds = 100;

                    // DSP Filters (Stateful)
                    var highPass = BiQuadFilter.HighPassFilter(_sampleRate, 100, 1);
                    var lowPass = BiQuadFilter.LowPassFilter(_sampleRate, 8000, 1);

                    // Noise Gate Threshold (User Configurable)
                    // 0-100 => 0.0 - 1.0
                    float noiseGateThreshold = noiseThresholdPercent / 100.0f;

                    _waveIn.DataAvailable += (s, a) =>
                    {
                        byte[] buf = new byte[a.BytesRecorded];
                        Buffer.BlockCopy(a.Buffer, 0, buf, 0, a.BytesRecorded);
                        _audioQueue.Add(buf);
                    };

                    _waveIn.StartRecording();

                    // CONSUMER Loop (runs on the Task thread)
                    ProcessAudioQueue(noiseGateThreshold, highPass, lowPass);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, $"Start Error: {ex.Message}");
                    Stop();
                }
            });
        }

        private string LoadPrompt(string languageCode)
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prompts.json");
                if (!File.Exists(path))
                {
                    Console.WriteLine($"[AudioService] Warning: prompts.json not found at {path}");
                    return "";
                }

                var json = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<Dictionary<string, LanguageConfig>>(json);
                if (config == null) return "";

                LanguageConfig langConfig = null;
                // 1. Exact Match
                if (config.ContainsKey(languageCode))
                {
                    langConfig = config[languageCode];
                }
                // 2. Prefix Match (e.g. ja-JP -> ja)
                else
                {
                    var prefix = languageCode.Split('-')[0];
                    langConfig = config.FirstOrDefault(x => x.Key.StartsWith(prefix)).Value;
                }

                if (langConfig == null || langConfig.Scenes == null) return "";

                var sb = new StringBuilder();
                foreach (var scene in langConfig.Scenes.Values)
                {
                    if (scene.System != null) sb.AppendLine(string.Join("\n", scene.System));
                    if (scene.User != null) sb.AppendLine(string.Join("\n", scene.User));
                    if (scene.Hotwords != null && scene.Hotwords.Items != null)
                        sb.AppendLine(string.Join(", ", scene.Hotwords.Items));
                }
                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AudioService] LoadPrompt Error: {ex.Message}");
                return "";
            }
        }

        private System.Collections.Concurrent.BlockingCollection<byte[]> _audioQueue = new System.Collections.Concurrent.BlockingCollection<byte[]>();
        private System.Collections.Concurrent.BlockingCollection<byte[]> _whisperSegments; // Replaces Stream/FloatQueue
        private MemoryStream _currentWhisperBuffer = new MemoryStream();
        private MemoryStream _currentSentenceAudio = new MemoryStream();

        private void ProcessAudioQueue(float noiseGateThreshold, BiQuadFilter highPass, BiQuadFilter lowPass)
        {
            Console.WriteLine("[AudioService] Consumer Loop Started.");
            bool _lastFrameWasSilence = false;
            int _whisperHoldFrames = 0;
            const int HOLDOVER_FRAMES = 5; // ~500ms hangover
            bool _hasLoggedFlow = false;

            foreach (var buffer in _audioQueue.GetConsumingEnumerable())
            {
                if (!_isListening) break;
                if (!_hasLoggedFlow)
                {
                    Console.WriteLine($"[AudioService] First Audio Packet Processed. Size: {buffer.Length}");
                    _hasLoggedFlow = true;
                }
                int bytesRecorded = buffer.Length;
                float maxSampleVal = 0;

                // 1. Process Loop (Consumer)
                for (int i = 0; i < bytesRecorded; i += 2)
                {
                    short sampleShort = (short)((buffer[i + 1] << 8) | buffer[i]);
                    float sample = sampleShort / 32768f;

                    // Apply Gain (1.0x - No Boost to prevent clipping on System Audio)
                    // sample *= 1.0f;

                    sample = highPass.Transform(sample);
                    sample = lowPass.Transform(sample);

                    float abs = Math.Abs(sample);
                    if (abs > maxSampleVal) maxSampleVal = abs;

                    if (sample > 1.0f) sample = 1.0f;
                    if (sample < -1.0f) sample = -1.0f;
                    short processedShort = (short)(sample * 32767);
                    buffer[i] = (byte)(processedShort & 0xFF);
                    buffer[i + 1] = (byte)((processedShort >> 8) & 0xFF);
                }

                bool isSilence = false;
                if (maxSampleVal < noiseGateThreshold)
                {
                    // Array.Clear(buffer, 0, bytesRecorded);
                    maxSampleVal = 0;
                    isSilence = true;
                }

                _currentSentenceAudio.Write(buffer, 0, bytesRecorded);

                bool isSpeechDetected = !isSilence;
                if (isSpeechDetected) _whisperHoldFrames = HOLDOVER_FRAMES;
                else if (_whisperHoldFrames > 0) _whisperHoldFrames--;

                bool effectiveSpeech = isSpeechDetected || (_whisperHoldFrames > 0);

                if (effectiveSpeech)
                {
                    _lastFrameWasSilence = false;
                    // Resample to 16kHz (Averaging / Low-Pass)
                    // Ratio approx 3 for 48k. 
                    int step = (int)Math.Round(_sampleRate / 16000.0);
                    if (step < 1) step = 1;

                    var outBuffer = new byte[bytesRecorded];
                    int outIdx = 0;

                    // Iterate blocks of `step` samples
                    for (int i = 0; i < bytesRecorded; i += 2 * step)
                    {
                        // Average `step` samples
                        long sum = 0;
                        int count = 0;
                        for (int k = 0; k < step; k++)
                        {
                            int idx = i + k * 2;
                            if (idx + 1 < bytesRecorded)
                            {
                                short val = (short)((buffer[idx + 1] << 8) | buffer[idx]);
                                sum += val;
                                count++;
                            }
                        }

                        if (count > 0)
                        {
                            short avg = (short)(sum / count);
                            // Output Bytes (Debug Wav)
                            outBuffer[outIdx++] = (byte)(avg & 0xFF);
                            outBuffer[outIdx++] = (byte)((avg >> 8) & 0xFF);
                        }
                    }

                    // Accumulate for Batch
                    if (outIdx > 0)
                    {
                        _currentWhisperBuffer.Write(outBuffer, 0, outIdx);

                        // Safety Flush: If buffer > 8 seconds (~256KB), force flush to prevent lag
                        if (_currentWhisperBuffer.Length > 256000)
                        {
                            Console.WriteLine("[Whisper] Force Flushing Long Segment...");
                            _whisperSegments.Add(_currentWhisperBuffer.ToArray());
                            _currentWhisperBuffer.SetLength(0);
                        }
                    }
                }
                else
                {
                    // Speech Ended
                    if (!_lastFrameWasSilence)
                    {
                        // Commit Segment if it has content
                        if (_currentWhisperBuffer.Length > 0)
                        {
                            _whisperSegments.Add(_currentWhisperBuffer.ToArray());
                            _currentWhisperBuffer.SetLength(0);
                        }

                        _lastFrameWasSilence = true;
                    }
                }

                // Flush Debug Writer periodically (every 5s)
                if (DateTime.Now.Second % 5 == 0)
                {
                    // No-op for now
                }

                int level = (int)(maxSampleVal * 100);
                OnAudioLevel?.Invoke(this, level);
            }
        }

        private async Task ProcessWhisperLoop()
        {
            Console.WriteLine("[Whisper] Batch Loop Started.");
            try
            {
                foreach (var segmentBytes in _whisperSegments.GetConsumingEnumerable())
                {
                    Console.WriteLine($"[Whisper] Processing Segment ({segmentBytes.Length} bytes)...");
                    using (var ms = new MemoryStream())
                    {
                        WriteWavHeader(ms, 16000, 1, segmentBytes.Length);
                        ms.Write(segmentBytes, 0, segmentBytes.Length);
                        ms.Position = 0;

                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        await foreach (var res in _whisperProcessor.ProcessAsync(ms))
                        {
                            string text = res.Text?.Trim();
                            if (string.IsNullOrWhiteSpace(text)) continue;
                            if (text.StartsWith("[Music]") || text.StartsWith("(Music)")) continue;

                            Console.WriteLine($"[Whisper] Recognized: {text}");
                            OnSpeechRecognized?.Invoke(this, text);
                        }
                        sw.Stop();
                        Console.WriteLine($"[Whisper] Segment Processed in {sw.ElapsedMilliseconds}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Whisper] Loop Exit: {ex.Message}");
            }
        }

        private void WriteWavHeader(Stream stream, int sampleRate, int channels, int dataSize = 0)
        {
            int extra = 36;
            int fileSize = (dataSize > 0) ? dataSize + extra : int.MaxValue;
            int chunkLimit = (dataSize > 0) ? dataSize : int.MaxValue;

            var encoding = System.Text.Encoding.UTF8;
            stream.Write(encoding.GetBytes("RIFF"), 0, 4);
            stream.Write(BitConverter.GetBytes(fileSize), 0, 4);
            stream.Write(encoding.GetBytes("WAVE"), 0, 4);
            stream.Write(encoding.GetBytes("fmt "), 0, 4);
            stream.Write(BitConverter.GetBytes(16), 0, 4); // SubChunk1Size
            stream.Write(BitConverter.GetBytes((short)1), 0, 2); // PCM
            stream.Write(BitConverter.GetBytes((short)channels), 0, 2);
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);
            stream.Write(BitConverter.GetBytes(sampleRate * channels * 2), 0, 4); // ByteRate
            stream.Write(BitConverter.GetBytes((short)(channels * 2)), 0, 2); // BlockAlign
            stream.Write(BitConverter.GetBytes((short)16), 0, 2); // BitsPerSample
            stream.Write(encoding.GetBytes("data"), 0, 4);
            stream.Write(BitConverter.GetBytes(chunkLimit), 0, 4);
        }

        public void Stop()
        {
            try
            {
                if (_waveIn != null)
                {
                    _waveIn.StopRecording();
                    _waveIn.Dispose();
                    _waveIn = null;
                }

                // Don't dispose _model, keep it loaded for restart
            }
            catch { }
            finally
            {
                _isListening = false;
                OnListeningStatusChanged?.Invoke(this, false);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
