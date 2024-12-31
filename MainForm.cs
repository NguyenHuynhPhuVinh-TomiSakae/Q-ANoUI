using System.Windows.Forms;
using System.Drawing;

namespace System203
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            
            // Tạo và thêm các nút vào form
            Button btnNoUI = CreateHackerButton("Hỏi Đáp không giao diện", 70, 40);
            Button btnCtrlK = CreateHackerButton("Ctrl+K AI", 70, 100);
            
            // Gán sự kiện click cho các nút
            btnNoUI.Click += BtnNoUI_Click;
            btnCtrlK.Click += BtnCtrlK_Click;
            
            // Thêm nút vào form
            Controls.Add(btnNoUI);
            Controls.Add(btnCtrlK);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // MainForm
            // 
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(382, 203);
            ForeColor = Color.LightGreen;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "System203";
            ResumeLayout(false);
        }

        private Button CreateHackerButton(string text, int x, int y)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new System.Drawing.Point(x, y);
            btn.Size = new System.Drawing.Size(240, 40);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.FromArgb(45, 45, 45);
            btn.ForeColor = Color.LightGreen;
            btn.Font = new Font("Consolas", 10, FontStyle.Bold);
            btn.FlatAppearance.BorderColor = Color.LightGreen;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void BtnNoUI_Click(object sender, EventArgs e)
        {
            this.Hide();
            NoUIProgram.Start();
        }

        private void BtnCtrlK_Click(object sender, EventArgs e)
        {
            this.Hide();
            CtrlKProgram.Start();
        }
    }
} 