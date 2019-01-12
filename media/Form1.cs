using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace media
{
    public partial class 图片检索 : Form
    {
        FolderBrowserDialog dialog = new FolderBrowserDialog();
        double[] img; //检索图片的颜色直方图
        double[][] imglist = new double[2000][];//图片库中的颜色直方图
        string[] files; //图片库中的图片路径
        double[] sim = new double[2000]; //图片与目标图片间的相似度
        string path;
        int tag=0;//标记窗口大小状态;
        int pwidth=0, pheight=0;//定义灰度值数组的长、宽;
        Image image0, image1;//待比较图片和图片集中的图;
        public int[] f = new int[2000];//记录各图片的指纹;
        public double[] hm = new double[2000];//记录汉明距离;
        int fnum = 0;//记录指纹数;
        int of;//记录待比较图片的指纹;

        //定义无边框窗体Form  
        [DllImport("user32.dll")]//*********************拖动无窗体的控件  
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;

        private void gPanelTitleBack_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);//调用移动无窗体控件函数;
        }

        public 图片检索()
        {
            InitializeComponent();
            panel4.Visible = false;
            this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.gPanelTitleBack_MouseDown);
            this.panel5.MouseDown += new System.Windows.Forms.MouseEventHandler(this.gPanelTitleBack_MouseDown);
            this.panel6.MouseDown += new System.Windows.Forms.MouseEventHandler(this.gPanelTitleBack_MouseDown);
        }

        public double[] GetHistogram(byte[] rgb, int len)
        {
            int i, index, r, g, b;//将颜色16等分
            double[] rgbgistogram = new double[64];//共有64种颜色
            for (i = 0; i < len; i += 3)
            {
                r = rgb[i + 2] / 64;
                g = rgb[i + 1] / 64;
                b = rgb[i] / 64;
                index = r + g * 4 + b * 4 * 4;
                rgbgistogram[index]++;
            }
            return rgbgistogram; //返回直方图
        }

        private double[] RetrieveRGB(Bitmap p)
        {
            double[] histogram = new double[64];
            if (p != null)
            {
                int width = p.Width;
                int height = p.Height;
                int length = height * 3 * width;
                byte[] RGB = new byte[length];
                Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData data = p.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                System.IntPtr Scan0 = data.Scan0;
                System.Runtime.InteropServices.Marshal.Copy(Scan0, RGB, 0, length);
                histogram = GetHistogram(RGB, length);
                System.Runtime.InteropServices.Marshal.Copy(RGB, 0, Scan0, length);
                p.UnlockBits(data);
            }
            return histogram;
        }

        private void button1_Click_1(object sender, EventArgs e)//关闭按钮;
        {
            Application.Exit();
        }
        
        private void button5_Click(object sender, EventArgs e)//最小化按钮;
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void button11_Click(object sender, EventArgs e)//最大化按钮;
        {
            if (tag == 0)
            {
                this.WindowState = FormWindowState.Maximized;
                tag = 1;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                tag = 0;
            }
        }

        private void button1_MouseMove(object sender, MouseEventArgs e)//关闭按钮的移动1;
        {
            this.button1.BackColor = System.Drawing.Color.Red;
        }

        private void button1_MouseLeave(object sender, EventArgs e)//关闭按钮的移动2;
        {
            this.button1.BackColor = System.Drawing.Color.FromArgb(35, 97, 108);
        }
        
        private void button7_Click(object sender, EventArgs e)//下拉面板;
        {
            panel4.Visible = true;
        }

        private void textChanged(object sender, EventArgs e)
        {
            if (sender.Equals(textBox2))
            {
                label2.Visible = textBox2.Text.Length < 1;
            }
        }

        private void label2_Click_1(object sender, EventArgs e)
        {
            if (sender.Equals(label2))
            {
                textBox2.Focus();
            }
        }
        
        private void button10_Click(object sender, EventArgs e)//检索+回收面板;
        {
            panel4.Visible = false;
            int i, j1, j2, j3, index;
            for (i = 0; i < files.Length; i++)
            {
                sim[i] = 0;
                for (j1 = 0; j1 < 4; j1++)
                    for (j2 = 0; j2 < 4; j2++)
                        for (j3 = 0; j3 < 4; j3++)
                        {
                            index = j3 + 4 * j2 + 16 * j1;
                            if (img[index] != 0 || imglist[i][index] != 0)
                            {
                                sim[i] += (1 - (Math.Abs(img[index] - imglist[i][index]) / Math.Max(img[index], imglist[i][index])));
                            }
                        }
                sim[i] = sim[i] / 64; //得到相似度;
            }
            //Sort(sim, files.Length);
            Sort(hm,sim, files.Length);
        }
        
        private void Sort(double[] hm, double[] sim, int len)
        {
            int i, j;
            //for (i = 0; i < len; i++)
                //this.textBox1.Text += Convert.ToString(hm[i]+"h  "+sim[i] + "s  ");
            string temp; double tmp;
            files = Directory.GetFiles(path);
            for (i = 0; i < len - 1; i++)
            {
                for (j = 0; j < len - i - 1; j++)
                {
                    if (hm[j] > hm[j + 1])
                    {
                        temp = files[j + 1];
                        files[j + 1] = files[j];
                        files[j] = temp;
                        tmp = hm[j + 1];
                        hm[j + 1] = hm[j];
                        hm[j] = tmp;
                        tmp = sim[j + 1];
                        sim[j + 1] = sim[j];
                        sim[j] = tmp;
                    }
                    else if (hm[j] == hm[j + 1])//若汉明距离相同，则按颜色相似度排序;
                    {
                        if (sim[j] < sim[j + 1])
                        {
                            temp = files[j + 1];
                            files[j + 1] = files[j];
                            files[j] = temp;
                            tmp = sim[j + 1];
                            sim[j + 1] = sim[j];
                            sim[j] = tmp;
                            tmp = hm[j + 1];
                            hm[j + 1] = hm[j];
                            hm[j] = tmp;
                        }
                    }
                }
            }

            //for (i = 0; i < len; i++)
              //  this.textBox3.Text += Convert.ToString(hm[i] + "h  " + sim[i] + "s  ");
            for (i = 0; i < len; i++) { Console.WriteLine(sim[i]); }
            int num = int.Parse(this.textBox2.Text);
            this.panel5.Controls.Clear();
            for (i = 0; i <= num / 4; i++)
            {
                for (j = i * 4; j < i * 4 + 4 && j < num; j++)
                {
                    PictureBox pb = new PictureBox();
                    pb.Width = 230;
                    pb.Height = 230;
                    pb.Location = new Point(j % 4 * 240, i * 240);
                    pb.ImageLocation = files[j];
                    pb.SizeMode = PictureBoxSizeMode.Zoom;
                    panel5.Controls.Add(pb);
                }
            }
        }
        
        private double getAverage(double[,] pixels)//得到像素点的平均值;
        {
            double count = 0;
            for (int i = 0; i < pwidth; i++)
            {
                for (int j = 0; j < pheight; j++)
                {
                    count += pixels[i,j];
                }
            }
            return count / (pwidth * pheight);
        }

        private int FingerPrint(Bitmap image)//平均哈希算法执行;
        {
            int i, j;
            pwidth = image.Width;
            pheight = image.Height;
            double[,] pixels = new double[pheight, pwidth];
            for (i = 0; i < pwidth; i++)
            {
                for (j = 0; j < pheight; j++)
                {
                    Color p = image.GetPixel(i, j);
                    pixels[i, j] = (double)(p.R * 0.3 + p.G * 0.59 + p.B * 0.11);
                }
            }

            double avg = getAverage(pixels);
            //textBox3.Text += Convert.ToString(avg + "m");
            
            byte[] bytes = new byte[pwidth* pheight];
            for (i = 0; i < pwidth; i++)
            {
                for (j = 0; j < pheight; j++)
                {
                    if (pixels[i, j] >= avg)
                    {
                        bytes[i * pheight + j] = 1;
                    }
                    else
                    {
                        bytes[i * pheight + j] = 0;
                    }
                }
            }
            int fingerprint = 0;
            for (i = 0; i < bytes.Length; i++)
            {
                fingerprint += (bytes[bytes.Length - i - 1] << i);
            }
            //fingerprint = getFingerPrint(pixels, avg);
            //textBox3.Text += Convert.ToString(fingerprint + "n");
            return fingerprint;
        }

        private int compareFingerPrint(int orgin_fingerprint, int compared_fingerprint)//对比指纹,计算汉明距离;
        {
            int count = 0;
            for (int i = 0; i < 64; i++)
            {
                byte orgin = (byte)(orgin_fingerprint & (1 << i));
                byte compared = (byte)(compared_fingerprint & (1 << i));
                if (orgin != compared)
                {
                    count++;
                }
            }
            return count;
        }
        
        private void run()
        {
            for (int i = 0; i < fnum; i++)
            {
                hm[i] = compareFingerPrint(of, f[i]);
                //textBox3.Text += Convert.ToString(hm[i])+ "V\n";
            }
        }

        private void button8_Click(object sender, EventArgs e)//choose picture；
        {
            OpenFileDialog openFile = new OpenFileDialog();
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string path = openFile.FileName;
                    image0 = Image.FromFile(path);
                    this.pictureBox2.Image = image0;
                    //Bitmap bmp = new Bitmap(image0, new Size(256, 256));
                    Bitmap bmp = new Bitmap(image0, new Size(8, 8));
                    img = RetrieveRGB(bmp); //得到检索图片的颜色直方图
                    of = FingerPrint(bmp);
                    //textBox3.Text += Convert.ToString(of + "n");
                }
                catch (Exception excp)
                {
                    MessageBox.Show(excp.Message);
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)//Import picture sets;
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                path = dialog.SelectedPath;
                try
                {
                    files = Directory.GetFiles(path);
                    fnum = 0;
                    for (int i = 0; i < files.Length; i++)
                    {
                        image1 = Image.FromFile(files[i]);
                        Bitmap bmp = new Bitmap(image1, new Size(8, 8));
                        //Bitmap bmp = new Bitmap(image1, new Size(256, 256));
                        imglist[i] = new double[64];
                        imglist[i] = RetrieveRGB(bmp); //得到彩色直方图
                        f[fnum]=FingerPrint(bmp);
                        //textBox3.Text += Convert.ToString(f[fnum] + "p");
                        fnum++;
                    }
                    run();
                }
                catch (Exception excp)
                {
                    MessageBox.Show(excp.Message);
                }
            }
        }
    }
}
