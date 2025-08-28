namespace Ffmpeg.zxx
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    public class FFmpegWrapper
    {
        private readonly string _ffmpegPath;

        // FFmpeg进程执行的事件委托
        public delegate void OnProgressHandler(string inputPath, double percentage);
        public event OnProgressHandler OnProgress;

        // 构造函数
        public FFmpegWrapper(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
            if (!File.Exists(_ffmpegPath))
            {
                throw new FileNotFoundException("未找到FFmpeg可执行文件!", _ffmpegPath);
            }
        }

        /// <summary>
        /// 获取视频信息
        /// </summary>
        public async Task<VideoInfo> GetVideoInfoAsync(string inputPath)
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException("未找到输入视频文件!", inputPath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-i \"{inputPath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // 解析视频信息
             return ParseVideoInfo(output);
        }

        /// <summary>
        /// 转换视频格式
        /// </summary>
        public async Task ConvertVideoAsync(string inputPath, string outputPath, VideoConversionOptions options)
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException("未找到输入视频文件!", inputPath);
            }

            double _TotalSeconds = 0;
            var videoInfo = await GetVideoInfoAsync(inputPath);
            if (videoInfo != null)
            {
                _TotalSeconds = videoInfo.Duration.TotalSeconds;
            }

            // 构建FFmpeg命令
            string arguments = BuildFFmpegArguments(inputPath, outputPath, options);

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            // 添加输出数据接收处理
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    UpdateProgress(e.Data, inputPath, _TotalSeconds);
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg进程已退出，返回代码 {process.ExitCode}");
            }
        }

        /// <summary>
        /// 构建FFmpeg命令参数
        /// </summary>
        private string BuildFFmpegArguments(string inputPath, string outputPath, VideoConversionOptions options)
        {
            var args = new List<string>
        {
            "-i", $"\"{inputPath}\"",
            "-y" // 覆盖输出文件
        };

            // 添加视频编码器
            if (!string.IsNullOrEmpty(options.VideoCodec))
            {
                args.Add("-c:v");
                args.Add(options.VideoCodec);
            }

            // 添加音频编码器
            if (!string.IsNullOrEmpty(options.AudioCodec))
            {
                args.Add("-c:a");
                args.Add(options.AudioCodec);
            }

            // 设置视频比特率
            if (options.VideoBitrate > 0)
            {
                args.Add("-b:v");
                args.Add($"{options.VideoBitrate}k");
            }

            // 设置音频比特率
            if (options.AudioBitrate > 0)
            {
                args.Add("-b:a");
                args.Add($"{options.AudioBitrate}k");
            }

            // 设置分辨率
            if (!string.IsNullOrEmpty(options.Resolution))
            {
                args.Add("-s");
                args.Add(options.Resolution);
            }

            // 添加输出文件路径
            args.Add($"\"{outputPath}\"");

            return string.Join(" ", args);
        }

        /// <summary>
        /// 更新转换进度
        /// </summary>
        private void UpdateProgress(string data, string inputPath, double _TotalSeconds)
        {
            // 解析FFmpeg输出，计算进度
            if (data.Contains("time="))
            {
                try
                {
                    var timeIndex = data.IndexOf("time=");
                    var timeStr = data.Substring(timeIndex + 5, 11);
                    var time = TimeSpan.Parse(timeStr);

                    // 假设我们已经知道总时长，这里简化处理
                    double percentage = (time.TotalSeconds / _TotalSeconds) * 100;
                    OnProgress?.Invoke(inputPath, percentage);
                }
                catch
                {
                    // 解析失败时忽略
                }
            }
        }

        /// <summary>
        /// 解析视频信息
        /// </summary>
        private VideoInfo ParseVideoInfo(string ffmpegOutput)
        {
            var videoInfo = new VideoInfo();

            // 解析时长
            var durationMatch = Regex.Match(ffmpegOutput, @"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            if (durationMatch.Success)
            {
                videoInfo.Duration = TimeSpan.Parse($"{durationMatch.Groups[1]}:{durationMatch.Groups[2]}:{durationMatch.Groups[3]}.{durationMatch.Groups[4]}");
            }

            // 解析视频编码器
            var videoCodecMatch = Regex.Match(ffmpegOutput, @"Video: (\w+)");
            if (videoCodecMatch.Success)
            {
                videoInfo.VideoCodec = videoCodecMatch.Groups[1].Value;
            }
            // 解析音频编码器
            var audioCodecMatch = Regex.Match(ffmpegOutput, @"Audio: (\w+)");
            if (audioCodecMatch.Success)
            {
                videoInfo.AudioCodec = audioCodecMatch.Groups[1].Value;
            }
            // 解析视频比特率
            var videoBitrateMatch = Regex.Match(ffmpegOutput, @"bitrate: (\d+) kb/s");
            if (videoBitrateMatch.Success)
            {
                videoInfo.VideoBitrate = int.Parse(videoBitrateMatch.Groups[1].Value);
            }
            // 解析音频比特率
            var audioBitrateMatch = Regex.Match(ffmpegOutput, @"(\d+) kb/s");
            if (audioBitrateMatch.Success)
            {
                videoInfo.AudioBitrate = int.Parse(audioBitrateMatch.Groups[1].Value);
            }
            // 解析分辨率
            var resolutionMatch = Regex.Match(ffmpegOutput, @"(\d{2,4})x(\d{2,4})");
            if (resolutionMatch.Success)
            {
                videoInfo.Resolution = resolutionMatch.Value;
                videoInfo.Width = int.Parse(resolutionMatch.Groups[1].Value);
                videoInfo.Height = int.Parse(resolutionMatch.Groups[2].Value);
            }

            return videoInfo;
        }

        /// <summary>
        /// 通用音频转换方法
        /// </summary>
        /// <param name="inputPath">输入音频文件路径</param>
        /// <param name="outputPath">输出音频文件路径</param>
        /// <param name="options">音频转换选项</param>
        public async Task ConvertAudioAsync(string inputPath, string outputPath, AudioConversionOptions options)
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException("未找到输入音频文件!", inputPath);
            }
            // 获取音频总时长
            double _TotalSeconds = 0;
            var audioInfo = await GetAudioInfoAsync(inputPath);
            if (audioInfo != null)
            {
                _TotalSeconds = audioInfo.Duration.TotalSeconds;
            }
            // 构建FFmpeg命令
            string arguments = BuildFFmpegAudioArguments(inputPath, outputPath, options);
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = startInfo };
            // 添加输出数据接收处理
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    UpdateProgress(e.Data, inputPath, _TotalSeconds);
                }
            };
            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg进程已退出，返回代码 {process.ExitCode}");
            }
        }
        /// <summary>
        /// 构建音频转换的FFmpeg命令参数
        /// -i input.mp3：指定输入音频文件。
        /// -c:a codec：指定音频编码器（如 aac=AAC格式、mp3=MP3格式、flac=FLAC格式、pcm_s16le=PCM格式等）
        /// -b:a bitrate：设置音频比特率（如 128kbps）
        /// -ar sampleRate：设置采样率（如 44100=44.1kHz）
        /// -ac channels：设置声道数（如 2）
        /// </summary>
        private string BuildFFmpegAudioArguments(string inputPath, string outputPath, AudioConversionOptions options)
        {
            var args = new List<string>
            {
                "-i", $"\"{inputPath}\"",
                "-y" // 覆盖输出文件
            };
            // 添加音频编码器
            if (!string.IsNullOrEmpty(options.AudioCodec))
            {
                args.Add("-c:a");
                args.Add(options.AudioCodec);
            }
            // 设置音频比特率
            if (options.AudioBitrate > 0)
            {
                args.Add("-b:a");
                args.Add($"{options.AudioBitrate}k");
            }
            // 设置采样率
            if (options.SampleRate > 0)
            {
                args.Add("-ar");
                args.Add(options.SampleRate.ToString());
            }
            // 设置声道数
            if (options.Channel > 0)
            {
                args.Add("-ac");
                args.Add($"{(int)options.Channel}");
            }
            if (outputPath.ToLower().Contains(".pcm"))
            {
                args.Add("-f s16le");
            }
            // 添加输出文件路径
            args.Add($"\"{outputPath}\"");
            return string.Join(" ", args);
        }

        /// <summary>
        /// 获取音频信息
        /// </summary>
        public async Task<AudioInfo> GetAudioInfoAsync(string inputPath)
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException("未找到输入视频文件!", inputPath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-i \"{inputPath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // 解析视频信息
            return ParseAudioInfo(output);
        }

        /// <summary>
        /// 解析视频信息
        /// </summary>
        private AudioInfo ParseAudioInfo(string ffmpegOutput)
        {
            var audioInfo = new AudioInfo();

            // 解析时长
            var durationMatch = Regex.Match(ffmpegOutput, @"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            if (durationMatch.Success)
            {
                audioInfo.Duration = TimeSpan.Parse($"{durationMatch.Groups[1]}:{durationMatch.Groups[2]}:{durationMatch.Groups[3]}.{durationMatch.Groups[4]}");
            }

            // 正则表达式匹配音频信息
            string pattern = @"Audio: (\w+) \(\w+\), (\d+) Hz, (\w+), \w+, (\d+) kb/s";
            Match match = Regex.Match(ffmpegOutput, pattern);
            if (!match.Success)
            {
                throw new ArgumentException("无法解析 FFmpeg 输出字符串");
            }
            // 提取匹配的字段
            audioInfo.AudioCodec = match.Groups[1].Value;
            audioInfo.SampleRate = int.Parse(match.Groups[2].Value);
            string channel = match.Groups[3].Value;
            audioInfo.AudioBitrate = int.Parse(match.Groups[4].Value);
            // 解析声道数
            AudioChannel audioChannel = channel.ToLower() switch
            {
                "mono" => AudioChannel.Mono,
                "stereo" => AudioChannel.Stereo,
                _ => throw new ArgumentException("未知的声道数")
            };
            audioInfo.Channel = audioChannel;
            return audioInfo;
        }

    }

    /// <summary>
    /// 视频转换选项类
    /// </summary>
    public class VideoConversionOptions
    {
        /// <summary>
        /// 视频编码器
        /// </summary>
        public string VideoCodec { get; set; }
        /// <summary>
        /// 音频编码器
        /// </summary>
        public string AudioCodec { get; set; }
        /// <summary>
        /// 视频比特率
        /// </summary>
        public int VideoBitrate { get; set; }
        /// <summary>
        /// 音频比特率
        /// </summary>
        public int AudioBitrate { get; set; }
        /// <summary>
        /// 分辨率 
        /// </summary>
        public string Resolution { get; set; }
    }

    /// <summary>
    /// 视频信息类
    /// </summary>
    public class VideoInfo: VideoConversionOptions
    {
        /// <summary>
        /// 视频时长
        /// </summary>
        public TimeSpan Duration { get; set; }
        /// <summary>
        /// 分辨率宽度
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// 分辨率高度
        /// </summary>
        public int Height { get; set; }
    }

    /// <summary>
    /// 音频转换选项类
    /// </summary>
    public class AudioConversionOptions
    {
        /// <summary>
        /// 音频编码器
        /// </summary>
        public string AudioCodec { get; set; }
        /// <summary>
        /// 音频比特率
        /// </summary>
        public int AudioBitrate { get; set; }
        /// <summary>
        /// 采样率[44100Hz=CD质量、48000Hz=DVD质量]
        /// </summary>
        public int SampleRate { get; set; }
        /// <summary>
        /// 声道数
        /// </summary>
        public AudioChannel Channel { get; set; }
    }
    public enum AudioChannel
    {
        [Description("单声道")]
        Mono = 1, // 单声道
        [Description("立体声")]
        Stereo = 2, // 立体声
    }

    /// <summary>
    /// 音频信息类
    /// </summary>
    public class AudioInfo : AudioConversionOptions
    {
        /// <summary>
        /// 视频时长
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}
