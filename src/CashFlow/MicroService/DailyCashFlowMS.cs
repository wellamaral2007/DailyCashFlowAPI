using Base.MicroService;
using CashFlow.DAL;
using CashFlow.Event;

namespace CashFlow.MicroService;

/*******************************
Microservice logic with SAGA Choreography
with EDA for high performance and
rely of solution
*******************************/
public class DailyCashFlowMS : AbstractMicroService, IDailyCashFlowMS
{    

    IDailyCashFlowDAL DailyCashFlowDAL;

    /*******************************
    Called When Event of Topic is 
    throwCreditDebit for register 
    in relational Table
    *******************************/
    private void RegisterCreditDebit(DailyCashFlowEvent DailyCashFlowEvent)
    {
        DailyCashFlowDAL.insertCreditDebit(DailyCashFlowEvent);
    }

   /*******************************
    According Event of Topic processing
    Event with CQRS for update
    BalanceValue after write the throw
    of Credit or Debit
    *******************************/
    private void UpdateBalance(DailyCashFlowEvent DailyCashFlowEvent)
    {
        DailyCashFlowDAL.updateBalanceValue(DailyCashFlowEvent);
    }

   /*******************************
    Called When Event of Topic is 
    registred for update Key-Value of
    BalanceValue State in Redis
    *******************************/
    public void ProcessingDailyCashEvent(DailyCashFlowEvent DailyCashFlowEvent)
    {
        if (DailyCashFlowEvent.EventType.CompareTo("throw") == 0) 
        {
            RegisterCreditDebit(DailyCashFlowEvent);
        } else if (DailyCashFlowEvent.EventType.CompareTo("registred") == 0) 
        {
            UpdateBalance(DailyCashFlowEvent);
        }
    }

   /*******************************
    Take current Key-Value of
    BalanceValue State in Redis
    for report return
    *******************************/
    public Double ObtainDailyBalance()
    {
        return DailyCashFlowDAL.readBalanceValue();
    }

    
}