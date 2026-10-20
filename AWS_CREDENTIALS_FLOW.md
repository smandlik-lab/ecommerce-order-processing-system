# AWS Credentials Flow in Ecommerce Order Processing System

## Overview
AWS credentials are automatically resolved by the **AWS SDK for .NET** using its **standard credential chain**, without storing credentials in configuration files.

---

## 1. How AWS Credentials are Configured

### In `Program.cs` (Lines 118-119)
```csharp
// Register AWS Options and SQS Client
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSQS>();
```

### What This Does:
- **`AddDefaultAWSOptions()`** → Reads AWS settings from `IConfiguration` (appsettings.json)
- **`AddAWSService<IAmazonSQS>()`** → Registers `IAmazonSQS` client in dependency injection
- The AWS SDK automatically applies the **credential chain** to resolve credentials

---

## 2. AWS SDK Credential Chain (Order of Resolution)

The AWS SDK for .NET uses the following **prioritized credential chain**:

### Priority Order:
1. **Environment Variables** (HIGHEST PRIORITY)
   - `AWS_ACCESS_KEY_ID`
   - `AWS_SECRET_ACCESS_KEY`
   - `AWS_SESSION_TOKEN` (optional, for temporary credentials)

2. **AWS Profile** (from `~/.aws/credentials`)
   - Default profile or specified profile via `AWS_PROFILE` environment variable
   - Usually configured via AWS CLI: `aws configure`

3. **IAM Role** (on EC2 / ECS / Lambda)
   - If running on AWS infrastructure, uses attached IAM role
   - Automatic temporary credential rotation

4. **ECS Task Role** (in ECS)
   - Credentials from ECS task metadata endpoint

5. **Lambda Execution Role** (in Lambda)
   - Automatic credentials from Lambda runtime

---

## 3. Configuration in Your Application

### `appsettings.json` (AWS Region Only)
```json
{
  "AWS": {
	"Region": "ap-southeast-2"
  },
  "AWSQueueSettings": {
	"OrderQueueUrl": "https://sqs.ap-southeast-2.amazonaws.com/103714492750/order-processing-queue"
  }
}
```

**Important:** Only the **Region** and **Queue URL** are in `appsettings.json`.  
**Credentials are NOT stored here** (good security practice).

### How AWS Region is Used:
```csharp
builder.Configuration.GetAWSOptions()
// Reads: config["AWS:Region"] = "ap-southeast-2"
// This region is used to construct SQS endpoint URLs
```

---

## 4. OrderQueueService - How It Uses SQS

### Constructor:
```csharp
public OrderQueueService(IAmazonSQS sqsClient, IConfiguration configuration)
{
	_sqsClient = sqsClient;  // ← Already authenticated by AWS SDK
	_queueUrl = configuration["AWSQueueSettings:OrderQueueUrl"]
				?? throw new InvalidOperationException("Queue URL not configured.");
}
```

### Sending Messages:
```csharp
public async Task EnqueuePendingOrderAsync(int orderId, int delaySeconds = 300)
{
	var messageBody = JsonSerializer.Serialize(new { OrderId = orderId });
	var request = new SendMessageRequest
	{
		QueueUrl = _queueUrl,
		MessageBody = messageBody,
		DelaySeconds = delaySeconds
	};

	// _sqsClient automatically uses resolved credentials
	await _sqsClient.SendMessageAsync(request);
}
```

---

## 5. Setting Up AWS Credentials for Local Development

### Option A: Using AWS CLI Profile (Recommended)
```powershell
# Configure default AWS profile
aws configure
# Enter:
# - AWS Access Key ID: [your-key]
# - AWS Secret Access Key: [your-secret]
# - Default region: ap-southeast-2
# - Default output format: json

# This creates ~/.aws/credentials and ~/.aws/config
```

### Option B: Environment Variables (For CI/CD or Scripts)
```powershell
# PowerShell (Windows)
$env:AWS_ACCESS_KEY_ID = "your-access-key"
$env:AWS_SECRET_ACCESS_KEY = "your-secret-key"
$env:AWS_DEFAULT_REGION = "ap-southeast-2"

# Or permanently in system environment variables
```

### Option C: .NET User Secrets (Development Only)
**Not recommended for AWS credentials** (uses local machine storage).  
Use AWS CLI profiles instead.

---

## 6. Order Processing Flow with AWS Credentials

