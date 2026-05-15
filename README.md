# API for controling throws of Daily Cash Flow Balance.

## Solution
A HTTP API Rest that receive throw values of credit or debit with high-performance, reily, security and availiabality by POST method
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
to run.

**CQRS**
Three microservices separate for read and write operations, command with GeneratorDailyCashFlowMS and ProcessDailyCashFlowMS to write, using SAGA pattern, and for read DailyCashFlowBalanceMS for query operations just.

**EDA**
Events reily and high performance.

**SAGA Choreography** 
Events of microservice reily and high performance, a microservice GenerateDailyCashFlowMS for throw credit or debit of cash flow to keep rely of solution and time of response. Microservice ProcessDailyCashFlowMS for update balance value of day in redis key-value with sum of values of throw credit or debit with transaction control keeping persistence of information in sql database, with flow value, and redis key-value.

**Singleton**
Redis key-value for balance value of day, being a centralized storage of the state of the application, with the use of the Singleton pattern. Created one time by day.

**DAL**
Data Access Layer, classes RedisDailyCashFlowDAL and SQLDailyCashFlowDAL, specilized for Redis and SQL respectively for manipulate data repository, with the use of the AbstractCRUDDAL class, which is a base class for the DAL classes.

**DDD**
Domain Driven Design (DDD) oriented to the business problem domain, with the use of the repository pattern to access the data layer which is DailyCashFlow the entity domain, inclusive the api signature of the service layer.

**Template Method**
To apply a default pattern for services microservices and DAL classes.

**AOP**
Abstract Class for Cross-Cutting AOP for instance DALS of Microservice,
and Template Method for Handle Error, Logging, Transaction for simplify code 


## Logical of Solution

DailyCashFlow REST api exposed in APIGee, like facade, with operations POST and GET to post flow cash and get today balance
having three microservice separated by read and write with CQRS pattern DailyFlowBalanceMS, GenerateDailyCashFlowMS and ProcessDailyCashFlowMS. In write part using with SAGA Choreography with EDA events that are processed by independent components for high performance.

1 - Access by DailyCashFlow REST api with a security OAuth 2 Token and encrypted channel  
with HTTP request with method POST witch throw of credit or debit value or GET method for read current *Balance Value* of today;  
2 - GenerateDailyCashFlowMS microservice create throw event with current datetime and value of throw putting in Pub/Sub;  
3 - Api return a 200 status code for client;  
4 - ProcessDailyCashFlowMS microservice listen Pub/Sub Events and processing **throw** event saving throw in relation database and update value of *Balance Value* in memorystore redis key-value, like singleton for application about system state, with sum of values, having transaction control for avoiding inconsistencies;

5 - For get balance, DailyFlowBalanceMS Microservice read key-value of balance value of day, return value to client.


# HTTP Request-Reponse
**POST** Method for throw Credit or Debit

**GET** Method for get Balance Value

## Architecture Solution

![DailyCashFlowArchitecture-Solution Diagram](https://github.com/wellamaral2007/DailyCashFlowAPI/blob/main/documentation/DailyCashFlowArchitecture-Solution%20Diagram.png)


## Architecture Software

![DailyCashFlowArchitecture-Software Class](https://github.com/wellamaral2007/DailyCashFlowAPI/blob/main/documentation/DailyCashFlowArchitecture-Software%20Class%20Diagram.png)



## Code
This code was developed with C# programming language and .Net Core 8


## Steps to Run Local

Config APIGateway APIGee GCP Servless  
*todo detail*

Config FAAS Cloud Function Staless with .Net Core GCP Servless  
*todo detail*

Config Pub/Sub Messaging and Event Topic GCP Servless  
*todo detail*

Config Memorystore Redis GCP Servless  
*todo detail*

Use apiman tool to do a request to API.  
*todo detail*
