
using System;

namespace cosmicconsole{

    public class Command
    {
        public string alias;
        public string description;
        public Action<string[]> method;
    }
}
