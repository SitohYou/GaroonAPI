using GaroonAPI.GaroonService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GaroonAPI
{
    public partial class Form1 : Form
    {
        // キーID
        // 玉井 : 1031
        // 中野 : 1080
        // 齊藤 : 1147
        // 中島 : 1232

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(textBox2.Text.Length < 1 || textBox3.Text.Length < 1) return;
            button1.Enabled = false;
            Mail mail = new Mail();
            mail.OnMessageChanged += ChangeTextBox;
            mail.OnMessageAdd += AddTextBox;
            mail.GetMailInfoCompleted += GetMailInfoEnabled;
            mail.GetMailInfo(textBox2.Text, textBox3.Text, textBox4.Text);
        }

        // テキストボックス上書き
        private void ChangeTextBox(string message)
        {
            textBox1.Text = message;
        }

        // テキストボックス追記
        private void AddTextBox(string message)
        {
            textBox1.Text += "\r\n" + message;
        }

        private void GetMailInfoEnabled()
        {
            button1.Enabled = true;
        }

        private void button2_MouseDown(object sender, MouseEventArgs e)
        {
            textBox3.UseSystemPasswordChar = false;
        }

        private void button2_MouseUp(object sender, MouseEventArgs e)
        {
            textBox3.UseSystemPasswordChar = true;
        }
    }
}