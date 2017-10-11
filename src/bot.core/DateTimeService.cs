using System;
using bot.model;

namespace bot.core
{
    public class DateTimeService:IDateTime
    {
        public DateTime Now =>DateTime.Now;
    }
}
