using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            //Setting your app 
            //This is an Example
            Scanner scanner = new Scanner(new DateTime(2017, 1, 24, 22, 0, 0), new DateTime(2017, 1, 25, 10, 0, 0),
                "00:00:av:ad:dd", "mail@gmail.com", "israel israeli", new Message() { Subject = "Alert for connecting over 25 minutes", Body = "Your device connect to network in {0}. \n Since it is connected to more than {1} minutes" },
                new Message() { Subject = "Summary of connection time", Body = "Your Device connected in {0}\n Log out in {1}\n Total time is {2}" },
                new Message() { Subject = "", Body = "" });

            Application.Run();
                
        }
    }
}
