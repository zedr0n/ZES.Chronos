/// <filename>
///     AddMinedCoinToContract.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Hashflare.Commands
{
  public class AddMinedCoinToContract : ZES.Infrastructure.Domain.Command
  {
    public AddMinedCoinToContract() 
    {
    }  
    public string ContractId
    {
       get; 
       set;
    }  
    public string Type
    {
       get; 
       set;
    }  
    public double Quantity
    {
       get; 
       set;
    }  
    public override string Target
    {
       get
      {
        return ContractId;
      }
    }  
    public AddMinedCoinToContract(string contractId, string type, double quantity) 
    {
      ContractId = contractId; 
      Type = type; 
      Quantity = quantity;
    }
  }
}

