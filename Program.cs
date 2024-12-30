using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;

namespace QANoUI
{
    public class Program
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_LWIN = 0x5B; // Mã phím Windows
        private const int CTRL = 0x0002;
        private const int H_KEY = 0x48;
        private const int J_KEY = 0x4A;
        private const int K_KEY = 0x4B;
        
        private static IFirebaseClient? client;
        
        private static string lastTestValue = "";
        
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Thread mainThread = new Thread(() =>
            {
                RunAsync().GetAwaiter().GetResult();
            });
            mainThread.SetApartmentState(ApartmentState.STA);
            mainThread.Start();
            Application.Run();
        }

        static async Task RunAsync()
        {
            MessageBox.Show("Q&ANoUI version 1.0\nNhấn OK để bắt đầu chạy ngầm...", "Q&ANoUI", MessageBoxButtons.OK);
            
            try 
            {
                // Cấu hình Firebase
                IFirebaseConfig config = new FirebaseConfig
                {
                     AuthSecret = "y5KYvYGhHei338L0jIWmgHhR5B4oIEs9kybuky01",
                    BasePath = "https://test-e61cd-default-rtdb.asia-southeast1.firebasedatabase.app/"
                };

                client = new FireSharp.FirebaseClient(config);

                // Đăng ký các phím tắt
                RegisterHotKey(Process.GetCurrentProcess().MainWindowHandle, 1, CTRL, H_KEY);
                RegisterHotKey(Process.GetCurrentProcess().MainWindowHandle, 2, CTRL, J_KEY);
                RegisterHotKey(Process.GetCurrentProcess().MainWindowHandle, 3, CTRL, K_KEY);

                // Bắt đầu task theo dõi Firebase
                _ = MonitorFirebaseAsync();

                // Vòng lặp chính
                while (true)
                {
                    await Task.Delay(100);
                    
                    if (GetAsyncKeyState((Keys.Control)) != 0)
                    {
                        if (GetAsyncKeyState(Keys.H) != 0)
                        {
                            await SaveToFirebase();
                        }
                        else if (GetAsyncKeyState(Keys.J) != 0)
                        {
                            await SaveClipboardToFirebase();
                            await Task.Delay(500);
                        }
                        else if (GetAsyncKeyState(Keys.K) != 0)
                        {
                            await LoadClipboardFromFirebase();
                            await Task.Delay(500);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static async Task MonitorFirebaseAsync()
        {
            while (true)
            {
                try
                {
                    if (client != null)
                    {
                        var response = await client.GetAsync("test");
                        string currentValue = response?.Body?.ToString() ?? "";
                        
                        // Chỉ thực hiện khi giá trị thay đổi và là "1"
                        if (currentValue == "1" && currentValue != lastTestValue)
                        {
                            // Nhấn phím Windows
                            keybd_event(VK_LWIN, 0, 0, 0);
                            // Chờ một chút
                            await Task.Delay(100);
                            // Nhả phím Windows
                            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, 0);
                        }
                        
                        lastTestValue = currentValue;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi khi theo dõi Firebase: {ex.Message}");
                }
                await Task.Delay(1000); // Kiểm tra mỗi giây
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        private static async Task SaveToFirebase()
        {
            try
            {
                if (client != null)
                {
                    // Lưu giá trị "1" dưới dạng chuỗi
                    await client.SetAsync("test", "1");
                    // Không cần nhấn phím Windows ở đây nữa vì MonitorFirebaseAsync sẽ làm việc đó
                    await Task.Delay(5000);
                    await client.DeleteAsync("test");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi lưu vào Firebase: {ex.Message}");
            }
        }

        private static async Task SaveClipboardToFirebase()
        {
            try
            {
                string clipboardText = "";
                Thread staThread = new Thread(() =>
                {
                    if (Clipboard.ContainsText(TextDataFormat.Text))
                    {
                        clipboardText = Clipboard.GetText(TextDataFormat.Text);
                    }
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();

                if (!string.IsNullOrEmpty(clipboardText) && client != null)
                {
                    Debug.WriteLine($"Đang lưu text: {clipboardText}");
                    string encodedText = clipboardText.Replace("\\", "[[BACKSLASH]]")
                                                   .Replace("\r\n", "[[NEWLINE]]")
                                                   .Replace("\n", "[[NEWLINE]]")
                                                   .Replace("\"", "[[QUOTE]]")
                                                   .Trim('"');
                    await client.SetAsync("clipboard", encodedText);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi lưu clipboard vào Firebase: {ex.Message}");
                MessageBox.Show($"Lỗi khi lưu clipboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static async Task LoadClipboardFromFirebase()
        {
            try
            {
                if (client != null)
                {
                    var response = await client.GetAsync("clipboard");
                    if (!string.IsNullOrEmpty(response.Body))
                    {
                        string decodedText = response.Body.Trim('"')
                                             .Replace("[[NEWLINE]]", Environment.NewLine)
                                             .Replace("[[QUOTE]]", "\"")
                                             .Replace("[[BACKSLASH]]", "\\");
                        Debug.WriteLine($"Đang lấy text: {decodedText}");
                        
                        Thread staThread = new Thread(() =>
                        {
                            Clipboard.SetText(decodedText, TextDataFormat.Text);
                        });
                        staThread.SetApartmentState(ApartmentState.STA);
                        staThread.Start();
                        staThread.Join();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi lấy clipboard từ Firebase: {ex.Message}");
                MessageBox.Show($"Lỗi khi lấy clipboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    } //kết nối csdl

    public enum Keys
    {
        Control = 0x11,
        H = 0x48,
        J = 0x4A,
        K = 0x4B
    }
}
