using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {



            string path = @"C:\Users\Christoph\Documents\Visual Studio 2012\Projects\GHUrbanMorphologyEHub\UrbanFormEHub\bin\";
            bool carbmin = false;
            bool minpartload = false;
            double carbcon = 10.06;
            //Ehub ehub = new Ehub(path, carbmin, minpartload, carbcon);
            Ehub ehub = new Ehub(path, carbmin, minpartload);
        }
    }
}
