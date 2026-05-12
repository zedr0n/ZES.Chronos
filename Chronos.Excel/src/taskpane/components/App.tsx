import * as React from "react";
import {Checkbox, Dropdown, IDropdownOption, PrimaryButton, SearchBox} from "@fluentui/react";
import Header from "./Header";
import Progress from "./Progress";
import { request } from 'graphql-request';
import RangeInput from "./RangeInput";

declare global {
  interface Window {
    server: string;
    period: number;
    riderProjects: { name: string; root: string }[];
    console : any;
  }
}

window.server = "https://localhost:5001";
window.period = 5000;
window.riderProjects = [
  { name: "ZES.Chronos", root: "D:\\dev\\ZES.Chronos" },
  { name: "ZES", root: "D:\\dev\\ZES" }
];
window.console = console;

export interface AppProps {
  title: string;
  isOfficeInitialized: boolean;
}

export interface AppState {
  logs: string[];
  isLoadingLogs: boolean;
  logLevelFilter: string;
  logSearch: string;
  showRetroactiveExecution: boolean;
  showThreadNumber: boolean;
  logError?: string;
}

export default class App extends React.Component<AppProps, AppState> {
  constructor(props, context) {
    super(props, context);
    this.state = {
      logs: [],
      isLoadingLogs: false,
      logLevelFilter: "all",
      logSearch: "",
      showRetroactiveExecution: true,
      showThreadNumber: false
    };
  }
  
  server : string = "https://localhost:5001";
  logRefreshTimer?: number;
  logWindowRef = React.createRef<HTMLDivElement>();
  logLevelOptions: IDropdownOption[] = [
    { key: "all", text: "All" },
    { key: "E", text: "Errors" },
    { key: "W", text: "Warnings" },
    { key: "I", text: "Info" },
    { key: "D", text: "Debug" },
    { key: "T", text: "Trace" }
  ];

  componentDidMount() {
    this.refreshLogs();
    this.logRefreshTimer = window.setInterval(this.refreshLogs, window.period);
  }

  componentWillUnmount() {
    if (this.logRefreshTimer !== undefined) {
      window.clearInterval(this.logRefreshTimer);
    }
  }

  componentDidUpdate(previousProps: AppProps, previousState: AppState) {
    const previousLogs = previousState.logs.join("\n");
    const currentLogs = this.state.logs.join("\n");
    if (previousLogs !== currentLogs || previousState.logError !== this.state.logError) {
      this.scrollLogsToBottom();
    }
  }

