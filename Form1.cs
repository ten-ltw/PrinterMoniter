using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using NAudio.Wave;
using System.Speech.Synthesis;
using System.Timers;
using System.Xml.Linq;

namespace PrinterMoniter
{
    public partial class Form1 : Form
    {
        private bool _isRunning = false;
        private UIA3Automation? _automation;
        private System.Timers.Timer? _timer;
        private readonly HashSet<IntPtr> _processedWindows = new();
        private readonly Queue<Action> _playQueue = new Queue<Action>();
        private bool _isPlaying = false;
        private readonly object _lock = new object();
        public Form1()
        {
            InitializeComponent();
        }

        private void selectFileBtn_Click(object sender, EventArgs e)
        {
            if (fileSelector.ShowDialog() == DialogResult.OK)
            {
                xmlFilePathLabel.Text = fileSelector.FileName;
                GetPaperStatistics(xmlFilePathLabel.Text);
            }
        }

        private void startOrStopBtn_Click(object sender, EventArgs e)
        {
            if (_isRunning)
            {
                _isRunning = false;
                startOrStopBtn.Text = "开始";
                Stop();
            }
            else
            {
                _isRunning = true;
                startOrStopBtn.Text = "停止";
                Start();
            }
        }

        private void Start()
        {
            Console.WriteLine("打印机弹窗监控已启动 (.NET 8 + FlaUI)...");

            // 创建 UIA3 自动化实例
            _automation = new UIA3Automation();

            // 使用定时器轮询窗口
            _timer = new System.Timers.Timer(1000); // 每1秒检测一次
            _timer.Elapsed += CheckForPrinterWindows;
            _timer.Start();

            Console.WriteLine("正在监控打印机弹窗...");
        }

        private void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _automation?.Dispose();

            Console.WriteLine("监控已停止");
        }

        private void CheckForPrinterWindows(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (_automation == null) return;

                // 获取桌面
                var desktop = _automation.GetDesktop();

                // 获取所有顶层窗口
                var windows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));

                foreach (var window in windows)
                {
                    try
                    {
                        // 获取窗口句柄
                        var hwnd = window.Properties.NativeWindowHandle.ValueOrDefault;
                        if (hwnd == IntPtr.Zero) continue;

                        // 避免重复处理
                        if (_processedWindows.Contains(hwnd)) continue;

                        // 获取窗口标题
                        string title = window.Properties.Name.ValueOrDefault ?? "";

                        if (title.Contains("jpg") || IsPrinterWindow(title))
                        {
                            _processedWindows.Add(hwnd);
                            Console.WriteLine($"\n检测到打印机窗口: {title}");
                            ProcessPrinterDialog(window);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 忽略已关闭的窗口
                        if (!ex.Message.Contains("closed"))
                        {
                            Console.WriteLine($"处理窗口时出错: {ex.Message}");
                        }
                    }
                }

                // 清理已关闭的窗口
                _processedWindows.RemoveWhere(hwnd => !IsWindowStillOpen(hwnd));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查窗口时出错: {ex.Message}");
            }
        }

        private bool IsWindowStillOpen(IntPtr hwnd)
        {
            try
            {
                return _automation?.FromHandle(hwnd) != null;
            }
            catch
            {
                return false;
            }
        }

        private bool IsPrinterWindow(string title)
        {
            if (string.IsNullOrEmpty(title)) return false;

            string[] keywords = { "状态消息", "维护", "支付完成" };

            return keywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private void ProcessPrinterDialog(AutomationElement window)
        {
            try
            {
                // 获取窗口内所有文本
                string content = GetWindowContent(window);

                Console.WriteLine($"窗口内容: {content}");

                // 判断问题类型并处理
                var errorType = content switch
                {
                    var c when c.Contains("纸张已用完") => "缺纸",
                    var c when c.Contains("卡纸") => "卡纸",
                    var c when c.Contains("打印机") => "支付完成",
                    var c when c.Contains("抱歉") => "维护提示",
                    _ => "其他问题"
                };

                label1.Text = $"→ 检测到：{errorType}";
                Console.WriteLine($"→ 检测到：{errorType}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理对话框失败: {ex.Message}");
            }
        }

        private string GetWindowContent(AutomationElement window)
        {
            try
            {
                var allText = new System.Text.StringBuilder();
                allText.Append(window.Properties.Name.ValueOrDefault + " ");

                // 查找所有文本、按钮、编辑框
                var elements = window.FindAllDescendants(cf =>
                    cf.ByControlType(ControlType.Text)
                //.Or(cf.ByControlType(ControlType.Button))
                //.Or(cf.ByControlType(ControlType.Edit))
                );

                foreach (var element in elements)
                {
                    string text = element.Properties.Name.ValueOrDefault ?? "";
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        allText.Append(text + " ");
                    }
                }

                return allText.ToString();
            }
            catch
            {
                return "";
            }
        }

        private List<SimplePaperStatistical> GetPaperStatistics(string xmlFilePath)
        {
            try
            {
                var doc = XDocument.Load(xmlFilePath);

                var statistics = doc.Descendants("PrintPaperStatistical")
                    .Select(p => new SimplePaperStatistical
                    {
                        MachineName = p.Element("MachineName")?.Value ?? "",
                        PrinterName = p.Element("PrinterName")?.Value ?? "",
                        PaperAmount = int.Parse(p.Element("PaperAmount")?.Value ?? "0"),
                        AlertPaperAmount = int.Parse(p.Element("AlertPaperAmount")?.Value ?? "0")
                    })
                    .ToList();

                return statistics;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取配置失败: {ex.Message}");
                return new List<SimplePaperStatistical>();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SpeakText("剩余数量10张");
            Delay(1000);
            PlayAudio("./test.mp3");
            SpeakText("播放完成");
        }

        // 播放TTS
        private void SpeakText(string text)
        {
            lock (_lock)
            {
                _playQueue.Enqueue(() => PlayTtsInternal(text));
                StartProcessing();
            }
        }

        // 播放音频文件
        private void PlayAudio(string filePath)
        {
            lock (_lock)
            {
                _playQueue.Enqueue(() => PlayAudioInternal(filePath));
                StartProcessing();
            }
        }

        public void Delay(int milliseconds)
        {
            lock (_lock)
            {
                _playQueue.Enqueue(() => Thread.Sleep(milliseconds));
                StartProcessing();
            }
        }

        private void StartProcessing()
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                Task.Run(ProcessQueue);
            }
        }

        private void ProcessQueue()
        {
            while (true)
            {
                Action? action = null;

                lock (_lock)
                {
                    if (_playQueue.Count == 0)
                    {
                        _isPlaying = false;
                        return;
                    }
                    action = _playQueue.Dequeue();
                }

                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"播放失败: {ex.Message}");
                }
            }
        }

        private void PlayTtsInternal(string text)
        {
            try
            {
                using var synth = new SpeechSynthesizer();
                synth.SetOutputToDefaultAudioDevice();
                synth.Rate = 0;
                synth.Volume = 100;
                synth.Speak(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TTS播放失败: {ex.Message}");
            }
        }

        private void PlayAudioInternal(string filePath)
        {
            try
            {
                using var audioFile = new AudioFileReader(filePath);
                using var outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.Play();

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"音频播放失败: {ex.Message}");
            }
        }
    }

    // 简化的纸张统计模型
    public class SimplePaperStatistical
    {
        public string MachineName { get; set; } = string.Empty;
        public string PrinterName { get; set; } = string.Empty;
        public int PaperAmount { get; set; }
        public int AlertPaperAmount { get; set; }
    }

}
