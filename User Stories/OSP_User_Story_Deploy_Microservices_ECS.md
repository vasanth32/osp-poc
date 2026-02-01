# User Story: Deploy .NET Microservices to AWS ECS Fargate

## Overview

**User Story**: As a DevOps Engineer / Developer  
**I want to** deploy dummy .NET microservices to AWS ECS Fargate with Application Load Balancer  
**So that** I can learn the complete deployment process and understand containerized microservices architecture on AWS

## Context

- **Goal**: Learn AWS ECS Fargate deployment process
- **Time**: 2-hour POC
- **Services**: 2-3 dummy .NET microservices (simplified for POC)
- **Infrastructure**: ECS Fargate, ECR, ALB, VPC, Security Groups
- **Method**: Fastest approach (AWS Console + CLI mix)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Internet Users                        │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
         ┌───────────────────────┐
         │  Application Load     │
         │  Balancer (ALB)       │
         │  - HTTPS (443)        │
         │  - HTTP (80) → HTTPS  │
         └───────────┬───────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│  Target Group 1 │    │  Target Group 2 │
│  (Service 1)    │    │  (Service 2)    │
└────────┬────────┘    └────────┬────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│  ECS Service 1  │    │  ECS Service 2  │
│  (Fargate)      │    │  (Fargate)      │
│  - Task 1       │    │  - Task 1       │
│  - Task 2       │    │  - Task 2       │
└────────┬────────┘    └────────┬────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│  ECR Repository │    │  ECR Repository │
│  (Docker Image) │    │  (Docker Image) │
└─────────────────┘    └─────────────────┘
```

---

## Prerequisites

### Required Tools
- ✅ AWS Account with admin access
- ✅ AWS CLI installed and configured (`aws configure`)
- ✅ Docker Desktop installed and running
- ✅ .NET 8.0 SDK installed
- ✅ Git (optional, for version control)

### AWS Services Used
- **ECR** (Elastic Container Registry) - Store Docker images
- **ECS** (Elastic Container Service) - Run containers
- **Fargate** - Serverless compute for containers
- **ALB** (Application Load Balancer) - Route traffic
- **VPC** - Network isolation
- **IAM** - Permissions and roles
- **CloudWatch Logs** - Container logging

---

## Step-by-Step Deployment Guide

### Phase 1: Create Dummy .NET Microservices (30 minutes)

#### Step 1.1: Create First Microservice (Service1)

**Create ASP.NET Core Web API**:

```bash
# Create solution
mkdir OSP-Microservices-POC
cd OSP-Microservices-POC

# Create Service1
dotnet new webapi -n Service1 -f net8.0
cd Service1

# Add health check endpoint
# (We'll modify Program.cs)
```

**Update `Service1/Program.cs`**:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint
app.MapGet("/", () => new { 
    Service = "Service1", 
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow 
});

app.Run();
```

**Update `Service1/Controllers/WeatherForecastController.cs`** (or create new):

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Service1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Service1Controller : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Service = "Service1",
            Message = "Hello from Service1!",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Service1" });
    }
}
```

**Test locally**:
```bash
cd Service1
dotnet run
# Visit https://localhost:5001/api/service1
```

#### Step 1.2: Create Second Microservice (Service2)

```bash
# From solution root
dotnet new webapi -n Service2 -f net8.0
cd Service2

# Copy same Program.cs and Controller structure as Service1
# Just change "Service1" to "Service2" in responses
```

**Quick copy-paste approach**:
- Copy `Service1/Program.cs` to `Service2/Program.cs`
- Copy `Service1/Controllers/Service1Controller.cs` to `Service2/Controllers/Service2Controller.cs`
- Change "Service1" to "Service2" in both files

#### Step 1.3: Create Dockerfiles

**Create `Service1/Dockerfile`**:

```dockerfile
# Stage 1: Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Service1/Service1.csproj", "Service1/"]
RUN dotnet restore "Service1/Service1.csproj"
COPY . .
WORKDIR "/src/Service1"
RUN dotnet build "Service1.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "Service1.csproj" -c Release -o /app/publish

