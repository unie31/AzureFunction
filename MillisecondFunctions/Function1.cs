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
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;


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
            //app configuration
            _configuration = new ConfigurationBuilder()
             .SetBasePath(context.FunctionAppDirectory)
             .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true) //if running locally, you need connStrings and appsettings configured in this file
             .AddEnvironmentVariables()
             .Build();

            //blob storage client config
            _containerName = Helper.GenerateDateString();
            _storageAccount = new CloudStorageAccount(new StorageCredentials(_configuration["ConnectionStrings:storageConnection:accountName"], _configuration["ConnectionStrings:storageConnection:accountKey"]), true);
            _blobClient = _storageAccount.CreateCloudBlobClient();
            _container = _blobClient.GetContainerReference(_containerName);
            _container.CreateIfNotExistsAsync().Wait();


            //Function logic
            UpdateDatabase(jsonData, log, _configuration);
            WriteToBlobFile(jsonData, log, _configuration, _blobClient, _container);
            WriteToStorageTable(jsonData, log, _configuration);
        }

        private async void WriteToStorageTable(string jsonData, ILogger log, IConfigurationRoot configuration)
        {
            CloudStorageAccount tableStorageAccount = new CloudStorageAccount(
                new StorageCredentials(configuration["ConnectionStrings:storageConnection:accountName"], configuration["ConnectionStrings:storageConnection:accountKey"]), true);
            
            // Create the table client.
            CloudTableClient tableClient = tableStorageAccount.CreateCloudTableClient();

            // Get a reference to a table named "storagetable"
            CloudTable storagetable = tableClient.GetTableReference("storagetable");
            await storagetable.CreateIfNotExistsAsync();

            //add an entity to the table
            TableOperation insertOperation = TableOperation.Insert(new TableEntity() { PartitionKey = Helper.GenerateDateString(), RowKey = jsonData });

            // Execute the insert operation.
            try
            {
                await storagetable.ExecuteAsync(insertOperation);
                log.LogInformation("inserted json to storage table");

            }
            catch (StorageException)
            {
                log.LogInformation("This entry already exists in the table!");
            }


            //////////////
            ////uncomment below to query the table contents
            //////////////

            //// Construct the query operation for all customer entities where PartitionKey="3012020".
            //TableQuery<TableEntity> query = new TableQuery<TableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "3012020"));
            //TableContinuationToken token = null;
            //do
            //{
            //    TableQuerySegment<TableEntity> resultSegment = await storagetable.ExecuteQuerySegmentedAsync(query, token);
            //    token = resultSegment.ContinuationToken;

            //    foreach (TableEntity entity in resultSegment.Results)
            //    {
            //        Console.WriteLine("{0}, {1}", entity.PartitionKey, entity.RowKey);
            //    }
            //} while (token != null);
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
