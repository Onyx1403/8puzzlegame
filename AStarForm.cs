using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _8PuzzleGame
{
    public partial class MainFrom : Form
    {
        private enum Direction { LEFT = 0, RIGHT = 1, UP = 2, DOWN = 3 }
        private System.Diagnostics.Stopwatch stopwatch;

        #region Thuoc tinh
        private const string FINISH = "123804765";// trạng thái đích
        private const int SPEED = 800; // tốc độ di chuyển của các ô (ms)
        private List<Status_Node> OpenList; // danh sách các trạng thái cần mở (frontier)
        private List<Status_Node> CloseList; // danh sách các trạng thái đã mở (closed)
        private List<Status_Node> TraceList; // danh sách các trạng thái theo đường đi tới đích
        private int StepCount; // đếm số bước di chuyển hiện tại
        private Status_Node CurNode; // trạng thái hiện tại đang hiển thị/xét
        private Status_Node Game; // trạng thái đề bài (bắt đầu)
        private bool first = true;
        private PictureBox pbGame;
        private ProgressBar progressBar1;
        private Timer timerPlay;
        private IContainer components;
        private Button btnNew;
        private Button btnResolve;
        private Button btnReset;
        private Label lblCount;
        private Label lblFinish;
        private Label lblCountStatistic;
        private bool reset = false;
        #endregion

        #region Cac phuong thuc
        // tính toán xem trạng thái có thể giải được hay không
        // Tính số nghịch thế (bỏ qua '0')
        private int CountInversions(string code)
        {
            int[] arr = new int[9];
            for (int i = 0; i < 9; i++)
                arr[i] = code[i] - '0';

            int inv = 0;
            for (int i = 0; i < 9; i++)
            {
                if (arr[i] == 0) continue;
                for (int j = i + 1; j < 9; j++)
                {
                    if (arr[j] == 0) continue;
                    if (arr[j] < arr[i]) inv++;
                }
            }
            return inv;
        }

        // Với 3x3: trạng thái có thể giải nếu parity (tính chẵn/lẻ) nghịch thế giống trạng thái đích
        private bool IsCanSolve(Status_Node n)
        {
            int invStart = CountInversions(n.Code);
            int invGoal = CountInversions(FINISH);
            return (invStart % 2) == (invGoal % 2);
        }


        // thêm trạng thái vào danh sách mở
        // sao cho danh sách luôn được sắp xếp theo giá trị F tăng dần
        // biến vào là node n

        private void AddToOpenList(Status_Node n)
        {
            // Nếu tồn tại node tương đương với G nhỏ hơn hoặc bằng thì không thêm
            for (int i = 0; i < OpenList.Count; i++)
            {
                if (n.Code == OpenList[i].Code)
                {
                    if (n.G < OpenList[i].G)
                    {
                        // thay thế phần tử hiện tại bằng đường đi tốt hơn
                        OpenList.RemoveAt(i);
                        break; // tiếp tục chèn
                    }
                    else
                    {
                        return; // không tốt hơn phần tử tồn tại, bỏ qua
                    }
                }
            }

            // chèn giữ thứ tự tăng dần theo F
            int insertIndex = 0;
            while (insertIndex < OpenList.Count && OpenList[insertIndex].F <= n.F)
                insertIndex++;
            OpenList.Insert(insertIndex, n);
        }
        // lấy 1 trạng thái từ danh sách mở
        // do list đã sắp xếp theo F tăng dần nên lấy phần tử đầu tiên (OpenList[0])
        // trả về node lấy được

        private Status_Node GetNodeFromOpenList()
        {
            if (OpenList.Count > 0)
            {
                Status_Node n = OpenList[0];
                OpenList.RemoveAt(0);
                return n;
            }
            return null;

        }

        // kiểm tra sự tồn tại của 1 node trong list Close
        // nếu có trả về true, ngược lại trả về false
        private bool IsInCloseList(Status_Node n)
        {
            for (int i = 0; i < CloseList.Count; i++)
            {
                if (CloseList[i].Code.Equals(n.Code))
                    return true;
            }
            return false;
        }

        // hàm đánh giá
        // trả về giá trị H là tổng khoảng cách Manhattan đến vị trí đích
        private int CalculateH(string current)
        {
            int h = 0;
            for (int i = 0; i < 9; i++)
            {
                char tile = current[i];
                if (tile == '0') continue;
                int curR = i / 3, curC = i % 3;

                int goalIndex = FINISH.IndexOf(tile);
                int goalR = goalIndex / 3, goalC = goalIndex % 3;

                h += Math.Abs(curR - goalR) + Math.Abs(curC - goalC);
            }
            return h;
        }

        // tính toán các trạng thái con của node n (sử dụng bảng kề để tránh sai sót)
        private void ExpandNode(Status_Node n)
        {
            int index = n.Code.IndexOf('0');
            if (index < 0) return;
            int[][] neighbors = new int[9][]
            {
                new[] {1,3},    // 0
                new[] {0,2,4},  // 1
                new[] {1,5},    // 2
                new[] {0,4,6},  // 3
                new[] {1,3,5,7},// 4
                new[] {2,4,8},  // 5
                new[] {3,7},    // 6
                new[] {4,6,8},  // 7
                new[] {5,7}     // 8
            };

            foreach (int nb in neighbors[index])
            {
                char[] childArr = n.Code.ToCharArray();
                // đổi chỗ ô trống tại index với ô tại nb
                char tmp = childArr[index];
                childArr[index] = childArr[nb];
                childArr[nb] = tmp;
                string map = new string(childArr);
                if (n.Parent == null || !n.Code.Equals(map))
                {
                    Status_Node child = new Status_Node(map, n, n.G + 1, CalculateH(map));
                    if (!IsInCloseList(child))
                    {
                        AddToOpenList(child);
                    }
                }
            }
        }

        private void Stop()// dừng lại nếu đã tìm được đích
        {
            if (this.CurNode != null && this.CurNode.Code.Equals(FINISH))// nếu nút hiện tại là trạng thái đích
            {
                // hiển thị thông báo hoàn thành
                MessageBox.Show("Đã giải xong", "Hoàn thành", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // tiếp tục hay thoát
                if (MessageBox.Show("Bạn có muốn tiếp tục không?", "Tiếp tục", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    lblFinish.Text = "";
                    lblCountStatistic.Text = "";
                }
                else
                {
                    Application.Exit();
                }
            }
        }
        // di chuyển các ô trong 1 hướng cụ thể (trái, phải, lên, xuống)
        private bool MoveStep(Direction dir)
        {
            int index = -1;// khởi tạo biến index với giá trị -1
            if (this.CurNode != null)
            {
                index = this.CurNode.Code.IndexOf('0');// tìm vị trí ô trống trong trạng thái hiện tại
            }
            if (index < 0) return false;
            int r = index / 3;
            int c = index % 3;
            char[,] data = this.CurNode.Map;
            switch (dir)// thực hiện việc di chuyển ô trống theo hướng được chỉ định
            {
                case Direction.LEFT:
                    {
                        if (c == 0) return false; // không thể di chuyển sang trái
                        char tmp = data[r, c - 1];
                        data[r, c] = tmp;
                        data[r, c - 1] = '0';
                        this.CurNode.Map = data;
                        break;
                    }
                case Direction.RIGHT:
                    {
                        if (c == 2) return false; // không thể di chuyển sang phải
                        char tmp = data[r, c + 1];
                        data[r, c] = tmp;
                        data[r, c + 1] = '0';
                        this.CurNode.Map = data;
                        break;
                    }
                case Direction.UP:
                    {
                        if (r == 0) return false; // không thể di chuyển lên trên
                        char tmp = data[r - 1, c];
                        data[r, c] = tmp;
                        data[r - 1, c] = '0';
                        this.CurNode.Map = data;
                        break;
                    }
                case Direction.DOWN:
                    {
                        if (r == 2) return false; // không thể di chuyển xuống dưới
                        char tmp = data[r + 1, c];
                        data[r, c] = tmp;
                        data[r + 1, c] = '0';
                        this.CurNode.Map = data;
                        break;
                    }

            }
            return true;
        }

        // Tìm kiếm A* từ startCode đến goalCode, trả về node đích hoặc null nếu không tìm được
        private Status_Node StartPosition(string startCode, string goalCode)
        {
            OpenList.Clear();
            CloseList.Clear();

            Status_Node start = new Status_Node(startCode, null, 0, CalculateH(startCode));
            AddToOpenList(start);

            while (OpenList.Count > 0)
            {
                Status_Node current = GetNodeFromOpenList();
                if (current.Code == goalCode)
                    return current;
                CloseList.Add(current);
                ExpandNode(current);
            }
            return null;
        }
        #endregion
        public MainFrom()
        {
            InitializeComponent();
            OpenList = new List<Status_Node>();
            CloseList = new List<Status_Node>();
            TraceList = new List<Status_Node>();
            stopwatch = new System.Diagnostics.Stopwatch();
            this.timerPlay.Interval = SPEED; // thời gian giữa các bước di chuyển
        }
        private void PanitBackground(Graphics g)// vẽ lưới 3 x 3
        {
            // Vẽ lưới khớp với Status_Node.Paint dùng 3 ô 100px (tổng 300x300)
            g.Clear(Color.Gray);
            Pen p = new Pen(Color.Black, 3);

            // Viền ngoài (0,0) tới (300,300)
            g.DrawRectangle(p, 0, 0, 300, 300);
            // Các đường dọc
            g.DrawLine(p, 100, 0, 100, 300);
            g.DrawLine(p, 200, 0, 200, 300);
            // Các đường ngang
            g.DrawLine(p, 0, 100, 300, 100);
            g.DrawLine(p, 0, 200, 300, 200);
        }
        // vẽ 8 puzzle lên picturebox
        private void pbGame_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            PanitBackground(g);
            if (this.CurNode != null)
            {
                this.CurNode.Paint(g);
            }
        }
        // di chuyển các ô khi click chuột
        private void pbGame_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.timerPlay.Enabled)// nếu đang trong quá trình tự động giải
            {
                return;// không cho phép di chuyển bằng chuột
            }
            Point p = new Point(e.Y / 100, e.X / 100);// lấy toạ độ ô được click (giữ hướng toạ độ ban đầu)
            bool isok = false;// biến kiểm tra tính hợp lệ của bước di chuyển
            if (!isok)
            {
                Point p1 = p;
                p1.Offset(-1, 0);// ô bên trên
                if (p1.X >= 0 && p1.Y >= 0 && p1.X < 3 && p1.Y < 3)// kiểm tra ô bên trên có hợp lệ không
                {
                    if (this.CurNode.Map[p1.X, p1.Y] == '0')// nếu ô bên trên là ô trống
                    {
                        isok = MoveStep(Direction.UP);// di chuyển lên trên
                    }
                }
            }
            if (!isok)
            {
                Point p2 = p;
                p2.Offset(1, 0);// ô bên dưới
                if (p2.X >= 0 && p2.Y >= 0 && p2.X < 3 && p2.Y < 3)// kiểm tra ô bên dưới có hợp lệ không
                {
                    if (this.CurNode.Map[p2.X, p2.Y] == '0')// nếu ô bên dưới là ô trống
                    {
                        isok = MoveStep(Direction.DOWN);// di chuyển xuống dưới
                    }
                }
            }
            if (!isok)
            {
                Point p3 = p;
                p3.Offset(0, -1);// ô bên trái
                if (p3.X >= 0 && p3.Y >= 0 && p3.X < 3 && p3.Y < 3)// kiểm tra ô bên trái có hợp lệ không
                {
                    if (this.CurNode.Map[p3.X, p3.Y] == '0')// nếu ô bên trái là ô trống
                    {
                        isok = MoveStep(Direction.LEFT);// di chuyển sang trái
                    }
                }
            }
            if (!isok)
            {
                Point p4 = p;
                p4.Offset(0, 1);// ô bên phải
                if (p4.X >= 0 && p4.Y >= 0 && p4.X < 3 && p4.Y < 3)// kiểm tra ô bên phải có hợp lệ không
                {
                    if (this.CurNode.Map[p4.X, p4.Y] == '0')// nếu ô bên phải là ô trống
                    {
                        isok = MoveStep(Direction.RIGHT);// di chuyển sang phải
                    }
                }
            }
            if (isok)
            {
                this.btnResolve.Text = "Giải";
                this.pbGame.Refresh();// cập nhật lại picturebox
                this.StepCount++;// tăng số bước di chuyển
                this.lblCount.Text = this.StepCount.ToString();// hiển thị số bước di chuyển
                Stop();
            }
        }

        private void BtnNew_click(object sender, EventArgs e)
        {
            this.timerPlay.Enabled = false;
            this.btnResolve.Enabled = true;
            // khởi tạo trạng thái ban đầu
            if (this.first)
            {
                // bắt đầu từ trạng thái đích và thực hiện các bước hợp lệ ngẫu nhiên để trạng thái vẫn có thể giải được
                this.CurNode = new Status_Node("123804765", null, 0, CalculateH("123804765"));
                Random rnd = new Random();
                for (int i = 0; i < 100; i++)
                    MoveStep((Direction)(rnd.Next(4)));
                this.first = false;
            }
            else
            {
                if (this.reset)
                {
                    this.CurNode = this.Game;
                    this.reset = false;
                }
                else
                {
                    // khởi tạo ngẫu nhiên bằng cách xáo trộn từ trạng thái đích (luôn solvable)
                    this.CurNode = new Status_Node(FINISH, null, 0, 0);
                    Random random = new Random();
                    for (int i = 0; i < 100; i++)
                    {
                        int j = random.Next(1000) % 4;
                        MoveStep((Direction)j);// xáo trộn bằng cách di chuyển ngẫu nhiên
                    }
                }
            }
            this.Game = new Status_Node(this.CurNode.Code, null, 0, CalculateH(this.CurNode.Code));
            this.btnResolve.Text = "Giải";
            this.StepCount = 0;
            this.lblCount.Text = this.StepCount.ToString();
            this.pbGame.Refresh();// cập nhật lại picturebox
        }
        private void btnResolve_Click(object sender, EventArgs e)
        {
            if (this.btnResolve.Text == "Giải" || this.btnResolve.Text == "Tiếp tục")
            {
                if (this.btnResolve.Text == "Giải")
                {
                    if (IsCanSolve(this.CurNode))
                    {
                        progressBar1.Style = ProgressBarStyle.Marquee;
                        progressBar1.MarqueeAnimationSpeed = 20;
                        stopwatch.Restart(); // bắt đầu đếm thời gian tìm kiếm
                        Status_Node n = StartPosition(this.CurNode.Code, FINISH);
                        stopwatch.Stop(); // dừng thời gian sau khi tìm xong
                        if (n != null)
                        {
                            TraceList.Clear();
                            // xây dựng đường đi từ bắt đầu tới đích
                            Stack<Status_Node> st = new Stack<Status_Node>();
                            Status_Node cur = n;
                            while (cur != null)
                            {
                                st.Push(cur);
                                cur = cur.Parent;
                            }
                            while (st.Count > 0)
                                TraceList.Add(st.Pop());
                            lblCountStatistic.Text = $"Thời gian giải: {stopwatch.ElapsedMilliseconds} ms";

                            // chuẩn bị cho hoạt họa
                            this.StepCount = 0;
                            progressBar1.Style = ProgressBarStyle.Continuous;
                            progressBar1.Value = 0;
                            progressBar1.Maximum = TraceList.Count;
                            this.timerPlay.Enabled = true;

                        }
                        else
                        {
                            MessageBox.Show("Không tìm được lời giải.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Trạng thái chưa thể giải được", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                // tạm dừng
                this.btnResolve.Text = "Tiếp tục";
                this.timerPlay.Enabled = false;
            }
        }
        // truy vết, hiển thị các bước di chuyển
        private void timerPlay_Tick(object sender, EventArgs e)
        {
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Maximum = this.TraceList.Count;
            progressBar1.Minimum = 0;
            progressBar1.Step = 1;
            if (this.StepCount < this.TraceList.Count)
            {
                this.CurNode = this.TraceList[this.StepCount];
                pbGame.Refresh();// cập nhật lại picturebox
                this.lblCount.Text = "Số bước giải: " + this.StepCount++;
                progressBar1.PerformStep();
                if (this.StepCount == this.TraceList.Count)
                {
                    this.timerPlay.Enabled = false;
                    lblFinish.Text = "Đã giải xong";
                    MessageBox.Show("Đã giải xong", "Hoàn thành", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (MessageBox.Show("Bạn có muốn tiếp tục không", "?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        lblFinish.Text = "";
                        lblCountStatistic.Text = "";
                        progressBar1.Value = 0;
                        BtnNew_click(null, null);
                    }
                    else
                    {
                        Application.Exit();
                    }
                    GC.Collect();
                    this.btnResolve.Text = "Giải";
                }
            }
        }
        private void MainFrom_Shown(object sender, EventArgs e)
        {
            BtnNew_click(null, null);
        }
        private void pbGame_Click(object sender, EventArgs e)
        {
        }
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pbGame = new System.Windows.Forms.PictureBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.timerPlay = new System.Windows.Forms.Timer(this.components);
            this.btnNew = new System.Windows.Forms.Button();
            this.btnResolve = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.lblCount = new System.Windows.Forms.Label();
            this.lblFinish = new System.Windows.Forms.Label();
            this.lblCountStatistic = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbGame)).BeginInit();
            this.SuspendLayout();
            // 
            // pbGame
            // 
            this.pbGame.Location = new System.Drawing.Point(32, 65);
            this.pbGame.Name = "pbGame";
            this.pbGame.Size = new System.Drawing.Size(300, 300);
            this.pbGame.TabIndex = 0;
            this.pbGame.TabStop = false;
            this.pbGame.Paint += new System.Windows.Forms.PaintEventHandler(this.pbGame_Paint);
            this.pbGame.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pbGame_MouseClick);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(32, 371);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(246, 23);
            this.progressBar1.TabIndex = 1;
            // 
            // timerPlay
            // 
            this.timerPlay.Tick += new System.EventHandler(this.timerPlay_Tick);
            // 
            // btnNew
            // 
            this.btnNew.Location = new System.Drawing.Point(431, 128);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(75, 23);
            this.btnNew.TabIndex = 2;
            this.btnNew.Text = "Tạo mới";
            this.btnNew.UseVisualStyleBackColor = true;
            this.btnNew.Click += new System.EventHandler(this.BtnNew_click);
            // 
            // btnResolve
            // 
            this.btnResolve.Location = new System.Drawing.Point(431, 177);
            this.btnResolve.Name = "btnResolve";
            this.btnResolve.Size = new System.Drawing.Size(75, 23);
            this.btnResolve.TabIndex = 3;
            this.btnResolve.Text = "Giải";
            this.btnResolve.UseVisualStyleBackColor = true;
            this.btnResolve.Click += new System.EventHandler(this.btnResolve_Click);
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(431, 232);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(75, 23);
            this.btnReset.TabIndex = 4;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // lblCount
            // 
            this.lblCount.AutoSize = true;
            this.lblCount.Location = new System.Drawing.Point(446, 65);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(41, 16);
            this.lblCount.TabIndex = 5;
            this.lblCount.Text = "Count";
            // 
            // lblFinish
            // 
            this.lblFinish.AutoSize = true;
            this.lblFinish.Location = new System.Drawing.Point(443, 287);
            this.lblFinish.Name = "lblFinish";
            this.lblFinish.Size = new System.Drawing.Size(0, 16);
            this.lblFinish.TabIndex = 6;
            // 
            // lblCountStatistic
            // 
            this.lblCountStatistic.AutoSize = true;
            this.lblCountStatistic.Location = new System.Drawing.Point(443, 329);
            this.lblCountStatistic.Name = "lblCountStatistic";
            this.lblCountStatistic.Size = new System.Drawing.Size(0, 16);
            this.lblCountStatistic.TabIndex = 7;
            // 
            // MainFrom
            // 
            this.ClientSize = new System.Drawing.Size(560, 386);
            this.Controls.Add(this.lblCountStatistic);
            this.Controls.Add(this.lblFinish);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnResolve);
            this.Controls.Add(this.btnNew);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.pbGame);
            this.Name = "MainFrom";
            this.Load += new System.EventHandler(this.MainFrom_Load);
            this.Shown += new System.EventHandler(this.MainFrom_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pbGame)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void MainFrom_Load(object sender, EventArgs e)
        {

        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            // Đặt lại CurNode về trạng thái đích
            this.CurNode = new Status_Node(FINISH, null, 0, CalculateH(FINISH));

            // Reset các thông số hiển thị
            this.StepCount = 0;
            this.lblCount.Text = "0";
            this.lblFinish.Text = "";
            this.lblCountStatistic.Text = "";
            this.btnResolve.Text = "Giải";

            // Vẽ lại bàn cờ
            this.pbGame.Refresh();
        }

    }
}