  scrollLogsToBottom = () => {
    const logWindow = this.logWindowRef.current;
    if (logWindow) {
      logWindow.scrollTop = logWindow.scrollHeight;
    }
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
 
  addTransaction = async() => {
    await this.doRange(this.addTransactionEx)
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
 
  addTransactionEx = async(range : Excel.Range) => {
    var data = range.values;
    var rInput = new RangeInput(data);

    var account : any[]
    var txId : any[]
    var res : any[][]
    
    if (data.length < 1 || data[0].length < 2)
      return null

    const rows = rInput.getRows();
    const hasTotal =  rInput.headers.indexOf("Total") > 0
    res = rows.map(v => [ v.get("Account"), v.get("TxId") ])

    console.log(res)

    if (res != undefined) {
      for (const r of res) {
        const mutation = `mutation {
              addTransaction( name : "${r[0]}", txId : "${r[1]}")
            }`
        console.log(mutation)
        await request(this.server, mutation)
      }
    }
    else {
      console.error("Headers not found")
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

  flushLog = async() =>
  {
    const mutation = "mutation { flushLog }"
    await request(window.server, mutation)
    await this.refreshLogs()
  }

  refreshLogs = async() =>
  {
    this.setState({ isLoadingLogs: true, logError: undefined });
    try {
      const result = await request(window.server, "{ logs }") as { logs?: string[] };
      this.setState({
        logs: result.logs || [],
        isLoadingLogs: false
      });
    }
    catch (error) {
      const message = error && error.message ? error.message : String(error);
      this.setState({
        isLoadingLogs: false,
        logError: message
      });
    }
  }

  logClassName = (message: string) => {
    if (message.indexOf("|E|") >= 0) {
      return "ms-chronos-log__entry ms-chronos-log__entry--error";
    }
    if (message.indexOf("|W|") >= 0) {
      return "ms-chronos-log__entry ms-chronos-log__entry--warning";
    }
    if (message.indexOf("|D|") >= 0) {
      return "ms-chronos-log__entry ms-chronos-log__entry--debug";
    }
    if (message.indexOf("|T|") >= 0) {
      return "ms-chronos-log__entry ms-chronos-log__entry--trace";
    }
    if (message.indexOf("|I|") >= 0) {
      return "ms-chronos-log__entry ms-chronos-log__entry--info";
    }
    return "ms-chronos-log__entry ms-chronos-log__entry--info";
  }

  renderLogEntries = () => {
    if (this.state.logError) {
      return <div className='ms-chronos-log__entry ms-chronos-log__entry--error'>{this.renderLogMessage(this.displayLogMessage(this.state.logError))}</div>;
    }

    const logs = this.filteredLogs();
    if (logs.length === 0) {
      return <div className='ms-chronos-log__empty'>No log messages.</div>;
    }

    return logs.map((message, index) =>
      <div className={this.logClassName(message)} key={index}>{this.renderLogMessage(this.displayLogMessage(message))}</div>
    );
  }

  filteredLogs = () => {
    let logs = this.state.logs;

    if (this.state.logLevelFilter !== "all") {
      const marker = `|${this.state.logLevelFilter}|`;
      logs = logs.filter(message => message.indexOf(marker) >= 0);
    }

    if (!this.state.showRetroactiveExecution) {
      logs = logs.filter(message => message.indexOf("Retroactive execution :") < 0);
    }

    const search = this.state.logSearch.trim().toLowerCase();
    if (search.length > 0) {
      logs = logs.filter(message => message.toLowerCase().indexOf(search) >= 0);
    }

    return logs;
  }

  setLogLevelFilter = (event: React.FormEvent<HTMLDivElement>, option?: IDropdownOption) => {
    if (option) {
      this.setState({ logLevelFilter: option.key as string });
    }
  }

  setShowRetroactiveExecution = (event?: React.FormEvent<HTMLElement | HTMLInputElement>, checked?: boolean) => {
    this.setState({ showRetroactiveExecution: checked !== false });
  }

  setShowThreadNumber = (event?: React.FormEvent<HTMLElement | HTMLInputElement>, checked?: boolean) => {
    this.setState({ showThreadNumber: checked === true });
  }

  setLogSearch = (event?: React.ChangeEvent<HTMLInputElement>, value?: string) => {
    this.setState({ logSearch: value || "" });
  }

  displayLogMessage = (message: string) => {
    return this.state.showThreadNumber ? message : message.replace(/^<\s*\d+\s*>\s*/, "");
  }

  renderLogMessage = (message: string) => {
    const linkPattern = /(https?:\/\/[^\s<>"']+|[A-Za-z]:\\[^\r\n\]]+\.[A-Za-z0-9]+(?::line \d+)?)/g;
    const parts: React.ReactNode[] = [];
    let lastIndex = 0;
    let match: RegExpExecArray | null;

    while ((match = linkPattern.exec(message)) !== null) {
      if (match.index > lastIndex) {
        parts.push(message.substring(lastIndex, match.index));
      }

      const text = match[0];
      const trailing = text.match(/[),.;:]+$/);
      const linkText = trailing ? text.substring(0, text.length - trailing[0].length) : text;

      parts.push(
        <a className='ms-chronos-log__link' href={this.logLinkHref(linkText)} key={parts.length} target='_blank' rel='noreferrer'>
          {linkText}
        </a>
      );

      if (trailing) {
        parts.push(trailing[0]);
      }

      lastIndex = match.index + text.length;
    }

    if (lastIndex < message.length) {
      parts.push(message.substring(lastIndex));
    }

    return parts;
  }

  logLinkHref = (text: string) => {
    if (text.indexOf("http://") === 0 || text.indexOf("https://") === 0) {
      return text;
    }

    const lineMatch = text.match(/:line (\d+)$/);
    const filePath = text.replace(/:line \d+$/, "");
    const project = this.riderProjectForPath(filePath);
    const path = project
      ? filePath.substring(project.root.length).replace(/^\\/, "").replace(/\\/g, "/")
      : filePath.replace(/\\/g, "/");
    const pathWithLine = lineMatch ? `${path}:${lineMatch[1]}` : path;

    if (project) {
      return `jetbrains://rd/navigate/reference?project=${encodeURIComponent(project.name)}&path=${encodeURIComponent(pathWithLine)}`;
    }

    return `jetbrains://rd/navigate/reference?path=${encodeURIComponent(pathWithLine)}`;
  }

  riderProjectForPath = (filePath: string) => {
    const normalized = filePath.toLowerCase();
    return window.riderProjects
      .filter(project => normalized.indexOf(project.root.toLowerCase()) === 0)
      .sort((a, b) => b.root.length - a.root.length)[0];
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
        <Header logo='assets/logo-filled.png' title={this.props.title} message='Chronos' />
        <div style={{display: "flex", justifyContent: "center"}}>
          <PrimaryButton onClick={this.flushLog}>Flush log</PrimaryButton>
        </div>
        <main className='ms-welcome__main'>
          <section className='ms-chronos-log'>
            <div className='ms-chronos-log__header'>
              <h2>Log</h2>
              <div className='ms-chronos-log__tools'>
                <Dropdown
                  ariaLabel='Log message type'
                  className='ms-chronos-log__filter'
                  options={this.logLevelOptions}
                  selectedKey={this.state.logLevelFilter}
                  onChange={this.setLogLevelFilter}
                />
                <Checkbox
                  checked={this.state.showRetroactiveExecution}
                  className='ms-chronos-log__checkbox'
                  label='Retroactive'
                  onChange={this.setShowRetroactiveExecution}
                />
                <Checkbox
                  checked={this.state.showThreadNumber}
                  className='ms-chronos-log__checkbox'
                  label='Thread'
                  onChange={this.setShowThreadNumber}
                />
                <SearchBox
                  className='ms-chronos-log__search'
                  placeholder='Search'
                  value={this.state.logSearch}
                  onChange={this.setLogSearch}
                />
                {this.state.isLoadingLogs && <span>Refreshing</span>}
              </div>
            </div>
            <div className='ms-chronos-log__window' ref={this.logWindowRef}>
              {this.renderLogEntries()}
            </div>
          </section>
        </main>
      </div>
    );
  }
}
