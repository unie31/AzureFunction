using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MillisecondFunctions.Models;
using Newtonsoft.Json;
using System.Linq;

namespace MillisecondFunctions
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([QueueTrigger("queue", Connection = "")]string jsonData, ILogger log)
        {
            log.LogInformation($"Received item from queue: {jsonData}");

            MillisecondTestContext db = new MillisecondTestContext();
            DTO dto= JsonConvert.DeserializeObject<DTO>(jsonData);

            Customer customer = Helper.FromDTOtoCustomer(dto);

            var duplicateCheck = (from c in db.Customer
                                 where c.Email == customer.Email
                                 select c).FirstOrDefault();

            //if duplicateCheck null, create new record
            if (duplicateCheck == null)
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


            //write to db and check email duplicates

            //check if attributes == 10
            //congratulate



        }
    }
}
