using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX42;
using QuickFix.Transport;
using System;
using System.Threading;

namespace OrderGenerator
{
    class Program : MessageCracker, IApplication
    {
        private static readonly Random random = new Random();

        //Métodos da interface IApplication (nao estao fazendo nada, mas fui obrigado a colocar pois a interface exige).
        public void OnCreate(SessionID sessionID) { }
        public void OnLogon(SessionID sessionID) { }
        public void OnLogout(SessionID sessionID) { }
        public void ToAdmin(QuickFix.Message message, SessionID sessionID) { }
        public void FromAdmin(QuickFix.Message message, SessionID sessionID) { }
        public void ToApp(QuickFix.Message message, SessionID sessionID) { }
        public void FromApp(QuickFix.Message message, SessionID sessionID) { }

        //Simbolos das açoes
        private static string[] Symbols = { "PETR4", "VALE3", "VIIA4" };
        //Lados das ordens compra/venda
        private static string[] Sides = { Side.BUY.ToString(), Side.SELL.ToString() };

        static void Main(string[] args)
        {
            //Criando uma sessionsettings para configurar a sessão.
            SessionSettings settings = new SessionSettings(@"C:\\Users\\SOLANO\\source\\repos\\OrderGenerator\\OrderGenerator\\quickfix.cfg");

            //Instancia de program para usar como aplicação fix.
            IApplication app = new Program();

            //Armazenar mensagens fix em um arquivo.
            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);

            //Armazenar logs em um arquivo
            ILogFactory logFactory = new FileLogFactory(settings);

            //Iniciar uma sessao fix.
            SocketInitiator initiator = new SocketInitiator(app, storeFactory, settings, logFactory);
            initiator.Start();
            
            while (true)
            {
                Thread.Sleep(1000);
                GenerateOrder();
            }
        }

        public static void GenerateOrder()
        {
            try
            {
                //Gerar aleatóriamente simbolo, lado de ordem, qtd e preço
                string symbol = Symbols[random.Next(Symbols.Length)];
                string side = Sides[random.Next(Sides.Length)];
                int qty = random.Next(1, 100000);
                decimal price = Math.Round((decimal)random.NextDouble() * 1000, 2);

                //Campos apropriados para order NewOrderSingle
                NewOrderSingle order = new NewOrderSingle(
                    new ClOrdID(DateTime.Now.Ticks.ToString()),
                    new HandlInst('1'),
                    new Symbol(symbol),
                    new Side(side[0]),
                    new TransactTime(DateTime.UtcNow),
                    new OrdType(OrdType.LIMIT));

                //Atribuições
                order.OrderQty = new OrderQty(qty);
                order.Price = new Price(price);

                //Envio
                Session.SendToTarget(order, new SessionID("FIX.4.2", "OrderGenerator", "OrderAccumulator"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating order: {ex.Message}");
            }
        }
    }
}
