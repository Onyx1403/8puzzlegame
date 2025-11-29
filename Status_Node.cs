using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _8PuzzleGame
{
    internal class Status_Node
    {
        private string m_Code;
        private char[,] m_Map = new char[3, 3];
        private int m_G;
        private int m_H;
        private Status_Node m_Parent;
        private Font m_Front;

        public string Code
        {
            get { return m_Code; }
            set
            {
                m_Code = value;
                for (int i = 0; i < 9; i++)
                {
                    m_Map[i / 3, i % 3] = value[i];
                }
            }
        }

        public Status_Node Parent
        {
            get { return m_Parent; }
            set { m_Parent = value; }
        }

        public char[,] Map
        {
            get { return m_Map; }
            set
            {
                m_Map = value;
                StringBuilder sb = new StringBuilder(9);

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        sb.Append(value[i, j]);
                    }
                }
                m_Code = sb.ToString();
            }
        }
        // ham danh gia
        public int G
        {
            get { return m_G; }
            set { m_G = value; }
        }

        public int H
        {
            get { return m_H; }
            set { m_H = value; }
        }

        public int F
        {
            get { return m_G + m_H; }
        }

        public Status_Node(string code, Status_Node parent, int g, int h)
        {
            this.Code = code;
            this.m_G = g;
            this.m_H = h;
            this.m_Parent = parent;
            m_Front = new Font("Times New Roman", 25F, FontStyle.Regular, GraphicsUnit.Point);
        }

        // vẽ ma trận Puzzle lên đồ họa dựa trên ma trận và ký tự của mỗi ô
        public void Paint(Graphics g)
        {
            if (g == null)
            {
                return;
            }
            for (int col = 0; col < 3; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    Rectangle rect = new Rectangle(row * 100, col * 100, 100, 100);
                    g.DrawRectangle(Pens.Black, rect);
                    // vẽ ký tự trong ô
                    char c = m_Map[col, row];
                    if (c != '0') // không vẽ ô trống
                    {
                        StringFormat sf = new StringFormat();
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        g.DrawString(c.ToString(), m_Front, Brushes.Blue, rect, sf);
                    }
                }
            }
        }
        // --- Phương thức So sánh ---
        // Cần thiết để sử dụng trong HashSet/Dictionary (Mặc dù A* thường dùng List/PriorityQueue)
        public override bool Equals(object obj)
        {
            return obj is Status_Node node &&
                   m_Code == node.m_Code;
        }
        public override int GetHashCode()
        {
            return -2134842013 + EqualityComparer<string>.Default.GetHashCode(m_Code);

        }
    }
}