# Stage 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Service1.dll"]
```

**Create `Service2/Dockerfile`** (same structure, change Service1 to Service2)

**Test Docker build locally**:
```bash
cd Service1
docker build -t service1:local .
docker run -p 8080:80 service1:local
# Visit http://localhost:8080/api/service1
```

---

### Phase 2: AWS Infrastructure Setup (40 minutes)

#### Option A: AWS Console (Fastest - Recommended for POC) ⚡

**Step 2.1: Create VPC and Networking (Console)**

1. **VPC**:
   - Go to VPC → Create VPC
   - Name: `osp-poc-vpc`
   - IPv4 CIDR: `10.0.0.0/16`
   - Tenancy: Default
   - Create VPC

2. **Subnets**:
   - Create 2 public subnets (for ALB):
     - `osp-poc-public-subnet-1`: `10.0.1.0/24`, AZ: us-east-1a
     - `osp-poc-public-subnet-2`: `10.0.2.0/24`, AZ: us-east-1b
   - Create 2 private subnets (for ECS):
     - `osp-poc-private-subnet-1`: `10.0.11.0/24`, AZ: us-east-1a
     - `osp-poc-private-subnet-2`: `10.0.12.0/24`, AZ: us-east-1b

3. **Internet Gateway**:
   - Create Internet Gateway: `osp-poc-igw`
   - Attach to VPC

4. **Route Tables**:
   - Public route table: Add route `0.0.0.0/0` → Internet Gateway
   - Associate public subnets with public route table
   - Private route table: Default (no internet gateway)

5. **NAT Gateway** (Optional for POC - skip to save cost):
   - Only needed if ECS tasks need outbound internet
   - For POC, we can skip (tasks won't pull images from internet if using ECR)

**Step 2.2: Create Security Groups (Console)**

1. **ALB Security Group** (`osp-poc-alb-sg`):
   - Inbound: HTTP (80) from `0.0.0.0/0`, HTTPS (443) from `0.0.0.0/0`
   - Outbound: All traffic

2. **ECS Security Group** (`osp-poc-ecs-sg`):
   - Inbound: HTTP (80) from ALB security group
   - Outbound: All traffic

**Step 2.3: Create ECR Repositories (Console)**

1. Go to ECR → Create repository
2. Repository 1:
   - Name: `osp-poc/service1`
   - Tag immutability: Disabled (for POC)
   - Scan on push: Disabled (for POC)
3. Repository 2:
   - Name: `osp-poc/service2`
   - Same settings

**Step 2.4: Create ECS Cluster (Console)**

1. Go to ECS → Clusters → Create cluster
2. Cluster name: `osp-poc-cluster`
3. Infrastructure: AWS Fargate (Serverless)
4. Create cluster

**Step 2.5: Create IAM Roles (Console)**

1. **ECS Task Execution Role** (`osp-poc-ecs-task-execution-role`):
   - Go to IAM → Roles → Create role
   - Trusted entity: ECS Tasks
   - Attach policies:
     - `AmazonECSTaskExecutionRolePolicy` (for pulling images, writing logs)
   - Create role

2. **ECS Task Role** (`osp-poc-ecs-task-role`):
   - Create role
   - Trusted entity: ECS Tasks
   - No policies needed for POC (add later if services need AWS access)
   - Create role

**Step 2.6: Create CloudWatch Log Groups (Console)**

1. Go to CloudWatch → Log groups → Create log group
2. Log group 1: `/ecs/osp-poc-service1`
3. Log group 2: `/ecs/osp-poc-service2`
4. Retention: 7 days (for POC cost savings)

#### Option B: AWS CLI (Faster for Scripting) ⚡⚡

**Complete setup script** (`setup-aws-infrastructure.sh`):

```bash
#!/bin/bash
set -e

REGION="us-east-1"
VPC_NAME="osp-poc-vpc"
CLUSTER_NAME="osp-poc-cluster"

echo "=== Creating VPC ==="
VPC_ID=$(aws ec2 create-vpc \
  --cidr-block 10.0.0.0/16 \
  --region $REGION \
  --query 'Vpc.VpcId' \
  --output text)

aws ec2 create-tags \
  --resources $VPC_ID \
  --tags Key=Name,Value=$VPC_NAME \
  --region $REGION

