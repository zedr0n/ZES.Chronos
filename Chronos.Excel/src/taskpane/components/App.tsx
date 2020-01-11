import * as React from 'react';
import { Button, ButtonType } from 'office-ui-fabric-react';
import Header from './Header';
import HeroList, { HeroListItem } from './HeroList';
import Progress from './Progress';
import { request } from 'graphql-request';
import RangeInput from "./RangeInput";

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
  
  createAccount = async() => {
    await this.doRange(this.createAccountEx);
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
      await request('https://localhost:5001', mutation);
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
        await request('https://localhost:5001', mutation);
      }
    }
    else {
      console.error("Name header not found!")
    }
  }

  createCoin = async() => {
    await this.doRange(this.createCoinEx);
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
          <Button className='ms-coin__action' buttonType={ButtonType.hero} iconProps={{ iconName: 'ChevronRight' }} onClick={this.createAccount}>Create account</Button>
          <Button className='ms-coin__action' buttonType={ButtonType.hero} iconProps={{ iconName: 'ChevronRight' }} onClick={this.createCoin}>Create coin</Button>
        </HeroList>
      </div>
    );
  }
}
