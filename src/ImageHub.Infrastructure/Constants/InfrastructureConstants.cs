namespace ImageHub.Infrastructure.Constants;

public static class InfrastructureConstants
{
    public static class Config
    {
        public static class Aws
        {
            public static class S3
            {
                public const string SectionName = "AWS:S3";
            }

            public static class Sqs
            {
                public const string SectionName = "AWS:Sqs";
            }
            
            public static class DynamoDb
            {
                public const string TableNamePrefix = "AWS:DynamoDb:TableNamePrefix";
            }
        }
    }
}