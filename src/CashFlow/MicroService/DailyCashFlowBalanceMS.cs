using Base.MicroService;
using CashFlow.DAL;
using CashFlow.Event;

namespace CashFlow.MicroService;

/*******************************
Microservice logic with CQRS 
read operations just - 
*******************************/
public class DailyCashFlowBalanceMS : AbstractMicroService, IDailyCashFlowBalanceMS
{    

    IDailyCashFlowDAL DailyCashFlowDAL;

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