using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace EaCS3EHub
{
    public class EaCS3EHubInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "EaCS3EHub";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("14ec9618-9527-4d94-9569-b794062793ca");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "ETH Zurich";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
