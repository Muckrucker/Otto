using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Otto;

namespace Test_Harness
{
    public partial class Form1 : Form
    {
        private Otto.Otto _otto;

        public Form1()
        {
            InitializeComponent();
            _otto = new Otto.Otto();
        }

        private void btn_Go_Click(object sender, EventArgs e)
        {
            _otto.Initialize(tbx_Url.Text);
        }

        private void btn_Generate_Click(object sender, EventArgs e)
        {
            _otto.Generate(tbx_Classname.Text, Otto.Otto.ClassLanguage.VB);
        }
    }
}
