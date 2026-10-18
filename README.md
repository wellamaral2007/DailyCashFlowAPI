# Definition

This is it a test of Carrefour Bank, for Solution Architect, which we have a challenge to control entrys of daily cash flow balance, and generate a report daily balance conform defined by document desafio-arquiteto-software-ago2024.pdf. 
I am Wellington Amaral, Solution Architect, that being participating in this challenge, to model a solution 
with the best software architecture and solution architecture patterns and practices that can be used in a real-world scenario to achieve the goal of the challenge. In terms of software architecture and solution architecture the main requirement is about the scalability and availability, supporting until 50 entries per second caming, canning have unvailibility of the service of 5% of time, reliability, keeping information of daily balance of cash flow without lose entrys, high performance, and security.

For this challenge, I defined a REST API solution with microservices, that is showed below. 
Where I did focus, in **design** of the solution and **explore** in terms of software architecture and solution architecture, having for next steps to do:

 1 - Create a Google Cloud Platform (GCP) service account for hosting the solution;

 2 - Run deploy shell script to create and configure the servless Secret Manager, APIGee, Cloud Functions, 
 Pub/Sub, CloudSQL and Redis;

 3 - Adjusts in the code and deploy shell files.

Therefore little adjusts in the code, still need to do, to make run complete solution proposed, and to deploy in GCP. 
This code focus more about the microservices, and design patterns applied, to achieve the goal of the challenge. 
This document is designed to showcase how the target challenge was approached, detailing the chosen patterns and the reasoning behind them, supported by modeling with software architecture and solution architecture diagrams, maded 
in drawio tool. 

## Finance Security API 

It is a REST API to generate a OAuth2 token for authentication and validate for the DailyCashFlow API access.

# API for controling entrys of Daily Cash Flow Balance.

## Solution
A HTTP API Rest that receive entry values of credit or debit with high-performance, reliable, security and availiabality by POST method
and obtain balance value of day that can be called of digital App(Angular for example) for control Cash Flow Balance and generate a report daily balance.

For this API, we propose to use GCP Cloud Servless services of *APIGateway APIGee, FAAS Cloud Function Staless with .Net Core, Pub/Sub Messaging, CloudSQL SQLServer,
and Memorystore Redis* for agile to delivery, auto-scaling no need of maintain servers and operations, for easy to make DevOps.

In development we use design patterns to facility reuse, simplicity, raise of quality of software solution.


## Architecture Patterns and Practices

* **CLEAN Architecture Software**
Simplicity of Concerns 

* **SOLID**
 Classes and objects with high reuse and low acoplament, following the principles of SOLID

* **API Gateway**
Hight Security with OAUTH2 and HTTPS to access API and governace and documentation of service

* **Facade**
A defined interface by APIGee apigateway that hide the complexity of the underlying implementation

* **Design First**
Definition of api with Swagger documentation, canning define api independently of the implementation. 
The Open API 3.0 specification of the APIs is defined in the api folder: openapi-financesecurity.yaml and openapi-dailycashflow.yaml.

* **Microservice**
Micro and independent executable unit of service running in FAAS that provide a container 
to run, what easing the development and manutenance of the application.

* **CQRS**
Three microservices separate for read and write operations, command with GeneratorDailyCashFlowMS and ProcessDailyCashFlowMS to write, using SAGA pattern, and for read DailyCashFlowBalanceMS for query operations just.

* **EDA**
Events reliable and high performance.

* **SAGA Choreography** 
Events in microservice reliable and high performance, a microservice GenerateDailyCashFlowMS for entry credit or debit of cash flow to keep reliable of solution and time of response. Microservice ProcessDailyCashFlowMS for update balance value of day in redis key-value with sum of values of entry credit or debit with transaction control keeping persistence of information in sql database, with flow value, and redis key-value.

* **Singleton**
Redis key-value for balance value of day, being a centralized storage of the state of the application, with the use of the Singleton pattern. Created one time by day.

