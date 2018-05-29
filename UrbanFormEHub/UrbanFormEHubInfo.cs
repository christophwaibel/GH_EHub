using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace UrbanFormEHub
{
    public class UrbanFormEHubInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "UrbanFormEHub";
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
                return new Guid("1c624dff-ad36-4ed0-86ad-1115bc666047");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
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