echo "VPC Created: $VPC_ID"

echo "=== Creating Subnets ==="
PUBLIC_SUBNET_1=$(aws ec2 create-subnet \
  --vpc-id $VPC_ID \
  --cidr-block 10.0.1.0/24 \
  --availability-zone ${REGION}a \
  --region $REGION \
  --query 'Subnet.SubnetId' \
  --output text)

PUBLIC_SUBNET_2=$(aws ec2 create-subnet \
  --vpc-id $VPC_ID \
  --cidr-block 10.0.2.0/24 \
  --availability-zone ${REGION}b \
  --region $REGION \
  --query 'Subnet.SubnetId' \
  --output text)

PRIVATE_SUBNET_1=$(aws ec2 create-subnet \
  --vpc-id $VPC_ID \
  --cidr-block 10.0.11.0/24 \
  --availability-zone ${REGION}a \
  --region $REGION \
  --query 'Subnet.SubnetId' \
  --output text)

PRIVATE_SUBNET_2=$(aws ec2 create-subnet \
  --vpc-id $VPC_ID \
  --cidr-block 10.0.12.0/24 \
  --availability-zone ${REGION}b \
  --region $REGION \
  --query 'Subnet.SubnetId' \
  --output text)

echo "Subnets Created: $PUBLIC_SUBNET_1, $PUBLIC_SUBNET_2, $PRIVATE_SUBNET_1, $PRIVATE_SUBNET_2"

echo "=== Creating Internet Gateway ==="
IGW_ID=$(aws ec2 create-internet-gateway \
  --region $REGION \
  --query 'InternetGateway.InternetGatewayId' \
  --output text)

aws ec2 attach-internet-gateway \
  --internet-gateway-id $IGW_ID \
  --vpc-id $VPC_ID \
  --region $REGION

echo "Internet Gateway Created: $IGW_ID"

echo "=== Creating Route Table ==="
PUBLIC_RT=$(aws ec2 create-route-table \
  --vpc-id $VPC_ID \
  --region $REGION \
  --query 'RouteTable.RouteTableId' \
  --output text)

aws ec2 create-route \
  --route-table-id $PUBLIC_RT \
  --destination-cidr-block 0.0.0.0/0 \
  --gateway-id $IGW_ID \
  --region $REGION

aws ec2 associate-route-table \
  --subnet-id $PUBLIC_SUBNET_1 \
  --route-table-id $PUBLIC_RT \
  --region $REGION

aws ec2 associate-route-table \
  --subnet-id $PUBLIC_SUBNET_2 \
  --route-table-id $PUBLIC_RT \
  --region $REGION

echo "=== Creating Security Groups ==="
ALB_SG=$(aws ec2 create-security-group \
  --group-name osp-poc-alb-sg \
  --description "Security group for ALB" \
  --vpc-id $VPC_ID \
  --region $REGION \
  --query 'GroupId' \
  --output text)

aws ec2 authorize-security-group-ingress \
  --group-id $ALB_SG \
  --protocol tcp \
  --port 80 \
  --cidr 0.0.0.0/0 \
  --region $REGION

aws ec2 authorize-security-group-ingress \
  --group-id $ALB_SG \
  --protocol tcp \
  --port 443 \
  --cidr 0.0.0.0/0 \
  --region $REGION

ECS_SG=$(aws ec2 create-security-group \
  --group-name osp-poc-ecs-sg \
  --description "Security group for ECS tasks" \
  --vpc-id $VPC_ID \
  --region $REGION \
  --query 'GroupId' \
  --output text)

aws ec2 authorize-security-group-ingress \
  --group-id $ECS_SG \
  --protocol tcp \
  --port 80 \
  --source-group $ALB_SG \
  --region $REGION

echo "Security Groups Created: ALB=$ALB_SG, ECS=$ECS_SG"

echo "=== Creating ECR Repositories ==="
aws ecr create-repository \
  --repository-name osp-poc/service1 \
  --region $REGION

aws ecr create-repository \
  --repository-name osp-poc/service2 \
  --region $REGION

