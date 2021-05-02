using System;
using System.Linq;

namespace Mastermind.Core
{
    public class DecodingBoard
    {
        public DecodingBoard(BoardConfig boardConfig)
        {
            if (boardConfig.ShieldSize <= 0 || boardConfig.TotalRows <= 0)
                throw new ArgumentException(nameof(boardConfig));
            
            BoardConfig = boardConfig;
        }

        public BoardConfig BoardConfig { get; }

        public Shield Shield { get; private set; }

        public void CodeMaker(Shield shield)
        {
            if (shield is null) throw new ArgumentNullException(nameof(shield));
            if (shield.Count != BoardConfig.ShieldSize) throw new ArgumentException(nameof(shield));
            Shield = shield;
        }

        public Response CodeBreaker(CodePeg[] code)
        {
            if (code is null) throw new ArgumentNullException(nameof(code));
            if (code.Length != Shield.Count) throw new ArgumentException(nameof(code));

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
            if (totalKeyPegs < 0 || totalKeyPegs > Shield.Count) throw new ArgumentException(nameof(response));

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