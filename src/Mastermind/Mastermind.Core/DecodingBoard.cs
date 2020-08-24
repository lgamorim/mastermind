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
            FindBlackKeyPegs(code, keyPegs);
            FindWhiteKeyPegs(code, keyPegs);

            var blackKeyPegs = keyPegs.Count(k => k == KeyPeg.Black);
            var whiteKeyPegs = keyPegs.Count(k => k == KeyPeg.White);
            var response = new Response(blackKeyPegs, whiteKeyPegs);

            return response;
        }

        public bool HasSolvedSecretCode(Response response)
        {
            var totalKeyPegs = response.BlackKeyPegs + response.WhiteKeyPegs;
            if (totalKeyPegs < 0 || totalKeyPegs > Shield.Count) throw new ArgumentException();
            
            return response.BlackKeyPegs == Shield.Count;
        }

        private void FindBlackKeyPegs(CodePeg[] code, KeyPeg?[] keyPegs)
        {
            for (var i = 0; i < code.Length; i++)
                if (Shield.HasColorAt(i, code[i]))
                    keyPegs[i] = KeyPeg.Black;
        }

        private void FindWhiteKeyPegs(CodePeg[] code, KeyPeg?[] keyPegs)
        {
            foreach (var color in code)
                for (var i = 0; i < code.Length; i++)
                    if (keyPegs[i] is null && Shield.HasColorAt(i, color))
                    {
                        keyPegs[i] = KeyPeg.White;
                        break;
                    }
        }
    }
}