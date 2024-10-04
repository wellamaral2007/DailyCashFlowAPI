using Base.MicroService;
using CashFlow.Event;

namespace CashFlow.MicroService;

/*******************************
Microservice logic with SAGA Choreography
with EDA for high performance and
rely of solution
*******************************/
public class DailyCashFlowMS : AbstractMicroService, IDailyCashFlowMS
{    

    /*******************************
    Called When Event of Topic is 
    throwCreditDebit for register 
    in relational Table
    *******************************/
    private void RegisterCreditDebit(DailyCashFlowEvent DailyCashFlowEvent)
    {

    }

   /*******************************
    According Event of Topic processing
    Event with CQRS for update
    BalanceValue after write the throw
    of Credit or Debit
    *******************************/
    private void UpdateBalance(Double CreditDebitValue)
    {

    }

   /*******************************
    Called When Event of Topic is 
    registred for update Key-Value of
    BalanceValue State in Redis
    *******************************/
    public void ProcessingDailyCashEvent(DailyCashFlowEvent DailyCashFlowEvent)
    {

    }

   /*******************************
    Take current Key-Value of
    BalanceValue State in Redis
    for report return
    *******************************/
    public double ObtainDailyBalance()
    {
        return 0;

    }

    
}