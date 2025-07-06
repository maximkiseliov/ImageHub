# ImageHub

A cloud-based image storage and processing service built with .NET.

## Project Overview

ImageHub is a web API for uploading, retrieving, resizing, and managing images. Images are stored in AWS S3 and the application is deployed on AWS Elastic Beanstalk.

## Features

- **Image Upload**: Upload images with support for large files (up to 300MB)
- **Image Retrieval**: Get images by ID
- **Image Resizing**: Resize images to specific dimensions
- **Image Deletion**: Remove images from storage
- **Asynchronous Processing**: Background processing of large images using AWS SQS
- **Metadata Storage**: Image metadata storage in DynamoDB

## Project Structure

- **ImageHub.Domain**: Core domain entities and logic
- **ImageHub.Application**: Application services and business logic
- **ImageHub.Infrastructure**: External service integrations (AWS S3, DynamoDB, SQS)
- **ImageHub.Web.Api**: REST API endpoints and controllers
- **ImageHub.Web.Api.Worker**: Background processing jobs for image operations

## Technical Stack

- **Backend**: ASP.NET Core
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

## API Endpoints

All endpoints are under the `api/images` base path:

- `POST api/images` - Upload a new image
- `GET api/images/{id}` - Retrieve an image by ID
- `POST api/images/{id}/resize` - Resize an existing image
- `DELETE api/images/{id}` - Delete an image

## Configuration

### File Upload Limits

The application supports file uploads up to 300MB

### Logging

All application logs are written to Elastic Beanstalk log streams and can be accessed via:
- CloudWatch Logs in the `/aws/elasticbeanstalk/<environment-name>/var/log/web.stdout.log` log group

## Deployment

The project is deployed using CloudFormation templates located in the `deploy/cloudFormation` directory:
- `cloudFormationBase.yml` - Base infrastructure setup (S3, DynamoDB, SQS)
- `cloudFormationEb.yml` - Elastic Beanstalk environment configuration

CI/CD is handled through GitHub Actions defined in `.github/workflows/build-and-deploy.yml`.