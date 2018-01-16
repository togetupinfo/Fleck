using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fleck.MsgServer
{
    public class ImgObj
    {
        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        private int age;

        public int Age
        {
            get { return age; }
            set { age = value; }
        }
        private string imagedata;

        private string processnum;


        public string ImageData
        {
            get { return imagedata; }
            set { imagedata = value; }
        }

        public string ProcessNum { get => processnum; set => processnum = value; }
        public string Modality { get => modality; set => modality = value; }
        public string HospitalName { get => hospitalname; set => hospitalname = value; }

        private string modality;

        private string hospitalname;
    }
}