* **DAL**
Data Access Layer, classes RedisDailyCashFlowDAL and SQLDailyCashFlowDAL, specilized for Redis and SQL respectively for manipulate data repository, with the use of the AbstractCRUDDAL class, which is a base class for the DAL classes.

* **Circuit Breaker**
In microservices, circuit breaker pattern is used to detect failures and prevent cascading failures. In this case, DailyCashFlowBalanceMS have circuit breaker to grow availability of service, case RedisDailyCashFlowDAL entry exception by any errors like connection failure, circuit breaker will be open, the service passing to get value of balance of day, by sum of all entrys of day by SQLDailyCashFlowDAL, until circuit breaker close and returning value by RedisDailyCashFlowDAL.

* **DDD**
Domain Driven Design (DDD) oriented to the business problem domain, with the use of the repository pattern to access the data layer which is DailyCashFlow the entity domain, inclusive the api signature of the service layer.

* **Template Method**
To apply a default pattern for services microservices and DAL classes.

* **PAAS**
For this solution, APIGateway, FAAS, Pub/Sub, Memorystore, and Redis are used as PaaS, considering the short time to delivery, and no need to install all infraestructure for this solution.

* **Servless**
The solution is auto-scaling, auto-managed and auto-healing in production, with the use of the serverless platform, GCP Cloud Servless, like FAAS, APIGateway, Pub/Sub, Memorystore, and Redis servless services turning easily keep solution up with high availability and high performance.

## Core Logic & Workflow

The API acts as a Facade (via Apigee) exposing POST (entries) and GET (balance) endpoints.

## 1. Write Flow (Credit/Debit) Request: 

* Client sends a POST request with an OAuth2 Token.

* Ingestion: The GenerateDailyCashFlowMS validates the entry and publishes an event to GCP Pub/Sub.

* Response: The API immediately returns 202 Accepted (or 200 OK) to the client.

* Processing: The ProcessDailyCashFlowMS triggers on the Pub/Sub event, persists the transaction in SQL Server, 
and updates the current balance in Redis (atomic update).

## 2. Read Flow (Daily Balance) Request: 

* Client sends a GET request.

* Retrieval: The DailyFlowBalanceMS attempts to read the balance from Redis (Cache-aside/Singleton state).

* Resilience: If Redis is unavailable, the Circuit Breaker opens, and the service calculates the balance in 
real-time using SQL Server data, ensuring the system never goes down.

## Architecture Solution

