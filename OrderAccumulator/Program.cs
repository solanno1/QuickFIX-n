using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX42;
using System;
using System.Collections.Generic;
using QuickFix.Transport;

namespace OrderAccumulator
{
    // https://new.quickfixn.org/n/documentation/
    class Program : MessageCracker, IApplication
    {        
        private readonly Dictionary<string, decimal> exposureBySymbol = new Dictionary<string, decimal>();
        private const decimal LIMIT = 1000000M;

        //Métodos da interface IApplication (nao estao fazendo nada, mas fui obrigado a colocar pois a interface exige).
        public void OnCreate(SessionID sessionID) { }
        public void OnLogon(SessionID sessionID) { }
        public void OnLogout(SessionID sessionID) { }
        public void ToAdmin(QuickFix.Message message, SessionID sessionID) { }
        public void FromAdmin(QuickFix.Message message, SessionID sessionID) { }
        public void ToApp(QuickFix.Message message, SessionID sessionID) { }

        //Implementação que chama a função crack que processa as mensagens FIX.
        public void FromApp(QuickFix.Message message, SessionID sessionID)
        {
            Crack(message, sessionID);
        }

        //Implementação para quando uma mensagens NewOrderSingle é recebida.
        public void OnMessage(NewOrderSingle order, SessionID sessionID)
        {
            //Extração de informações.
            string symbol = order.Symbol.Obj;
            decimal price = order.Price.Obj;
            decimal qty = order.OrderQty.Obj;
            char side = order.Side.Obj;

            //Cálculo de acordo com o exercício proposto.
            decimal newExposure = price * qty * (side == Side.BUY ? 1 : -1);

            //Verificação para ver se a exposição atual por simbolo já existe.
            if (exposureBySymbol.ContainsKey(symbol))
            {
                newExposure += exposureBySymbol[symbol];
            }

            //Verificar se passa do limite pré-definido.
            if (Math.Abs(newExposure) > LIMIT)
            {
                OrderCancelReject reject = new OrderCancelReject(
                 new OrderID("MissingOrderID"),  
                 new ClOrdID(order.ClOrdID.Obj),
                 new OrigClOrdID(order.ClOrdID.Obj),
                 new OrdStatus(OrdStatus.REJECTED),
                 new CxlRejResponseTo(CxlRejResponseTo.ORDER_CANCEL_REQUEST)
            );
                Session.SendToTarget(reject, sessionID);
            }
            else
            {
                exposureBySymbol[symbol] = newExposure;
                ExecutionReport report = new ExecutionReport(
                new OrderID("OrderID"),
                new ExecID("ExecID"),
                new ExecTransType(ExecTransType.NEW),
                new ExecType(ExecType.NEW),
                new OrdStatus(OrdStatus.NEW),
                new Symbol(symbol), 
                new Side(side), 
                new LeavesQty(0), 
                new CumQty(0), 
                new AvgPx(0.0m)
                );
                Session.SendToTarget(report, sessionID);
            }
        }

        static void Main(string[] args)
        {
            SessionSettings settings = new SessionSettings(@"C:\\Users\\SOLANO\\source\\repos\\OrderGenerator\\OrderAccumulator\\quickfix-accumulator.cfg");
            IApplication app = new Program();
            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
            ILogFactory logFactory = new FileLogFactory(settings);
            ThreadedSocketAcceptor acceptor = new ThreadedSocketAcceptor(app, storeFactory, settings, logFactory);

            acceptor.Start();
        }
    }
}