### Step 1: Create Order (Authenticated)
```
POST /api/orders
│
└─→ OrderService.CreateOrderAsync(userId)
	├─→ Creates Order with status = PENDING
	├─→ Saves to database
	└─→ Calls _orderQueueService.EnqueuePendingOrderAsync(orderId)
```

### Step 2: Enqueue to SQS (Uses Credentials)
```
OrderQueueService.EnqueuePendingOrderAsync(orderId, 300)
│
└─→ IAmazonSQS.SendMessageAsync(request)
	│
	├─→ AWS SDK Credential Chain Resolution:
	│   1. Check ENV: AWS_ACCESS_KEY_ID
	│   2. Check ~/.aws/credentials (AWS CLI profile)
	│   3. Check EC2/ECS/Lambda IAM role
	│
	└─→ Send signed request to:
		https://sqs.ap-southeast-2.amazonaws.com/...
```

### Step 3: Background Worker Consumes Messages
```
OrderQueueConsumerService (HostedService)
│
└─→ Runs in background loop with AWS credentials:
	├─→ Calls IAmazonSQS.ReceiveMessageAsync()
	├─→ Deserializes message → gets OrderId
	├─→ Updates Order status = COMPLETED
	├─→ Saves to database
	└─→ Deletes message from queue
```

---

## 7. Verification & Troubleshooting

### Check Current Credentials:
```powershell
# Using AWS CLI
aws sts get-caller-identity

# Output shows:
# {
#     "UserId": "AIDAJ...",
#     "Account": "103714492750",
#     "Arn": "arn:aws:iam::103714492750:user/your-user"
# }
```

### Check AWS Profile:
```powershell
aws configure list
# Shows: name                    value             type    location
#        profile                default           manual  --profile
#        access_key     ****XXXXX       env             AWS_ACCESS_KEY_ID
#        secret_key     ****XXXXX       env             AWS_SECRET_ACCESS_KEY
#        region         ap-southeast-2   config file  ~/.aws/config
```

### Common Issues:

| Issue | Cause | Solution |
|-------|-------|----------|
| **NoCredentialsException** | Credentials not found | Configure AWS CLI: `aws configure` |
| **InvalidOperationException: Queue URL not configured** | Missing `AWSQueueSettings:OrderQueueUrl` | Check `appsettings.json` |
| **Access Denied (403)** | IAM user lacks SQS permissions | Grant `sqs:SendMessage`, `sqs:ReceiveMessage`, `sqs:DeleteMessage` |
| **Wrong region** | Region mismatch | Verify AWS Region matches queue region |

---

## 8. Security Best Practices

✅ **DO:**
- Use AWS CLI profiles for local development
- Use IAM roles in production (EC2, ECS, Lambda)
- Use temporary credentials with `AWS_SESSION_TOKEN`
- Rotate credentials regularly
- Restrict IAM policy to least privilege (only SQS permissions needed)

❌ **DON'T:**
- Store AWS credentials in `appsettings.json` or source code
- Commit `.aws/credentials` file
- Use root AWS account credentials
- Hard-code credentials in environment variables

---

## 9. Production Deployment (AWS)

### EC2 / ECS / Lambda
1. Create an **IAM Role** with SQS permissions
2. Attach role to EC2 instance / ECS task / Lambda function
3. AWS SDK automatically uses role credentials (no manual setup needed)

### Example IAM Policy:
```json
{
  "Version": "2012-10-17",
  "Statement": [
	{
	  "Effect": "Allow",
	  "Action": [
		"sqs:SendMessage",
		"sqs:ReceiveMessage",
		"sqs:DeleteMessage",
		"sqs:GetQueueAttributes"
	  ],
	  "Resource": "arn:aws:sqs:ap-southeast-2:103714492750:order-processing-queue"
	}
  ]
}
```

---

## Summary

| Component | Source | Purpose |
|-----------|--------|---------|
| **AWS Credentials** | Environment / AWS CLI `~/.aws/credentials` | Authenticate SQS API calls |
| **AWS Region** | `appsettings.json` → `config["AWS:Region"]` | Determine SQS endpoint |
| **Queue URL** | `appsettings.json` → `config["AWSQueueSettings:OrderQueueUrl"]` | SQS queue identifier |
| **IAmazonSQS Client** | `AddAWSService<IAmazonSQS>()` in Program.cs | Inject pre-authenticated SQS client |

The AWS SDK handles credential resolution automatically—you only need to:
1. Set up AWS credentials (AWS CLI or environment variables)
2. Provide region and queue URL in configuration
3. Inject `IAmazonSQS` where needed
