# API for controling throws of Daily Cash Flow Balance.

## Solution
A HTTP API Rest that receive throw values of credit or debit with high-performance, reily, security and availiabality by PUT method
and obtain balance value of day that can be called of digital App for control Cash Flow Balance and generate a report daily balance.

For this API, we use GCP Cloud Servless services of *APIGateway APIGee, FAAS Cloud Function Staless with .Net Core, Pub/Sub Messaging, CloudSQL SQLServer,
and Memorystore Redis* for agile to delivery, auto-scaling no need of maintain servers and operations, for easy to make DevOps.

In development we use design patterns to facility reuse, simplicity, raise of quality of software solution.


## Design Patterns

**CLEAN Architecture Software**
Simplicity of Concerns 

**SOLID**
 Classes and objects with high reuse and low acoplament

**API Gateway**
Hight Security with OAUTH2 and HTTPS to access API and governace and documentation of service

**EDA**
Events reily and high performance

**Desing First**
Definition of api with Swagger documentation

**Microservice**
Micro and independent executable unit of service running in FAAS that provide a container 
to run

**SAGA Choreography** 
Events of microservice reily and high performance


## Logical of Microservice

Microservice logic with SAGA Choreography
with EDA events that are processed for high performance and
rely of solution exposed by API Gateway with security to internet.

1 - Access by API Gateway with a security OAuth 2 Token and encrypted channel  
with HTTP request with method PUT witch throw of credit or debit value or GET method for read current *Balance Value*  
2 - Microservice create throw event with current datetime and value of throw to Pub/Sub  
3 - Microservice return a 200 status code for client  
4 - Microservice listen Pub/Sub Events and processing case **throw** event save it in relation database and put in Pub/Sub **registred** event  
5 - Microservice listen Pub/Sub Events and processing case **registred** event update value of *Balance Value* in memorystore redis key-value with sum of values    

Steps 4 and 5 are made wtih transaction control.


# HTTP Request-Reponse
**PUT** Method for throw Credit or Debit

**GET** Method for get Balance Value

# Throw Event
Event of Topic is 
throw for register 
in relational Table of Database

# Registred Event
According Event of Topic processing
Event with CQRS for update
BalanceValue after write the throw
of Credit or Debit in database when value have signal + (credit) or -(debit)
and update Key-Value of *BalanceValue* State in Redis

## Architecture Solution

![DailyCashFlowArchitecture-Solution Diagram](https://github.com/user-attachments/assets/7e5a6e95-6b3b-420b-9b91-6b610f94bd91)


## Architecture Software

![DailyCashFlowArchitecture-Software Class](https://github.com/user-attachments/assets/1495cdfa-0390-4177-94c6-3c074e3db800)



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