echo "=== Creating ECS Cluster ==="
aws ecs create-cluster \
  --cluster-name $CLUSTER_NAME \
  --region $REGION

echo "=== Creating CloudWatch Log Groups ==="
aws logs create-log-group \
  --log-group-name /ecs/osp-poc-service1 \
  --region $REGION

aws logs create-log-group \
  --log-group-name /ecs/osp-poc-service2 \
  --region $REGION

echo "=== Setup Complete ==="
echo "VPC_ID: $VPC_ID"
echo "PUBLIC_SUBNET_1: $PUBLIC_SUBNET_1"
echo "PUBLIC_SUBNET_2: $PUBLIC_SUBNET_2"
echo "PRIVATE_SUBNET_1: $PRIVATE_SUBNET_1"
echo "PRIVATE_SUBNET_2: $PRIVATE_SUBNET_2"
echo "ALB_SG: $ALB_SG"
echo "ECS_SG: $ECS_SG"
echo "Save these values for next steps!"
```

**Run script**:
```bash
chmod +x setup-aws-infrastructure.sh
./setup-aws-infrastructure.sh > setup-output.txt
# Save the output values (VPC_ID, Subnet IDs, Security Group IDs)
```

#### Option C: Terraform (Best for IaC) ⚡⚡⚡

**Create `terraform/main.tf`**:

```hcl
terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = "us-east-1"
}

# VPC
resource "aws_vpc" "osp_poc" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = {
    Name = "osp-poc-vpc"
  }
}

# Internet Gateway
resource "aws_internet_gateway" "osp_poc" {
  vpc_id = aws_vpc.osp_poc.id

  tags = {
    Name = "osp-poc-igw"
  }
}

# Public Subnets
resource "aws_subnet" "public_1" {
  vpc_id                  = aws_vpc.osp_poc.id
  cidr_block              = "10.0.1.0/24"
  availability_zone       = "us-east-1a"
  map_public_ip_on_launch = true

  tags = {
    Name = "osp-poc-public-subnet-1"
  }
}

resource "aws_subnet" "public_2" {
  vpc_id                  = aws_vpc.osp_poc.id
  cidr_block              = "10.0.2.0/24"
  availability_zone       = "us-east-1b"
  map_public_ip_on_launch = true

  tags = {
    Name = "osp-poc-public-subnet-2"
  }
}

# Private Subnets
resource "aws_subnet" "private_1" {
  vpc_id            = aws_vpc.osp_poc.id
  cidr_block        = "10.0.11.0/24"
  availability_zone = "us-east-1a"

  tags = {
    Name = "osp-poc-private-subnet-1"
  }
}

resource "aws_subnet" "private_2" {
  vpc_id            = aws_vpc.osp_poc.id
  cidr_block        = "10.0.12.0/24"
  availability_zone = "us-east-1b"

  tags = {
    Name = "osp-poc-private-subnet-2"
  }
}

# Route Table for Public Subnets
resource "aws_route_table" "public" {
  vpc_id = aws_vpc.osp_poc.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.osp_poc.id
  }

  tags = {
    Name = "osp-poc-public-rt"
  }
}

resource "aws_route_table_association" "public_1" {
  subnet_id      = aws_subnet.public_1.id
  route_table_id = aws_route_table.public.id
}

resource "aws_route_table_association" "public_2" {
  subnet_id      = aws_subnet.public_2.id
  route_table_id = aws_route_table.public.id
}

# Security Groups
resource "aws_security_group" "alb" {
  name        = "osp-poc-alb-sg"
  description = "Security group for ALB"
  vpc_id      = aws_vpc.osp_poc.id

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "osp-poc-alb-sg"
  }
}

resource "aws_security_group" "ecs" {
  name        = "osp-poc-ecs-sg"
  description = "Security group for ECS tasks"
  vpc_id      = aws_vpc.osp_poc.id

  ingress {
    from_port       = 80
    to_port         = 80
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "osp-poc-ecs-sg"
  }
}

# ECR Repositories
resource "aws_ecr_repository" "service1" {
  name                 = "osp-poc/service1"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = false
  }
}

resource "aws_ecr_repository" "service2" {
  name                 = "osp-poc/service2"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = false
  }
}

