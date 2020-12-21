/// <filename>
///     AddQuoteUrl.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Core.Commands
{
  public class AddQuoteUrl : ZES.Infrastructure.Domain.Command
  {
    public AddQuoteUrl() 
    {
    }  
    public string Fordom
    {
       get; 
       set;
    }  
    public string Url
    {
       get; 
       set;
    }  
    public override string Target
    {
       get
      {
        return Fordom;
      }
    }  
    public AddQuoteUrl(string fordom, string url) 
    {
      Fordom = fordom; 
      Url = url;
    }
  }
}
