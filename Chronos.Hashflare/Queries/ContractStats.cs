/// <filename>
///     ContractStats.cs
/// </filename>

// <auto-generated/>

using ZES.Interfaces.Clocks;

namespace Chronos.Hashflare.Queries
{
  public class ContractStats : ZES.Interfaces.Domain.ISingleState
  {
    public ContractStats() 
    {
    }  
    public string ContractId
    {
       get; 
       set;
    }

    public int Quantity
    {
        get;
        set;
    }
    public string Type
    {
       get; 
       set;
    }  
    public double Mined
    {
       get; 
       set;
    }  
    public Time Date
    {
      get; 
      set;
    }

    public double Cost
    {
        get;
        set;
    }
    
    public ContractStats(string contractId, string type, double mined, Time date, int quantity, double cost) 
    {
      ContractId = contractId; 
      Type = type; 
      Mined = mined; 
      Date = date;
      Quantity = quantity;
      Cost = cost;
    }
  }
}