![DailyCashFlowArchitecture-Solution Diagram](https://github.com/wellamaral2007/DailyCashFlowAPI/blob/main/documentation/DailyCashFlowArchitecture-Solution%20Diagram.png)


## Architecture Software

![DailyCashFlowArchitecture-Software Class](https://github.com/wellamaral2007/DailyCashFlowAPI/blob/main/documentation/DailyCashFlowArchitecture-Software%20Class%20Diagram.png)

## Development Environment

* Language: C# (.NET 9)

* Infrastructure: GCP (Google Cloud Platform)

## Deployment

* Deploy shell script been createad to create and configure each GCP services servless Secret Manager, APIGee, Cloud Functions, Pub/Sub, CloudSQL and Redis, to run the solution in deploy folder.

## Tests

* Units

  Tests with Moq and Xunit unit tests of components of solution.

* Integration

  Automated with Xunit for checking entrys of daily cash flow, and checking of balance value of day of integration way 
  with all GCP components in business cenario.

* Performance 

  Automated with NBomber to test performance of solution, with load test, dispatching of requests 50 requests per second, and checking of lost unti 5% of time.

## Next Steps - Setup to Run (from deploy folder on linux OS)

* Install GCP CLI and login to GCP
  
  ./setup-gcp.sh

## Execution of Deployment Automation Scripts

* Run the other scripts in deploy folder in sequence

  ./deploy-all.sh

* Secrets configuration

  ./deploy-secrets.sh

* Network configuration

  ./deploy-network.sh

* Databases configuration

  ./deploy-databases.sh

* Pub/Sub configuration

  ./deploy-pubsub.sh

* Access Control configuration
  
  ./deploy-iam.sh

* Cloud Functions configuration

  ./deploy-cloud-functions.sh

* Deploy API Gateway project and configuration

  ./deploy-apigee.sh

* Make all tests run, unitary, integration and load tests

  ./tests-all.sh

  ### 🔹 3.1 Orchestration Diagram & Pipeline Execution Breakdowns

The complete infrastructure deployment is managed through the execution of the main loop. Below is the automated structural flow followed by a line-by-line breakdown of each script's responsibility within the Cloud architecture:

```text
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│                               deploy-all.sh Execution Flow                               │
├───────────────────┬───────────────────┬──────────────────────────┬───────────────────────┤
│      STAGE 1      │      STAGE 2      │         STAGE 3          │        STAGE 4        │
│   🛡️ Foundation   │  ⚡ Core Services  │   🚀 Compute & Gateways  │     🧪 Validation     │
├───────────────────┼───────────────────┼──────────────────────────┼───────────────────────┤
│ • gcloud set      │ • Secrets (Vault) │ • Cloud Functions (FaaS) │ • tests-all.sh        │
│ • setup-gcp.sh    │ • Network & IAM   │ • Apigee API Proxies     │   - Domain Unit       │
│                   │ • DBs & Pub/Sub   │                          │   - DAL Integration   │
│                   │                   │                          │   - NBomber Load Test │
└───────────────────┴───────────────────┴──────────────────────────┴───────────────────────┘
```

#### 📋 Line-by-Line Script Execution Details

* **`gcloud config set project finance-solution-2026`**
  * Modulates the active Google Cloud SDK context to target the dedicated production project boundary workspace before invoking any automation.
* **`./setup-gcp.sh`**
  * Triggers the cloud bootstrapping phase by programmatically enabling native Google Cloud APIs (Cloud SQL Admin, Memorystore, Pub/Sub, Cloud Functions, and Apigee Deployment Management).
* **`./deploy-secrets.sh`**
  * Seeds symmetric database credentials and secure API encryption tokens directly into Google Secret Manager, resolved dynamically at runtime via the `VaultCredentialUtility` wrapper helper.
* **`./deploy-network.sh`**
  * Establishes the private network topology core by provisioning VPC meshes, Private Service Connect links, and **Cloud Firewall Ingress Rules** (`allow-bastion-to-infrastructure`) to lock down internal communication.
* **`./deploy-databases.sh`**
  * Provisions the highly available, multi-node storage layers by initializing Cloud SQL for MySQL Master (`10.10.0.11`), Read Replica (`10.10.0.13`), and Google Memorystore for Redis (`10.10.76.235`).
* **`./deploy-pubsub.sh`**
  * Configures the EDA asynchronous message brokers by provisioning the `DailyCashFlowEntry` pub/sub topic alongside its dedicated push subscription listener paths.
* **`./deploy-iam.sh`**
  * Enforces least-privilege cloud governance by generating isolated Service Accounts, binding them tightly to the runtime compute execution blocks to prevent security leaks.
* **`./deploy-cloud-functions.sh`**
  * Compiles and publishes the optimized `.NET 9` container framework environment (`net9.0-alpine`), explicitly anchoring the Kestrel inbound loop via the `FUNCTION_TARGET` environment configuration.
* **`./deploy-apigee.sh`**
  * Publishes the API Gateway proxy configuration schema into targeted environment hostname groups, enforcing `Verify-Credentials` basic auth and OAuth2 token validation rules on edge ports.
* **`./tests-all.sh`**
  * Kicks off the continuous non-blocking validation matrix, routing the solution across four strict pipeline barriers (Domain Unit, DAL Integration, and NBomber 50 RPS load validations).
