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
using FastColoredTextBoxNS;
using System.Threading;


namespace jdMiniCS
{
    public partial class frmMain : Form
    {
        string[] keywords = { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "add", "alias", "ascending", "descending", "dynamic", "from", "get", "global", "group", "into", "join", "let", "orderby", "partial", "remove", "select", "set", "value", "var", "where", "yield" };
        string[] methods = { "Equals()", "GetHashCode()", "GetType()", "ToString()" };
        string[] snippets = { "if(^)\n{\n;\n}", "if(^)\n{\n;\n}\nelse\n{\n;\n}", "for(^;;)\n{\n;\n}", "while(^)\n{\n;\n}", "do\n{\n^;\n}while();", "switch(^)\n{\ncase : break;\n}" };
        string[] declarationSnippets = { 
               "public class ^\n{\n}", "private class ^\n{\n}", "internal class ^\n{\n}",
               "public struct ^\n{\n;\n}", "private struct ^\n{\n;\n}", "internal struct ^\n{\n;\n}",
               "public void ^()\n{\n;\n}", "private void ^()\n{\n;\n}", "internal void ^()\n{\n;\n}", "protected void ^()\n{\n;\n}",
               "public ^{ get; set; }", "private ^{ get; set; }", "internal ^{ get; set; }", "protected ^{ get; set; }"
               };
        Style invisibleCharsStyle = new InvisibleCharsRenderer(Pens.Gray);
        Color currentLineColor = Color.FromArgb(100, 210, 210, 255);
        Color changedLineColor = Color.FromArgb(255, 230, 230, 255);

        AutocompleteMenu popupMenu;

