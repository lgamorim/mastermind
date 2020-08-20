using System;

namespace Mastermind.Core
{
    public class DecodingBoard
    {
        public Shield Shield { get; private set; }

        public void CodeMaker(Shield shield)
        {
            if(shield is null) throw new ArgumentNullException();
            Shield = shield;
        }
    }
}