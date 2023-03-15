using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Drawing;
using System.Net;
using System.Net.Http;

using System.Runtime.InteropServices;

public class MouseEvents
{
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_MOUSEMOVE = 0x0200;
}

public class DrawArea : Panel
{
    bool drawing = false;
    bool errase = false;
    Bitmap bmap;
    Bitmap drawBmap;
    int oldX, oldY;
    int VS = 0;
    public DrawArea(Bitmap b) : base()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                  ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer |
                  ControlStyles.ResizeRedraw, true);
        bmap = b;
        drawBmap = new Bitmap(bmap.Width, bmap.Height);
        drawBmap.MakeTransparent(Color.Black);
        this.AutoScroll = true;
        ScrollBar vScrollBar1 = new VScrollBar();
        vScrollBar1.Dock = DockStyle.Right;
        vScrollBar1.Scroll += (sender, e) =>
        {
            this.VerticalScroll.Value = vScrollBar1.Value;
            this.VS = -(bmap.Height / 100 * (vScrollBar1.Value));
            this.Invalidate();
        };
        this.Controls.Add(vScrollBar1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        draw(e.Graphics);
    }

    private void draw(Graphics g)
    {
        g.DrawImage(bmap, 0, VS);
        g.DrawImage(drawBmap, 0, VS);
    }

    public void Errase()
    {
        Graphics g = Graphics.FromImage(drawBmap);
        g.Clear(Color.Transparent);
        this.Invalidate();
    }

    public async void Upload()
    {
        string filepath = System.IO.Path.GetTempFileName();
        Graphics g = Graphics.FromImage(bmap);
        g.DrawImage(drawBmap, 0, VS);
        bmap.Save(filepath);
        using (var client = new HttpClient())
        {
            using (var content =
                new MultipartFormDataContent("Upload---- ----BOUNDARYBOUNDARY----"))
            {
                FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                HttpContent fileStreamContent = new StreamContent(file);

                content.Add(fileStreamContent, "imagedata", "upload.jpg");

                using (
                   var message =
                       await client.PostAsync("https://tttt.dev.platform.clickio.com/shot.php", content))
                {
                    var input = await message.Content.ReadAsStringAsync();
                    System.Diagnostics.Process.Start(input);
                    System.Environment.Exit(0);
                }
            }
        }

    }

    protected override void WndProc(ref System.Windows.Forms.Message m)
    {
        switch (m.Msg)
        {
            case MouseEvents.WM_RBUTTONDOWN:
                oldX = (m.LParam.ToInt32() & 0xFFFF);
                oldY = (m.LParam.ToInt32() >> 16);
                this.errase = true;
                Graphics g = Graphics.FromImage(drawBmap);
                Rectangle crop = new Rectangle(oldX - 3, oldY - 3, 6, 6);
                g.SetClip(crop);
                g.Clear(Color.Transparent);
                this.Invalidate();
                break;
            case MouseEvents.WM_RBUTTONUP:
                this.errase = false;
                this.Invalidate();
                break;
            case MouseEvents.WM_LBUTTONDOWN:
                oldX = (m.LParam.ToInt32() & 0xFFFF);
                oldY = (m.LParam.ToInt32() >> 16);
                this.drawing = true;
                g = Graphics.FromImage(drawBmap);
                g.DrawLine(Pens.Red, oldX - 1, oldY - 1, oldX + 1, oldY + 1);
                this.Invalidate();
                break;
            case MouseEvents.WM_LBUTTONUP:
                this.drawing = false;
                break;
            case MouseEvents.WM_MOUSEMOVE:
                if (this.drawing || this.errase)
                {
                    int startX = (m.LParam.ToInt32() & 0xFFFF);
                    int startY = (m.LParam.ToInt32() >> 16);
                    g = Graphics.FromImage(drawBmap);
                    if (this.drawing)
                    {
                        g.DrawLine(Pens.Red, oldX - 1, oldY - 1, startX + 1, startY + 1);
                    }
                    else
                    {
                        crop = new Rectangle(startX - 1, startY - 1, 6, 6);
                        g.SetClip(crop);
                        g.Clear(Color.Transparent);
                    }
                    oldX = startX + 1;
                    oldY = startY + 1;
                    this.Invalidate();
                }
                break;
        }
        base.WndProc(ref m);
    }

}

public class Editor : Form
{
    private DrawArea drawArea;

    public Editor(Bitmap b)
    {
        this.WindowState = FormWindowState.Maximized;

        ToolBar toolBar = new ToolBar();
        Controls.Add(toolBar);
        ToolBarButton toolBarButton1 = new ToolBarButton();
        toolBarButton1.Text = "Upload";
        toolBar.Buttons.Add(toolBarButton1);

        ToolBarButton toolBarButton3 = new ToolBarButton();
        toolBarButton3.Text = "Erase";
        toolBar.Buttons.Add(toolBarButton3);

        this.Padding = new Padding(3);
        drawArea = new DrawArea(b);
        drawArea.Dock = DockStyle.Fill;
        this.Controls.Add(drawArea);
        drawArea.BringToFront();

        toolBar.ButtonClick += (sender, e) =>
        {
            switch (toolBar.Buttons.IndexOf(e.Button))
            {
                case 0:
                    drawArea.Upload();
                    break;
                case 1:
                    drawArea.Errase();
                    break;
            }
        };

    }
}


