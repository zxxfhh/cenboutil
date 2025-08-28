
using Ffmpeg.zxx;

Console.Title = "FFmpeg测试";
FFmpegWrapper ffmpeg = null;

FFmpegInit();
if (ffmpeg != null)
{
    string videofilepath = Path.Combine(AppContext.BaseDirectory, "files", "20250226135039.mpeg");
    if (File.Exists(videofilepath))
    {
        // 设置转换选项
        var options = new VideoConversionOptions
        {
            VideoCodec = "libx264", // H.264编码
            AudioCodec = "aac",     // AAC音频编码
            Resolution = "1280x720" // 720p分辨率
        };
        // 执行转换
        ffmpeg.ConvertVideoAsync(videofilepath, videofilepath.Replace(".mpeg", ".mp4"), options).Wait();
    }
    string audiofilepath = Path.Combine(AppContext.BaseDirectory, "files", "glyx.mp3");
    if (File.Exists(audiofilepath))
    {
        // 配置音频转换选项
        var options = new AudioConversionOptions
        {
            AudioCodec = "pcm_s16le", // 输出为 PCM 格式
            AudioBitrate = 48,       // 128 kbps
            SampleRate = 44100,       // 44.1 kHz
            Channel = AudioChannel.Mono
        };
        // 执行转换
        ffmpeg.ConvertAudioAsync(audiofilepath, audiofilepath.Replace(".mp3", ".pcm"), options).Wait();
    }
}
Console.ReadKey();

void FFmpegInit()
{
    try
    {
        string ffmpegexe = "ffmpeg";
        // 获取当前操作系统信息
        OperatingSystem os = Environment.OSVersion;
        if (os.Platform == PlatformID.Win32NT) ffmpegexe = "ffmpeg.exe";
        string filepath = Path.Combine(AppContext.BaseDirectory, "ffmpeg", ffmpegexe);
        if (File.Exists(filepath))
        {
            // 初始化FFmpeg包装器
            ffmpeg = new FFmpegWrapper(filepath);
            // 设置进度回调
            ffmpeg.OnProgress += (inputPath, percentage) =>
            {
                Console.WriteLine($"原始文件：{inputPath}，转换进度: {percentage:F2}%");
            };
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}