# ECS Cluster
resource "aws_ecs_cluster" "osp_poc" {
  name = "osp-poc-cluster"

  setting {
    name  = "containerInsights"
    value = "disabled"
  }

  tags = {
    Name = "osp-poc-cluster"
  }
}

# CloudWatch Log Groups
resource "aws_cloudwatch_log_group" "service1" {
  name              = "/ecs/osp-poc-service1"
  retention_in_days = 7
}

resource "aws_cloudwatch_log_group" "service2" {
  name              = "/ecs/osp-poc-service2"
  retention_in_days = 7
}

# IAM Role for ECS Task Execution
resource "aws_iam_role" "ecs_task_execution" {
  name = "osp-poc-ecs-task-execution-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution" {
  role       = aws_iam_role.ecs_task_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# IAM Role for ECS Task
resource "aws_iam_role" "ecs_task" {
  name = "osp-poc-ecs-task-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
      }
    ]
  })
}

# Outputs
output "vpc_id" {
  value = aws_vpc.osp_poc.id
}

output "public_subnet_1_id" {
  value = aws_subnet.public_1.id
}

output "public_subnet_2_id" {
  value = aws_subnet.public_2.id
}

output "private_subnet_1_id" {
  value = aws_subnet.private_1.id
}

output "private_subnet_2_id" {
  value = aws_subnet.private_2.id
}

output "alb_security_group_id" {
  value = aws_security_group.alb.id
}

output "ecs_security_group_id" {
  value = aws_security_group.ecs.id
}

output "ecr_repository_1_uri" {
  value = aws_ecr_repository.service1.repository_url
}

output "ecr_repository_2_uri" {
  value = aws_ecr_repository.service2.repository_url
}

output "ecs_cluster_name" {
  value = aws_ecs_cluster.osp_poc.name
}

output "ecs_task_execution_role_arn" {
  value = aws_iam_role.ecs_task_execution.arn
}

output "ecs_task_role_arn" {
  value = aws_iam_role.ecs_task.arn
}
```

**Run Terraform**:
```bash
cd terraform
terraform init
terraform plan
terraform apply
# Save the output values!
```

**Recommendation**: Use **AWS Console** for fastest setup, or **AWS CLI script** if you want to automate.

---

### Phase 3: Build and Push Docker Images to ECR (20 minutes)

#### Step 3.1: Get ECR Login Token

```bash
# Get your AWS account ID
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
REGION="us-east-1"

# Login to ECR
aws ecr get-login-password --region $REGION | \
  docker login --username AWS --password-stdin \
  $ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com
```

#### Step 3.2: Build and Push Service1

```bash
# Get ECR repository URI
ECR_REPO_1="$ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com/osp-poc/service1"

# Build Docker image
cd Service1
docker build -t osp-poc/service1:latest .

# Tag for ECR
docker tag osp-poc/service1:latest $ECR_REPO_1:latest

# Push to ECR
docker push $ECR_REPO_1:latest

echo "Service1 image pushed: $ECR_REPO_1:latest"
```

#### Step 3.3: Build and Push Service2

```bash
# Get ECR repository URI
ECR_REPO_2="$ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com/osp-poc/service2"

# Build Docker image
cd ../Service2
docker build -t osp-poc/service2:latest .

# Tag for ECR
docker tag osp-poc/service2:latest $ECR_REPO_2:latest

# Push to ECR
docker push $ECR_REPO_2:latest

echo "Service2 image pushed: $ECR_REPO_2:latest"
```

**Verify in AWS Console**: Go to ECR → Repositories → Check images are uploaded

---

### Phase 4: Create ECS Task Definitions (15 minutes)

#### Option A: AWS Console

1. Go to ECS → Task Definitions → Create new Task Definition
2. **Task Definition for Service1**:
   - Family: `osp-poc-service1`
   - Launch type: Fargate
   - Operating system: Linux/X86_64
   - Task size:
     - CPU: 0.5 vCPU (512)
     - Memory: 1 GB (1024)
   - Task execution role: `osp-poc-ecs-task-execution-role`
   - Task role: `osp-poc-ecs-task-role`
   - Container 1:
     - Name: `service1`
     - Image URI: `ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/osp-poc/service1:latest`
     - Port mappings: 80 (TCP)
     - Environment variables:
       - `ASPNETCORE_ENVIRONMENT` = `Production`
       - `ASPNETCORE_URLS` = `http://+:80`
     - Log configuration:
       - Log driver: `awslogs`
       - Log group: `/ecs/osp-poc-service1`
       - Region: `us-east-1`
       - Stream prefix: `ecs`
     - Health check:
       - Command: `CMD-SHELL,curl -f http://localhost/health || exit 1`
       - Interval: 30 seconds
       - Timeout: 5 seconds
       - Retries: 3
   - Create

