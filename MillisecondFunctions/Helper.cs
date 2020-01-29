using System;
using System.Collections.Generic;
using System.Text;
using MillisecondFunctions.Models;
using System.Linq;
using SendGrid;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;


namespace MillisecondFunctions
{
    public static class Helper
    {
        public static Customer FromDTOtoCustomer(DTO input)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in input.Attributes)
            {
                sb.Append(item + ";");
            }

            Customer c = new Customer()
            {
                Key = input.Key,
                Email = input.Email,
                Attributes = sb.ToString()
            };

            return c;
        }

        public static void SendEmailIfTenAttributes(Customer customer, MillisecondTestContext db, ILogger log)
        {
            var v = (from c in db.Customer
                     where c.Email == customer.Email
                     select c).FirstOrDefault();

            string[] attrArray = v.Attributes.Split(';', StringSplitOptions.RemoveEmptyEntries);
            //send email if ten or more attributes
            if (attrArray.Count() >= 10)
            {
                log.LogInformation("customer has 10 or more attributes! Sending an email...");
                StringBuilder sb = new StringBuilder();

                //build the email message body
                sb.Append("<ul>");
                foreach (var item in attrArray)
                {
                    sb.Append("<li>" + item + "</li>");
                }
                sb.Append("</ul>");

                var msg = new SendGridMessage();
                msg.SetFrom(new EmailAddress("urho@testi.fi", "Junior IT consultant"));
                //msg.AddTos(new List<EmailAddress>() { new EmailAddress(customer.Email) });
                msg.AddTo(customer.Email);
                msg.SetSubject("Congratulations!");

                msg.AddContent(MimeType.Text, "your record has 10 attributes:");
                msg.AddContent(MimeType.Html, sb.ToString());

                var apiKey = Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
                var client = new SendGridClient(apiKey);

                log.LogInformation("sending email to recipient");
                var response = client.SendEmailAsync(msg);

            }
        }
    }
}
