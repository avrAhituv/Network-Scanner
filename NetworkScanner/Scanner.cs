using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;

namespace NetworkScanner
{
    public class Scanner
    {  
        private string MacDevice { get; set; } 
        private int MinimunTimeConnection { get; set; }
        private int MinimumTimeForTwoConnection { get; set; } 
        public bool IsDeviceConnection { get; set; } = false;
        public bool IsSendMail { get; set; } = false;
        public bool IsSendMailSummary { get; set; } = false;
        private string MailAddress { get; set; }
        private string ContactName { get; set; }      
        private DateTime timeStart { get; set; }
        private DateTime timeStop { get; set; }
        private List<DeviceInNetwork> listAllDevice { get; set; } = new List<DeviceInNetwork>();
        public List<DataTableOfConnection> listAllConnection { get; set; } = new List<DataTableOfConnection>();
        private Message MessageForInitalConnection { get; set; } 
        private Message MessageForEndConnection { get; set; }
        private Message MessageForTwoConsecutiveConnection { get; set; }

        private Timer timer;
        Timer timerForFindMac;
        TimeSpan difference;

        public Scanner(DateTime start, DateTime stop,string MacAddressForDevice,string mailAddress,string contactName,
            Message messageForInitalConnection,Message messageForEndConnection,Message messageForTwoConsecutiveConnection,
            int minimunTimeConnection=25,int minimumTimeForTwoConnection=30)
        {
            timeStart = start;
            timeStop = stop;
            MacDevice = MacAddressForDevice;
            MailAddress = mailAddress;
            ContactName = contactName;
            MinimunTimeConnection = minimunTimeConnection;
            MinimumTimeForTwoConnection = minimumTimeForTwoConnection;
            MessageForInitalConnection = messageForInitalConnection;
            MessageForEndConnection = messageForEndConnection;
            MessageForTwoConsecutiveConnection = messageForTwoConsecutiveConnection;

            TimeTracking();          
        }
        public void TimeTracking()
        {
            WriteToLog(" TimeTrackint start ");
          
            DateTime now = DateTime.Now;
            
            //אם עדיין לא הגיע הזמן
            if (now < timeStart)
            {
                timer = new Timer();
                timer.Enabled = true;
                timer.AutoReset = true;
                difference = timeStart - now;
                timer.Interval = difference.TotalMilliseconds;
                timer.Elapsed += (s, e) => { StartUpScanner(); TimeTracking(); };
                timer.Start();
            }
            else if (now >= timeStart && now < timeStop)
            {
                if (timer != null)
                    timer.Stop();
                else //זה אומר שזה הריצה הראשונה של התכנית ולכן צריך להפעיל את פונקציית הסריקה
                    StartUpScanner();
                timer = new Timer();
                timer.Enabled = true;
                timer.AutoReset = true;
                difference = timeStop - now;
                timer.Interval = difference.TotalMilliseconds;
                timer.Elapsed += (s, e) => { StopScanner(); TimeTracking(); };
                timer.Start();
            }
            else if (now >= timeStop)
            {
                if (timer != null)
                    timer.Stop();               
                timer = new Timer();
                timer.Enabled = true;
                timer.AutoReset = true;
                //מכוון את הטיימר שיפעל למחרת
                difference = timeStart.AddDays(1) - now;
                timer.Interval = difference.TotalMilliseconds;
                timer.Elapsed += (s, e) => { StartUpScanner(); TimeTracking(); };
                timer.Start();
                WriteToLog(" Update time in timer ");
            }
        }

        private void StopScanner()
        {
            WriteToLog(" Scanner Stop");
         
            timerForFindMac.Stop();
        }

        private void StartUpScanner()
        {
            WriteToLog(" StartUpScanner start ");

            ScanNetwork();

            timerForFindMac = new Timer();
            timerForFindMac.Enabled = true;
            timerForFindMac.AutoReset = true;
            timerForFindMac.Interval += 120000;
            timerForFindMac.Elapsed += (s, h) => ScanNetwork();
            timerForFindMac.Start();
        }

        private async void ScanNetwork()
        {
            WriteToLog(" ScanNetwork start ");
           
            var list = await GetMacInNetwork.ScanNetwork();           
            listAllDevice = list;
            ChekIfDeviceConnected();
        }

