using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CSharp;


namespace jdMiniCS
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private object Eval(string sExpression)
        {
            CSharpCodeProvider c = new CSharpCodeProvider();
            CompilerParameters cp = new CompilerParameters();

            cp.ReferencedAssemblies.Add("system.dll");

            cp.CompilerOptions = Properties.Settings.Default.CompilerOptionsMem;
            cp.GenerateInMemory = true;

            StringBuilder sb = new StringBuilder("");
            sb.Append("using System;\n");

            sb.Append("namespace CSCodeEvaler{ \n");
            sb.Append("public class CSCodeEvaler{ \n");
            sb.Append("public object EvalCode(){\n");
            sb.Append("return " + sExpression + "; \n");
            sb.Append("} \n");
            sb.Append("} \n");
            sb.Append("}\n");

            CompilerResults cr = c.CompileAssemblyFromSource(cp, sb.ToString());
            if (cr.Errors.Count > 0)
            {
                return   string.Format("Error ({0}) evaluating: {1}",  cr.Errors[0].ErrorText, sExpression);
            }

            System.Reflection.Assembly a = cr.CompiledAssembly;
            object o = a.CreateInstance("CSCodeEvaler.CSCodeEvaler");

            Type t = o.GetType();
            MethodInfo mi = t.GetMethod("EvalCode");

            object s = mi.Invoke(o, null);
            return s;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(Properties.Settings.Default.CSharpTemplate))
            {
                using (StreamReader filText = File.OpenText(Properties.Settings.Default.CSharpTemplate))
                {
                    string strT = "";
                    strT = filText.ReadToEnd();
                    rtfSource.Text = strT;
                }
            }
            this.txtCmd.Select();
        }

        private object jdCompileRun()
        {
            CSharpCodeProvider c = new CSharpCodeProvider();
            CompilerParameters cp = new CompilerParameters();

            string strL = null;
            Boolean blnExit = false;

            StringReader strReader = new StringReader(Properties.Settings.Default.UsingsMem);
            while (!blnExit)
            {
                strL = strReader.ReadLine();
                if (strL != null)
                {
                    cp.ReferencedAssemblies.Add(strL);
                }
                else
                    blnExit = true;
            }

            cp.CompilerOptions = Properties.Settings.Default.CompilerOptionsMem;
            cp.GenerateInMemory = true;

            CompilerResults cr = c.CompileAssemblyFromSource(cp, rtfSource.Text);
            if (cr.Errors.Count > 0)
            {
                StringBuilder errs = new StringBuilder("");

                for (int i=0; i <cr.Errors.Count; i++) {
                    errs.AppendLine(string.Format("Line {0}: Error {1}", cr.Errors[i].Line, cr.Errors[i].ErrorText));
                }

                return  errs.ToString();
            }

            System.Reflection.Assembly a = cr.CompiledAssembly;
            object o = a.CreateInstance("jdMiniCS.jdMiniCS");

            Type t = o.GetType();
            MethodInfo mi = t.GetMethod("main");

            object s = mi.Invoke(o, null);
            return s;
        }

        private object jdCompileExe()
        {
            CSharpCodeProvider c = new CSharpCodeProvider();
            CompilerParameters cp = new CompilerParameters();

            string strL = null;
            Boolean blnExit = false;

            StringReader strReader = new StringReader(Properties.Settings.Default.UsingsExe);
            while (!blnExit)
            {
                strL = strReader.ReadLine();
                if (strL != null)
                {
                    cp.ReferencedAssemblies.Add(strL);
                } else
                    blnExit = true;
            }

            cp.GenerateExecutable = true;
            cp.OutputAssembly = "output.exe";
            cp.CompilerOptions = Properties.Settings.Default.CompilerOptionsExe;
            cp.GenerateInMemory = false;
            
            cp.TempFiles = new TempFileCollection(".", false);

            if (c.Supports(GeneratorSupport.EntryPointMethod))
            {
                cp.MainClass = "jdMiniCS.jdMiniCS";
            }

            StringBuilder sb = new StringBuilder("");
            sb.Append(rtfSource.Text);

            CompilerResults cr = c.CompileAssemblyFromSource(cp, sb.ToString());
            if (cr.Errors.Count > 0)
            {
                StringBuilder errs = new StringBuilder("");

                for (int i = 0; i < cr.Errors.Count; i++)
                {
                    errs.AppendLine(string.Format("Line {0}: Error {1}", cr.Errors[i].Line, cr.Errors[i].ErrorText));
                }

                return errs.ToString();
            }
            else
                return "OK";
        }

        private void btnCompileRun_Click(object sender, EventArgs e)
        {
            object s;
            s = jdCompileRun();
            txtResult.Text = s.ToString();
            
        }
 
        private void btnEval_Click(object sender, EventArgs e)
        {
            rtfShow.Text = rtfShow.Text + ">" + txtCmd.Text + "\n" + Eval(txtCmd.Text).ToString() + "\n";
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dlgOpen.ShowDialog() == DialogResult.OK)
                if (File.Exists(dlgOpen.FileName))
                {
                    using (StreamReader filText = File.OpenText(dlgOpen.FileName))
                    {
                        string strT = "";
                        strT = filText.ReadToEnd();
                        rtfSource.Text = strT;
                    }
                    this.tabControl1.SelectTab(1);
                }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dlgSave.ShowDialog() == DialogResult.OK)
               File.WriteAllText(dlgSave.FileName,rtfSource.Text);
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            object s;
            s = jdCompileExe();
            txtResult.Text = s.ToString();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmAbout f = new frmAbout();
            f.Show();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                this.txtCmd.Select();
            }
        }

        private void frmMain_Paint(object sender, PaintEventArgs e)
        {
            // Lall

        }

        private void rtfSource_Click(object sender, EventArgs e)
        {
            ShowXY();
        }

        private void ShowXY()
        {
            int cp = rtfSource.SelectionStart;
            this.ttPos.Text = "Line: " + (rtfSource.GetLineFromCharIndex(cp) + 1);
        }

        private void rtfSource_KeyUp(object sender, KeyEventArgs e)
        {
            ShowXY();
        }
    }
}
