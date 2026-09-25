using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeploymentAdditional
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // init text
            string csvpath = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".Resources." + "notes.txt";

            Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(csvpath);
            if (resource == null)
            {
                this.richTextBox1.Text = "an error has occurred - " + csvpath;
                return;
            }

            StreamReader reader = new StreamReader(resource);

            string line;
            while ((line = reader.ReadLine()) != null)
                this.richTextBox1.Text += line + "\n";

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = "mailto:alberto.manganaro@amcc.it";
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }

        }
    }
}
