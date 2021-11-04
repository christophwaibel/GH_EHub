using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILOG.CPLEX;
using ILOG.Concert;

namespace ConsoleTest
{
    class Program
    {
        static void mMain(string[] args)
        {



            string path = @"C:\Users\Christoph\Documents\Visual Studio 2012\Projects\GHUrbanMorphologyEHub\UrbanFormEHub\bin\";
            bool carbmin = false;
            bool minpartload = false;
            double gfa = 2000;  // gross floor area of urban design
            double carbcon = 10.06;     //this needs to be kgco2/m2a, using the GFA of the urban form design
            //Ehub ehub = new Ehub(path, gfa, carbmin, minpartload, carbcon);
            Ehub ehub = new Ehub(path, gfa, carbmin, minpartload);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("hi justin");

            Cplex cpl = new Cplex();
            INumVar x = cpl.NumVar(0, 1);
            Console.WriteLine("upper bound: " + Convert.ToString(x.UB));
            Console.ReadKey();
        }
    }
}
