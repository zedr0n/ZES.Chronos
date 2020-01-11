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

  createCoin = async() => {
    try{
      await Excel.run(async context => {
        /**
         * Insert your Excel code here
         */
        const range = context.workbook.getSelectedRange();

        // Read the range address
        range.load("address");
        range.load("values");

        await context.sync();
        
        try {
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
        catch (error) {
          range.values[0][0] = JSON.stringify(error.message, undefined, 2);
          console.error(error);
        }
        
        await context.sync();
      })
    } 
    catch (error) {console.error(error);}
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
          <Button className='ms-coin__action' buttonType={ButtonType.hero} iconProps={{ iconName: 'ChevronRight' }} onClick={this.createCoin}>Create coin</Button>
        </HeroList>
      </div>
    );
  }
}
