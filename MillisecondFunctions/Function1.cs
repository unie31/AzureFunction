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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;

namespace MillisecondFunctions
{

    //was static
    public class Function1
    {
        //blob storage client fields
        private IConfigurationRoot _configuration;
        private CloudStorageAccount _storageAccount;
        private CloudBlobClient _blobClient;
        private CloudBlobContainer _container;
        private string _containerName = "containerName"; //gets a date based value in Run


        [FunctionName("Function1")]
        public void Run([QueueTrigger("queue", Connection = "")]string jsonData, ILogger log, ExecutionContext context)
        {
            _configuration = new ConfigurationBuilder()
             .SetBasePath(context.FunctionAppDirectory)
             .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true) //if running locally, you need connStrings and appsettings configured in this file
             .AddEnvironmentVariables()
             .Build();

            //blob storage client config
            _containerName = Helper.GenerateBlobFileName();

            _storageAccount = new CloudStorageAccount(new StorageCredentials(_configuration["ConnectionStrings:storageConnection:accountName"], _configuration["ConnectionStrings:storageConnection:accountKey"]), true);
            _blobClient = _storageAccount.CreateCloudBlobClient();
            _container = _blobClient.GetContainerReference(_containerName);
            _container.CreateIfNotExistsAsync().Wait();

            UpdateDatabase(jsonData, log, _configuration);
            WriteToBlobFile(jsonData, log, _configuration, _blobClient, _container);
        }

        private async static void WriteToBlobFile(string jsonData, ILogger log, IConfigurationRoot config, CloudBlobClient blobClient, CloudBlobContainer container)
        {
            //hash email
            DTO dto = JsonConvert.DeserializeObject<DTO>(jsonData);
            dto.Email = Helper.HashEmail(dto.Email);

            string hashedJson = JsonConvert.SerializeObject(dto);

            //new file for every day: date based filename
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(Guid.NewGuid().ToString() + ".json");
            await blockBlob.UploadTextAsync(hashedJson);
            log.LogInformation("created a blob in container: " + container.Name);
        }

        private static void UpdateDatabase(string jsonData, ILogger log, IConfigurationRoot config)
        {
            log.LogInformation($"Received item from queue: {jsonData}");

            MillisecondTestContext db = new MillisecondTestContext(config);
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
            if (existingCustomer != null)
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
