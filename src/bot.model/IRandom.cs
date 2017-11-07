using System;

namespace bot.model
{
    public interface IRandom:IService
    {
        int Get();
    }

    public class MyRandom: IRandom
    {
        private readonly Random _random;

        public MyRandom()
        {
            _random = new Random();
        }


        public int Get()
        {
            return _random.Next();
        }
    }
}
