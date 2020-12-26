import * as React from 'react';
import { Button, ButtonType } from 'office-ui-fabric-react';
import Header from './Header';
import HeroList, { HeroListItem } from './HeroList';
import Progress from './Progress';
import { request } from 'graphql-request';
import RangeInput from "./RangeInput";

declare global {
  interface Window {
    server: string;
    period: number;
    console : any;
  }
}

window.server = "https://localhost:5001";
window.period = 5000;
window.console = console;

export interface AppProps {
  title: string;
  isOfficeInitialized: boolean;
}

export interface AppState {
  listItems: HeroListItem[];
}

export default class App extends React.Component<AppProps, AppState> {
  constructor(props, context) {
    super(props, context);
    this.state = {
      listItems: []
    };
  }
  
  server : string = "https://localhost:5001";

  componentDidMount() {
    this.setState({
      listItems: [ /*
        {
          icon: 'Ribbon',
          primaryText: 'Achieve more with Office integration'
        },
        {
          icon: 'Unlock',
          primaryText: 'Unlock features and functionality'
        },
        {
          icon: 'Design',
          primaryText: 'Create and visualize like a pro'
        }*/
      ]
    });
  }

  ExcelDateToJSDate = (serial : number) => {
    var utc_days  = Math.floor(serial - 25569);
    var utc_value = utc_days * 86400;
    var date_info = new Date(utc_value * 1000);

    var fractional_day = serial - Math.floor(serial) + 0.0000001;

    var total_seconds = Math.floor(86400 * fractional_day);

    var seconds = total_seconds % 60;

    total_seconds -= seconds;

    var hours = Math.floor(total_seconds / (60 * 60));
    var minutes = Math.floor(total_seconds / 60) % 60;

    return new Date(date_info.getFullYear(), date_info.getMonth(), date_info.getDate(), hours, minutes, seconds);
  }
  
  doRange = async(fn : (data : Excel.Range) => Promise<void>) => {
    try{
      await Excel.run(async context => {
        const range = context.workbook.getSelectedRange();

        // Read the range address
        range.load("address");
        range.load("values");

        await context.sync();

        try {
          await fn(range);
        } catch (error) {
          range.values[0][0] = JSON.stringify(error.message, undefined, 2);
          console.error(error);
        }

        await context.sync();
      });
    }
    catch(e) { console.error(e); }
  }
  
  registerHashflare = async() => {
    await this.doRange(this.registerHashflareEx);
  }
  
  addMined = async() => {
    await this.doRange(this.addMinedEx)
  }

  buyHashrate = async() => {
    await this.doRange(this.buyHashrateEx)
  }
  
  createAccount = async() => {
    await this.doRange(this.createAccountEx);
  }

  createCoin = async() => {
    await this.doRange(this.createCoinEx);
  }
  
  recordTransaction = async() => {
    await this.doRange(this.recordTransactionEx)
  }
  
  updateQuote = async() => {
    await this.doRange(this.updateQuoteEx)
  }

  registerHashflareEx = async(range : Excel.Range) => {
    var data = range.values;
    var rInput = new RangeInput(data);

    if (data.length < 1 || data[0].length < 2)
      return;

    var username = data[0][0];
    var timestamp = this.ExcelDateToJSDate(data[0][1]).getTime();

    const mutation = `mutation {
        registerHashflare( username : "${username}", timestamp : ${timestamp} )
    }`;
    console.log(mutation);
    await request(this.server, mutation);
  }

  addMinedEx = async(range : Excel.Range) => {
    var data = range.values;
    var input = new RangeInput(data);

    for(var m of input.getRows())
    {
      var timestamp = this.ExcelDateToJSDate(m.get("Date")).getTime();
      const mutation = `mutation {
        addMinedAmount (type : "${m.get("Product")}", quantity : ${m.get("Quantity")},  timestamp : ${timestamp} )
      }`;
      console.log(mutation);
      await request(this.server, mutation);
    }
  }
  
  buyHashrateEx = async(range : Excel.Range) => {
    var data = range.values;
    var input = new RangeInput(data);

    var mutations : {mutation : string, timestamp : number}[] = [];
    for(var m of input.getRows())
    {
      var timestamp = this.ExcelDateToJSDate(m.get("Date")).getTime();
      const mutation = `mutation {
        buyHashrate ( txId : "${m.get("TxId")}", type : "${m.get("Product")}", quantity : ${m.get("Quantity")}, total : ${m.get("Total")}, timestamp : ${timestamp} )
      }`;
      // console.log(mutation);
      mutations.push({mutation, timestamp});
      // await request(this.server, mutation);
    }

    mutations.sort((a, b) => a.timestamp > b.timestamp ? 1 : -1);
    for(var x of mutations) {
      console.log(x.mutation);
      await request(this.server, x.mutation);
    }
  }

  createAccountEx = async(range : Excel.Range) => {
    var data = range.values;
    var rInput = new RangeInput(data);
    
    var names : any[];
    var types : any[];

    const rows = rInput.getRows();
    const nRows = rows.length;
    names = rows.map(v => v.get("Name"));
    types = rows.map(v => v.get("Type"));
    
    if (names == undefined || types == undefined)
      return;
    
    for(var i = 0; i < nRows; i++)
    {
      const mutation = `mutation {
        createAccount( name : "${names[i]}", type : "${types[i]}")
      }`;
      console.log(mutation);
      await request(this.server, mutation);
    }
  }
  