public class MyForm : Form
{
    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    private int startX, startY, stopX, stopY;
    private int x, y, r, d;
    private bool select = false;

    public MyForm()
    {
        this.Cursor = System.Windows.Forms.Cursors.Cross;

        this.BackColor = Color.Orange;
        this.TransparencyKey = Color.Orange;

        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;

        x = y = r = d = 0;
        foreach (Screen screen in Screen.AllScreens)
        {
            if (y > screen.WorkingArea.Top) y = screen.WorkingArea.Top;
            if (x > screen.WorkingArea.Left) x = screen.WorkingArea.Left;
            if (d < (screen.WorkingArea.Top + screen.WorkingArea.Height)) d = screen.WorkingArea.Top + screen.WorkingArea.Height;
            if (r < (screen.WorkingArea.Left + screen.WorkingArea.Width)) r = screen.WorkingArea.Left + screen.WorkingArea.Width;
        }

        MoveWindow(this.Handle, x, y, r - x, d - y, false);
        //       this.WindowState = FormWindowState.Maximized;
    }


    private float getScalingFactor()
    {
        Graphics g = Graphics.FromHwnd(IntPtr.Zero);
        IntPtr desktop = g.GetHdc();
        int LogicalScreenHeight = GetDeviceCaps(desktop, 10);
        int PhysicalScreenHeight = GetDeviceCaps(desktop, 117);

        float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;

        return ScreenScalingFactor;
    }


    protected override void WndProc(ref System.Windows.Forms.Message m)
    {
        switch (m.Msg)
        {
            case MouseEvents.WM_RBUTTONDOWN:
                //        Console.WriteLine("WM_RBUTTONDOWN");
                this.Close();
                break;
            case MouseEvents.WM_RBUTTONUP:
                //        Console.WriteLine("WM_RBUTTONUP");
                break;

            case MouseEvents.WM_LBUTTONDOWN:
                startX = (m.LParam.ToInt32() & 0xFFFF);
                startY = (m.LParam.ToInt32() >> 16);
                select = true;
                break;
            case MouseEvents.WM_LBUTTONUP:
                if (select)
                {
                    stopX = (m.LParam.ToInt32() & 0xFFFF);
                    stopY = (m.LParam.ToInt32() >> 16);

                    //            Console.WriteLine("WM_LBUTTONUp {0:D} {1:D} {2:D} {3:D}" , startX, startY, stopX, stopY);
                    if (startX != stopX && startY != stopY)
                    {
                        float scale = getScalingFactor();
                        startX = (int)(startX * scale);
                        startY = (int)(startY * scale);
                        stopX = (int)(stopX * scale);
                        stopY = (int)(stopY * scale);
                        if (startX > stopX) { int tmp = stopX; stopX = startX; startX = tmp; }
                        if (startY > stopY) { int tmp = stopY; stopY = startY; startY = tmp; }
                        this.Hide();
                        Bitmap bitmap = new Bitmap(stopX - startX, stopY - startY);
                        Graphics g = Graphics.FromImage(bitmap);
                        g.CopyFromScreen((int)(x * scale) + startX, (int)(y * scale) + startY, 0, 0, new Size(stopX - startX, stopY - startY));
//                        bitmap.Save("test.png");

                        using (Form form = new Editor(bitmap))
                        {
                            form.Text = "ScreenShot Editor";
                            form.ShowDialog();
                        }

                        this.Close();
                    }
                    select = false;
                }
                break;
            case MouseEvents.WM_MOUSEMOVE:
                if (select)
                {
                    stopX = (m.LParam.ToInt32() & 0xFFFF);
                    stopY = (m.LParam.ToInt32() >> 16);
                    //            Console.WriteLine("WM_LBUTTONMove  {0:D} {1:D}" ,  stopX, stopY);
                    this.Invalidate();
                }
                break;

        }

        base.WndProc(ref m);
    }


    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        draw(e.Graphics);
    }

    private void draw(Graphics g)
    {
        Pen myPen = new Pen(Color.Red);
        myPen.Width = 3;
        int X = startX;
        int W = stopX - startX;
        if (startX > stopX)
        {
            X = stopX;
            W = startX - stopX;
        }

        int Y = startY;
        int H = stopY - startY;
        if (startY > stopY)
        {
            Y = stopY;
            H = startY - stopY;
        }
        g.DrawRectangle(myPen, X, Y, W, H);
    }


    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int AllocConsole();

    public static void Main()
    {
        //        AllocConsole();
        //        Console.Clear();
        using (Form form = new MyForm())
        {
            form.Text = "ScreenShoter";
            form.ShowDialog();
        }

    }
}
