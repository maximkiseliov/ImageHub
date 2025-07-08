# ImageHub

A cloud-based image storage and processing service built with .NET.

## Project Overview

ImageHub is a web API for uploading, retrieving, resizing, and managing images. Images are stored in AWS S3, and the application is deployed on AWS Elastic Beanstalk.

## Features

- **Image Upload**: Upload images
- **Image Retrieval**: Get images by ID
- **Image Resizing**: Resize images to specific dimensions
- **Image Deletion**: Remove images from storage
- **Asynchronous Processing**: Background processing of large images using AWS SQS
- **Metadata Storage**: Image metadata storage in DynamoDB

## Technical Stack

- **Backend**: ASP.NET Core
- **Testing**: xUnit for unit and integration tests
- **Cloud Storage**: AWS S3
- **Database**: Amazon DynamoDB for metadata storage
- **Message Queue**: AWS SQS for background processing
- **Deployment**: AWS Elastic Beanstalk
- **Documentation**: Scalar UI

## Architecture

The application uses a distributed architecture:
1. **Web API**: Handles direct requests and queues larger jobs
2. **Worker Service**: Processes queued operations asynchronously
3. **SQS Queue**: Manages the backlog of processing tasks
4. **DynamoDB**: Stores metadata about images and processing status
5. **S3**: Stores the actual image files in various resolutions


## Project Structure

### Source

- **ImageHub.Domain**: Core domain entities and logic
- **ImageHub.Application**: Application services and business logic
- **ImageHub.Infrastructure**: External service integrations (AWS S3, DynamoDB, SQS)
- **ImageHub.Web.Api**: REST API endpoints and controllers
- **ImageHub.Web.Api.Worker**: Background processing jobs for image operations

### Tests

- **ImageHub.Tests.Shared**: Common testing utilities and fixtures
- **ImageHub.Application.Tests**: Tests for application layer
- **ImageHub.Infrastructure.Tests**: Tests for infrastructure layer
- **ImageHub.Web.Api.Tests**: Tests for web API
- **ImageHub.Web.Api.Worker.Tests**: Tests for worker service

## API Endpoints

All endpoints are under the `api/images` base path:

- `POST api/images` - Upload a new image
- `GET api/images/{id}` - Retrieve an image by ID
- `POST api/images/{id}/resize` - Resize an existing image
- `DELETE api/images/{id}` - Delete an image

## Configuration

### AWS

Run `aws configure` to set up your credentials.

## Local Development with Docker

### Prerequisites
- Docker and Docker Compose
- AWS CLI configured with appropriate permissions
- Git repository cloned locally

### Setup Instructions

1. *Create Required AWS Resources*

   Deploy the CloudFormation base template to create the necessary AWS infrastructure:
   ```
   aws cloudformation deploy \
     --template-file deploy/cloudFormation/cloudFormationBase.yml \
     --stack-name imagehub-local \
     --parameter-overrides AppName=imagehub-local
   ```
2. *Configure Environment Variables*

   After deployment, retrieve resource information:
   ```
   aws cloudformation describe-stacks --stack-name imagehub-local --query "Stacks[0].Outputs"
   ```
   
   Create or update the .env file in the project root with the following values (replace with your AWS resource details):
   ```
   ASPNETCORE_ENVIRONMENT=Development
   AWS_ACCESS_KEY_ID=your_access_key_id
   AWS_SECRET_ACCESS_KEY=your_secret_access_key
   AWS_DEFAULT_REGION=your_region
   S3_BUCKET_NAME=your-bucket-name
   SQS_RESIZE_QUEUE_URL=your_sqs_queue_url
   ```
   
3. *Build and Run Docker Containers*

   Start the application services:
   `docker-compose up --build`
   
   This launches:
   - Web API at http://localhost:5001
   - Worker service at http://localhost:5002

4. *Stopping and Cleanup*

   To stop the containers:
   `docker-compose down`

5. *Remove AWS resources when finished:*
   `aws cloudformation delete-stack --stack-name imagehub-local`

## Deployment

The project is deployed using CloudFormation templates located in the `deploy/cloudFormation` directory:
- `cloudFormationBase.yml` - Base infrastructure setup (S3, DynamoDB, SQS)
- `cloudFormationEb.yml` - Elastic Beanstalk environment configuration

## CI/CD Pipeline

The project uses GitHub Actions for continuous integration and deployment:
- `.github/workflows/build-and-test.yml` - Builds and tests the application
- `.github/workflows/deploy-to-aws.yml` - Deploys the application to AWS

## Logging
### Production Logs
When running in AWS logs are written to Elastic Beanstalk log streams and can be accessed via Elastic Beanstalk Console under `Logs` section

### Local Development Logs
When running locally with Docker or running service directly, logs are written to the console and written to file which can be found in the `/logs` directory of the project root