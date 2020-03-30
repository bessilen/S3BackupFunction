using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace S3BackupFunction
{
    public class Function
    {
        private static readonly AmazonS3Client S3Client = new AmazonS3Client(RegionEndpoint.USEast1);
        private const string BucketName = "titles-backup-bucket";
        
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            foreach(var message in evnt.Records)
            {
                await ProcessMessageAsync(message, context);
            }
        }

        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            context.Logger.LogLine($"Processed message {message.Body}");

            var title = JsonConvert.DeserializeObject<Message>(message.Body);

            switch (title.EventType)
            {
                case "PUT":
                {
                    var stream = 
                    await S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        Key = $"{title.Payload.Isbn}.json",
                        BucketName = BucketName,
                        InputStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(title.Payload))),
                    });
                    break;
                }
                case "DELETE":
                {
                    await S3Client.DeleteObjectAsync(BucketName, $"{title.Payload.Isbn}.json");
                    break;
                }
            }
        }

        class Message
        {
            public string EventType { get; set; }
            
            public Title Payload { get; set; }
        }

        class Title
        {
            public string Isbn { get; set; }
            
            public string Description { get; set; }
            
            public string Name { get; set; }
        }
    }
}
