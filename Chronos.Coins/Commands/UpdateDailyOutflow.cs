/// <filename>
///     UpdateDailyOutflow.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Coins.Commands
{
  public class UpdateDailyOutflow : ZES.Infrastructure.Domain.Command
  {
    public UpdateDailyOutflow() 
    {
    }  
    public string Address
    {
       get; 
       set;
    }  
    public int Index
    {
       get; 
       set;
    }  
    public bool UseRemote
    {
       get; 
       set;
    }  
    public bool UseV2
    {
       get; 
       set;
    }  
    public int Count
    {
       get; 
       set;
    }  
    public override string Target
    {
       get
      {
        return Address;
      }
    }  
    public UpdateDailyOutflow(string address, int index)
    {
      Pure = true;
      Address = address; 
      Index = index;
    }
  }
}

