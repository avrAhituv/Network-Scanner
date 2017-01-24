using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

public class MessageGmail
{
    //חשוב להקפיד שהערכים יהיו כך      
    //key=שם הנמען,value=כתובת המייל שלו
    public Dictionary<string, string> ToList { get; set; } = new Dictionary<string, string>();
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsBodyHtml { get; set; } = false;
    public List<Attachment> ListFileAttachment { get; set; } = new List<Attachment>();
}

public static class SendMail
{  
    private static string senderName = ConfigurationManager.AppSettings["From"].ToString();
    private static string senderEmailId = ConfigurationManager.AppSettings["SMTPUserName"].ToString();
    private static string password = ConfigurationManager.AppSettings["SMTPPasssword"].ToString();
    private static MailAddress fromAddress = new MailAddress(senderEmailId, senderName,Encoding.UTF8);

    public static bool SendEMail(MessageGmail message)
    {       
        var success = false;
        var msg = createMessage(message);
        msg = addFilesToMessage(message, msg);

        var client = createClient();
        try
        {           
            client.Send(msg);
            success = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.ReadLine();
            //להוסיף דיוח על שגיאה אם רוצים
        }
        return success;
    }

    private static MailMessage createMessage(MessageGmail message)
    {
        MailMessage msg = new MailMessage()
        {
            From = fromAddress,
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsBodyHtml,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };
        //מצרף את כתובות המייל לשליחה
        foreach (var address in message.ToList)
        {
            var ToAddress = new MailAddress(address.Value, address.Key);
            msg.To.Add(ToAddress);
        }
        return msg;
    }

    private static SmtpClient createClient()
    {
        SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
        client.EnableSsl = true;
        client.UseDefaultCredentials = false;

        client.DeliveryFormat = SmtpDeliveryFormat.International;
        client.Credentials = new NetworkCredential(fromAddress.Address, password);
        return client;
    }

    private static MailMessage addFilesToMessage(MessageGmail originalMessage, MailMessage msg)
    {
        if (originalMessage.ListFileAttachment != null)
        {
            foreach (var file in originalMessage.ListFileAttachment)
            {
                msg.Attachments.Add(file);
            }
        }
        return msg;
    }
}