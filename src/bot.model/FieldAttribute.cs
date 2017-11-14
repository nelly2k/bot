using System;

namespace bot.model
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAttribute:Attribute
    {
        public string Title { get; set; }

        public FieldAttribute(string title)
        {
            Title = title;
        }
    }
}