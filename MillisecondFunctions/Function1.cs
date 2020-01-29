using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MillisecondFunctions.Models;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace MillisecondFunctions
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([QueueTrigger("queue", Connection = "")]string jsonData, ILogger log, ExecutionContext context)
        {
            //config for sendgrid api key
            var config = new ConfigurationBuilder()
                 .SetBasePath(context.FunctionAppDirectory)
                 .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                 .AddEnvironmentVariables()
                 .Build();

            log.LogInformation($"Received item from queue: {jsonData}");

            MillisecondTestContext db = new MillisecondTestContext();
            DTO dto = JsonConvert.DeserializeObject<DTO>(jsonData);

            Customer customer = Helper.FromDTOtoCustomer(dto);

            var existingCustomer = (from c in db.Customer
                                 where c.Email == customer.Email
                                 select c).FirstOrDefault();

            //if duplicateCheck null, create new record
            if (existingCustomer == null)
            {
                log.LogInformation($"Creating a new record for: {customer.Email}");

                Customer newCustomer = new Customer()
                {
                    Key = customer.Key,
                    Email = customer.Email,
                    Attributes = customer.Attributes
                };

                db.Add(newCustomer);
                db.SaveChanges();
                log.LogInformation($"Record created successfully for: {customer.Email}");

            }

            //if duplicateCheck not null, update current record
            if(existingCustomer != null)
            {
                log.LogInformation($"Updating the record for: {customer.Email}");
                StringBuilder sb = new StringBuilder(existingCustomer.Attributes);
                foreach (var item in dto.Attributes)
                {
                    sb.Append(item + ";");
                }
                existingCustomer.Attributes = sb.ToString();
                db.SaveChanges();
                log.LogInformation("Record updated successfully for: " + customer.Email);
                
                //check if 10 (or more) attributes 
                Helper.SendEmailIfTenAttributes(existingCustomer, db, log, config);

            }


        }
    }
}
