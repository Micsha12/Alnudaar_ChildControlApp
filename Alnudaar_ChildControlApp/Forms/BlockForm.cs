using System;
using System.Drawing;
using System.Windows.Forms;

public class BlockForm : Form
{
    public BlockForm(string nextAllowedTime)
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.BackColor = Color.Black;
        this.ShowInTaskbar = false;

        var label = new Label
        {
            Text = $"Screen time is over.\n\nThe next time you can use your PC is:\n{nextAllowedTime}\n\nYou can ask your parent to change it if you both agree.",
            ForeColor = Color.White,
            BackColor = Color.Black,
            Font = new Font("Arial", 28, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        this.Controls.Add(label);
    }

    // Prevent Alt+F4
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x200; // CS_NOCLOSE
            return cp;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        e.Cancel = true; // Prevent closing
    }
}