3. **Repeat for Service2** (change names and image URI)

#### Option B: AWS CLI (JSON File)

**Create `task-definition-service1.json`**:

```json
{
  "family": "osp-poc-service1",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "512",
  "memory": "1024",
  "containerDefinitions": [
    {
      "name": "service1",
      "image": "ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/osp-poc/service1:latest",
      "portMappings": [
        {
          "containerPort": 80,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        },
        {
          "name": "ASPNETCORE_URLS",
          "value": "http://+:80"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/osp-poc-service1",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      },
      "healthCheck": {
        "command": ["CMD-SHELL", "curl -f http://localhost/health || exit 1"],
        "interval": 30,
        "timeout": 5,
        "retries": 3,
        "startPeriod": 60
      }
    }
  ],
  "taskRoleArn": "arn:aws:iam::ACCOUNT_ID:role/osp-poc-ecs-task-role",
  "executionRoleArn": "arn:aws:iam::ACCOUNT_ID:role/osp-poc-ecs-task-execution-role"
}
```

**Replace `ACCOUNT_ID` with your AWS account ID**, then:

```bash
# Register task definition
aws ecs register-task-definition \
  --cli-input-json file://task-definition-service1.json \
  --region us-east-1

# Repeat for service2 (create task-definition-service2.json)
```

---

### Phase 5: Create Application Load Balancer (20 minutes)

#### Step 5.1: Create ALB (Console)