        private void ChekIfDeviceConnected()
        {           
            var device = listAllDevice.Find(d => d.MacAddres == MacDevice);
            if (device != null)
            {
                if (!IsDeviceConnection)
                {
                    IsDeviceConnection = true;
                    IsSendMailSummary = false;
                    WriteToLog(" DeviceConnection start ");
                  
                    var devInfoConnection = new DataTableOfConnection()
                    {
                        dateTimeStart = DateTime.Now
                    };
                    listAllConnection.Add(devInfoConnection);
                }
                else
                {
                    WriteToLog("DeviceConnection");
                  
                    //בדיקה האם הוא מחובר יותר מעשרים וחמש דקות
                    if ((DateTime.Now - listAllConnection.Last().dateTimeStart).Minutes > MinimunTimeConnection && !IsSendMail)
                    {
                        string subject = MessageForInitalConnection.Subject;
                        string message = string.Format(MessageForInitalConnection.Body, listAllConnection.Last().dateTimeStart,MinimunTimeConnection);
                        sendMail(message,subject);
                        IsSendMail = true;
                    }
                }
            }
            else
            {
                if (IsDeviceConnection && !IsSendMailSummary)
                {
                    WriteToLog(" DeviceConnection stop ");                   

                    IsDeviceConnection = false;
                    IsSendMail = false;
                    var lastConnect = listAllConnection.Last();
                    lastConnect.dateTimeStop = DateTime.Now;
                    lastConnect.LengthTime = lastConnect.dateTimeStop - lastConnect.dateTimeStart;
                    var beforeTheLast = listAllConnection.Where(d => d.Id == lastConnect.Id - 1).FirstOrDefault();
                    //אם משך החיבור מעל זמן מסוים - שלח מייל
                    if (lastConnect.LengthTime.TotalMinutes > MinimunTimeConnection)
                    {
                        string subject = MessageForEndConnection.Subject;
                        string message = string.Format(MessageForEndConnection.Body , lastConnect.dateTimeStart, lastConnect.dateTimeStop,lastConnect.LengthTime.TotalMinutes);
                        sendMail(message,subject);
                        IsSendMailSummary = true;
                    }
                    //אם שני החיבורים האחרונים יחד הם מעל זמן מסוים וההפרש בין סיומו של הראשון לתחילתו של האחרון הוא פחות מ10 דקות - גם כן שולח מייל
                    else if ((lastConnect.LengthTime.TotalMinutes + beforeTheLast?.LengthTime.TotalMinutes > MinimunTimeConnection)
                        && ((lastConnect.dateTimeStart - beforeTheLast?.dateTimeStop).Value.TotalMinutes < MinimumTimeForTwoConnection))
                    {
                        string subject = MessageForTwoConsecutiveConnection.Subject;
                        string message = string.Format(MessageForTwoConsecutiveConnection.Body, beforeTheLast.dateTimeStart, beforeTheLast.dateTimeStop, (lastConnect.dateTimeStop - beforeTheLast.LengthTime).Minute, lastConnect.dateTimeStart, lastConnect.dateTimeStop);
                        sendMail(message, subject);
                        IsSendMailSummary = true;
                    }
                }
            }
        }        

        public bool sendMail(string messageValue,string subject)
        {
            MessageGmail message = new MessageGmail();
            message.ToList.Add(ContactName, MailAddress);          
            message.Subject = subject;
            message.Body = messageValue;            
            
           try
            {         
            var success = SendMail.SendEMail(message);
                if (success)
                {
                    WriteToLog(" send mail ");
                }
                return success;
            }
            catch(Exception ex)
            {
                WriteToLog(ex.Message);
            }
            return false;
        }

        public static void WriteToLog(string message)
        {
            StreamWriter sw = null;
            string myDocument = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            
            string dateLog = DateTime.Today.ToString("dd-MM-yyyy");
            try
            {
                sw = new StreamWriter(myDocument + "\\appLog"+dateLog+".txt",true);
                
                sw.WriteLine(DateTime.Now.ToString() + " : " + message);
                sw.Flush();
                sw.Close();
            }
            catch
            {

            }
        }       

    }
    public class DataOfDevice
    {
        static private int id = 1;
        public int Id { get; set; }
        public DateTime dateTimeStart { get; set; }
        public DataOfDevice()
        {
            dateTimeStart = DateTime.Now;
            Id = id;
            id++;
        }
    }
    public class DataTableOfConnection
    {
        private static int id = 1;
        public int Id { get; set; }
        public DateTime dateTimeStart { get; set; }
        public DateTime dateTimeStop { get; set; }
        public TimeSpan LengthTime { get; set; }
        public DataTableOfConnection()
        {
            Id = id;
            id++;
        }
    }

    public class Message
    {       
        public string Body { get; set; }
        public string Subject { get; set; }
        public bool IsBodyHTML { get; set; } = false;
    }
}
