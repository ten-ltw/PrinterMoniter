using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.UIA3;
using System;
using System.Linq;
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
        public Form1()
        {
            InitializeComponent();
        }

        private void selectFileBtn_Click(object sender, EventArgs e)
        {
            // ���� ShowDialog() ������ʾ�ļ�ѡ��Ի���
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
                startOrStopBtn.Text = "��ʼ";
                Stop();
            }
            else
            {
                _isRunning = true;
                startOrStopBtn.Text = "ֹͣ";
                Start();
            }
        }

        private void Start()
        {
            Console.WriteLine("��ӡ��������������� (.NET 8 + FlaUI)...");

            // ���� UIA3 �Զ���ʵ��
            _automation = new UIA3Automation();

            // ʹ�ö�ʱ����ѯ����
            _timer = new System.Timers.Timer(1000); // ÿ1����һ��
            _timer.Elapsed += CheckForPrinterWindows;
            _timer.Start();

            Console.WriteLine("���ڼ�ش�ӡ������...");
        }

        private void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _automation?.Dispose();

            Console.WriteLine("�����ֹͣ");
        }

        private void CheckForPrinterWindows(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (_automation == null) return;

                // ��ȡ����
                var desktop = _automation.GetDesktop();

                // ��ȡ���ж��㴰��
                var windows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));

                foreach (var window in windows)
                {
                    try
                    {
                        // ��ȡ���ھ��
                        var hwnd = window.Properties.NativeWindowHandle.ValueOrDefault;
                        if (hwnd == IntPtr.Zero) continue;

                        // �����ظ�����
                        if (_processedWindows.Contains(hwnd)) continue;

                        // ��ȡ���ڱ���
                        string title = window.Properties.Name.ValueOrDefault ?? "";

                        if (title.Contains("jpg") || IsPrinterWindow(title))
                        {
                            _processedWindows.Add(hwnd);
                            Console.WriteLine($"\n��⵽��ӡ������: {title}");
                            ProcessPrinterDialog(window);
                        }
                    }
                    catch (Exception ex)
                    {
                        // �����ѹرյĴ���
                        if (!ex.Message.Contains("closed"))
                        {
                            Console.WriteLine($"������ʱ����: {ex.Message}");
                        }
                    }
                }

                // �����ѹرյĴ���
                _processedWindows.RemoveWhere(hwnd => !IsWindowStillOpen(hwnd));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"��鴰��ʱ����: {ex.Message}");
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

            string[] keywords = {"״̬��Ϣ", "ά��", "֧�����"};

            return keywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private void ProcessPrinterDialog(AutomationElement window)
        {
            try
            {
                // ��ȡ�����������ı�
                string content = GetWindowContent(window);

                Console.WriteLine($"��������: {content}");

                // �ж��������Ͳ�����
                var errorType = content switch
                {
                    var c when c.Contains("ֽ��������") => "ȱֽ",
                    var c when c.Contains("��ֽ") => "��ֽ",
                    var c when c.Contains("��ӡ��") => "֧�����",
                    var c when c.Contains("��Ǹ") => "ά����ʾ",
                    _ => "��������"
                };

                label1.Text = $"�� ��⵽��{errorType}";
                Console.WriteLine($"�� ��⵽��{errorType}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"����Ի���ʧ��: {ex.Message}");
            }
        }

        private string GetWindowContent(AutomationElement window)
        {
            try
            {
                var allText = new System.Text.StringBuilder();
                allText.Append(window.Properties.Name.ValueOrDefault + " ");

                // ���������ı�����ť���༭��
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
                Console.WriteLine($"��ȡ����ʧ��: {ex.Message}");
                return new List<SimplePaperStatistical>();
            }
        }
    }

    // �򻯵�ֽ��ͳ��ģ��
    public class SimplePaperStatistical
    {
        public string MachineName { get; set; } = string.Empty;
        public string PrinterName { get; set; } = string.Empty;
        public int PaperAmount { get; set; }
        public int AlertPaperAmount { get; set; }
    }

}
