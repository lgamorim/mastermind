using System;
using System.Linq;

namespace Mastermind.Core
{
    public class DecodingBoard
    {
        public Shield Shield { get; private set; }

        public void CodeMaker(Shield shield)
        {
            if (shield is null) throw new ArgumentNullException();
            Shield = shield;
        }

        public Response CodeBreaker(CodePeg[] code)
        {
            if (code is null) throw new ArgumentNullException();
            if (code.Length != Shield.Count) throw new ArgumentException();

            var keyPegs = new KeyPeg?[code.Length];
            for (var i = 0; i < code.Length; i++)
                if (Shield.HasColorAt(i, code[i]))
                    keyPegs[i] = KeyPeg.Black;
                else
                    for (var j = 0; j < code.Length; j++)
                        if (keyPegs[j] != KeyPeg.Black && Shield.HasColorAt(j, code[i]))
                        {
                            keyPegs[i] = KeyPeg.White;
                            break;
                        }

            var blackKeyPegs = keyPegs.Count(k => k == KeyPeg.Black);
            var whiteKeyPegs = keyPegs.Count(k => k == KeyPeg.White);
            var response = new Response(blackKeyPegs, whiteKeyPegs);

            return response;
        }
    }
}