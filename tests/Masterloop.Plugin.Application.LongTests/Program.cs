using System;

namespace Masterloop.Plugin.Application.LongTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Masterloop.Plugin.Application.LongTests");
            ObservationSubscriber oSub = new ObservationSubscriber("", "", "", true);
            if (oSub.Init())
            {
                oSub.Run();
            }
        }
    }
}
