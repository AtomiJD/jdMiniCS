using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace jdMiniCS { 
    public class jdMiniCS {

        public string main()
        {
            //Please Code after here!
            for (int i = 0; i < 100; i++)
                Console.WriteLine("Nr.: " + i.ToString());
            MessageBox.Show("Hello jd");
            return "OK";
        }
    }
}
