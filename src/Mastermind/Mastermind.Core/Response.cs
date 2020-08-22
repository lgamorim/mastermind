namespace Mastermind.Core
{
    public readonly struct Response
    {
        public Response(int blackKeyPegs, int whiteKeyPegs)
        {
            BlackKeyPegs = blackKeyPegs;
            WhiteKeyPegs = whiteKeyPegs;
        }
        
        public int BlackKeyPegs { get; }
        
        public int WhiteKeyPegs { get; }
    }
}