1. Go to EC2 → Load Balancers → Create Load Balancer
2. **Application Load Balancer**:
   - Name: `osp-poc-alb`
   - Scheme: Internet-facing
   - IP address type: IPv4
   - VPC: `osp-poc-vpc`
   - Mappings: Select both public subnets (us-east-1a, us-east-1b)
   - Security groups: `osp-poc-alb-sg`
   - Listeners:
     - HTTP (80): Redirect to HTTPS (443)
     - HTTPS (443): (we'll configure after creating)
   - Create load balancer

#### Step 5.2: Create Target Groups

**Target Group 1 (Service1)**:
1. Go to EC2 → Target Groups → Create target group
2. **Basic configuration**:
   - Target type: IP addresses
   - Name: `osp-poc-service1-tg`
   - Protocol: HTTP
   - Port: 80
   - VPC: `osp-poc-vpc`
   - Health checks:
     - Health check path: `/health`
     - Advanced: Interval 30s, Timeout 5s, Healthy threshold 2, Unhealthy threshold 3
   - Create target group

**Target Group 2 (Service2)**:
- Repeat with name: `osp-poc-service2-tg`

#### Step 5.3: Configure ALB Listeners

1. Go to ALB → Listeners → Edit listener (HTTPS 443)
2. **Add rules**:
   - **Rule 1**:
     - Condition: Path is `/api/service1*`
     - Action: Forward to `osp-poc-service1-tg`
   - **Rule 2**:
     - Condition: Path is `/api/service2*`
     - Action: Forward to `osp-poc-service2-tg`
   - **Default action**: Return fixed response (404)

**Note**: For HTTP (80), set default action to redirect to HTTPS.

---

### Phase 6: Create ECS Services (15 minutes)

#### Option A: AWS Console

1. Go to ECS → Clusters → `osp-poc-cluster` → Services → Create
2. **Service 1**:
   - Launch type: Fargate
   - Task Definition: `osp-poc-service1` (latest)
   - Service name: `osp-poc-service1`
   - Desired tasks: 1 (for POC, can increase later)
   - VPC: `osp-poc-vpc`
   - Subnets: Select both private subnets
   - Security groups: `osp-poc-ecs-sg`
   - Auto-assign public IP: Disabled (tasks in private subnets)
   - Load balancer: Application Load Balancer
     - Load balancer: `osp-poc-alb`
     - Target group: `osp-poc-service1-tg`
     - Container to load balance: `service1:80`
   - Create service

3. **Repeat for Service2**

#### Option B: AWS CLI

```bash
# Get values from previous steps
CLUSTER_NAME="osp-poc-cluster"
ALB_ARN="arn:aws:elasticloadbalancing:us-east-1:ACCOUNT_ID:loadbalancer/app/osp-poc-alb/..."
TG1_ARN="arn:aws:elasticloadbalancing:us-east-1:ACCOUNT_ID:targetgroup/osp-poc-service1-tg/..."
TG2_ARN="arn:aws:elasticloadbalancing:us-east-1:ACCOUNT_ID:targetgroup/osp-poc-service2-tg/..."
SUBNET_1="subnet-xxx"
SUBNET_2="subnet-yyy"
ECS_SG="sg-xxx"

# Create Service1
aws ecs create-service \
  --cluster $CLUSTER_NAME \
  --service-name osp-poc-service1 \
  --task-definition osp-poc-service1:1 \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[$SUBNET_1,$SUBNET_2],securityGroups=[$ECS_SG],assignPublicIp=DISABLED}" \
  --load-balancers "targetGroupArn=$TG1_ARN,containerName=service1,containerPort=80" \
  --region us-east-1

# Create Service2
aws ecs create-service \
  --cluster $CLUSTER_NAME \
  --service-name osp-poc-service2 \
  --task-definition osp-poc-service2:1 \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[$SUBNET_1,$SUBNET_2],securityGroups=[$ECS_SG],assignPublicIp=DISABLED}" \
  --load-balancers "targetGroupArn=$TG2_ARN,containerName=service2,containerPort=80" \
  --region us-east-1
```

---

### Phase 7: Test Deployment (10 minutes)

#### Step 7.1: Wait for Services to Stabilize

```bash
# Check service status
aws ecs describe-services \
  --cluster osp-poc-cluster \
  --services osp-poc-service1 osp-poc-service2 \
  --region us-east-1

# Check tasks are running
aws ecs list-tasks \
  --cluster osp-poc-cluster \
  --service-name osp-poc-service1 \
  --region us-east-1
```

**In Console**: Go to ECS → Services → Check tasks are running (green status)

#### Step 7.2: Get ALB DNS Name

```bash
# Get ALB DNS name
aws elbv2 describe-load-balancers \
  --names osp-poc-alb \
  --region us-east-1 \
  --query 'LoadBalancers[0].DNSName' \
  --output text
```

#### Step 7.3: Test Endpoints

```bash
# Get ALB DNS (replace with your ALB DNS)
ALB_DNS="osp-poc-alb-1234567890.us-east-1.elb.amazonaws.com"

# Test Service1
curl http://$ALB_DNS/api/service1

# Test Service2
curl http://$ALB_DNS/api/service2

# Test health endpoints
curl http://$ALB_DNS/api/service1/health
curl http://$ALB_DNS/api/service2/health
```

**Expected Response**:
```json
{
  "service": "Service1",
  "message": "Hello from Service1!",
  "timestamp": "2024-01-15T10:30:00Z",
  "environment": "Production"
}
```

---

## Verification Checklist

- [ ] VPC and subnets created
- [ ] Security groups configured
- [ ] ECR repositories created
- [ ] Docker images pushed to ECR
- [ ] ECS cluster created
- [ ] Task definitions registered
- [ ] ALB created and configured
- [ ] Target groups created and healthy
- [ ] ECS services running
- [ ] Tasks in "Running" state
- [ ] Can access services via ALB DNS
- [ ] Health checks passing
- [ ] CloudWatch logs visible

---

## Troubleshooting

### Issue: Tasks not starting
- **Check**: Task definition CPU/memory (Fargate has specific combinations)
- **Check**: IAM roles have correct permissions
- **Check**: ECR image URI is correct
- **Check**: CloudWatch logs for errors

### Issue: Health checks failing
- **Check**: Container port is 80 (not 443)
- **Check**: Health check path is correct (`/health`)
- **Check**: Security groups allow traffic from ALB to ECS

### Issue: Cannot access ALB
- **Check**: ALB security group allows HTTP/HTTPS from your IP
- **Check**: ALB is in public subnets
- **Check**: Target groups have healthy targets

### Issue: Images not pulling from ECR
- **Check**: Task execution role has `AmazonECSTaskExecutionRolePolicy`
- **Check**: ECR repository URI is correct
- **Check**: Image exists in ECR

---

## Cleanup (After POC)

**To avoid AWS charges, delete resources**:

```bash
# Delete ECS services
aws ecs update-service --cluster osp-poc-cluster --service osp-poc-service1 --desired-count 0
aws ecs update-service --cluster osp-poc-cluster --service osp-poc-service2 --desired-count 0
aws ecs delete-service --cluster osp-poc-cluster --service osp-poc-service1
aws ecs delete-service --cluster osp-poc-cluster --service osp-poc-service2

# Delete ALB and target groups
aws elbv2 delete-load-balancer --load-balancer-arn <ALB_ARN>
aws elbv2 delete-target-group --target-group-arn <TG_ARN>

# Delete ECR images (optional)
aws ecr batch-delete-image --repository-name osp-poc/service1 --image-ids imageTag=latest
aws ecr batch-delete-image --repository-name osp-poc/service2 --image-ids imageTag=latest

# Delete ECR repositories
aws ecr delete-repository --repository-name osp-poc/service1 --force
aws ecr delete-repository --repository-name osp-poc/service2 --force

# Delete ECS cluster
aws ecs delete-cluster --cluster osp-poc-cluster

# Delete VPC (via Console or Terraform destroy)
```

**Or use Terraform**:
```bash
cd terraform
terraform destroy
```

---

## Time Breakdown (2 Hours)

| Phase | Task | Time |
|-------|------|------|
| **Phase 1** | Create dummy .NET services | 30 min |
| **Phase 2** | AWS infrastructure setup | 40 min |
| **Phase 3** | Build and push Docker images | 20 min |
| **Phase 4** | Create ECS task definitions | 15 min |
| **Phase 5** | Create ALB and target groups | 20 min |
| **Phase 6** | Create ECS services | 15 min |
| **Phase 7** | Test and verify | 10 min |
| **Total** | | **2 hours 30 min** |

**Note**: Can reduce to 2 hours by:
- Using AWS Console (faster than CLI for first-time)
- Skipping Terraform (use Console/CLI)
- Creating only 1 service first, then duplicate

---

## Next Steps (After POC)

1. **Add HTTPS/SSL Certificate**:
   - Request ACM certificate
   - Attach to ALB listener

2. **Auto Scaling**:
   - Configure ECS service auto-scaling
   - Set min/max tasks based on CPU/memory

3. **CI/CD Pipeline**:
   - GitHub Actions / AWS CodePipeline
   - Auto-build and deploy on git push

4. **Monitoring**:
   - CloudWatch dashboards
   - Container Insights
   - Alarms for service health

5. **Service Discovery**:
   - AWS Cloud Map for service-to-service communication

6. **API Gateway**:
   - Add API Gateway in front of ALB
   - Rate limiting, API keys

---

## Key Learnings

✅ **Containerization**: Docker images for .NET apps  
✅ **ECR**: Store and version Docker images  
✅ **ECS Fargate**: Serverless container orchestration  
✅ **ALB**: Route traffic to multiple services  
✅ **VPC Networking**: Public/private subnets, security groups  
✅ **IAM Roles**: Task execution and task roles  
✅ **CloudWatch Logs**: Container logging  
✅ **Health Checks**: Ensure service availability  

---

## References

- [AWS ECS Documentation](https://docs.aws.amazon.com/ecs/)
- [ECS Fargate Getting Started](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/getting-started-fargate.html)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet-aspnet)
- [ALB Target Groups](https://docs.aws.amazon.com/elasticloadbalancing/latest/application/target-group-register-targets.html)

