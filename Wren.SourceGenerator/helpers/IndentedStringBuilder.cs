using System.Text;

namespace Wren.SourceGenerator;

    public sealed class IndentedStringBuilder
    {
        private readonly StringBuilder sb = new();
        private readonly string indentUnit;
        private int indentLevel;
        private bool atLineStart = true;
 
        public IndentedStringBuilder(string indentUnit = "    ")
        {
            this.indentUnit = indentUnit;
        }
 
        public int IndentLevel => indentLevel;
 
        public IndentedStringBuilder Indent(int level = 1)
        {
            indentLevel += level;
            return this;
        }
 
        public IndentedStringBuilder Unindent(int level = 1)
        { 
            indentLevel = Math.Max(indentLevel - level, 0);
            return this;
        }
 
        public IndentScope Indented() => new(this);
 
        public IndentedStringBuilder Append(string text)
        {
            WriteIndentIfNeeded();
            sb.Append(text);
            return this;
        }
 
        public IndentedStringBuilder Append(char c)
        {
            WriteIndentIfNeeded();
            sb.Append(c);
            if (c == '\n')
                atLineStart = true;
            return this;
        }
 
        public IndentedStringBuilder AppendLine()
        {
            sb.AppendLine();
            atLineStart = true;
            return this;
        }
 
        public IndentedStringBuilder AppendLine(string text)
        {
            WriteIndentIfNeeded();
            sb.Append(text);
            sb.AppendLine();
            atLineStart = true;
            return this;
        }
 
        public IndentedStringBuilder AppendBlock(string openLine, Action<IndentedStringBuilder> body, string closeLine)
        {
            AppendLine(openLine);
            using (Indented())
            {
                body(this);
            }
            AppendLine(closeLine);
            return this;
        }
 
        public IndentedStringBuilder Clear()
        {
            sb.Clear();
            indentLevel = 0;
            atLineStart = true;
            return this;
        }
 
        public override string ToString() => sb.ToString();
 
        private void WriteIndentIfNeeded()
        {
            if (atLineStart && indentLevel > 0)
            {
                for (int i = 0; i < indentLevel; i++)
                    sb.Append(indentUnit);
            }
            atLineStart = false;
        }
 
        public readonly struct IndentScope : IDisposable
        {
            private readonly IndentedStringBuilder owner;
 
            internal IndentScope(IndentedStringBuilder owner)
            {
                this.owner = owner;
                this.owner.Indent();
            }
 
            public void Dispose() => owner.Unindent();
        }
    }