  createCoinEx = async(range : Excel.Range) => {
    var data = range.values;
    var rInput = new RangeInput(data);

    var names : any[];
    var tickers: any[];
    if (data.length == 1 && data[0].length == 2) {
      names = data[0][0];
      tickers = data[0][1];
    }
    else {
      const rows = rInput.getRows();
      names = rows.map(v => v.get("Name"));
      tickers = rows.map(v => v.get("Ticker"));
    }

    if (names != undefined && tickers != undefined) {
      for( var n of names.map((x, i) => [x, tickers[i]] ))
      {
        const mutation = `mutation { 
                createCoin( command : { name : "${n[0]}", ticker : "${n[1]}" } )
              }`;
        console.log(mutation);
        await request(this.server, mutation);
      }
    }
    else {
      console.error("Name header not found!")
    }
  }
  
  recordTransactionEx = async(range : Excel.Range) => {
    var data = range.values;
    var rInput = new RangeInput(data);
    
    var txId : any[]
    var amounts : any[]
    var assets : any[]
    var comments : any[]
    var dates : any[]
    var res : any[][]
    var quotes : any[]
    
    if (data.length < 1 || data[0].length < 5)
      return null
    
    if (data.length == 1 && data[0].length >= 5) {
      txId = data[0][0];
      amounts = data[0][1];
      assets = data[0][2];
      comments = data[0][3];
      dates = data[0][4];
      res = [ data[0][0], data[0][1], data[0][2], data[0][3], data[0][4], this.ExcelDateToJSDate(data[0][4]).toISOString() ]
    }
    else {
      const rows = rInput.getRows();
      const hasTotal =  rInput.headers.indexOf("Total") > 0
      res = rows.map(v => [ v.get("TxId"), v.get("Amount"), v.get("Asset"), v.get("Type"), v.get("Comment"), this.ExcelDateToJSDate(v.get("Date")).toISOString(), hasTotal ? v.get("Total") : 0 ])
        
    }
    
    console.log(res)
    
    if (res != undefined) {
      for (const r of res) {
        const mutation = `mutation {
              recordTransaction( txId : "${r[0]}", amount : ${r[1]}, assetId : "${r[2]}", type : "${r[3]}", comment : "${r[4]}", date : "${r[5]}" )
            }`
        console.log(mutation)
        await request(this.server, mutation) 
        
        if (r[6] > 0)
        {
          const quoteMutation = `mutation {
            addTransactionQuote( assetId : "USD", txId : "${r[0]}", amount : ${r[6]})
          }`
          console.log(quoteMutation)
          await request(this.server, quoteMutation)
        }
      }
    }
    else {
      console.error("Headers not found")
    }
  }

  updateQuoteEx = async(range : Excel.Range) => {
    var data = range.values;
    var nRows = data.length;
    if (data[0].length == 3)
    {
      for (var i = 0; i < nRows; i++)
      {
        const mutation = `mutation {
            updateQuote( forAsset : "${data[i][1]}", domAsset : "${data[i][2]}", date : "${this.ExcelDateToJSDate(data[i][0]).toISOString()}" )
          }`
        console.log(mutation)
        await request(this.server, mutation)
      }
    }
    else {
      console.error("Domestic or foreign asset not found")
    }
  }
    
  
  render() {
    const {
      title,
      isOfficeInitialized,
    } = this.props;

    if (!isOfficeInitialized) {
      return (
        <Progress
          title={title}
          logo='assets/logo-filled.png'
          message='Please sideload your addin to see app body.'
        />
      );
    }

    return (
      <div className='ms-welcome'>
        <Header logo='assets/logo-filled.png' title={this.props.title} message='Welcome' />
        <HeroList message='' items={this.state.listItems}>
          <Button className='ms-coin__action' buttonType={ButtonType.hero} iconProps={{ iconName: 'ChevronRight' }} onClick={this.registerHashflare}>Register hashflare</Button>
          <Button className='ms-coin__action' buttonType={ButtonType.hero} iconProps={{ iconName: 'ChevronRight' }} onClick={this.buyHashrate}>Buy hashrate</Button>
          <Button className='ms-coin__action' buttonType={ButtonType.hero} iconProps={{ iconName: 'ChevronRight' }} onClick={this.addMined}>Add mined amount</Button>

          <Button className='ms-coin__action' buttonType={ButtonType.hero} iconProps={{ iconName: 'ChevronRight' }} onClick={this.createAccount}>Create account</Button>
          <Button className='ms-coin__action' buttonType={ButtonType.hero} iconProps={{ iconName: 'ChevronRight' }} onClick={this.createCoin}>Create coin</Button>
          <Button className='ms-coin__action' buttonType={ButtonType.hero} iconProps={{ iconName: 'ChevronRight' }} onClick={this.recordTransaction}>Record transaction</Button>

          <Button className='ms-coin__action' buttonType={ButtonType.hero} iconProps={{ iconName: 'ChevronRight' }} onClick={this.updateQuote}>Update quote</Button>

        </HeroList>
      </div>
    );
  }
}