        private Style sameWordsStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(50, Color.Gray)));

        DateTime lastNavigatedDateTime = DateTime.Now;

        string currentFileName = "";

        public frmMain()
        {
            InitializeComponent();

            popupMenu = new AutocompleteMenu(fctb);
            popupMenu.ForeColor = Color.White;
            popupMenu.BackColor = Color.Gray;
            popupMenu.SelectedColor = Color.Purple;
            popupMenu.SearchPattern = @"[\w\.]";
            popupMenu.AllowTabKey = true;
            popupMenu.Items.SetAutocompleteItems(new DynamicCollection(popupMenu, fctb));

            Console.SetOut(new ControlWriter(txtResult));
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

            object s;
            try
            {
                s = mi.Invoke(o, null);
            }
            catch (Exception ex)
            {
                s = ex.InnerException;
            }

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
                    fctb.Text = strT;
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

            CompilerResults cr = c.CompileAssemblyFromSource(cp, fctb.Text);
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
            object s;
            try
            {
                s = mi.Invoke(o, null);
            }
            catch (Exception ex)
            {
                s = ex.InnerException;
            }

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
            sb.Append(fctb.Text);

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
            txtResult.Text += s.ToString();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            btnCompileRun_Click(sender, e);
        }
 
        private void btnEval_Click(object sender, EventArgs e)
        {
            object s;
            s = Eval(txtCmd.Text);
            if (s==null)
                rtfShow.Text = rtfShow.Text + ">" + txtCmd.Text + "\n" + "null\n";
            else
                rtfShow.Text = rtfShow.Text + ">" + txtCmd.Text + "\n" +s.ToString() + "\n";
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
                        fctb.Text = strT;
                    }
                    this.tabControl1.SelectTab(1);
                    currentFileName = dlgOpen.FileName;
                }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            if (dlgSave.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dlgSave.FileName, fctb.Text);
                currentFileName = dlgSave.FileName;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentFileName == "")
            {
                if (dlgSave.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dlgSave.FileName, fctb.Text);
                    currentFileName = dlgSave.FileName;
                }
            }
            else
            {
                File.WriteAllText(currentFileName, fctb.Text);
                ttPos.Text = "File saved.";
            }
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

        private void BuildAutocompleteMenu(AutocompleteMenu popupMenu)
        {
            List<AutocompleteItem> items = new List<AutocompleteItem>();

            foreach (var item in snippets)
                items.Add(new SnippetAutocompleteItem(item) { ImageIndex = 1 });
            foreach (var item in declarationSnippets)
                items.Add(new DeclarationSnippet(item) { ImageIndex = 0 });
            foreach (var item in methods)
                items.Add(new MethodAutocompleteItem(item) { ImageIndex = 2 });
            foreach (var item in keywords)
                items.Add(new AutocompleteItem(item));

            items.Add(new InsertSpaceSnippet());
            items.Add(new InsertSpaceSnippet(@"^(\w+)([=<>!:]+)(\w+)$"));
            items.Add(new InsertEnterSnippet());

            //set as autocomplete source
            popupMenu.Items.SetAutocompleteItems(items);
            popupMenu.SearchPattern = @"[\w\.:=!<>]";
        }

        internal class DynamicCollection : IEnumerable<AutocompleteItem>
        {
            private AutocompleteMenu menu;
            private FastColoredTextBox tb;

            public DynamicCollection(AutocompleteMenu menu, FastColoredTextBox tb)
            {
                this.menu = menu;
                this.tb = tb;
            }

            public IEnumerator<AutocompleteItem> GetEnumerator()
            {
                //get current fragment of the text
                var text = menu.Fragment.Text;

                //extract class name (part before dot)
                var parts = text.Split('.');
                if (parts.Length < 2)
                    yield break;
                var className = parts[parts.Length - 2];

                //find type for given className
                var type = FindTypeByName(className);

                if (type == null)
                    yield break;

                //return static methods of the class
                foreach (var methodName in type.GetMethods().AsEnumerable().Select(mi => mi.Name).Distinct())
                    yield return new MethodAutocompleteItem(methodName + "()")
                    {
                        ToolTipTitle = methodName,
                        ToolTipText = "Description of method " + methodName + " goes here.",
                    };

                //return static properties of the class
                foreach (var pi in type.GetProperties())
                    yield return new MethodAutocompleteItem(pi.Name)
                    {
                        ToolTipTitle = pi.Name,
                        ToolTipText = "Description of property " + pi.Name + " goes here.",
                    };
            }

            Type FindTypeByName(string name)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Type type = null;
                foreach (var a in assemblies)
                {
                    foreach (var t in a.GetTypes())
                        if (t.Name == name)
                        {
                            return t;
                        }
                }

                return null;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// This item appears when any part of snippet text is typed
        /// </summary>
        class DeclarationSnippet : SnippetAutocompleteItem
        {
            public DeclarationSnippet(string snippet)
                : base(snippet)
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var pattern = Regex.Escape(fragmentText);
                if (Regex.IsMatch(Text, "\\b" + pattern, RegexOptions.IgnoreCase))
                    return CompareResult.Visible;
                return CompareResult.Hidden;
            }
        }

        /// <summary>
        /// Divides numbers and words: "123AND456" -> "123 AND 456"
        /// Or "i=2" -> "i = 2"
        /// </summary>
        class InsertSpaceSnippet : AutocompleteItem
        {
            string pattern;

            public InsertSpaceSnippet(string pattern)
                : base("")
            {
                this.pattern = pattern;
            }

            public InsertSpaceSnippet()
                : this(@"^(\d+)([a-zA-Z_]+)(\d*)$")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                if (Regex.IsMatch(fragmentText, pattern))
                {
                    Text = InsertSpaces(fragmentText);
                    if (Text != fragmentText)
                        return CompareResult.Visible;
                }
                return CompareResult.Hidden;
            }

            public string InsertSpaces(string fragment)
            {
                var m = Regex.Match(fragment, pattern);
                if (m == null)
                    return fragment;
                if (m.Groups[1].Value == "" && m.Groups[3].Value == "")
                    return fragment;
                return (m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value).Trim();
            }

            public override string ToolTipTitle
            {
                get
                {
                    return Text;
                }
            }
        }

        /// <summary>
        /// Inerts line break after '}'
        /// </summary>
        class InsertEnterSnippet : AutocompleteItem
        {
            Place enterPlace = Place.Empty;

            public InsertEnterSnippet()
                : base("[Line break]")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var r = Parent.Fragment.Clone();
                while (r.Start.iChar > 0)
                {
                    if (r.CharBeforeStart == '}')
                    {
                        enterPlace = r.Start;
                        return CompareResult.Visible;
                    }

                    r.GoLeftThroughFolded();
                }

                return CompareResult.Hidden;
            }

            public override string GetTextForReplace()
            {
                //extend range
                Range r = Parent.Fragment;
                Place end = r.End;
                r.Start = enterPlace;
                r.End = r.End;
                //insert line break
                return Environment.NewLine + r.Text;
            }

            public override void OnSelected(AutocompleteMenu popupMenu, SelectedEventArgs e)
            {
                base.OnSelected(popupMenu, e);
                if (Parent.Fragment.tb.AutoIndent)
                    Parent.Fragment.tb.DoAutoIndent();
            }

            public override string ToolTipTitle
            {
                get
                {
                    return "Insert line break after '}'";
                }
            }
        }

        private void fctb_SelectionChangedDelayed(object sender, EventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            //remember last visit time
            if (tb.Selection.IsEmpty && tb.Selection.Start.iLine < tb.LinesCount)
            {
                if (lastNavigatedDateTime != tb[tb.Selection.Start.iLine].LastVisit)
                {
                    tb[tb.Selection.Start.iLine].LastVisit = DateTime.Now;
                    lastNavigatedDateTime = tb[tb.Selection.Start.iLine].LastVisit;
                }
            }

            //highlight same words
            tb.VisibleRange.ClearStyle(sameWordsStyle);
            if (!tb.Selection.IsEmpty)
                return;//user selected diapason
            //get fragment around caret
            var fragment = tb.Selection.GetFragment(@"\w");
            string text = fragment.Text;
            if (text.Length == 0)
                return;
            //highlight same words
            Range[] ranges = tb.VisibleRange.GetRanges("\\b" + text + "\\b").ToArray();

            if (ranges.Length > 1)
                foreach (var r in ranges)
                    r.SetStyle(sameWordsStyle);
        }

        private void fctb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            FastColoredTextBox tb = (sender as FastColoredTextBox);
            //rebuild object explorer
            string text = (sender as FastColoredTextBox).Text;
            ThreadPool.QueueUserWorkItem(
                (o) => ReBuildObjectExplorer(text)
            );

            //show invisible chars
            //JDTODO: HighlightInvisibleChars(e.ChangedRange);
        }

        //private void HighlightInvisibleChars(Range range)
        //{
        //    range.ClearStyle(invisibleCharsStyle);
        //    if (btInvisibleChars.Checked)
        //        range.SetStyle(invisibleCharsStyle, @".$|.\r\n|\s");
        //}

        List<ExplorerItem> explorerList = new List<ExplorerItem>();

        private void ReBuildObjectExplorer(string text)
        {
            try
            {
                List<ExplorerItem> list = new List<ExplorerItem>();
                int lastClassIndex = -1;
                //find classes, methods and properties
                Regex regex = new Regex(@"^(?<range>[\w\s]+\b(class|struct|enum|interface)\s+[\w<>,\s]+)|^\s*(public|private|internal|protected)[^\n]+(\n?\s*{|;)?", RegexOptions.Multiline);
                foreach (Match r in regex.Matches(text))
                    try
                    {
                        string s = r.Value;
                        int i = s.IndexOfAny(new char[] { '=', '{', ';' });
                        if (i >= 0)
                            s = s.Substring(0, i);
                        s = s.Trim();

                        var item = new ExplorerItem() { title = s, position = r.Index };
                        if (Regex.IsMatch(item.title, @"\b(class|struct|enum|interface)\b"))
                        {
                            item.title = item.title.Substring(item.title.LastIndexOf(' ')).Trim();
                            item.type = ExplorerItemType.Class;
                            list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), new ExplorerItemComparer());
                            lastClassIndex = list.Count;
                        }
                        else
                            if (item.title.Contains(" event "))
                            {
                                int ii = item.title.LastIndexOf(' ');
                                item.title = item.title.Substring(ii).Trim();
                                item.type = ExplorerItemType.Event;
                            }
                            else
                                if (item.title.Contains("("))
                                {
                                    var parts = item.title.Split('(');
                                    item.title = parts[0].Substring(parts[0].LastIndexOf(' ')).Trim() + "(" + parts[1];
                                    item.type = ExplorerItemType.Method;
                                }
                                else
                                    if (item.title.EndsWith("]"))
                                    {
                                        var parts = item.title.Split('[');
                                        if (parts.Length < 2) continue;
                                        item.title = parts[0].Substring(parts[0].LastIndexOf(' ')).Trim() + "[" + parts[1];
                                        item.type = ExplorerItemType.Method;
                                    }
                                    else
                                    {
                                        int ii = item.title.LastIndexOf(' ');
                                        item.title = item.title.Substring(ii).Trim();
                                        item.type = ExplorerItemType.Property;
                                    }
                        list.Add(item);
                    }
                    catch { ;}

                list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), new ExplorerItemComparer());

            }
            catch { ;}
        }

        enum ExplorerItemType
        {
            Class, Method, Property, Event
        }

        class ExplorerItem
        {
            public ExplorerItemType type;
            public string title;
            public int position;
        }

        class ExplorerItemComparer : IComparer<ExplorerItem>
        {
            public int Compare(ExplorerItem x, ExplorerItem y)
            {
                return x.title.CompareTo(y.title);
            }
        }

        public class ControlWriter : TextWriter
        {
            private Control textbox;
            public ControlWriter(Control textbox)
            {
                this.textbox = textbox;
            }

            public override void Write(char value)
            {
                textbox.Text += value;
            }

            public override void Write(string value)
            {
                textbox.Text += value;
            }

            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }


      }

    public class InvisibleCharsRenderer : Style
    {
        Pen pen;

        public InvisibleCharsRenderer(Pen pen)
        {
            this.pen = pen;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            var tb = range.tb;
            using (Brush brush = new SolidBrush(pen.Color))
                foreach (var place in range)
                {
                    switch (tb[place].c)
                    {
                        case ' ':
                            var point = tb.PlaceToPoint(place);
                            point.Offset(tb.CharWidth / 2, tb.CharHeight / 2);
                            gr.DrawLine(pen, point.X, point.Y, point.X + 1, point.Y);
                            break;
                    }

                    if (tb[place.iLine].Count - 1 == place.iChar)
                    {
                        var point = tb.PlaceToPoint(place);
                        point.Offset(tb.CharWidth, 0);
                        gr.DrawString("¶", tb.Font, brush, point);
                    }
                }
        }
    }
}
