﻿using Stateless;
using System;
using System.Runtime.CompilerServices;
using System.Timers;

namespace StatePattern
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello State Pattern!");

            // OrderTest();

            IMessageService messageService = new ConsoleMessageService();

            ProxyLamp lamp = new ProxyLamp(new LampStateMachine(messageService));
            // lamp.TimeLimit = TimeSpan.Parse("13:00");

            Console.WriteLine(lamp.Graph);


            Console.WriteLine(lamp.State);

            lamp.PushUp();
            Console.WriteLine(lamp.State);

            
            Console.ReadKey();

            lamp.PushDown();
            Console.WriteLine(lamp.State);

            lamp.PushUp();
            Console.WriteLine(lamp.State);

            lamp.PushUp();
            Console.WriteLine(lamp.State);

            lamp.PushUp();
            Console.WriteLine(lamp.State);

            lamp.PushDown();
            Console.WriteLine(lamp.State);

        }

        private static void OrderTest()
        {
            Order order = Order.Create();

            order.Completion();

            if (order.Status == OrderStatus.Completion)
            {
                order.Status = OrderStatus.Sent;
                Console.WriteLine("Your order was sent.");
            }

            order.Cancel();
        }
    }

    #region Models

    public class Order
    {
        public Order(string orderNumber)
        {
            Status = OrderStatus.Created;

            OrderNumber = orderNumber;
            OrderDate = DateTime.Now;
         
        }

        public DateTime OrderDate { get; set; }

        public string OrderNumber { get; set; }

        public OrderStatus Status { get; set; }

        private static int indexer;

        public static Order Create()
        {
            Order order = new Order($"Order #{indexer++}");

            if (order.Status == OrderStatus.Created)
            {
                Console.WriteLine("Thank you for your order");
            }

            return order;
        }

        public void Completion()
        {
            if (Status == OrderStatus.Created)
            {
                this.Status = OrderStatus.Completion;

                Console.WriteLine("Your order is in progress");
            }
        }

        public void Cancel()
        {
            if (this.Status == OrderStatus.Created || this.Status == OrderStatus.Completion)
            {
                this.Status = OrderStatus.Canceled;

                Console.WriteLine("Your order was cancelled.");
            }
        }

    }

    public enum OrderStatus
    {
        Created,
        Completion,
        Sent,
        Canceled,
        Done
    }


    public interface IMessageService
    {
        void Send(string message);
    }

    public class ConsoleMessageService : IMessageService
    {
        public void Send(string message)
        {
            Console.WriteLine(message);
        }
    }

    public interface ITimeService
    {
        TimeSpan TimeOfDay { get; }
    }

    public class TimeService : ITimeService
    {
        public TimeSpan TimeOfDay => DateTime.Now.TimeOfDay;
    }

    public interface ITimerService
    {
        event ElapsedEventHandler Elapsed;

        void Start();
        void Stop();
    }

    public class TimerService : ITimerService
    {
        public event ElapsedEventHandler Elapsed;

        private Timer timer = new Timer(TimeSpan.FromSeconds(5).TotalMilliseconds);

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }
    }

    public class LampStateMachine : StateMachine<LampState, LampTrigger>
    {
        private ITimeService timeService;

        private int redCounter = 0;

        private ITimerService timer;

        private bool IsOverLimit => redCounter >= 5;

        public TimeSpan TimeLimit { get; set; } = TimeSpan.Parse("13:00");


        private bool IsOverTime => timeService.TimeOfDay >= TimeLimit;

        public LampStateMachine(IMessageService messageService, ITimeService timeService = null, LampState initialState = LampState.Off, ITimerService timer = null)
            : base(initialState)
        {
            this.timeService = timeService ?? new TimeService();
            this.timer = timer ?? new TimerService();

            timer.Elapsed += (s, a) => this.Fire(LampTrigger.ElapsedTime);

            this.Configure(LampState.Off)
               // .Permit(LampTrigger.PushUp, LampState.On)
               .PermitIf(LampTrigger.PushUp, LampState.On, () => IsOverTime, $"Limit > {TimeLimit}")
               .IgnoreIf(LampTrigger.PushUp, () => !IsOverTime, $"Limit <= {TimeLimit}")
               .Ignore(LampTrigger.PushDown)
               .OnEntry(() => messageService.Send("Dziękujemy za wyłączenie światła"), nameof(messageService.Send));

            this.Configure(LampState.On)
                .OnEntry(() => messageService.Send("Pamiętaj o wyłączeniu światła!"), nameof(messageService.Send))
                .OnEntry(() => timer.Start(), "Start timer")
                .Permit(LampTrigger.PushDown, LampState.Off)
                .PermitIf(LampTrigger.PushUp, LampState.Red, () => !IsOverLimit)
                .PermitIf(LampTrigger.PushUp, LampState.Off, () => IsOverLimit)
                .Permit(LampTrigger.ElapsedTime, LampState.Off)
                .OnExit(() => timer.Stop());


            this.Configure(LampState.Red)
                .OnEntry(() => redCounter++)
                .Permit(LampTrigger.PushUp, LampState.On)
                .Permit(LampTrigger.PushDown, LampState.Off);


            this.OnTransitioned(t => Console.WriteLine($"{t.Source} -> {t.Destination}"));
            this.timer = timer;
        }

    }

    // Przykład użycia wzorca Proxy
    public class ProxyLamp : Lamp
    {
        private StateMachine<LampState, LampTrigger> machine;
      
        public string Graph => Stateless.Graph.UmlDotGraph.Format(machine.GetInfo());

        public override LampState State => machine.State;

        public ProxyLamp(StateMachine<LampState, LampTrigger> machine)
            : base()
        {
            this.machine = machine;
        }

        public override void PushUp() => machine.Fire(LampTrigger.PushUp);
        public override void PushDown() => machine.Fire(LampTrigger.PushDown);

    }


    // dotnet add package stateless

    public class Lamp
    {
        public virtual LampState State { get; set; }

        public virtual void PushUp()
        {
            if (State== LampState.Off)
            {
                State = LampState.On;
            }
            else
            if (State == LampState.On)
            {
                State = LampState.Red;
            }
        }

        public virtual void PushDown()
        {
            if (State == LampState.On)
            {
                State = LampState.Off;
            }
            else
           if (State == LampState.Red)
            {
                State = LampState.Off;
            }
        }
    }

    public enum LampTrigger
    {
        PushUp,
        PushDown,
        ElapsedTime
    }

    public enum LampState
    {
        On,
        Off,
        Red
    }

    #endregion

}
