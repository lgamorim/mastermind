using System;

namespace Mastermind.Core
{
    public class Shield
    {
        private readonly CodePeg[] colors;

        public Shield(CodePeg[] colors)
        {
            if (colors is null) throw new ArgumentNullException();
            if (colors.Length == 0) throw new ArgumentException();
            this.colors = colors;
        }

        public CodePeg this[int index] => colors[index];

        public int Count => colors.Length;
    }
}