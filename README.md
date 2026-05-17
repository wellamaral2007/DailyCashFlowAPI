# API for controling entrys of Daily Cash Flow Balance.

## Solution
A HTTP API Rest that receive entry values of credit or debit with high-performance, reliable, security and availiabality by POST method
and obtain balance value of day that can be called of digital App(Angular for example) for control Cash Flow Balance and generate a report daily balance.

For this API, we use GCP Cloud Servless services of *APIGateway APIGee, FAAS Cloud Function Staless with .Net Core, Pub/Sub Messaging, CloudSQL SQLServer,
and Memorystore Redis* for agile to delivery, auto-scaling no need of maintain servers and operations, for easy to make DevOps.

In development we use design patterns to facility reuse, simplicity, raise of quality of software solution.


## Architecture Patterns and Practices

**CLEAN Architecture Software**
Simplicity of Concerns 

**SOLID**
 Classes and objects with high reuse and low acoplament, following the principles of SOLID

**API Gateway**
Hight Security with OAUTH2 and HTTPS to access API and governace and documentation of service

**Facade**
A defined interface by APIGee apigateway that hide the complexity of the underlying implementation

**Design First**
Definition of api with Swagger documentation, canning define api independently of the implementation.

**Microservice**
Micro and independent executable unit of service running in FAAS that provide a container 
to run, what easing the development and manutenance of the application.

**CQRS**
Three microservices separate for read and write operations, command with GeneratorDailyCashFlowMS and ProcessDailyCashFlowMS to write, using SAGA pattern, and for read DailyCashFlowBalanceMS for query operations just.

**EDA**
Events reliable and high performance.

**SAGA Choreography** 
Events in microservice reliable and high performance, a microservice GenerateDailyCashFlowMS for entry credit or debit of cash flow to keep reliable of solution and time of response. Microservice ProcessDailyCashFlowMS for update balance value of day in redis key-value with sum of values of entry credit or debit with transaction control keeping persistence of information in sql database, with flow value, and redis key-value.

**Singleton**
Redis key-value for balance value of day, being a centralized storage of the state of the application, with the use of the Singleton pattern. Created one time by day.

**DAL**
Data Access Layer, classes RedisDailyCashFlowDAL and SQLDailyCashFlowDAL, specilized for Redis and SQL respectively for manipulate data repository, with the use of the AbstractCRUDDAL class, which is a base class for the DAL classes.

**Circuit Breaker**
In microservices, circuit breaker pattern is used to detect failures and prevent cascading failures. In this case, DailyCashFlowBalanceMS have circuit breaker to grow availability of service, case RedisDailyCashFlowDAL entry exception by any errors like connection failure, circuit breaker will be open, the service passing to get value of balance of day, by sum of all entrys of day by SQLDailyCashFlowDAL, until circuit breaker close and returning value by RedisDailyCashFlowDAL.

**DDD**
Domain Driven Design (DDD) oriented to the business problem domain, with the use of the repository pattern to access the data layer which is DailyCashFlow the entity domain, inclusive the api signature of the service layer.

**Template Method**
To apply a default pattern for services microservices and DAL classes.

**PAAS**
For this solution, APIGateway, FAAS, Pub/Sub, Memorystore, and Redis are used as PaaS, considering the short time to delivery, and no need to install all infraestructure for this solution.

**Servless**
The solution is auto-scaling, auto-managed and auto-healing in production, with the use of the serverless platform, GCP Cloud Servless, like FAAS, APIGateway, Pub/Sub, Memorystore, and Redis servless services turning easily keep solution up with high availability and high performance.

## Core Logic & Workflow

The API acts as a Facade (via Apigee) exposing POST (entries) and GET (balance) endpoints.

## 1. Write Flow (Credit/Debit) Request: Client sends a POST request with an OAuth2 Token.Ingestion: 

The GenerateDailyCashFlowMS validates the entry and publishes an event to GCP Pub/Sub.Response: 
The API immediately returns 202 Accepted (or 200 OK) to the client.Processing: 
The ProcessDailyCashFlowMS triggers on the Pub/Sub event, persists the transaction in SQL Server, 
and updates the current balance in Redis (atomic update).

## 2. Read Flow (Daily Balance) Request: Client sends a GET request.Retrieval: 

DailyFlowBalanceMS attempts to read the balance from Redis (Cache-aside/Singleton state).Resilience: 
If Redis is unavailable, the Circuit Breaker opens, and the service calculates the balance in 
real-time using SQL Server data, ensuring the system never goes down.

## Architecture Solution

![DailyCashFlowArchitecture-Solution Diagram](https://github.com/wellamaral2007/DailyCashFlowAPI/blob/main/documentation/DailyCashFlowArchitecture-Solution%20Diagram.png)


## Architecture Software

![DailyCashFlowArchitecture-Software Class](https://github.com/wellamaral2007/DailyCashFlowAPI/blob/main/documentation/DailyCashFlowArchitecture-Software%20Class%20Diagram.png)

## Development Environment

Language: C# (.NET 8)Infrastructure: GCP (Google Cloud Platform)

## Local Setup (Future Work) to Run 

Detailed GCP API Gateway configuration.

Cloud Functions deployment scripts.

Pub/Sub Topic and Subscription setup.

Redis Memorystore connectivity guide.

Use apiman tool to do a request to API.  
