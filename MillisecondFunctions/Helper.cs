using System;
using System.Collections.Generic;
using System.Text;
using MillisecondFunctions.Models;
using System.Linq;
using SendGrid;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace MillisecondFunctions
{
    public class Helper
    {
        //dependency injection 
        private IConfiguration _configuration;
        public Helper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

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

        public static async void SendEmailIfTenAttributes(Customer customer, MillisecondTestContext db, ILogger log, IConfigurationRoot config)
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


                //var apiKey = System.Environment.GetEnvironmentVariable("sendgrid-apikey");
                //var apiKey = _configuration["sendgrid-apikey"];
                //var apiKey = ConfigurationManager.AppSettings["APPSETTING_sendgrid-apikey"];
                //hardcoded and ugly
                //var apiKey = "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";
                var apiKey = config["sendgrid-apikey"];
                var client = new SendGridClient(apiKey);
                log.LogInformation("api key: " + apiKey);

                var response = await client.SendEmailAsync(msg);
                log.LogInformation("email sent to recipient");

            }
        }
    }
}
