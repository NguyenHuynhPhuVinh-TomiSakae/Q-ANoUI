using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;

namespace System203
{
    public class NoUIProgram
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
        private const int SHIFT = 0x0004;
        private const int X_KEY = 0x58;
        
        private static IFirebaseClient? client;
        
        private static string lastTestValue = "";
        
        // Thêm biến để lưu trạng thái của Ctrl + H
        private static bool enableCtrlH = false;
        
        public static void Start()
        {
            // Hiển thị MessageBox để hỏi người dùng có muốn bật Ctrl + H
            DialogResult result = MessageBox.Show(
                "Bạn có muốn sử dụng tính năng Ctrl + H không?",
                "Cấu hình",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            enableCtrlH = (result == DialogResult.Yes);

            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }
            
            Thread mainThread = new Thread(() =>
            {
                RunAsync().GetAwaiter().GetResult();
            });
            mainThread.SetApartmentState(ApartmentState.STA);
            mainThread.Start();
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        static async Task RunAsync()
        {
            string hướngDẫn = "HƯỚNG DẪN SỬ DỤNG:\n\n";
            
            if (enableCtrlH)
            {
                hướngDẫn += "Ctrl + H: Gửi tín hiệu tới máy khác!\n";
            }
            
            hướngDẫn += "Ctrl + J: Lưu nội dung clipboard hiện tại lên CSDL.\n" + 
                        "Ctrl + K: Lấy nội dung clipboard đã lưu từ CSDL.\n\n" +
                        "Ctrl + Shift + X: Thoát chương trình\n\n" +
                        "Nhấn OK để chạy chương trình ẩn.";
                              
            MessageBox.Show(hướngDẫn, "System203", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
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
                if (enableCtrlH)
                {
                    RegisterHotKey(Process.GetCurrentProcess().MainWindowHandle, 1, CTRL, H_KEY);
                }
                RegisterHotKey(Process.GetCurrentProcess().MainWindowHandle, 2, CTRL, J_KEY);
                RegisterHotKey(Process.GetCurrentProcess().MainWindowHandle, 3, CTRL, K_KEY);
                RegisterHotKey(Process.GetCurrentProcess().MainWindowHandle, 4, CTRL | SHIFT, X_KEY);

                // Bắt đầu task theo dõi Firebase
                _ = MonitorFirebaseAsync();

                // Vòng lặp chính
                while (true)
                {
                    await Task.Delay(100);
                    
                    if (GetAsyncKeyState((Keys.Control)) != 0)
                    {
                        if (enableCtrlH && GetAsyncKeyState(Keys.H) != 0)
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
                        else if (GetAsyncKeyState(Keys.X) != 0 && GetAsyncKeyState(Keys.ShiftKey) != 0)
                        {
                            Environment.Exit(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi: {ex.Message}");
            }
        }

        private static async Task MonitorFirebaseAsync()
        {
            while (true)
            {
                try
                {
                    // Chỉ kiểm tra Firebase nếu Ctrl + H được bật
                    if (client != null && enableCtrlH)
                    {
                        var response = await client.GetAsync("test");
                        string currentValue = response?.Body?.ToString()?.Trim('"') ?? "";
                        
                        Debug.WriteLine($"Current value: {currentValue}, Last value: {lastTestValue}");
                        
                        // Chỉ thực hiện khi giá trị thay đổi và là "1"
                        if (currentValue == "1" && currentValue != lastTestValue)
                        {
                            Debug.WriteLine("Triggering Windows key press");
                            
                            // Nhấn phím Windows
                            keybd_event(VK_LWIN, 0, 0, 0);
                            await Task.Delay(200);
                            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, 0);
                            
                            Debug.WriteLine("Windows key press completed");
                        }
                        
                        lastTestValue = currentValue;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi khi theo dõi Firebase: {ex.Message}");
                }
                await Task.Delay(500);
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
                    Debug.WriteLine("Starting SaveToFirebase");
                    
                    // Đảm bảo xóa giá trị cũ trước
                    await client.DeleteAsync("test");
                    await Task.Delay(100);
                    
                    // Reset lastTestValue
                    lastTestValue = "";
                    
                    // Lưu giá trị "1"
                    Debug.WriteLine("Setting test value to 1");
                    await client.SetAsync("test", "1");
                    await Task.Delay(5000);
                    
                    Debug.WriteLine("Deleting test value");
                    await client.DeleteAsync("test");
                    
                    Debug.WriteLine("SaveToFirebase completed");
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
            }
        }
    } //kết nối csdl

    public enum Keys
    {
        Control = 0x11,
        H = 0x48,
        J = 0x4A,
        K = 0x4B,
        X = 0x58,
        ShiftKey = 0x10
    }
}