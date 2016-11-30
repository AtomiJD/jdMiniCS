using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace jdMiniCS { 
    public class jdMiniCS {

        [STAThread]
        static void Main()
        {
           jdMiniCS p = new jdMiniCS();
           p.main();
        }


        public string main()
        {
            //Please Code after here!
	    MessageBox.Show("Hallo jd");
            return "OK";
        }
    }
}
